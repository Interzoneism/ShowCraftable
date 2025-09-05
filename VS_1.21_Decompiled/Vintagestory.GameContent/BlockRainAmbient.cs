using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRainAmbient : Block
{
	private ICoreClientAPI capi;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		ClimateCondition selfClimateCond = capi.World.Player.Entity.selfClimateCond;
		if (selfClimateCond != null && selfClimateCond.Rainfall > 0.1f && selfClimateCond.Temperature > 3f && (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y || world.BlockAccessor.GetDistanceToRainFall(pos, 3) <= 2))
		{
			return selfClimateCond.Rainfall;
		}
		return 0f;
	}
}
