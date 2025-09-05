using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemCommandHandbook : ModSystem
{
	private ICoreClientAPI capi;

	private GuiDialogHandbook dialog;

	private ICoreServerAPI sapi;

	private ServerCommandsSyntax serverCommandsSyntaxClient;

	public event InitCustomPagesDelegate OnInitCustomPages;

	internal void TriggerOnInitCustomPages(List<GuiHandbookPage> pages)
	{
		this.OnInitCustomPages?.Invoke(pages);
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("commandhandbook").RegisterMessageType<ServerCommandsSyntax>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.PlayerNowPlaying += Event_PlayerNowPlaying;
		api.ChatCommands.Create("chbr").RequiresPlayer().RequiresPrivilege(Privilege.chat)
			.WithDescription("Reload command handbook texts")
			.HandleWith(onCommandHandbookReload);
	}

	private void Event_PlayerNowPlaying(IServerPlayer byPlayer)
	{
		sendSyntaxPacket(byPlayer);
	}

	private void sendSyntaxPacket(IServerPlayer byPlayer)
	{
		ServerCommandsSyntax message = genCmdSyntaxPacket(new Caller
		{
			Player = byPlayer,
			Type = EnumCallerType.Player
		});
		sapi.Network.GetChannel("commandhandbook").SendPacket(message, byPlayer);
	}

	private ServerCommandsSyntax genCmdSyntaxPacket(Caller caller)
	{
		List<ChatCommandSyntax> list = new List<ChatCommandSyntax>();
		foreach (KeyValuePair<string, IChatCommand> item in IChatCommandApi.GetOrdered(sapi.ChatCommands))
		{
			IChatCommand value = item.Value;
			list.Add(new ChatCommandSyntax
			{
				AdditionalInformation = value.AdditionalInformation,
				CallSyntax = value.CallSyntax,
				CallSyntaxUnformatted = value.CallSyntaxUnformatted,
				Description = value.Description,
				Examples = value.Examples,
				FullName = value.FullName,
				Name = item.Key,
				FullnameAlias = value.GetFullName(item.Key, isRootAlias: true),
				FullSyntax = value.GetFullSyntaxHandbook(caller, string.Empty, value.RootAliases?.Contains(item.Key) ?? false),
				Aliases = value.Aliases,
				RootAliases = value.RootAliases
			});
		}
		return new ServerCommandsSyntax
		{
			Commands = list.ToArray()
		};
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("commandhandbook").SetMessageHandler<ServerCommandsSyntax>(onServerCommandsSyntax);
		api.RegisterLinkProtocol("commandhandbook", onHandBookLinkClicked);
		api.ChatCommands.Create("chb").WithDescription("Opens the command hand book").RequiresPrivilege(Privilege.chat)
			.HandleWith(onCommandHandbook)
			.BeginSubCommand("expcmds")
			.WithDescription("Export all commands to a html format for the wiki.")
			.HandleWith(onCmd)
			.EndSubCommand();
	}

	private TextCommandResult onCmd(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (GuiHandbookPage allHandbookPage in dialog.allHandbookPages)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<h3>");
			handler.AppendFormatted(allHandbookPage.PageCode);
			handler.AppendLiteral("</h3>");
			stringBuilder2.Append(ref handler);
			stringBuilder.Append(((GuiHandbookCommandPage)allHandbookPage).TextCacheAll.Replace("\n", "\n</br>").Replace("<strong>", "<b>").Replace("</strong>", "</b>"));
		}
		File.WriteAllText(Path.Combine(GamePaths.ModConfig, "cmds.html"), stringBuilder.ToString());
		return TextCommandResult.Success("exported all cmds");
	}

	private void onHandBookLinkClicked(LinkTextComponent comp)
	{
		string text = comp.Href.Substring("commandhandbook://".Length);
		text = text.Replace("\\", "");
		if (text.StartsWithOrdinal("tab-"))
		{
			if (!dialog.IsOpened())
			{
				dialog.TryOpen();
			}
			dialog.selectTab(text.Substring(4));
			return;
		}
		if (!dialog.IsOpened())
		{
			dialog.TryOpen();
		}
		if (text.Length > 0)
		{
			dialog.OpenDetailPageFor(text);
		}
	}

	private TextCommandResult onCommandHandbookReload(TextCommandCallingArgs args)
	{
		sendSyntaxPacket(args.Caller.Player as IServerPlayer);
		return TextCommandResult.Success("ok, reloaded");
	}

	private void onServerCommandsSyntax(ServerCommandsSyntax packet)
	{
		serverCommandsSyntaxClient = packet;
		dialog = new GuiDialogCommandHandbook(capi, onCreatePagesAsync, onComposePage);
		capi.Logger.VerboseDebug("Done initialising handbook");
	}

	private TextCommandResult onCommandHandbook(TextCommandCallingArgs args)
	{
		if (dialog.IsOpened())
		{
			dialog.TryClose();
		}
		else
		{
			dialog.TryOpen();
		}
		return TextCommandResult.Success();
	}

	private List<GuiHandbookPage> onCreatePagesAsync()
	{
		List<GuiHandbookPage> list = new List<GuiHandbookPage>();
		foreach (KeyValuePair<string, IChatCommand> item in IChatCommandApi.GetOrdered(capi.ChatCommands))
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			IChatCommand value = item.Value;
			list.Add(new GuiHandbookCommandPage(value, value.CommandPrefix + item.Key, "client", value.RootAliases?.Contains(item.Key) ?? false));
		}
		ChatCommandSyntax[] commands = serverCommandsSyntaxClient.Commands;
		foreach (ChatCommandSyntax chatCommandSyntax in commands)
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			list.Add(new GuiHandbookCommandPage(chatCommandSyntax, chatCommandSyntax.FullnameAlias, "server"));
		}
		return list;
	}

	private void onComposePage(GuiHandbookPage page, GuiComposer detailViewGui, ElementBounds textBounds, ActionConsumable<string> openDetailPageFor)
	{
		page.ComposePage(detailViewGui, textBounds, null, openDetailPageFor);
	}

	public override void Dispose()
	{
		base.Dispose();
		dialog?.Dispose();
	}
}
