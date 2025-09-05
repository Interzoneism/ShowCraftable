using System;

namespace Vintagestory.API.MathTools;

public static class ColorBlend
{
	public delegate int ColorBlendDelegate(int col1, int col2);

	private static ColorBlendDelegate[] Blenders;

	private static readonly uint[] masTable;

	static ColorBlend()
	{
		masTable = new uint[768]
		{
			0u, 0u, 0u, 1u, 0u, 0u, 1u, 0u, 1u, 2863311531u,
			0u, 33u, 1u, 0u, 2u, 3435973837u, 0u, 34u, 2863311531u, 0u,
			34u, 1227133513u, 1227133513u, 33u, 1u, 0u, 3u, 954437177u, 0u, 33u,
			3435973837u, 0u, 35u, 3123612579u, 0u, 35u, 2863311531u, 0u, 35u, 1321528399u,
			0u, 34u, 1227133513u, 1227133513u, 34u, 2290649225u, 0u, 35u, 1u, 0u,
			4u, 4042322161u, 0u, 36u, 954437177u, 0u, 34u, 3616814565u, 3616814565u, 36u,
			3435973837u, 0u, 36u, 3272356035u, 3272356035u, 36u, 3123612579u, 0u, 36u, 2987803337u,
			0u, 36u, 2863311531u, 0u, 36u, 1374389535u, 0u, 35u, 1321528399u, 0u,
			35u, 2545165805u, 2545165805u, 36u, 1227133513u, 1227133513u, 35u, 2369637129u, 0u, 36u,
			2290649225u, 0u, 36u, 1108378657u, 1108378657u, 35u, 1u, 0u, 5u, 1041204193u,
			0u, 35u, 4042322161u, 0u, 37u, 1963413621u, 1963413621u, 36u, 954437177u, 0u,
			35u, 1857283155u, 1857283155u, 36u, 3616814565u, 3616814565u, 37u, 1762037865u, 1762037865u, 36u,
			3435973837u, 0u, 37u, 3352169597u, 0u, 37u, 3272356035u, 3272356035u, 37u, 799063683u,
			0u, 35u, 3123612579u, 0u, 37u, 1527099483u, 1527099483u, 36u, 2987803337u, 0u,
			37u, 2924233053u, 0u, 37u, 2863311531u, 0u, 37u, 1402438301u, 0u, 36u,
			1374389535u, 0u, 36u, 2694881441u, 0u, 37u, 1321528399u, 0u, 36u, 2593187801u,
			2593187801u, 37u, 2545165805u, 2545165805u, 37u, 2498890063u, 2498890063u, 37u, 1227133513u, 1227133513u,
			36u, 1205604855u, 1205604855u, 36u, 2369637129u, 0u, 37u, 582368447u, 0u, 35u,
			2290649225u, 0u, 37u, 1126548799u, 0u, 36u, 1108378657u, 1108378657u, 36u, 1090785345u,
			1090785345u, 36u, 1u, 0u, 6u, 4228890877u, 0u, 38u, 1041204193u, 0u,
			36u, 128207979u, 0u, 33u, 4042322161u, 0u, 38u, 1991868891u, 0u, 37u,
			1963413621u, 1963413621u, 37u, 3871519817u, 0u, 38u, 954437177u, 0u, 36u, 941362695u,
			941362695u, 36u, 1857283155u, 1857283155u, 37u, 458129845u, 0u, 35u, 3616814565u, 3616814565u,
			38u, 892460737u, 0u, 36u, 1762037865u, 1762037865u, 37u, 3479467177u, 0u, 38u,
			3435973837u, 0u, 38u, 3393554407u, 0u, 38u, 3352169597u, 0u, 38u, 827945503u,
			0u, 36u, 3272356035u, 3272356035u, 38u, 3233857729u, 0u, 38u, 799063683u, 0u,
			36u, 789879043u, 0u, 36u, 3123612579u, 0u, 38u, 3088515809u, 0u, 38u,
			1527099483u, 1527099483u, 37u, 755159085u, 755159085u, 36u, 2987803337u, 0u, 38u, 2955676419u,
			0u, 38u, 2924233053u, 0u, 38u, 723362913u, 723362913u, 36u, 2863311531u, 0u,
			38u, 2833792855u, 2833792855u, 38u, 1402438301u, 0u, 37u, 2776544515u, 0u, 38u,
			1374389535u, 0u, 37u, 2721563435u, 2721563435u, 38u, 2694881441u, 0u, 38u, 2668717543u,
			2668717543u, 38u, 1321528399u, 0u, 37u, 654471207u, 654471207u, 36u, 2593187801u, 2593187801u,
			38u, 2568952401u, 2568952401u, 38u, 2545165805u, 2545165805u, 38u, 630453915u, 630453915u, 36u,
			2498890063u, 2498890063u, 38u, 619094385u, 619094385u, 36u, 1227133513u, 1227133513u, 37u, 2432547849u,
			2432547849u, 38u, 1205604855u, 1205604855u, 37u, 2390242669u, 2390242669u, 38u, 2369637129u, 0u,
			38u, 587345955u, 587345955u, 36u, 582368447u, 0u, 36u, 1154949189u, 0u, 37u,
			2290649225u, 0u, 38u, 2271718239u, 2271718239u, 38u, 1126548799u, 0u, 37u, 2234779731u,
			2234779731u, 38u, 1108378657u, 1108378657u, 37u, 274877907u, 0u, 35u, 1090785345u, 1090785345u,
			37u, 270549121u, 270549121u, 35u, 1u, 0u, 7u, 266354561u, 0u, 35u,
			4228890877u, 0u, 39u, 4196609267u, 0u, 39u, 1041204193u, 0u, 37u, 4133502361u,
			0u, 39u, 128207979u, 0u, 34u, 4072265289u, 0u, 39u, 4042322161u, 0u,
			39u, 125400505u, 0u, 34u, 1991868891u, 0u, 38u, 1977538899u, 0u, 38u,
			1963413621u, 1963413621u, 38u, 974744351u, 0u, 37u, 3871519817u, 0u, 39u, 3844446251u,
			0u, 39u, 954437177u, 0u, 37u, 3791419407u, 0u, 39u, 941362695u, 941362695u,
			37u, 3739835469u, 0u, 39u, 1857283155u, 1857283155u, 38u, 3689636335u, 0u, 39u,
			458129845u, 0u, 36u, 910191745u, 0u, 37u, 3616814565u, 3616814565u, 39u, 3593175255u,
			0u, 39u, 892460737u, 0u, 37u, 3546811703u, 0u, 39u, 1762037865u, 1762037865u,
			38u, 875407347u, 0u, 37u, 3479467177u, 0u, 39u, 3457583735u, 3457583735u, 39u,
			3435973837u, 0u, 39u, 3414632385u, 0u, 39u, 3393554407u, 0u, 39u, 3372735055u,
			0u, 39u, 3352169597u, 0u, 39u, 1665926709u, 0u, 38u, 827945503u, 0u,
			37u, 1645975491u, 0u, 38u, 3272356035u, 3272356035u, 39u, 1626496491u, 0u, 38u,
			3233857729u, 0u, 39u, 401868285u, 401868285u, 36u, 799063683u, 0u, 37u, 3177779271u,
			3177779271u, 39u, 789879043u, 0u, 37u, 1570730897u, 0u, 38u, 3123612579u, 0u,
			39u, 1552982525u, 1552982525u, 38u, 3088515809u, 0u, 39u, 1535630765u, 1535630765u, 38u,
			1527099483u, 1527099483u, 38u, 3037324939u, 0u, 39u, 755159085u, 755159085u, 37u, 3004130131u,
			0u, 39u, 2987803337u, 0u, 39u, 371456631u, 371456631u, 36u, 2955676419u, 0u,
			39u, 2939870663u, 0u, 39u, 2924233053u, 0u, 39u, 363595115u, 363595115u, 36u,
			723362913u, 723362913u, 37u, 2878302691u, 0u, 39u, 2863311531u, 0u, 39u, 356059465u,
			0u, 36u, 2833792855u, 2833792855u, 39u, 352407573u, 352407573u, 36u, 1402438301u, 0u,
			38u, 2790638649u, 2790638649u, 39u, 2776544515u, 0u, 39u, 1381296015u, 0u, 38u,
			1374389535u, 0u, 38u, 42735993u, 0u, 33u, 2721563435u, 2721563435u, 39u, 2708156719u,
			0u, 39u, 2694881441u, 0u, 39u, 1340867839u, 0u, 38u, 2668717543u, 2668717543u,
			39u, 663956297u, 0u, 37u, 1321528399u, 0u, 38u, 2630410593u, 0u, 39u,
			654471207u, 654471207u, 37u, 2605477791u, 0u, 39u, 2593187801u, 2593187801u, 39u, 2581013211u,
			0u, 39u, 2568952401u, 2568952401u, 39u, 1278501893u, 0u, 38u, 2545165805u, 2545165805u,
			39u, 1266718465u, 1266718465u, 38u, 630453915u, 630453915u, 37u, 313787565u, 313787565u, 36u,
			2498890063u, 2498890063u, 39u, 621895717u, 621895717u, 37u, 619094385u, 619094385u, 37u, 616318177u,
			616318177u, 37u, 1227133513u, 1227133513u, 38u, 2443359173u, 0u, 39u, 2432547849u, 2432547849u,
			39u, 2421831779u, 2421831779u, 39u, 1205604855u, 1205604855u, 38u, 1200340205u, 0u, 38u,
			2390242669u, 2390242669u, 39u, 1189947649u, 1189947649u, 38u, 2369637129u, 0u, 39u, 589866753u,
			589866753u, 37u, 587345955u, 587345955u, 37u, 1169693221u, 1169693221u, 38u, 582368447u, 0u,
			37u, 144977799u, 144977799u, 35u, 1154949189u, 0u, 38u, 2300233531u, 0u, 39u,
			2290649225u, 0u, 39u, 285143057u, 0u, 36u, 2271718239u, 2271718239u, 39u, 2262369605u,
			0u, 39u, 1126548799u, 0u, 38u, 2243901281u, 2243901281u, 39u, 2234779731u, 2234779731u,
			39u, 278216505u, 278216505u, 36u, 1108378657u, 1108378657u, 38u, 1103927337u, 1103927337u, 38u,
			274877907u, 0u, 36u, 2190262207u, 0u, 39u, 1090785345u, 1090785345u, 38u, 2172947881u,
			0u, 39u, 270549121u, 270549121u, 36u, 2155905153u, 0u, 39u
		};
		Blenders = new ColorBlendDelegate[9] { Normal, Darken, Lighten, Multiply, Screen, ColorDodge, ColorBurn, Overlay, OverlayCutout };
	}

