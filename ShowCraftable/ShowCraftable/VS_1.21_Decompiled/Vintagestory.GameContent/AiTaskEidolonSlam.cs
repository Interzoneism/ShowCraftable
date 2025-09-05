using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AiTaskEidolonSlam : AiTaskBaseTargetable
{
	private int durationMs;

	private int releaseAtMs;

	private long lastSearchTotalMs;

	protected int searchWaitMs = 2000;

	private float maxDist = 15f;

	private float projectileDamage;

	private int projectileDamageTier;

	private AssetLocation projectileCode;

	public float creatureSpawnChance;

	public float creatureSpawnCount = 4.5f;

	private AssetLocation creatureCode;

	public float spawnRange;

	public float spawnHeight;

	public float spawnAmount;

	private float accum;

	private bool didSpawn;

	private int creaturesLeftToSpawn;

	public AiTaskEidolonSlam(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		durationMs = taskConfig["durationMs"].AsInt(1500);
		releaseAtMs = taskConfig["releaseAtMs"].AsInt(1000);
		projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
		projectileDamageTier = taskConfig["projectileDamageTier"].AsInt();
		projectileCode = AssetLocation.Create(taskConfig["projectileCode"].AsString("thrownstone-{rock}"), entity.Code.Domain);
		if (taskConfig["creatureCode"].Exists)
		{
			creatureCode = AssetLocation.Create(taskConfig["creatureCode"].AsString(), entity.Code.Domain);
		}
		spawnRange = taskConfig["spawnRange"].AsFloat(9f);
		spawnHeight = taskConfig["spawnHeight"].AsFloat(9f);
		spawnAmount = taskConfig["spawnAmount"].AsFloat(10f);
		maxDist = taskConfig["maxDist"].AsFloat(12f);
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > 0.10000000149011612 && (WhenInEmotionState == null || !IsInEmotionState(WhenInEmotionState)))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (WhenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		float range = maxDist;
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (Entity e) => IsTargetableEntity(e, range), EnumEntitySearchType.Creatures);
		return targetEntity != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		accum = 0f;
		didSpawn = false;
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (animMeta != null)
		{
			animMeta.EaseInSpeed = 1f;
			animMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(animMeta);
		}
		accum += dt;
		Vec3d pos = entity.Pos.XYZ;
		float damage = 6f;
		if (accum > (float)releaseAtMs / 1000f && !didSpawn)
		{
			didSpawn = true;
			Random random = entity.World.Rand;
			if (entity.World.Rand.NextDouble() < (double)creatureSpawnChance)
			{
				int count = 0;
				partitionUtil.WalkEntities(pos, 7.0, delegate(Entity e)
				{
					if (e.Code.Equals(creatureCode) && e.Alive)
					{
						count++;
					}
					return true;
				}, EnumEntitySearchType.Creatures);
				creaturesLeftToSpawn = Math.Max(0, GameMath.RoundRandom(entity.World.Rand, creatureSpawnCount) - count);
			}
			for (int num = 0; (float)num < spawnAmount; num++)
			{
				float dx = (float)random.NextDouble() * 2f * spawnRange - spawnRange;
				float dz = (float)random.NextDouble() * 2f * spawnRange - spawnRange;
				float dy = spawnHeight;
				spawnProjectile(dx, dy, dz);
			}
			partitionUtil.WalkEntities(pos, 9.0, delegate(Entity e)
			{
				if (e.EntityId == entity.EntityId || !e.IsInteractable)
				{
					return true;
				}
				if (!e.Alive || !e.OnGround)
				{
					return true;
				}
				double num5 = e.Pos.DistanceTo(pos);
				float num6 = (float)(5.0 - num5) / 5f;
				float damage2 = Math.Max(0.02f, damage * GlobalConstants.CreatureDamageModifier * num6);
				e.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Entity,
					SourceEntity = entity,
					Type = EnumDamageType.BluntAttack,
					DamageTier = 1,
					KnockbackStrength = 0f
				}, damage2);
				float num7 = GameMath.Clamp(10f - (float)num5, 0f, 5f);
				Vec3d vec3d = (entity.ServerPos.XYZ - e.ServerPos.XYZ).Normalize();
				vec3d.Y = 0.699999988079071;
				float num8 = num7 * GameMath.Clamp((1f - e.Properties.KnockbackResistance) / 10f, 0f, 1f);
				e.WatchedAttributes.SetFloat("onHurtDir", (float)Math.Atan2(vec3d.X, vec3d.Z));
				e.WatchedAttributes.SetDouble("kbdirX", vec3d.X * (double)num8);
				e.WatchedAttributes.SetDouble("kbdirY", vec3d.Y * (double)num8);
				e.WatchedAttributes.SetDouble("kbdirZ", vec3d.Z * (double)num8);
				e.WatchedAttributes.SetInt("onHurtCounter", e.WatchedAttributes.GetInt("onHurtCounter") + 1);
				e.WatchedAttributes.SetFloat("onHurt", 0.01f);
				return true;
			}, EnumEntitySearchType.Creatures);
			IPlayer[] allOnlinePlayers = entity.World.AllOnlinePlayers;
			for (int num2 = 0; num2 < allOnlinePlayers.Length; num2++)
			{
				IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[num2];
				if (serverPlayer.ConnectionState == EnumClientState.Playing)
				{
					double num3 = serverPlayer.Entity.Pos.X - entity.Pos.X;
					double num4 = serverPlayer.Entity.Pos.Z - entity.Pos.Z;
					if (Math.Abs(num3) <= (double)spawnRange && Math.Abs(num4) <= (double)spawnRange)
					{
						spawnProjectile((float)num3, spawnHeight, (float)num4);
					}
				}
			}
			entity.World.Api.ModLoader.GetModSystem<ScreenshakeToClientModSystem>().ShakeScreen(entity.Pos.XYZ, 1f, 16f);
		}
		return accum < (float)durationMs / 1000f;
	}

	private void spawnProjectile(float dx, float dy, float dz)
	{
		if (creaturesLeftToSpawn > 0)
		{
			spawnCreature(dx, dy, dz);
			return;
		}
		AssetLocation assetLocation = projectileCode.Clone();
		string text = "granite";
		IMapChunk mapChunkAtBlockPos = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
		if (mapChunkAtBlockPos != null)
		{
			int num = (int)entity.Pos.Z % 32;
			int num2 = (int)entity.Pos.X % 32;
			text = entity.World.Blocks[mapChunkAtBlockPos.TopRockIdMap[num * 32 + num2]].Variant["rock"] ?? "granite";
		}
		assetLocation.Path = assetLocation.Path.Replace("{rock}", text);
		EntityProperties entityType = entity.World.GetEntityType(assetLocation);
		EntityThrownStone entityThrownStone = entity.World.ClassRegistry.CreateEntity(entityType) as EntityThrownStone;
		entityThrownStone.FiredBy = entity;
		entityThrownStone.Damage = projectileDamage;
		entityThrownStone.DamageTier = projectileDamageTier;
		entityThrownStone.ProjectileStack = new ItemStack(entity.World.GetItem(new AssetLocation("stone-" + text)));
		entityThrownStone.NonCollectible = true;
		entityThrownStone.VerticalImpactBreakChance = 1f;
		entityThrownStone.ImpactParticleSize = 1.5f;
		entityThrownStone.ImpactParticleCount = 30;
		entityThrownStone.ServerPos.SetPosWithDimension(entity.ServerPos.XYZ.Add(dx, dy, dz));
		entityThrownStone.Pos.SetFrom(entityThrownStone.ServerPos);
		entityThrownStone.World = entity.World;
		entity.World.SpawnEntity(entityThrownStone);
	}

	private void spawnCreature(float dx, float dy, float dz)
	{
		EntityProperties entityType = base.entity.World.GetEntityType(creatureCode);
		Entity entity = base.entity.World.ClassRegistry.CreateEntity(entityType);
		entity.ServerPos.SetPosWithDimension(base.entity.ServerPos.XYZ.Add(dx, dy, dz));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.World = base.entity.World;
		base.entity.World.SpawnEntity(entity);
		creaturesLeftToSpawn--;
	}
}
