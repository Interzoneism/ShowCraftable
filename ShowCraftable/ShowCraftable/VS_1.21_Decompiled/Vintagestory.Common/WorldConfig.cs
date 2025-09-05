using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class WorldConfig
{
	public List<ModContainer> mods;

	protected List<PlayStyle> playstyles;

	protected string playstylecode;

	protected JsonObject jworldconfig;

	protected Dictionary<string, WorldConfigurationValue> worldConfigsPlaystyle = new Dictionary<string, WorldConfigurationValue>();

	protected Dictionary<string, WorldConfigurationValue> worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>();

	public int MapsizeY = 256;

	public string Seed;

	public bool IsNewWorld;

	public List<PlayStyle> PlayStyles => playstyles;

	public PlayStyle CurrentPlayStyle => playstyles.FirstOrDefault((PlayStyle p) => p.Code == playstylecode);

	public int CurrentPlayStyleIndex => playstyles.IndexOf(CurrentPlayStyle);

	public Dictionary<string, WorldConfigurationValue> WorldConfigsPlaystyle => worldConfigsPlaystyle;

	public Dictionary<string, WorldConfigurationValue> WorldConfigsCustom => worldConfigsCustom;

	public JsonObject Jworldconfig => jworldconfig;

	public WorldConfigurationValue this[string code]
	{
		get
		{
			if (worldConfigsCustom.TryGetValue(code, out var value))
			{
				return value;
			}
			worldConfigsPlaystyle.TryGetValue(code, out value);
			return value;
		}
	}

	internal void loadFromSavegame(SaveGame savegame)
	{
		if (savegame != null)
		{
			Seed = savegame.Seed.ToString();
			MapsizeY = savegame.MapSizeY;
			selectPlayStyle(savegame.PlayStyle);
			loadWorldConfigValues(new JsonObject(JToken.Parse(savegame.WorldConfiguration.ToJsonToken())), WorldConfigsCustom);
			updateJWorldConfig();
		}
	}

	public WorldConfig(List<ModContainer> mods)
	{
		this.mods = mods;
		LoadPlayStyles();
	}

	public void LoadPlayStyles()
	{
		playstyles = new List<PlayStyle>();
		foreach (ModContainer mod in mods)
		{
			if (mod.Error.HasValue || !mod.Enabled || mod.WorldConfig?.PlayStyles == null)
			{
				continue;
			}
			PlayStyle[] playStyles = mod.WorldConfig.PlayStyles;
			foreach (PlayStyle playstyle in playStyles)
			{
				if (playstyles.Find((PlayStyle sAttr) => sAttr.Code == playstyle.Code) == null)
				{
					playstyles.Add(playstyle);
				}
			}
		}
		playstyles = playstyles.OrderBy((PlayStyle p) => p.ListOrder).ToList();
		if (playstyles.Count == 0)
		{
			playstyles.Add(new PlayStyle
			{
				Code = "default",
				LangCode = "default",
				WorldConfig = new JsonObject((JToken)(object)JObject.Parse("{}"))
			});
		}
	}

	public void selectPlayStyle(int index)
	{
		playstylecode = playstyles[index].Code;
		loadWorldConfigValuesFromPlaystyle();
	}

	public void selectPlayStyle(string playstylecode)
	{
		this.playstylecode = playstylecode;
		loadWorldConfigValuesFromPlaystyle();
	}

	private void loadWorldConfigValuesFromPlaystyle()
	{
		if (playstylecode != null)
		{
			PlayStyle currentPlayStyle = CurrentPlayStyle;
			jworldconfig = currentPlayStyle.WorldConfig.Clone();
			loadWorldConfigValues(jworldconfig, worldConfigsPlaystyle);
			updateJWorldConfig();
		}
	}

	public void loadWorldConfigValues(JsonObject jworldconfig, Dictionary<string, WorldConfigurationValue> intoDict)
	{
		intoDict.Clear();
		foreach (ModContainer mod in mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				WorldConfigurationValue worldConfigurationValue = new WorldConfigurationValue();
				worldConfigurationValue.Attribute = worldConfigurationAttribute;
				worldConfigurationValue.Code = worldConfigurationAttribute.Code;
				JsonObject jsonObject = jworldconfig[worldConfigurationValue.Code];
				if (jsonObject.Exists)
				{
					switch (worldConfigurationValue.Attribute.DataType)
					{
					case EnumDataType.Bool:
						worldConfigurationValue.Value = jsonObject.AsBool((bool)worldConfigurationValue.Attribute.TypedDefault);
						break;
					case EnumDataType.DoubleInput:
					case EnumDataType.DoubleRange:
						worldConfigurationValue.Value = jsonObject.AsDouble((double)worldConfigurationValue.Attribute.TypedDefault);
						break;
					case EnumDataType.String:
					case EnumDataType.DropDown:
					case EnumDataType.StringRange:
						worldConfigurationValue.Value = jsonObject.AsString((string)worldConfigurationValue.Attribute.TypedDefault);
						break;
					case EnumDataType.IntInput:
					case EnumDataType.IntRange:
						worldConfigurationValue.Value = jsonObject.AsInt((int)worldConfigurationValue.Attribute.TypedDefault);
						break;
					}
					intoDict[worldConfigurationValue.Code] = worldConfigurationValue;
				}
			}
		}
	}

	public void updateJWorldConfig()
	{
		if (CurrentPlayStyle != null)
		{
			jworldconfig = allDefaultValues(mods);
			updateJWorldConfigFrom(worldConfigsPlaystyle);
			updateJWorldConfigFrom(worldConfigsCustom);
		}
	}

	public static JsonObject allDefaultValues(List<ModContainer> mods)
	{
		JToken val = JToken.Parse("{}");
		JObject val2 = (JObject)(object)((val is JObject) ? val : null);
		foreach (ModContainer mod in mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				switch (worldConfigurationAttribute.DataType)
				{
				case EnumDataType.Bool:
					val2[worldConfigurationAttribute.Code] = JToken.op_Implicit((bool)worldConfigurationAttribute.TypedDefault);
					break;
				case EnumDataType.DoubleInput:
				case EnumDataType.DoubleRange:
					val2[worldConfigurationAttribute.Code] = JToken.op_Implicit((double)worldConfigurationAttribute.TypedDefault);
					break;
				case EnumDataType.IntInput:
				case EnumDataType.IntRange:
					val2[worldConfigurationAttribute.Code] = JToken.op_Implicit((int)worldConfigurationAttribute.TypedDefault);
					break;
				case EnumDataType.String:
				case EnumDataType.DropDown:
				case EnumDataType.StringRange:
					val2[worldConfigurationAttribute.Code] = JToken.op_Implicit((string)worldConfigurationAttribute.TypedDefault);
					break;
				}
			}
		}
		return new JsonObject(val);
	}

	public void updateJWorldConfigFrom(Dictionary<string, WorldConfigurationValue> dict)
	{
		JToken token = jworldconfig.Token;
		JObject val = (JObject)(object)((token is JObject) ? token : null);
		foreach (KeyValuePair<string, WorldConfigurationValue> item in dict)
		{
			object value = item.Value.Value;
			switch (item.Value.Attribute.DataType)
			{
			case EnumDataType.Bool:
				val[item.Key] = JToken.op_Implicit((bool)value);
				break;
			case EnumDataType.DoubleInput:
			case EnumDataType.DoubleRange:
				val[item.Key] = JToken.op_Implicit((double)value);
				break;
			case EnumDataType.IntInput:
			case EnumDataType.IntRange:
				val[item.Key] = JToken.op_Implicit((int)value);
				break;
			case EnumDataType.String:
			case EnumDataType.DropDown:
			case EnumDataType.StringRange:
				val[item.Key] = JToken.op_Implicit((string)value);
				break;
			}
		}
	}

	public void ApplyConfigs(List<GuiElement> inputElements)
	{
		int num = 0;
		worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>();
		foreach (ModContainer mod in mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				if (worldConfigurationAttribute.OnCustomizeScreen)
				{
					GuiElement guiElement = inputElements[num];
					WorldConfigurationValue worldConfigurationValue = new WorldConfigurationValue();
					worldConfigurationValue.Attribute = worldConfigurationAttribute;
					worldConfigurationValue.Code = worldConfigurationAttribute.Code;
					switch (worldConfigurationAttribute.DataType)
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
						worldConfigurationValue.Value = (double)((decimal)guiElementSlider3.GetValue() / worldConfigurationAttribute.Multiplier);
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
						worldConfigurationValue.Value = worldConfigurationAttribute.Values[guiElementSlider.GetValue()];
						break;
					}
					}
					worldConfigsCustom.Add(worldConfigurationValue.Code, worldConfigurationValue);
					num++;
				}
			}
		}
	}

	public string ToRichText(bool withCustomConfigs)
	{
		return ToRichText(CurrentPlayStyle, withCustomConfigs);
	}

	public string ToRichText(PlayStyle playstyle, bool withCustomConfigs)
	{
		if (CurrentPlayStyle == null)
		{
			return "";
		}
		JsonObject jsonObject = playstyle.WorldConfig.Clone();
		if (withCustomConfigs)
		{
			JToken token = jsonObject.Token;
			JObject val = (JObject)(object)((token is JObject) ? token : null);
			foreach (KeyValuePair<string, WorldConfigurationValue> item in worldConfigsCustom)
			{
				object value = item.Value.Value;
				switch (item.Value.Attribute.DataType)
				{
				case EnumDataType.Bool:
					val[item.Key] = JToken.op_Implicit((bool)value);
					break;
				case EnumDataType.DoubleInput:
				case EnumDataType.DoubleRange:
					val[item.Key] = JToken.op_Implicit((double)value);
					break;
				case EnumDataType.IntInput:
				case EnumDataType.IntRange:
					val[item.Key] = JToken.op_Implicit((int)value);
					break;
				case EnumDataType.String:
				case EnumDataType.DropDown:
				case EnumDataType.StringRange:
					val[item.Key] = JToken.op_Implicit((string)value);
					break;
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<font opacity=\"0.6\">" + Lang.Get("World height:") + "</font> " + MapsizeY);
		if (Seed == null || Seed.Length == 0)
		{
			stringBuilder.AppendLine("<font opacity=\"0.6\">" + Lang.Get("Random seed") + "</font> ");
		}
		else
		{
			stringBuilder.AppendLine("<font opacity=\"0.6\">" + Lang.Get("Seed: ", Seed) + "</font> " + Seed);
		}
		foreach (ModContainer mod in mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				WorldConfigurationValue worldConfigurationValue = new WorldConfigurationValue();
				worldConfigurationValue.Attribute = worldConfigurationAttribute;
				worldConfigurationValue.Code = worldConfigurationAttribute.Code;
				JsonObject jsonObject2 = jsonObject[worldConfigurationValue.Code];
				if (jsonObject2.Exists && ((object)jsonObject2.Token).ToString() != worldConfigurationAttribute.Default)
				{
					stringBuilder.AppendLine("<font opacity=\"0.6\">" + Lang.Get("worldattribute-" + worldConfigurationAttribute.Code) + ":</font> " + worldConfigurationAttribute.valueToHumanReadable(((object)jsonObject2.Token).ToString()));
				}
			}
		}
		return stringBuilder.ToString();
	}

	public string ToJson()
	{
		jworldconfig.Token[(object)"playstyle"] = JToken.op_Implicit(playstylecode);
		jworldconfig.Token[(object)"worldHeight"] = JToken.op_Implicit(MapsizeY);
		return jworldconfig.ToString();
	}

	public void FromJson(string json)
	{
		JsonObject jsonObject = new JsonObject(JToken.Parse(json));
		try
		{
			playstylecode = ((jsonObject == null) ? null : ((object)jsonObject.Token[(object)"playstyle"])?.ToString());
			MapsizeY = ((jsonObject == null) ? ((int?)null) : ((object)jsonObject.Token[(object)"worldHeight"])?.ToString()?.ToInt()) ?? MapsizeY;
		}
		catch (Exception)
		{
			return;
		}
		selectPlayStyle(playstylecode);
		loadWorldConfigValues(jsonObject, WorldConfigsCustom);
		updateJWorldConfig();
	}

	public WorldConfig Clone()
	{
		return new WorldConfig(mods)
		{
			playstylecode = playstylecode,
			jworldconfig = jworldconfig.Clone(),
			worldConfigsPlaystyle = new Dictionary<string, WorldConfigurationValue>(worldConfigsPlaystyle),
			worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>(worldConfigsCustom),
			MapsizeY = MapsizeY,
			Seed = Seed,
			IsNewWorld = IsNewWorld
		};
	}
}
