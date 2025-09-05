using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenWorldCustomize : GuiScreen
{
	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	private Dictionary<string, List<GuiElement>> elementsByCategory = new Dictionary<string, List<GuiElement>>();

	private List<GuiElement> allInputElements = new List<GuiElement>();

	private GuiTab[] tabs;

	private List<string> categories = new List<string>();

	private List<WorldConfigurationAttribute> sortedAttributes = new List<WorldConfigurationAttribute>();

	private Action<bool> didApply;

	private GuiElementContainer container;

	public WorldConfig wcu;

	private List<PlaystyleListEntry> cells;

	public GuiScreenWorldCustomize(Action<bool> didApply, ScreenManager screenManager, GuiScreen parentScreen, WorldConfig wcu, List<PlaystyleListEntry> playstyles)
		: base(screenManager, parentScreen)
	{
		this.wcu = wcu;
		if (playstyles == null)
		{
			loadPlaystyleCells();
		}
		List<ModContainer> list = ScreenManager.verifiedMods.ToList();
		list.Sort(delegate(ModContainer x, ModContainer y)
		{
			if (x?.Info?.CoreMod == true && y?.Info?.CoreMod == true)
			{
				return 0;
			}
			if (x?.Info?.CoreMod == true)
			{
				return -1;
			}
			return (y?.Info?.CoreMod == true) ? 1 : (x?.Info?.Name.CompareTo(y?.Info?.Name)).GetValueOrDefault();
		});
		foreach (ModContainer item in list)
		{
			ModWorldConfiguration worldConfig = item.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (attribute.OnCustomizeScreen && sortedAttributes.Find((WorldConfigurationAttribute sAttr) => sAttr.Code == attribute.Code) == null)
				{
					attribute.ModInfo = item.Info;
					int num2 = sortedAttributes.FindLastIndex((WorldConfigurationAttribute nAttribute) => nAttribute.Category == attribute.Category);
					if (sortedAttributes.Count == 0 || num2 == -1 || num2 + 1 >= sortedAttributes.Count)
					{
						sortedAttributes.Add(attribute);
					}
					else
					{
						sortedAttributes.Insert(num2 + 1, attribute);
					}
				}
			}
		}
		this.didApply = didApply;
		ShowMainMenu = true;
		screenManager.GamePlatform.WindowResized += delegate
		{
			invalidate();
		};
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			invalidate();
		});
	}

	public override void OnScreenLoaded()
	{
		base.OnScreenLoaded();
		InitGui();
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayercustomize");
		}
	}

	private void InitGui()
	{
		cells = loadPlaystyleCells();
		tabs = loadTabs();
		double num = (float)ScreenManager.GamePlatform.WindowSize.Width / RuntimeEnv.GUIScale;
		double num2 = (float)ScreenManager.GamePlatform.WindowSize.Height / RuntimeEnv.GUIScale;
		double num3 = Math.Max(400.0, num * 0.5);
		double num4 = Math.Max(300.0, num2 - 175.0);
		double num5 = num3 - 20.0;
		ElementBounds elementBounds = ElementBounds.FixedSize(60.0, 25.0).WithFixedPadding(10.0, 0.0);
		ElementBounds bounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, (int)GameMath.Clamp(num5 / 2.0, 300.0, num5), 30.0);
		ElementBounds rightColumn = ElementBounds.Fixed(-52.0, 0.0, (int)GameMath.Clamp(num5 / 2.0, 125.0, num5 - 300.0), 30.0).WithAlignment(EnumDialogArea.RightFixed);
		string[] values = cells.Select((PlaystyleListEntry c) => c.PlayStyle.Code).ToArray();
		string[] names = cells.Select((PlaystyleListEntry c) => c.Title).ToArray();
		int selectedIndex = cells.FindIndex((PlaystyleListEntry c) => c.PlayStyle.Code == wcu.CurrentPlayStyle.Code);
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, 185.0, num3, 20.0);
		ElementBounds elementBounds3;
		ElementComposer = dialogBase("mainmenu-singleplayercustomize").AddStaticText(Lang.Get("singleplayer-customize"), CairoFont.WhiteSmallishText(), bounds).AddStaticText(Lang.Get("singleplayer-playstyle"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), "playstyleText").AddDropDown(values, names, selectedIndex, onPlayStyleChanged, rightColumn = rightColumn.BelowCopy(-31.0, 12.0).WithFixedWidth(rightColumn.fixedWidth - 30.0), "playstyleDropDown")
			.AddIconButton("paste", OnPasteWorldConfig, rightColumn.CopyOffsetedSibling(32.0).WithFixedSize(30.0, 31.0))
			.AddHoverText(Lang.Get("playstyle-pastefromclipboard"), CairoFont.WhiteDetailText(), 200, rightColumn.CopyOffsetedSibling(32.0).WithFixedSize(30.0, 31.0).WithFixedPadding(5.0))
			.AddStaticText(Lang.Get("singleplayer-worldheight"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 12.0), "worldHeightText")
			.AddSlider(onNewWorldHeightValue, rightColumn = rightColumn.BelowCopy(31.0, 15.0).WithFixedSize(rightColumn.fixedWidth + 30.0, 20.0), "worldHeight")
			.AddStaticText(Lang.Get("singleplayer-seed"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 11.0), "worldseedText")
			.AddTextInput(rightColumn = rightColumn.BelowCopy(0.0, 18.0).WithFixedHeight(30.0), null, null, "worldseed")
			.AddIf(!wcu.IsNewWorld)
			.AddRichtext("<font opacity=\"0.6\">" + Lang.Get("singleplayer-disabledcustomizations") + "</font>", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 8.0).WithFixedWidth(600.0))
			.Execute(delegate
			{
				rightColumn = rightColumn.BelowCopy(0.0, 18.0);
			})
			.EndIf()
			.AddHorizontalTabs(tabs, bounds2, onTabClicked, CairoFont.WhiteSmallText().WithWeight((FontWeight)1), CairoFont.WhiteSmallText().WithWeight((FontWeight)1).WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.AddInset(elementBounds3 = elementBounds2.BelowCopy(0.0, 12.0).WithFixedSize(num3, num4 - elementBounds2.fixedY - elementBounds2.fixedHeight))
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0));
		if (!wcu.IsNewWorld)
		{
			ElementComposer.GetStaticText("playstyleText").Font.Color[3] = 0.5;
			ElementComposer.GetStaticText("worldHeightText").Font.Color[3] = 0.5;
			ElementComposer.GetStaticText("worldseedText").Font.Color[3] = 0.5;
			ElementComposer.GetDropDown("playstyleDropDown").Enabled = false;
			ElementComposer.GetSlider("worldHeight").Enabled = false;
			ElementComposer.GetTextInput("worldseed").Enabled = false;
		}
		container = new GuiElementContainer(ElementComposer.Api, listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0));
		container.Tabbable = true;
		int num6 = 0;
		double num7 = 26.0;
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 2.0, (int)GameMath.Clamp(num5 / 2.0, 300.0, num5), num7);
		ElementBounds elementBounds5 = ElementBounds.Fixed(-20.0, 0.0, (int)GameMath.Clamp(num5 / 2.0, 125.0, num5 - 300.0), num7).WithAlignment(EnumDialogArea.RightFixed);
		elementBounds4 = elementBounds4.FlatCopy();
		elementBounds5 = elementBounds5.FlatCopy();
		elementsByCategory.Clear();
		allInputElements.Clear();
		Dictionary<string, List<GuiElement>> dictionary = new Dictionary<string, List<GuiElement>>();
		foreach (WorldConfigurationAttribute attribute in sortedAttributes)
		{
			if (!attribute.OnCustomizeScreen)
			{
				continue;
			}
			if (!elementsByCategory.TryGetValue(attribute.Category, out var value))
			{
				value = (elementsByCategory[attribute.Category] = new List<GuiElement>());
				elementBounds4 = ElementBounds.Fixed(0.0, 2.0, (int)GameMath.Clamp(num5 / 2.0, 300.0, num5), num7);
				elementBounds5 = ElementBounds.Fixed(-20.0, 0.0, (int)GameMath.Clamp(num5 / 2.0, 125.0, num5 - 300.0), num7).WithAlignment(EnumDialogArea.RightFixed);
				elementBounds4 = elementBounds4.FlatCopy();
				elementBounds5 = elementBounds5.FlatCopy();
			}
			if (!dictionary.TryGetValue(attribute.Category, out var value2))
			{
				value2 = (dictionary[attribute.Category] = new List<GuiElement>());
			}
			bool flag = wcu.IsNewWorld || !attribute.OnlyDuringWorldCreate;
			WorldConfigurationValue worldConfigurationValue = wcu[attribute.Code];
			object typedDefault = attribute.TypedDefault;
			GuiElementControl guiElementControl = null;
			switch (attribute.DataType)
			{
			case EnumDataType.Bool:
			{
				bool value4 = (bool)typedDefault;
				if (worldConfigurationValue != null)
				{
					value4 = (bool)worldConfigurationValue.Value;
				}
				GuiElementSwitch guiElementSwitch = new GuiElementSwitch(ScreenManager.api, null, elementBounds5.FlatCopy().WithFixedAlignmentOffset(0.0 - elementBounds5.fixedWidth + num7, 0.0), num7);
				guiElementSwitch.SetValue(value4);
				guiElementControl = guiElementSwitch;
				break;
			}
			case EnumDataType.DoubleInput:
			{
				double value6 = (double)typedDefault;
				if (worldConfigurationValue != null)
				{
					value6 = (double)worldConfigurationValue.Value;
				}
				GuiElementNumberInput guiElementNumberInput2 = new GuiElementNumberInput(ScreenManager.api, elementBounds5?.FlatCopy(), null, CairoFont.WhiteSmallText());
				guiElementNumberInput2.SetValue(value6);
				guiElementControl = guiElementNumberInput2;
				break;
			}
			case EnumDataType.DoubleRange:
			{
				double num16 = (double)typedDefault;
				if (worldConfigurationValue != null)
				{
					num16 = (double)worldConfigurationValue.Value;
				}
				GuiElementSlider guiElementSlider3 = new GuiElementSlider(ScreenManager.api, null, elementBounds5.FlatCopy());
				guiElementSlider3.TooltipExceedClipBounds = true;
				guiElementSlider3.ShowTextWhenResting = true;
				if (attribute.SkipValues != null)
				{
					string[] skipValues = attribute.SkipValues;
					for (int num9 = 0; num9 < skipValues.Length; num9++)
					{
						string[] array4 = skipValues[num9].Split("...", 2, StringSplitOptions.RemoveEmptyEntries);
						if (array4.Length == 1)
						{
							guiElementSlider3.AddSkipValue((int)((decimal)array4[0].ToDouble() * attribute.Multiplier));
							continue;
						}
						int num17 = (int)((decimal)array4[0].ToDouble() * attribute.Multiplier);
						int num18 = (int)((decimal)array4[1].ToDouble() * attribute.Multiplier);
						for (int num19 = (int)((decimal)attribute.Step * attribute.Multiplier); num17 <= num18; num17 += num19)
						{
							guiElementSlider3.AddSkipValue(num17);
						}
					}
				}
				string unitCode = "worldattribute-" + attribute.Code + "-unit";
				int alarm2 = (int)((decimal)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max) * attribute.Multiplier);
				if (attribute.Values != null && attribute.Names != null)
				{
					string[] values4 = attribute.Values;
					string[] names4 = new string[attribute.Names.Length];
					for (int num20 = 0; num20 < values4.Length; num20++)
					{
						string text5 = "worldconfig-" + attribute.Code + "-" + attribute.Names[num20];
						if (ClientSettings.DeveloperMode && !Lang.HasTranslation(text5))
						{
							Console.WriteLine("\"{0}\": \"{1}\",", text5, attribute.Names[num20]);
						}
						names4[num20] = Lang.Get(text5);
					}
					guiElementSlider3.OnSliderTooltip = (int num21) => values4.Contains<string>(((decimal)num21 / attribute.Multiplier).ToString()) ? names4[values4.IndexOf<string>(((decimal)num21 / attribute.Multiplier).ToString())] : (Lang.GetWithFallback(unitCode, "{0}", (decimal)num21 / attribute.Multiplier / attribute.DisplayUnit) + ((num21 > alarm2) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "") : ""));
					guiElementSlider3.OnSliderRestingText = (int num21) => values4.Contains<string>(((decimal)num21 / attribute.Multiplier).ToString()) ? names4[values4.IndexOf<string>(((decimal)num21 / attribute.Multiplier).ToString())] : Lang.GetWithFallback(unitCode, "{0}", (decimal)num21 / attribute.Multiplier / attribute.DisplayUnit);
				}
				else
				{
					guiElementSlider3.OnSliderTooltip = (int num21) => Lang.GetWithFallback(unitCode, "{0}", (decimal)num21 / attribute.Multiplier / attribute.DisplayUnit) + ((num21 > alarm2) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "") : "");
					guiElementSlider3.OnSliderRestingText = (int num21) => Lang.GetWithFallback(unitCode, "{0}", (decimal)num21 / attribute.Multiplier / attribute.DisplayUnit);
				}
				guiElementSlider3.SetValues((int)((decimal)num16 * attribute.Multiplier), (int)((decimal)attribute.Min * attribute.Multiplier), (int)((decimal)attribute.Max * attribute.Multiplier), (int)((decimal)attribute.Step * attribute.Multiplier));
				guiElementControl = guiElementSlider3;
				break;
			}
			case EnumDataType.IntInput:
			{
				double value5 = (int)typedDefault;
				if (worldConfigurationValue != null)
				{
					value5 = (int)worldConfigurationValue.Value;
				}
				GuiElementNumberInput guiElementNumberInput = new GuiElementNumberInput(ScreenManager.api, elementBounds5?.FlatCopy(), null, CairoFont.WhiteSmallText());
				guiElementNumberInput.IntMode = true;
				guiElementNumberInput.SetValue(value5);
				guiElementControl = guiElementNumberInput;
				break;
			}
			case EnumDataType.IntRange:
			{
				int currentValue2 = (int)typedDefault;
				if (worldConfigurationValue != null)
				{
					currentValue2 = (int)worldConfigurationValue.Value;
				}
				GuiElementSlider guiElementSlider2 = new GuiElementSlider(ScreenManager.api, null, elementBounds5.FlatCopy());
				guiElementSlider2.TooltipExceedClipBounds = true;
				guiElementSlider2.ShowTextWhenResting = true;
				if (attribute.SkipValues != null)
				{
					string[] skipValues = attribute.SkipValues;
					for (int num9 = 0; num9 < skipValues.Length; num9++)
					{
						string[] array = skipValues[num9].Split("...", 2, StringSplitOptions.RemoveEmptyEntries);
						if (array.Length == 1)
						{
							guiElementSlider2.AddSkipValue(array[0].ToInt());
							continue;
						}
						int num10 = array[1].ToInt();
						int num11 = (int)attribute.Step;
						for (int num12 = array[0].ToInt(); num12 <= num10; num12 += num11)
						{
							guiElementSlider2.AddSkipValue(num12);
						}
					}
				}
				string unit = "worldattribute-" + attribute.Code + "-unit";
				int alarm = (int)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max);
				if (attribute.Values != null && attribute.Names != null)
				{
					string[] values3 = attribute.Values;
					string[] names3 = new string[attribute.Names.Length];
					for (int num13 = 0; num13 < values3.Length; num13++)
					{
						string text2 = "worldconfig-" + attribute.Code + "-" + attribute.Names[num13];
						if (ClientSettings.DeveloperMode && !Lang.HasTranslation(text2))
						{
							Console.WriteLine("\"{0}\": \"{1}\",", text2, attribute.Names[num13]);
						}
						names3[num13] = Lang.Get(text2);
					}
					guiElementSlider2.OnSliderTooltip = (int num21) => values3.Contains<string>(num21.ToString()) ? names3[values3.IndexOf<string>(num21.ToString())] : (Lang.Get(unit, (decimal)num21 / attribute.DisplayUnit) + ((num21 > alarm) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "") : ""));
					guiElementSlider2.OnSliderRestingText = (int num21) => values3.Contains<string>(num21.ToString()) ? names3[values3.IndexOf<string>(num21.ToString())] : Lang.Get(unit, (decimal)num21 / attribute.DisplayUnit);
				}
				else
				{
					guiElementSlider2.OnSliderTooltip = (int num21) => Lang.GetWithFallback(unit, "{0}", (decimal)num21 / attribute.DisplayUnit) + ((num21 > alarm) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "") : "");
					guiElementSlider2.OnSliderRestingText = (int num21) => Lang.GetWithFallback(unit, "{0}", (decimal)num21 / attribute.DisplayUnit);
				}
				guiElementSlider2.SetValues(currentValue2, (int)attribute.Min, (int)attribute.Max, (int)attribute.Step);
				guiElementControl = guiElementSlider2;
				break;
			}
			case EnumDataType.DropDown:
			{
				string value3 = (string)typedDefault;
				if (worldConfigurationValue != null)
				{
					value3 = (string)worldConfigurationValue.Value;
				}
				int num14 = attribute.Values.IndexOf(value3);
				string[] array2 = attribute.Values;
				string[] array3 = new string[attribute.Names.Length];
				for (int num15 = 0; num15 < array2.Length; num15++)
				{
					string text3 = "worldconfig-" + attribute.Code + "-" + attribute.Names[num15];
					if (ClientSettings.DeveloperMode && !Lang.HasTranslation(text3))
					{
						Console.WriteLine("\"{0}\": \"{1}\",", text3, attribute.Names[num15]);
					}
					array3[num15] = Lang.Get(text3);
				}
				if (num14 < 0)
				{
					array2 = array2.Append(value3);
					array3 = array3.Append(value3);
					num14 = array3.Length - 1;
				}
				guiElementControl = new GuiElementDropDown(ScreenManager.api, array2, array3, num14, null, elementBounds5.FlatCopy(), CairoFont.WhiteSmallText(), multiSelect: false);
				break;
			}
			case EnumDataType.String:
			{
				string text4 = (string)typedDefault;
				if (worldConfigurationValue != null)
				{
					text4 = (string)worldConfigurationValue.Value;
				}
				GuiElementTextInput guiElementTextInput = new GuiElementTextInput(ScreenManager.api, elementBounds5.FlatCopy(), null, CairoFont.WhiteSmallText());
				guiElementTextInput.SetValue(text4);
				guiElementControl = guiElementTextInput;
				break;
			}
			case EnumDataType.StringRange:
			{
				string[] values2 = attribute.Values;
				string[] names2 = new string[attribute.Names.Length];
				int currentValue = values2.IndexOf((string)typedDefault);
				if (worldConfigurationValue != null)
				{
					currentValue = values2.IndexOf((string)worldConfigurationValue.Value);
				}
				for (int num8 = 0; num8 < values2.Length; num8++)
				{
					string text = "worldconfig-" + attribute.Code + "-" + attribute.Names[num8];
					if (ClientSettings.DeveloperMode && !Lang.HasTranslation(text))
					{
						Console.WriteLine("\"{0}\": \"{1}\",", text, attribute.Names[num8]);
					}
					names2[num8] = Lang.Get(text);
				}
				GuiElementSlider guiElementSlider = new GuiElementSlider(ScreenManager.api, null, elementBounds5.FlatCopy());
				guiElementSlider.TooltipExceedClipBounds = true;
				guiElementSlider.ShowTextWhenResting = true;
				guiElementSlider.OnSliderTooltip = (int num21) => names2[num21];
				guiElementSlider.SetValues(currentValue, 0, values2.Length - 1, 1);
				guiElementControl = guiElementSlider;
				break;
			}
			}
			guiElementControl.Enabled = flag;
			value.Add(guiElementControl);
			allInputElements.Add(guiElementControl);
			CairoFont cairoFont = (attribute.ModInfo.CoreMod ? CairoFont.WhiteSmallText() : CairoFont.WhiteSmallText().WithColor(GuiStyle.WarningTextColor));
			if (!flag)
			{
				cairoFont.Color[3] = 0.5;
			}
			value.Add(new GuiElementStaticText(ScreenManager.api, Lang.Get("worldattribute-" + attribute.Code), EnumTextOrientation.Left, elementBounds4, cairoFont));
			string text6 = Lang.GetIfExists("worldattribute-" + attribute.Code + "-desc");
			if (text6 != null)
			{
				ElementBounds elementBounds6 = elementBounds4.FlatCopy();
				elementBounds6.fixedWidth -= 50.0;
				if (!attribute.ModInfo.CoreMod)
				{
					text6 = text6 + "\n\n<font color=\"#F2C983\">" + Lang.Get("createworld-worldattribute-notcoremodhover", attribute.ModInfo.Name) + "</font>";
				}
				GuiElementHoverText item = new GuiElementHoverText(ScreenManager.api, text6, CairoFont.WhiteSmallText(), 320, elementBounds6);
				value2.Add(item);
			}
			elementBounds4 = elementBounds4.BelowCopy(0.0, 9.9);
			elementBounds5 = elementBounds5.BelowCopy(0.0, 10.0);
			num6++;
		}
		foreach (KeyValuePair<string, List<GuiElement>> item2 in elementsByCategory)
		{
			if (dictionary.TryGetValue(item2.Key, out var value7))
			{
				foreach (GuiElement item3 in value7)
				{
					item2.Value.Add(item3);
				}
			}
			item2.Value.Add(new GuiElementStaticText(ScreenManager.api, " ", EnumTextOrientation.Left, elementBounds4 = elementBounds4.BelowCopy(), CairoFont.WhiteDetailText()));
		}
		updateWorldHeightSlider();
		ElementComposer.AddInteractiveElement(container, "configlist").EndClip().AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.AddButton(Lang.Get("general-back"), OnBack, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0))
			.AddButton(Lang.Get("general-apply"), OnApply, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0).WithFixedWidth(200.0)
				.WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-13.0, 0.0))
			.EndChildElements()
			.Compose();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		selectTab(0);
		ElementComposer.GetTextInput("worldseed").SetValue(wcu.Seed);
		setConfigSliderAlarmValues();
	}

	private GuiTab[] loadTabs()
	{
		List<GuiTab> list = new List<GuiTab>();
		categories.Clear();
		int num = 0;
		foreach (WorldConfigurationAttribute sortedAttribute in sortedAttributes)
		{
			if (sortedAttribute.OnCustomizeScreen && !categories.Contains(sortedAttribute.Category))
			{
				categories.Add(sortedAttribute.Category);
				list.Add(new GuiTab
				{
					Name = Lang.Get("worldconfig-category-" + sortedAttribute.Category),
					DataInt = num++
				});
			}
		}
		return list.ToArray();
	}

	private void onTabClicked(int dataint)
	{
		selectTab(dataint);
	}

	private void selectTab(int tabIndex)
	{
		string key = categories[tabIndex];
		container.Clear();
		foreach (GuiElement item in elementsByCategory[key])
		{
			container.Add(item);
		}
		ElementComposer.ReCompose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		updateWorldHeightSlider();
	}

	private void setConfigSliderAlarmValues()
	{
		int num = 0;
		foreach (WorldConfigurationAttribute sortedAttribute in sortedAttributes)
		{
			if (!sortedAttribute.OnCustomizeScreen)
			{
				continue;
			}
			if (!wcu.IsNewWorld && sortedAttribute.OnlyDuringWorldCreate)
			{
				num++;
				continue;
			}
			_ = wcu[sortedAttribute.Code];
			_ = sortedAttribute.TypedDefault;
			GuiElement guiElement = allInputElements[num];
			switch (sortedAttribute.DataType)
			{
			case EnumDataType.DoubleRange:
				(guiElement as GuiElementSlider).SetAlarmValue((int)((decimal)Math.Clamp(sortedAttribute.Alarm, sortedAttribute.Min, sortedAttribute.Max) * sortedAttribute.Multiplier));
				break;
			case EnumDataType.IntRange:
				(guiElement as GuiElementSlider).SetAlarmValue((int)Math.Clamp(sortedAttribute.Alarm, sortedAttribute.Min, sortedAttribute.Max));
				break;
			}
			num++;
		}
	}

	private void setFieldValues()
	{
		int num = 0;
		foreach (WorldConfigurationAttribute sortedAttribute in sortedAttributes)
		{
			if (!sortedAttribute.OnCustomizeScreen)
			{
				continue;
			}
			if (!wcu.IsNewWorld && sortedAttribute.OnlyDuringWorldCreate)
			{
				num++;
				continue;
			}
			WorldConfigurationValue worldConfigurationValue = wcu[sortedAttribute.Code];
			object typedDefault = sortedAttribute.TypedDefault;
			GuiElement guiElement = allInputElements[num];
			switch (sortedAttribute.DataType)
			{
			case EnumDataType.Bool:
			{
				GuiElementSwitch obj4 = guiElement as GuiElementSwitch;
				bool value = (bool)typedDefault;
				if (worldConfigurationValue != null)
				{
					value = (bool)worldConfigurationValue.Value;
				}
				obj4.SetValue(value);
				break;
			}
			case EnumDataType.DoubleInput:
			{
				GuiElementNumberInput obj6 = guiElement as GuiElementNumberInput;
				double value3 = (double)typedDefault;
				if (worldConfigurationValue != null)
				{
					value3 = (double)worldConfigurationValue.Value;
				}
				obj6.SetValue(value3);
				break;
			}
			case EnumDataType.DoubleRange:
			{
				GuiElementSlider obj5 = guiElement as GuiElementSlider;
				double num3 = (double)typedDefault;
				if (worldConfigurationValue != null)
				{
					num3 = (double)worldConfigurationValue.Value;
				}
				obj5.SetValues((int)((decimal)num3 * sortedAttribute.Multiplier), (int)((decimal)sortedAttribute.Min * sortedAttribute.Multiplier), (int)((decimal)sortedAttribute.Max * sortedAttribute.Multiplier), (int)((decimal)sortedAttribute.Step * sortedAttribute.Multiplier));
				break;
			}
			case EnumDataType.IntInput:
			{
				GuiElementNumberInput obj3 = guiElement as GuiElementNumberInput;
				int num2 = (int)typedDefault;
				if (worldConfigurationValue != null)
				{
					num2 = (int)worldConfigurationValue.Value;
				}
				obj3.SetValue(num2);
				break;
			}
			case EnumDataType.IntRange:
			{
				GuiElementSlider obj2 = guiElement as GuiElementSlider;
				int currentValue2 = (int)typedDefault;
				if (worldConfigurationValue != null)
				{
					currentValue2 = (int)worldConfigurationValue.Value;
				}
				obj2.SetValues(currentValue2, (int)sortedAttribute.Min, (int)sortedAttribute.Max, (int)sortedAttribute.Step);
				break;
			}
			case EnumDataType.DropDown:
			{
				string value2 = (string)typedDefault;
				if (worldConfigurationValue != null)
				{
					value2 = (string)worldConfigurationValue.Value;
				}
				int selectedIndex = sortedAttribute.Values.IndexOf(value2);
				(guiElement as GuiElementDropDown).SetSelectedIndex(selectedIndex);
				break;
			}
			case EnumDataType.String:
			{
				string text = (string)typedDefault;
				if (worldConfigurationValue != null)
				{
					text = (string)worldConfigurationValue.Value;
				}
				(guiElement as GuiElementTextInput).SetValue(text);
				break;
			}
			case EnumDataType.StringRange:
			{
				GuiElementSlider obj = guiElement as GuiElementSlider;
				int currentValue = sortedAttribute.Values.IndexOf((string)typedDefault);
				if (worldConfigurationValue != null)
				{
					currentValue = sortedAttribute.Values.IndexOf((string)worldConfigurationValue.Value);
				}
				obj.SetValues(currentValue, 0, sortedAttribute.Values.Length - 1, 1);
				break;
			}
			}
			num++;
		}
	}

	private void onPlayStyleChanged(string code, bool selected)
	{
		wcu.selectPlayStyle(code);
		updateWorldHeightSlider();
		setFieldValues();
	}

	private void updateWorldHeightSlider()
	{
		if (wcu.CurrentPlayStyle.Code != "creativebuilding")
		{
			ElementComposer.GetSlider("worldHeight").SetValues(wcu.MapsizeY, 128, 512, 64, " blocks");
			ElementComposer.GetSlider("worldHeight").SetAlarmValue(384);
			ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", value) + ((value > 384) ? ("\n" + Lang.Get("createworld-worldheight-warning")) : "");
		}
		else
		{
			ElementComposer.GetSlider("worldHeight").SetValues(wcu.MapsizeY, 128, 2048, 64, " blocks");
			ElementComposer.GetSlider("worldHeight").SetAlarmValue(1024);
			ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", value) + ((value > 1024) ? ("\n" + Lang.Get("createworld-worldheight-warning")) : "");
		}
	}

	private List<PlaystyleListEntry> loadPlaystyleCells()
	{
		cells = new List<PlaystyleListEntry>();
		wcu.LoadPlayStyles();
		foreach (PlayStyle playStyle in wcu.PlayStyles)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("playstyle-" + playStyle.LangCode),
				PlayStyle = playStyle
			});
		}
		if (cells.Count == 0)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("noplaystyles-title"),
				DetailText = Lang.Get("noplaystyles-desc"),
				PlayStyle = null,
				Enabled = false
			});
		}
		return cells;
	}

	private bool onNewWorldHeightValue(int value)
	{
		wcu.MapsizeY = value;
		return true;
	}

	public override void OnKeyDown(KeyEvent e)
	{
		base.OnKeyDown(e);
		if (e.CtrlPressed && e.KeyCode == 104)
		{
			OnPasteWorldConfig();
		}
	}

	private void OnPasteWorldConfig(bool ok = false)
	{
		try
		{
			string clipboardText = ScreenManager.Platform.XPlatInterface.GetClipboardText();
			if (clipboardText.StartsWith("{"))
			{
				wcu.FromJson(clipboardText);
				ScreenManager.GamePlatform.Logger.Notification("Pasted world config loaded!");
				updateWorldHeightSlider();
				setFieldValues();
			}
		}
		catch (Exception e)
		{
			ScreenManager.GamePlatform.Logger.Warning("Unable to load pasted world config:");
			ScreenManager.GamePlatform.Logger.Warning(e);
		}
	}

	private bool OnApply()
	{
		wcu.Seed = ElementComposer.GetTextInput("worldseed").GetText();
		wcu.MapsizeY = ElementComposer.GetSlider("worldHeight").GetValue();
		int num = 0;
		wcu.WorldConfigsCustom.Clear();
		foreach (WorldConfigurationAttribute sortedAttribute in sortedAttributes)
		{
			if (sortedAttribute.OnCustomizeScreen)
			{
				GuiElement guiElement = allInputElements[num];
				WorldConfigurationValue worldConfigurationValue = new WorldConfigurationValue();
				worldConfigurationValue.Attribute = sortedAttribute;
				worldConfigurationValue.Code = sortedAttribute.Code;
				switch (sortedAttribute.DataType)
				{
				case EnumDataType.Bool:
				{
					GuiElementSwitch guiElementSwitch = guiElement as GuiElementSwitch;
					worldConfigurationValue.Value = guiElementSwitch.On;
					break;
				}
				case EnumDataType.DoubleInput:
				{
					GuiElementNumberInput guiElementNumberInput2 = guiElement as GuiElementNumberInput;
					worldConfigurationValue.Value = guiElementNumberInput2.GetText().ToDouble();
					break;
				}
				case EnumDataType.DoubleRange:
				{
					GuiElementSlider guiElementSlider3 = guiElement as GuiElementSlider;
					worldConfigurationValue.Value = (double)((decimal)guiElementSlider3.GetValue() / sortedAttribute.Multiplier);
					break;
				}
				case EnumDataType.IntInput:
				{
					GuiElementNumberInput guiElementNumberInput = guiElement as GuiElementNumberInput;
					worldConfigurationValue.Value = guiElementNumberInput.GetText().ToInt();
					break;
				}
				case EnumDataType.IntRange:
				{
					GuiElementSlider guiElementSlider2 = guiElement as GuiElementSlider;
					worldConfigurationValue.Value = guiElementSlider2.GetValue();
					break;
				}
				case EnumDataType.DropDown:
				{
					GuiElementDropDown guiElementDropDown = guiElement as GuiElementDropDown;
					worldConfigurationValue.Value = guiElementDropDown.SelectedValue;
					break;
				}
				case EnumDataType.String:
				{
					GuiElementTextInput guiElementTextInput = guiElement as GuiElementTextInput;
					worldConfigurationValue.Value = guiElementTextInput.GetText();
					break;
				}
				case EnumDataType.StringRange:
				{
					GuiElementSlider guiElementSlider = guiElement as GuiElementSlider;
					worldConfigurationValue.Value = sortedAttribute.Values[guiElementSlider.GetValue()];
					break;
				}
				}
				wcu.WorldConfigsCustom.Add(worldConfigurationValue.Code, worldConfigurationValue);
				num++;
			}
		}
		wcu.updateJWorldConfig();
		didApply(obj: true);
		return true;
	}

	private bool OnBack()
	{
		didApply(obj: false);
		return true;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = container.Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
		foreach (GuiElement element in container.Elements)
		{
			if (element is GuiElementDropDown guiElementDropDown && guiElementDropDown.listMenu.IsOpened)
			{
				guiElementDropDown.listMenu.Close();
			}
		}
	}
}
