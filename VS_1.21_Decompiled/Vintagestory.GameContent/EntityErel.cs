using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public sealed class EntityErel : EntityAgent
{
	private ILoadedSound aliveSound;

	private ILoadedSound glideSound;

	private AiTaskManager taskManager;

	private readonly Dictionary<string, int[]> attackCooldowns = new Dictionary<string, int[]>();

	private float nextFlyIdleSec = -1f;

	private float nextFlapCruiseSec = -1f;

	private float prevYaw;

	private float annoyCheckAccum;

	private bool wasAtBossFightArea;

	private EntityBehaviorHealth healthBehavior;

	private int previousTickDimension;

	public override bool CanSwivel => true;

	public override bool CanSwivelNow => true;

	public override bool StoreWithChunk => false;

	public override bool AllowOutsideLoadedRange => true;

	public override bool AlwaysActive => true;

	public long LastAttackTime { get; set; }

	public double LastAnnoyedTotalDays
	{
		get
		{
			return WatchedAttributes.GetDouble("lastannoyedtotaldays", -9999999.0);
		}
		set
		{
			WatchedAttributes.SetDouble("lastannoyedtotaldays", value);
		}
	}

	public bool Annoyed
	{
		get
		{
			return WatchedAttributes.GetBool("annoyed");
		}
		set
		{
			WatchedAttributes.SetBool("annoyed", value);
		}
	}

	static EntityErel()
	{
		AiTaskRegistry.Register<AiTaskFlyCircle>("flycircle");
		AiTaskRegistry.Register<AiTaskFlyCircleIfEntity>("flycircleifentity");
		AiTaskRegistry.Register<AiTaskFlyCircleTarget>("flycircletarget");
		AiTaskRegistry.Register<AiTaskFlyWander>("flywander");
		AiTaskRegistry.Register<AiTaskFlySwoopAttack>("flyswoopattack");
		AiTaskRegistry.Register<AiTaskFlyDiveAttack>("flydiveattack");
		AiTaskRegistry.Register<AiTaskFireFeathersAttack>("firefeathersattack");
		AiTaskRegistry.Register<AiTaskFlyLeave>("flyleave");
	}

	public EntityErel()
	{
		SimulationRange = 1024;
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		WatchedAttributes.SetBool("showHealthbar", value: true);
		if (api is ICoreClientAPI coreClientAPI)
		{
			aliveSound = coreClientAPI.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/creature/erel/alive"),
				ShouldLoop = true,
				Range = 48f
			});
			aliveSound.Start();
			glideSound = coreClientAPI.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/creature/erel/glide"),
				ShouldLoop = true,
				Range = 24f
			});
		}
		healthBehavior = GetBehavior<EntityBehaviorHealth>();
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(Annoyed);
		if (World.Side == EnumAppSide.Server)
		{
			taskManager = GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			taskManager.OnShouldExecuteTask += TaskManager_OnShouldExecuteTask;
			if (Annoyed)
			{
				taskManager.GetTask<AiTaskFlyLeave>().AllowExecute = true;
			}
			attackCooldowns["swoop"] = new int[2]
			{
				taskManager.GetTask<AiTaskFlySwoopAttack>().Mincooldown,
				taskManager.GetTask<AiTaskFlySwoopAttack>().Maxcooldown
			};
			attackCooldowns["dive"] = new int[2]
			{
				taskManager.GetTask<AiTaskFlyDiveAttack>().Mincooldown,
				taskManager.GetTask<AiTaskFlyDiveAttack>().Maxcooldown
			};
			attackCooldowns["feathers"] = new int[2]
			{
				taskManager.GetTask<AiTaskFireFeathersAttack>().Mincooldown,
				taskManager.GetTask<AiTaskFireFeathersAttack>().Maxcooldown
			};
		}
		updateAnnoyedState();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		aliveSound?.Dispose();
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (Api.Side == EnumAppSide.Server)
		{
			doOccasionalFlapping(dt);
			annoyCheckAccum += dt;
			if (annoyCheckAccum > 1f)
			{
				annoyCheckAccum = 0f;
				updateAnnoyedState();
				toggleBossFightModeNearTower();
				stopAttacksOutsideDevaRange();
			}
		}
		else
		{
			aliveSound.SetPosition((float)Pos.X, (float)Pos.InternalY, (float)Pos.Z);
			glideSound.SetPosition((float)Pos.X, (float)Pos.InternalY, (float)Pos.Z);
			if (AnimManager.IsAnimationActive("fly-flapactive", "fly-flapactive-fast") && glideSound.IsPlaying)
			{
				glideSound.Stop();
			}
			else if ((AnimManager.IsAnimationActive("fly-idle", "fly-flapcruise") || AnimManager.ActiveAnimationsByAnimCode.Count == 0) && !glideSound.IsPlaying)
			{
				glideSound.Start();
			}
			setCurrentShape(Pos.Dimension);
		}
		if (!AnimManager.IsAnimationActive("dive", "slam"))
		{
			ServerPos.Motion.Length();
			_ = 0.01;
		}
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (!inTowerRange())
		{
			damage /= 2f;
		}
		if (World.Side == EnumAppSide.Server)
		{
			int num = nearbyPlayerCount();
			damage *= 1f / (1f + (float)Math.Sqrt((num - 1) / 4));
		}
		if (healthBehavior != null && healthBehavior.Health - damage < 0f)
		{
			float damage2 = MathF.Max(healthBehavior.Health - 1f, 0f);
			return base.ReceiveDamage(damageSource, damage2);
		}
		return base.ReceiveDamage(damageSource, damage);
	}

	public void ChangeDimension(int dim)
	{
		if (ServerPos.Dimension != dim)
		{
			spawnTeleportParticles(Pos);
		}
		Pos.Dimension = dim;
		ServerPos.Dimension = dim;
		long newChunkIndex3d = Api.World.ChunkProvider.ChunkIndex3D(Pos);
		Api.World.UpdateEntityChunk(this, newChunkIndex3d);
	}

	public void ChangeDimensionNoParticles(int dim)
	{
		Pos.Dimension = dim;
		ServerPos.Dimension = dim;
		long newChunkIndex3d = Api.World.ChunkProvider.ChunkIndex3D(Pos);
		Api.World.UpdateEntityChunk(this, newChunkIndex3d);
	}

	protected override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned)
	{
		base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
		AnimManager.LoadAnimator(World.Api, this, entityShape, AnimManager.Animator?.Animations, requirePosesOnServer, "head");
	}

	private void spawnTeleportParticles(EntityPos pos)
	{
		int num = 53;
		int num2 = 221;
		int num3 = 172;
		SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(300f, 400f, (num << 16) | (num2 << 8) | num3 | 0x64000000, new Vec3d(pos.X - 2.5, pos.Y, pos.Z - 2.5), new Vec3d(pos.X + 2.5, pos.Y + 5.8, pos.Z + 2.5), new Vec3f(-0.7f, -0.7f, -0.7f), new Vec3f(1.4f, 1.4f, 1.4f), 2f, 0f, 0.15f, 0.3f, EnumParticleModel.Quad);
		simpleParticleProperties.addLifeLength = 1f;
		simpleParticleProperties.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
		int dimension = pos.Dimension;
		Api.World.SpawnParticles(simpleParticleProperties);
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y, pos.Z, null, randomizePitch: false, 128f);
		simpleParticleProperties.MinPos.Y += dimension * 32768;
		Api.World.SpawnParticles(simpleParticleProperties);
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y + (double)(dimension * 32768), pos.Z, null, randomizePitch: false, 128f);
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

	private void toggleBossFightModeNearTower()
	{
		ModSystemDevastationEffects modSystem = Api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		Vec3d vec3d = ((ServerPos.Dimension == 0) ? modSystem.DevaLocationPresent : modSystem.DevaLocationPast);
		WatchedAttributes.SetBool("showHealthbar", ServerPos.InternalY > vec3d.Y + 70.0);
		AiTaskFlyCircleIfEntity task = taskManager.GetTask<AiTaskFlyCircleIfEntity>();
		bool flag = task.getEntity() != null && ServerPos.XYZ.HorizontalSquareDistanceTo(task.CenterPos) < 4900f;
		AiTaskFlySwoopAttack task2 = taskManager.GetTask<AiTaskFlySwoopAttack>();
		AiTaskFlyDiveAttack task3 = taskManager.GetTask<AiTaskFlyDiveAttack>();
		AiTaskFireFeathersAttack task4 = taskManager.GetTask<AiTaskFireFeathersAttack>();
		task4.Enabled = flag;
		task3.Enabled = flag;
		if (wasAtBossFightArea && !flag)
		{
			task2.Mincooldown = attackCooldowns["swoop"][0];
			task2.Maxcooldown = attackCooldowns["swoop"][1];
			task3.Mincooldown = attackCooldowns["dive"][0];
			task3.Maxcooldown = attackCooldowns["dive"][1];
			task4.Mincooldown = attackCooldowns["feathers"][0];
			task4.Maxcooldown = attackCooldowns["feathers"][1];
		}
		if (!wasAtBossFightArea && flag)
		{
			task2.Mincooldown = attackCooldowns["swoop"][0] / 2;
			task2.Maxcooldown = attackCooldowns["swoop"][1] / 2;
			task3.Mincooldown = attackCooldowns["dive"][0] / 2;
			task3.Maxcooldown = attackCooldowns["dive"][1] / 2;
			task4.Mincooldown = attackCooldowns["feathers"][0] / 2;
			task4.Maxcooldown = attackCooldowns["feathers"][1] / 2;
		}
		wasAtBossFightArea = flag;
	}

	private void updateAnnoyedState()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			return;
		}
		if (!Annoyed)
		{
			if ((double)(healthBehavior.Health / healthBehavior.MaxHealth) < 0.6)
			{
				Api.World.PlaySoundAt("sounds/creature/erel/annoyed", this, null, randomizePitch: false, 1024f);
				AnimManager.StartAnimation("defeat");
				LastAnnoyedTotalDays = Api.World.Calendar.TotalDays;
				Annoyed = true;
				taskManager.GetTask<AiTaskFlyLeave>().AllowExecute = true;
				Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(on: true);
			}
		}
		else if (Api.World.Calendar.TotalDays - LastAnnoyedTotalDays > 14.0)
		{
			Annoyed = false;
			healthBehavior.Health = healthBehavior.MaxHealth;
			taskManager.GetTask<AiTaskFlyLeave>().AllowExecute = false;
			Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(on: false);
		}
	}

	private void doOccasionalFlapping(float dt)
	{
		float num = Math.Abs(GameMath.AngleRadDistance(prevYaw, ServerPos.Yaw));
		double num2 = ServerPos.Motion.Length();
		if (AnimManager.IsAnimationActive("dive", "slam"))
		{
			return;
		}
		if ((ServerPos.Motion.Y >= 0.03 || (double)num > 0.05 || num2 < 0.15) && (AnimManager.IsAnimationActive("fly-idle", "fly-flapcruise") || AnimManager.ActiveAnimationsByAnimCode.Count == 0))
		{
			AnimManager.StopAnimation("fly-flapcruise");
			AnimManager.StopAnimation("fly-idle");
			AnimManager.StartAnimation("fly-flapactive-fast");
			return;
		}
		if (ServerPos.Motion.Y <= 0.01 && (double)num < 0.03 && num2 >= 0.35 && AnimManager.IsAnimationActive("fly-flapactive", "fly-flapactive-fast"))
		{
			AnimManager.StopAnimation("fly-flapactive");
			AnimManager.StopAnimation("fly-flapactive-fast");
			AnimManager.StartAnimation("fly-idle");
		}
		prevYaw = ServerPos.Yaw;
		if (nextFlyIdleSec > 0f)
		{
			nextFlyIdleSec -= dt;
			if (nextFlyIdleSec < 0f)
			{
				AnimManager.StopAnimation("fly-flapcruise");
				AnimManager.StartAnimation("fly-idle");
				return;
			}
		}
		if (nextFlapCruiseSec < 0f)
		{
			nextFlapCruiseSec = (float)Api.World.Rand.NextDouble() * 15f + 5f;
		}
		else if (AnimManager.IsAnimationActive("fly-idle"))
		{
			nextFlapCruiseSec -= dt;
			if (nextFlapCruiseSec < 0f)
			{
				AnimManager.StopAnimation("fly-idle");
				AnimManager.StartAnimation("fly-flapcruise");
				nextFlyIdleSec = (float)(Api.World.Rand.NextDouble() * 4.0 + 1.0) * 130f / 30f;
			}
		}
	}

	private void stopAttacksOutsideDevaRange()
	{
		if (!outSideDevaRange())
		{
			return;
		}
		IAiTask[] activeTasksBySlot = taskManager.ActiveTasksBySlot;
		foreach (IAiTask aiTask in activeTasksBySlot)
		{
			if (aiTask is AiTaskFlySwoopAttack || aiTask is AiTaskFlyDiveAttack || aiTask is AiTaskFireFeathersAttack || aiTask is AiTaskFlyCircleTarget)
			{
				taskManager.StopTask(aiTask.GetType());
			}
		}
	}

	private bool outSideDevaRange()
	{
		return distanceToTower() > 600.0;
	}

	private bool inTowerRange()
	{
		return distanceToTower() < 100.0;
	}

	private double distanceToTower()
	{
		ModSystemDevastationEffects modSystem = Api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		Vec3d pos = ((ServerPos.Dimension == 0) ? modSystem.DevaLocationPresent : modSystem.DevaLocationPast);
		return ServerPos.DistanceTo(pos);
	}

	private void setCurrentShape(int dimension)
	{
		if (base.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
		{
			if (dimension == 2)
			{
				entityShapeRenderer.OverrideEntityShape = base.Properties.Client.LoadedAlternateShapes[0];
			}
			else
			{
				entityShapeRenderer.OverrideEntityShape = base.Properties.Client.LoadedAlternateShapes[1];
			}
			if (previousTickDimension != dimension)
			{
				entityShapeRenderer.TesselateShape();
				previousTickDimension = dimension;
			}
		}
	}

	private bool TaskManager_OnShouldExecuteTask(IAiTask t)
	{
		if ((t is AiTaskFlySwoopAttack || t is AiTaskFlyDiveAttack || t is AiTaskFireFeathersAttack || t is AiTaskFlyCircleTarget) && outSideDevaRange())
		{
			return false;
		}
		return true;
	}
}
