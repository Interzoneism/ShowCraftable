using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class GuiManager : ClientSystem
{
	public static bool DEBUG_PRINT_INTERACTIONS;

	internal InventoryItemRenderer inventoryItemRenderer;

	private bool ignoreFocusEvents;

	private GuiDialog prevMousedOverDialog;

	private bool didHoverSlotEventTrigger;

	private ItemSlot prevHoverSlot;

	public override string Name => "gdm";

	public IWorldAccessor World => game;

	public GuiManager(ClientMain game)
		: base(game)
	{
		inventoryItemRenderer = new InventoryItemRenderer(game);
		game.eventManager.OnGameWindowFocus.Add(FocusChanged);
		game.eventManager.OnDialogOpened.Add(OnGuiOpened);
		game.eventManager.OnDialogClosed.Add(OnGuiClosed);
		RegisterDefaultDialogs();
		game.eventManager.RegisterRenderer(OnBeforeRenderFrame3D, EnumRenderStage.Before, Name, 0.1);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, Name, 0.1);
		game.Logger.Notification("Initialized GUI Manager");
	}

	public override void OnServerIdentificationReceived()
	{
		game.eventManager?.RegisterRenderer(OnRenderFrameGUI, EnumRenderStage.Ortho, Name, 1.0);
	}

	private void FocusChanged(bool focus)
	{
	}

	public void RegisterDefaultDialogs()
	{
		game.RegisterDialog(new HudEntityNameTags(game.api), new GuiDialogEscapeMenu(game.api), new HudIngameError(game.api), new HudIngameDiscovery(game.api), new HudDialogChat(game.api), new HudElementInteractionHelp(game.api), new HudHotbar(game.api), new HudStatbar(game.api), new GuiDialogInventory(game.api), new GuiDialogCharacter(game.api), new GuiDialogConfirmRemapping(game.api), new GuiDialogMacroEditor(game.api), new HudDebugScreen(game.api), new HudElementCoordinates(game.api), new HudElementBlockAndEntityInfo(game.api), new GuiDialogTickProfiler(game.api), new HudDisconnected(game.api), new HudNotMinecraft(game.api), new GuiDialogTransformEditor(game.api), new GuiDialogSelboxEditor(game.api), new GuiDialogToolMode(game.api), new GuiDialogDead(game.api), new GuiDialogFirstlaunchInfo(game.api), new HudMouseTools(game.api), new HudDropItem(game.api));
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.GuiDialog;
	}

	internal void OnEscapePressed()
	{
		bool flag = true;
		for (int i = 0; i < game.OpenedGuis.Count; i++)
		{
			bool flag2 = game.OpenedGuis[i].OnEscapePressed();
			flag = flag && flag2;
			if (flag2)
			{
				i--;
			}
		}
	}

	internal void OnGuiClosed(GuiDialog dialog)
	{
		game.OpenedGuis.Remove(dialog);
		if (dialog.UnregisterOnClose)
		{
			game.LoadedGuis.Remove(dialog);
		}
		bool flag = game.DialogsOpened > 0;
		if (game.player == null)
		{
			return;
		}
		ClientPlayerInventoryManager inventoryMgr = game.player.inventoryMgr;
		if (inventoryMgr.currentHoveredSlot != null)
		{
			InventoryBase inventory = inventoryMgr.currentHoveredSlot.Inventory;
			if ((inventory != null && !inventory.HasOpened(game.player)) || !flag)
			{
				inventoryMgr.currentHoveredSlot = null;
			}
		}
		if (game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focused) == null)
		{
			GuiDialog dialog2 = game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focusable);
			RequestFocus(dialog2);
		}
		if (!flag)
		{
			game.UpdateFreeMouse();
		}
	}

	internal void OnGuiOpened(GuiDialog dialog)
	{
		if (game.OpenedGuis.Contains(dialog))
		{
			game.OpenedGuis.Remove(dialog);
		}
		int num = game.OpenedGuis.FindIndex((GuiDialog d) => dialog.DrawOrder >= d.DrawOrder);
		if (num >= 0)
		{
			game.OpenedGuis.Insert(num, dialog);
		}
		else
		{
			game.OpenedGuis.Add(dialog);
		}
	}

	internal void RequestFocus(GuiDialog dialog)
	{
		if (!game.LoadedGuis.Contains(dialog))
		{
			game.Logger.Error("The dialog {0} requested focus, but was not added yet. Missing call to api.Gui.RegisterDialog()", dialog.DebugName);
		}
		else
		{
			if (ignoreFocusEvents || !dialog.IsOpened())
			{
				return;
			}
			Move(game.LoadedGuis, dialog, game.LoadedGuis.FindIndex((GuiDialog d) => d.InputOrder == dialog.InputOrder && d.DrawOrder == dialog.DrawOrder));
			Move(game.OpenedGuis, dialog, game.OpenedGuis.FindIndex((GuiDialog d) => d.DrawOrder == dialog.DrawOrder));
			ignoreFocusEvents = true;
			foreach (GuiDialog item in game.LoadedGuis.Where((GuiDialog d) => d != dialog).ToList())
			{
				item.UnFocus();
			}
			dialog.Focus();
			ignoreFocusEvents = false;
		}
	}

	private void Move<T>(List<T> list, T element, int to)
	{
		int num = list.FindIndex((T e) => e.Equals(element));
		if (num == -1)
		{
			return;
		}
		if (num > to)
		{
			for (int num2 = num; num2 > to; num2--)
			{
				list[num2] = list[num2 - 1];
			}
		}
		else if (num < to)
		{
			for (int num3 = num; num3 < to; num3++)
			{
				list[num3] = list[num3 + 1];
			}
		}
		list[to] = element;
	}

	public override void OnBlockTexturesLoaded()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnBlockTexturesLoaded();
		}
	}

	internal override void OnLevelFinalize()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnLevelFinalize();
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnOwnPlayerDataReceived();
		}
	}

	public void OnBeforeRenderFrame3D(float deltaTime)
	{
		foreach (GuiDialog item in Enumerable.Reverse(game.OpenedGuis))
		{
			if (item.ShouldReceiveRenderEvents())
			{
				item.OnBeforeRenderFrame3D(deltaTime);
			}
		}
	}

	public void OnRenderFrameGUI(float deltaTime)
	{
		game.GlPushMatrix();
		string text = null;
		foreach (GuiDialog item in Enumerable.Reverse(game.OpenedGuis))
		{
			if (item.ShouldReceiveRenderEvents())
			{
				item.OnRenderGUI(deltaTime);
				game.Platform.CheckGlError(item.DebugName);
				game.GlTranslate(0.0, 0.0, item.ZSize);
				if (item.MouseOverCursor != null)
				{
					text = item.MouseOverCursor;
				}
				ScreenManager.FrameProfiler.Mark("rendGui", item.DebugName);
			}
		}
		game.Platform.UseMouseCursor((text != null) ? text : "normal");
		game.GlPopMatrix();
		ScreenManager.FrameProfiler.Mark("rendGuiDone");
	}

	public void OnFinalizeFrame(float dt)
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnFinalizeFrame(dt);
			ScreenManager.FrameProfiler.Mark("gdm-finFr-", item.DebugName);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		int keyCode = args.KeyCode;
		List<GuiDialog> list = game.OpenedGuis.ToList();
		foreach (GuiDialog item in list)
		{
			if (item.CaptureAllInputs())
			{
				item.OnKeyDown(args);
				if (args.Handled)
				{
					return;
				}
			}
		}
		if (keyCode == 50 && game.DialogsOpened > 0)
		{
			OnEscapePressed();
			args.Handled = true;
			return;
		}
		foreach (GuiDialog item2 in list)
		{
			if (item2.ShouldReceiveKeyboardEvents())
			{
				item2.OnKeyDown(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnKeyUp(KeyEvent args)
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			if (item.ShouldReceiveKeyboardEvents())
			{
				item.OnKeyUp(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnKeyPress(KeyEvent args)
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			if (item.ShouldReceiveKeyboardEvents())
			{
				item.OnKeyPress(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			if (!item.ShouldReceiveMouseEvents())
			{
				continue;
			}
			item.OnMouseDown(args);
			if (args.Handled)
			{
				if (DEBUG_PRINT_INTERACTIONS)
				{
					game.Logger.Debug("[GuiManager] OnMouseDown handled by {0}", item.GetType().Name);
				}
				RequestFocus(item);
				break;
			}
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			if (!item.ShouldReceiveMouseEvents())
			{
				continue;
			}
			item.OnMouseUp(args);
			if (args.Handled)
			{
				if (DEBUG_PRINT_INTERACTIONS)
				{
					game.Logger.Debug("[GuiManager] OnMouseUp handled by {0}", item.GetType().Name);
				}
				break;
			}
		}
	}

	public override void OnMouseMove(MouseEvent args)
	{
		didHoverSlotEventTrigger = false;
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			if (item.ShouldReceiveMouseEvents())
			{
				item.OnMouseMove(args);
				if (args.Handled)
				{
					OnMouseMoveOver(item);
					return;
				}
			}
		}
		OnMouseMoveOver(null);
	}

	private void OnMouseMoveOver(GuiDialog nowMouseOverDialog)
	{
		if ((nowMouseOverDialog != prevMousedOverDialog || nowMouseOverDialog == null) && !didHoverSlotEventTrigger && prevHoverSlot != null)
		{
			game.api.Input.TriggerOnMouseLeaveSlot(prevHoverSlot);
		}
		prevMousedOverDialog = nowMouseOverDialog;
	}

	public override bool OnMouseEnterSlot(ItemSlot slot)
	{
		prevHoverSlot = slot;
		didHoverSlotEventTrigger = true;
		return false;
	}

	public override bool OnMouseLeaveSlot(ItemSlot itemSlot)
	{
		didHoverSlotEventTrigger = true;
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			if (loadedGui.ShouldReceiveMouseEvents() && loadedGui.OnMouseLeaveSlot(itemSlot))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			if (openedGui.CaptureAllInputs())
			{
				openedGui.OnMouseWheel(args);
				if (args.IsHandled)
				{
					return;
				}
			}
		}
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			if (!loadedGui.IsOpened())
			{
				continue;
			}
			bool flag = false;
			foreach (GuiComposer value in loadedGui.Composers.Values)
			{
				flag |= value.Bounds.PointInside(game.MouseCurrentX, game.MouseCurrentY);
			}
			if (flag && loadedGui.ShouldReceiveMouseEvents())
			{
				loadedGui.OnMouseWheel(args);
				if (args.IsHandled)
				{
					return;
				}
			}
		}
		foreach (GuiDialog loadedGui2 in game.LoadedGuis)
		{
			if (loadedGui2.ShouldReceiveMouseEvents())
			{
				loadedGui2.OnMouseWheel(args);
				if (args.IsHandled)
				{
					break;
				}
			}
		}
	}

	public override bool CaptureAllInputs()
	{
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			if (openedGui.CaptureAllInputs())
			{
				return true;
			}
		}
		return false;
	}

	public override bool CaptureRawMouse()
	{
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			if (openedGui.CaptureRawMouse())
			{
				return true;
			}
		}
		return false;
	}

	public void SendPacketClient(Packet_Client packetClient)
	{
		game.SendPacketClient(packetClient);
	}

	public override void Dispose(ClientMain game)
	{
		inventoryItemRenderer?.Dispose();
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			loadedGui?.Dispose();
		}
	}
}
