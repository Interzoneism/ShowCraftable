using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityEidolon : EntityAgent
{
	private ILoadedSound activeSound;

	private AiTaskEidolonSlam slamTask;

	private EntityBehaviorHealth bhHealth;

	private HashSet<string> hurtByPlayerUids = new HashSet<string>();

	private bool IsAsleep
	{
		get
		{
			AiTaskManager aiTaskManager = GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
			if (aiTaskManager == null)
			{
				return false;
			}
			IAiTask[] activeTasksBySlot = aiTaskManager.ActiveTasksBySlot;
			for (int i = 0; i < activeTasksBySlot.Length; i++)
			{
				if (activeTasksBySlot[i]?.Id == "inactive")
				{
					return true;
				}
			}
			return false;
		}
	}

	static EntityEidolon()
	{
		AiTaskRegistry.Register<AiTaskEidolonSlam>("eidolonslam");
		AiTaskRegistry.Register<AiTaskEidolonMeleeAttack>("eidolonmeleeattack");
	}

	public EntityEidolon()
	{
		AnimManager = new EidolonAnimManager();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		Notify("starttask", "inactive");
		WatchedAttributes.SetBool("showHealthbar", value: false);
		AllowDespawn = false;
		if (api.Side == EnumAppSide.Server)
		{
			slamTask = GetBehavior<EntityBehaviorTaskAI>().TaskManager.GetTask<AiTaskEidolonSlam>();
			bhHealth = GetBehavior<EntityBehaviorHealth>();
		}
	}

	public override void OnGameTick(float dt)
	{
		if (Api is ICoreClientAPI coreClientAPI)
		{
			bool flag = Alive && !AnimManager.IsAnimationActive("inactive");
			bool flag2 = activeSound != null && activeSound.IsPlaying;
			if (flag && !flag2)
			{
				if (activeSound == null)
				{
					activeSound = coreClientAPI.World.LoadSound(new SoundParams
					{
						Location = new AssetLocation("sounds/creature/eidolon/awake"),
						DisposeOnFinish = false,
						ShouldLoop = true,
						Position = Pos.XYZ.ToVec3f(),
						SoundType = EnumSoundType.Entity,
						Volume = 0f,
						Range = 16f
					});
				}
				activeSound.Start();
				activeSound.FadeTo(0.10000000149011612, 0.5f, delegate
				{
				});
			}
			if (!flag && flag2)
			{
				activeSound.FadeOutAndStop(2.5f);
			}
			GetBehavior<EntityBehaviorBoss>().ShouldPlayTrack = flag && coreClientAPI.World.Player.Entity.Pos.DistanceTo(Pos) < 15.0;
		}
		else if (slamTask.creatureSpawnChance <= 0f && (double)(bhHealth.Health / bhHealth.MaxHealth) < 0.5)
		{
			slamTask.creatureSpawnChance = 0.3f;
		}
		else
		{
			slamTask.creatureSpawnChance = 0f;
		}
		base.OnGameTick(dt);
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		activeSound?.Stop();
		activeSound?.Dispose();
	}

	public override void Revive()
	{
		base.Revive();
		hurtByPlayerUids.Clear();
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		if (World.Side == EnumAppSide.Server)
		{
			string text = (damageSource.GetCauseEntity() as EntityPlayer)?.PlayerUID;
			if (text != null)
			{
				hurtByPlayerUids.Add(text);
			}
		}
		return base.ShouldReceiveDamage(damageSource, damage);
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		Entity sourceEntity = damageSource.SourceEntity;
		if (sourceEntity != null && sourceEntity.Code.PathStartsWith("thrownboulder"))
		{
			return false;
		}
		if (IsAsleep && damageSource.Type != EnumDamageType.Heal)
		{
			return false;
		}
		if (World.Side == EnumAppSide.Server)
		{
			int num = nearbyPlayerCount();
			damage *= 1f / (1f + (float)Math.Sqrt((num - 1) / 2));
		}
		damageSource.KnockbackStrength = 0f;
		return base.ReceiveDamage(damageSource, damage);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer);
		drops[0].StackSize = Math.Max(1, hurtByPlayerUids.Count);
		return drops;
	}

	private int nearbyPlayerCount()
	{
		int num = 0;
		IPlayer[] allOnlinePlayers = World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
			if (serverPlayer.ConnectionState == EnumClientState.Playing && serverPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				double value = serverPlayer.Entity.Pos.X - Pos.X;
				double value2 = serverPlayer.Entity.Pos.Z - Pos.Z;
				if (Math.Abs(value) <= 7.0 && Math.Abs(value2) <= 7.0)
				{
					num++;
				}
			}
		}
		return num;
	}
}
