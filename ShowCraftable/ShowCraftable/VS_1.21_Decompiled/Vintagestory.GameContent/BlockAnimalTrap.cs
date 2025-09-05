using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockAnimalTrap : Block
{
	protected float rotInterval = (float)Math.PI / 8f;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CanStep = false;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num)
		{
			BlockEntityAnimalTrap blockEntity = GetBlockEntity<BlockEntityAnimalTrap>(blockSel.Position);
			if (blockEntity != null)
			{
				BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
				double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
				float num2 = (float)(int)Math.Round((float)Math.Atan2(y, x) / rotInterval) * rotInterval;
				blockEntity.RotationYDeg = num2 * (180f / (float)Math.PI);
				blockEntity.MarkDirty(redrawOnClient: true);
			}
		}
		return num;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return GetBlockEntity<BlockEntityAnimalTrap>(blockSel.Position)?.Interact(byPlayer, blockSel) ?? base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		BlockEntityAnimalTrap blockEntity = GetBlockEntity<BlockEntityAnimalTrap>(pos);
		if (blockEntity != null && blockEntity.TrapState == EnumTrapState.Trapped)
		{
			return Array.Empty<ItemStack>();
		}
		if (blockEntity != null && blockEntity.TrapState == EnumTrapState.Destroyed)
		{
			BlockDropItemStack[] array = Attributes?["destroyedDrops"]?.AsObject<BlockDropItemStack[]>();
			if (array == null)
			{
				return Array.Empty<ItemStack>();
			}
			List<ItemStack> list = new List<ItemStack>();
			foreach (BlockDropItemStack obj in array)
			{
				obj.Resolve(world, "Block ", Code);
				ItemStack itemStack = obj.ToRandomItemstackForPlayer(byPlayer, world, dropQuantityMultiplier);
				if (itemStack != null)
				{
					list.Add(itemStack);
				}
				if (obj.LastDrop)
				{
					break;
				}
			}
			return list.ToArray();
		}
		return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityAnimalTrap blockEntity = GetBlockEntity<BlockEntityAnimalTrap>(pos);
		if (blockEntity != null)
		{
			blockModelData = blockEntity.GetCurrentMesh(null).Clone().Rotate(Vec3f.Half, 0f, (blockEntity.RotationYDeg - 90f) * ((float)Math.PI / 180f), 0f);
			decalModelData = blockEntity.GetCurrentMesh(decalTexSource).Clone().Rotate(Vec3f.Half, 0f, (blockEntity.RotationYDeg - 90f) * ((float)Math.PI / 180f), 0f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public bool IsAppetizingBait(ICoreAPI api, ItemStack baitStack)
	{
		CollectibleObject collectible = baitStack.Collectible;
		if (collectible.NutritionProps == null)
		{
			JsonObject attributes = collectible.Attributes;
			if (attributes == null || !attributes["foodTags"].Exists)
			{
				return false;
			}
		}
		return api.World.EntityTypes.Any(delegate(EntityProperties type)
		{
			JsonObject attributes2 = type.Attributes;
			return attributes2 != null && attributes2["creatureDiet"].AsObject<CreatureDiet>()?.Matches(baitStack, checkCategory: true, 0.5f) == true;
		});
	}

	public bool CanFitBait(ICoreAPI api, ItemStack baitStack)
	{
		CollectibleObject collobj = baitStack.Collectible;
		JsonObject attributes = Attributes;
		if (attributes == null)
		{
			return true;
		}
		return attributes["excludeFoodTags"].AsArray<string>()?.Any(delegate(string tag)
		{
			JsonObject attributes2 = collobj.Attributes;
			return attributes2 != null && attributes2["foodTags"].AsArray<string>()?.Contains(tag) == true;
		}) != true;
	}
}
