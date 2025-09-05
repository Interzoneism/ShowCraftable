using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Cairo;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiScreenPublicServers : GuiScreen
{
	private ElementBounds tableBounds;

	private ElementBounds clippingBounds;

	private bool isLoading;

	private long ellapsedMs;

	private string searchText;

	private List<ServerListEntry> cells = new List<ServerListEntry>();

	private ResponsePacket packet;

	private int serversTotal;

	private int playersTotal;

	public GuiScreenPublicServers(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		ShowMainMenu = true;
		InitGui();
		screenManager.GamePlatform.WindowResized += delegate
		{
			invalidate();
		};
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			invalidate();
		});
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
			if (packet != null)
			{
				popCells();
			}
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-browserpublicservers");
		}
	}

	private void InitGui()
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds elementBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		_ = Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale;
		string text = Lang.Get("Password protected");
		TextExtents textExtents = CairoFont.WhiteSmallText().GetTextExtents(text);
		double num = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
		string text2 = Lang.Get("Open for all");
		textExtents = CairoFont.WhiteSmallText().GetTextExtents(text2);
		double num2 = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
		string text3 = Lang.Get("Modded");
		textExtents = CairoFont.WhiteSmallText().GetTextExtents(text3);
		double num3 = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds3;
		ElementComposer = dialogBase("mainmenu-browserpublicservers").AddDynamicText(Lang.Get("multiplayer-loadingpublicservers"), CairoFont.WhiteSmallishText(), elementBounds2, "titleText").AddSwitch(onToggleOpen4All, ElementBounds.Fixed(0, 45), "4allSwitch", 20.0, 3.0).AddStaticTextAutoBoxSize(text2, CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed(25, 45))
			.AddSwitch(onTogglePwdProtected, ElementBounds.Fixed((int)(50.0 + num2), 45), "pwdSwitch", 20.0, 3.0)
			.AddStaticTextAutoBoxSize(text, CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(75.0 + num2), 45))
			.AddSwitch(onToggleWhitelisted, ElementBounds.Fixed((int)(100.0 + num + num2), 45), "whitelistSwitch", 20.0, 3.0)
			.AddStaticTextAutoBoxSize(Lang.Get("Whitelisted"), CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(125.0 + num + num2), 45))
			.AddSwitch(onToggleModded, ElementBounds.Fixed((int)(170.0 + num + num2 + num3), 45), "moddedSwitch", 20.0, 3.0)
			.AddStaticTextAutoBoxSize(Lang.Get("Modded"), CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(195.0 + num + num2 + num3), 45))
			.AddTextInput(ElementStdBounds.Rowed(1f, 0.0).WithFixedSize(300.0, 30.0), OnSearch, null, "search")
			.AddInset(elementBounds3 = elementBounds2.BelowCopy(0.0, 70.0).WithFixedSize(Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale, (float)Math.Max(300, height) / ClientSettings.GUIScale - 270f))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(tableBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCell, new List<ServerListEntry>(), "serverstable")
			.EndClip()
			.AddButton(Lang.Get("general-back"), OnBack, elementBounds.FixedUnder(elementBounds3, 10.0))
			.AddRichtext("", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0.0, 0.0, 500.0, 30.0).FixedUnder(elementBounds3, 20.0).WithAlignment(EnumDialogArea.RightFixed), "summaryText")
			.EndChildElements()
			.Compose();
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
		ElementComposer.GetTextInput("search").SetPlaceHolderText(Lang.Get("Search..."));
		ElementComposer.GetTextInput("search").SetValue(searchText);
		ElementComposer.GetSwitch("4allSwitch").SetValue(ClientSettings.ShowOpenForAllServers);
		ElementComposer.GetSwitch("pwdSwitch").SetValue(ClientSettings.ShowPasswordProtectedServers);
		ElementComposer.GetSwitch("whitelistSwitch").SetValue(ClientSettings.ShowWhitelistedServers);
		ElementComposer.GetSwitch("moddedSwitch").SetValue(ClientSettings.ShowModdedServers);
	}

	private IGuiElementCell createCell(ServerListEntry cell, ElementBounds bounds)
	{
		GuiElementMainMenuCell result = new GuiElementMainMenuCell(ScreenManager.api, cell, bounds)
		{
			MainTextWidthSub = GuiElement.scaled(40.0),
			ShowModifyIcons = false,
			OnMouseDownOnCellLeft = OnClickCellLeft,
			FixedHeight = 50.0
		};
		cell.LeftOffY = -2f;
		return result;
	}

	private void OnSearch(string text)
	{
		if (searchText != text)
		{
			searchText = text;
			updateFilter();
		}
	}

	private void updateFilter()
	{
		ElementComposer.GetCellList<ServerListEntry>("serverstable").FilterCells(delegate(IGuiElementCell c)
		{
			ServerListEntry serverListEntry = (c as GuiElementMainMenuCell).cellEntry as ServerListEntry;
			if (!ClientSettings.ShowPasswordProtectedServers && serverListEntry.hasPassword)
			{
				return false;
			}
			if (!ClientSettings.ShowWhitelistedServers && serverListEntry.whitelisted)
			{
				return false;
			}
			if (!ClientSettings.ShowModdedServers)
			{
				ModPacket[] mods = serverListEntry.mods;
				if (mods != null && mods.Length != 0)
				{
					return false;
				}
			}
			if (!ClientSettings.ShowOpenForAllServers && !serverListEntry.hasPassword && !serverListEntry.whitelisted)
			{
				return false;
			}
			return searchText == null || searchText.Length == 0 || (serverListEntry.serverName?.CaseInsensitiveContains(searchText) ?? false);
		});
		updateStatsAndBounds();
		ElementBounds bounds = ElementComposer.GetCellList<ServerListEntry>("serverstable").Bounds;
		bounds.fixedY = 0.0;
		bounds.CalcWorldBounds();
	}

	private void onToggleOpen4All(bool on)
	{
		ClientSettings.ShowOpenForAllServers = on;
		updateFilter();
	}

	private void onToggleWhitelisted(bool on)
	{
		ClientSettings.ShowWhitelistedServers = on;
		updateFilter();
	}

	private void onToggleModded(bool on)
	{
		ClientSettings.ShowModdedServers = on;
		updateFilter();
	}

	private void onTogglePwdProtected(bool on)
	{
		ClientSettings.ShowPasswordProtectedServers = on;
		updateFilter();
	}

	private bool OnBack()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	public override void OnScreenLoaded()
	{
		LoadServerEntries();
		InitGui();
	}

	private void LoadServerEntries()
	{
		isLoading = true;
		getServersAsync(ClientSettings.MasterserverUrl + "list", delegate(ResponsePacket packet)
		{
			isLoading = false;
			this.packet = packet;
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				popCells();
			});
		});
	}

	private void popCells()
	{
		cells.Clear();
		if (packet?.status != "ok")
		{
			ServerListEntry item = new ServerListEntry
			{
				Title = "Could not connect to master server",
				TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor)
			};
			cells.Add(item);
		}
		else
		{
			IOrderedEnumerable<ServerListEntry> orderedEnumerable = packet.data.OrderByDescending((ServerListEntry elem) => elem.players + ((elem.players == 0 && elem.serverName == "Vintage Story Server") ? (-1) : 0));
			serversTotal = 0;
			playersTotal = 0;
			foreach (ServerListEntry item2 in orderedEnumerable)
			{
				serversTotal++;
				playersTotal += item2.players;
				List<string> list = new List<string>();
				list.Add(Lang.Get("{0}/{1} players online", item2.players, item2.maxPlayers));
				if (item2.hasPassword)
				{
					list.Add(Lang.Get("password protected"));
				}
				if (item2.mods.Length != 0)
				{
					list.Add(Lang.Get("modded"));
				}
				if (item2.whitelisted)
				{
					list.Add(Lang.Get("whitelisted"));
				}
				item2.Title = item2.serverName;
				item2.RightTopText = Lang.Get("v{0}", item2.gameVersion);
				item2.DetailText = string.Join(", ", list);
			}
			cells = orderedEnumerable.ToList();
			GuiElementRichtext richtext = ElementComposer.GetRichtext("summaryText");
			richtext.Bounds.fixedWidth = 500.0;
			richtext.SetNewTextWithoutRecompose(Lang.Get("multiplayer-publicservers-stats", playersTotal, serversTotal), CairoFont.WhiteSmallText());
			richtext.BeforeCalcBounds();
			richtext.Bounds.fixedWidth = richtext.MaxLineWidth / (double)RuntimeEnv.GUIScale + 10.0;
			richtext.RecomposeText();
		}
		ElementComposer.GetCellList<ServerListEntry>("serverstable").ReloadCells(cells);
		updateFilter();
		ElementComposer.GetDynamicText("titleText").SetNewText(Lang.Get("multiplayer-browsepublicservers"));
	}

	private void updateStatsAndBounds()
	{
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<ServerListEntry>("serverstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnClickCellLeft(int index)
	{
		if (cells[index].serverIp != null)
		{
			ScreenManager.LoadScreen(new GuiScreenPublicServerView(cells[index], ScreenManager, this));
		}
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		base.RenderToDefaultFramebuffer(dt);
		if (isLoading && ScreenManager.GamePlatform.EllapsedMs - ellapsedMs > 1000)
		{
			int num = (int)(ScreenManager.GamePlatform.EllapsedMs / 1000 % 2);
			string[] array = new string[2]
			{
				Lang.Get("multiplayer-loadingpublicservers"),
				Lang.Get("multiplayer-loadingpublicservers2")
			};
			ElementComposer.GetDynamicText("titleText").SetNewText(array[num]);
			ellapsedMs = ScreenManager.GamePlatform.EllapsedMs;
		}
	}

	private async void getServersAsync(string url, Action<ResponsePacket> onComplete)
	{
		ResponsePacket packet = null;
		try
		{
			HttpResponseMessage obj = await VSWebClient.Inst.GetAsync(url);
			obj.EnsureSuccessStatusCode();
			string text = await obj.Content.ReadAsStringAsync();
			packet = JsonConvert.DeserializeObject<ResponsePacket>(text);
			ScreenManager.GamePlatform.Logger.Notification("Master server list retrieved. Status {0}. Response length: {1}", packet.status, text?.Length ?? (-1));
		}
		catch (Exception e)
		{
			ScreenManager.GamePlatform.Logger.Error("Failed retrieving master server list at url {0}.", url);
			ScreenManager.GamePlatform.Logger.Error(e);
		}
		onComplete(packet);
	}
}
