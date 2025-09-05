using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskBellAlarmR : AiTaskBaseTargetableR
{
	protected int nextSpawnIntervalMs;

	protected List<Entity> spawnedEntities = new List<Entity>();

	protected float timeSinceLastSpawnSec;

	protected CollisionTester collisionTester = new CollisionTester();

	private AiTaskBellAlarmConfig Config => GetConfig<AiTaskBellAlarmConfig>();

	public AiTaskBellAlarmR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskBellAlarmConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		return SearchForTarget();
	}

	public override void StartExecute()
	{
		if (Config.RepeatSound != null)
		{
			(entity.Api as ICoreServerAPI)?.Network.BroadcastEntityPacket(entity.EntityId, 1025, SerializerUtil.Serialize(Config.RepeatSound));
		}
		nextSpawnIntervalMs = Config.SpawnIntervalMinMs + entity.World.Rand.Next(Config.SpawnIntervalMaxMs - Config.SpawnIntervalMinMs);
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (targetEntity == null)
		{
			return false;
		}
		timeSinceLastSpawnSec += dt;
		if (timeSinceLastSpawnSec * 1000f > (float)nextSpawnIntervalMs)
		{
			if (!(entity.Api is ICoreServerAPI coreServerAPI))
			{
				return false;
			}
			int num = entity.World.GetPlayersAround(entity.ServerPos.XYZ, Config.PlayerSpawnScaleRange, Config.PlayerSpawnScaleRange, (IPlayer player) => player.Entity.Alive).Length;
			float num2 = 1f + (float)(num - 1) * coreServerAPI.Server.Config.SpawnCapPlayerScaling * Config.PlayerScalingFactor;
			TrySpawnCreatures(GameMath.RoundRandom(base.Rand, (float)Config.SpawnMaxQuantity * num2 - 1f) + 1, Config.SpawnRange);
			nextSpawnIntervalMs = Config.SpawnIntervalMinMs + entity.World.Rand.Next(Config.SpawnIntervalMaxMs - Config.SpawnIntervalMinMs);
			timeSinceLastSpawnSec = 0f;
		}
		if (targetEntity.Pos.SquareDistanceTo(entity.Pos) > Config.MaxDistanceToTarget * Config.MaxDistanceToTarget)
		{
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		(entity.Api as ICoreServerAPI)?.Network.BroadcastEntityPacket(entity.EntityId, 1026);
		base.FinishExecute(cancelled);
	}

	public override void OnEntityDespawn(EntityDespawnData reason)
	{
		(entity.Api as ICoreServerAPI)?.Network.BroadcastEntityPacket(entity.EntityId, 1026);
		base.OnEntityDespawn(reason);
	}

	protected virtual void TrySpawnCreatures(int maxQuantity, int range)
	{
		if (!(entity.Api is ICoreServerAPI coreServerAPI))
		{
			return;
		}
		FastVec3d fastVec3d = new FastVec3d(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
		Vec3d vec3d = new Vec3d();
		BlockPos blockPos = new BlockPos(0);
		BlockPos blockPos2 = new BlockPos(0);
		for (int i = 0; i < spawnedEntities.Count; i++)
		{
			if (spawnedEntities[i] == null || !spawnedEntities[i].Alive)
			{
				spawnedEntities.RemoveAt(i);
				i--;
			}
		}
		if (Config.EntitiesToSpawn.Length == 0 || spawnedEntities.Count > maxQuantity)
		{
			return;
		}
		int num = 50;
		int num2 = 0;
		while (num-- > 0 && num2 < 1)
		{
			int num3 = base.Rand.Next(Config.EntitiesToSpawn.Length);
			EntityProperties entityProperties = Config.EntitiesToSpawn[num3];
			int num4 = coreServerAPI.World.Rand.Next(2 * range) - range;
			int num5 = coreServerAPI.World.Rand.Next(2 * range) - range;
			int num6 = coreServerAPI.World.Rand.Next(2 * range) - range;
			vec3d.Set((double)((int)fastVec3d.X + num4) + 0.5, (double)((int)fastVec3d.Y + num5) + 0.001, (double)((int)fastVec3d.Z + num6) + 0.5);
			blockPos.Set((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
			while (coreServerAPI.World.BlockAccessor.GetBlockBelow(blockPos).Id == 0 && vec3d.Y > 0.0)
			{
				blockPos.Y--;
				vec3d.Y -= 1.0;
			}
			blockPos2.Set((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
			if (coreServerAPI.World.BlockAccessor.IsValidPos(blockPos2))
			{
				Cuboidf entityBoxRel = entityProperties.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
				if (!collisionTester.IsColliding(coreServerAPI.World.BlockAccessor, entityBoxRel, vec3d, alsoCheckTouch: false))
				{
					DoSpawn(entityProperties, vec3d, entity.HerdId);
					num2++;
				}
			}
		}
	}

	protected virtual void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdId)
	{
		Entity entity = base.entity.Api.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent entityAgent)
		{
			entityAgent.HerdId = herdId;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)base.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", Config.Origin);
		base.entity.World.SpawnEntity(entity);
		spawnedEntities.Add(entity);
	}

	protected override bool CheckDetectionRange(Entity target, double range)
	{
		if (!base.CheckDetectionRange(target, range))
		{
			return false;
		}
		if (!(target is EntityPlayer entityPlayer))
		{
			return true;
		}
		double num = target.Pos.DistanceTo(entity.Pos.XYZ);
		bool flag = entityPlayer.ServerControls.LeftMouseDown || entityPlayer.ServerControls.RightMouseDown || entityPlayer.ServerControls.HandUse != EnumHandInteract.None;
		bool flag2 = entityPlayer.ServerControls.TriesToMove || entityPlayer.ServerControls.Jump || !entityPlayer.OnGround;
		bool num2 = !flag && !flag2;
		bool flag3 = entityPlayer.ServerControls.Sneak && !entityPlayer.ServerControls.Jump && entityPlayer.OnGround && !flag;
		if (num2)
		{
			range *= (double)Config.SilentSoundRangeReduction;
		}
		else if (flag3)
		{
			range *= (double)Config.QuietSoundRangeReduction;
		}
		return num <= range;
	}

	protected override float GetSeekingRange()
	{
		float num = base.GetSeekingRange();
		if (!entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.IsTaskActive(Config.ListenAiTaskId))
		{
			num *= Config.NotListeningRangeReduction;
		}
		return num;
	}
}
