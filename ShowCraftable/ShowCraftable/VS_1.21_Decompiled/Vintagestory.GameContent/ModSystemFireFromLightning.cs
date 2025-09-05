using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemFireFromLightning : ModSystem
{
	private ICoreServerAPI api;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.ModLoader.GetModSystem<WeatherSystemServer>().OnLightningImpactEnd += ModSystemFireFromLightning_OnLightningImpactEnd;
	}

	private void ModSystemFireFromLightning_OnLightningImpactEnd(ref Vec3d impactPos, ref EnumHandling handling)
	{
		if (handling != EnumHandling.PassThrough || !api.World.Config.GetBool("lightningFires"))
		{
			return;
		}
		Random rand = api.World.Rand;
		BlockPos blockPos = impactPos.AsBlockPos.Add(rand.Next(2) - 1, rand.Next(2) - 1, rand.Next(2) - 1);
		if (api.World.BlockAccessor.GetBlock(blockPos).CombustibleProps == null)
		{
			return;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos blockPos2 = blockPos.AddCopy(blockFacing);
			if (api.World.BlockAccessor.GetBlock(blockPos2).BlockId == 0 && api.ModLoader.GetModSystem<WeatherSystemBase>().GetEnvironmentWetness(blockPos2, 10.0) < 0.01)
			{
				api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("fire")).BlockId, blockPos2);
				api.World.BlockAccessor.GetBlockEntity(blockPos2)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(blockFacing, null);
			}
		}
	}
}