	public static int Blend(EnumColorBlendMode blendMode, int colorBase, int colorOver)
	{
		return Blenders[(int)blendMode](colorBase, colorOver);
	}

	public static int Normal(int rgb1, int rgb2)
	{
		return ColorUtil.ColorOver(rgb2, rgb1);
	}

	public static int Overlay(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		vSColor2.Rn *= vSColor2.An;
		vSColor2.Gn *= vSColor2.An;
		vSColor2.Bn *= vSColor2.An;
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3;
		if (vSColor.B < 128)
		{
			num3 = 2 * vSColor.B * vSColor2.B + 128;
			num3 = (num3 >> 8) + num3 >> 8;
		}
		else
		{
			num3 = 2 * (255 - vSColor.B) * (255 - vSColor2.B) + 128;
			num3 = (num3 >> 8) + num3 >> 8;
			num3 = 255 - num3;
		}
		int num4;
		if (vSColor.G < 128)
		{
			num4 = 2 * vSColor.G * vSColor2.G + 128;
			num4 = (num4 >> 8) + num4 >> 8;
		}
		else
		{
			num4 = 2 * (255 - vSColor.G) * (255 - vSColor2.G) + 128;
			num4 = (num4 >> 8) + num4 >> 8;
			num4 = 255 - num4;
		}
		int num5;
		if (vSColor.R < 128)
		{
			num5 = 2 * vSColor.R * vSColor2.R + 128;
			num5 = (num5 >> 8) + num5 >> 8;
		}
		else
		{
			num5 = 2 * (255 - vSColor.R) * (255 - vSColor2.R) + 128;
			num5 = (num5 >> 8) + num5 >> 8;
			num5 = 255 - num5;
		}
		int num6 = a * a2 + 128;
		num6 = (num6 >> 8) + num6 >> 8;
		int num7 = a2 - num6;
		int num8 = num2 * 3;
		uint num9 = masTable[num8];
		uint num10 = masTable[num8 + 1];
		uint num11 = masTable[num8 + 2];
		uint num12 = (uint)((vSColor.B * num + vSColor2.B * num7 + num3 * num6) * num9 + num10 >> (int)num11);
		uint num13 = (uint)((vSColor.G * num + vSColor2.G * num7 + num4 * num6) * num9 + num10 >> (int)num11);
		int num14 = (int)((vSColor.R * num + vSColor2.R * num7 + num5 * num6) * num9 + num10 >> (int)num11);
		int num15 = a * (255 - a2) + 128;
		num15 = (num15 >> 8) + num15 >> 8;
		num15 += a2;
		return num14 + (int)(num13 << 8) + (int)(num12 << 16) + (num15 << 24);
	}

