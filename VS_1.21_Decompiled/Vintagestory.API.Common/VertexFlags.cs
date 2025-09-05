using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class VertexFlags
{
	public const int GlowLevelBitMask = 255;

	public const int ZOffsetBitPos = 8;

	public const int ZOffsetBitMask = 1792;

	public const int ReflectiveBitMask = 2048;

	public const int Lod0BitMask = 4096;

	public const int NormalBitPos = 13;

	public const int NormalBitMask = 33546240;

	public const int WindModeBitsMask = 503316480;

	public const int WindModeBitsPos = 25;

	public const int WindDataBitsMask = -536870912;

	public const int WindDataBitsPos = 29;

	public const int WindBitsMask = -33554432;

	public const int LiquidIsLavaBitMask = 134217728;

	public const int LiquidWeakFoamBitMask = 268435456;

	public const int LiquidWeakWaveBitMask = 536870912;

	public const int LiquidFullAlphaBitMask = 1073741824;

	public const int LiquidExposedToSkyBitMask = int.MinValue;

	public const int ClearWindBitsMask = 33554431;

	public const int ClearWindModeBitsMask = -503316481;

	public const int ClearWindDataBitsMask = 536870911;

	public const int ClearZOffsetMask = -1793;

	public const int ClearNormalBitMask = -33546241;

	private int all;

	private byte glowLevel;

	private byte zOffset;

	private bool reflective;

	private bool lod0;

	private short normal;

	private EnumWindBitMode windMode;

	private byte windData;

	private const int nValueBitMask = 14;

	private const int nXValueBitMask = 114688;

	private const int nYValueBitMask = 1835008;

	private const int nZValueBitMask = 29360128;

	private const int nXSignBitPos = 12;

	private const int nYSignBitPos = 16;

	private const int nZSignBitPos = 20;

	[JsonProperty]
	public int All
	{
		get
		{
			return all;
		}
		set
		{
			glowLevel = (byte)(value & 0xFF);
			zOffset = (byte)((value >> 8) & 7);
			reflective = ((value >> 11) & 1) != 0;
			lod0 = ((value >> 12) & 1) != 0;
			normal = (short)((value >> 13) & 0xFFF);
			windMode = (EnumWindBitMode)((value >> 25) & 0xF);
			windData = (byte)((value >> 29) & 7);
			all = value;
		}
	}

	[JsonProperty]
	public byte GlowLevel
	{
		get
		{
			return glowLevel;
		}
		set
		{
			glowLevel = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public byte ZOffset
	{
		get
		{
			return zOffset;
		}
		set
		{
			zOffset = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public bool Reflective
	{
		get
		{
			return reflective;
		}
		set
		{
			reflective = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public bool Lod0
	{
		get
		{
			return lod0;
		}
		set
		{
			lod0 = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public short Normal
	{
		get
		{
			return normal;
		}
		set
		{
			normal = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public EnumWindBitMode WindMode
	{
		get
		{
			return windMode;
		}
		set
		{
			windMode = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public byte WindData
	{
		get
		{
			return windData;
		}
		set
		{
			windData = value;
			UpdateAll();
		}
	}

	public static int PackNormal(Vec3d normal)
	{
		return PackNormal(normal.X, normal.Y, normal.Z);
	}

	public static int PackNormal(double x, double y, double z)
	{
		int num = (int)(x * 7.000001) * 2;
		int num2 = (int)(y * 7.000001) * 2;
		int num3 = (int)(z * 7.000001) * 2;
		return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
	}

	public static int PackNormal(Vec3f normal)
	{
		int num = (int)(normal.X * 7.000001f) * 2;
		int num2 = (int)(normal.Y * 7.000001f) * 2;
		int num3 = (int)(normal.Z * 7.000001f) * 2;
		return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
	}

	public static int PackNormal(Vec3i normal)
	{
		int num = (int)((float)normal.X * 7.000001f) * 2;
		int num2 = (int)((float)normal.Y * 7.000001f) * 2;
		int num3 = (int)((float)normal.Z * 7.000001f) * 2;
		return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
	}

	public static void UnpackNormal(int vertexFlags, float[] intoFloats)
	{
		int num = vertexFlags & 0x1C000;
		int num2 = vertexFlags & 0x1C0000;
		int num3 = vertexFlags & 0x1C00000;
		int num4 = 1 - ((vertexFlags >> 12) & 2);
		int num5 = 1 - ((vertexFlags >> 16) & 2);
		int num6 = 1 - ((vertexFlags >> 20) & 2);
		intoFloats[0] = (float)(num4 * num) / 114688f;
		intoFloats[1] = (float)(num5 * num2) / 1835008f;
		intoFloats[2] = (float)(num6 * num3) / 29360128f;
	}

	public static void UnpackNormal(int vertexFlags, double[] intoDouble)
	{
		int num = vertexFlags & 0x1C000;
		int num2 = vertexFlags & 0x1C0000;
		int num3 = vertexFlags & 0x1C00000;
		int num4 = 1 - ((vertexFlags >> 12) & 2);
		int num5 = 1 - ((vertexFlags >> 16) & 2);
		int num6 = 1 - ((vertexFlags >> 20) & 2);
		intoDouble[0] = (float)(num4 * num) / 114688f;
		intoDouble[1] = (float)(num5 * num2) / 1835008f;
		intoDouble[2] = (float)(num6 * num3) / 29360128f;
	}

	public VertexFlags()
	{
	}

	public VertexFlags(int flags)
	{
		All = flags;
	}

	private void UpdateAll()
	{
		all = (int)((uint)(glowLevel | ((zOffset & 7) << 8)) | ((reflective ? 1u : 0u) << 11) | ((Lod0 ? 1u : 0u) << 12) | (uint)((normal & 0xFFF) << 13) | (uint)((int)(windMode & (EnumWindBitMode)15) << 25)) | ((windData & 7) << 29);
	}

	public VertexFlags Clone()
	{
		return new VertexFlags(All);
	}

	public override string ToString()
	{
		return $"Glow: {glowLevel}, ZOffset: {ZOffset}, Reflective: {reflective}, Lod0: {lod0}, Normal: {normal}, WindMode: {WindMode}, WindData: {windData}";
	}

	public static void SetWindMode(ref int flags, int windMode)
	{
		flags |= windMode << 25;
	}

	public static void SetWindData(ref int flags, int windData)
	{
		flags |= windData << 29;
	}

	public static void ReplaceWindData(ref int flags, int windData)
	{
		flags = (flags & 0x1FFFFFFF) | (windData << 29);
	}
}
