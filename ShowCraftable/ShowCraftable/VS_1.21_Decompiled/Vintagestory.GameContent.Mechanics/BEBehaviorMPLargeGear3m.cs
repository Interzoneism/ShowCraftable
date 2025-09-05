using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPLargeGear3m : BEBehaviorMPBase
{
	public float ratio = 5.5f;

	public BEBehaviorMPLargeGear3m(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		AxisSign = new int[3] { 0, 1, 0 };
		if (api.Side == EnumAppSide.Client)
		{
			Blockentity.RegisterGameTickListener(onEverySecond, 1000);
		}
	}

	public override bool isInvertedNetworkFor(BlockPos pos)
	{
		return propagationDir == BlockFacing.DOWN;
	}

	private void onEverySecond(float dt)
	{
		float num = ((network == null) ? 0f : network.Speed);
		if (Api.World.Rand.NextDouble() < (double)(num / 4f))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/woodcreak"), (double)Position.X + 0.5, (double)Position.Y + 0.5, (double)Position.Z + 0.5, null, 0.85f + num);
		}
	}

	public override void SetPropagationDirection(MechPowerPath path)
	{
		BlockFacing blockFacing = path.NetworkDir();
		if (blockFacing != BlockFacing.UP && blockFacing != BlockFacing.DOWN)
		{
			blockFacing = (path.IsInvertedTowards(Position) ? BlockFacing.UP : BlockFacing.DOWN);
			base.GearedRatio = path.gearingRatio / ratio;
		}
		else
		{
			base.GearedRatio = path.gearingRatio;
		}
		if (propagationDir == blockFacing.Opposite && network != null)
		{
			if (!network.DirectionHasReversed)
			{
				network.TurnDir = ((network.TurnDir == EnumRotDirection.Clockwise) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
			}
			network.DirectionHasReversed = true;
		}
		propagationDir = blockFacing;
	}

	public override bool IsPropagationDirection(BlockPos fromPos, BlockFacing test)
	{
		if (propagationDir == test)
		{
			return true;
		}
		if (test.IsHorizontal)
		{
			if (fromPos.AddCopy(test) == Position)
			{
				return propagationDir == BlockFacing.DOWN;
			}
			if (fromPos.AddCopy(test.Opposite) == Position)
			{
				return propagationDir == BlockFacing.UP;
			}
		}
		return false;
	}

	public override float GetGearedRatio(BlockFacing face)
	{
		if (!face.IsHorizontal)
		{
			return base.GearedRatio;
		}
		return base.GearedRatio * ratio;
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath pathDir)
	{
		BlockFacing outFacing = pathDir.OutFacing;
		BELargeGear3m bELargeGear3m = Blockentity as BELargeGear3m;
		int num = 0;
		if (outFacing == BlockFacing.UP || outFacing == BlockFacing.DOWN)
		{
			MechPowerPath[] array = new MechPowerPath[2 + bELargeGear3m.CountGears(Api)];
			array[num] = pathDir;
			array[++num] = new MechPowerPath(pathDir.OutFacing.Opposite, pathDir.gearingRatio, null, !pathDir.invert);
			bool inverted = (outFacing == BlockFacing.DOWN) ^ pathDir.invert;
			for (int i = 0; i < 4; i++)
			{
				BlockFacing facing = BlockFacing.HORIZONTALS[i];
				if (bELargeGear3m.HasGearAt(Api, Position.AddCopy(facing)))
				{
					array[++num] = new MechPowerPath(facing, pathDir.gearingRatio * ratio, null, inverted);
				}
			}
			return array;
		}
		MechPowerPath[] array2 = new MechPowerPath[2 + bELargeGear3m.CountGears(Api)];
		bool flag = pathDir.IsInvertedTowards(Position);
		array2[0] = new MechPowerPath(BlockFacing.DOWN, pathDir.gearingRatio / ratio, null, flag);
		array2[1] = new MechPowerPath(BlockFacing.UP, pathDir.gearingRatio / ratio, null, !flag);
		num = 1;
		bool inverted2 = (outFacing == BlockFacing.DOWN) ^ !flag;
		for (int j = 0; j < 4; j++)
		{
			BlockFacing facing2 = BlockFacing.HORIZONTALS[j];
			if (bELargeGear3m.HasGearAt(Api, Position.AddCopy(facing2)))
			{
				array2[++num] = new MechPowerPath(facing2, pathDir.gearingRatio, null, inverted2);
			}
		}
		return array2;
	}

	public bool AngledGearNotAlreadyAdded(BlockPos position)
	{
		return ((BELargeGear3m)Blockentity).AngledGearNotAlreadyAdded(position);
	}

	public override float GetResistance()
	{
		return 0.004f;
	}

	internal bool OnInteract(IPlayer byPlayer)
	{
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		_ = Api.World.EntityDebugMode;
	}

	internal float GetSmallgearAngleRad()
	{
		return AngleRad * ratio;
	}
}
