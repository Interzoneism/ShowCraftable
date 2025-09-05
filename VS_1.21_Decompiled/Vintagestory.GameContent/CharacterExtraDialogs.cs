using System;
using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class CharacterExtraDialogs : ModSystem
{
	private ICoreClientAPI capi;

	private GuiDialogCharacterBase dlg;

	private GuiDialog.DlgComposers Composers => dlg.Composers;

	public event Action<StringBuilder> OnEnvText;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	private bool IsOpened()
	{
		return dlg.IsOpened();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		base.StartClientSide(api);
		dlg = api.Gui.LoadedGuis.Find((GuiDialog dlg) => dlg is GuiDialogCharacterBase) as GuiDialogCharacterBase;
		dlg.OnOpened += Dlg_OnOpened;
		dlg.OnClosed += Dlg_OnClosed;
		dlg.TabClicked += Dlg_TabClicked;
		dlg.ComposeExtraGuis += Dlg_ComposeExtraGuis;
		api.Event.RegisterGameTickListener(On2sTick, 2000);
	}

	private void Dlg_TabClicked(int tabIndex)
	{
		if (tabIndex != 0)
		{
			Dlg_OnClosed();
		}
		if (tabIndex == 0)
		{
			Dlg_OnOpened();
		}
	}

	private void Dlg_ComposeExtraGuis()
	{
		ComposeEnvGui();
		ComposeStatsGui();
	}

	private void On2sTick(float dt)
	{
		if (IsOpened())
		{
			updateEnvText();
		}
	}

	private void Dlg_OnClosed()
	{
		capi.World.Player.Entity.WatchedAttributes.UnregisterListener(UpdateStatBars);
		capi.World.Player.Entity.WatchedAttributes.UnregisterListener(UpdateStats);
	}

	private void Dlg_OnOpened()
	{
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("hunger", UpdateStatBars);
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("stats", UpdateStats);
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("bodyTemp", UpdateStats);
	}

	public virtual void ComposeEnvGui()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds bounds = Composers["playercharacter"].Bounds;
		CairoFont cairoFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
		string envText = getEnvText();
		int num = 1 + Regex.Matches(envText, "\n").Count;
		FontExtents fontExtents = cairoFont.GetFontExtents();
		double fixedHeight = ((FontExtents)(ref fontExtents)).Height * cairoFont.LineHeightMultiplier * (double)num / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 25.0, (int)(bounds.InnerWidth / (double)RuntimeEnv.GUIScale - 40.0), fixedHeight);
		elementBounds.Name = "textbounds";
		ElementBounds elementBounds2 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds2.Name = "bgbounds";
		elementBounds2.BothSizing = ElementSizing.FitToChildren;
		elementBounds2.WithChildren(elementBounds);
		ElementBounds elementBounds3 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.None).WithFixedPosition(bounds.renderX / (double)RuntimeEnv.GUIScale, bounds.renderY / (double)RuntimeEnv.GUIScale + bounds.OuterHeight / (double)RuntimeEnv.GUIScale + 10.0);
		elementBounds3.Name = "dialogbounds";
		Composers["environment"] = capi.Gui.CreateCompo("environment", elementBounds3).AddShadedDialogBG(elementBounds2).AddDialogTitleBar(Lang.Get("Environment"), delegate
		{
			dlg.OnTitleBarClose();
		})
			.BeginChildElements(elementBounds2)
			.AddDynamicText(envText, cairoFont, elementBounds, "dyntext")
			.EndChildElements()
			.Compose();
	}

	private void updateEnvText()
	{
		if (IsOpened() && Composers?["environment"] != null)
		{
			Composers["environment"].GetDynamicText("dyntext").SetNewTextAsync(getEnvText());
		}
	}

	private string getEnvText()
	{
		string text = capi.World.Calendar.PrettyDate();
		ClimateCondition climateAt = capi.World.BlockAccessor.GetClimateAt(capi.World.Player.Entity.Pos.AsBlockPos);
		string text2 = "?";
		string text3 = "?";
		if (climateAt != null)
		{
			text2 = (int)climateAt.Temperature + "°C";
			text3 = Lang.Get("freq-veryrare");
			if ((double)climateAt.WorldgenRainfall > 0.9)
			{
				text3 = Lang.Get("freq-allthetime");
			}
			else if ((double)climateAt.WorldgenRainfall > 0.7)
			{
				text3 = Lang.Get("freq-verycommon");
			}
			else if ((double)climateAt.WorldgenRainfall > 0.45)
			{
				text3 = Lang.Get("freq-common");
			}
			else if ((double)climateAt.WorldgenRainfall > 0.3)
			{
				text3 = Lang.Get("freq-uncommon");
			}
			else if ((double)climateAt.WorldgenRainfall > 0.15)
			{
				text3 = Lang.Get("freq-rarely");
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Lang.Get("character-envtext", text, text2, text3));
		this.OnEnvText?.Invoke(stringBuilder);
		return stringBuilder.ToString();
	}

	public virtual void ComposeStatsGui()
	{
		ElementBounds bounds = Composers["playercharacter"].Bounds;
		ElementBounds bounds2 = Composers["environment"].Bounds;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 25.0, 90.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(120.0, 30.0, 120.0, 8.0);
		ElementBounds leftColumnBoundsW = ElementBounds.Fixed(0.0, 0.0, 140.0, 20.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(165.0, 0.0, 120.0, 20.0);
		EntityPlayer entity = capi.World.Player.Entity;
		double num = bounds2.InnerHeight / (double)RuntimeEnv.GUIScale + 10.0;
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 0.0, 235.0, bounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + num).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds bounds3 = elementBounds4.ForkBoundingParent().WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset((bounds.renderX + bounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale, num / 2.0);
		getHealthSat(out var health, out var maxHealth, out var saturation, out var maxSaturation);
		float blended = entity.Stats.GetBlended("walkspeed");
		float blended2 = entity.Stats.GetBlended("healingeffectivness");
		float blended3 = entity.Stats.GetBlended("hungerrate");
		float blended4 = entity.Stats.GetBlended("rangedWeaponsAcc");
		float blended5 = entity.Stats.GetBlended("rangedWeaponsSpeed");
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
		float num2 = entity.WatchedAttributes.GetFloat("wetness");
		string text = "";
		if ((double)num2 > 0.7)
		{
			text = Lang.Get("wetness_soakingwet");
		}
		else if ((double)num2 > 0.4)
		{
			text = Lang.Get("wetness_wet");
		}
		else if ((double)num2 > 0.1)
		{
			text = Lang.Get("wetness_slightlywet");
		}
		Composers["playerstats"] = capi.Gui.CreateCompo("playerstats", bounds3).AddShadedDialogBG(elementBounds4).AddDialogTitleBar(Lang.Get("Stats"), delegate
		{
			dlg.OnTitleBarClose();
		})
			.BeginChildElements(elementBounds4);
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("playerinfo-nutrition"), CairoFont.WhiteSmallText().WithWeight((FontWeight)1), elementBounds.WithFixedWidth(200.0)).AddStaticText(Lang.Get("playerinfo-nutrition-Freeza"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy().WithFixedWidth(90.0)).AddStaticText(Lang.Get("playerinfo-nutrition-Vegita"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Krillin"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Cell"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Dairy"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy())
				.AddStatbar(elementBounds2 = elementBounds2.BelowCopy(0.0, 16.0), GuiStyle.FoodBarColor, "fruitBar")
				.AddStatbar(elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "vegetableBar")
				.AddStatbar(elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "grainBar")
				.AddStatbar(elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "proteinBar")
				.AddStatbar(elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "dairyBar");
			leftColumnBoundsW = leftColumnBoundsW.FixedUnder(elementBounds, -5.0);
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Physical"), CairoFont.WhiteSmallText().WithWeight((FontWeight)1), leftColumnBoundsW.WithFixedWidth(200.0).WithFixedOffset(0.0, 23.0)).Execute(delegate
		{
			leftColumnBoundsW = leftColumnBoundsW.FlatCopy();
			leftColumnBoundsW.fixedY += 5.0;
		});
		if (health.HasValue)
		{
			GuiComposer composer = Composers["playerstats"].AddStaticText(Lang.Get("Health Points"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy());
			float? num3 = health;
			string? text2 = num3.ToString();
			num3 = maxHealth;
			composer.AddDynamicText(text2 + " / " + num3, CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY).WithFixedHeight(30.0), "health");
		}
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Satiety"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)saturation.Value + " / " + (int)maxSaturation.Value, CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "satiety");
		}
		if (treeAttribute != null)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Body Temperature"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddRichtext((treeAttribute == null) ? "-" : getBodyTempText(treeAttribute), CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "bodytemp");
		}
		if (text.Length > 0)
		{
			Composers["playerstats"].AddRichtext(text, CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy());
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Walk speed"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * blended) + "%", CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "walkspeed").AddStaticText(Lang.Get("Healing effectivness"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy())
			.AddDynamicText((int)Math.Round(100f * blended2) + "%", CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "healeffectiveness");
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Hunger rate"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * blended3) + "%", CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "hungerrate");
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Ranged Accuracy"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * blended4) + "%", CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "rangedweaponacc").AddStaticText(Lang.Get("Ranged Charge Speed"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy())
			.AddDynamicText((int)Math.Round(100f * blended5) + "%", CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "rangedweaponchargespeed")
			.EndChildElements()
			.Compose();
		UpdateStatBars();
	}

	private string getBodyTempText(ITreeAttribute tempTree)
	{
		float num = tempTree.GetFloat("bodytemp");
		if (num > 37f)
		{
			num = 37f + (num - 37f) / 10f;
		}
		return $"{num:0.#}°C";
	}

	private void getHealthSat(out float? health, out float? maxHealth, out float? saturation, out float? maxSaturation)
	{
		health = null;
		maxHealth = null;
		saturation = null;
		maxSaturation = null;
		ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute != null)
		{
			health = treeAttribute.TryGetFloat("currenthealth");
			maxHealth = treeAttribute.TryGetFloat("maxhealth");
		}
		if (health.HasValue)
		{
			health = (float)Math.Round(health.Value, 1);
		}
		if (maxHealth.HasValue)
		{
			maxHealth = (float)Math.Round(maxHealth.Value, 1);
		}
		ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (treeAttribute2 != null)
		{
			saturation = treeAttribute2.TryGetFloat("currentsaturation");
			maxSaturation = treeAttribute2.TryGetFloat("maxsaturation");
		}
		if (saturation.HasValue)
		{
			saturation = (int)saturation.Value;
		}
	}

	private void UpdateStats()
	{
		EntityPlayer entity = capi.World.Player.Entity;
		GuiComposer guiComposer = Composers["playerstats"];
		if (guiComposer != null && IsOpened())
		{
			getHealthSat(out var health, out var maxHealth, out var saturation, out var maxSaturation);
			float blended = entity.Stats.GetBlended("walkspeed");
			float blended2 = entity.Stats.GetBlended("healingeffectivness");
			float blended3 = entity.Stats.GetBlended("hungerrate");
			float blended4 = entity.Stats.GetBlended("rangedWeaponsAcc");
			float blended5 = entity.Stats.GetBlended("rangedWeaponsSpeed");
			if (health.HasValue)
			{
				GuiElementDynamicText dynamicText = guiComposer.GetDynamicText("health");
				float? num = health;
				string? text = num.ToString();
				num = maxHealth;
				dynamicText.SetNewText(text + " / " + num);
			}
			if (saturation.HasValue)
			{
				guiComposer.GetDynamicText("satiety").SetNewText((int)saturation.Value + " / " + (int)maxSaturation.Value);
			}
			guiComposer.GetDynamicText("walkspeed").SetNewText((int)Math.Round(100f * blended) + "%");
			guiComposer.GetDynamicText("healeffectiveness").SetNewText((int)Math.Round(100f * blended2) + "%");
			guiComposer.GetDynamicText("hungerrate")?.SetNewText((int)Math.Round(100f * blended3) + "%");
			guiComposer.GetDynamicText("rangedweaponacc").SetNewText((int)Math.Round(100f * blended4) + "%");
			guiComposer.GetDynamicText("rangedweaponchargespeed").SetNewText((int)Math.Round(100f * blended5) + "%");
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
			guiComposer.GetRichtext("bodytemp").SetNewText(getBodyTempText(treeAttribute), CairoFont.WhiteDetailText());
		}
	}

	private void UpdateStatBars()
	{
		GuiComposer guiComposer = Composers["playerstats"];
		if (guiComposer != null && IsOpened())
		{
			ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (treeAttribute != null)
			{
				float num = treeAttribute.GetFloat("currentsaturation");
				float num2 = treeAttribute.GetFloat("maxsaturation");
				float value = treeAttribute.GetFloat("fruitLevel");
				float value2 = treeAttribute.GetFloat("vegetableLevel");
				float value3 = treeAttribute.GetFloat("grainLevel");
				float value4 = treeAttribute.GetFloat("proteinLevel");
				float value5 = treeAttribute.GetFloat("dairyLevel");
				guiComposer.GetDynamicText("satiety").SetNewText((int)num + " / " + num2);
				Composers["playerstats"].GetStatbar("fruitBar").SetLineInterval(num2 / 10f);
				Composers["playerstats"].GetStatbar("vegetableBar").SetLineInterval(num2 / 10f);
				Composers["playerstats"].GetStatbar("grainBar").SetLineInterval(num2 / 10f);
				Composers["playerstats"].GetStatbar("proteinBar").SetLineInterval(num2 / 10f);
				Composers["playerstats"].GetStatbar("dairyBar").SetLineInterval(num2 / 10f);
				Composers["playerstats"].GetStatbar("fruitBar").SetValues(value, 0f, num2);
				Composers["playerstats"].GetStatbar("vegetableBar").SetValues(value2, 0f, num2);
				Composers["playerstats"].GetStatbar("grainBar").SetValues(value3, 0f, num2);
				Composers["playerstats"].GetStatbar("proteinBar").SetValues(value4, 0f, num2);
				Composers["playerstats"].GetStatbar("dairyBar").SetValues(value5, 0f, num2);
			}
		}
	}
}
