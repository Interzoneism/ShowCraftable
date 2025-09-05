using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Essentials;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public abstract class AiTaskBaseR : IAiTask
{
	protected AiTaskBaseConfig baseConfig;

	[ThreadStatic]
	private static Random? randThreadStatic;

	protected readonly EntityAgent entity;

	protected readonly IWorldAccessor world;

	protected WaypointsTraverser pathTraverser;

	protected EntityBehaviorEmotionStates? emotionStatesBehavior;

	protected EntityBehaviorTaskAI taskAiBehavior;

	protected EntityPartitioning partitionUtil;

	protected const int maxLightLevel = 32;

	protected const int standardHoursPerDay = 24;

	protected bool stopTask;

	protected bool active;

	protected long durationUntilMs;

	protected float currentDayTimeInaccuracy;

	protected Entity? attackedByEntity;

	protected long attackedByEntityMs;

	protected long lastSoundTotalMs;

	protected long cooldownUntilMs;

	protected double cooldownUntilTotalHours;

	protected long executionStartTimeMs;

	protected EntityTagArray tagsAppliedOnStart;

	public virtual string Id => Config.Id;

	public virtual int Slot => Config.Slot;

	public virtual float Priority => Config.Priority;

	public virtual float PriorityForCancel => Config.PriorityForCancel;

	public string ProfilerName { get; set; } = "";

	public string[] WhenInEmotionState => Config.WhenInEmotionState;

	public Entity? AttackedByEntity => attackedByEntity;

	private AiTaskBaseConfig Config => baseConfig;

	protected Random Rand => randThreadStatic ?? (randThreadStatic = new Random());

	protected bool RecentlyAttacked => entity.World.ElapsedMilliseconds - attackedByEntityMs < Config.RecentlyAttackedTimeoutMs;

	protected AiTaskBaseR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
	{
		if (randThreadStatic == null)
		{
			randThreadStatic = new Random((int)entity.EntityId);
		}
		this.entity = entity;
		world = entity.World;
		pathTraverser = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser ?? throw new ArgumentException($"PathTraverser should not be null, possible error on EntityBehaviorTaskAI initialization for entity: {entity.Code}.");
		emotionStatesBehavior = entity.GetBehavior<EntityBehaviorEmotionStates>();
		taskAiBehavior = entity.GetBehavior<EntityBehaviorTaskAI>() ?? throw new ArgumentException($"Entity '{entity.Code}' does not have EntityBehaviorTaskAI.");
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>() ?? throw new ArgumentException("EntityPartitioning mod system is not found");
		baseConfig = LoadConfig<AiTaskBaseConfig>(entity, taskConfig, aiConfig);
		int initialMinCooldownMs = Config.InitialMinCooldownMs;
		int initialMaxCooldownMs = Config.InitialMaxCooldownMs;
		if (Config.TemperatureRange != null && Config.TemperatureRange.Length != 2)
		{
			entity.Api.Logger.Error($"Invalid 'temperatureRange' value in AI task '{Config.Code}' for entity '{entity.Code}'");
			throw new ArgumentException($"Invalid 'temperatureRange' value in AI task '{Config.Code}' for entity '{entity.Code}'");
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + initialMinCooldownMs + entity.World.Rand.Next(initialMaxCooldownMs - initialMinCooldownMs);
		attackedByEntityMs = -Config.RecentlyAttackedTimeoutMs;
	}

	protected AiTaskBaseR(EntityAgent entity)
	{
		if (randThreadStatic == null)
		{
			randThreadStatic = new Random((int)entity.EntityId);
		}
		this.entity = entity;
		world = entity.World;
		pathTraverser = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser ?? throw new ArgumentException($"PathTraverser should not be null, possible error on EntityBehaviorTaskAI initialization for entity: {entity.Code}.");
		emotionStatesBehavior = entity.GetBehavior<EntityBehaviorEmotionStates>();
		taskAiBehavior = entity.GetBehavior<EntityBehaviorTaskAI>() ?? throw new ArgumentException($"Entity '{entity.Code}' does not have EntityBehaviorTaskAI.");
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>() ?? throw new ArgumentException("EntityPartitioning mod system is not found");
		AiTaskRegistry.TaskCodes.TryGetValue(GetType(), out string value);
		baseConfig = new AiTaskBaseConfig();
		baseConfig.Code = ((baseConfig.Code == "") ? (value ?? "") : baseConfig.Code);
		baseConfig.Init(entity);
	}

	public virtual void AfterInitialize()
	{
		if (!baseConfig.Initialized)
		{
			throw new InvalidOperationException($"Config was not initialized for task '{Config.Code}' and entity '{entity.Code}'. Have you forgot to call 'base.Init()' method in 'Init()'?");
		}
	}

	public abstract bool ShouldExecute();

	public virtual void StartExecute()
	{
		if (Config.AnimationMeta != null)
		{
			entity.AnimManager.StartAnimation(Config.AnimationMeta);
		}
		if (Config.Sound != null && entity.World.Rand.NextDouble() <= (double)Config.SoundChance)
		{
			if (Config.SoundStartMs > 0)
			{
				entity.World.RegisterCallback(delegate
				{
					entity.World.PlaySoundAt(Config.Sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
					lastSoundTotalMs = entity.World.ElapsedMilliseconds;
				}, Config.SoundStartMs);
			}
			else
			{
				entity.World.PlaySoundAt(Config.Sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
				lastSoundTotalMs = entity.World.ElapsedMilliseconds;
			}
		}
		if (Config.MaxDurationMs <= 0)
		{
			durationUntilMs = 0L;
		}
		else
		{
			durationUntilMs = entity.World.ElapsedMilliseconds + Config.MinDurationMs + entity.World.Rand.Next(Config.MaxDurationMs - Config.MinDurationMs);
		}
		executionStartTimeMs = entity.World.ElapsedMilliseconds;
		tagsAppliedOnStart = ~entity.Tags & Config.TagsAppliedToEntity;
		if (tagsAppliedOnStart != EntityTagArray.Empty)
		{
			entity.Tags |= tagsAppliedOnStart;
			entity.MarkTagsDirty();
		}
		active = true;
		stopTask = false;
	}

	public virtual bool ContinueExecute(float dt)
	{
		return ContinueExecuteChecks(dt);
	}

	public virtual void FinishExecute(bool cancelled)
	{
		cooldownUntilMs = entity.World.ElapsedMilliseconds + Config.MinCooldownMs + entity.World.Rand.Next(Config.MaxCooldownMs - Config.MinCooldownMs);
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + Config.MinCooldownHours + entity.World.Rand.NextDouble() * (Config.MaxCooldownHours - Config.MinCooldownHours);
		if (Config.AnimationMeta != null && Config.AnimationMeta.Code != "attack" && Config.AnimationMeta.Code != "idle")
		{
			entity.AnimManager.StopAnimation(Config.AnimationMeta.Code);
		}
		if (Config.FinishSound != null)
		{
			entity.World.PlaySoundAt(Config.FinishSound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
		}
		if (tagsAppliedOnStart != EntityTagArray.Empty)
		{
			entity.Tags &= ~tagsAppliedOnStart;
			entity.MarkTagsDirty();
		}
		active = false;
	}

	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		if (entity.State == EnumEntityState.Active)
		{
			IWorldAccessor worldAccessor = entity.World;
			cooldownUntilMs = worldAccessor.ElapsedMilliseconds + Config.MinCooldownMs + worldAccessor.Rand.Next(Config.MaxCooldownMs - Config.MinCooldownMs);
		}
	}

	public virtual bool Notify(string key, object data)
	{
		return false;
	}

	public virtual void OnEntityLoaded()
	{
	}

	public virtual void OnEntitySpawn()
	{
	}

	public virtual void OnEntityDespawn(EntityDespawnData reason)
	{
	}

	public virtual void OnEntityHurt(DamageSource source, float damage)
	{
		if (Config.StopOnHurt)
		{
			stopTask = true;
		}
		attackedByEntity = source.GetCauseEntity();
		if (attackedByEntity != null)
		{
			attackedByEntityMs = entity.World.ElapsedMilliseconds;
		}
	}

	public virtual void OnNoPath(Vec3d target)
	{
	}

	public virtual bool CanContinueExecute()
	{
		return true;
	}

	protected static TConfig LoadConfig<TConfig>(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) where TConfig : AiTaskBaseConfig
	{
		TConfig val;
		try
		{
			val = taskConfig.AsObject<TConfig>();
			if (val == null)
			{
				string value = taskConfig["code"]?.AsString("") ?? "";
				string text = $"Failed to parse task config '{value} ({typeof(TConfig)})' for entity '{entity.Code}'.";
				entity.Api.Logger.Error(text);
				throw new ArgumentNullException(text);
			}
		}
		catch (Exception value2)
		{
			string value3 = taskConfig["code"]?.AsString("") ?? "";
			entity.Api.Logger.Error($"Failed to parse task config '{value3} ({typeof(TConfig)})' for entity '{entity.Code}'. Exception:\n{value2}\n");
			throw;
		}
		try
		{
			val.Init(entity, taskConfig, aiConfig);
		}
		catch (Exception value4)
		{
			string value5 = taskConfig["code"]?.AsString("") ?? "";
			entity.Api.Logger.Error($"Failed initiate config for task '{value5} ({typeof(TConfig)})' for entity '{entity.Code}'. Exception:\n{value4}\n");
			throw;
		}
		if (!val.Initialized)
		{
			string message = $"Config was not initialized for task '{val.Code}' and entity '{entity.Code}'. Have you forgot to call 'base.Init()' method in '{typeof(TConfig)}.Init()'?";
			entity.Api.Logger.Error(message);
			throw new InvalidOperationException(message);
		}
		return val;
	}

	protected TConfig GetConfig<TConfig>() where TConfig : AiTaskBaseConfig
	{
		return (baseConfig as TConfig) ?? throw new InvalidOperationException($"Wrong type of config '{baseConfig.GetType()}', should be '{typeof(TConfig)}' or it subclass.");
	}

	protected virtual bool PreconditionsSatisficed()
	{
		if (!CheckExecutionChance())
		{
			return false;
		}
		if (!CheckCooldowns())
		{
			return false;
		}
		if (!CheckEntityState())
		{
			return false;
		}
		if (!CheckEmotionStates())
		{
			return false;
		}
		if (!CheckEntityLightLevel())
		{
			return false;
		}
		currentDayTimeInaccuracy = (float)entity.World.Rand.NextDouble() * Config.DayTimeFrameInaccuracy - Config.DayTimeFrameInaccuracy / 2f;
		if (!CheckDayTimeFrames())
		{
			return false;
		}
		if (!CheckTemperature())
		{
			return false;
		}
		if (Config.DontExecuteIfRecentlyAttacked && RecentlyAttacked)
		{
			return false;
		}
		return true;
	}

	protected virtual bool IsInEmotionState(params string[] emotionStates)
	{
		if (emotionStatesBehavior == null)
		{
			return false;
		}
		if (emotionStates.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < emotionStates.Length; i++)
		{
			if (emotionStatesBehavior.IsInEmotionState(emotionStates[i]))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual bool DurationExceeded()
	{
		if (durationUntilMs > 0)
		{
			return entity.World.ElapsedMilliseconds > durationUntilMs;
		}
		return false;
	}

	protected virtual bool CheckEntityLightLevel()
	{
		if (Config.EntityLightLevels[0] == 0 && Config.EntityLightLevels[1] == 32)
		{
			return true;
		}
		int lightLevel = entity.World.BlockAccessor.GetLightLevel((int)entity.Pos.X, (int)entity.Pos.InternalY, (int)entity.Pos.Z, Config.EntityLightLevelType);
		if (Config.EntityLightLevels[0] <= lightLevel)
		{
			return lightLevel <= Config.EntityLightLevels[1];
		}
		return false;
	}

	protected virtual bool CheckDayTimeFrames()
	{
		if (Config.DuringDayTimeFrames.Length == 0)
		{
			return true;
		}
		double hourOfDay = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f + currentDayTimeInaccuracy;
		DayTimeFrame[] duringDayTimeFrames = Config.DuringDayTimeFrames;
		foreach (DayTimeFrame dayTimeFrame in duringDayTimeFrames)
		{
			if (dayTimeFrame.Matches(hourOfDay))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual bool CheckTemperature()
	{
		if (Config.TemperatureRange == null)
		{
			return true;
		}
		float temperature = entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature;
		if (Config.TemperatureRange[0] <= temperature)
		{
			return temperature <= Config.TemperatureRange[1];
		}
		return false;
	}

	protected virtual bool CheckCooldowns()
	{
		if (cooldownUntilMs <= entity.World.ElapsedMilliseconds)
		{
			return cooldownUntilTotalHours <= entity.World.Calendar.TotalHours;
		}
		return false;
	}

	protected virtual bool CheckEntityState()
	{
		if (Config.WhenSwimming.HasValue && Config.WhenSwimming != entity.Swimming)
		{
			return false;
		}
		if (Config.WhenFeetInLiquid.HasValue && Config.WhenFeetInLiquid != entity.FeetInLiquid)
		{
			return false;
		}
		return true;
	}

	protected virtual bool CheckEmotionStates()
	{
		if (Config.WhenInEmotionState.Length != 0 && !IsInEmotionState(Config.WhenInEmotionState))
		{
			return false;
		}
		if (Config.WhenNotInEmotionState.Length != 0 && IsInEmotionState(Config.WhenNotInEmotionState))
		{
			return false;
		}
		return true;
	}

	protected virtual bool CheckExecutionChance()
	{
		return Rand.NextDouble() <= (double)Config.ExecutionChance;
	}

	protected virtual bool ContinueExecuteChecks(float dt)
	{
		if (stopTask)
		{
			stopTask = false;
			return false;
		}
		if (DurationExceeded())
		{
			durationUntilMs = 0L;
			return false;
		}
		if (Config.Sound != null && Config.SoundRepeatMs > 0 && entity.World.ElapsedMilliseconds > lastSoundTotalMs + Config.SoundRepeatMs)
		{
			entity.World.PlaySoundAt(Config.Sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
			lastSoundTotalMs = entity.World.ElapsedMilliseconds;
		}
		if (Config.StopIfOutOfDayTimeFrames && !CheckDayTimeFrames())
		{
			return false;
		}
		return true;
	}

	protected virtual int GetOwnGeneration()
	{
		int num = entity.WatchedAttributes.GetInt("generation");
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes != null && attributes.IsTrue("tamed"))
		{
			num += 10;
		}
		return num;
	}
}
