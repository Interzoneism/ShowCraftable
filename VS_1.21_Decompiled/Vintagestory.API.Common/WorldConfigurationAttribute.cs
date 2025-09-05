using System.Globalization;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class WorldConfigurationAttribute
{
	public EnumDataType DataType;

	public string Category;

	public string Code;

	public double Min;

	public double Max;

	public double Step;

	public double Alarm = 2147483647.0;

	public decimal Multiplier = 1m;

	public decimal DisplayUnit = 1m;

	public bool OnCustomizeScreen = true;

	public string Default;

	public string[] Values;

	public string[] Names;

	public string[] SkipValues;

	public bool OnlyDuringWorldCreate;

	[JsonIgnore]
	public ModInfo ModInfo { get; set; }

	public object TypedDefault => stringToValue(Default);

	public object stringToValue(string text)
	{
		switch (DataType)
		{
		case EnumDataType.Bool:
		{
			bool.TryParse(text, out var result3);
			return result3;
		}
		case EnumDataType.DoubleInput:
		case EnumDataType.DoubleRange:
		{
			double.TryParse(text, NumberStyles.Float, GlobalConstants.DefaultCultureInfo, out var result2);
			return result2;
		}
		case EnumDataType.IntInput:
		case EnumDataType.IntRange:
		{
			int.TryParse(text, NumberStyles.Integer, GlobalConstants.DefaultCultureInfo, out var result);
			return result;
		}
		case EnumDataType.String:
		case EnumDataType.DropDown:
		case EnumDataType.StringRange:
			return text;
		default:
			return null;
		}
	}

	public string valueToHumanReadable(string value)
	{
		switch (DataType)
		{
		case EnumDataType.Bool:
			if (!(value.ToLowerInvariant() == "true"))
			{
				return Lang.Get("Off");
			}
			return Lang.Get("On");
		case EnumDataType.DropDown:
		case EnumDataType.StringRange:
		{
			int num = Values.IndexOf(value);
			string text;
			if (num < 0)
			{
				text = value;
				if (text == null)
				{
					return "";
				}
			}
			else
			{
				text = Lang.Get("worldconfig-" + Code + "-" + Names[num]);
			}
			return text;
		}
		case EnumDataType.IntInput:
		case EnumDataType.DoubleInput:
		case EnumDataType.IntRange:
		case EnumDataType.String:
		case EnumDataType.DoubleRange:
			return value.ToString();
		default:
			return null;
		}
	}
}
