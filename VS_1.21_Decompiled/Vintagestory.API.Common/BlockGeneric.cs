using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockGeneric : Block
{
	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				strongBlockBehavior.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData, ref handled);
				if (handled != EnumHandling.PassThrough)
				{
					flag = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return;
				}
			}
		}
		if (!flag)
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				strongBlockBehavior.OnDecalTesselation(world, decalMesh, pos, ref handled);
				if (handled != EnumHandling.PassThrough)
				{
					flag = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return;
				}
			}
		}
		if (!flag)
		{
			base.OnDecalTesselation(world, decalMesh, pos);
		}
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		bool flag = false;
		Cuboidf result = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				result = strongBlockBehavior.GetParticleBreakBox(blockAccess, pos, facing, ref handled);
				switch (handled)
				{
				case EnumHandling.PreventSubsequent:
					return result;
				case EnumHandling.PreventDefault:
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			return result;
		}
		return base.GetParticleBreakBox(blockAccess, pos, facing);
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		bool flag = false;
		List<Cuboidf> list = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (!(blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior))
			{
				continue;
			}
			EnumHandling handled = EnumHandling.PassThrough;
			Cuboidf[] particleCollisionBoxes = strongBlockBehavior.GetParticleCollisionBoxes(blockAccessor, pos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return particleCollisionBoxes;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
			if (particleCollisionBoxes != null)
			{
				if (list == null)
				{
					list = new List<Cuboidf>();
				}
				list.AddRange(particleCollisionBoxes);
			}
		}
		if (flag)
		{
			return list.ToArray();
		}
		if (list == null)
		{
			return base.GetParticleCollisionBoxes(blockAccessor, pos);
		}
		list.AddRange(base.GetParticleCollisionBoxes(blockAccessor, pos));
		return list.ToArray();
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		bool flag = false;
		List<Cuboidf> list = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (!(blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior))
			{
				continue;
			}
			EnumHandling handled = EnumHandling.PassThrough;
			Cuboidf[] collisionBoxes = strongBlockBehavior.GetCollisionBoxes(blockAccessor, pos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return collisionBoxes;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
			if (collisionBoxes != null)
			{
				if (list == null)
				{
					list = new List<Cuboidf>();
				}
				list.AddRange(collisionBoxes);
			}
		}
		if (flag)
		{
			return list.ToArray();
		}
		if (list == null)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		list.AddRange(base.GetCollisionBoxes(blockAccessor, pos));
		return list.ToArray();
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		bool flag = false;
		List<Cuboidf> list = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (!(blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior))
			{
				continue;
			}
			EnumHandling handled = EnumHandling.PassThrough;
			Cuboidf[] selectionBoxes = strongBlockBehavior.GetSelectionBoxes(blockAccessor, pos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return selectionBoxes;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
			if (selectionBoxes != null)
			{
				if (list == null)
				{
					list = new List<Cuboidf>();
				}
				list.AddRange(selectionBoxes);
			}
		}
		if (flag)
		{
			return list.ToArray();
		}
		if (list == null)
		{
			return base.GetSelectionBoxes(blockAccessor, pos);
		}
		list.AddRange(base.GetSelectionBoxes(blockAccessor, pos));
		return list.ToArray();
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				bool flag3 = strongBlockBehavior.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, ref handled);
				if (handled != EnumHandling.PassThrough)
				{
					flag = flag && flag3;
					flag2 = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return flag;
				}
			}
		}
		if (flag2)
		{
			return flag;
		}
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i] is StrongBlockBehavior strongBlockBehavior)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				bool flag3 = strongBlockBehavior.DoParticalSelection(world, pos, ref handled);
				if (handled != EnumHandling.PassThrough)
				{
					flag = flag && flag3;
					flag2 = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return flag;
				}
			}
		}
		if (flag2)
		{
			return flag;
		}
		return base.DoParticalSelection(world, pos);
	}
}
