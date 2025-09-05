using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskGotoEntityConversable : AiTaskBaseR
{
	protected bool stuck;

	protected float currentFollowTime;

	protected string animationCode = "walk";

	public Entity TargetEntity { get; }

	public float MoveSpeed { get; set; } = 0.02f;

	public float SeekingRange { get; set; } = 25f;

	public float MaxFollowTime { get; set; } = 60f;

	public float AllowedExtraDistance { get; set; }

	public bool Finished => !pathTraverser.Ready;

	public AiTaskGotoEntityConversable(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		world.Logger.Error("This AI task 'AiTaskGotoEntityConversable' can only be created from code.");
		throw new InvalidOperationException("This AI task can only be created from code.");
	}

	public AiTaskGotoEntityConversable(EntityAgent entity, Entity target)
		: base(entity)
	{
		TargetEntity = target;
		baseConfig.AnimationMeta = new AnimationMetaData
		{
			Code = animationCode,
			Animation = animationCode,
			AnimationSpeed = 1f
		}.Init();
	}

	public override bool ShouldExecute()
	{
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		stuck = false;
		pathTraverser.NavigateTo_Async(TargetEntity.ServerPos.XYZ, MoveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck, null, 999);
		currentFollowTime = 0f;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		currentFollowTime += dt;
		pathTraverser.CurrentTarget.X = TargetEntity.ServerPos.X;
		pathTraverser.CurrentTarget.Y = TargetEntity.ServerPos.Y;
		pathTraverser.CurrentTarget.Z = TargetEntity.ServerPos.Z;
		Cuboidd cuboidd = TargetEntity.SelectionBox.ToDouble().Translate(TargetEntity.ServerPos.X, TargetEntity.ServerPos.Y, TargetEntity.ServerPos.Z);
		Vec3d vec = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(vec);
		float num2 = MinDistanceToTarget();
		if (currentFollowTime < MaxFollowTime && num < (double)(SeekingRange * SeekingRange) && num > (double)num2)
		{
			return !stuck;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		pathTraverser.Stop();
	}

	public virtual bool TargetReached()
	{
		Cuboidd cuboidd = TargetEntity.SelectionBox.ToDouble().Translate(TargetEntity.ServerPos.X, TargetEntity.ServerPos.Y, TargetEntity.ServerPos.Z);
		Vec3d vec = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(vec);
		float num2 = MinDistanceToTarget();
		return num < (double)num2;
	}

	protected virtual void OnStuck()
	{
		stuck = true;
	}

	protected virtual void OnGoalReached()
	{
		pathTraverser.Active = true;
	}

	protected virtual float MinDistanceToTarget()
	{
		return AllowedExtraDistance + Math.Max(0.8f, TargetEntity.SelectionBox.XSize / 2f + entity.SelectionBox.XSize / 2f);
	}
}