	public static int Darken(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3 = Math.Min(vSColor.B, vSColor2.B);
		int num4 = Math.Min(vSColor.G, vSColor2.G);
		int num5 = Math.Min(vSColor.R, vSColor2.R);
		int num6 = a * a2 + 128;
		num6 = (num6 >> 8) + num6 >> 8;
		int num7 = a2 - num6;
		int num8 = num2 * 3;
		uint num9 = masTable[num8];
		uint num10 = masTable[num8 + 1];
		uint num11 = masTable[num8 + 2];
		uint num12 = (uint)((vSColor.B * num + vSColor2.B * num7 + num3 * num6) * num9 + num10 >> (int)num11);
		uint num13 = (uint)((vSColor.G * num + vSColor2.G * num7 + num4 * num6) * num9 + num10 >> (int)num11);
		int num14 = (int)((vSColor.R * num + vSColor2.R * num7 + num5 * num6) * num9 + num10 >> (int)num11);
		int num15 = a * (255 - a2) + 128;
		num15 = (num15 >> 8) + num15 >> 8;
		num15 += a2;
		return num14 + (int)(num13 << 8) + (int)(num12 << 16) + (num15 << 24);
	}

	public static int Lighten(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3 = Math.Max(vSColor.B, vSColor2.B);
		int num4 = Math.Max(vSColor.G, vSColor2.G);
		int num5 = Math.Max(vSColor.R, vSColor2.R);
		int num6 = a * a2 + 128;
		num6 = (num6 >> 8) + num6 >> 8;
		int num7 = a2 - num6;
		int num8 = num2 * 3;
		uint num9 = masTable[num8];
		uint num10 = masTable[num8 + 1];
		uint num11 = masTable[num8 + 2];
		uint num12 = (uint)((vSColor.B * num + vSColor2.B * num7 + num3 * num6) * num9 + num10 >> (int)num11);
		uint num13 = (uint)((vSColor.G * num + vSColor2.G * num7 + num4 * num6) * num9 + num10 >> (int)num11);
		int num14 = (int)((vSColor.R * num + vSColor2.R * num7 + num5 * num6) * num9 + num10 >> (int)num11);
		int num15 = a * (255 - a2) + 128;
		num15 = (num15 >> 8) + num15 >> 8;
		num15 += a2;
		return num14 + (int)(num13 << 8) + (int)(num12 << 16) + (num15 << 24);
	}

