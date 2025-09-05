using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public static class GuiStyle
{
	public static double ElementToDialogPadding;

	public static double HalfPadding;

	public static double DialogToScreenPadding;

	public static double TitleBarHeight;

	public static double DialogBGRadius;

	public static double ElementBGRadius;

	public static double LargeFontSize;

	public static double NormalFontSize;

	public static double SubNormalFontSize;

	public static double SmallishFontSize;

	public static double SmallFontSize;

	public static double DetailFontSize;

	public static string DecorativeFontName;

	public static string StandardFontName;

	public static int LeftDialogMargin;

	public static int RightDialogMargin;

	public static double[] ColorTime1;

	public static double[] ColorTime2;

	public static double[] ColorRust1;

	public static double[] ColorRust2;

	public static double[] ColorRust3;

	public static double[] ColorWood;

	public static double[] ColorParchment;

	public static double[] ColorSchematic;

	public static double[] ColorRot1;

	public static double[] ColorRot2;

	public static double[] ColorRot3;

	public static double[] ColorRot4;

	public static double[] ColorRot5;

	public static double[] DialogSlotBackColor;

	public static double[] DialogSlotFrontColor;

	public static double[] DialogLightBgColor;

	public static double[] DialogDefaultBgColor;

	public static double[] DialogStrongBgColor;

	public static double[] DialogBorderColor;

	public static double[] DialogHighlightColor;

	public static double[] DialogAlternateBgColor;

	public static double[] DialogDefaultTextColor;

	public static double[] DarkBrownColor;

	public static double[] HotbarNumberTextColor;

	public static double[] DiscoveryTextColor;

	public static double[] SuccessTextColor;

	public static string SuccessTextColorHex;

	public static string ErrorTextColorHex;

	public static double[] ErrorTextColor;

	public static double[] WarningTextColor;

	public static double[] LinkTextColor;

	public static double[] ButtonTextColor;

	public static double[] ActiveButtonTextColor;

	public static double[] DisabledTextColor;

	public static double[] ActiveSlotColor;

	public static double[] HealthBarColor;

	public static double[] OxygenBarColor;

	public static double[] FoodBarColor;

	public static double[] XPBarColor;

	public static double[] TitleBarColor;

	public static double[] MacroIconColor;

	public static int[] DamageColorGradient;

	static GuiStyle()
	{
		ElementToDialogPadding = 20.0;
		HalfPadding = 5.0;
		DialogToScreenPadding = 10.0;
		TitleBarHeight = 31.0;
		DialogBGRadius = 1.0;
		ElementBGRadius = 1.0;
		LargeFontSize = 40.0;
		NormalFontSize = 30.0;
		SubNormalFontSize = 24.0;
		SmallishFontSize = 20.0;
		SmallFontSize = 16.0;
		DetailFontSize = 14.0;
		DecorativeFontName = "Lora";
		StandardFontName = "sans-serif";
		ColorTime1 = new double[4]
		{
			56.0 / 255.0,
			232.0 / 255.0,
			61.0 / 85.0,
			1.0
		};
		ColorTime2 = new double[4]
		{
			79.0 / 255.0,
			98.0 / 255.0,
			94.0 / 255.0,
			1.0
		};
		ColorRust1 = new double[4]
		{
			208.0 / 255.0,
			91.0 / 255.0,
			4.0 / 85.0,
			1.0
		};
		ColorRust2 = new double[4]
		{
			143.0 / 255.0,
			47.0 / 255.0,
			0.0,
			1.0
		};
		ColorRust3 = new double[4]
		{
			116.0 / 255.0,
			49.0 / 255.0,
			4.0 / 255.0,
			1.0
		};
		ColorWood = new double[4]
		{
			44.0 / 85.0,
			92.0 / 255.0,
			67.0 / 255.0,
			1.0
		};
		ColorParchment = new double[4]
		{
			79.0 / 85.0,
			206.0 / 255.0,
			152.0 / 255.0,
			1.0
		};
		ColorSchematic = new double[4]
		{
			1.0,
			226.0 / 255.0,
			194.0 / 255.0,
			1.0
		};
		ColorRot1 = new double[4]
		{
			98.0 / 255.0,
			23.0 / 85.0,
			13.0 / 51.0,
			1.0
		};
		ColorRot2 = new double[4]
		{
			0.4,
			22.0 / 51.0,
			112.0 / 255.0,
			1.0
		};
		ColorRot3 = new double[4]
		{
			98.0 / 255.0,
			74.0 / 255.0,
			64.0 / 255.0,
			1.0
		};
		ColorRot4 = new double[4]
		{
			0.17647058823529413,
			7.0 / 51.0,
			11.0 / 85.0,
			1.0
		};
		ColorRot5 = new double[4]
		{
			5.0 / 51.0,
			1.0 / 17.0,
			13.0 / 255.0,
			1.0
		};
		DialogSlotBackColor = ColorSchematic;
		DialogSlotFrontColor = ColorWood;
		DialogLightBgColor = ColorUtil.Hex2Doubles("#403529", 0.75);
		DialogDefaultBgColor = ColorUtil.Hex2Doubles("#403529", 0.8);
		DialogStrongBgColor = ColorUtil.Hex2Doubles("#403529", 1.0);
		DialogBorderColor = new double[4] { 0.0, 0.0, 0.0, 0.3 };
		DialogHighlightColor = ColorUtil.Hex2Doubles("#a88b6c", 0.9);
		DialogAlternateBgColor = ColorUtil.Hex2Doubles("#b5aea6", 0.93);
		DialogDefaultTextColor = ColorUtil.Hex2Doubles("#e9ddce", 1.0);
		DarkBrownColor = ColorUtil.Hex2Doubles("#5a4530", 1.0);
		HotbarNumberTextColor = ColorUtil.Hex2Doubles("#5a4530", 0.5);
		DiscoveryTextColor = ColorParchment;
		SuccessTextColor = new double[4] { 0.5, 1.0, 0.5, 1.0 };
		SuccessTextColorHex = "#80ff80";
		ErrorTextColorHex = "#ff8080";
		ErrorTextColor = new double[4] { 1.0, 0.5, 0.5, 1.0 };
		WarningTextColor = new double[4]
		{
			242.0 / 255.0,
			67.0 / 85.0,
			131.0 / 255.0,
			1.0
		};
		LinkTextColor = new double[4] { 0.5, 0.5, 1.0, 1.0 };
		ButtonTextColor = new double[4]
		{
			224.0 / 255.0,
			69.0 / 85.0,
			11.0 / 15.0,
			1.0
		};
		ActiveButtonTextColor = new double[4]
		{
			197.0 / 255.0,
			137.0 / 255.0,
			24.0 / 85.0,
			1.0
		};
		DisabledTextColor = new double[4] { 1.0, 1.0, 1.0, 0.35 };
		ActiveSlotColor = new double[4]
		{
			98.0 / 255.0,
			197.0 / 255.0,
			73.0 / 85.0,
			1.0
		};
		HealthBarColor = new double[4] { 0.659, 0.0, 0.0, 1.0 };
		OxygenBarColor = new double[4] { 0.659, 0.659, 1.0, 1.0 };
		FoodBarColor = new double[4] { 0.482, 0.521, 0.211, 1.0 };
		XPBarColor = new double[4] { 0.745, 0.61, 0.0, 1.0 };
		TitleBarColor = new double[4] { 0.0, 0.0, 0.0, 0.2 };
		MacroIconColor = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		int[] array = new int[11]
		{
			ColorUtil.Hex2Int("#A7251F"),
			ColorUtil.Hex2Int("#F01700"),
			ColorUtil.Hex2Int("#F04900"),
			ColorUtil.Hex2Int("#F07100"),
			ColorUtil.Hex2Int("#F0D100"),
			ColorUtil.Hex2Int("#F0ED00"),
			ColorUtil.Hex2Int("#E2F000"),
			ColorUtil.Hex2Int("#AAF000"),
			ColorUtil.Hex2Int("#71F000"),
			ColorUtil.Hex2Int("#33F000"),
			ColorUtil.Hex2Int("#00F06B")
		};
		DamageColorGradient = new int[100];
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < 10; j++)
			{
				DamageColorGradient[10 * i + j] = ColorUtil.ColorOverlay(array[i], array[i + 1], (float)j / 10f);
			}
		}
	}
}
