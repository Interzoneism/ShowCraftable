using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BELargeGear3m : BlockEntity, IGearAcceptor
{
	public BlockPos[] gear;

	public override void Initialize(ICoreAPI api)
	{
		gear = new BlockPos[4];
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		TestGear(blockAccessor, Pos.NorthCopy());
		TestGear(blockAccessor, Pos.SouthCopy());
		TestGear(blockAccessor, Pos.WestCopy());
		TestGear(blockAccessor, Pos.EastCopy());
		base.Initialize(api);
	}

	private void TestGear(IBlockAccessor accessor, BlockPos pos)
	{
		if (accessor.GetBlock(pos) is BlockAngledGears)
		{
			AddGear(pos);
			accessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAngledGears>()?.SetLargeGear(this);
		}
	}

	public bool AngledGearNotAlreadyAdded(BlockPos position)
	{
		if (gear == null || HasGearAt(position))
		{
			return false;
		}
		AddGear(position);
		return true;
	}

	bool IGearAcceptor.CanAcceptGear(BlockPos pos)
	{
		if (pos.Y != Pos.Y)
		{
			return false;
		}
		int num = Pos.X - pos.X;
		int num2 = Pos.Z - pos.Z;
		if (num != 0 && num2 != 0)
		{
			return false;
		}
		if (HasGearAt(pos))
		{
			return false;
		}
		if (num + num2 != 1)
		{
			return num + num2 == -1;
		}
		return true;
	}

	public bool HasGears()
	{
		for (int i = 0; i < 4; i++)
		{
			if (gear[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	public int CountGears()
	{
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			if (gear[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	public int CountGears(ICoreAPI api)
	{
		if (gear == null)
		{
			Initialize(api);
		}
		return CountGears();
	}

	public bool HasGearAt(BlockPos pos)
	{
		if (!pos.Equals(gear[0]) && !pos.Equals(gear[1]) && !pos.Equals(gear[2]))
		{
			return pos.Equals(gear[3]);
		}
		return true;
	}

	public bool HasGearAt(ICoreAPI api, BlockPos pos)
	{
		if (gear == null)
		{
			Initialize(api);
		}
		return HasGearAt(pos);
	}

	public void AddGear(BlockPos pos)
	{
		for (int i = 0; i < 4; i++)
		{
			if (gear[i] == null)
			{
				gear[i] = pos;
				break;
			}
		}
	}

	public void RemoveGearAt(BlockPos pos)
	{
		if (gear == null)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			if (pos.Equals(gear[i]))
			{
				gear[i] = null;
				break;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
	}
}
