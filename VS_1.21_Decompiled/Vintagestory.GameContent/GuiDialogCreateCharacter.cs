using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogCreateCharacter : GuiDialog
{
	protected bool didSelect;

	protected IInventory characterInv;

	protected ElementBounds insetSlotBounds;

	protected Action<GuiComposer> onBeforeCompose;

	private CharacterSystem modSys;

	private int currentClassIndex;

	private int curTab;

	private int rows = 7;

	private float charZoom = 1f;

	private bool charNaked = true;

	protected int dlgHeight = 513;

	public string[] variantCategories = new string[1] { "standard" };

	protected float yaw = -1.2707963f;

	protected bool rotateCharacter;

	private Vec4f lighPos = new Vec4f(-1f, -1f, 0f, 0f).NormalizeXYZ();

	private Matrixf mat = new Matrixf();

	protected virtual bool AllowClassSelection => true;

	protected virtual bool AllowKeepCurrent => false;

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => true;

	public override float ZSize => (float)GuiElement.scaled(280.0);

	public GuiDialogCreateCharacter(ICoreClientAPI capi, CharacterSystem modSys)
		: base(capi)
	{
		this.modSys = modSys;
	}

	protected virtual bool AllowedSkinPartSelection(string code)
	{
		return true;
	}

	protected void ComposeGuis()
	{
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c96: Unknown result type (might be due to invalid IL or missing references)
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double unscaledSlotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
		characterInv = capi.World.Player.InventoryManager.GetOwnInventory("character");
		ElementBounds bounds = ElementBounds.Fixed(0.0, -25.0, 450.0, 25.0);
		double num = 20.0 + unscaledSlotPadding;
		ElementBounds bounds2 = ElementBounds.FixedSize(717.0, dlgHeight).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds bounds3 = ElementBounds.FixedSize(757.0, dlgHeight + 40).WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		GuiTab[] tabs = new GuiTab[2]
		{
			new GuiTab
			{
				Name = Lang.Get("tab-skinandvoice"),
				DataInt = 0
			},
			new GuiTab
			{
				Name = Lang.Get("tab-charclass"),
				DataInt = 1
			}
		};
		GuiComposer guiComposer = (Composers["createcharacter"] = capi.Gui.CreateCompo("createcharacter", bounds3).AddShadedDialogBG(bounds2).AddDialogTitleBar((curTab == 0) ? Lang.Get("Customize Skin") : ((curTab == 1) ? Lang.Get("Select character class") : Lang.Get("Select your outfit")), OnTitleBarClose)
			.AddIf(AllowClassSelection)
			.AddHorizontalTabs(tabs, bounds, onTabClicked, CairoFont.WhiteSmallText().WithWeight((FontWeight)1), CairoFont.WhiteSmallText().WithWeight((FontWeight)1), "tabs")
			.EndIf()
			.BeginChildElements(bounds2));
		EntityBehaviorPlayerInventory behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
		behavior.hideClothing = false;
		if (curTab == 0)
		{
			EntityBehaviorExtraSkinnable behavior2 = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
			behavior.hideClothing = charNaked;
			(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
			CairoFont cairoFont = CairoFont.WhiteSmallText();
			TextExtents textExtents = cairoFont.GetTextExtents(Lang.Get("Show dressed"));
			int num2 = 22;
			ElementBounds elementBounds = ElementBounds.Fixed(0.0, num, 204.0, dlgHeight - 59).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
			insetSlotBounds = ElementBounds.Fixed(0.0, num + 2.0, 265.0, elementBounds.fixedHeight - 2.0 * unscaledSlotPadding - 10.0).FixedRightOf(elementBounds, 10.0);
			ElementBounds.Fixed(0.0, num, 54.0, dlgHeight - 59).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding).FixedRightOf(insetSlotBounds, 10.0);
			ElementBounds bounds4 = ElementBounds.Fixed((double)(int)insetSlotBounds.fixedX + insetSlotBounds.fixedWidth / 2.0 - ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale / 2.0 - 12.0, 0.0, ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 1.0, ((TextExtents)(ref textExtents)).Height / (double)RuntimeEnv.GUIScale).FixedUnder(insetSlotBounds, 4.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(12.0, 6.0);
			ElementBounds elementBounds2 = null;
			ElementBounds elementBounds3 = null;
			double fixedX = 0.0;
			SkinnablePart[] availableSkinParts = behavior2.AvailableSkinParts;
			foreach (SkinnablePart skinnablePart in availableSkinParts)
			{
				elementBounds2 = ElementBounds.Fixed(fixedX, (elementBounds3 == null || elementBounds3.fixedY == 0.0) ? (-10.0) : (elementBounds3.fixedY + 8.0), num2, num2);
				if (!AllowedSkinPartSelection(skinnablePart.Code))
				{
					continue;
				}
				string code = skinnablePart.Code;
				AppliedSkinnablePartVariant appliedVar = behavior2.AppliedSkinParts.FirstOrDefault((AppliedSkinnablePartVariant sp) => sp.PartCode == code);
				SkinnablePartVariant[] array = skinnablePart.Variants.Where((SkinnablePartVariant p) => variantCategories.Contains(p.Category) || (AllowKeepCurrent && p.Code == appliedVar.Code)).ToArray();
				if (skinnablePart.Type == EnumSkinnableType.Texture && !skinnablePart.UseDropDown)
				{
					int[] colors = array.Select((SkinnablePartVariant p) => p.Color).ToArray();
					int selectedIndex = 0;
					guiComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedSize(210.0, 22.0));
					guiComposer.AddColorListPicker(colors, delegate(int index)
					{
						onToggleSkinPart(code, index);
					}, elementBounds2 = elementBounds2.BelowCopy().WithFixedSize(num2, num2), 180, "picker-" + code);
					for (int num3 = 0; num3 < array.Length; num3++)
					{
						if (array[num3].Code == appliedVar?.Code)
						{
							selectedIndex = num3;
						}
						GuiElementColorListPicker colorListPicker = guiComposer.GetColorListPicker("picker-" + code + "-" + num3);
						colorListPicker.ShowToolTip = true;
						colorListPicker.TooltipText = Lang.Get("color-" + array[num3].Code);
					}
					guiComposer.ColorListPickerSetValue("picker-" + code, selectedIndex);
				}
				else
				{
					int selectedIndex2 = Math.Max(0, array.IndexOf((SkinnablePartVariant v) => v.Code == appliedVar?.Code));
					string[] names = array.Select((SkinnablePartVariant v) => Lang.Get("skinpart-" + code + "-" + v.Code)).ToArray();
					string[] values = array.Select((SkinnablePartVariant v) => v.Code).ToArray();
					guiComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedSize(210.0, 22.0));
					string ifExists = Lang.GetIfExists("skinpartdesc-" + code);
					if (ifExists != null)
					{
						guiComposer.AddHoverText(ifExists, CairoFont.WhiteSmallText(), 300, elementBounds2 = elementBounds2.FlatCopy());
					}
					guiComposer.AddDropDown(values, names, selectedIndex2, delegate(string variantcode, bool selected)
					{
						onToggleSkinPart(code, variantcode);
					}, elementBounds2 = elementBounds2.BelowCopy().WithFixedSize(200.0, 25.0), "dropdown-" + code);
				}
				elementBounds3 = elementBounds2.FlatCopy();
				if (skinnablePart.Colbreak)
				{
					fixedX = insetSlotBounds.fixedX + insetSlotBounds.fixedWidth + 22.0;
					elementBounds3.fixedY = 0.0;
				}
			}
			guiComposer.AddInset(insetSlotBounds, 2).AddToggleButton(Lang.Get("Show dressed"), cairoFont, OnToggleDressOnOff, bounds4, "showdressedtoggle").AddIf(modSys != null)
				.AddButton(Lang.Get("Randomize"), () => OnRandomizeSkin(new Dictionary<string, string>()), ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8.0, 6.0), CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
				.EndIf()
				.AddIf(modSys != null && capi.Settings.String.Exists("lastSkinSelection"))
				.AddButton(Lang.Get("Last selection"), () => OnRandomizeSkin(modSys.getPreviousSelection()), ElementBounds.Fixed(130, dlgHeight - 25).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8.0, 6.0), CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
				.EndIf()
				.AddSmallButton(Lang.Get("Confirm Skin"), OnNext, ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(12.0, 6.0));
			guiComposer.GetToggleButton("showdressedtoggle").SetValue(!charNaked);
			onBeforeCompose?.Invoke(guiComposer);
		}
		if (curTab == 1)
		{
			(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
			num -= 10.0;
			ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, num, 0.0, dlgHeight - 47).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
			insetSlotBounds = ElementBounds.Fixed(0.0, num + 25.0, 190.0, elementBounds4.fixedHeight - 2.0 * unscaledSlotPadding + 10.0).FixedRightOf(elementBounds4, 10.0);
			ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, num, 1, rows).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding).FixedRightOf(insetSlotBounds, 10.0);
			ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, num + 25.0, 35.0, unscaledSlotSize - 4.0).WithFixedPadding(2.0).FixedRightOf(insetSlotBounds, 20.0);
			ElementBounds elementBounds6 = ElementBounds.Fixed(0.0, num + 25.0, 200.0, unscaledSlotSize - 4.0 - 8.0).FixedRightOf(elementBounds5, 20.0);
			ElementBounds elementBounds7 = elementBounds6.ForkBoundingParent(4.0, 4.0, 4.0, 4.0);
			ElementBounds elementBounds8 = ElementBounds.Fixed(0.0, num + 25.0, 35.0, unscaledSlotSize - 4.0).WithFixedPadding(2.0).FixedRightOf(elementBounds7, 20.0);
			CairoFont cairoFont2 = CairoFont.WhiteMediumText();
			double fixedY = elementBounds6.fixedY;
			double fixedHeight = elementBounds6.fixedHeight;
			FontExtents fontExtents = cairoFont2.GetFontExtents();
			elementBounds6.fixedY = fixedY + (fixedHeight - ((FontExtents)(ref fontExtents)).Height / (double)RuntimeEnv.GUIScale) / 2.0;
			ElementBounds bounds5 = ElementBounds.Fixed(0.0, 0.0, 480.0, 100.0).FixedUnder(elementBounds5, 20.0).FixedRightOf(insetSlotBounds, 20.0);
			guiComposer.AddInset(insetSlotBounds, 2).AddIconButton("left", delegate
			{
				changeClass(-1);
			}, elementBounds5.FlatCopy()).AddInset(elementBounds7, 2)
				.AddDynamicText("Commoner", cairoFont2.Clone().WithOrientation(EnumTextOrientation.Center), elementBounds6, "className")
				.AddIconButton("right", delegate
				{
					changeClass(1);
				}, elementBounds8.FlatCopy())
				.AddRichtext("", CairoFont.WhiteDetailText(), bounds5, "characterDesc")
				.AddSmallButton(Lang.Get("Confirm Class"), OnConfirm, ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(12.0, 6.0));
			changeClass(0);
		}
		GuiElementHorizontalTabs horizontalTabs = guiComposer.GetHorizontalTabs("tabs");
		if (horizontalTabs != null)
		{
			horizontalTabs.unscaledTabSpacing = 20.0;
			horizontalTabs.unscaledTabPadding = 10.0;
			horizontalTabs.activeElement = curTab;
		}
		guiComposer.Compose();
	}

	private bool OnRandomizeSkin(Dictionary<string, string> preselection)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		EntityBehaviorPlayerInventory behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
		behavior.doReloadShapeAndSkin = false;
		modSys.randomizeSkin(entity, preselection);
		EntityBehaviorExtraSkinnable behavior2 = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (AppliedSkinnablePartVariant appliedPart in behavior2.AppliedSkinParts)
		{
			string partcode = appliedPart.PartCode;
			SkinnablePart skinnablePart = behavior2.AvailableSkinParts.FirstOrDefault((SkinnablePart part) => part.Code == partcode);
			int selectedIndex = skinnablePart.Variants.IndexOf((SkinnablePartVariant part) => part.Code == appliedPart.Code);
			if (skinnablePart.Type == EnumSkinnableType.Texture && !skinnablePart.UseDropDown)
			{
				Composers["createcharacter"].ColorListPickerSetValue("picker-" + partcode, selectedIndex);
			}
			else
			{
				Composers["createcharacter"].GetDropDown("dropdown-" + partcode).SetSelectedIndex(selectedIndex);
			}
		}
		behavior.doReloadShapeAndSkin = true;
		reTesselate();
		return true;
	}

	private void OnToggleDressOnOff(bool on)
	{
		charNaked = !on;
		capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>().hideClothing = charNaked;
		if (modSys != null)
		{
			string classCode = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass", modSys.characterClasses[0].Code);
			modSys.setCharacterClass(capi.World.Player.Entity, classCode);
		}
		reTesselate();
	}

	protected virtual void onToggleSkinPart(string partCode, string variantCode)
	{
		capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>().selectSkinPart(partCode, variantCode);
	}

	protected virtual void onToggleSkinPart(string partCode, int index)
	{
		EntityBehaviorExtraSkinnable? behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		string code = behavior.AvailableSkinPartsByCode[partCode].Variants[index].Code;
		behavior.selectSkinPart(partCode, code);
	}

	protected virtual bool OnNext()
	{
		if (AllowClassSelection)
		{
			curTab = 1;
			ComposeGuis();
		}
		else
		{
			didSelect = true;
			TryClose();
		}
		return true;
	}

	private void onTabClicked(int tabid)
	{
		curTab = tabid;
		ComposeGuis();
	}

	public override void OnGuiOpened()
	{
		string text = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		if (AllowClassSelection)
		{
			if (text != null)
			{
				modSys.setCharacterClass(capi.World.Player.Entity, text);
			}
			else
			{
				modSys.setCharacterClass(capi.World.Player.Entity, modSys.characterClasses[0].Code);
			}
		}
		ComposeGuis();
		(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
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
			Composers["createcharacter"].GetSlotGrid("leftSlots")?.OnGuiClosed(capi);
			Composers["createcharacter"].GetSlotGrid("rightSlots")?.OnGuiClosed(capi);
		}
		if (modSys != null)
		{
			CharacterClass characterClass = modSys.characterClasses[currentClassIndex];
			modSys.ClientSelectionDone(characterInv, characterClass.Code, didSelect);
		}
		capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>().hideClothing = false;
		reTesselate();
	}

	private bool OnConfirm()
	{
		didSelect = true;
		TryClose();
		return true;
	}

	protected virtual void OnTitleBarClose()
	{
		TryClose();
	}

	protected void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}

	private void changeClass(int dir)
	{
		currentClassIndex = GameMath.Mod(currentClassIndex + dir, modSys.characterClasses.Count);
		CharacterClass characterClass = modSys.characterClasses[currentClassIndex];
		Composers["createcharacter"].GetDynamicText("className").SetNewText(Lang.Get("characterclass-" + characterClass.Code));
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("characterdesc-" + characterClass.Code));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Lang.Get("traits-title"));
		foreach (Trait item in from code in characterClass.Traits
			select modSys.TraitsByCode[code] into trait
			orderby (int)trait.Type
			select trait)
		{
			stringBuilder2.Clear();
			foreach (KeyValuePair<string, double> attribute in item.Attributes)
			{
				if (stringBuilder2.Length > 0)
				{
					stringBuilder2.Append(", ");
				}
				stringBuilder2.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", attribute.Key, attribute.Value)));
			}
			if (stringBuilder2.Length > 0)
			{
				stringBuilder.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + item.Code), stringBuilder2));
				continue;
			}
			string ifExists = Lang.GetIfExists("traitdesc-" + item.Code);
			if (ifExists != null)
			{
				stringBuilder.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + item.Code), ifExists));
			}
			else
			{
				stringBuilder.AppendLine(Lang.Get("trait-" + item.Code));
			}
		}
		if (characterClass.Traits.Length == 0)
		{
			stringBuilder.AppendLine(Lang.Get("No positive or negative traits"));
		}
		Composers["createcharacter"].GetRichtext("characterDesc").SetNewText(stringBuilder.ToString(), CairoFont.WhiteDetailText());
		modSys.setCharacterClass(capi.World.Player.Entity, characterClass.Code);
		reTesselate();
	}

	protected void reTesselate()
	{
		(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
	}

	public void PrepAndOpen()
	{
		TryOpen();
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		if (insetSlotBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && curTab == 0)
		{
			charZoom = GameMath.Clamp(charZoom + args.deltaPrecise / 5f, 0.5f, 1f);
		}
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
		if (capi.IsGamePaused)
		{
			capi.World.Player.Entity.talkUtil.OnGameTick(deltaTime);
		}
		capi.Render.GlPushMatrix();
		if (focused)
		{
			capi.Render.GlTranslate(0f, 0f, 150f);
		}
		capi.Render.GlRotate(-14f, 1f, 0f, 0f);
		mat.Identity();
		mat.RotateXDeg(-14f);
		Vec4f vec4f = mat.TransformVector(lighPos);
		double num = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(vec4f.X, vec4f.Y, vec4f.Z));
		capi.Render.PushScissor(insetSlotBounds);
		double posX = insetSlotBounds.renderX + num - GuiElement.scaled(195.0) * (double)charZoom + GuiElement.scaled(115f * (1f - charZoom));
		double posY = insetSlotBounds.renderY + num + GuiElement.scaled(10f * (1f - charZoom));
		double posZ = (float)GuiElement.scaled(230.0);
		float size = (float)GuiElement.scaled(330f * charZoom);
		if (curTab == 1)
		{
			posX = insetSlotBounds.renderX + num - GuiElement.scaled(110.0);
			posY = insetSlotBounds.renderY + num - GuiElement.scaled(15.0);
			size = (float)GuiElement.scaled(205.0);
		}
		capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, posX, posY, posZ, yaw, size, -1);
		capi.Render.PopScissor();
		capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1f, -1f, 0f).Normalize());
		capi.Render.GlPopMatrix();
	}
}
