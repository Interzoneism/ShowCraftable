using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class SystemTemporalStability : ModSystem
{
	private IServerNetworkChannel serverChannel;

	private IClientNetworkChannel clientChannel;

	private SimplexNoise stabilityNoise;

	private ICoreAPI api;

	private ICoreServerAPI sapi;

	private bool temporalStabilityEnabled;

	private bool stormsEnabled;

	private Dictionary<string, TemporalStormConfig> configs;

	private Dictionary<EnumTempStormStrength, TemporalStormText> texts;

	private TemporalStormConfig config;

	private TemporalStormRunTimeData data = new TemporalStormRunTimeData();

	private TempStormMobConfig mobConfig;

	private ModSystemRifts riftSys;

	public float modGlitchStrength;

	public HashSet<AssetLocation> stormMobCache = new HashSet<AssetLocation>();

	private string worldConfigStorminess;

	private CollisionTester collisionTester = new CollisionTester();

	private long spawnBreakUntilMs;

	private int nobreakSpawns;

	private Dictionary<string, Dictionary<string, int>> rareSpawnsCountByCodeByPlayer = new Dictionary<string, Dictionary<string, int>>();

	public float StormStrength
	{
		get
		{
			if (data.nowStormActive)
			{
				return data.stormGlitchStrength;
			}
			return 0f;
		}
	}

	public TemporalStormRunTimeData StormData => data;

	public event GetTemporalStabilityDelegate OnGetTemporalStability;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		riftSys = api.ModLoader.GetModSystem<ModSystemRifts>();
		texts = new Dictionary<EnumTempStormStrength, TemporalStormText>
		{
			{
				EnumTempStormStrength.Light,
				new TemporalStormText
				{
					Approaching = Lang.Get("A light temporal storm is approaching"),
					Imminent = Lang.Get("A light temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			},
			{
				EnumTempStormStrength.Medium,
				new TemporalStormText
				{
					Approaching = Lang.Get("A medium temporal storm is approaching"),
					Imminent = Lang.Get("A medium temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			},
			{
				EnumTempStormStrength.Heavy,
				new TemporalStormText
				{
					Approaching = Lang.Get("A heavy temporal storm is approaching"),
					Imminent = Lang.Get("A heavy temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			}
		};
		configs = new Dictionary<string, TemporalStormConfig>
		{
			{
				"veryrare",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 30f, 5f),
					StrengthIncrease = 0.025f,
					StrengthIncreaseCap = 0.25f
				}
			},
			{
				"rare",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 25f, 5f),
					StrengthIncrease = 0.05f,
					StrengthIncreaseCap = 0.5f
				}
			},
			{
				"sometimes",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 15f, 5f),
					StrengthIncrease = 0.1f,
					StrengthIncreaseCap = 1f
				}
			},
			{
				"often",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 7.5f, 2.5f),
					StrengthIncrease = 0.15f,
					StrengthIncreaseCap = 1.5f
				}
			},
			{
				"veryoften",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 4.5f, 1.5f),
					StrengthIncrease = 0.2f,
					StrengthIncreaseCap = 2f
				}
			}
		};
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		api.Event.BlockTexturesLoaded += LoadNoise;
		clientChannel = api.Network.RegisterChannel("temporalstability").RegisterMessageType(typeof(TemporalStormRunTimeData)).SetMessageHandler<TemporalStormRunTimeData>(onServerData);
	}

	private void onServerData(TemporalStormRunTimeData data)
	{
		this.data = data;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.ChatCommands.Create("nexttempstorm").WithDescription("Tells you the amount of days until the next storm").RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnCmdNextStorm)
			.BeginSubCommand("now")
			.WithDescription("Start next temporal storm now")
			.HandleWith(delegate
			{
				data.nextStormTotalDays = api.World.Calendar.TotalDays;
				return TextCommandResult.Success();
			})
			.EndSubCommand();
		serverChannel = api.Network.RegisterChannel("temporalstability").RegisterMessageType(typeof(TemporalStormRunTimeData));
		api.Event.SaveGameLoaded += delegate
		{
			bool flag = sapi.WorldManager.SaveGame.IsNew;
			if (!sapi.World.Config.HasAttribute("temporalStability"))
			{
				string playStyle = sapi.WorldManager.SaveGame.PlayStyle;
				if (playStyle == "surviveandbuild" || playStyle == "wildernesssurvival")
				{
					sapi.WorldManager.SaveGame.WorldConfiguration.SetBool("temporalStability", value: true);
				}
			}
			if (!sapi.World.Config.HasAttribute("temporalStorms"))
			{
				string playStyle2 = sapi.WorldManager.SaveGame.PlayStyle;
				if (playStyle2 == "surviveandbuild" || playStyle2 == "wildernesssurvival")
				{
					sapi.WorldManager.SaveGame.WorldConfiguration.SetString("temporalStorms", (playStyle2 == "surviveandbuild") ? "sometimes" : "often");
				}
			}
			byte[] array = sapi.WorldManager.SaveGame.GetData("temporalStormData");
			if (array != null)
			{
				try
				{
					data = SerializerUtil.Deserialize<TemporalStormRunTimeData>(array);
				}
				catch (Exception)
				{
					api.World.Logger.Notification("Failed loading temporal storm data, will initialize new data set");
					data = new TemporalStormRunTimeData();
					flag = true;
				}
			}
			else
			{
				data = new TemporalStormRunTimeData();
				flag = true;
			}
			LoadNoise();
			if (flag)
			{
				prepareNextStorm();
			}
		};
		api.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.PlayerNowPlaying += Event_PlayerNowPlaying;
		api.Event.RegisterGameTickListener(onTempStormTick, 2000);
	}

	private TextCommandResult OnCmdNextStorm(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (data.nowStormActive)
		{
			double num = data.stormActiveTotalDays - api.World.Calendar.TotalDays;
			return TextCommandResult.Success(Lang.Get(data.nextStormStrength.ToString() + " Storm still active for {0:0.##} days", num));
		}
		double num2 = data.nextStormTotalDays - api.World.Calendar.TotalDays;
		return TextCommandResult.Success(Lang.Get("temporalstorm-cmd-daysleft", num2));
	}

	private void Event_PlayerNowPlaying(IServerPlayer byPlayer)
	{
		if (sapi.WorldManager.SaveGame.IsNew && stormsEnabled)
		{
			double num = data.nextStormTotalDays - api.World.Calendar.TotalDays;
			byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("{0} days until the first temporal storm.", (int)num), EnumChatType.Notification);
		}
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		byPlayer.Entity.OnCanSpawnNearby = (EntityProperties type, Vec3d spawnPos, RuntimeSpawnConditions sc) => CanSpawnNearby(byPlayer, type, spawnPos, sc);
		serverChannel.SendPacket(data, byPlayer);
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("temporalStormData", SerializerUtil.Serialize(data));
	}

	private void onTempStormTick(float dt)
	{
		if (config == null)
		{
			return;
		}
		if (!stormsEnabled)
		{
			data.stormGlitchStrength = 0f;
			data.nowStormActive = false;
			return;
		}
		if (data.nowStormActive)
		{
			trySpawnMobs();
		}
		double num = data.nextStormTotalDays - api.World.Calendar.TotalDays;
		if (num > 0.03 && num < 0.35 && data.stormDayNotify > 1)
		{
			data.stormDayNotify = 1;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Approaching, EnumChatType.Notification);
		}
		if (num <= 0.02 && data.stormDayNotify > 0)
		{
			data.stormDayNotify = 0;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Imminent, EnumChatType.Notification);
		}
		if (!(num <= 0.0))
		{
			return;
		}
		float num2 = (float)api.World.Config.GetDecimal("tempstormDurationMul", 1.0);
		double num3 = (0.10000000149011612 + data.nextStormStrDouble * 0.10000000149011612) * (double)num2;
		if (!data.nowStormActive && num + num3 < 0.0)
		{
			prepareNextStorm();
			serverChannel.BroadcastPacket(data);
			return;
		}
		if (!data.nowStormActive)
		{
			data.stormActiveTotalDays = api.World.Calendar.TotalDays + num3;
			data.stormGlitchStrength = 0.53f + (float)api.World.Rand.NextDouble() / 10f;
			if (data.nextStormStrength == EnumTempStormStrength.Medium)
			{
				data.stormGlitchStrength = 0.67f + (float)api.World.Rand.NextDouble() / 10f;
			}
			if (data.nextStormStrength == EnumTempStormStrength.Heavy)
			{
				data.stormGlitchStrength = 0.9f + (float)api.World.Rand.NextDouble() / 10f;
			}
			data.nowStormActive = true;
			serverChannel.BroadcastPacket(data);
			foreach (Entity value in ((CachingConcurrentDictionary<long, Entity>)(api.World as IServerWorldAccessor).LoadedEntities).Values)
			{
				if (stormMobCache.Contains(value.Code))
				{
					value.Attributes.SetBool("ignoreDaylightFlee", value: true);
				}
			}
		}
		double num4 = data.stormActiveTotalDays - api.World.Calendar.TotalDays;
		if (num4 < 0.02 && data.stormDayNotify == 0)
		{
			data.stormDayNotify = -1;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Waning, EnumChatType.Notification);
		}
		if (!(num4 < 0.0))
		{
			return;
		}
		data.stormGlitchStrength = 0f;
		data.nowStormActive = false;
		data.stormDayNotify = 99;
		prepareNextStorm();
		serverChannel.BroadcastPacket(data);
		foreach (Entity value2 in ((CachingConcurrentDictionary<long, Entity>)(api.World as IServerWorldAccessor).LoadedEntities).Values)
		{
			if (stormMobCache.Contains(value2.Code))
			{
				value2.Attributes.RemoveAttribute("ignoreDaylightFlee");
				if (api.World.Rand.NextDouble() < 0.5)
				{
					sapi.World.DespawnEntity(value2, new EntityDespawnData
					{
						Reason = EnumDespawnReason.Expire
					});
				}
			}
		}
	}

	private void prepareNextStorm()
	{
		if (config == null)
		{
			return;
		}
		double num = Math.Min(config.StrengthIncreaseCap, (double)config.StrengthIncrease * api.World.Calendar.TotalDays / (double)config.Frequency.avg);
		double num2 = api.World.Config.GetDecimal("tempStormFrequencyMul", 1.0);
		data.nextStormTotalDays = api.World.Calendar.TotalDays + (double)config.Frequency.nextFloat(1f, api.World.Rand) / (1.0 + num / 3.0) / num2;
		double val = num + api.World.Rand.NextDouble() * api.World.Rand.NextDouble() * (double)(float)num * 5.0;
		int nextStormStrength = (int)Math.Min(2.0, val);
		data.nextStormStrength = (EnumTempStormStrength)nextStormStrength;
		data.nextStormStrDouble = Math.Max(0.0, num);
		Dictionary<string, TempStormMobConfig.TempStormSpawnPattern> patterns = mobConfig.spawnsByStormStrength.spawnPatterns;
		string[] array = patterns.Keys.ToArray().Shuffle(sapi.World.Rand);
		float num3 = array.Sum((string code) => patterns[code].Weight);
		double num4 = sapi.World.Rand.NextDouble() * (double)num3;
		foreach (string text in array)
		{
			TempStormMobConfig.TempStormSpawnPattern tempStormSpawnPattern = patterns[text];
			num4 -= (double)tempStormSpawnPattern.Weight;
			if (num4 <= 0.0)
			{
				data.spawnPatternCode = text;
			}
		}
		data.rareSpawnCount = new Dictionary<string, int>();
		TempStormMobConfig.RareStormSpawnsVariant[] variants = mobConfig.rareSpawns.Variants;
		foreach (TempStormMobConfig.RareStormSpawnsVariant rareStormSpawnsVariant in variants)
		{
			data.rareSpawnCount[rareStormSpawnsVariant.Code] = GameMath.RoundRandom(sapi.World.Rand, rareStormSpawnsVariant.ChancePerStorm);
		}
		rareSpawnsCountByCodeByPlayer.Clear();
	}

	private void trySpawnMobs()
	{
		float stormStrength = StormStrength;
		if (stormStrength < 0.01f || api.World.Rand.NextDouble() < 0.5 || spawnBreakUntilMs > api.World.ElapsedMilliseconds)
		{
			return;
		}
		EntityPartitioning modSystem = api.ModLoader.GetModSystem<EntityPartitioning>();
		int range = 15;
		nobreakSpawns++;
		if (api.World.Rand.NextDouble() + 0.03999999910593033 < (double)((float)nobreakSpawns / 100f))
		{
			spawnBreakUntilMs = api.World.ElapsedMilliseconds + 1000 * api.World.Rand.Next(15);
		}
		IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
		foreach (IPlayer plr in allOnlinePlayers)
		{
			if (!(api.World.Rand.NextDouble() < 0.7))
			{
				trySpawnForPlayer(plr, range, stormStrength, modSystem);
			}
		}
	}

	private void trySpawnForPlayer(IPlayer plr, int range, float stormStr, EntityPartitioning part)
	{
		Vec3d vec3d = new Vec3d();
		BlockPos blockPos = new BlockPos();
		TempStormMobConfig.RareStormSpawnsVariant[] rareSpawns = mobConfig.rareSpawns.Variants.Shuffle(api.World.Rand);
		TempStormMobConfig.TempStormSpawnPattern tempStormSpawnPattern = mobConfig.spawnsByStormStrength.spawnPatterns[data.spawnPatternCode];
		Dictionary<string, AssetLocation[]> variantGroups = mobConfig.spawnsByStormStrength.variantGroups;
		Dictionary<string, float> variantQuantityMuls = mobConfig.spawnsByStormStrength.variantQuantityMuls;
		Dictionary<string, EntityProperties[]> resolvedVariantGroups = mobConfig.spawnsByStormStrength.resolvedVariantGroups;
		Dictionary<string, int> rareSpawnCounts = new Dictionary<string, int>();
		Dictionary<string, int> mainSpawnCountsByGroup = new Dictionary<string, int>();
		Vec3d xYZ = plr.Entity.ServerPos.XYZ;
		part.WalkEntities(xYZ, range + 30, delegate(Entity e)
		{
			foreach (KeyValuePair<string, AssetLocation[]> item in variantGroups)
			{
				if (item.Value.Contains(e.Code))
				{
					mainSpawnCountsByGroup.TryGetValue(item.Key, out var value8);
					mainSpawnCountsByGroup[item.Key] = value8 + 1;
				}
			}
			for (int i = 0; i < rareSpawns.Length; i++)
			{
				if (rareSpawns[i].Code.Equals(e.Code))
				{
					rareSpawnCounts.TryGetValue(rareSpawns[i].GroupCode, out var value9);
					rareSpawnCounts[rareSpawns[i].GroupCode] = value9 + 1;
					break;
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		if (!rareSpawnsCountByCodeByPlayer.TryGetValue(plr.PlayerUID, out var value))
		{
			Dictionary<string, int> dictionary = (rareSpawnsCountByCodeByPlayer[plr.PlayerUID] = new Dictionary<string, int>());
			value = dictionary;
		}
		foreach (KeyValuePair<string, int> item2 in rareSpawnCounts)
		{
			value.TryGetValue(item2.Key, out var value2);
			rareSpawnCounts.TryGetValue(item2.Key, out var value3);
			value[item2.Key] = value3 + value2;
		}
		foreach (KeyValuePair<string, float> groupWeight in tempStormSpawnPattern.GroupWeights)
		{
			int num = (int)Math.Round((2f + stormStr * 8f) * groupWeight.Value);
			if (variantQuantityMuls.TryGetValue(groupWeight.Key, out var value4))
			{
				num = (int)Math.Round((float)num * value4);
			}
			mainSpawnCountsByGroup.TryGetValue(groupWeight.Key, out var value5);
			if (value5 >= num)
			{
				continue;
			}
			EntityProperties[] array = resolvedVariantGroups[groupWeight.Key];
			int num2 = 10;
			int num3 = 0;
			while (num2-- > 0 && num3 < 2)
			{
				float value6 = (stormStr * 0.15f + (float)api.World.Rand.NextDouble() * (0.3f + stormStr / 2f)) * (float)array.Length;
				int num4 = GameMath.RoundRandom(api.World.Rand, value6);
				EntityProperties entityProperties = array[GameMath.Clamp(num4, 0, array.Length - 1)];
				if ((num4 == 3 || num4 == 4) && api.World.Rand.NextDouble() < 0.2)
				{
					for (int num5 = 0; num5 < rareSpawns.Length; num5++)
					{
						value.TryGetValue(rareSpawns[num5].GroupCode, out var value7);
						if (value7 == 0)
						{
							entityProperties = rareSpawns[num5].ResolvedCode;
							num2 = -1;
							break;
						}
					}
				}
				int num6 = api.World.Rand.Next(2 * range) - range;
				int num7 = api.World.Rand.Next(2 * range) - range;
				int num8 = api.World.Rand.Next(2 * range) - range;
				vec3d.Set((double)((int)xYZ.X + num6) + 0.5, (double)((int)xYZ.Y + num7) + 0.001, (double)((int)xYZ.Z + num8) + 0.5);
				blockPos.Set((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
				while (api.World.BlockAccessor.GetBlock(blockPos.X, blockPos.Y - 1, blockPos.Z).Id == 0 && vec3d.Y > 0.0)
				{
					blockPos.Y--;
					vec3d.Y -= 1.0;
				}
				if (api.World.BlockAccessor.IsValidPos((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z))
				{
					Cuboidf entityBoxRel = entityProperties.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
					if (!collisionTester.IsColliding(api.World.BlockAccessor, entityBoxRel, vec3d, alsoCheckTouch: false))
					{
						DoSpawn(entityProperties, vec3d, 0L);
						num3++;
					}
				}
			}
		}
	}

	private void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
	{
		Entity entity = api.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent entityAgent)
		{
			entityAgent.HerdId = herdid;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)api.World.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "timedistortion");
		api.World.SpawnEntity(entity);
		entity.WatchedAttributes.SetDouble("temporalStability", GameMath.Clamp(1f - 1.5f * StormStrength, 0f, 1f));
		entity.Attributes.SetBool("ignoreDaylightFlee", value: true);
		if (entity.GetBehavior("timeddespawn") is ITimedDespawn timedDespawn)
		{
			timedDespawn.SetDespawnByCalendarDate(data.stormActiveTotalDays + 0.1 * (double)StormStrength * api.World.Rand.NextDouble());
		}
	}

	private bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
	{
		if (mobConfig == null || !stormMobCache.Contains(properties.Code))
		{
			return true;
		}
		IPlayer player = api.World.NearestPlayer(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);
		if (player == null)
		{
			return true;
		}
		double val = player.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
		val = Math.Min(val, 1f - 1f * data.stormGlitchStrength);
		if (val < 0.25)
		{
			int num = -1;
			foreach (KeyValuePair<string, AssetLocation[]> variantGroup in mobConfig.spawnsByStormStrength.variantGroups)
			{
				for (int i = 0; i < variantGroup.Value.Length; i++)
				{
					if (variantGroup.Value[i].Equals(properties.Code))
					{
						num = i;
						_ = variantGroup.Key;
						break;
					}
				}
			}
			if (num == -1)
			{
				return true;
			}
			EntityProperties[] array = null;
			TempStormMobConfig.TempStormSpawnPattern tempStormSpawnPattern = mobConfig.spawnsByStormStrength.spawnPatterns[data.spawnPatternCode];
			float num2 = tempStormSpawnPattern.GroupWeights.Sum((KeyValuePair<string, float> w) => w.Value);
			double num3 = sapi.World.Rand.NextDouble() * (double)num2;
			foreach (KeyValuePair<string, float> groupWeight in tempStormSpawnPattern.GroupWeights)
			{
				num3 -= (double)groupWeight.Value;
				if (num3 <= 0.0)
				{
					array = mobConfig.spawnsByStormStrength.resolvedVariantGroups[groupWeight.Key];
				}
			}
			int num4 = (int)Math.Round((0.25 - val) * 15.0);
			int num5 = Math.Min(num + num4, array.Length - 1);
			properties = array[num5];
		}
		return true;
	}

	private void LoadNoise()
	{
		if (api.Side == EnumAppSide.Server)
		{
			updateOldWorlds();
		}
		temporalStabilityEnabled = api.World.Config.GetBool("temporalStability", defaultValue: true);
		if (!temporalStabilityEnabled)
		{
			return;
		}
		stabilityNoise = SimplexNoise.FromDefaultOctaves(4, 0.1, 0.9, api.World.Seed);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		worldConfigStorminess = api.World.Config.GetString("temporalStorms");
		stormsEnabled = worldConfigStorminess != "off";
		if (worldConfigStorminess != null && configs.ContainsKey(worldConfigStorminess))
		{
			config = configs[worldConfigStorminess];
		}
		else
		{
			string playStyle = sapi.WorldManager.SaveGame.PlayStyle;
			if (playStyle == "surviveandbuild" || playStyle == "wildernesssurvival")
			{
				config = configs["rare"];
			}
			else
			{
				config = null;
			}
		}
		sapi.Event.OnEntityDeath += Event_OnEntityDeath;
		mobConfig = sapi.Assets.Get("config/mobextraspawns.json").ToObject<MobExtraSpawnsTemp>().temporalStormSpawns;
		Dictionary<string, EntityProperties[]> dictionary = (mobConfig.spawnsByStormStrength.resolvedVariantGroups = new Dictionary<string, EntityProperties[]>());
		foreach (KeyValuePair<string, AssetLocation[]> variantGroup in mobConfig.spawnsByStormStrength.variantGroups)
		{
			int num = 0;
			dictionary[variantGroup.Key] = new EntityProperties[variantGroup.Value.Length];
			AssetLocation[] value = variantGroup.Value;
			foreach (AssetLocation assetLocation in value)
			{
				dictionary[variantGroup.Key][num++] = sapi.World.GetEntityType(assetLocation);
				stormMobCache.Add(assetLocation);
			}
		}
		TempStormMobConfig.RareStormSpawnsVariant[] variants = mobConfig.rareSpawns.Variants;
		foreach (TempStormMobConfig.RareStormSpawnsVariant rareStormSpawnsVariant in variants)
		{
			rareStormSpawnsVariant.ResolvedCode = sapi.World.GetEntityType(rareStormSpawnsVariant.Code);
			stormMobCache.Add(rareStormSpawnsVariant.Code);
		}
	}

	internal float GetGlitchEffectExtraStrength()
	{
		if (!data.nowStormActive)
		{
			return modGlitchStrength;
		}
		return data.stormGlitchStrength + modGlitchStrength;
	}

	private void Event_OnEntityDeath(Entity entity, DamageSource damageSource)
	{
		Entity entity2 = damageSource?.GetCauseEntity();
		if (entity2 != null && entity2.WatchedAttributes.HasAttribute("temporalStability") && entity.Properties.Attributes != null)
		{
			float num = entity.Properties.Attributes["onDeathStabilityRecovery"].AsFloat();
			double num2 = entity2.WatchedAttributes.GetDouble("temporalStability", 1.0);
			entity2.WatchedAttributes.SetDouble("temporalStability", Math.Min(1.0, num2 + (double)num));
		}
	}

	public float GetTemporalStability(BlockPos pos)
	{
		return GetTemporalStability(pos.X, pos.Y, pos.Z);
	}

	public float GetTemporalStability(Vec3d pos)
	{
		return GetTemporalStability(pos.X, pos.Y, pos.Z);
	}

	public bool CanSpawnNearby(IPlayer byPlayer, EntityProperties type, Vec3d spawnPosition, RuntimeSpawnConditions sc)
	{
		int lightLevel = api.World.BlockAccessor.GetLightLevel((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z, sc.LightLevelType);
		JsonObject attributes = type.Attributes;
		if (attributes != null && attributes["spawnCloserDuringLowStability"].AsBool())
		{
			double val = ((!temporalStabilityEnabled) ? 1.0 : Math.Min(1.0, 4.0 * byPlayer.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0)));
			val = Math.Min(val, Math.Max(0f, 1f - 2f * data.stormGlitchStrength));
			if ((double)lightLevel * val > (double)sc.MaxLightLevel || (double)lightLevel * val < (double)sc.MinLightLevel)
			{
				return false;
			}
			if (api.World.BlockAccessor.GetLightLevel((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z, EnumLightLevelType.OnlySunLight) >= 16)
			{
				float num = NearestRiftDistance(spawnPosition);
				if (api.World.Calendar.GetSunPosition(spawnPosition, api.World.Calendar.TotalDays).Y >= 0f)
				{
					if (num < 6f)
					{
						return api.World.Rand.NextDouble() < 0.07;
					}
					return false;
				}
				return num < 20f;
			}
			double num2 = byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition);
			if (val < 0.5)
			{
				return num2 < 100.0;
			}
			return num2 > (double)(sc.MinDistanceToPlayer * sc.MinDistanceToPlayer) * val;
		}
		if (sc.MinLightLevel > lightLevel || sc.MaxLightLevel < lightLevel)
		{
			return false;
		}
		return byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition) > (double)(sc.MinDistanceToPlayer * sc.MinDistanceToPlayer);
	}

	private float NearestRiftDistance(Vec3d pos)
	{
		return riftSys.ServerRifts.Nearest((Rift rift) => rift.Position.SquareDistanceTo(pos))?.Position.DistanceTo(pos) ?? 9999f;
	}

	public float GetTemporalStability(double x, double y, double z)
	{
		if (!temporalStabilityEnabled)
		{
			return 2f;
		}
		float num = (float)GameMath.Clamp(stabilityNoise.Noise(x / 80.0, y / 80.0, z / 80.0) * 1.2000000476837158 + 0.10000000149011612, -1.0, 2.0);
		float num2 = (float)((double)TerraGenConfig.seaLevel - y);
		float v = GameMath.Clamp(1.6f + num, 0.8f, 1.5f);
		float t = (float)GameMath.Clamp(Math.Pow(Math.Max(0f, (float)y) / (float)TerraGenConfig.seaLevel, 2.0), 0.0, 1.0);
		num = GameMath.Mix(num, v, t);
		num -= Math.Max(0f, num2 / (float)api.World.BlockAccessor.MapSizeY) / 3.5f;
		num = GameMath.Clamp(num, 0f, 1.5f);
		float num3 = 1.5f * GetGlitchEffectExtraStrength();
		float num4 = GameMath.Clamp(num - num3, 0f, 1.5f);
		if (this.OnGetTemporalStability != null)
		{
			num4 = this.OnGetTemporalStability(num4, x, y, z);
		}
		return num4;
	}

	private void updateOldWorlds()
	{
		if (!api.World.Config.HasAttribute("temporalStorms"))
		{
			if (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival")
			{
				api.World.Config.SetString("temporalStorms", "often");
			}
			if (sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild")
			{
				api.World.Config.SetString("temporalStorms", "rare");
			}
		}
		if (!api.World.Config.HasAttribute("temporalStability") && (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival" || sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild"))
		{
			api.World.Config.SetBool("temporalStability", value: true);
		}
	}
}
