using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemAmbientParticles : ModSystem
{
	private SimpleParticleProperties liquidParticles;

	private SimpleParticleProperties summerAirParticles;

	private SimpleParticleProperties fireflyParticles;

	private ClampedSimplexNoise fireflyLocationNoise;

	private ClampedSimplexNoise fireflyrateNoise;

	private ICoreClientAPI capi;

	private ClimateCondition climate = new ClimateCondition();

	private bool spawnParticles;

	private Vec3d position = new Vec3d();

	private BlockPos blockPos = new BlockPos();

	public event ActionBoolReturn ShouldSpawnAmbientParticles;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Event.RegisterGameTickListener(OnSlowTick, 1000);
		capi.Event.RegisterAsyncParticleSpawner(AsyncParticleSpawnTick);
		liquidParticles = new SimpleParticleProperties
		{
			MinSize = 0.1f,
			MaxSize = 0.1f,
			MinQuantity = 1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			ParticleModel = EnumParticleModel.Quad,
			ShouldDieInAir = true,
			VertexFlags = 512
		};
		liquidParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -9f);
		summerAirParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(35, 230, 230, 150),
			ParticleModel = EnumParticleModel.Quad,
			MinSize = 0.05f,
			MaxSize = 0.1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			WithTerrainCollision = false,
			ShouldDieInLiquid = true,
			MinVelocity = new Vec3f(-0.125f, -0.125f, -0.125f),
			MinQuantity = 1f,
			AddQuantity = 0f
		};
		summerAirParticles.AddVelocity = new Vec3f(0.25f, 0.25f, 0.25f);
		summerAirParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
		summerAirParticles.MinPos = new Vec3d();
		fireflyParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(150, 150, 250, 139),
			ParticleModel = EnumParticleModel.Quad,
			MinSize = 0.1f,
			MaxSize = 0.1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			ShouldDieInLiquid = true,
			MinVelocity = new Vec3f(-0.25f, -0.0625f, -0.25f),
			MinQuantity = 2f,
			AddQuantity = 0f,
			LightEmission = ColorUtil.ToRgba(255, 77, 250, 139)
		};
		fireflyParticles.AddVelocity = new Vec3f(0.5f, 0.125f, 0.5f);
		fireflyParticles.VertexFlags = 255;
		fireflyParticles.AddPos.Set(1.0, 1.0, 1.0);
		fireflyParticles.AddQuantity = 8f;
		fireflyParticles.addLifeLength = 1f;
		fireflyParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
		fireflyParticles.RandomVelocityChange = true;
		fireflyLocationNoise = new ClampedSimplexNoise(new double[1] { 1.0 }, new double[1] { 5.0 }, capi.World.Rand.Next());
		fireflyrateNoise = new ClampedSimplexNoise(new double[1] { 1.0 }, new double[1] { 5.0 }, capi.World.Rand.Next());
	}

	private void OnSlowTick(float dt)
	{
		climate = capi.World.BlockAccessor.GetClimateAt(capi.World.Player.Entity.Pos.AsBlockPos);
		if (climate == null)
		{
			climate = new ClimateCondition();
		}
		spawnParticles = capi.Settings.Bool["ambientParticles"];
	}

	private bool AsyncParticleSpawnTick(float dt, IAsyncParticleManager manager)
	{
		if (!spawnParticles)
		{
			return true;
		}
		if (this.ShouldSpawnAmbientParticles != null && !this.ShouldSpawnAmbientParticles())
		{
			return true;
		}
		int num = capi.Settings.Int["particleLevel"];
		IClientWorldAccessor world = capi.World;
		EntityPlayer entity = world.Player.Entity;
		ClimateCondition climateAt = world.BlockAccessor.GetClimateAt(blockPos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z));
		float num2 = 0.5f * (float)num;
		while (num2-- > 0f)
		{
			double x = world.Rand.NextDouble() * 32.0 - 16.0;
			double y = world.Rand.NextDouble() * 20.0 - 10.0;
			double z = world.Rand.NextDouble() * 32.0 - 16.0;
			position.Set(entity.Pos.X, entity.Pos.Y, entity.Pos.Z).Add(x, y, z);
			blockPos.Set(position);
			if (!world.BlockAccessor.IsValidPos(blockPos))
			{
				continue;
			}
			double num3 = 0.05 + (double)Math.Max(0f, world.Calendar.DayLightStrength) * 0.4;
			if ((double)climateAt.Rainfall <= 0.01 && GlobalConstants.CurrentWindSpeedClient.X < 0.2f && world.Rand.NextDouble() < num3 && climate.Temperature >= 14f && climate.WorldgenRainfall >= 0.4f && blockPos.Y > world.SeaLevel && manager.BlockAccess.GetBlock(blockPos).Id == 0)
			{
				IMapChunk mapChunk = manager.BlockAccess.GetMapChunk(blockPos.X / 32, blockPos.Z / 32);
				if (mapChunk != null && blockPos.Y > mapChunk.RainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32])
				{
					summerAirParticles.MinPos.Set(position);
					summerAirParticles.RandomVelocityChange = true;
					manager.Spawn(summerAirParticles);
				}
				continue;
			}
			Block block = manager.BlockAccess.GetBlock(blockPos, 2);
			if (block.IsLiquid() && block.LiquidLevel >= 7)
			{
				liquidParticles.MinVelocity = new Vec3f((float)world.Rand.NextDouble() / 16f - 1f / 32f, (float)world.Rand.NextDouble() / 16f - 1f / 32f, (float)world.Rand.NextDouble() / 16f - 1f / 32f);
				liquidParticles.MinPos = position;
				if (world.Rand.Next(3) > 0)
				{
					liquidParticles.RandomVelocityChange = false;
					liquidParticles.Color = ColorUtil.HsvToRgba(110, 40 + world.Rand.Next(50), 200 + world.Rand.Next(30), 50 + world.Rand.Next(40));
				}
				else
				{
					liquidParticles.RandomVelocityChange = true;
					liquidParticles.Color = ColorUtil.HsvToRgba(110, 20 + world.Rand.Next(25), 100 + world.Rand.Next(15), 100 + world.Rand.Next(40));
				}
				manager.Spawn(liquidParticles);
			}
		}
		if ((double)climateAt.Rainfall < 0.15 && climateAt.Temperature > 5f)
		{
			double num4 = (fireflyrateNoise.Noise(world.Calendar.TotalDays / 3.0, 0.0) - 0.4000000059604645) * 4.0;
			float num5 = Math.Max(0f, (float)(num4 - (double)(Math.Abs(GlobalConstants.CurrentWindSpeedClient.X) * 2f)));
			int num6 = GameMath.RoundRandom(world.Rand, num5 * 0.01f * (float)num);
			while (num6-- > 0)
			{
				double x2 = world.Rand.NextDouble() * 80.0 - 40.0;
				double y2 = world.Rand.NextDouble() * 80.0 - 40.0;
				double z2 = world.Rand.NextDouble() * 80.0 - 40.0;
				position.Set(entity.Pos.X, entity.Pos.Y, entity.Pos.Z).Add(x2, y2, z2);
				blockPos.Set(position);
				if (!world.BlockAccessor.IsValidPos(blockPos))
				{
					continue;
				}
				double num7 = Math.Max(0.0, fireflyLocationNoise.Noise((double)blockPos.X / 60.0, (double)blockPos.Z / 60.0, world.Calendar.TotalDays / 5.0 - 0.800000011920929) - 0.5) * 2.0;
				double num8 = (double)Math.Max(0f, 1f - world.Calendar.DayLightStrength * 2f) * num7;
				int y3 = blockPos.Y;
				blockPos.Y = manager.BlockAccess.GetTerrainMapheightAt(blockPos);
				Block block2 = manager.BlockAccess.GetBlock(blockPos);
				if (world.Rand.NextDouble() <= num8 && climate.Temperature >= 8f && climate.Temperature <= 29f && climate.WorldgenRainfall >= 0.4f && block2.Fertility > 30 && blockPos.Y > world.SeaLevel)
				{
					blockPos.Y += world.Rand.Next(4);
					position.Y += blockPos.Y - y3;
					block2 = manager.BlockAccess.GetBlock(blockPos);
					Cuboidf[] collisionBoxes = block2.GetCollisionBoxes(manager.BlockAccess, blockPos);
					if (collisionBoxes == null || collisionBoxes.Length == 0)
					{
						fireflyParticles.AddVelocity.X = 0.5f + GlobalConstants.CurrentWindSpeedClient.X;
						fireflyParticles.MinPos = position;
						manager.Spawn(fireflyParticles);
					}
				}
			}
		}
		return true;
	}
}
