using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBeehive : Block
{
	private BlockPos atPos = new BlockPos();

	private Cuboidf[] nocoll = Array.Empty<Cuboidf>();

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		if (world.Side != EnumAppSide.Client)
		{
			EntityProperties entityType = world.GetEntityType(new AssetLocation("beemob"));
			Entity entity = world.ClassRegistry.CreateEntity(entityType);
			if (entity != null)
			{
				entity.ServerPos.X = (float)pos.X + 0.5f;
				entity.ServerPos.Y = (float)pos.Y + 0.5f;
				entity.ServerPos.Z = (float)pos.Z + 0.5f;
				entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "brokenbeehive");
				world.SpawnEntity(entity);
			}
		}
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return nocoll;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRand, BlockPatchAttributes attributes = null)
	{
		for (int i = 2; i < 7; i++)
		{
			atPos.Set(pos.X, pos.Y - i, pos.Z);
			Block block = blockAccessor.GetBlock(atPos);
			EnumBlockMaterial blockMaterial = block.GetBlockMaterial(blockAccessor, atPos);
			if ((blockMaterial != EnumBlockMaterial.Wood && blockMaterial != EnumBlockMaterial.Leaves) || !block.SideSolid[BlockFacing.DOWN.Index])
			{
				continue;
			}
			atPos.Set(pos.X, pos.Y - i - 1, pos.Z);
			Block block2 = blockAccessor.GetBlock(atPos);
			EnumBlockMaterial blockMaterial2 = block2.GetBlockMaterial(blockAccessor, atPos);
			BlockPos pos2 = atPos.DownCopy();
			if (blockMaterial2 == EnumBlockMaterial.Wood && blockMaterial == EnumBlockMaterial.Wood && blockAccessor.GetBlock(pos2).GetBlockMaterial(blockAccessor, pos2) == EnumBlockMaterial.Wood && block2.Variant["rotation"] == "ud")
			{
				Block block3 = blockAccessor.GetBlock(new AssetLocation("wildbeehive-inlog-" + block.Variant["wood"]));
				blockAccessor.SetBlock(block3.BlockId, atPos);
				if (EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(EntityClass, atPos);
				}
				return true;
			}
			if (blockMaterial2 == EnumBlockMaterial.Leaves || blockMaterial2 == EnumBlockMaterial.Air)
			{
				int num = pos.X % 32;
				int num2 = pos.Z % 32;
				int num3 = blockAccessor.GetMapChunkAtBlockPos(atPos).WorldGenTerrainHeightMap[num2 * 32 + num];
				if (pos.Y - num3 < 4)
				{
					return false;
				}
				blockAccessor.SetBlock(BlockId, atPos);
				if (EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(EntityClass, atPos);
				}
				return true;
			}
		}
		return false;
	}
}
