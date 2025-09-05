using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

internal class SystemClientCommands : ClientSystem
{
	private enum EnumPngsExportRequest
	{
		None,
		CreativeInventory,
		All,
		One,
		Hand
	}

	private EnumPngsExportRequest exportRequest;

	private string exportDomain;

	private int size;

	private EnumItemClass exportType;

	private string exportCode;

	public override string Name => "ccom";

	public SystemClientCommands(ClientMain game)
		: base(game)
	{
		game.api.RegisterLinkProtocol("command", onCommandLinkClicked);
		ICoreClientAPI api = game.api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(parsers.OptionalWord("commandname"), parsers.OptionalWord("subcommand"), parsers.OptionalWord("subsubcommand"))
			.WithDescription("Display list of available server commands")
			.HandleWith(handleHelp);
		api.ChatCommands.GetOrCreate("dev").BeginSubCommand("reload").WithRootAlias("reload")
			.WithDescription("Asseted reloading utility. Incase of shape reload will also Re-tesselate. Incase of textures will regenerate the texture atlasses.")
			.WithArgs(parsers.Word("assetcategory"))
			.HandleWith(OnCmdReload)
			.EndSubCommand();
		api.ChatCommands.Create("clients").WithAlias("online").WithDescription("List of connected players")
			.WithArgs(parsers.OptionalWord("ping"))
			.HandleWith(OnCmdListClients);
		api.ChatCommands.Create("freemove").WithDescription("Toggle Freemove").WithArgs(parsers.OptionalBool("freeMove"))
			.HandleWith(OnCmdFreeMove);
		api.ChatCommands.Create("gui").WithDescription("Hide/Show all GUIs").WithArgs(parsers.OptionalBool("show_gui"))
			.HandleWith(OnCmdToggleGUI);
		api.ChatCommands.Create("movespeed").WithDescription("Set Movespeed").WithArgs(parsers.OptionalFloat("speed", 1f))
			.HandleWith(OnCmdMoveSpeed);
		api.ChatCommands.Create("noclip").WithDescription("Toggle noclip").WithArgs(parsers.OptionalBool("noclip"))
			.HandleWith(OnCmdNoClip);
		api.ChatCommands.Create("viewdistance").WithDescription("Set view distance").WithArgs(parsers.OptionalInt("viewdistance"))
			.HandleWith(OnCmdViewDistance);
		api.ChatCommands.Create("lockfly").WithDescription("Locks a movement axis during flying/swimming").WithArgs(parsers.OptionalInt("axis"))
			.HandleWith(OnCmdLockFly);
		api.ChatCommands.Create("resolution").WithDescription("Sets the screen size to given width and height. Can be either [width] [height] or [360p|480p|720p|1080p|2160p]").WithArgs(parsers.OptionalWord("width"), parsers.OptionalWord("height"))
			.HandleWith(OnCmdResolution);
		api.ChatCommands.Create("clientconfig").WithAlias("cf").WithDescription("Set/Gets a client setting")
			.WithArgs(parsers.Word("name"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdSetting);
		api.ChatCommands.Create("clientconfigcreate").WithDescription("Create a new client setting that does not exist").WithArgs(parsers.Word("name"), parsers.Word("datatype"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdSettingCreate);
		api.ChatCommands.Create("cp").WithDescription("Copy something to your clipboard").BeginSubCommand("posi")
			.WithDescription("Copy position as integer")
			.HandleWith(OnCmdCpPosi)
			.EndSubCommand()
			.BeginSubCommand("aposi")
			.WithDescription("Copy position as absolute integer")
			.HandleWith(OnCmdCpAposi)
			.EndSubCommand()
			.BeginSubCommand("apos")
			.WithDescription("Copy position as absolute floating point number")
			.HandleWith(OnCmdCpApos)
			.EndSubCommand()
			.BeginSubCommand("chat")
			.WithDescription("Copy the chat history")
			.HandleWith(OnCmdCpChat)
			.EndSubCommand();
		api.ChatCommands.Create("reconnect").WithDescription("Reconnect to server").HandleWith(OnCmdReconnect);
		api.ChatCommands.Create("recordingmode").WithDescription("Makes the game brighter for recording (Sets gamma level to 1.1 and brightness level to 1.5)").HandleWith(OnCmdRecordingMode);
		api.ChatCommands.Create("blockitempngexport").WithDescription("Export all items and blocks as png images").WithArgs(parsers.OptionalWordRange("exportRequest", "inv", "all"), parsers.OptionalInt("size", 100), parsers.OptionalWord("exportDomain"))
			.HandleWith(OnCmdBlockItemPngExport);
		api.ChatCommands.Create("exponepng").BeginSubCommand("code").WithDescription("Export one items as png image")
			.WithArgs(parsers.WordRange("exportType", "block", "item"), parsers.Word("exportCode"), parsers.OptionalInt("size", 100))
			.HandleWith(OnCmdOnePngExportCode)
			.EndSubCommand()
			.BeginSubCommand("hand")
			.WithDescription("Export icon for currently held item/block")
			.WithArgs(parsers.OptionalInt("size", 100))
			.HandleWith(OnCmdOnePngExportHand)
			.EndSubCommand();
		api.ChatCommands.Create("gencraftjson").WithDescription("Copies a snippet of json from your currently held item usable as a crafting recipe ingredient").HandleWith(OnCmdGenCraftJson);
		api.ChatCommands.Create("zfar").WithDescription("Sets the zfar clipping plane. Useful when up the limit of 1km view distance.").WithArgs(parsers.OptionalFloat("zfar"))
			.HandleWith(OnCmdZfar);
		api.ChatCommands.Create("crash").WithDescription("Crashes the game.").HandleWith(OnCmdCrash);
		api.ChatCommands.Create("timelapse").WithDescription("Start a sequence of timelapse photography, with specified interval (days) and duration (months)").WithArgs(parsers.Float("interval"), parsers.Float("duration"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdTimelapse);
		game.eventManager.RegisterRenderer(OnRenderBlockItemPngs, EnumRenderStage.Ortho, "renderblockitempngs", 0.5);
	}

	private TextCommandResult OnCmdCpPosi(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
			asBlockPos.Sub(game.SpawnPosition.AsBlockPos.X, 0, game.SpawnPosition.AsBlockPos.Z);
			game.Platform.XPlatInterface.SetClipboardText(asBlockPos?.ToString() ?? "");
			return TextCommandResult.Success("Position as integer copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpAposi(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZInt?.ToString() ?? "");
			return TextCommandResult.Success("Absolute Position as integer copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpApos(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZ?.ToString() ?? "");
			return TextCommandResult.Success("Absolute Position copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpChat(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in game.ChatHistoryByPlayerGroup[game.currentGroupid])
		{
			stringBuilder.AppendLine(item);
		}
		game.Platform.XPlatInterface.SetClipboardText(stringBuilder.ToString());
		return TextCommandResult.Success("Current chat history copied to clipboard");
	}

	private TextCommandResult handleHelp(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Dictionary<string, IChatCommand> ordered = IChatCommandApi.GetOrdered(game.api.chatcommandapi.AllSubcommands());
		Caller caller = args.Caller;
		if (caller.CallerPrivileges == null)
		{
			caller.CallerPrivileges = new string[1] { "*" };
		}
		if (args.Parsers[0].IsMissing)
		{
			stringBuilder.AppendLine("Available commands:");
			ChatCommandImpl.WriteCommandsList(stringBuilder, ordered, args.Caller);
			stringBuilder.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command"));
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		string text = (string)args[0];
		if (!args.Parsers[1].IsMissing)
		{
			bool flag = false;
			foreach (KeyValuePair<string, IChatCommand> item in ordered)
			{
				if (item.Key == text)
				{
					ordered = IChatCommandApi.GetOrdered(item.Value.AllSubcommands);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + text + " " + (string)args[1]);
			}
			text = (string)args[1];
			if (!args.Parsers[2].IsMissing)
			{
				flag = false;
				foreach (KeyValuePair<string, IChatCommand> item2 in ordered)
				{
					if (item2.Key == text)
					{
						ordered = IChatCommandApi.GetOrdered(item2.Value.AllSubcommands);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + (string)args[0] + text + " " + (string)args[2]);
				}
				text = (string)args[2];
			}
		}
		foreach (KeyValuePair<string, IChatCommand> item3 in ordered)
		{
			if (item3.Key == text)
			{
				IChatCommand value = item3.Value;
				if (value.IsAvailableTo(args.Caller))
				{
					return TextCommandResult.Success(value.GetFullSyntaxConsole(args.Caller));
				}
				return TextCommandResult.Error("Insufficient privilege to use this command");
			}
		}
		return TextCommandResult.Error(Lang.Get("No such command found") + ": " + text);
	}

	private void onCommandLinkClicked(LinkTextComponent linkComp)
	{
		game.eventManager?.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, linkComp.Href.Substring("command://".Length), EnumChatType.Macro, null);
	}

	private TextCommandResult OnCmdCrash(TextCommandCallingArgs textCommandCallingArgs)
	{
		throw new Exception("Crash on request");
	}

	private TextCommandResult OnCmdRecordingMode(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (ClientSettings.BrightnessLevel == 1f)
		{
			ClientSettings.BrightnessLevel = 1.2f;
			ClientSettings.ExtraGammaLevel = 1.3f;
			game.ShowChatMessage("Recording bright mode now on");
		}
		else
		{
			ClientSettings.BrightnessLevel = 1f;
			ClientSettings.ExtraGammaLevel = 1f;
			game.ShowChatMessage("Recording bright mode now off");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdTimelapse(TextCommandCallingArgs args)
	{
		float timelapse = (float)args[0];
		float num = (float)args[1];
		game.timelapse = timelapse;
		game.timelapseEnd = num * (float)game.Calendar.DaysPerMonth;
		game.ShouldRender2DOverlays = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGenCraftJson(TextCommandCallingArgs args)
	{
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null)
		{
			return TextCommandResult.Success("Require something held in your hands");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder2);
		handler.AppendLiteral("type: \"");
		handler.AppendFormatted(activeHotbarSlot.Itemstack.Class);
		handler.AppendLiteral("\", code: \"");
		handler.AppendFormatted(activeHotbarSlot.Itemstack.Collectible.Code);
		handler.AppendLiteral("\"");
		stringBuilder3.Append(ref handler);
		TreeAttribute treeAttribute = activeHotbarSlot.Itemstack.Attributes.Clone() as TreeAttribute;
		for (int i = 0; i < GlobalConstants.IgnoredStackAttributes.Length; i++)
		{
			treeAttribute.RemoveAttribute(GlobalConstants.IgnoredStackAttributes[i]);
		}
		string text = treeAttribute.ToJsonToken();
		if (text.Length > 0 && treeAttribute.Count > 0)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral(", attributes: ");
			handler.AppendFormatted(text);
			stringBuilder4.Append(ref handler);
		}
		stringBuilder.Append(" }");
		game.Platform.XPlatInterface.SetClipboardText(stringBuilder.ToString());
		return TextCommandResult.Success("Ok, copied to your clipboard");
	}

	private void OnRenderBlockItemPngs(float dt)
	{
		if (exportRequest == EnumPngsExportRequest.None)
		{
			return;
		}
		bool flag = exportRequest == EnumPngsExportRequest.All;
		FrameBufferRef frameBufferRef = game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", size, size)
		{
			Attachments = new FramebufferAttrsAttachment[2]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = size,
						Height = size,
						PixelFormat = EnumTexturePixelFormat.Rgba,
						PixelInternalFormat = EnumTextureInternalFormat.Rgba8
					}
				},
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.DepthAttachment,
					Texture = new RawTexture
					{
						Width = size,
						Height = size,
						PixelFormat = EnumTexturePixelFormat.DepthComponent,
						PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
					}
				}
			}
		});
		game.Platform.LoadFrameBuffer(frameBufferRef);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.OrthoMode(size, size);
		float[] clearColor = new float[4];
		GamePaths.EnsurePathExists("icons/block");
		GamePaths.EnsurePathExists("icons/item");
		if (exportRequest == EnumPngsExportRequest.One || exportRequest == EnumPngsExportRequest.Hand)
		{
			game.Platform.ClearFrameBuffer(frameBufferRef, clearColor);
			ItemStack itemStack;
			if (exportRequest == EnumPngsExportRequest.One)
			{
				if (exportType == EnumItemClass.Item)
				{
					Item item = game.GetItem(new AssetLocation(exportCode));
					if (item == null)
					{
						game.ShowChatMessage("Not an item " + exportCode);
						exportRequest = EnumPngsExportRequest.None;
						return;
					}
					itemStack = new ItemStack(item);
				}
				else
				{
					Block block = game.GetBlock(new AssetLocation(exportCode));
					if (block == null)
					{
						game.ShowChatMessage("Not a block " + exportCode);
						exportRequest = EnumPngsExportRequest.None;
						return;
					}
					itemStack = new ItemStack(block);
				}
			}
			else
			{
				itemStack = game.player.inventoryMgr.ActiveHotbarSlot.Itemstack;
				if (itemStack == null)
				{
					game.ShowChatMessage("Nothing in hands");
					exportRequest = EnumPngsExportRequest.None;
					return;
				}
			}
			game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(itemStack), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
			game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true).Save("icons/" + exportType.Name() + "/" + itemStack.Collectible.Code.Path + ".png");
		}
		else
		{
			for (int i = 0; i < game.Blocks.Count; i++)
			{
				game.Platform.ClearFrameBuffer(frameBufferRef, clearColor);
				Block block2 = game.Blocks[i];
				if (!(block2?.Code == null) && (flag || (block2.CreativeInventoryTabs != null && block2.CreativeInventoryTabs.Length != 0)) && (exportDomain == null || !(block2.Code.Domain != exportDomain)))
				{
					game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(block2)), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
					game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true).Save("icons/block/" + block2.Code.Path + ".png");
				}
			}
			for (int j = 0; j < game.Items.Count; j++)
			{
				game.Platform.ClearFrameBuffer(frameBufferRef, clearColor);
				Item item2 = game.Items[j];
				if (!(item2?.Code == null) && (flag || (item2.CreativeInventoryTabs != null && item2.CreativeInventoryTabs.Length != 0)) && (exportDomain == null || !(item2.Code.Domain != exportDomain)))
				{
					game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(item2)), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
					BitmapRef bitmapRef = game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true);
					string text = item2.Code.Path;
					if (text.Contains("/"))
					{
						text = text.Replace("/", "-");
					}
					bitmapRef.Save("icons/item/" + text + ".png");
				}
			}
		}
		exportRequest = EnumPngsExportRequest.None;
		game.OrthoMode(game.Width, game.Height);
		game.Platform.UnloadFrameBuffer(frameBufferRef);
		game.Platform.DisposeFrameBuffer(frameBufferRef);
		game.ShowChatMessage("Ok, exported to " + Path.GetFullPath("icons/"));
	}

	private void cCopy(int groupId, CmdArgs args)
	{
		switch (args.PopWord())
		{
		case "posi":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
				asBlockPos.Sub(game.SpawnPosition.AsBlockPos.X, 0, game.SpawnPosition.AsBlockPos.Z);
				game.Platform.XPlatInterface.SetClipboardText(asBlockPos?.ToString() ?? "");
				game.ShowChatMessage("Position as integer copied to clipboard");
			}
			break;
		case "apos":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZ?.ToString() ?? "");
				game.ShowChatMessage("Absolute Position copied to clipboard");
			}
			break;
		case "aposi":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZInt?.ToString() ?? "");
				game.ShowChatMessage("Absolute Position as integer copied to clipboard");
			}
			break;
		case "chat":
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string item in game.ChatHistoryByPlayerGroup[game.currentGroupid])
			{
				stringBuilder.AppendLine(item);
			}
			game.Platform.XPlatInterface.SetClipboardText(stringBuilder.ToString());
			game.ShowChatMessage("Current chat history copied to clipboard");
			break;
		}
		}
	}

	private TextCommandResult OnCmdSetting(TextCommandCallingArgs targs)
	{
		CmdArgs rawArgs = targs.RawArgs;
		string text = targs[0] as string;
		if (text == "sedi")
		{
			text = "showentitydebuginfo";
		}
		if (rawArgs.Length == 0)
		{
			if (!ClientSettings.Inst.HasSetting(text))
			{
				return TextCommandResult.Success("No such setting '" + text + "' (you can create setttings using .clientconfigcreate)");
			}
			return TextCommandResult.Success($"{text} is set to {ClientSettings.Inst.GetSetting(text)}");
		}
		Type settingType = ClientSettings.Inst.GetSettingType(text);
		if (settingType == null)
		{
			return TextCommandResult.Success("No such setting '" + text + "'");
		}
		if (settingType == typeof(string))
		{
			string text2 = rawArgs.PopWord();
			ClientSettings.Inst.String[text] = text2;
			game.ShowChatMessage(text + " now set to " + text2);
		}
		if (settingType == typeof(int))
		{
			int? value = rawArgs.PopInt(0);
			if (value.HasValue)
			{
				ClientSettings.Inst.Int[text] = value.Value;
				game.ShowChatMessage($"{text} now set to {value}");
			}
			else
			{
				game.ShowChatMessage("Supplied value is not an integer");
			}
		}
		if (settingType == typeof(float))
		{
			float? value2 = rawArgs.PopFloat(0f);
			if (value2.HasValue)
			{
				ClientSettings.Inst.Float[text] = value2.Value;
				game.ShowChatMessage($"{text} now set to {value2}");
			}
			else
			{
				game.ShowChatMessage("Supplied value is not an integer");
			}
		}
		if (settingType == typeof(bool))
		{
			bool flag = false;
			flag = ((!(rawArgs.PeekWord() == "toggle")) ? (rawArgs.PopBool(false) == true) : (!ClientSettings.Inst.Bool[text]));
			ClientSettings.Inst.Bool[text] = flag;
			game.ShowChatMessage($"{text} now set to {flag}");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSettingCreate(TextCommandCallingArgs targs)
	{
		CmdArgs rawArgs = targs.RawArgs;
		string text = targs[0] as string;
		if (ClientSettings.Inst.HasSetting(text))
		{
			return TextCommandResult.Success("Setting '" + text + "' already exists");
		}
		string text2 = targs[1] as string;
		switch (text2)
		{
		case "string":
		{
			string text3 = rawArgs.PopAll();
			ClientSettings.Inst.String[text] = text3;
			return TextCommandResult.Success(text + " now set to " + text3);
		}
		case "int":
		{
			int? value2 = rawArgs.PopInt(0);
			if (value2.HasValue)
			{
				ClientSettings.Inst.Int[text] = value2.Value;
				return TextCommandResult.Success($"{text} now set to {value2}");
			}
			return TextCommandResult.Success($"Supplied value is not an integer");
		}
		case "float":
		{
			float? value = rawArgs.PopFloat(0f);
			if (value.HasValue)
			{
				ClientSettings.Inst.Float[text] = value.Value;
				return TextCommandResult.Success($"{text} now set to {value}");
			}
			return TextCommandResult.Success($"Supplied value is not an integer");
		}
		case "bool":
		{
			bool valueOrDefault = rawArgs.PopBool(false) == true;
			ClientSettings.Inst.Bool[text] = valueOrDefault;
			return TextCommandResult.Success($"{text} now set to {valueOrDefault}");
		}
		default:
			return TextCommandResult.Success("Unknown datatype: " + text2 + ". Must be string, int, float or bool");
		}
	}

	private TextCommandResult OnCmdLockFly(TextCommandCallingArgs args)
	{
		EnumFreeMovAxisLock freeMovePlaneLock = EnumFreeMovAxisLock.None;
		if (!args.Parsers[0].IsMissing)
		{
			int num = (int)args[0];
			if (num <= 3)
			{
				freeMovePlaneLock = (EnumFreeMovAxisLock)num;
			}
		}
		game.player.worlddata.RequestMode(game, freeMovePlaneLock);
		return TextCommandResult.Success("Lock fly axis " + freeMovePlaneLock);
	}

	private TextCommandResult OnCmdResolution(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current resolution: " + game.Platform.WindowSize.Width + "x" + game.Platform.WindowSize.Height);
		}
		bool flag = true;
		int result = 0;
		int result2 = 0;
		switch (args[0] as string)
		{
		case "360p":
			result = 640;
			result2 = 360;
			break;
		case "480p":
			result = 854;
			result2 = 480;
			break;
		case "720p":
			result = 1280;
			result2 = 720;
			break;
		case "1080p":
			result = 1920;
			result2 = 1080;
			break;
		case "1440p":
			result = 2560;
			result2 = 1440;
			break;
		case "2160p":
			result = 3840;
			result2 = 2160;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag && args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success("Width or Height missing");
		}
		if (!flag)
		{
			int.TryParse(args[0] as string, out result);
			int.TryParse(args[1] as string, out result2);
		}
		if (result <= 0 || result2 <= 0)
		{
			return TextCommandResult.Success("Width or Height not a number or 0 or below 0");
		}
		game.Platform.SetWindowSize(result, result2);
		return TextCommandResult.Success($"Resolution {result}x{result2} set.");
	}

	private TextCommandResult OnCmdZfar(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current Zfar: " + game.MainCamera.ZFar);
		}
		try
		{
			game.MainCamera.ZFar = (float)args[0];
			game.Reset3DProjection();
			return TextCommandResult.Success("Zfar is now: " + game.MainCamera.ZFar);
		}
		catch (Exception)
		{
			return TextCommandResult.Success("Failed parsing param");
		}
	}

	private TextCommandResult OnCmdReload(TextCommandCallingArgs args)
	{
		AssetCategory.categories.TryGetValue(args[0] as string, out var value);
		if (value == null)
		{
			return TextCommandResult.Success("No such asset category found");
		}
		int num = game.Platform.AssetManager.Reload(value);
		if (value == AssetCategory.shaders)
		{
			bool flag = ShaderRegistry.ReloadShaders();
			bool flag2 = game.eventManager != null && game.eventManager.TriggerReloadShaders();
			flag = flag && flag2;
			return TextCommandResult.Success("Shaders reloaded" + (flag ? "" : ". errors occured, please check client log"));
		}
		if (value == AssetCategory.shapes)
		{
			game.eventManager?.TriggerReloadShapes();
			return TextCommandResult.Success(num + " assets reloaded and shapes re-tesselated");
		}
		if (value == AssetCategory.textures)
		{
			game.ReloadTextures();
			return TextCommandResult.Success(num + " assets reloaded and atlasses re-generated");
		}
		if (value == AssetCategory.sounds)
		{
			ScreenManager.LoadSoundsInitial();
		}
		if (value == AssetCategory.lang)
		{
			Lang.Load(game.Logger, game.AssetManager, ClientSettings.Language);
			return TextCommandResult.Success("language files reloaded");
		}
		return TextCommandResult.Success(num + " assets reloaded");
	}

	private TextCommandResult OnCmdViewDistance(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current view distance: " + ClientSettings.ViewDistance);
		}
		ClientSettings.ViewDistance = (int)args[0];
		return TextCommandResult.Success("View distance set");
	}

	private TextCommandResult OnCmdListClients(TextCommandCallingArgs args)
	{
		bool flag = args[0] as string == "ping";
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (KeyValuePair<string, ClientPlayer> item in game.PlayersByUid)
		{
			string playerName = item.Value.PlayerName;
			if (playerName != null)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				if (flag)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
					handler.AppendFormatted(playerName);
					handler.AppendLiteral(" (");
					handler.AppendFormatted((int)(item.Value.Ping * 1000f));
					handler.AppendLiteral("ms)");
					stringBuilder3.Append(ref handler);
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
					handler.AppendFormatted(playerName);
					stringBuilder4.Append(ref handler);
				}
				num++;
			}
		}
		return TextCommandResult.Success($"{num} Players: {stringBuilder}");
	}

	private TextCommandResult OnCmdReconnect(TextCommandCallingArgs textCommandCallingArgs)
	{
		game.exitReason = "reconnect command triggered";
		game.DoReconnect();
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdFreeMove(TextCommandCallingArgs args)
	{
		if (game.AllowFreemove)
		{
			game.player.worlddata.RequestModeFreeMove(game, (bool)args[0]);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success(Lang.Get("Flymode not allowed"));
	}

	private TextCommandResult OnCmdToggleGUI(TextCommandCallingArgs args)
	{
		game.ShouldRender2DOverlays = (bool)args[0];
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMoveSpeed(TextCommandCallingArgs args)
	{
		if (!game.AllowFreemove)
		{
			return TextCommandResult.Success(Lang.Get("Flymode not allowed"));
		}
		float num = (float)args[0];
		if (num > 500f)
		{
			return TextCommandResult.Success("Entered movespeed to high! max. 500x");
		}
		game.player.worlddata.SetMode(game, num);
		return TextCommandResult.Success("Movespeed: " + num + "x");
	}

	private TextCommandResult OnCmdNoClip(TextCommandCallingArgs args)
	{
		game.player.worlddata.RequestModeNoClip(game, (bool)args[0]);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdBlockItemPngExport(TextCommandCallingArgs args)
	{
		exportRequest = ((args[0] as string == "inv") ? EnumPngsExportRequest.CreativeInventory : EnumPngsExportRequest.All);
		size = (int)args[1];
		exportDomain = (args.Parsers[2].IsMissing ? null : (args[2] as string));
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdOnePngExportHand(TextCommandCallingArgs args)
	{
		exportRequest = EnumPngsExportRequest.Hand;
		size = (int)args[0];
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdOnePngExportCode(TextCommandCallingArgs args)
	{
		exportRequest = EnumPngsExportRequest.One;
		exportType = ((!(args[0] as string == "block")) ? EnumItemClass.Item : EnumItemClass.Block);
		exportCode = args[1] as string;
		size = (int)args[2];
		return TextCommandResult.Success();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
