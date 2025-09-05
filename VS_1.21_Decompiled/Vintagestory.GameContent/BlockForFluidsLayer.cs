using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockForFluidsLayer : Block
{
	public float InsideDamage;

	public EnumDamageType DamageType;

	public override bool ForFluidsLayer => true;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		InsideDamage = Attributes?["insideDamage"].AsFloat() ?? 0f;
		DamageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), Attributes?["damageType"].AsString("Fire") ?? "Fire");
	}

	public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
		if (InsideDamage > 0f && world.Side == EnumAppSide.Server)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Type = DamageType,
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				SourcePos = pos.ToVec3d()
			}, InsideDamage);
		}
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		world.BlockAccessor.SetBlock(BlockId, blockSel.Position, 2);
		return true;
	}
}
