using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract]
public class SurvivalConfig
{
	public JsonItemStack[] StartStacks = new JsonItemStack[2]
	{
		new JsonItemStack
		{
			Type = EnumItemClass.Item,
			Code = new AssetLocation("bread-spelt-perfect"),
			StackSize = 8
		},
		new JsonItemStack
		{
			Type = EnumItemClass.Block,
			Code = new AssetLocation("torch-up"),
			StackSize = 1
		}
	};

	[ProtoMember(1)]
	public float[] SunLightLevels = new float[32]
	{
		0.015f, 0.176f, 0.206f, 0.236f, 0.266f, 0.296f, 0.326f, 0.356f, 0.386f, 0.416f,
		0.446f, 0.476f, 0.506f, 0.536f, 0.566f, 0.596f, 0.626f, 0.656f, 0.686f, 0.716f,
		0.746f, 0.776f, 0.806f, 0.836f, 0.866f, 0.896f, 0.926f, 0.956f, 0.986f, 1f,
		1f, 1f
	};

	[ProtoMember(2)]
	public float[] BlockLightLevels = new float[32]
	{
		0.016f, 0.146f, 0.247f, 0.33f, 0.401f, 0.463f, 0.519f, 0.569f, 0.615f, 0.656f,
		0.695f, 0.73f, 0.762f, 0.792f, 0.82f, 0.845f, 0.868f, 0.89f, 0.91f, 0.927f,
		0.944f, 0.958f, 0.972f, 0.983f, 0.993f, 1f, 1f, 1f, 1f, 1f,
		1f, 1f
	};

	[ProtoMember(3)]
	public float PerishSpeedModifier = 1f;

	[ProtoMember(4)]
	public float CreatureDamageModifier = 1f;

	[ProtoMember(5)]
	public float ToolDurabilityModifier = 1f;

	[ProtoMember(6)]
	public float ToolMiningSpeedModifier = 1f;

	[ProtoMember(7)]
	public float HungerSpeedModifier = 1f;

	[ProtoMember(8)]
	public float BaseMoveSpeed = 1.5f;

	[ProtoMember(9)]
	public int SunBrightness = 22;

	[ProtoMember(10)]
	public int PolarEquatorDistance = 50000;

	public ItemStack[] ResolvedStartStacks;

	public void ResolveStartItems(IWorldAccessor world)
	{
		if (StartStacks == null)
		{
			ResolvedStartStacks = Array.Empty<ItemStack>();
			return;
		}
		List<ItemStack> list = new List<ItemStack>();
		for (int i = 0; i < StartStacks.Length; i++)
		{
			if (StartStacks[i].Resolve(world, "start item stack"))
			{
				list.Add(StartStacks[i].ResolvedItemstack);
			}
		}
		ResolvedStartStacks = list.ToArray();
	}
}