	public static int Multiply(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3 = vSColor.B * vSColor2.B + 128;
		num3 = (num3 >> 8) + num3 >> 8;
		int num4 = vSColor.G * vSColor2.G + 128;
		num4 = (num4 >> 8) + num4 >> 8;
		int num5 = vSColor.R * vSColor2.R + 128;
		num5 = (num5 >> 8) + num5 >> 8;
		int num6 = a * a2 + 128;
		num6 = (num6 >> 8) + num6 >> 8;
		int num7 = a2 - num6;
		int num8 = num2 * 3;
		uint num9 = masTable[num8];
		uint num10 = masTable[num8 + 1];
		uint num11 = masTable[num8 + 2];
		uint num12 = (uint)((vSColor.B * num + vSColor2.B * num7 + num3 * num6) * num9 + num10 >> (int)num11);
		uint num13 = (uint)((vSColor.G * num + vSColor2.G * num7 + num4 * num6) * num9 + num10 >> (int)num11);
		int num14 = (int)((vSColor.R * num + vSColor2.R * num7 + num5 * num6) * num9 + num10 >> (int)num11);
		int num15 = a * (255 - a2) + 128;
		num15 = (num15 >> 8) + num15 >> 8;
		num15 += a2;
		return num14 + (int)(num13 << 8) + (int)(num12 << 16) + (num15 << 24);
	}

