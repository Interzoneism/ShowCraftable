using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityLayer : BlockEntity
{
	protected static readonly int WEIGHTLIMIT = 75;

	protected static readonly Vec3d center = new Vec3d(0.5, 0.125, 0.5);

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(OnEvery250Ms, 250);
	}

	private void OnEvery250Ms(float dt)
	{
		IWorldAccessor world = Api.World;
		Vec3d position = center.AddCopy(Pos);
		BlockPos blockPos = Pos.DownCopy();
		if (CheckSupport(world.BlockAccessor, blockPos))
		{
			return;
		}
		Entity[] entitiesAround = world.GetEntitiesAround(position, 1f, 1.5f, (Entity e) => e?.Properties.Weight > (float)WEIGHTLIMIT);
		foreach (Entity entity in entitiesAround)
		{
			Cuboidd cuboidd = new Cuboidd();
			EntityPos pos = entity.Pos;
			cuboidd.Set(entity.SelectionBox).Translate(pos.X, pos.Y, pos.Z);
			Cuboidf cuboidf = new Cuboidf();
			cuboidf.Set(base.Block.CollisionBoxes[0]);
			cuboidf.Translate(Pos.X, Pos.Y, Pos.Z);
			if (cuboidd.MinY <= (double)cuboidf.MaxY + 0.01 && cuboidd.MinY >= (double)cuboidf.MinY - 0.01)
			{
				bool flag = cuboidd.MaxZ > (double)cuboidf.Z2;
				bool flag2 = cuboidd.MinZ < (double)cuboidf.Z1;
				bool num2 = cuboidd.MinX < (double)cuboidf.X1;
				bool num3 = cuboidd.MinZ > (double)cuboidf.X2;
				bool flag3 = false;
				IBlockAccessor blockAccessor = world.BlockAccessor;
				if (num3)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.EastCopy());
				}
				if (num3 && flag2)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.EastCopy().North());
				}
				if (num3 && flag)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.EastCopy().South());
				}
				if (num2)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.WestCopy());
				}
				if (num2 && flag2)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.WestCopy().North());
				}
				if (num2 && flag)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.WestCopy().South());
				}
				if (flag2)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.NorthCopy());
				}
				if (flag)
				{
					flag3 |= CheckSupport(blockAccessor, blockPos.SouthCopy());
				}
			}
		}
	}

	protected bool CheckSupport(IBlockAccessor access, BlockPos pos)
	{
		return access.GetBlock(pos).Replaceable < 6000;
	}
}
