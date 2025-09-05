using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskBellAlarm : AiTaskBase
{
	private string[] seekEntityCodesExact = new string[1] { "player" };

	private string[] seekEntityCodesBeginsWith = Array.Empty<string>();

	private int spawnRange;

	private float seekingRange = 12f;

	private EntityProperties[] spawnMobs;

	private Entity targetEntity;

	private AssetLocation repeatSoundLoc;

	private ICoreServerAPI sapi;

	private int spawnIntervalMsMin = 2000;

	private int spawnIntervalMsMax = 12000;

	private int spawnMaxQuantity = 5;

	private int nextSpawnIntervalMs;

	private List<Entity> spawnedEntities = new List<Entity>();

	private float spawnAccum;

	private CollisionTester collisionTester = new CollisionTester();

	public AiTaskBellAlarm(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		sapi = entity.World.Api as ICoreServerAPI;
		spawnRange = taskConfig["spawnRange"].AsInt(12);
		spawnIntervalMsMin = taskConfig["spawnIntervalMsMin"].AsInt(2500);
		spawnIntervalMsMax = taskConfig["spawnIntervalMsMax"].AsInt(12000);
		spawnMaxQuantity = taskConfig["spawnMaxQuantity"].AsInt(5);
		seekingRange = taskConfig["seekingRange"].AsFloat(12f);
		AssetLocation[] array = taskConfig["spawnMobs"].AsObject(Array.Empty<AssetLocation>());
		List<EntityProperties> list = new List<EntityProperties>();
		AssetLocation[] array2 = array;
		foreach (AssetLocation assetLocation in array2)
		{
			EntityProperties entityType = sapi.World.GetEntityType(assetLocation);
			if (entityType == null)
			{
				sapi.World.Logger.Warning("AiTaskBellAlarm defined spawnmob {0}, but no such entity type found, will ignore.", assetLocation);
			}
			else
			{
				list.Add(entityType);
			}
		}
		spawnMobs = list.ToArray();
		repeatSoundLoc = ((!taskConfig["repeatSound"].Exists) ? null : AssetLocation.Create(taskConfig["repeatSound"].AsString(), entity.Code.Domain).WithPathPrefixOnce("sounds/"));
		string[] array3 = taskConfig["onNearbyEntityCodes"].AsArray(new string[1] { "player" });
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		foreach (string text in array3)
		{
			if (text.EndsWith('*'))
			{
				list3.Add(text.Substring(0, text.Length - 1));
			}
			else
			{
				list2.Add(text);
			}
		}
		seekEntityCodesExact = list2.ToArray();
		seekEntityCodesBeginsWith = list3.ToArray();
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + mincooldownHours + entity.World.Rand.NextDouble() * (maxcooldownHours - mincooldownHours);
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() > 0.05)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		float num = seekingRange;
		bool listening = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.IsTaskActive("listen");
		num = (listening ? (num * 1.25f) : (num / 3f));
		targetEntity = entity.World.GetNearestEntity(entity.ServerPos.XYZ, num, num, delegate(Entity e)
		{
			if (!e.Alive || e.EntityId == entity.EntityId)
			{
				return false;
			}
			if (e is EntityPlayer entityPlayer)
			{
				IPlayer player = entityPlayer.Player;
				if (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					IPlayer player2 = (e as EntityPlayer).Player;
					if (player2 == null || player2.WorldData.CurrentGameMode != EnumGameMode.Spectator)
					{
						bool num2 = entityPlayer.ServerControls.TriesToMove || entityPlayer.ServerControls.LeftMouseDown || entityPlayer.ServerControls.RightMouseDown || entityPlayer.ServerControls.Jump || !entityPlayer.OnGround || entityPlayer.ServerControls.HandUse != EnumHandInteract.None;
						bool flag = entityPlayer.ServerControls.TriesToMove && !entityPlayer.ServerControls.LeftMouseDown && !entityPlayer.ServerControls.RightMouseDown && !entityPlayer.ServerControls.Jump && entityPlayer.OnGround && entityPlayer.ServerControls.HandUse == EnumHandInteract.None && entityPlayer.ServerControls.Sneak;
						if (!num2)
						{
							if (entityPlayer.Pos.DistanceTo(entity.Pos.XYZ) >= (double)(3 - (listening ? 1 : 0)))
							{
								return false;
							}
						}
						else if (flag && entityPlayer.Pos.DistanceTo(entity.Pos.XYZ) >= (double)(6 - (listening ? 3 : 0)))
						{
							return false;
						}
						return true;
					}
				}
			}
			return false;
		});
		if (targetEntity != null)
		{
			return true;
		}
		return false;
	}

	public override void StartExecute()
	{
		sapi.Network.BroadcastEntityPacket(entity.EntityId, 1025, SerializerUtil.Serialize(repeatSoundLoc));
		nextSpawnIntervalMs = spawnIntervalMsMin + entity.World.Rand.Next(spawnIntervalMsMax - spawnIntervalMsMin);
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		spawnAccum += dt;
		if (spawnAccum > (float)nextSpawnIntervalMs / 1000f)
		{
			float num = (float)sapi.World.GetPlayersAround(entity.ServerPos.XYZ, 15f, 10f, (IPlayer plr) => plr.Entity.Alive).Length * sapi.Server.Config.SpawnCapPlayerScaling;
			trySpawnCreatures(GameMath.RoundRandom(sapi.World.Rand, (float)spawnMaxQuantity * num), spawnRange);
			nextSpawnIntervalMs = spawnIntervalMsMin + entity.World.Rand.Next(spawnIntervalMsMax - spawnIntervalMsMin);
			spawnAccum = 0f;
		}
		if ((double)targetEntity.Pos.SquareDistanceTo(entity.Pos) > Math.Pow(seekingRange + 5f, 2.0))
		{
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		sapi.Network.BroadcastEntityPacket(entity.EntityId, 1026);
		base.FinishExecute(cancelled);
	}

	public override void OnEntityDespawn(EntityDespawnData reason)
	{
		sapi.Network.BroadcastEntityPacket(entity.EntityId, 1026);
		base.OnEntityDespawn(reason);
	}

	private void trySpawnCreatures(int maxquantity, int range = 13)
	{
		Vec3d xYZ = entity.Pos.XYZ;
		Vec3d vec3d = new Vec3d();
		BlockPos blockPos = new BlockPos();
		for (int i = 0; i < spawnedEntities.Count; i++)
		{
			if (spawnedEntities[i] == null || !spawnedEntities[i].Alive)
			{
				spawnedEntities.RemoveAt(i);
				i--;
			}
		}
		if (spawnedEntities.Count > maxquantity)
		{
			return;
		}
		int num = 50;
		int num2 = 0;
		while (num-- > 0 && num2 < 1)
		{
			int num3 = sapi.World.Rand.Next(spawnMobs.Length);
			EntityProperties entityProperties = spawnMobs[num3];
			int num4 = sapi.World.Rand.Next(2 * range) - range;
			int num5 = sapi.World.Rand.Next(2 * range) - range;
			int num6 = sapi.World.Rand.Next(2 * range) - range;
			vec3d.Set((double)((int)xYZ.X + num4) + 0.5, (double)((int)xYZ.Y + num5) + 0.001, (double)((int)xYZ.Z + num6) + 0.5);
			blockPos.Set((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
			while (sapi.World.BlockAccessor.GetBlockBelow(blockPos).Id == 0 && vec3d.Y > 0.0)
			{
				blockPos.Y--;
				vec3d.Y -= 1.0;
			}
			if (sapi.World.BlockAccessor.IsValidPos((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z))
			{
				Cuboidf entityBoxRel = entityProperties.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
				if (!collisionTester.IsColliding(sapi.World.BlockAccessor, entityBoxRel, vec3d, alsoCheckTouch: false))
				{
					DoSpawn(entityProperties, vec3d, 0L);
					num2++;
				}
			}
		}
	}

	private void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
	{
		Entity entity = sapi.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent entityAgent)
		{
			entityAgent.HerdId = herdid;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)sapi.World.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "bellalarm");
		sapi.World.SpawnEntity(entity);
		spawnedEntities.Add(entity);
	}
}