	public static int Screen(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3 = vSColor2.B * vSColor.B + 128;
		num3 = (num3 >> 8) + num3 >> 8;
		num3 = vSColor2.B + vSColor.B - num3;
		int num4 = vSColor2.G * vSColor.G + 128;
		num4 = (num4 >> 8) + num4 >> 8;
		num4 = vSColor2.G + vSColor.G - num4;
		int num5 = vSColor2.R * vSColor.R + 128;
		num5 = (num5 >> 8) + num5 >> 8;
		num5 = vSColor2.R + vSColor.R - num5;
		int num6 = a * a2 + 128;
		num6 = (num6 >> 8) + num6 >> 8;
		int num7 = a2 - num6;
		int num8 = num2 * 3;
		uint num9 = masTable[num8];
		uint num10 = masTable[num8 + 1];
		uint num11 = masTable[num8 + 2];
		uint num12 = (uint)((vSColor.B * num + vSColor2.B * num7 + num3 * num6) * num9 + num10 >> (int)num11);
		uint num13 = (uint)((vSColor.G * num + vSColor2.G * num7 + num4 * num6) * num9 + num10 >> (int)num11);
		int num14 = (int)((vSColor.R * num + vSColor2.R * num7 + num5 * num6) * num9 + num10 >> (int)num11);
		int num15 = a * (255 - a2) + 128;
		num15 = (num15 >> 8) + num15 >> 8;
		num15 += a2;
		return num14 + (int)(num13 << 8) + (int)(num12 << 16) + (num15 << 24);
	}

	public static int ColorDodge(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3;
		if (vSColor2.B == byte.MaxValue)
		{
			num3 = 255;
		}
		else
		{
			int num4 = (255 - vSColor2.B) * 3;
			uint num5 = masTable[num4];
			uint num6 = masTable[num4 + 1];
			uint num7 = masTable[num4 + 2];
			num3 = (int)(vSColor.B * 255 * num5 + num6 >> (int)num7);
			num3 = Math.Min(255, num3);
		}
		int num8;
		if (vSColor2.G == byte.MaxValue)
		{
			num8 = 255;
		}
		else
		{
			int num9 = (255 - vSColor2.G) * 3;
			uint num10 = masTable[num9];
			uint num11 = masTable[num9 + 1];
			uint num12 = masTable[num9 + 2];
			num8 = (int)(vSColor.G * 255 * num10 + num11 >> (int)num12);
			num8 = Math.Min(255, num8);
		}
		int num13;
		if (vSColor2.R == byte.MaxValue)
		{
			num13 = 255;
		}
		else
		{
			int num14 = (255 - vSColor2.R) * 3;
			uint num15 = masTable[num14];
			uint num16 = masTable[num14 + 1];
			uint num17 = masTable[num14 + 2];
			num13 = (int)(vSColor.R * 255 * num15 + num16 >> (int)num17);
			num13 = Math.Min(255, num13);
		}
		int num18 = a * a2 + 128;
		num18 = (num18 >> 8) + num18 >> 8;
		int num19 = a2 - num18;
		int num20 = num2 * 3;
		uint num21 = masTable[num20];
		uint num22 = masTable[num20 + 1];
		uint num23 = masTable[num20 + 2];
		uint num24 = (uint)((vSColor.B * num + vSColor2.B * num19 + num3 * num18) * num21 + num22 >> (int)num23);
		uint num25 = (uint)((vSColor.G * num + vSColor2.G * num19 + num8 * num18) * num21 + num22 >> (int)num23);
		int num26 = (int)((vSColor.R * num + vSColor2.R * num19 + num13 * num18) * num21 + num22 >> (int)num23);
		int num27 = a * (255 - a2) + 128;
		num27 = (num27 >> 8) + num27 >> 8;
		num27 += a2;
		return num26 + (int)(num25 << 8) + (int)(num24 << 16) + (num27 << 24);
	}

