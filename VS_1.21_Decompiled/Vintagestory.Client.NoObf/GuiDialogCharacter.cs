using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class GuiDialogCharacter : GuiDialogCharacterBase
{
	protected IInventory characterInv;

	protected ElementBounds insetSlotBounds;

	protected float yaw = -1.2707963f;

	protected bool rotateCharacter;

	protected bool showArmorSlots = true;

	private int curTab;

	private List<GuiTab> tabs = new List<GuiTab>(new GuiTab[1]
	{
		new GuiTab
		{
			Name = Lang.Get("charactertab-character"),
			DataInt = 0
		}
	});

	public List<Action<GuiComposer>> rendertabhandlers = new List<Action<GuiComposer>>();

	private Size2d mainTabInnerSize = new Size2d();

	private Vec4f lighPos = new Vec4f(-1f, -1f, 0f, 0f).NormalizeXYZ();

	private Matrixf mat = new Matrixf();

	public override string ToggleKeyCombinationCode => "characterdialog";

	public override bool PrefersUngrabbedMouse => false;

	public override float ZSize => RuntimeEnv.GUIScale * 280f;

	public override List<GuiTab> Tabs => tabs;

	public override List<Action<GuiComposer>> RenderTabHandlers => rendertabhandlers;

	public override event Action ComposeExtraGuis;

	public override event Action<int> TabClicked;

	public GuiDialogCharacter(ICoreClientAPI capi)
		: base(capi)
	{
		rendertabhandlers.Add(ComposeCharacterTab);
	}

	private void registerArmorIcons()
	{
		capi.Gui.Icons.CustomIcons["armorhead"] = capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-helmet.svg"));
		capi.Gui.Icons.CustomIcons["armorbody"] = capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-body.svg"));
		capi.Gui.Icons.CustomIcons["armorlegs"] = capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-legs.svg"));
	}

	private void ComposeCharacterTab(GuiComposer compo)
	{
		if (!capi.Gui.Icons.CustomIcons.ContainsKey("left_hand"))
		{
			registerArmorIcons();
		}
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + unscaledSlotPadding, 1, 6).FixedGrow(0.0, unscaledSlotPadding);
		ElementBounds elementBounds2 = null;
		ElementBounds elementBounds3 = null;
		ElementBounds elementBounds4 = null;
		if (showArmorSlots)
		{
			elementBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + unscaledSlotPadding, 1, 1).FixedGrow(0.0, unscaledSlotPadding);
			elementBounds.FixedRightOf(elementBounds2, 10.0);
			elementBounds3 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + unscaledSlotPadding + 102.0, 1, 1).FixedGrow(0.0, unscaledSlotPadding);
			elementBounds.FixedRightOf(elementBounds3, 10.0);
			elementBounds4 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + unscaledSlotPadding + 204.0, 1, 1).FixedGrow(0.0, unscaledSlotPadding);
			elementBounds.FixedRightOf(elementBounds4, 10.0);
		}
		insetSlotBounds = ElementBounds.Fixed(0.0, 22.0 + unscaledSlotPadding, 190.0, elementBounds.fixedHeight - 2.0 * unscaledSlotPadding - 4.0);
		insetSlotBounds.FixedRightOf(elementBounds, 10.0);
		ElementBounds elementBounds5 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + unscaledSlotPadding, 1, 6).FixedGrow(0.0, unscaledSlotPadding);
		elementBounds5.FixedRightOf(insetSlotBounds, 10.0);
		elementBounds.fixedHeight -= 6.0;
		elementBounds5.fixedHeight -= 6.0;
		compo.AddIf(showArmorSlots).AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 12 }, elementBounds2, "armorSlotsHead").AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 13 }, elementBounds3, "armorSlotsBody")
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 14 }, elementBounds4, "armorSlotsLegs")
			.EndIf()
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[6] { 0, 1, 2, 11, 3, 4 }, elementBounds, "leftSlots")
			.AddInset(insetSlotBounds, 0)
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[6] { 6, 7, 8, 10, 5, 9 }, elementBounds5, "rightSlots");
	}

	protected virtual void ComposeGuis()
	{
		characterInv = capi.World.Player.InventoryManager.GetOwnInventory("character");
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		if (curTab == 0)
		{
			elementBounds.BothSizing = ElementSizing.FitToChildren;
		}
		else
		{
			elementBounds.BothSizing = ElementSizing.Fixed;
			elementBounds.fixedWidth = mainTabInnerSize.Width;
			elementBounds.fixedHeight = mainTabInnerSize.Height;
		}
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		string text = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		string text2 = Lang.Get("characterdialog-title-nameandclass", capi.World.Player.PlayerName, Lang.Get("characterclass-" + text));
		if (!Lang.HasTranslation("characterclass-" + text))
		{
			text2 = capi.World.Player.PlayerName;
		}
		ElementBounds bounds2 = ElementBounds.Fixed(5.0, -24.0, 350.0, 25.0);
		ClearComposers();
		Composers["playercharacter"] = capi.Gui.CreateCompo("playercharacter", bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar(text2, OnTitleBarClose)
			.AddHorizontalTabs(tabs.ToArray(), bounds2, onTabClicked, CairoFont.WhiteSmallText().WithWeight((FontWeight)1), CairoFont.WhiteSmallText().WithWeight((FontWeight)1), "tabs")
			.BeginChildElements(elementBounds);
		Composers["playercharacter"].GetHorizontalTabs("tabs").activeElement = curTab;
		rendertabhandlers[curTab](Composers["playercharacter"]);
		Composers["playercharacter"].EndChildElements().Compose();
		if (ComposeExtraGuis != null)
		{
			ComposeExtraGuis();
		}
		if (curTab == 0)
		{
			mainTabInnerSize.Width = elementBounds.InnerWidth / (double)RuntimeEnv.GUIScale;
			mainTabInnerSize.Height = elementBounds.InnerHeight / (double)RuntimeEnv.GUIScale;
		}
	}

	private void onTabClicked(int tabindex)
	{
		TabClicked?.Invoke(tabindex);
		curTab = tabindex;
		ComposeGuis();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		rotateCharacter = insetSlotBounds.PointInside(args.X, args.Y);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		rotateCharacter = false;
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		if (rotateCharacter)
		{
			yaw -= (float)args.DeltaX / 100f;
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
		if (curTab == 0)
		{
			capi.Render.GlPushMatrix();
			if (focused)
			{
				capi.Render.GlTranslate(0f, 0f, 150f);
			}
			double num = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
			capi.Render.GlRotate(-14f, 1f, 0f, 0f);
			mat.Identity();
			mat.RotateXDeg(-14f);
			Vec4f vec4f = mat.TransformVector(lighPos);
			capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(vec4f.X, vec4f.Y, vec4f.Z));
			capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, insetSlotBounds.renderX + num - GuiElement.scaled(41.0), insetSlotBounds.renderY + num - GuiElement.scaled(30.0), GuiElement.scaled(250.0), yaw, (float)GuiElement.scaled(135.0), -1);
			capi.Render.GlPopMatrix();
			capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1f, -1f, 0f).Normalize());
			if (!insetSlotBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && !rotateCharacter)
			{
				yaw += (float)(Math.Sin((float)capi.World.ElapsedMilliseconds / 1000f) / 200.0);
			}
		}
	}

	public override void OnGuiOpened()
	{
		ComposeGuis();
		if ((capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival) && characterInv != null)
		{
			characterInv.Open(capi.World.Player);
		}
	}

	public override void OnGuiClosed()
	{
		if (characterInv != null)
		{
			characterInv.Close(capi.World.Player);
			Composers["playercharacter"].GetSlotGrid("leftSlots")?.OnGuiClosed(capi);
			Composers["playercharacter"].GetSlotGrid("rightSlots")?.OnGuiClosed(capi);
		}
		curTab = 0;
	}

	protected void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}
}
