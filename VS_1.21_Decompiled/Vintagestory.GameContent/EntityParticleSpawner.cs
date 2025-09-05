using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityParticleSpawner : ModSystem
{
	private ICoreClientAPI capi;

	private Random rand = new Random();

	private NormalizedSimplexNoise grasshopperNoise;

	private NormalizedSimplexNoise cicadaNoise;

	private NormalizedSimplexNoise matingGnatsSwarmNoise;

	private NormalizedSimplexNoise coquiNoise;

	private NormalizedSimplexNoise waterstriderNoise;

	private Queue<Action> SimTickExecQueue = new Queue<Action>();

	public HashSet<string> disabledInsects;

	private EntityParticleSystem sys;

	private float accum;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		grasshopperNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 100);
		coquiNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.0025, 0.9, api.World.Seed * 101);
		waterstriderNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 102);
		matingGnatsSwarmNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 103);
		cicadaNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 104);
		sys = api.ModLoader.GetModSystem<EntityParticleSystem>();
		sys.OnSimTick += Sys_OnSimTick;
		disabledInsects = new HashSet<string>();
		List<string> list = capi.Settings.Strings["disabledInsects"];
		if (list != null)
		{
			disabledInsects.AddRange(list);
		}
		api.ChatCommands.GetOrCreate("insectconfig").WithArgs(api.ChatCommands.Parsers.WordRange("type", "grasshopper", "cicada", "gnats", "coqui", "waterstrider"), api.ChatCommands.Parsers.OptionalBool("enable/disable")).HandleWith(onCmdInsectConfig);
		api.ChatCommands.GetOrCreate("debug").BeginSub("eps").WithDesc("eps")
			.BeginSub("testspawn")
			.WithDesc("testspawn")
			.WithArgs(api.ChatCommands.Parsers.WordRange("type", "gh", "ws", "coq", "mg", "cic", "fis"))
			.HandleWith(handleSpawn)
			.EndSub()
			.BeginSub("count")
			.WithDesc("count")
			.HandleWith(handleCount)
			.EndSub()
			.BeginSub("clear")
			.WithDesc("clear")
			.HandleWith(handleClear)
			.EndSub()
			.BeginSub("testnoise")
			.WithDesc("testnoise")
			.HandleWith(handleTestnoise)
			.WithArgs(api.ChatCommands.Parsers.OptionalWordRange("clear", "clear"))
			.EndSub()
			.EndSub();
	}

	private TextCommandResult onCmdInsectConfig(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("{0} are currently {1}", text, disabledInsects.Contains(text) ? Lang.Get("disabled") : Lang.Get("enabled")));
		}
		bool flag = !(bool)args[1];
		if (flag)
		{
			disabledInsects.Add(text);
		}
		else
		{
			disabledInsects.Remove(text);
		}
		capi.Settings.Strings["disabledInsects"] = disabledInsects.ToList();
		return TextCommandResult.Success(Lang.Get("{0} are now {1}", text, flag ? Lang.Get("disabled") : Lang.Get("enabled")));
	}

	private TextCommandResult handleCount(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item in sys.Count.Dict)
		{
			stringBuilder.AppendLine($"{item.Key}: {item.Value}");
		}
		if (stringBuilder.Length == 0)
		{
			return TextCommandResult.Success("No entityparticle alive");
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult handleTestnoise(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = capi.World.Player.Entity.Pos.XYZ.AsBlockPos;
		Block block = capi.World.GetBlock(new AssetLocation("creativeblock-35"));
		bool flag = !args.Parsers[0].IsMissing;
		for (int i = -200; i <= 200; i++)
		{
			for (int j = -200; j <= 200; j++)
			{
				double num = matingGnatsSwarmNoise.Noise(asBlockPos.X + i, asBlockPos.Z + j);
				if (flag || num < 0.65)
				{
					capi.World.BlockAccessor.SetBlock(0, new BlockPos(asBlockPos.X + i, 160, asBlockPos.Z + j));
				}
				else
				{
					capi.World.BlockAccessor.SetBlock(block.Id, new BlockPos(asBlockPos.X + i, 160, asBlockPos.Z + j));
				}
			}
		}
		return TextCommandResult.Success("testnoise");
	}

	private TextCommandResult handleClear(TextCommandCallingArgs args)
	{
		sys.Clear();
		sys.SpawnedFish.Clear();
		return TextCommandResult.Success("cleared");
	}

	private TextCommandResult handleSpawn(TextCommandCallingArgs args)
	{
		string type = args[0] as string;
		SimTickExecQueue.Enqueue(delegate
		{
			EntityPos pos = capi.World.Player.Entity.Pos;
			ClimateCondition climateAt = capi.World.BlockAccessor.GetClimateAt(pos.AsBlockPos);
			float cohesion = (float)GameMath.Max(rand.NextDouble() * 1.1, 0.25);
			Vec3d vec3d = pos.XYZ.AddCopy(0f, 1.5f, 0f);
			for (int i = 0; i < 20; i++)
			{
				double num = pos.X + (rand.NextDouble() - 0.5) * 10.0;
				double num2 = pos.Z + (rand.NextDouble() - 0.5) * 10.0;
				double num3 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num, (int)num2);
				if (type == "gh")
				{
					EntityParticleGrasshopper eparticle = new EntityParticleGrasshopper(capi, num, num3 + 1.0 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle);
				}
				if (type == "coq")
				{
					EntityParticleCoqui eparticle2 = new EntityParticleCoqui(capi, num, num3 + 1.0 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle2);
				}
				if (type == "ws")
				{
					Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2, 2);
					if (blockRaw.LiquidCode == "water" && blockRaw.PushVector == null)
					{
						EntityParticleWaterStrider eparticle3 = new EntityParticleWaterStrider(capi, num, num3 + (double)((float)blockRaw.LiquidLevel / 8f), num2);
						sys.SpawnParticle(eparticle3);
					}
				}
				if (type == "fis")
				{
					num = pos.X + (rand.NextDouble() - 0.5) * 2.0;
					num2 = pos.Z + (rand.NextDouble() - 0.5) * 2.0;
					Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2, 2);
					if (blockRaw2.LiquidCode == "saltwater" && blockRaw2.PushVector == null)
					{
						EntityParticleFish eparticle4 = new EntityParticleFish(capi, num, num3 - (double)blockRaw2.LiquidLevel, num2, new Vec3f(0.4f), 0, 0.3f);
						sys.SpawnParticle(eparticle4);
					}
				}
				if (type == "mg")
				{
					sys.SpawnParticle(new EntityParticleMatingGnats(capi, cohesion, vec3d.X, vec3d.Y, vec3d.Z));
				}
				if (type == "cic")
				{
					spawnCicadas(pos, climateAt);
				}
			}
		});
		return TextCommandResult.Success(type + " spawned.");
	}

	private void Sys_OnSimTick(float dt)
	{
		accum += dt;
		while (SimTickExecQueue.Count > 0)
		{
			SimTickExecQueue.Dequeue()();
		}
		if (!(accum > 0.5f))
		{
			return;
		}
		accum = 0f;
		EntityPos pos = capi.World.Player.Entity.Pos;
		if (pos.Dimension == 0)
		{
			ClimateCondition climateAt = capi.World.BlockAccessor.GetClimateAt(pos.AsBlockPos);
			if (!disabledInsects.Contains("grasshopper"))
			{
				spawnGrasshoppers(pos, climateAt);
			}
			if (!disabledInsects.Contains("cicada"))
			{
				spawnCicadas(pos, climateAt);
			}
			if (!disabledInsects.Contains("gnats"))
			{
				spawnMatingGnatsSwarm(pos, climateAt);
			}
			if (!disabledInsects.Contains("coqui"))
			{
				spawnCoquis(pos, climateAt);
			}
			if (!disabledInsects.Contains("waterstrider"))
			{
				spawnWaterStriders(pos, climateAt);
			}
			spawnFish(pos, climateAt);
		}
	}

	private void spawnWaterStriders(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 35f || climate.Temperature < 19f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || waterstriderNoise.Noise(pos.X, pos.Z) < 0.5 || sys.Count["waterStrider"] > 50)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double num = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double num2 = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double num3 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num, (int)num2);
			if (!(pos.HorDistanceTo(num, num2) < 3.0))
			{
				Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2, 2);
				Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3 - 1, (int)num2);
				Block blockRaw3 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3 + 1, (int)num2);
				if (blockRaw.LiquidCode == "water" && blockRaw.PushVector == null && blockRaw2.Replaceable < 6000 && blockRaw3.Id == 0)
				{
					EntityParticleWaterStrider eparticle = new EntityParticleWaterStrider(capi, num, num3 + (double)((float)blockRaw.LiquidLevel / 8f), num2);
					sys.SpawnParticle(eparticle);
				}
			}
		}
	}

	private void spawnFish(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 40f || climate.Temperature < 0f)
		{
			return;
		}
		BlockPos blockPos = new BlockPos(0);
		if (sys.Count["fish"] > 500)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			int num = (int)(pos.X + (rand.NextDouble() - 0.5) * 60.0);
			int num2 = (int)(pos.Z + (rand.NextDouble() - 0.5) * 60.0);
			blockPos.Set(num, 0, num2);
			int terrainMapheightAt = capi.World.BlockAccessor.GetTerrainMapheightAt(blockPos);
			blockPos.Y = Math.Min(capi.World.SeaLevel - 1, terrainMapheightAt + 2);
			if (blockPos.HorDistanceSqTo(pos.X, pos.Z) < 16f)
			{
				continue;
			}
			FastVec3i fastVec3i = new FastVec3i(blockPos.X, blockPos.Y, blockPos.Z);
			if (sys.SpawnedFish.Contains(fastVec3i) || GameMath.MurmurHash3Mod(blockPos.X, blockPos.Y, blockPos.Z, 100) < 80)
			{
				continue;
			}
			Block block = capi.World.BlockAccessor.GetBlock(blockPos, 2);
			Block block2 = capi.World.BlockAccessor.GetBlock(blockPos.DownCopy());
			if (!(block.LiquidCode != "saltwater") && block2.Code.Path.StartsWithFast("coral"))
			{
				sys.SpawnedFish.Add(fastVec3i);
				int num3 = 5 + rand.Next(15);
				EntityParticleFish[] array = new EntityParticleFish[num3];
				Vec3f vec3f = new Vec3f(0.55f + (float)rand.NextDouble() * 0.65f, 0.3f, 0.3f);
				vec3f.Mul(1f + (float)rand.NextDouble() * 0.5f);
				float maxspeed = 0.15f + (float)rand.NextDouble() * 0.2f;
				int colorindex = rand.Next(EntityParticleFish.Colors.Length);
				for (int j = 0; j < num3; j++)
				{
					double num4 = rand.NextDouble() - 0.5;
					double num5 = rand.NextDouble() - 0.5;
					EntityParticleFish entityParticleFish = new EntityParticleFish(capi, (double)num + num4, blockPos.Y, (double)num2 + num5, vec3f, colorindex, maxspeed);
					entityParticleFish.StartPos = fastVec3i;
					array[j] = entityParticleFish;
					sys.SpawnParticle(entityParticleFish);
				}
				for (int k = 0; k < num3; k++)
				{
					array[k].FriendFishes = new EntityParticleFish[4]
					{
						array[GameMath.Mod(k - 2, num3)],
						array[GameMath.Mod(k - 1, num3)],
						array[GameMath.Mod(k + 1, num3)],
						array[GameMath.Mod(k + 2, num3)]
					};
				}
			}
		}
	}

	private void spawnGrasshoppers(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature >= 30f || climate.Temperature < 18f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || grasshopperNoise.Noise(pos.X, pos.Z) < 0.7 || sys.Count["grassHopper"] > 40)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double num = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double num2 = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double num3 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num, (int)num2);
			if (!(pos.HorDistanceTo(num, num2) < 3.0))
			{
				Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3 + 1, (int)num2);
				Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2);
				if (blockRaw.BlockMaterial == EnumBlockMaterial.Plant && blockRaw2.BlockMaterial == EnumBlockMaterial.Soil)
				{
					EntityParticleGrasshopper eparticle = new EntityParticleGrasshopper(capi, num, num3 + 1.01 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle);
				}
			}
		}
	}

	private void spawnCicadas(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 33f || climate.Temperature < 22f || climate.WorldGenTemperature < 10f || climate.WorldGenTemperature > 22f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || cicadaNoise.Noise(pos.X, pos.Z, capi.World.Calendar.Year) < 0.7 || sys.Count["cicada"] > 40)
		{
			return;
		}
		for (int i = 0; i < 400; i++)
		{
			double num = pos.X + (rand.NextDouble() - 0.5) * 50.0;
			double num2 = pos.Z + (rand.NextDouble() - 0.5) * 50.0;
			double num3 = pos.Y + (rand.NextDouble() - 0.5) * 10.0;
			if (pos.HorDistanceTo(num, num2) < 2.0)
			{
				continue;
			}
			Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2);
			Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3 - 1, (int)num2);
			if (blockRaw.BlockMaterial != EnumBlockMaterial.Wood || !(blockRaw.Variant["type"] == "grown") || blockRaw2.Id != blockRaw.Id)
			{
				continue;
			}
			Vec3f normalf = BlockFacing.HORIZONTALS[rand.Next(4)].Normalf;
			double num4 = (float)(int)num + 0.5f + normalf.X * 0.52f;
			double num5 = num3 + 0.1 + rand.NextDouble() * 0.8;
			double num6 = (float)(int)num2 + 0.5f + normalf.Z * 0.52f;
			if (capi.World.BlockAccessor.GetBlockRaw((int)num4, (int)num5, (int)num6).Replaceable >= 6000)
			{
				EntityParticleCicada eparticle = new EntityParticleCicada(capi, num4, num5, num6);
				sys.SpawnParticle(eparticle);
				continue;
			}
			num4 += (double)normalf.X;
			num6 += (double)normalf.Z;
			if (capi.World.BlockAccessor.GetBlockRaw((int)num4, (int)num5, (int)num6).Replaceable >= 6000)
			{
				EntityParticleCicada eparticle2 = new EntityParticleCicada(capi, num4, num5, num6);
				sys.SpawnParticle(eparticle2);
			}
		}
	}

	private void spawnCoquis(EntityPos pos, ClimateCondition climate)
	{
		if (climate.WorldGenTemperature < 30f || (double)climate.WorldgenRainfall < 0.7 || coquiNoise.Noise(pos.X, pos.Z) < 0.8 || sys.Count["coqui"] > 60)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double num = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double num2 = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double num3 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num, (int)num2);
			if (!(pos.HorDistanceTo(num, num2) < 3.0))
			{
				Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3 + 1, (int)num2);
				Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num, (int)num3, (int)num2);
				if (blockRaw.BlockMaterial == EnumBlockMaterial.Plant && blockRaw2.BlockMaterial == EnumBlockMaterial.Soil)
				{
					EntityParticleCoqui eparticle = new EntityParticleCoqui(capi, num, num3 + 1.01 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle);
				}
			}
		}
	}

	private void spawnMatingGnatsSwarm(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature < 17f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.6 || GlobalConstants.CurrentWindSpeedClient.Length() > 0.35f || matingGnatsSwarmNoise.Noise(pos.X, pos.Z) < 0.5 || sys.Count["matinggnats"] > 200)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < 100; i++)
		{
			if (num >= 6)
			{
				break;
			}
			double num2 = pos.X + (rand.NextDouble() - 0.5) * 24.0;
			double num3 = pos.Z + (rand.NextDouble() - 0.5) * 24.0;
			double num4 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num2, (int)num3);
			if (pos.HorDistanceTo(num2, num3) < 2.0)
			{
				continue;
			}
			Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num2, (int)num4 + 2, (int)num3);
			Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num2, (int)num4 + 1, (int)num3);
			Block blockRaw3 = capi.World.BlockAccessor.GetBlockRaw((int)num2, (int)num4, (int)num3, 2);
			Block blockRaw4 = capi.World.BlockAccessor.GetBlockRaw((int)num2, (int)num4 - 2, (int)num3, 2);
			if (blockRaw3.LiquidCode == "water" && blockRaw2.Id == 0 && blockRaw.Id == 0 && blockRaw4.Id == 0)
			{
				float cohesion = (float)GameMath.Max(rand.NextDouble() * 1.1, 0.25) / 2f;
				int num5 = 10 + rand.Next(21);
				for (int j = 0; j < num5; j++)
				{
					sys.SpawnParticle(new EntityParticleMatingGnats(capi, cohesion, (double)(int)num2 + 0.5, num4 + 1.5 + rand.NextDouble() * 0.5, (double)(int)num3 + 0.5));
				}
				num++;
			}
		}
	}
}
