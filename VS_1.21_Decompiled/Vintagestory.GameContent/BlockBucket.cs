using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBucket : BlockLiquidContainerTopOpened
{
	protected override string meshRefsCacheKey => "bucketMeshRefs" + Code;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBucket blockEntityBucket)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			float num3 = (float)Math.PI / 8f;
			float meshAngle = (float)(int)Math.Round(num2 / num3) * num3;
			blockEntityBucket.MeshAngle = meshAngle;
		}
		return num;
	}
}
