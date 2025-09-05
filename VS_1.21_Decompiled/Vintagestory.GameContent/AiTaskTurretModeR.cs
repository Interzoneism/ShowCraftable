using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskTurretModeR : AiTaskShootAtEntityR
{
	protected int searchWaitMs = 2000;

	protected EnumTurretState currentState;

	protected IProjectile? prevProjectile;

	protected float currentStateTime;

	private AiTaskTurretModeConfig Config => GetConfig<AiTaskTurretModeConfig>();

	protected virtual bool inFiringRange
	{
		get
		{
			double num = targetEntity?.ServerPos.DistanceTo(entity.ServerPos) ?? double.MaxValue;
			if (num >= (double)Config.FiringRangeMin)
			{
				return num <= (double)Config.FiringRangeMax;
			}
			return false;
		}
	}

	protected virtual bool inSensingRange => (targetEntity?.ServerPos.DistanceTo(entity.ServerPos) ?? 3.4028234663852886E+38) <= (double)GetSeekingRange();

	protected virtual bool inAbortRange => (targetEntity?.ServerPos.DistanceTo(entity.ServerPos) ?? 3.4028234663852886E+38) <= (double)Config.AbortRange;

	public AiTaskTurretModeR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskTurretModeConfig>(entity, taskConfig, aiConfig);
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		entity.AnimManager.OnAnimationStopped += AnimManager_OnAnimationStopped;
	}

	public override bool ShouldExecute()
	{
		if (base.ShouldExecute())
		{
			return !inAbortRange;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		currentState = EnumTurretState.Idle;
		currentStateTime = 0f;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecuteChecks(dt))
		{
			return false;
		}
		if (targetEntity == null)
		{
			return false;
		}
		currentStateTime += dt;
		UpdateState();
		AdjustYaw(dt);
		return currentState != EnumTurretState.Stop;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.StopAnimation(Config.TurretAnimation);
		entity.StopAnimation(Config.HoldAnimation);
		prevProjectile = null;
	}

	protected virtual void UpdateState()
	{
		currentState = currentState switch
		{
			EnumTurretState.Idle => Idle(currentState), 
			EnumTurretState.TurretMode => TurretMode(currentState), 
			EnumTurretState.TurretModeLoad => TurretModeLoad(currentState), 
			EnumTurretState.TurretModeHold => TurretModeHold(currentState), 
			EnumTurretState.TurretModeFired => TurretModeFired(currentState), 
			EnumTurretState.TurretModeReload => TurretModeReload(currentState), 
			EnumTurretState.TurretModeUnload => TurretModeUnload(currentState), 
			EnumTurretState.Stop => currentState, 
			_ => currentState, 
		};
	}

	protected virtual EnumTurretState Idle(EnumTurretState state)
	{
		if (inFiringRange)
		{
			entity.StartAnimation(Config.LoadAnimation);
			currentStateTime = 0f;
			return EnumTurretState.TurretMode;
		}
		if (inSensingRange)
		{
			entity.StartAnimation(Config.TurretAnimation);
			currentStateTime = 0f;
			return EnumTurretState.TurretMode;
		}
		return state;
	}

	protected virtual EnumTurretState TurretMode(EnumTurretState state)
	{
		if (!IsAnimationFinished(Config.TurretAnimation))
		{
			return state;
		}
		if (inAbortRange)
		{
			Abort();
			return state;
		}
		if (inFiringRange)
		{
			entity.StopAnimation(Config.TurretAnimation);
			entity.StartAnimation(Config.LoadFromTurretAnimation);
			if (Config.DrawSound != null)
			{
				entity.World.PlaySoundAt(Config.DrawSound, entity, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
			}
			currentStateTime = 0f;
			return EnumTurretState.TurretModeLoad;
		}
		if (currentStateTime > 5f)
		{
			entity.StopAnimation(Config.TurretAnimation);
			return EnumTurretState.Stop;
		}
		return state;
	}

	protected virtual EnumTurretState TurretModeLoad(EnumTurretState state)
	{
		if (!IsAnimationFinished(Config.LoadAnimation))
		{
			return state;
		}
		entity.StartAnimation(Config.HoldAnimation);
		currentStateTime = 0f;
		return EnumTurretState.TurretModeHold;
	}

	protected virtual EnumTurretState TurretModeHold(EnumTurretState state)
	{
		if (inFiringRange || inAbortRange)
		{
			if ((double)currentStateTime > 1.25)
			{
				SetOrAdjustDispersion();
				ShootProjectile();
				entity.StopAnimation(Config.HoldAnimation);
				entity.StartAnimation(Config.FireAnimation);
				return EnumTurretState.TurretModeFired;
			}
			return state;
		}
		if (currentStateTime > 2f)
		{
			entity.StopAnimation(Config.HoldAnimation);
			entity.StartAnimation(Config.UnloadAnimation);
			return EnumTurretState.TurretModeUnload;
		}
		return state;
	}

	protected virtual EnumTurretState TurretModeFired(EnumTurretState state)
	{
		if (targetEntity == null)
		{
			stopTask = true;
			return EnumTurretState.Stop;
		}
		float seekingRange = Config.SeekingRange;
		if (inAbortRange || !targetEntity.Alive || (targetEntity is EntityPlayer target && !TargetablePlayerMode(target)) || !HasDirectContact(targetEntity, seekingRange, seekingRange / 2f))
		{
			Abort();
			return state;
		}
		if (inSensingRange)
		{
			entity.StartAnimation(Config.ReloadAnimation);
			if (Config.ReloadSound != null)
			{
				entity.World.PlaySoundAt(Config.ReloadSound, entity, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
			}
			return EnumTurretState.TurretModeReload;
		}
		return state;
	}

	protected virtual EnumTurretState TurretModeReload(EnumTurretState state)
	{
		if (!IsAnimationFinished(Config.ReloadAnimation))
		{
			return state;
		}
		if (inAbortRange)
		{
			Abort();
			return state;
		}
		if (Config.DrawSound != null)
		{
			entity.World.PlaySoundAt(Config.DrawSound, entity, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
		}
		return EnumTurretState.TurretModeLoad;
	}

	protected virtual EnumTurretState TurretModeUnload(EnumTurretState state)
	{
		if (!IsAnimationFinished(Config.UnloadAnimation))
		{
			return state;
		}
		return EnumTurretState.Stop;
	}

	protected virtual void Abort()
	{
		currentState = EnumTurretState.Stop;
		entity.StopAnimation(Config.HoldAnimation);
		entity.StopAnimation(Config.TurretAnimation);
		AiTaskManager aiTaskManager = entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager ?? throw new InvalidOperationException("Failed to get task manager");
		AiTaskStayInRangeR task = aiTaskManager.GetTask<AiTaskStayInRangeR>();
		if (task != null && targetEntity != null)
		{
			task.TargetEntity = targetEntity;
			aiTaskManager.ExecuteTask<AiTaskStayInRangeR>();
		}
	}

	protected virtual bool IsAnimationFinished(string animationCode)
	{
		RunningAnimation animationState = entity.AnimManager.GetAnimationState(animationCode);
		if (animationState == null)
		{
			return false;
		}
		if (animationState.Running)
		{
			return animationState.AnimProgress >= Config.FinishedAnimationProgress;
		}
		return true;
	}

	protected virtual void AnimManager_OnAnimationStopped(string anim)
	{
		if (active && targetEntity != null)
		{
			UpdateState();
		}
	}
}
