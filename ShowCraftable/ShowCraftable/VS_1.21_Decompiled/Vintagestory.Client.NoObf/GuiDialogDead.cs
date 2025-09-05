using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class GuiDialogDead : GuiDialog
{
	private ClientMain game;

	private bool respawning;

	private int livesLeft = -1;

	private float secondsDead;

	private double ingameMinutesRevivableLeft;

	private int prevIngMinLeftInt;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogDead(ICoreClientAPI capi)
		: base(capi)
	{
		game = capi.World as ClientMain;
		game.RegisterGameTickListener(OnGameTick, 250);
	}

	private void OnGameTick(float dt)
	{
		if (!game.EntityPlayer.Alive)
		{
			secondsDead += dt;
		}
		else
		{
			secondsDead = 0f;
		}
		if (secondsDead >= 2.5f && !game.EntityPlayer.Alive && !IsOpened())
		{
			int num = game.Config.GetString("playerlives", "-1").ToInt(-1);
			livesLeft = num - game.Player.WorldData.Deaths;
			ComposeDialog();
			TryOpen();
		}
		if (secondsDead > 0f)
		{
			ingameMinutesRevivableLeft = game.player.Entity.RevivableIngameHoursLeft() * 60.0;
			if (prevIngMinLeftInt != (int)ingameMinutesRevivableLeft && ingameMinutesRevivableLeft >= 0.0 && Composers["menu"] != null)
			{
				GuiElementDynamicText dynamicText = Composers["menu"].GetDynamicText("reviveCountdown");
				if (ingameMinutesRevivableLeft <= 0.0)
				{
					dynamicText?.SetNewText("");
				}
				else
				{
					dynamicText?.SetNewText(Lang.Get("playerrevival-remainingtime", (int)ingameMinutesRevivableLeft));
				}
			}
			prevIngMinLeftInt = (int)ingameMinutesRevivableLeft;
		}
		if (IsOpened() && game.EntityPlayer.Alive)
		{
			respawning = false;
			TryClose();
		}
	}

	private void ComposeDialog()
	{
		ClearComposers();
		Composers["backgroundd"] = game.GuiComposers.Create("deadbg", ElementBounds.Fill).AddGrayBG(ElementBounds.Fill).Compose();
		string text = Lang.Get("Congratulations, you died!");
		if (livesLeft > 0)
		{
			text = Lang.Get("Congratulations, you died! {0} lives left.", livesLeft);
		}
		if (livesLeft == 0)
		{
			text = Lang.Get("Congratulations, you died! Forever!");
		}
		string text2 = (respawning ? Lang.Get("Respawning...") : Lang.Get("Respawn"));
		CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
		double width = 300.0;
		Composers["menu"] = game.GuiComposers.Create("deadmenu", ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(0.0, 40.0)).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(0.0, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddStaticText(text, CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f).WithFixedWidth(350.0))
			.AddDynamicText("", font, ElementStdBounds.MenuButton(0.35f).WithFixedSize(350.0, 25.0), "reviveCountdown")
			.AddIf(livesLeft != 0)
			.AddButton(text2, OnRespawn, ElementStdBounds.MenuButton(1f).WithFixedWidth(width), EnumButtonStyle.Normal, "respawnbtn")
			.EndIf()
			.AddIf(livesLeft == 0 && game.IsSingleplayer)
			.AddButton(Lang.Get("Delete World"), OnDeleteWorld, ElementStdBounds.MenuButton(1f).WithFixedWidth(width), EnumButtonStyle.Normal, "deletebtn")
			.EndIf()
			.AddButton(Lang.Get("Rage Quit"), OnLeaveWorld, ElementStdBounds.MenuButton(2f).WithFixedWidth(width))
			.EndChildElements()
			.Compose();
		if (Composers["menu"].GetButton("respawnbtn") != null)
		{
			Composers["menu"].GetButton("respawnbtn").Enabled = !respawning;
		}
		if (!respawning && (!game.IsSingleplayer || game.OpenedToLan))
		{
			ingameMinutesRevivableLeft = game.player.Entity.RevivableIngameHoursLeft() * 60.0;
			prevIngMinLeftInt = (int)ingameMinutesRevivableLeft;
			Composers["menu"].GetDynamicText("reviveCountdown")?.SetNewText(Lang.Get("{0} ingame minutes left for player revival", (int)ingameMinutesRevivableLeft));
		}
	}

	private bool OnDeleteWorld()
	{
		game.SendLeave(0);
		game.exitReason = "delete world button pressed";
		game.deleteWorld = true;
		game.DestroyGameSession(gotDisconnected: false);
		return true;
	}

	private bool OnLeaveWorld()
	{
		game.SendLeave(0);
		game.exitReason = "rage quit button pressed";
		game.DestroyGameSession(gotDisconnected: false);
		return true;
	}

	private bool OnRespawn()
	{
		respawning = true;
		ComposeDialog();
		game.Respawn();
		return true;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (Composers["menu"].GetButton("respawnbtn") != null)
		{
			Composers["menu"].GetButton("respawnbtn").Enabled = true;
		}
		game.ShouldRender2DOverlays = true;
	}

	public override bool TryClose()
	{
		if (!game.EntityPlayer.Alive)
		{
			return false;
		}
		return base.TryClose();
	}
}
