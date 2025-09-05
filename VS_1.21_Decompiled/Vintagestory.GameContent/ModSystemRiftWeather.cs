using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemRiftWeather : ModSystem
{
	private ICoreServerAPI sapi;

	private Dictionary<string, EntityProperties> drifterProps = new Dictionary<string, EntityProperties>();

	private Dictionary<string, int> defaultSpawnCaps = new Dictionary<string, int>();

	private CurrentPattern curPattern;

	private RiftWeatherConfig config;

	private Dictionary<string, SpawnPattern> patternsByCode = new Dictionary<string, SpawnPattern>();

	public SpawnPattern CurrentPattern => patternsByCode[curPattern.Code];

	public double CurrentPatternUntilHours => curPattern.UntilTotalHours;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("riftWeather").RegisterMessageType<SpawnPatternPacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("riftWeather").SetMessageHandler<SpawnPatternPacket>(onPacket);
		api.ModLoader.GetModSystem<CharacterExtraDialogs>().OnEnvText += ModSystemDrifterWeather_OnEnvText;
	}

	private void ModSystemDrifterWeather_OnEnvText(StringBuilder sb)
	{
		if (curPattern != null)
		{
			sb.AppendLine();
			sb.Append(Lang.Get("Rift activity: {0}", Lang.Get("rift-activity-" + curPattern.Code)));
		}
	}

	private void onPacket(SpawnPatternPacket msg)
	{
		curPattern = msg.Pattern;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		config = api.Assets.Get("config/riftweather.json").ToObject<RiftWeatherConfig>();
		SpawnPattern[] patterns = config.Patterns;
		foreach (SpawnPattern spawnPattern in patterns)
		{
			patternsByCode[spawnPattern.Code] = spawnPattern;
		}
		sapi = api;
		base.StartServerSide(api);
		api.Event.RegisterGameTickListener(onServerTick, 2000);
		api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, onRunGame);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.SaveGameCreated += Event_SaveGameCreated;
		api.Event.PlayerJoin += Event_PlayerJoin;
		sapi.ChatCommands.Create("dweather").WithDescription("Show the current rift activity").RequiresPrivilege(Privilege.controlserver)
			.HandleWith((TextCommandCallingArgs _) => TextCommandResult.Success("Current rift activity: " + curPattern.Code));
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		sapi.Network.GetChannel("riftWeather").SendPacket(new SpawnPatternPacket
		{
			Pattern = curPattern
		}, byPlayer);
	}

	private void Event_SaveGameCreated()
	{
		choosePattern();
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("riftweather", SerializerUtil.Serialize(curPattern));
	}

	private void Event_SaveGameLoaded()
	{
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("riftweather");
			if (data == null)
			{
				choosePattern();
				return;
			}
			curPattern = SerializerUtil.Deserialize<CurrentPattern>(data);
		}
		catch
		{
			choosePattern();
		}
		if (curPattern.Code == null)
		{
			choosePattern();
		}
	}

	private void choosePattern()
	{
		float num = 0f;
		List<SpawnPattern> list = new List<SpawnPattern>();
		double totalHours = sapi.World.Calendar.TotalHours;
		for (int i = 0; i < config.Patterns.Length; i++)
		{
			SpawnPattern spawnPattern = config.Patterns[i];
			if (spawnPattern.StartTotalHours < totalHours)
			{
				list.Add(spawnPattern);
				num += spawnPattern.Chance;
			}
		}
		SpawnPattern spawnPattern2 = list[list.Count - 1];
		float num2 = (float)sapi.World.Rand.NextDouble() * num;
		foreach (SpawnPattern item in list)
		{
			num2 -= item.Chance;
			if (num2 <= 0f)
			{
				spawnPattern2 = item;
				break;
			}
		}
		curPattern = new CurrentPattern
		{
			Code = spawnPattern2.Code,
			UntilTotalHours = totalHours + (double)spawnPattern2.DurationHours.nextFloat(1f, sapi.World.Rand)
		};
		sapi.Network.GetChannel("riftWeather").BroadcastPacket(new SpawnPatternPacket
		{
			Pattern = curPattern
		});
	}

	private void onRunGame()
	{
		foreach (EntityProperties entityType in sapi.World.EntityTypes)
		{
			if (entityType.Code.Path == "drifter-normal" || entityType.Code.Path == "drifter-deep")
			{
				drifterProps[entityType.Code.Path] = entityType;
				defaultSpawnCaps[entityType.Code.Path] = entityType.Server.SpawnConditions.Runtime.MaxQuantity;
				break;
			}
		}
	}

	private void onServerTick(float dt)
	{
		if (drifterProps == null)
		{
			return;
		}
		float mobSpawnMul = patternsByCode[curPattern.Code].MobSpawnMul;
		foreach (KeyValuePair<string, EntityProperties> drifterProp in drifterProps)
		{
			drifterProp.Value.Server.SpawnConditions.Runtime.MaxQuantity = Math.Max(0, (int)((float)defaultSpawnCaps[drifterProp.Key] * mobSpawnMul));
		}
		if (curPattern.UntilTotalHours < sapi.World.Calendar.TotalHours)
		{
			choosePattern();
		}
	}
}
