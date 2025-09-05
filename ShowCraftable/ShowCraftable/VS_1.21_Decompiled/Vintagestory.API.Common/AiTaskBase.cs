using System;
using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Essentials;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public abstract class AiTaskBase : IAiTask
{
	[ThreadStatic]
	private static Random randTL;

	public EntityAgent entity;

	public IWorldAccessor world;

	public AnimationMetaData animMeta;

	protected float priority;

	protected float priorityForCancel;

	protected int slot;

	public int Mincooldown;

	public int Maxcooldown;

	protected double mincooldownHours;

	protected double maxcooldownHours;

	protected AssetLocation finishSound;

	protected AssetLocation sound;

	protected float soundRange = 16f;

	protected int soundStartMs;

	protected int soundRepeatMs;

	protected float soundChance = 1.01f;

	protected long lastSoundTotalMs;

	public string WhenInEmotionState;

	public bool? WhenSwimming;

	public string WhenNotInEmotionState;

	protected long cooldownUntilMs;

	protected double cooldownUntilTotalHours;

	protected WaypointsTraverser pathTraverser;

	protected EntityBehaviorEmotionStates bhEmo;

	public DayTimeFrame[] duringDayTimeFrames;

	protected double defaultTimeoutSec = 30.0;

	protected TimeSpan timeout;

	protected long executeStartTimeMs;

	private string profilerName;

	public Random rand => randTL ?? (randTL = new Random());

	public string Id { get; set; }

	public string ProfilerName
	{
		get
		{
			return profilerName;
		}
		set
		{
			profilerName = value;
		}
	}

	public virtual int Slot => slot;

	public virtual float Priority
	{
		get
		{
			return priority;
		}
		set
		{
			priority = value;
		}
	}

	public virtual float PriorityForCancel => priorityForCancel;

	public AiTaskBase(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
	{
		this.entity = entity;
		world = entity.World;
		if (randTL == null)
		{
			randTL = new Random((int)entity.EntityId);
		}
		pathTraverser = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser;
		bhEmo = entity.GetBehavior<EntityBehaviorEmotionStates>();
		priority = taskConfig["priority"].AsFloat();
		priorityForCancel = taskConfig["priorityForCancel"].AsFloat(priority);
		Id = taskConfig["id"].AsString();
		slot = (taskConfig["slot"]?.AsInt()).Value;
		Mincooldown = (taskConfig["mincooldown"]?.AsInt()).Value;
		Maxcooldown = (taskConfig["maxcooldown"]?.AsInt(100)).Value;
		mincooldownHours = (taskConfig["mincooldownHours"]?.AsDouble()).Value;
		maxcooldownHours = (taskConfig["maxcooldownHours"]?.AsDouble()).Value;
		int value = (taskConfig["initialMinCoolDown"]?.AsInt(Mincooldown)).Value;
		int value2 = (taskConfig["initialMaxCoolDown"]?.AsInt(Maxcooldown)).Value;
		timeout = TimeSpan.FromSeconds(taskConfig["timeoutSec"]?.AsDouble(defaultTimeoutSec) ?? defaultTimeoutSec);
		JsonObject jsonObject = taskConfig["animation"];
		if (jsonObject.Exists)
		{
			string code = jsonObject.AsString()?.ToLowerInvariant();
			JsonObject jsonObject2 = taskConfig["animationSpeed"];
			float animationSpeed = jsonObject2.AsFloat(1f);
			AnimationMetaData animationMetaData = this.entity.Properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == code);
			if (animationMetaData != null)
			{
				if (jsonObject2.Exists)
				{
					animMeta = animationMetaData.Clone();
					animMeta.AnimationSpeed = animationSpeed;
				}
				else
				{
					animMeta = animationMetaData;
				}
			}
			else
			{
				animMeta = new AnimationMetaData
				{
					Code = code,
					Animation = code,
					AnimationSpeed = animationSpeed
				}.Init();
				animMeta.EaseInSpeed = 1f;
				animMeta.EaseOutSpeed = 1f;
			}
		}
		WhenSwimming = taskConfig["whenSwimming"]?.AsBool();
		WhenInEmotionState = taskConfig["whenInEmotionState"].AsString();
		WhenNotInEmotionState = taskConfig["whenNotInEmotionState"].AsString();
		JsonObject jsonObject3 = taskConfig["sound"];
		if (jsonObject3.Exists)
		{
			sound = AssetLocation.Create(jsonObject3.AsString(), entity.Code.Domain).WithPathPrefixOnce("sounds/");
			soundRange = taskConfig["soundRange"].AsFloat(16f);
			soundStartMs = taskConfig["soundStartMs"].AsInt();
			soundRepeatMs = taskConfig["soundRepeatMs"].AsInt();
		}
		JsonObject jsonObject4 = taskConfig["finishSound"];
		if (jsonObject4.Exists)
		{
			finishSound = AssetLocation.Create(jsonObject4.AsString(), entity.Code.Domain).WithPathPrefixOnce("sounds/");
		}
		duringDayTimeFrames = taskConfig["duringDayTimeFrames"].AsObject<DayTimeFrame[]>();
		cooldownUntilMs = entity.World.ElapsedMilliseconds + value + entity.World.Rand.Next(value2 - value);
	}

	protected bool PreconditionsSatisifed()
	{
		if (WhenSwimming.HasValue && WhenSwimming != entity.Swimming)
		{
			return false;
		}
		if (WhenInEmotionState != null && !IsInEmotionState(WhenInEmotionState))
		{
			return false;
		}
		if (WhenNotInEmotionState != null && IsInEmotionState(WhenNotInEmotionState))
		{
			return false;
		}
		if (!IsInValidDayTimeHours(initialRandomness: true))
		{
			return false;
		}
		return true;
	}

	protected bool IsInValidDayTimeHours(bool initialRandomness)
	{
		if (duringDayTimeFrames != null)
		{
			double num = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f;
			if (initialRandomness)
			{
				num += entity.World.Rand.NextDouble() * 0.30000001192092896 - 0.15000000596046448;
			}
			for (int i = 0; i < duringDayTimeFrames.Length; i++)
			{
				if (duringDayTimeFrames[i].Matches(num))
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	protected bool IsInEmotionState(string emostate)
	{
		if (bhEmo == null)
		{
			return false;
		}
		if (emostate.ContainsFast('|'))
		{
			string[] array = emostate.Split("|");
			for (int i = 0; i < array.Length; i++)
			{
				if (bhEmo.IsInEmotionState(array[i]))
				{
					return true;
				}
			}
			return false;
		}
		return bhEmo.IsInEmotionState(emostate);
	}

	public virtual void AfterInitialize()
	{
	}

	public abstract bool ShouldExecute();

	public virtual void StartExecute()
	{
		if (animMeta != null)
		{
			entity.AnimManager.StartAnimation(animMeta);
		}
		if (sound != null && entity.World.Rand.NextDouble() <= (double)soundChance)
		{
			if (soundStartMs > 0)
			{
				entity.World.RegisterCallback(delegate
				{
					entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
					lastSoundTotalMs = entity.World.ElapsedMilliseconds;
				}, soundStartMs);
			}
			else
			{
				entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
				lastSoundTotalMs = entity.World.ElapsedMilliseconds;
			}
		}
		executeStartTimeMs = entity.World.ElapsedMilliseconds;
	}

	public virtual bool ContinueExecute(float dt)
	{
		if (sound != null && soundRepeatMs > 0 && entity.World.ElapsedMilliseconds > lastSoundTotalMs + soundRepeatMs)
		{
			entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
			lastSoundTotalMs = entity.World.ElapsedMilliseconds;
		}
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		return true;
	}

	public virtual void FinishExecute(bool cancelled)
	{
		cooldownUntilMs = entity.World.ElapsedMilliseconds + Mincooldown + entity.World.Rand.Next(Maxcooldown - Mincooldown);
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + mincooldownHours + entity.World.Rand.NextDouble() * (maxcooldownHours - mincooldownHours);
		if (animMeta != null && animMeta.Code != "attack" && animMeta.Code != "idle")
		{
			entity.AnimManager.StopAnimation(animMeta.Code);
		}
		if (finishSound != null)
		{
			entity.World.PlaySoundAt(finishSound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
		}
	}

	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		if (entity.State == EnumEntityState.Active)
		{
			IWorldAccessor worldAccessor = entity.World;
			cooldownUntilMs = worldAccessor.ElapsedMilliseconds + Mincooldown + worldAccessor.Rand.Next(Maxcooldown - Mincooldown);
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
	}

	public virtual void OnNoPath(Vec3d target)
	{
	}

	public virtual bool CanContinueExecute()
	{
		return true;
	}

	protected virtual bool timeoutExceeded()
	{
		return (double)(entity.World.ElapsedMilliseconds - executeStartTimeMs) > timeout.TotalMilliseconds;
	}
}
