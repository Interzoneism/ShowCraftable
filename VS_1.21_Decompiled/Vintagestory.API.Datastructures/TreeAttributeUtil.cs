using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public static class TreeAttributeUtil
{
	public static Vec3i GetVec3i(this ITreeAttribute tree, string code, Vec3i defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var value) || !(value is IntAttribute intAttribute))
		{
			return defaultValue;
		}
		return new Vec3i(intAttribute.value, tree.GetInt(code + "Y"), tree.GetInt(code + "Z"));
	}

	public static BlockPos GetBlockPos(this ITreeAttribute tree, string code, BlockPos defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var value) || !(value is IntAttribute intAttribute))
		{
			return defaultValue;
		}
		return new BlockPos(intAttribute.value, tree.GetInt(code + "Y"), tree.GetInt(code + "Z"));
	}

	public static void SetVec3i(this ITreeAttribute tree, string code, Vec3i value)
	{
		tree.SetInt(code + "X", value.X);
		tree.SetInt(code + "Y", value.Y);
		tree.SetInt(code + "Z", value.Z);
	}

	public static void SetBlockPos(this ITreeAttribute tree, string code, BlockPos value)
	{
		tree.SetInt(code + "X", value.X);
		tree.SetInt(code + "Y", value.Y);
		tree.SetInt(code + "Z", value.Z);
	}

	public static Vec3i[] GetVec3is(this ITreeAttribute tree, string code, Vec3i[] defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var value) || !(value is IntArrayAttribute intArrayAttribute))
		{
			return defaultValue;
		}
		if (!tree.TryGetAttribute(code + "Y", out var value2) || !(value2 is IntArrayAttribute intArrayAttribute2))
		{
			return defaultValue;
		}
		if (!tree.TryGetAttribute(code + "Z", out var value3) || !(value3 is IntArrayAttribute intArrayAttribute3))
		{
			return defaultValue;
		}
		int[] value4 = intArrayAttribute.value;
		int[] value5 = intArrayAttribute2.value;
		int[] value6 = intArrayAttribute3.value;
		Vec3i[] array = new Vec3i[value4.Length];
		for (int i = 0; i < value4.Length; i++)
		{
			array[i] = new Vec3i(value4[i], value5[i], value6[i]);
		}
		return array;
	}

	public static void SetVec3is(this ITreeAttribute tree, string code, Vec3i[] value)
	{
		int[] array = new int[value.Length];
		int[] array2 = new int[value.Length];
		int[] array3 = new int[value.Length];
		for (int i = 0; i < value.Length; i++)
		{
			Vec3i vec3i = value[i];
			array[i] = vec3i.X;
			array2[i] = vec3i.Y;
			array3[i] = vec3i.Z;
		}
		tree[code + "X"] = new IntArrayAttribute(array);
		tree[code + "Y"] = new IntArrayAttribute(array2);
		tree[code + "Z"] = new IntArrayAttribute(array3);
	}
}
