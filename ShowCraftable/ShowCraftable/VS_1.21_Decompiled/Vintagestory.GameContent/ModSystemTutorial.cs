using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemTutorial : ModSystem
{
	private ICoreAPI api;

	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	private HudTutorial hud;

	private HashSet<string> tutorialModeActiveForPlayers = new HashSet<string>();

	private bool eventsRegistered;

	private ITutorial currentTutorialInst;

	public OrderedDictionary<string, ITutorial> Tutorials = new OrderedDictionary<string, ITutorial>();

	private HashSet<string> eventNames = new HashSet<string> { "onitemcollected", "onitemcrafted", "onitemknapped", "onitemclayformed", "onitemgrabbed" };

	public string CurrentTutorial { get; private set; }

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Network.RegisterChannel("tutorial").RegisterMessageType<ItemStackReceivedPacket>().RegisterMessageType<BlockPlacedPacket>()
			.RegisterMessageType<ActivateTutorialPacket>();
	}

	public void ActivateTutorialMode(string playerUid)
	{
		tutorialModeActiveForPlayers.Add(playerUid);
		if (!eventsRegistered)
		{
			sapi.Event.DidPlaceBlock += Event_DidPlaceBlock;
			api.Event.RegisterEventBusListener(onCollectedItem);
			eventsRegistered = true;
		}
	}

	internal void StopActiveTutorial()
	{
		currentTutorialInst.Save();
		hud.TryClose();
		CurrentTutorial = null;
		currentTutorialInst = null;
		tutorialModeActiveForPlayers.Remove(capi.World.Player.PlayerUID);
	}

	public float GetTutorialProgress(string code)
	{
		ITutorial tutorial = Tutorials[code];
		tutorial.Load();
		return tutorial.Progress;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("tutorial").SetMessageHandler<ItemStackReceivedPacket>(onCollectedItemstack).SetMessageHandler<BlockPlacedPacket>(onBlockPlaced);
		Tutorials["firststeps"] = new FirstStepsTutorial(capi);
		api.ModLoader.GetModSystem<ModSystemSurvivalHandbook>().OnInitCustomPages += ModSystemTutorial_OnInitCustomPages;
		api.Event.LevelFinalize += Event_LevelFinalize_Client;
		api.Event.LeaveWorld += Event_LeaveWorld_Client;
		api.Event.RegisterGameTickListener(onClientTick200ms, 200);
		capi.Input.AddHotkeyListener(onHotkey);
		capi.Input.InWorldAction += Input_InWorldAction;
		api.ChatCommands.Create("tutorial").WithDescription("Interact with the tutorial system").BeginSubCommand("hud")
			.WithDescription("Toggle the tutorial HUD")
			.HandleWith(ToggleHud)
			.EndSubCommand()
			.BeginSubCommand("restart")
			.WithDescription("Restart the currently selected tutorial")
			.HandleWith(OnTutRestart)
			.EndSubCommand()
			.BeginSubCommand("skip")
			.WithDescription("Skip the currently selected tutorial")
			.WithArgs(this.api.ChatCommands.Parsers.OptionalInt("skip_amount", 1))
			.HandleWith(OnTutSkip)
			.EndSubCommand();
	}

	private TextCommandResult OnTutRestart(TextCommandCallingArgs args)
	{
		if (currentTutorialInst == null)
		{
			return TextCommandResult.Success("No current tutorial selected.");
		}
		currentTutorialInst.Restart();
		reloadTutorialPage();
		hud.loadHud(currentTutorialInst.PageCode);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnTutSkip(TextCommandCallingArgs args)
	{
		if (currentTutorialInst == null)
		{
			return TextCommandResult.Success("No current tutorial selected.");
		}
		int num = (int)args.Parsers[0].GetValue();
		if (num <= 0)
		{
			return TextCommandResult.Success();
		}
		currentTutorialInst.Skip(num);
		reloadTutorialPage();
		hud.loadHud(currentTutorialInst.PageCode);
		return TextCommandResult.Success();
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		onStateUpdate((TutorialStepBase step) => step.OnAction(action, on));
	}

	private void onHotkey(string hotkeycode, KeyCombination keyComb)
	{
		if (capi.World.Player != null && tutorialModeActiveForPlayers.Contains(capi.World.Player.PlayerUID))
		{
			onStateUpdate((TutorialStepBase step) => step.OnHotkeyPressed(hotkeycode, keyComb));
		}
	}

	public void StartTutorial(string code, bool restart = false)
	{
		currentTutorialInst = Tutorials[code];
		if (restart)
		{
			currentTutorialInst.Restart();
		}
		else
		{
			currentTutorialInst.Load();
		}
		CurrentTutorial = code;
		hud.TryOpen();
		hud.loadHud(currentTutorialInst.PageCode);
		capi.Network.GetChannel("tutorial").SendPacket(new ActivateTutorialPacket
		{
			Code = code
		});
		tutorialModeActiveForPlayers.Add(capi.World.Player.PlayerUID);
	}

	private TextCommandResult ToggleHud(TextCommandCallingArgs args)
	{
		if (currentTutorialInst == null)
		{
			return TextCommandResult.Success("No current tutorial selected.");
		}
		if (hud.IsOpened())
		{
			hud.TryClose();
		}
		else
		{
			hud.TryOpen();
			hud.loadHud(currentTutorialInst.PageCode);
		}
		return TextCommandResult.Success();
	}

	private void Event_LeaveWorld_Client()
	{
		currentTutorialInst?.Save();
	}

	private void Event_LevelFinalize_Client()
	{
		hud = new HudTutorial(capi);
		capi.Gui.RegisterDialog(hud);
		if (currentTutorialInst != null)
		{
			currentTutorialInst.Load();
			hud.TryOpen();
			tutorialModeActiveForPlayers.Add(capi.World.Player.PlayerUID);
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		sapi.Event.PlayerJoin += Event_PlayerJoin;
		sapi.Network.GetChannel("tutorial").SetMessageHandler<ActivateTutorialPacket>(onActivateTutorial);
	}

	private void onActivateTutorial(IServerPlayer fromPlayer, ActivateTutorialPacket packet)
	{
		ActivateTutorialMode(fromPlayer.PlayerUID);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
	}

	private void ModSystemTutorial_OnInitCustomPages(List<GuiHandbookPage> pages)
	{
		foreach (KeyValuePair<string, ITutorial> tutorial in Tutorials)
		{
			pages.Add(new GuiHandbookTutorialPage(capi, "tutorial-" + tutorial.Key));
		}
	}

	private void onStateUpdate(ActionBoolReturn<TutorialStepBase> stepCall)
	{
		if (currentTutorialInst != null)
		{
			if (currentTutorialInst.OnStateUpdate(stepCall))
			{
				reloadTutorialPage();
				hud.loadHud(currentTutorialInst.PageCode);
			}
			if (currentTutorialInst.Complete)
			{
				StopActiveTutorial();
			}
		}
	}

	private void onCollectedItemstack(ItemStackReceivedPacket packet)
	{
		ItemStack stack = new ItemStack(packet.stackbytes);
		stack.ResolveBlockOrItem(api.World);
		onStateUpdate((TutorialStepBase step) => step.OnItemStackReceived(stack, packet.eventname));
	}

	private void onBlockPlaced(BlockPlacedPacket packet)
	{
		ItemStack stack = ((packet.withStackInHands == null) ? null : new ItemStack(packet.withStackInHands));
		stack?.ResolveBlockOrItem(api.World);
		onStateUpdate((TutorialStepBase step) => step.OnBlockPlaced(packet.pos, api.World.Blocks[packet.blockId], stack));
	}

	private void onClientTick200ms(float dt)
	{
		IPlayerInventoryManager invMan = capi.World.Player.InventoryManager;
		if (invMan != null)
		{
			onStateUpdate((TutorialStepBase step) => step.DoCheckPlayerInventory(invMan));
		}
		BlockSelection blockSel = capi.World.Player.CurrentBlockSelection;
		if (blockSel != null)
		{
			onStateUpdate((TutorialStepBase step) => step.OnBlockLookedAt(blockSel));
		}
	}

	private void reloadTutorialPage()
	{
		(capi.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogHandbook) as GuiDialogHandbook)?.ReloadPage();
	}

	private void Event_DidPlaceBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		if (tutorialModeActiveForPlayers.Contains(byPlayer.PlayerUID))
		{
			sapi.Network.GetChannel("tutorial").SendPacket(new BlockPlacedPacket
			{
				pos = blockSel.Position,
				blockId = api.World.BlockAccessor.GetBlock(blockSel.Position).Id,
				withStackInHands = withItemStack?.ToBytes()
			}, byPlayer);
		}
	}

	private void onCollectedItem(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (!eventNames.Contains(eventName))
		{
			return;
		}
		TreeAttribute treeAttribute = data as TreeAttribute;
		long entityId = treeAttribute.GetLong("byentityid", 0L);
		IPlayer player = (api.World.GetEntityById(entityId) as EntityPlayer)?.Player;
		if (player != null)
		{
			ItemStack itemstack = treeAttribute.GetItemstack("itemstack");
			if (tutorialModeActiveForPlayers.Contains(player.PlayerUID))
			{
				sapi.Network.GetChannel("tutorial").SendPacket(new ItemStackReceivedPacket
				{
					eventname = eventName,
					stackbytes = itemstack.ToBytes()
				}, player as IServerPlayer);
			}
		}
	}

	public RichTextComponentBase[] GetPageText(string pagecode, bool skipOld)
	{
		List<RichTextComponentBase> list = new List<RichTextComponentBase>();
		CairoFont cairoFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.5).WithFontSize(18f);
		ITutorial tutorial = currentTutorialInst;
		if (tutorial == null)
		{
			tutorial = Tutorials[pagecode.Substring("tutorial-".Length)];
		}
		List<TutorialStepBase> tutorialSteps = tutorial.GetTutorialSteps(skipOld);
		tutorialSteps.Reverse();
		foreach (TutorialStepBase item in tutorialSteps)
		{
			cairoFont = cairoFont.Clone();
			cairoFont.Color[3] = (item.Complete ? 0.7 : 1.0);
			cairoFont.WithFontSize(item.Complete ? 15 : 18);
			RichTextComponentBase[] text = item.GetText(cairoFont);
			if (item.Complete)
			{
				list.AddRange(VtmlUtil.Richtextify(capi, "<font color=\"green\"><icon path=\"icons/checkmark.svg\"></icon></font>", cairoFont));
				RichTextComponentBase[] array = text;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] is LinkTextComponent linkTextComponent)
					{
						linkTextComponent.Clickable = false;
					}
				}
			}
			list.AddRange(text);
			list.Add(new RichTextComponent(capi, "\n", cairoFont));
		}
		return list.ToArray();
	}
}
