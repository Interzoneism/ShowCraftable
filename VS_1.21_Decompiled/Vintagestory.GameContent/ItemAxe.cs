using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemAxe : Item
{
	private const int LeafGroups = 7;

	private static SimpleParticleProperties dustParticles;

	static ItemAxe()
	{
		dustParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(100, 200, 200, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.1f,
			WindAffected = true
		};
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.AddPos.Set(1.0, 1.0, 1.0);
		dustParticles.MinQuantity = 2f;
		dustParticles.AddQuantity = 12f;
		dustParticles.LifeLength = 4f;
		dustParticles.MinSize = 0.2f;
		dustParticles.MaxSize = 0.5f;
		dustParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
		dustParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
		dustParticles.DieOnRainHeightmap = false;
		dustParticles.WindAffectednes = 0.5f;
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if ((byEntity as EntityPlayer)?.EntitySelection != null)
		{
			return "axehit";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
	}

	public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return base.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel);
	}

	public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		ITreeAttribute tempAttributes = itemslot.Itemstack.TempAttributes;
		int num = tempAttributes.GetInt("lastposX", -1);
		int num2 = tempAttributes.GetInt("lastposY", -1);
		int num3 = tempAttributes.GetInt("lastposZ", -1);
		BlockPos position = blockSel.Position;
		float num4;
		if (position.X != num || position.Y != num2 || position.Z != num3 || counter % 30 == 0)
		{
			FindTree(player.Entity.World, position, out var resistance, out var woodTier);
			if (ToolTier < woodTier - 3)
			{
				return remainingResistance;
			}
			num4 = (float)Math.Max(1.0, Math.Sqrt((double)resistance / 1.45));
			tempAttributes.SetFloat("treeResistance", num4);
		}
		else
		{
			num4 = tempAttributes.GetFloat("treeResistance", 1f);
		}
		tempAttributes.SetInt("lastposX", position.X);
		tempAttributes.SetInt("lastposY", position.Y);
		tempAttributes.SetInt("lastposZ", position.Z);
		return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt / num4, counter);
	}

	public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
	{
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		double num = api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0.0;
		int resistance;
		int woodTier;
		Stack<BlockPos> stack = FindTree(world, blockSel.Position, out resistance, out woodTier);
		if (stack.Count == 0)
		{
			return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
		}
		bool flag = DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);
		float num2 = 1f;
		float num3 = 0.8f;
		int num4 = 0;
		bool flag2 = true;
		while (stack.Count > 0)
		{
			BlockPos blockPos = stack.Pop();
			Block block = world.BlockAccessor.GetBlock(blockPos);
			bool flag3 = block.BlockMaterial == EnumBlockMaterial.Wood;
			if (flag3 && !flag2)
			{
				continue;
			}
			num4++;
			bool flag4 = block.Code.Path.Contains("branchy");
			bool flag5 = block.BlockMaterial == EnumBlockMaterial.Leaves;
			world.BlockAccessor.BreakBlock(blockPos, player, flag5 ? num2 : (flag4 ? num3 : 1f));
			if (world.Side == EnumAppSide.Client)
			{
				dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, blockPos, BlockFacing.UP);
				dustParticles.Color |= -16777216;
				dustParticles.MinPos.Set(blockPos.X, blockPos.Y, blockPos.Z);
				if (block.BlockMaterial == EnumBlockMaterial.Leaves)
				{
					dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
					dustParticles.ParticleModel = EnumParticleModel.Quad;
					dustParticles.MinVelocity.Set(-0.4f + 4f * (float)num, -0.4f, -0.4f);
					dustParticles.AddVelocity.Set(0.8f + 4f * (float)num, 1.2f, 0.8f);
				}
				else
				{
					dustParticles.GravityEffect = 0.8f;
					dustParticles.ParticleModel = EnumParticleModel.Cube;
					dustParticles.MinVelocity.Set(-0.4f + (float)num, -0.4f, -0.4f);
					dustParticles.AddVelocity.Set(0.8f + (float)num, 1.2f, 0.8f);
				}
				world.SpawnParticles(dustParticles);
			}
			if (flag && flag3)
			{
				DamageItem(world, byEntity, itemslot);
				if (itemslot.Itemstack == null)
				{
					flag2 = false;
				}
			}
			if (flag5 && num2 > 0.03f)
			{
				num2 *= 0.85f;
			}
			if (flag4 && num3 > 0.015f)
			{
				num3 *= 0.7f;
			}
		}
		if (num4 > 35 && flag2)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), blockSel.Position, -0.25, player, randomizePitch: false, 32f, GameMath.Clamp((float)num4 / 100f, 0.25f, 1f));
		}
		return true;
	}

	public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos, out int resistance, out int woodTier)
	{
		Queue<Vec4i> queue = new Queue<Vec4i>();
		Queue<Vec4i> queue2 = new Queue<Vec4i>();
		HashSet<BlockPos> hashSet = new HashSet<BlockPos>();
		Stack<BlockPos> stack = new Stack<BlockPos>();
		resistance = 0;
		woodTier = 0;
		Block block = world.BlockAccessor.GetBlock(startPos);
		if (block.Code == null)
		{
			return stack;
		}
		string text = block.Attributes?["treeFellingGroupCode"].AsString();
		int num = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["treeFellingCanChop"].AsBool(defaultValue: true))
		{
			return stack;
		}
		EnumTreeFellingBehavior enumTreeFellingBehavior = EnumTreeFellingBehavior.Chop;
		if (block is ICustomTreeFellingBehavior customTreeFellingBehavior)
		{
			enumTreeFellingBehavior = customTreeFellingBehavior.GetTreeFellingBehavior(startPos, null, num);
			if (enumTreeFellingBehavior == EnumTreeFellingBehavior.NoChop)
			{
				resistance = stack.Count;
				return stack;
			}
		}
		if (num < 2)
		{
			return stack;
		}
		if (text == null)
		{
			return stack;
		}
		queue.Enqueue(new Vec4i(startPos, num));
		hashSet.Add(startPos);
		int[] array = new int[7];
		while (queue.Count > 0)
		{
			Vec4i vec4i = queue.Dequeue();
			stack.Push(new BlockPos(vec4i.X, vec4i.Y, vec4i.Z));
			resistance += vec4i.W + 1;
			if (woodTier == 0)
			{
				woodTier = vec4i.W;
			}
			if (stack.Count > 2500)
			{
				break;
			}
			block = world.BlockAccessor.GetBlockRaw(vec4i.X, vec4i.Y, vec4i.Z, 1);
			if (block is ICustomTreeFellingBehavior customTreeFellingBehavior2)
			{
				enumTreeFellingBehavior = customTreeFellingBehavior2.GetTreeFellingBehavior(startPos, null, num);
			}
			if (enumTreeFellingBehavior != EnumTreeFellingBehavior.NoChop)
			{
				onTreeBlock(vec4i, world.BlockAccessor, hashSet, startPos, enumTreeFellingBehavior == EnumTreeFellingBehavior.ChopSpreadVertical, text, queue, queue2, array);
			}
		}
		int num2 = 0;
		int num3 = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] > num2)
			{
				num2 = array[i];
				num3 = i;
			}
		}
		if (num3 >= 0)
		{
			text = num3 + 1 + text;
		}
		while (queue2.Count > 0)
		{
			Vec4i vec4i2 = queue2.Dequeue();
			stack.Push(new BlockPos(vec4i2.X, vec4i2.Y, vec4i2.Z));
			resistance += vec4i2.W + 1;
			if (stack.Count > 2500)
			{
				break;
			}
			onTreeBlock(vec4i2, world.BlockAccessor, hashSet, startPos, enumTreeFellingBehavior == EnumTreeFellingBehavior.ChopSpreadVertical, text, queue2, null, null);
		}
		return stack;
	}

	private void onTreeBlock(Vec4i pos, IBlockAccessor blockAccessor, HashSet<BlockPos> checkedPositions, BlockPos startPos, bool chopSpreadVertical, string treeFellingGroupCode, Queue<Vec4i> queue, Queue<Vec4i> leafqueue, int[] adjacentLeaves)
	{
		for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
		{
			Vec3i vec3i = Vec3i.DirectAndIndirectNeighbours[i];
			BlockPos blockPos = new BlockPos(pos.X + vec3i.X, pos.Y + vec3i.Y, pos.Z + vec3i.Z);
			float num = GameMath.Sqrt(blockPos.HorDistanceSqTo(startPos.X, startPos.Z));
			float num2 = blockPos.Y - startPos.Y;
			float num3 = (chopSpreadVertical ? 0.5f : 2f);
			if (num - 1f >= num3 * num2 || checkedPositions.Contains(blockPos))
			{
				continue;
			}
			Block block = blockAccessor.GetBlock(blockPos, 1);
			if (block.Code == null || block.Id == 0)
			{
				continue;
			}
			string text = block.Attributes?["treeFellingGroupCode"].AsString();
			Queue<Vec4i> queue2;
			if (text != treeFellingGroupCode)
			{
				if (text == null || leafqueue == null || block.BlockMaterial != EnumBlockMaterial.Leaves || text.Length != treeFellingGroupCode.Length + 1 || !text.EndsWithOrdinal(treeFellingGroupCode))
				{
					continue;
				}
				queue2 = leafqueue;
				int num4 = GameMath.Clamp(text[0] - 48, 1, 7);
				adjacentLeaves[num4 - 1]++;
			}
			else
			{
				queue2 = queue;
			}
			int num5 = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
			if (pos.W >= num5)
			{
				checkedPositions.Add(blockPos);
				if (!chopSpreadVertical || vec3i.Equals(0, 1, 0) || num5 <= 0)
				{
					queue2.Enqueue(new Vec4i(blockPos, num5));
				}
			}
		}
	}
}
