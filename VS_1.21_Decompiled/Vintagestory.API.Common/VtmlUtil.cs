using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class VtmlUtil
{
	public static Dictionary<string, Tag2RichTextDelegate> TagConverters = new Dictionary<string, Tag2RichTextDelegate>();

	private static CairoFont monospacedFont = new CairoFont(16.0, "Consolas", new double[4] { 1.0, 1.0, 1.0, 1.0 });

	public static RichTextComponentBase[] Richtextify(ICoreClientAPI capi, string vtmlCode, CairoFont baseFont, Action<LinkTextComponent> didClickLink = null)
	{
		List<RichTextComponentBase> elems = new List<RichTextComponentBase>();
		Stack<CairoFont> stack = new Stack<CairoFont>();
		stack.Push(baseFont);
		VtmlToken[] tokens = VtmlParser.Tokenize(capi.Logger, vtmlCode);
		Richtextify(capi, tokens, ref elems, stack, didClickLink);
		return elems.ToArray();
	}

	private static void Richtextify(ICoreClientAPI capi, VtmlToken[] tokens, ref List<RichTextComponentBase> elems, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink)
	{
		for (int i = 0; i < tokens.Length; i++)
		{
			Richtextify(capi, tokens[i], ref elems, fontStack, didClickLink);
		}
	}

	private static void Richtextify(ICoreClientAPI capi, VtmlToken token, ref List<RichTextComponentBase> elems, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink)
	{
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		if (token is VtmlTagToken)
		{
			VtmlTagToken vtmlTagToken = token as VtmlTagToken;
			switch (vtmlTagToken.Name)
			{
			case "br":
				elems.Add(new RichTextComponent(capi, "\r\n", fontStack.Peek()));
				break;
			case "hk":
			case "hotkey":
				if (!string.IsNullOrEmpty(vtmlTagToken.ContentText) && !vtmlTagToken.ContentText.All(char.IsWhiteSpace))
				{
					string text = vtmlTagToken.ContentText;
					if (text == "leftmouse")
					{
						text = "primarymouse";
					}
					if (text == "rightmouse")
					{
						text = "secondarymouse";
					}
					if (text == "toolmode")
					{
						text = "toolmodeselect";
					}
					HotkeyComponent hotkeyComponent = new HotkeyComponent(capi, text, fontStack.Peek());
					hotkeyComponent.PaddingLeft -= 1.0;
					hotkeyComponent.PaddingRight += 3.0;
					elems.Add(hotkeyComponent);
				}
				break;
			case "i":
			{
				CairoFont cairoFont = fontStack.Peek().Clone();
				cairoFont.Slant = (FontSlant)1;
				fontStack.Push(cairoFont);
				foreach (VtmlToken childElement in vtmlTagToken.ChildElements)
				{
					Richtextify(capi, childElement, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			case "a":
			{
				LinkTextComponent linkTextComponent = new LinkTextComponent(capi, vtmlTagToken.ContentText, fontStack.Peek(), didClickLink);
				if (!vtmlTagToken.Attributes.TryGetValue("href", out linkTextComponent.Href))
				{
					capi.Logger.Warning("Language file includes an <a /> link missing href");
				}
				elems.Add(linkTextComponent);
				break;
			}
			case "icon":
			{
				vtmlTagToken.Attributes.TryGetValue("name", out var value);
				vtmlTagToken.Attributes.TryGetValue("path", out var value2);
				if (value == null)
				{
					value = vtmlTagToken.ContentText;
				}
				IconComponent iconComponent = new IconComponent(capi, value, value2, fontStack.Peek());
				LineRectangled obj = iconComponent.BoundsPerLine[0];
				FontExtents fontExtents = fontStack.Peek().GetFontExtents();
				obj.Ascent = ((FontExtents)(ref fontExtents)).Ascent;
				elems.Add(iconComponent);
				break;
			}
			case "itemstack":
			{
				FontExtents fontExtents2 = fontStack.Peek().GetFontExtents();
				float num3 = (float)((FontExtents)(ref fontExtents2)).Height;
				EnumFloat result = EnumFloat.Inline;
				if (vtmlTagToken.Attributes.TryGetValue("floattype", out var value3) && !Enum.TryParse<EnumFloat>(value3, out result))
				{
					result = EnumFloat.Inline;
				}
				vtmlTagToken.Attributes.TryGetValue("code", out var value4);
				if (!vtmlTagToken.Attributes.TryGetValue("type", out var value5))
				{
					value5 = "block";
				}
				if (value4 == null)
				{
					value4 = vtmlTagToken.ContentText;
				}
				List<ItemStack> list = new List<ItemStack>();
				string[] array2 = value4.Split('|');
				foreach (string domainAndPath in array2)
				{
					CollectibleObject collectibleObject = ((!(value5 == "item")) ? ((CollectibleObject)capi.World.GetBlock(new AssetLocation(domainAndPath))) : ((CollectibleObject)capi.World.GetItem(new AssetLocation(domainAndPath))));
					if (collectibleObject == null)
					{
						collectibleObject = capi.World.GetBlock(0);
					}
					list.Add(new ItemStack(collectibleObject));
				}
				float num4 = 1.3f;
				if (vtmlTagToken.Attributes.TryGetValue("rsize", out var value6))
				{
					num4 *= value6.ToFloat();
				}
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, list.ToArray(), num3 / RuntimeEnv.GUIScale, result);
				slideshowItemstackTextComponent.Background = true;
				slideshowItemstackTextComponent.renderSize *= num4;
				slideshowItemstackTextComponent.VerticalAlign = EnumVerticalAlign.Middle;
				slideshowItemstackTextComponent.BoundsPerLine[0].Ascent = ((FontExtents)(ref fontExtents2)).Ascent;
				if (vtmlTagToken.Attributes.TryGetValue("offx", out var value7))
				{
					slideshowItemstackTextComponent.offX = GuiElement.scaled(value7.ToFloat());
				}
				if (vtmlTagToken.Attributes.TryGetValue("offy", out var value8))
				{
					slideshowItemstackTextComponent.offY = GuiElement.scaled(value8.ToFloat());
				}
				elems.Add(slideshowItemstackTextComponent);
				break;
			}
			case "font":
				fontStack.Push(getFont(vtmlTagToken, fontStack));
				foreach (VtmlToken childElement2 in vtmlTagToken.ChildElements)
				{
					Richtextify(capi, childElement2, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			case "clear":
				elems.Add(new ClearFloatTextComponent(capi));
				break;
			case "code":
			{
				double[] color = fontStack.Peek().Color;
				int num = ColorUtil.Rgb2Hsv((float)color[0], (float)color[1], (float)color[2]) | -16777216;
				num >>= 8;
				int num2 = ColorUtil.Hsv2Rgb((num & 0xFF00) + ((num & 0xFF) << 16) + ((num >> 16) & 0xFF));
				double[] array = new double[4];
				array[3] = 1.0;
				array[2] = (double)(num2 & 0xFF) / 255.0;
				array[1] = (double)((num2 >> 8) & 0xFF) / 255.0;
				array[0] = (double)((num2 >> 16) & 0xFF) / 255.0;
				fontStack.Push(monospacedFont.Clone().WithColor(array));
				foreach (VtmlToken childElement3 in vtmlTagToken.ChildElements)
				{
					Richtextify(capi, childElement3, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			case "strong":
				fontStack.Push(fontStack.Peek().Clone().WithWeight((FontWeight)1));
				foreach (VtmlToken childElement4 in vtmlTagToken.ChildElements)
				{
					Richtextify(capi, childElement4, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			if (vtmlTagToken.Name != null && TagConverters.ContainsKey(vtmlTagToken.Name))
			{
				RichTextComponentBase richTextComponentBase = TagConverters[vtmlTagToken.Name](capi, vtmlTagToken, fontStack, didClickLink);
				if (richTextComponentBase != null)
				{
					elems.Add(richTextComponentBase);
				}
			}
		}
		else
		{
			VtmlTextToken vtmlTextToken = token as VtmlTextToken;
			elems.Add(new RichTextComponent(capi, vtmlTextToken.Text, fontStack.Peek()));
		}
	}

	private static CairoFont getFont(VtmlTagToken tag, Stack<CairoFont> fontStack)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		EnumTextOrientation orientation = EnumTextOrientation.Left;
		double[] color = ColorUtil.WhiteArgbDouble;
		FontWeight val = (FontWeight)0;
		CairoFont cairoFont = fontStack.Pop();
		if (!tag.Attributes.ContainsKey("size") || !double.TryParse(tag.Attributes["size"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			result = cairoFont.UnscaledFontsize;
		}
		if (tag.Attributes.ContainsKey("scale"))
		{
			string text2 = tag.Attributes["scale"];
			if (text2.EndsWith("%") && double.TryParse(text2.Substring(0, text2.Length - 1), NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result2))
			{
				result = cairoFont.UnscaledFontsize * result2 / 100.0;
			}
		}
		text = (tag.Attributes.ContainsKey("family") ? tag.Attributes["family"] : cairoFont.Fontname);
		if (!tag.Attributes.ContainsKey("color") || !parseHexColor(tag.Attributes["color"], out color))
		{
			color = cairoFont.Color;
		}
		if (tag.Attributes.ContainsKey("opacity") && double.TryParse(tag.Attributes["opacity"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result3))
		{
			color = (double[])color.Clone();
			color[3] *= result3;
		}
		val = ((!tag.Attributes.ContainsKey("weight")) ? cairoFont.FontWeight : ((FontWeight)(tag.Attributes["weight"] == "bold")));
		if (!tag.Attributes.ContainsKey("lineheight") || !double.TryParse(tag.Attributes["lineheight"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result4))
		{
			result4 = cairoFont.LineHeightMultiplier;
		}
		if (tag.Attributes.ContainsKey("align"))
		{
			switch (tag.Attributes["align"])
			{
			case "left":
				orientation = EnumTextOrientation.Left;
				break;
			case "right":
				orientation = EnumTextOrientation.Right;
				break;
			case "center":
				orientation = EnumTextOrientation.Center;
				break;
			case "justify":
				orientation = EnumTextOrientation.Justify;
				break;
			}
		}
		else
		{
			orientation = cairoFont.Orientation;
		}
		fontStack.Push(cairoFont);
		return new CairoFont(result, text, color).WithWeight(val).WithLineHeightMultiplier(result4).WithOrientation(orientation);
	}

	public static bool parseHexColor(string colorText, out double[] color)
	{
		Color color2;
		try
		{
			color2 = ColorTranslator.FromHtml(colorText);
		}
		catch (Exception)
		{
			color = new double[4] { 0.0, 0.0, 0.0, 1.0 };
			return false;
		}
		if (color2 == Color.Empty)
		{
			color = null;
			return false;
		}
		color = new double[4]
		{
			(double)(int)color2.R / 255.0,
			(double)(int)color2.G / 255.0,
			(double)(int)color2.B / 255.0,
			(double)(int)color2.A / 255.0
		};
		return true;
	}

	public static string toHexColor(double[] color)
	{
		return "#" + ((int)(color[0] * 255.0)).ToString("X2") + ((int)(color[1] * 255.0)).ToString("X2") + ((int)(color[2] * 255.0)).ToString("X2");
	}
}
