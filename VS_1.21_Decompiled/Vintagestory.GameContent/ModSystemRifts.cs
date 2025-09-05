using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemRifts : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private RiftRenderer renderer;

	public Dictionary<int, Rift> riftsById = new Dictionary<int, Rift>();

	private List<Rift> serverRifts = new List<Rift>();

	public ILoadedSound[] riftSounds = new ILoadedSound[4];

	public Rift[] nearestRifts = Array.Empty<Rift>();

	public IServerNetworkChannel schannel;

	public int despawnDistance = 190;

	public int spawnMinDistance = 16;

	public int spawnAddDistance = 180;

	private bool riftsEnabled = true;

	private Dictionary<string, long> chunkIndexbyPlayer = new Dictionary<string, long>();

	private ModSystemRiftWeather modRiftWeather;

	private string riftMode;

	private int riftId;

	public List<Rift> ServerRifts => serverRifts;

	public int NextRiftId => riftId++;

	public event OnTrySpawnRiftDelegate OnTrySpawnRift;

	public event OnRiftSpawnedDelegate OnRiftSpawned;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Network.RegisterChannel("rifts").RegisterMessageType<RiftList>();
		modRiftWeather = api.ModLoader.GetModSystem<ModSystemRiftWeather>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		renderer = new RiftRenderer(api, riftsById);
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.RegisterGameTickListener(onClientTick, 100);
		api.Network.GetChannel("rifts").SetMessageHandler<RiftList>(onRifts);
	}

	private void onRifts(RiftList riftlist)
	{
		HashSet<int> hashSet = new HashSet<int>();
		hashSet.AddRange(riftsById.Keys);
		foreach (Rift rift in riftlist.rifts)
		{
			hashSet.Remove(rift.RiftId);
			if (riftsById.TryGetValue(rift.RiftId, out var value))
			{
				value.SetFrom(rift);
			}
			else
			{
				riftsById[rift.RiftId] = rift;
			}
		}
		foreach (int item in hashSet)
		{
			riftsById.Remove(item);
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.RegisterGameTickListener(OnServerTick100ms, 101);
		api.Event.RegisterGameTickListener(OnServerTick3s, 2999);
		setupCommands();
		schannel = sapi.Network.GetChannel("rifts");
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (riftsEnabled)
		{
			BroadCastRifts(byPlayer);
		}
	}

	private void OnServerTick100ms(float dt)
	{
		if (riftMode != "visible")
		{
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
			if (serverPlayer.ConnectionState != EnumClientState.Playing)
			{
				continue;
			}
			EntityBehaviorTemporalStabilityAffected behavior = serverPlayer.Entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
			if (behavior == null)
			{
				continue;
			}
			behavior.stabilityOffset = 0.0;
			Vec3d plrPos = serverPlayer.Entity.Pos.XYZ;
			Rift rift = riftsById.Values.Where((Rift r) => r.Size > 0f).Nearest((Rift r) => r.Position.SquareDistanceTo(plrPos));
			if (rift != null)
			{
				float num = Math.Max(0f, GameMath.Sqrt(plrPos.SquareDistanceTo(rift.Position)) - 2f - rift.Size / 2f);
				if (behavior != null)
				{
					behavior.stabilityOffset = (0.0 - Math.Pow(Math.Max(0f, 1f - num / 3f), 2.0)) * 20.0;
				}
			}
		}
	}

	private void OnServerTick3s(float dt)
	{
		if (!riftsEnabled)
		{
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		Dictionary<string, List<Rift>> dictionary = new Dictionary<string, List<Rift>>();
		IPlayer[] array = allOnlinePlayers;
		foreach (IPlayer player in array)
		{
			dictionary[player.PlayerUID] = new List<Rift>();
		}
		if (KillOldRifts(dictionary) | SpawnNewRifts(dictionary))
		{
			BroadCastRifts();
		}
		else
		{
			array = allOnlinePlayers;
			foreach (IPlayer player2 in array)
			{
				long playerChunkIndex = getPlayerChunkIndex(player2);
				if (!chunkIndexbyPlayer.ContainsKey(player2.PlayerUID) || chunkIndexbyPlayer[player2.PlayerUID] != playerChunkIndex)
				{
					BroadCastRifts(player2);
				}
				chunkIndexbyPlayer[player2.PlayerUID] = playerChunkIndex;
			}
		}
		UpdateServerRiftList();
	}

	private void UpdateServerRiftList()
	{
		List<Rift> list = new List<Rift>();
		foreach (Rift value in riftsById.Values)
		{
			list.Add(value);
		}
		serverRifts = list;
	}

	private bool SpawnNewRifts(Dictionary<string, List<Rift>> nearbyRiftsByPlayerUid)
	{
		Dictionary<string, List<Rift>>.KeyCollection keys = nearbyRiftsByPlayerUid.Keys;
		int num = 0;
		double totalDays = api.World.Calendar.TotalDays;
		foreach (string item in keys)
		{
			float riftCap = GetRiftCap(item);
			IPlayer player = api.World.PlayerByUid(item);
			if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				continue;
			}
			int count = nearbyRiftsByPlayerUid[item].Count;
			int num2 = (int)(riftCap - (float)count);
			float num3 = riftCap - (float)count - (float)num2;
			if (api.World.Rand.NextDouble() < (double)num3 / 50.0)
			{
				num2++;
			}
			if (num2 <= 0)
			{
				continue;
			}
			EntityPos pos = player.Entity.Pos;
			if (totalDays < 2.0 && api.World.Calendar.GetDayLightStrength(pos.X, pos.Z) > 0.9f)
			{
				continue;
			}
			for (int i = 0; i < num2; i++)
			{
				double num4 = (double)spawnMinDistance + api.World.Rand.NextDouble() * (double)spawnAddDistance;
				double num5 = api.World.Rand.NextDouble() * 6.2831854820251465;
				double z = num4 * Math.Sin(num5);
				double x = num4 * Math.Cos(num5);
				Vec3d vec3d = pos.XYZ.Add(x, 0.0, z);
				BlockPos blockPos = new BlockPos((int)vec3d.X, 0, (int)vec3d.Z);
				blockPos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(blockPos);
				if (!canSpawnRiftAt(blockPos))
				{
					continue;
				}
				float num6 = 2f + (float)api.World.Rand.NextDouble() * 4f;
				vec3d.Y = (float)blockPos.Y + num6 / 2f + 1f;
				Rift rift = new Rift
				{
					RiftId = NextRiftId,
					Position = vec3d,
					Size = num6,
					SpawnedTotalHours = api.World.Calendar.TotalHours,
					DieAtTotalHours = api.World.Calendar.TotalHours + 8.0 + api.World.Rand.NextDouble() * 48.0
				};
				this.OnRiftSpawned?.Invoke(rift);
				riftsById[rift.RiftId] = rift;
				num++;
				foreach (string item2 in keys)
				{
					_ = item2;
					if (player.Entity.Pos.HorDistanceTo(vec3d) <= (double)despawnDistance)
					{
						nearbyRiftsByPlayerUid[player.PlayerUID].Add(rift);
					}
				}
			}
		}
		return num > 0;
	}

	private bool canSpawnRiftAt(BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlock(pos).Replaceable < 6000)
		{
			pos.Up();
		}
		if (api.World.BlockAccessor.GetBlock(pos, 2).IsLiquid() && api.World.Rand.NextDouble() > 0.1)
		{
			return false;
		}
		int lightLevel = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlyBlockLight);
		int lightLevel2 = api.World.BlockAccessor.GetLightLevel(pos.UpCopy(), EnumLightLevelType.OnlyBlockLight);
		int lightLevel3 = api.World.BlockAccessor.GetLightLevel(pos.UpCopy(2), EnumLightLevelType.OnlyBlockLight);
		if (lightLevel >= 3 || lightLevel2 >= 3 || lightLevel3 >= 3)
		{
			return false;
		}
		bool result = true;
		if (this.OnTrySpawnRift != null)
		{
			Delegate[] invocationList = this.OnTrySpawnRift.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				OnTrySpawnRiftDelegate obj = (OnTrySpawnRiftDelegate)invocationList[i];
				EnumHandling handling = EnumHandling.PassThrough;
				obj(pos, ref handling);
				if (handling != EnumHandling.PreventSubsequent && handling == EnumHandling.PreventDefault)
				{
					result = false;
				}
			}
		}
		return result;
	}

	private float GetRiftCap(string playerUid)
	{
		EntityPos pos = api.World.PlayerByUid(playerUid).Entity.Pos;
		float dayLightStrength = api.World.Calendar.GetDayLightStrength(pos.X, pos.Z);
		return 8f * modRiftWeather.CurrentPattern.MobSpawnMul * GameMath.Clamp(1.1f - dayLightStrength, 0.45f, 1f);
	}

	private bool KillOldRifts(Dictionary<string, List<Rift>> nearbyRiftsByPlayerUid)
	{
		bool result = false;
		double totalHours = api.World.Calendar.TotalHours;
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Rift rift in riftsById.Values)
		{
			if (rift.DieAtTotalHours <= totalHours)
			{
				hashSet.Add(rift.RiftId);
				result = true;
				continue;
			}
			List<IPlayer> list = allOnlinePlayers.InRange((IPlayer player) => player.Entity.Pos.HorDistanceTo(rift.Position), despawnDistance);
			if (list.Count == 0)
			{
				rift.DieAtTotalHours = Math.Min(rift.DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
				result = true;
				continue;
			}
			foreach (IPlayer item in list)
			{
				nearbyRiftsByPlayerUid[item.PlayerUID].Add(rift);
			}
		}
		foreach (int item2 in hashSet)
		{
			riftsById.Remove(item2);
		}
		foreach (KeyValuePair<string, List<Rift>> item3 in nearbyRiftsByPlayerUid)
		{
			float riftCap = GetRiftCap(item3.Key);
			float num = (float)item3.Value.Count - riftCap;
			if (!(num <= 0f))
			{
				_ = api.World.PlayerByUid(item3.Key).Entity.Pos.XYZ;
				List<Rift> list2 = item3.Value.OrderBy((Rift rift2) => rift2.DieAtTotalHours).ToList();
				for (int num2 = 0; num2 < Math.Min(list2.Count, (int)num); num2++)
				{
					list2[num2].DieAtTotalHours = Math.Min(list2[num2].DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
					result = true;
				}
			}
		}
		return result;
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("rifts", riftsById);
	}

	private void Event_SaveGameLoaded()
	{
		riftMode = api.World.Config.GetString("temporalRifts", "visible");
		riftsEnabled = riftMode != "off";
		if (riftsEnabled)
		{
			try
			{
				riftsById = sapi.WorldManager.SaveGame.GetData<Dictionary<int, Rift>>("rifts");
			}
			catch (Exception)
			{
			}
			if (riftsById == null)
			{
				riftsById = new Dictionary<int, Rift>();
			}
			else
			{
				UpdateServerRiftList();
			}
		}
	}

	public void BroadCastRifts(IPlayer onlyToPlayer = null)
	{
		if (riftMode != "visible")
		{
			return;
		}
		List<Rift> list = new List<Rift>();
		float num = (float)Math.Pow(despawnDistance + 10, 2.0);
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			if (onlyToPlayer != null && onlyToPlayer.PlayerUID != player.PlayerUID)
			{
				continue;
			}
			chunkIndexbyPlayer[player.PlayerUID] = getPlayerChunkIndex(player);
			IServerPlayer serverPlayer = player as IServerPlayer;
			Vec3d xYZ = serverPlayer.Entity.Pos.XYZ;
			foreach (Rift value in riftsById.Values)
			{
				if (value.Position.SquareDistanceTo(xYZ) < num)
				{
					list.Add(value);
				}
			}
			schannel.SendPacket(new RiftList
			{
				rifts = list
			}, serverPlayer);
			list.Clear();
		}
	}

	private long getPlayerChunkIndex(IPlayer plr)
	{
		EntityPos pos = plr.Entity.Pos;
		return (api as ICoreServerAPI).WorldManager.ChunkIndex3D((int)pos.X / 32, (int)pos.Y / 32, (int)pos.Z / 32);
	}

	private void onClientTick(float dt)
	{
		if (!riftsEnabled)
		{
			return;
		}
		Vec3d plrPos = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
		nearestRifts = (from rift2 in riftsById.Values
			where rift2.Size > 0f
			orderby rift2.Position.SquareDistanceTo(plrPos) + (float)((!rift2.HasLineOfSight) ? 400 : 0)
			select rift2).ToArray();
		for (int num = 0; num < Math.Min(4, nearestRifts.Length); num++)
		{
			Rift rift = nearestRifts[num];
			rift.OnNearTick(capi, dt);
			ILoadedSound loadedSound = riftSounds[num];
			if (!loadedSound.IsPlaying)
			{
				loadedSound.Start();
				loadedSound.PlaybackPosition = loadedSound.SoundLengthSeconds * (float)capi.World.Rand.NextDouble();
			}
			float num2 = GameMath.Clamp(rift.GetNowSize(capi) / 3f, 0.1f, 1f);
			loadedSound.SetVolume(num2 * rift.VolumeMul);
			loadedSound.SetPosition((float)rift.Position.X, (float)rift.Position.Y, (float)rift.Position.Z);
		}
		for (int num3 = nearestRifts.Length; num3 < 4; num3++)
		{
			if (riftSounds[num3].IsPlaying)
			{
				riftSounds[num3].Stop();
			}
		}
	}

	private void Event_LeaveWorld()
	{
		for (int i = 0; i < 4; i++)
		{
			riftSounds[i]?.Stop();
			riftSounds[i]?.Dispose();
		}
	}

	private void Event_BlockTexturesLoaded()
	{
		for (int i = 0; i < 4; i++)
		{
			riftSounds[i] = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/rift.ogg"),
				ShouldLoop = true,
				Position = null,
				DisposeOnFinish = false,
				Volume = 1f,
				Range = 24f,
				SoundType = EnumSoundType.AmbientGlitchunaffected
			});
		}
	}

	private void setupCommands()
	{
		sapi.ChatCommands.GetOrCreate("debug").BeginSub("rift").WithDesc("Rift debug commands")
			.WithAdditionalInformation("With no sub-command, simply counts the number of loaded rifts")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(riftsById.Count + " rifts loaded"))
			.BeginSub("clear")
			.WithDesc("Immediately remove all loaded rifts")
			.HandleWith(delegate
			{
				riftsById.Clear();
				serverRifts = new List<Rift>();
				BroadCastRifts();
				return TextCommandResult.Success("Rifts cleared");
			})
			.EndSub()
			.BeginSub("fade")
			.WithDesc("Slowly remove all loaded rifts, over a few minutes")
			.HandleWith(delegate
			{
				foreach (Rift value in riftsById.Values)
				{
					value.DieAtTotalHours = Math.Min(value.DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
				}
				UpdateServerRiftList();
				BroadCastRifts();
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("spawn")
			.WithDesc("Spawn the specified quantity of rifts")
			.WithArgs(sapi.ChatCommands.Parsers.OptionalInt("quantity", 200))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				int num = (int)args[0];
				for (int i = 0; i < num; i++)
				{
					double num2 = (double)spawnMinDistance + api.World.Rand.NextDouble() * (double)spawnAddDistance;
					double num3 = api.World.Rand.NextDouble() * 6.2831854820251465;
					double z = num2 * Math.Sin(num3);
					double x = num2 * Math.Cos(num3);
					Vec3d vec3d = args.Caller.Pos.AddCopy(x, 0.0, z);
					BlockPos blockPos = new BlockPos((int)vec3d.X, 0, (int)vec3d.Z);
					blockPos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(blockPos);
					if (!api.World.BlockAccessor.GetBlock(blockPos, 2).IsLiquid() || !(api.World.Rand.NextDouble() > 0.1))
					{
						float num4 = 2f + (float)api.World.Rand.NextDouble() * 4f;
						vec3d.Y = (float)blockPos.Y + num4 / 2f + 1f;
						Rift rift = new Rift
						{
							RiftId = NextRiftId,
							Position = vec3d,
							Size = num4,
							SpawnedTotalHours = api.World.Calendar.TotalHours,
							DieAtTotalHours = api.World.Calendar.TotalHours + 8.0 + api.World.Rand.NextDouble() * 48.0
						};
						this.OnRiftSpawned?.Invoke(rift);
						riftsById[rift.RiftId] = rift;
					}
				}
				UpdateServerRiftList();
				BroadCastRifts();
				return TextCommandResult.Success("ok, " + num + " spawned.");
			})
			.EndSub()
			.BeginSub("spawnhere")
			.WithDesc("Spawn one rift")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				Vec3d position = args.Caller.Pos.AddCopy(args.Caller.Entity?.LocalEyePos ?? new Vec3d());
				float size = 3f;
				Rift rift = new Rift
				{
					RiftId = NextRiftId,
					Position = position,
					Size = size,
					SpawnedTotalHours = api.World.Calendar.TotalHours,
					DieAtTotalHours = api.World.Calendar.TotalHours + 16.0 + api.World.Rand.NextDouble() * 48.0
				};
				this.OnRiftSpawned?.Invoke(rift);
				riftsById[rift.RiftId] = rift;
				UpdateServerRiftList();
				BroadCastRifts();
				return TextCommandResult.Success("ok, rift spawned.");
			})
			.EndSub()
			.EndSub();
	}
}
