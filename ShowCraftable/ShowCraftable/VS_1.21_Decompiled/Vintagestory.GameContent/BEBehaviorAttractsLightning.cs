using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorAttractsLightning : BlockEntityBehavior
{
	private class ConfigurationProperties
	{
		public float ArtificialElevation { get; set; } = 1f;

		public float ElevationAttractivenessMultiplier { get; set; } = 1f;
	}

	private ConfigurationProperties configProps;

	private bool registered;

	private WeatherSystemServer weatherSystem => Api.ModLoader.GetModSystem<WeatherSystemServer>();

	public BEBehaviorAttractsLightning(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		configProps = properties.AsObject<ConfigurationProperties>();
		if (Api.Side == EnumAppSide.Server && !registered)
		{
			weatherSystem.OnLightningImpactBegin += OnLightningStart;
			registered = true;
		}
	}

	public override void OnBlockPlaced(ItemStack byItemstack = null)
	{
		base.OnBlockPlaced();
		if (Api.Side == EnumAppSide.Server && !registered)
		{
			weatherSystem.OnLightningImpactBegin += OnLightningStart;
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side != EnumAppSide.Client)
		{
			weatherSystem.OnLightningImpactBegin -= OnLightningStart;
		}
	}

	private void OnLightningStart(ref Vec3d impactPos, ref EnumHandling handling)
	{
		IWorldAccessor world = Blockentity.Api.World;
		BlockPos pos = Blockentity.Pos;
		int rainMapHeightAt = world.BlockAccessor.GetRainMapHeightAt(pos.X, pos.Z);
		if (rainMapHeightAt == pos.Y)
		{
			int rainMapHeightAt2 = world.BlockAccessor.GetRainMapHeightAt((int)impactPos.X, (int)impactPos.Z);
			float num = configProps.ArtificialElevation + (float)rainMapHeightAt - (float)rainMapHeightAt2;
			num *= configProps.ElevationAttractivenessMultiplier;
			num = GameMath.Min(40f, num);
			if (!(new Vec2d(Blockentity.Pos.X, Blockentity.Pos.Z).DistanceTo(impactPos.X, impactPos.Z) > (double)num))
			{
				impactPos = Blockentity.Pos.ToVec3d();
			}
		}
	}
}
