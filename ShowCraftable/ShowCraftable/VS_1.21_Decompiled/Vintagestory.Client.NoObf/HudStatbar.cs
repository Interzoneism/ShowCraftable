using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class HudStatbar : HudElement
{
	private float lastHealth;

	private float lastMaxHealth;

	private float lastOxygen;

	private float lastMaxOxygen;

	private float lastSaturation;

	private float lastMaxSaturation;

	private GuiElementStatbar healthbar;

	private GuiElementStatbar oxygenbar;

	private GuiElementStatbar saturationbar;

	private float lastPreviousHealthValue;

	private float? lastFutureHealth;

	private float lastHealthChangeVelocity;

	private long previousHealthHasChangedTotalMs;

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudStatbar(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterGameTickListener(OnGameTick, 20);
		capi.Event.RegisterGameTickListener(OnFlashStatbars, 2500);
	}

	private void OnGameTick(float dt)
	{
		UpdateHealth();
		UpdateOxygen();
		UpdateSaturation();
	}

	private void OnFlashStatbars(float dt)
	{
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute != null && healthbar != null && (double?)(treeAttribute.TryGetFloat("currenthealth") / treeAttribute.TryGetFloat("maxhealth")) < 0.2)
		{
			healthbar.ShouldFlash = true;
		}
		ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (treeAttribute2 != null && saturationbar != null && (double?)(treeAttribute2.TryGetFloat("currentsaturation") / treeAttribute2.TryGetFloat("maxsaturation")) < 0.2)
		{
			saturationbar.ShouldFlash = true;
		}
		if (capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen") != null && (double?)(treeAttribute2.TryGetFloat("currentoxygen") / treeAttribute2.TryGetFloat("maxoxygen")) < 0.2)
		{
			saturationbar.ShouldFlash = true;
		}
	}

	private void UpdateHealth()
	{
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute == null)
		{
			return;
		}
		float? num = treeAttribute.TryGetFloat("currenthealth");
		float? num2 = treeAttribute.TryGetFloat("futureHealth");
		float num3 = treeAttribute.GetFloat("previousHealthValue");
		float num4 = treeAttribute.GetFloat("healthChangeVelocity");
		float? num5 = treeAttribute.TryGetFloat("maxhealth");
		if (!num.HasValue || !num5.HasValue || (lastHealth == num && lastMaxHealth == num5 && num3 == lastPreviousHealthValue && lastHealthChangeVelocity == num4 && lastFutureHealth == num2) || healthbar == null)
		{
			return;
		}
		bool num6 = (double)Math.Abs(lastPreviousHealthValue - num3) > 0.01;
		float? previousValue = num3;
		if (num6)
		{
			if (capi.InWorldEllapsedMilliseconds - previousHealthHasChangedTotalMs < 2000)
			{
				previousValue = lastPreviousHealthValue;
			}
			previousHealthHasChangedTotalMs = capi.InWorldEllapsedMilliseconds;
		}
		healthbar.SetFutureValues(num2, num4);
		healthbar.SetPrevValue(previousValue, previousHealthHasChangedTotalMs, () => capi.InWorldEllapsedMilliseconds);
		healthbar.SetLineInterval(1f);
		healthbar.SetValues(num.Value, 0f, num5.Value);
		lastHealth = num.Value;
		lastMaxHealth = num5.Value;
		lastFutureHealth = num2;
		lastHealthChangeVelocity = num4;
		lastPreviousHealthValue = num3;
	}

	private void UpdateOxygen()
	{
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen");
		if (treeAttribute != null)
		{
			float? num = treeAttribute.TryGetFloat("currentoxygen");
			float? num2 = treeAttribute.TryGetFloat("maxoxygen");
			if (num.HasValue && num2.HasValue && (lastOxygen != num || lastMaxOxygen != num2) && oxygenbar != null)
			{
				oxygenbar.SetLineInterval(1000f);
				oxygenbar.SetValues(num.Value, 0f, num2.Value);
				lastOxygen = num.Value;
				lastMaxOxygen = num2.Value;
			}
		}
	}

	private void UpdateSaturation()
	{
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (treeAttribute != null)
		{
			float? num = treeAttribute.TryGetFloat("currentsaturation");
			float? num2 = treeAttribute.TryGetFloat("maxsaturation");
			if (num.HasValue && num2.HasValue && (lastSaturation != num || lastMaxSaturation != num2) && saturationbar != null)
			{
				saturationbar.SetLineInterval(100f);
				saturationbar.SetValues(num.Value, 0f, num2.Value);
				lastSaturation = num.Value;
				lastMaxSaturation = num2.Value;
			}
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		ComposeGuis();
		UpdateSaturation();
	}

	public void ComposeGuis()
	{
		float num = 850f;
		ElementBounds elementBounds = new ElementBounds
		{
			Alignment = EnumDialogArea.CenterBottom,
			BothSizing = ElementSizing.Fixed,
			fixedWidth = num,
			fixedHeight = 100.0
		}.WithFixedAlignmentOffset(0.0, 5.0);
		ElementBounds elementBounds2 = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)num * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(0.0, 5.0);
		elementBounds2.WithFixedHeight(10.0);
		ElementBounds elementBounds3 = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)num * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(0.0, -15.0);
		elementBounds3.WithFixedHeight(10.0);
		ElementBounds elementBounds4 = ElementStdBounds.Statbar(EnumDialogArea.RightTop, (double)num * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(-1.0, 5.0);
		elementBounds4.WithFixedHeight(10.0);
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
		ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
		ITreeAttribute treeAttribute3 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen");
		Composers["statbar"] = capi.Gui.CreateCompo("inventory-statbar", elementBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(elementBounds).AddIf(treeAttribute2 != null)
			.AddStatbar(elementBounds2, GuiStyle.HealthBarColor, "healthstatbar")
			.EndIf()
			.AddIf(treeAttribute3 != null)
			.AddStatbar(elementBounds3, GuiStyle.OxygenBarColor, hideable: true, "oxygenstatbar")
			.EndIf()
			.AddIf(treeAttribute != null)
			.AddInvStatbar(elementBounds4, GuiStyle.FoodBarColor, "saturationstatbar")
			.EndIf()
			.EndChildElements()
			.Compose();
		healthbar = Composers["statbar"].GetStatbar("healthstatbar");
		oxygenbar = Composers["statbar"].GetStatbar("oxygenstatbar");
		oxygenbar.HideWhenFull = true;
		saturationbar = Composers["statbar"].GetStatbar("saturationstatbar");
		TryOpen();
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			base.OnRenderGUI(deltaTime);
		}
	}

	protected override void OnFocusChanged(bool on)
	{
	}

	public override void OnMouseDown(MouseEvent args)
	{
	}
}
