using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerControl : ClientSystem
{
	private int forwardKey;

	private int backwardKey;

	private int leftKey;

	private int rightKey;

	private int jumpKey;

	private int sneakKey;

	private int sprintKey;

	private int sittingKey;

	private int ctrlKey;

	private int shiftKey;

	private bool nowFloorSitting;

	private EntityControls prevControls;

	public override string Name => "plco";

	public SystemPlayerControl(ClientMain game)
		: base(game)
	{
		base.game.RegisterGameTickListener(OnGameTick, 20);
		ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate
		{
			LoadKeyCodes();
		});
		LoadKeyCodes();
		prevControls = new EntityControls();
	}

	private void LoadKeyCodes()
	{
		forwardKey = ScreenManager.hotkeyManager.HotKeys["walkforward"].CurrentMapping.KeyCode;
		backwardKey = ScreenManager.hotkeyManager.HotKeys["walkbackward"].CurrentMapping.KeyCode;
		leftKey = ScreenManager.hotkeyManager.HotKeys["walkleft"].CurrentMapping.KeyCode;
		rightKey = ScreenManager.hotkeyManager.HotKeys["walkright"].CurrentMapping.KeyCode;
		sneakKey = ScreenManager.hotkeyManager.HotKeys["sneak"].CurrentMapping.KeyCode;
		sprintKey = ScreenManager.hotkeyManager.HotKeys["sprint"].CurrentMapping.KeyCode;
		jumpKey = ScreenManager.hotkeyManager.HotKeys["jump"].CurrentMapping.KeyCode;
		sittingKey = ScreenManager.hotkeyManager.HotKeys["sitdown"].CurrentMapping.KeyCode;
		ctrlKey = ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping.KeyCode;
		shiftKey = ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping.KeyCode;
	}

	public override void OnKeyDown(KeyEvent args)
	{
		EntityPlayer entityPlayer = game.EntityPlayer;
		if (args.KeyCode == sittingKey && !entityPlayer.Controls.TriesToMove && !entityPlayer.Controls.IsFlying)
		{
			nowFloorSitting = !entityPlayer.Controls.FloorSitting;
		}
		if (args.KeyCode == jumpKey || args.KeyCode == forwardKey || args.KeyCode == backwardKey || args.KeyCode == leftKey || args.KeyCode == rightKey)
		{
			nowFloorSitting = false;
		}
	}

	public void OnGameTick(float dt)
	{
		EntityControls entityControls = ((game.EntityPlayer.MountedOn == null) ? game.EntityPlayer.Controls : game.EntityPlayer.MountedOn.Controls);
		if (entityControls != null)
		{
			game.EntityPlayer.Controls.OnAction = game.api.inputapi.TriggerInWorldAction;
			bool flag = game.MouseGrabbed || (game.api.Settings.Bool["immersiveMouseMode"] && game.OpenedGuis.All((GuiDialog gui) => !gui.PrefersUngrabbedMouse));
			entityControls.Forward = game.KeyboardState[forwardKey];
			entityControls.Backward = game.KeyboardState[backwardKey];
			entityControls.Left = game.KeyboardState[leftKey];
			entityControls.Right = game.KeyboardState[rightKey];
			entityControls.Jump = game.KeyboardState[jumpKey] && flag && (game.EntityPlayer.PrevFrameCanStandUp || game.player.worlddata.NoClip);
			entityControls.Sneak = game.KeyboardState[sneakKey] && flag;
			bool sprint = entityControls.Sprint;
			entityControls.Sprint = (game.KeyboardState[sprintKey] || (sprint && entityControls.TriesToMove && ClientSettings.ToggleSprint)) && flag;
			entityControls.CtrlKey = game.KeyboardState[ctrlKey];
			entityControls.ShiftKey = game.KeyboardState[shiftKey];
			entityControls.DetachedMode = game.player.worlddata.FreeMove || game.EntityPlayer.IsEyesSubmerged();
			entityControls.FlyPlaneLock = game.player.worlddata.FreeMovePlaneLock;
			entityControls.Up = entityControls.DetachedMode && entityControls.Jump;
			entityControls.Down = entityControls.DetachedMode && entityControls.Sneak;
			entityControls.MovespeedMultiplier = game.player.worlddata.MoveSpeedMultiplier;
			entityControls.IsFlying = game.player.worlddata.FreeMove;
			entityControls.NoClip = game.player.worlddata.NoClip;
			entityControls.LeftMouseDown = game.InWorldMouseState.Left;
			entityControls.RightMouseDown = game.InWorldMouseState.Right;
			entityControls.FloorSitting = nowFloorSitting;
			SendServerPackets(prevControls, entityControls);
		}
	}

	private void SendServerPackets(EntityControls before, EntityControls now)
	{
		for (int i = 0; i < before.Flags.Length; i++)
		{
			if (before.Flags[i] != now.Flags[i])
			{
				game.SendPacketClient(new Packet_Client
				{
					Id = 21,
					MoveKeyChange = new Packet_MoveKeyChange
					{
						Down = (now.Flags[i] ? 1 : 0),
						Key = i
					}
				});
				before.Flags[i] = now.Flags[i];
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
