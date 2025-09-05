using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class HudDialogChat : HudElement
{
	private static int historyMax = 30;

	private LimitedList<string> typedMessagesHistory = new LimitedList<string>(100);

	private int typedMessagesHistoryPos = -1;

	private long lastActivityMs = -100000L;

	private int lastMessageInGroupId = -99999;

	private TreeAttribute eventAttr;

	internal bool MultiCommandPasteMode;

	private ClientMain game;

	private int chatWindowInnerWidth = ClientSettings.ChatWindowWidth;

	private int chatWindowInnerHeight = ClientSettings.ChatWindowHeight;

	private int tabsHeight = 23;

	private int chatInputHeight = 25;

	private int horPadding = 6;

	private int verPadding = 3;

	private int bottomOffset = 100;

	private int scrollbarPadding = 1;

	private int scrollbarWidth = 10;

	private GuiTab[] tabs;

	private double lastAlpha = 1.0;

	private bool isLinkChatTyped;

	public override string ToggleKeyCombinationCode => "beginchat";

	public override double InputOrder => 1.1;

	public override double DrawOrder => 0.0;

	public override EnumDialogType DialogType
	{
		get
		{
			if (IsOpened() && focused)
			{
				return EnumDialogType.Dialog;
			}
			return EnumDialogType.HUD;
		}
	}

	public HudDialogChat(ICoreClientAPI capi)
		: base(capi)
	{
		eventAttr = new TreeAttribute();
		eventAttr["key"] = new IntAttribute();
		eventAttr["text"] = new StringAttribute();
		eventAttr["scrolltoEnd"] = new BoolAttribute();
		game = capi.World as ClientMain;
		game.eventManager?.OnNewServerToClientChatLine.Add(OnNewServerToClientChatLine);
		game.eventManager?.OnNewClientToServerChatLine.Add(OnNewClientToServerChatLine);
		game.eventManager?.OnNewClientOnlyChatLine.Add(OnNewClientOnlyChatLine);
		game.ChatHistoryByPlayerGroup[GlobalConstants.GeneralChatGroup] = new LimitedList<string>(historyMax);
		ComposeChatGuis();
		Composers["chat"].UnfocusOwnElements();
		UpdateText();
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("recomposechat").RequiresPrivilege(Privilege.chat)
			.WithDescription("Recompose chat dialogs")
			.HandleWith(CmdChatC)
			.EndSubCommand();
		game.api.ChatCommands.Create("clearchat").WithDescription("Clear all chat history").HandleWith(CmdClearChat);
		game.api.ChatCommands.Create("chatsize").WithDescription("Set the chat dialog width and height (default 400x160)").WithArgs(parsers.OptionalInt("width", 700), parsers.OptionalInt("height", 200))
			.HandleWith(CmdChatSize);
		game.api.ChatCommands.Create("pastemode").WithDescription("Set the chats paste mode. If set to multi pasting multiple lines will produce multiple chat lines.").WithArgs(parsers.WordRange("mode", "single", "multi"))
			.HandleWith(CmdPasteMode);
		game.PacketHandlers[50] = HandlePlayerGroupPacket;
		game.PacketHandlers[49] = HandlePlayerGroupsPacket;
		game.PacketHandlers[57] = HandleGotoGroupPacket;
		ScreenManager.hotkeyManager.SetHotKeyHandler("chatdialog", OnKeyCombinationTab);
		ScreenManager.hotkeyManager.SetHotKeyHandler("beginclientcommand", delegate(KeyCombination kc)
		{
			OnKeyCombinationTab(kc);
			OnKeyCombinationToggle(kc);
			Composers["chat"].GetChatInput("chatinput").SetValue(".");
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("beginservercommand", delegate(KeyCombination kc)
		{
			OnKeyCombinationTab(kc);
			OnKeyCombinationToggle(kc);
			Composers["chat"].GetChatInput("chatinput").SetValue("/");
			return true;
		});
		game.api.RegisterLinkProtocol("screenshot", onLinkClicked);
		game.api.RegisterLinkProtocol("chattype", onChatType);
		game.api.RegisterLinkProtocol("datafolder", onDataFolderLinkClicked);
	}

	private void onDataFolderLinkClicked(LinkTextComponent comp)
	{
		string[] array = comp.Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 2 && array[1] == "worldedit")
		{
			NetUtil.OpenUrlInBrowser(Path.Combine(GamePaths.DataPath, "WorldEdit"));
		}
	}

	private void onLinkClicked(LinkTextComponent comp)
	{
		string[] array = comp.Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 2 && Regex.IsMatch(array[1], "[\\d\\w\\-]+\\.png"))
		{
			string text = Path.Combine(GamePaths.Screenshots, array[1]);
			if (File.Exists(text))
			{
				NetUtil.OpenUrlInBrowser(text);
			}
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		if (ClientSettings.ChatDialogVisible)
		{
			TryOpen();
			UnFocus();
		}
	}

	private TextCommandResult CmdPasteMode(TextCommandCallingArgs args)
	{
		MultiCommandPasteMode = args[0] as string == "multi";
		return TextCommandResult.Success("Pastemode " + args[0]?.ToString() + " set.");
	}

	private TextCommandResult CmdClearChat(TextCommandCallingArgs textCommandCallingArgs)
	{
		foreach (KeyValuePair<int, LimitedList<string>> item in game.ChatHistoryByPlayerGroup)
		{
			item.Value.Clear();
		}
		UpdateText();
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdChatC(TextCommandCallingArgs textCommandCallingArgs)
	{
		ComposeChatGuis();
		UpdateText();
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdChatSize(TextCommandCallingArgs args)
	{
		ClientSettings.ChatWindowWidth = (chatWindowInnerWidth = (int)args[0]);
		ClientSettings.ChatWindowHeight = (chatWindowInnerHeight = (int)args[1]);
		ComposeChatGuis();
		UpdateText();
		return TextCommandResult.Success();
	}

	private void ComposeChatGuis()
	{
		ClearComposers();
		int num = horPadding + chatWindowInnerWidth + scrollbarWidth + horPadding;
		int num2 = tabsHeight + verPadding + chatWindowInnerHeight + verPadding + chatInputHeight + verPadding;
		int num3 = tabsHeight + verPadding + chatWindowInnerHeight + verPadding;
		int num4 = bottomOffset + chatInputHeight + 3 * verPadding;
		int num5 = chatWindowInnerHeight - 2 * scrollbarPadding + 2 * verPadding - 1;
		ElementBounds bounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, num, num2).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, -bottomOffset);
		ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, horPadding, verPadding, chatWindowInnerWidth, chatWindowInnerHeight).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, -num4);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, chatWindowInnerWidth, chatWindowInnerHeight);
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 0.0, chatWindowInnerWidth, chatWindowInnerHeight);
		tabs = new GuiTab[game.OwnPlayerGroupsById.Count];
		int num6 = 0;
		foreach (KeyValuePair<int, PlayerGroup> item in game.OwnPlayerGroupsById)
		{
			tabs[num6++] = new GuiTab
			{
				DataInt = item.Key,
				Name = item.Value.Name
			};
		}
		CairoFont cairoFont = CairoFont.WhiteDetailText().WithFontSize(17f);
		Composers["chat"] = capi.Gui.CreateCompo("chatdialog", bounds).AddGameOverlay(ElementBounds.Fixed(0.0, tabsHeight, num, num2), GuiStyle.DialogLightBgColor).AddChatInput(ElementBounds.Fixed(0.0, num3, num, chatInputHeight), OnTextChanged, "chatinput")
			.AddCompactVerticalScrollbar(OnNewScrollbarValue, ElementBounds.Fixed(num - scrollbarWidth, tabsHeight + scrollbarPadding, scrollbarWidth, num5), "scrollbar")
			.AddHorizontalTabs(tabs, ElementBounds.Fixed(0.0, 0.0, num, tabsHeight), OnTabClicked, cairoFont, cairoFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.Compose();
		CairoFont notifyFont = cairoFont.Clone().WithColor(GuiStyle.DialogDefaultTextColor);
		Composers["chat"].GetHorizontalTabs("tabs").WithAlarmTabs(notifyFont);
		Composers["chat-group-" + GlobalConstants.GeneralChatGroup] = capi.Gui.CreateCompo("chat-group-" + GlobalConstants.GeneralChatGroup, elementBounds.FlatCopy()).BeginClip(elementBounds2.FlatCopy()).AddRichtext("", cairoFont, elementBounds3.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.DamageLogChatGroup] = capi.Gui.CreateCompo("chat-group-damagelog", elementBounds.FlatCopy()).BeginClip(elementBounds2.FlatCopy()).AddRichtext("", cairoFont, elementBounds3.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.InfoLogChatGroup] = capi.Gui.CreateCompo("chat-group-infolog", elementBounds.FlatCopy()).BeginClip(elementBounds2.FlatCopy()).AddRichtext("", cairoFont, elementBounds3.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.ServerInfoChatGroup] = capi.Gui.CreateCompo("chat-group-" + GlobalConstants.ServerInfoChatGroup, elementBounds.FlatCopy()).BeginClip(elementBounds2.FlatCopy()).AddRichtext("", cairoFont, elementBounds3.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		foreach (PlayerGroup value in game.OwnPlayerGroupsById.Values)
		{
			Composers["chat-group-" + value.Uid]?.Dispose();
			Composers["chat-group-" + value.Uid] = capi.Gui.CreateCompo("chat-group-" + value.Uid, elementBounds.FlatCopy()).BeginClip(elementBounds2.FlatCopy()).AddRichtext("", cairoFont, elementBounds3.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose();
		}
		Composers["chat"].GetCompactScrollbar("scrollbar").SetHeights(num5, num5);
		Composers["chat"].UnfocusOwnElements();
	}

	private void OnTabClicked(int groupId)
	{
		game.currentGroupid = groupId;
		int num = tabIndexByGroupId(groupId);
		Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[num] = false;
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(game.currentGroupid))
		{
			game.ChatHistoryByPlayerGroup[game.currentGroupid] = new LimitedList<string>(historyMax);
		}
		UpdateText();
	}

	private void OnNewScrollbarValue(float value)
	{
		GuiElementRichtext richtext = Composers["chat-group-" + game.currentGroupid].GetRichtext("chathistory");
		richtext.Bounds.fixedY = 0f - value;
		richtext.Bounds.CalcWorldBounds();
		lastActivityMs = game.Platform.EllapsedMs;
	}

	private void UpdateText()
	{
		GuiElementRichtext richtext = Composers["chat-group-" + game.currentGroupid].GetRichtext("chathistory");
		LimitedList<string> limitedList = game.ChatHistoryByPlayerGroup[game.currentGroupid];
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (string item in limitedList)
		{
			if (num++ > 0)
			{
				stringBuilder.Append("\r\n");
			}
			stringBuilder.Append(item);
		}
		richtext.SetNewText(stringBuilder.ToString(), CairoFont.WhiteDetailText().WithFontSize(17f));
		GuiElementScrollbar compactScrollbar = Composers["chat"].GetCompactScrollbar("scrollbar");
		compactScrollbar.SetNewTotalHeight((float)richtext.Bounds.fixedHeight + 5f);
		if (!compactScrollbar.mouseDownOnScrollbarHandle)
		{
			compactScrollbar.ScrollToBottom();
		}
	}

	private void OnTextChanged(string text)
	{
	}

	public override void OnRenderGUI(float deltaTime)
	{
		double num = (focused ? 1.0 : Math.Max(0.5, lastAlpha - (double)(deltaTime / 6f)));
		lastAlpha = num;
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			if (item.Key == "chat")
			{
				if (item.Value.Color == null)
				{
					item.Value.Color = new Vec4f(1f, 1f, 1f, 1f);
				}
				item.Value.Color.W = (float)lastAlpha;
				item.Value.Render(deltaTime);
			}
			else if (item.Key == "chat-group-" + game.currentGroupid)
			{
				item.Value.Render(deltaTime);
			}
		}
	}

	public override void OnFinalizeFrame(float dt)
	{
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			item.Value.PostRender(dt);
		}
		if (Focused)
		{
			lastActivityMs = game.Platform.EllapsedMs;
		}
		if (!ClientSettings.AutoChat)
		{
			return;
		}
		if (IsOpened() && game.Platform.EllapsedMs - lastActivityMs > 15000)
		{
			DoClose();
		}
		if (IsOpened() || game.Platform.EllapsedMs - lastActivityMs >= 50 || lastMessageInGroupId <= -99)
		{
			return;
		}
		int currentGroupid = lastMessageInGroupId;
		if (currentGroupid == GlobalConstants.CurrentChatGroup)
		{
			currentGroupid = game.currentGroupid;
		}
		if (ClientSettings.AutoChatOpenSelected)
		{
			if (currentGroupid == game.currentGroupid)
			{
				TryOpen();
				int num = tabIndexByGroupId(currentGroupid);
				if (num >= 0)
				{
					Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[num] = false;
				}
				UpdateText();
			}
		}
		else
		{
			TryOpen();
			int num2 = tabIndexByGroupId(currentGroupid);
			if (num2 >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[num2] = false;
				Composers["chat"].GetHorizontalTabs("tabs").SetValue(num2, callhandler: false);
			}
			game.currentGroupid = currentGroupid;
			UpdateText();
		}
	}

	public override bool OnEscapePressed()
	{
		return TryClose();
	}

	public override bool IsOpened(string dialogComposerName)
	{
		if (IsOpened())
		{
			return dialogComposerName == "chat-group-" + game.currentGroupid;
		}
		return false;
	}

	public override void UnFocus()
	{
		Composers["chat"].UnfocusOwnElements();
		base.UnFocus();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		Composers["chat"].UnfocusOwnElements();
		typedMessagesHistoryPos = -1;
	}

	public override bool TryClose()
	{
		UnFocus();
		return false;
	}

	public void DoClose()
	{
		lastActivityMs = -100000L;
		lastMessageInGroupId = -9999;
		base.TryClose();
	}

	private bool OnKeyCombinationTab(KeyCombination viaKeyComb)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			opened = true;
			OnGuiOpened();
			game.eventManager?.TriggerDialogOpened(this);
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = -9999;
		}
		else
		{
			ClientSettings.ChatDialogVisible = false;
			UnFocus();
			DoClose();
		}
		return true;
	}

	private void onChatType(LinkTextComponent link)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			TryOpen();
		}
		Focus();
		capi.Gui.RequestFocus(this);
		Composers["chat"].FocusElement(0);
		GuiElementChatInput chatInput = Composers["chat"].GetChatInput("chatinput");
		string text = chatInput.GetText();
		chatInput.SetValue((isLinkChatTyped ? "" : text) + link.Href.Substring("chattype://".Length).Replace("&lt;", "<").Replace("&gt;", ">"));
		isLinkChatTyped = true;
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			TryOpen();
		}
		Focus();
		capi.Gui.RequestFocus(this);
		Composers["chat"].FocusElement(0);
		string printableChar = GlKeyNames.GetPrintableChar(viaKeyComb.KeyCode);
		if (!viaKeyComb.Alt && !viaKeyComb.Ctrl && !viaKeyComb.Shift && !string.IsNullOrWhiteSpace(printableChar))
		{
			ignoreNextKeyPress = true;
		}
		return true;
	}

	public override void OnKeyPress(KeyEvent args)
	{
		if (IsOpened())
		{
			base.OnKeyPress(args);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (!IsOpened())
		{
			return;
		}
		GuiElementChatInput chatInput = Composers["chat"].GetChatInput("chatinput");
		if (args.KeyCode == 50)
		{
			UnFocus();
			args.Handled = true;
			return;
		}
		string message = chatInput.GetText();
		if (args.KeyCode == 49 || args.KeyCode == 82)
		{
			if (message.Length != 0)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				game.api.eventapi.TriggerSendChatMessage(game.currentGroupid, ref message, ref handling);
				if (handling == EnumHandling.PassThrough)
				{
					game.eventManager?.TriggerNewClientChatLine(game.currentGroupid, message, EnumChatType.OwnMessage, null);
				}
				if (typedMessagesHistoryPos != 0 || chatInput.GetText() != GetHistoricalMessage(typedMessagesHistoryPos))
				{
					typedMessagesHistory.Add(chatInput.GetText());
				}
				chatInput.SetValue("");
			}
			UnFocus();
			typedMessagesHistoryPos = -1;
			args.Handled = true;
			isLinkChatTyped = false;
			return;
		}
		if (args.KeyCode == 45 && typedMessagesHistoryPos < typedMessagesHistory.Count - 1)
		{
			typedMessagesHistoryPos++;
			chatInput.SetValue(GetHistoricalMessage(typedMessagesHistoryPos));
			chatInput.SetCaretPos(chatInput.GetText().Length);
			args.Handled = true;
			return;
		}
		if (args.KeyCode == 46 && typedMessagesHistoryPos >= 0 && typedMessagesHistory.Count > 0)
		{
			typedMessagesHistoryPos--;
			if (typedMessagesHistoryPos < 0)
			{
				chatInput.SetValue("");
			}
			else
			{
				chatInput.SetValue(GetHistoricalMessage(typedMessagesHistoryPos));
			}
			chatInput.SetCaretPos(chatInput.GetText().Length);
			args.Handled = true;
			return;
		}
		if (args.KeyCode == 104 && args.CtrlPressed)
		{
			string clipboardText = capi.Forms.GetClipboardText();
			clipboardText = clipboardText.Replace("\ufeff", "");
			if (MultiCommandPasteMode || clipboardText.StartsWithOrdinal(".pastemode multi"))
			{
				string[] array = Regex.Split(clipboardText, "(\r\n|\n|\r)");
				for (int i = 0; i < array.Length; i++)
				{
					game.eventManager?.TriggerNewClientChatLine(game.currentGroupid, array[i], EnumChatType.OwnMessage, null);
				}
				args.Handled = true;
				return;
			}
		}
		IntAttribute obj = eventAttr["key"] as IntAttribute;
		StringAttribute stringAttribute = eventAttr["text"] as StringAttribute;
		eventAttr.SetInt("deltacaretpos", 0);
		obj.value = args.KeyCode;
		stringAttribute.value = message;
		game.api.eventapi.PushEvent("chatkeydownpre", eventAttr);
		if (message != stringAttribute.value)
		{
			chatInput.SetValue(stringAttribute.value);
		}
		base.OnKeyDown(args);
		stringAttribute.value = chatInput.GetText();
		game.api.eventapi.PushEvent("chatkeydownpost", eventAttr);
		if (stringAttribute.value != chatInput.GetText())
		{
			chatInput.SetValue(stringAttribute.value, setCaretPosToEnd: false);
			message = stringAttribute.value;
			if (eventAttr.GetInt("deltacaretpos") != 0)
			{
				chatInput.SetCaretPos(chatInput.CaretPosInLine + eventAttr.GetInt("deltacaretpos"));
			}
		}
		if (message.Length == 0)
		{
			isLinkChatTyped = false;
		}
		if (ScreenManager.hotkeyManager.GetHotKeyByCode("chatdialog").DidPress(args, game, game.player, allowCharacterControls: true))
		{
			DoClose();
			UnFocus();
			args.Handled = true;
		}
		else if (focused)
		{
			args.Handled = true;
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		if (IsOpened())
		{
			GuiElement chatInput = Composers["chat"].GetChatInput("chatinput");
			if (chatInput.IsPositionInside(args.X, args.Y))
			{
				Composers["chat"].FocusElement(chatInput.TabIndex);
			}
		}
	}

	public string GetHistoricalMessage(int typedMessagesHistoryPos)
	{
		int num = typedMessagesHistory.Count - 1 - typedMessagesHistoryPos;
		if (num < 0 || num >= typedMessagesHistory.Count)
		{
			return null;
		}
		return typedMessagesHistory[num];
	}

	private int tabIndexByGroupId(int groupId)
	{
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i].DataInt == groupId)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnNewServerToClientChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		if (groupId != game.currentGroupid)
		{
			int num = tabIndexByGroupId(groupId);
			if (num >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[num] = true;
			}
		}
		if (message.Contains("</hk>", StringComparison.InvariantCulture))
		{
			int num2 = message.IndexOfOrdinal("<hk>");
			int num3 = message.IndexOfOrdinal("</hk>");
			if (num3 > num2)
			{
				string text = message.Substring(num2 + 4, num3 - num2 - 4);
				if (capi.Input.HotKeys.TryGetValue(text.ToLowerInvariant(), out var value))
				{
					message = message.Substring(0, num2) + value.CurrentMapping.ToString() + message.Substring(num3 + 5);
				}
			}
		}
		if ((chattype == EnumChatType.Notification || chattype == EnumChatType.CommandSuccess) && groupId != GlobalConstants.InfoLogChatGroup)
		{
			message = "<font color=\"#CCe0cfbb\">" + message + "</font>";
		}
		if (chattype != EnumChatType.OthersMessage && chattype != EnumChatType.JoinLeave && ClientSettings.AutoChat && ClientSettings.AutoChatOpenSelected && groupId != GlobalConstants.DamageLogChatGroup && groupId != GlobalConstants.AllChatGroups && groupId != GlobalConstants.ServerInfoChatGroup)
		{
			int num4 = tabIndexByGroupId(groupId);
			if (num4 >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[num4] = false;
				Composers["chat"].GetHorizontalTabs("tabs").SetValue(num4, callhandler: false);
			}
			if (groupId != GlobalConstants.CurrentChatGroup)
			{
				game.currentGroupid = groupId;
			}
		}
		if (groupId == GlobalConstants.AllChatGroups)
		{
			foreach (int key in game.ChatHistoryByPlayerGroup.Keys)
			{
				game.ChatHistoryByPlayerGroup[key].Add(message);
			}
			UpdateText();
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = game.currentGroupid;
			return;
		}
		if (groupId == GlobalConstants.CurrentChatGroup)
		{
			groupId = game.currentGroupid;
		}
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(groupId))
		{
			game.ChatHistoryByPlayerGroup[groupId] = new LimitedList<string>(historyMax);
		}
		game.ChatHistoryByPlayerGroup[groupId].Add(message);
		if (game.currentGroupid == groupId)
		{
			UpdateText();
		}
		if (groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
		{
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = groupId;
		}
	}

	private void OnNewClientToServerChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		HandleClientMessage(groupId, message);
		if (!message.StartsWithOrdinal(ChatCommandApi.ServerCommandPrefix) && !message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix) && groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
		{
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = groupId;
		}
	}

	private void OnNewClientOnlyChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		if (!(message == "") && message != null)
		{
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				HandleClientCommand(message, groupId);
			}
			else
			{
				game.ShowChatMessage(message);
			}
		}
	}

	public void HandleClientCommand(string message, int groupid)
	{
		message = message.Substring(1);
		int num = message.IndexOf(' ');
		string args;
		string commandName;
		if (num > 0)
		{
			args = message.Substring(num + 1);
			commandName = message.Substring(0, num);
		}
		else
		{
			args = "";
			commandName = message;
		}
		game.api.chatcommandapi.Execute(commandName, game.player, groupid, args);
	}

	public void HandleClientMessage(int groupid, string message)
	{
		if (!(message == "") && message != null)
		{
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				HandleClientCommand(message, groupid);
				return;
			}
			message = message.Substring(0, Math.Min(1024, message.Length));
			game.SendPacketClient(ClientPackets.Chat(groupid, message));
		}
	}

	private void HandleGotoGroupPacket(Packet_Server packet)
	{
		int groupId = packet.GotoGroup.GroupId;
		if (!game.OwnPlayerGroupsById.ContainsKey(groupId))
		{
			return;
		}
		game.currentGroupid = groupId;
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(game.currentGroupid))
		{
			game.ChatHistoryByPlayerGroup[game.currentGroupid] = new LimitedList<string>(historyMax);
		}
		UpdateText();
		GuiTab[] array = Composers["chat"].GetHorizontalTabs("tabs").tabs;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].DataInt == game.currentGroupid)
			{
				Composers["chat"].GetHorizontalTabs("tabs").activeElement = i;
				break;
			}
		}
	}

	private void HandlePlayerGroupsPacket(Packet_Server packet)
	{
		game.OwnPlayerGroupsById.Clear();
		game.OwnPlayerGroupsById[GlobalConstants.GeneralChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-general")
		};
		game.OwnPlayerGroupsById[GlobalConstants.DamageLogChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-damagelog")
		};
		game.OwnPlayerGroupsById[GlobalConstants.InfoLogChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-infolog")
		};
		game.OwnPlayerGroupMemembershipsById.Clear();
		if (game.Player?.Privileges != null && game.player.Privileges.Contains("controlserver") && !game.IsSingleplayer)
		{
			game.OwnPlayerGroupsById[GlobalConstants.ServerInfoChatGroup] = new PlayerGroup
			{
				Name = Lang.Get("chattab-serverinfo")
			};
		}
		for (int i = 0; i < packet.PlayerGroups.GroupsCount; i++)
		{
			PlayerGroup playerGroup = PlayerGroupFromPacket(packet.PlayerGroups.Groups[i]);
			game.OwnPlayerGroupsById[playerGroup.Uid] = playerGroup;
			game.OwnPlayerGroupMemembershipsById[playerGroup.Uid] = new PlayerGroupMembership
			{
				GroupName = playerGroup.Name,
				GroupUid = playerGroup.Uid,
				Level = (EnumPlayerGroupMemberShip)packet.PlayerGroups.Groups[i].Membership
			};
		}
		List<int> list = new List<int>();
		foreach (int key in game.ChatHistoryByPlayerGroup.Keys)
		{
			if (!game.OwnPlayerGroupsById.ContainsKey(key))
			{
				list.Add(key);
			}
		}
		foreach (int item in list)
		{
			game.ChatHistoryByPlayerGroup.Remove(item);
		}
		if (!game.OwnPlayerGroupsById.ContainsKey(game.currentGroupid))
		{
			game.currentGroupid = GlobalConstants.GeneralChatGroup;
		}
		ComposeChatGuis();
	}

	private void HandlePlayerGroupPacket(Packet_Server packet)
	{
		PlayerGroup playerGroup = PlayerGroupFromPacket(packet.PlayerGroup);
		game.OwnPlayerGroupsById[playerGroup.Uid] = playerGroup;
		game.OwnPlayerGroupMemembershipsById[playerGroup.Uid] = new PlayerGroupMembership
		{
			GroupName = playerGroup.Name,
			GroupUid = playerGroup.Uid,
			Level = (EnumPlayerGroupMemberShip)packet.PlayerGroup.Membership
		};
		ComposeChatGuis();
	}

	private PlayerGroup PlayerGroupFromPacket(Packet_PlayerGroup packet)
	{
		PlayerGroup playerGroup = new PlayerGroup
		{
			Name = packet.Name,
			OwnerUID = packet.Owneruid,
			Uid = packet.Uid
		};
		for (int i = 0; i < packet.ChathistoryCount; i++)
		{
			playerGroup.ChatHistory.Add(new Vintagestory.API.Common.ChatLine
			{
				ChatType = (EnumChatType)packet.Chathistory[i].ChatType,
				Message = packet.Chathistory[i].Message
			});
		}
		if (playerGroup.ChatHistory.Count > historyMax)
		{
			playerGroup.ChatHistory.RemoveAt(0);
		}
		return playerGroup;
	}
}
