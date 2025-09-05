using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class HudBosshealthBars : HudElement
{
	private float lastHealth;

	private float lastMaxHealth;

	public int barIndex;

	public EntityAgent TargetEntity;

	public int Dimension;

	private GuiElementStatbar healthbar;

	private long listenerId;

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudBosshealthBars(ICoreClientAPI capi, EntityAgent bossEntity, int barIndex)
		: base(capi)
	{
		TargetEntity = bossEntity;
		listenerId = capi.Event.RegisterGameTickListener(OnGameTick, 20);
		this.barIndex = barIndex;
		ComposeGuis();
		Dimension = bossEntity.ServerPos.Dimension;
	}

	private void OnGameTick(float dt)
	{
		UpdateHealth();
	}

	private void UpdateHealth()
	{
		ITreeAttribute treeAttribute = TargetEntity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute != null)
		{
			float? num = treeAttribute.TryGetFloat("currenthealth");
			float? num2 = treeAttribute.TryGetFloat("maxhealth");
			if (num.HasValue && num2.HasValue && (lastHealth != num || lastMaxHealth != num2) && healthbar != null)
			{
				healthbar.SetLineInterval(1f);
				healthbar.SetValues(num.Value, 0f, num2.Value);
				lastHealth = num.Value;
				lastMaxHealth = num2.Value;
			}
		}
	}

	public void ComposeGuis()
	{
		float num = 850f;
		ElementBounds elementBounds = new ElementBounds
		{
			Alignment = EnumDialogArea.CenterFixed,
			BothSizing = ElementSizing.Fixed,
			fixedWidth = num,
			fixedHeight = 50.0,
			fixedY = 10 + barIndex * 25
		}.WithFixedAlignmentOffset(0.0, 5.0);
		ElementBounds bounds = ElementBounds.Fixed(0.0, 18.0, num, 14.0);
		string text = TargetEntity.GetBehavior<EntityBehaviorBoss>()?.BossName ?? "";
		ITreeAttribute treeAttribute = TargetEntity.WatchedAttributes.GetTreeAttribute("health");
		string dialogName = "bosshealthbar-" + TargetEntity.EntityId;
		Composers["bosshealthbar"] = capi.Gui.CreateCompo(dialogName, elementBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(elementBounds).AddIf(treeAttribute != null)
			.AddStaticText(text, CairoFont.WhiteSmallText(), ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0))
			.AddStatbar(bounds, GuiStyle.HealthBarColor, "healthstatbar")
			.EndIf()
			.EndChildElements()
			.Compose();
		healthbar = Composers["bosshealthbar"].GetStatbar("healthstatbar");
		TryOpen();
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
	}

	protected override void OnFocusChanged(bool on)
	{
	}

	public override void OnMouseDown(MouseEvent args)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		capi.Event.UnregisterGameTickListener(listenerId);
	}
}