	public static int ColorBurn(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		VSColor vSColor2 = new VSColor(rgb2);
		int a = vSColor.A;
		int a2 = vSColor2.A;
		int num = a * (255 - a2) + 128;
		num = (num >> 8) + num >> 8;
		int num2 = num + a2;
		if (num2 == 0)
		{
			return 0;
		}
		int num3;
		if (vSColor2.B == 0)
		{
			num3 = 0;
		}
		else
		{
			int num4 = vSColor2.B * 3;
			uint num5 = masTable[num4];
			uint num6 = masTable[num4 + 1];
			uint num7 = masTable[num4 + 2];
			num3 = (int)((255 - vSColor.B) * 255 * num5 + num6 >> (int)num7);
			num3 = 255 - num3;
			num3 = Math.Max(0, num3);
		}
		int num8;
		if (vSColor2.G == 0)
		{
			num8 = 0;
		}
		else
		{
			int num9 = vSColor2.G * 3;
			uint num10 = masTable[num9];
			uint num11 = masTable[num9 + 1];
			uint num12 = masTable[num9 + 2];
			num8 = (int)((255 - vSColor.G) * 255 * num10 + num11 >> (int)num12);
			num8 = 255 - num8;
			num8 = Math.Max(0, num8);
		}
		int num13;
		if (vSColor2.R == 0)
		{
			num13 = 0;
		}
		else
		{
			int num14 = vSColor2.R * 3;
			uint num15 = masTable[num14];
			uint num16 = masTable[num14 + 1];
			uint num17 = masTable[num14 + 2];
			num13 = (int)((255 - vSColor.R) * 255 * num15 + num16 >> (int)num17);
			num13 = 255 - num13;
			num13 = Math.Max(0, num13);
		}
		int num18 = a * a2 + 128;
		num18 = (num18 >> 8) + num18 >> 8;
		int num19 = a2 - num18;
		int num20 = num2 * 3;
		uint num21 = masTable[num20];
		uint num22 = masTable[num20 + 1];
		uint num23 = masTable[num20 + 2];
		uint num24 = (uint)((vSColor.B * num + vSColor2.B * num19 + num3 * num18) * num21 + num22 >> (int)num23);
		uint num25 = (uint)((vSColor.G * num + vSColor2.G * num19 + num8 * num18) * num21 + num22 >> (int)num23);
		int num26 = (int)((vSColor.R * num + vSColor2.R * num19 + num13 * num18) * num21 + num22 >> (int)num23);
		int num27 = a * (255 - a2) + 128;
		num27 = (num27 >> 8) + num27 >> 8;
		num27 += a2;
		return num26 + (int)(num25 << 8) + (int)(num24 << 16) + (num27 << 24);
	}

	public static int OverlayCutout(int rgb1, int rgb2)
	{
		VSColor vSColor = new VSColor(rgb1);
		if (new VSColor(rgb2).A != 0)
		{
			vSColor.A = 0;
		}
		return vSColor.AsInt;
	}
}
