using System;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class AiTaskBaseTargetableR : AiTaskBaseR, IWorldIntersectionSupplier
{
	protected Entity? targetEntity;

	protected long lastTargetSearchMs;

	private BlockSelection blockSel = new BlockSelection();

	private EntitySelection entitySel = new EntitySelection();

	private readonly Vec3d rayTraceFrom = new Vec3d();

	private readonly Vec3d rayTraceTo = new Vec3d();

	private readonly Vec3d tmpPos = new Vec3d();

	protected EntityBehaviorControlledPhysics? physicsBehavior;

	protected float stepHeight;

	public Entity? TargetEntity => targetEntity;

	public virtual bool AggressiveTargeting => true;

	public Vec3i MapSize => entity.World.BlockAccessor.MapSize;

	public IBlockAccessor blockAccessor => entity.World.BlockAccessor;

	protected bool RecentlySearchedForTarget => entity.World.ElapsedMilliseconds - lastTargetSearchMs < Config.TargetSearchCooldownMs;

	protected virtual string[] HostileEmotionStates => new string[2] { "aggressiveondamage", "aggressivearoundentities" };

	private AiTaskBaseTargetableConfig Config => GetConfig<AiTaskBaseTargetableConfig>();

	public Block GetBlock(BlockPos pos)
	{
		return entity.World.BlockAccessor.GetBlock(pos);
	}

	public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
	{
		return entity.World.BlockAccessor.GetBlock(pos).GetCollisionBoxes(entity.World.BlockAccessor, pos);
	}

	public bool IsValidPos(BlockPos pos)
	{
		return entity.World.BlockAccessor.IsValidPos(pos);
	}

	public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity>? matches = null)
	{
		return Array.Empty<Entity>();
	}

	protected AiTaskBaseTargetableR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskBaseTargetableConfig>(entity, taskConfig, aiConfig);
		lastTargetSearchMs = entity.World.ElapsedMilliseconds - base.Rand.Next(Config.TargetSearchCooldownMs);
	}

	protected AiTaskBaseTargetableR(EntityAgent entity)
		: base(entity)
	{
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>() ?? throw new ArgumentException("EntityPartitioning mod system is not found");
		AiTaskRegistry.TaskCodes.TryGetValue(GetType(), out string value);
		baseConfig = new AiTaskBaseTargetableConfig();
		baseConfig.Code = ((baseConfig.Code == "") ? (value ?? "") : baseConfig.Code);
		baseConfig.Init(entity);
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		physicsBehavior = entity.GetBehavior<EntityBehaviorControlledPhysics>();
	}

	public override void StartExecute()
	{
		stepHeight = physicsBehavior?.StepHeight ?? 0.6f;
		base.StartExecute();
		if (Config.TriggerEmotionState != null)
		{
			entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState(Config.TriggerEmotionState, 1.0, targetEntity?.EntityId ?? 0);
		}
	}

	public virtual void ClearAttacker()
	{
		attackedByEntity = null;
		attackedByEntityMs = -2 * Config.RecentlyAttackedTimeoutMs;
	}

	protected virtual bool IsTargetableEntity(Entity target, float range)
	{
		if (!target.Alive && Config.IgnoreDeadEntities)
		{
			return false;
		}
		if (!CheckTargetHerdId(target))
		{
			return false;
		}
		if (Config.TargetEverything)
		{
			return CanSense(target, range);
		}
		if (CheckTargetWeight(target.Properties.Weight) && CheckTargetTags(target.Tags))
		{
			return CanSense(target, range);
		}
		if (CheckTargetCodes(target.Code.Path))
		{
			return CanSense(target, range);
		}
		return false;
	}

	protected virtual bool CheckTargetHerdId(Entity target)
	{
		if (Config.TargetEntitiesWithSameHerdId)
		{
			EntityAgent entityAgent = entity;
			if (entityAgent != null && target is EntityAgent entityAgent2 && entityAgent.HerdId != 0L && entityAgent.HerdId != entityAgent2.HerdId)
			{
				return false;
			}
		}
		if (Config.TargetEntitiesWithDifferentHerdId)
		{
			EntityAgent entityAgent3 = entity;
			if (entityAgent3 != null && target is EntityAgent entityAgent4 && entityAgent3.HerdId != 0L && entityAgent3.HerdId == entityAgent4.HerdId)
			{
				return false;
			}
		}
		return true;
	}

	protected virtual bool CheckTargetWeight(float weight)
	{
		float num = ((entity.Properties.Weight > 0f) ? (weight / entity.Properties.Weight) : float.MaxValue);
		if (Config.MinTargetWeight > 0f && num < Config.MinTargetWeight)
		{
			return false;
		}
		if (Config.MaxTargetWeight > 0f && num > Config.MaxTargetWeight)
		{
			return false;
		}
		return true;
	}

	protected virtual bool CheckTargetTags(EntityTagArray tags)
	{
		if (Config.NoTags)
		{
			return false;
		}
		if (!Config.ReverseTagsCheck)
		{
			if (EntityTagRule.IntersectsWithEach(tags, Config.EntityTags))
			{
				if (Config.SkipEntityTags.Length == 0)
				{
					return true;
				}
				if (!Config.ReverseSkipTagsCheck)
				{
					if (!EntityTagRule.IntersectsWithEach(tags, Config.SkipEntityTags))
					{
						return true;
					}
				}
				else if (!EntityTagRule.ContainsAllFromAtLeastOne(tags, Config.SkipEntityTags))
				{
					return true;
				}
			}
		}
		else if (EntityTagRule.ContainsAllFromAtLeastOne(tags, Config.EntityTags))
		{
			if (Config.SkipEntityTags.Length == 0)
			{
				return true;
			}
			if (!Config.ReverseSkipTagsCheck)
			{
				if (!EntityTagRule.IntersectsWithEach(tags, Config.SkipEntityTags))
				{
					return true;
				}
			}
			else if (!EntityTagRule.ContainsAllFromAtLeastOne(tags, Config.SkipEntityTags))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual bool CheckTargetCodes(string testPath)
	{
		if (Config.TargetEntityFirstLetters.Length == 0)
		{
			return false;
		}
		if (Config.TargetEntityFirstLetters.IndexOf(testPath[0]) < 0)
		{
			return false;
		}
		for (int i = 0; i < Config.TargetEntityCodesExact.Length; i++)
		{
			if (testPath == Config.TargetEntityCodesExact[i])
			{
				return true;
			}
		}
		for (int j = 0; j < Config.TargetEntityCodesBeginsWith.Length; j++)
		{
			if (testPath.StartsWithFast(Config.TargetEntityCodesBeginsWith[j]))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual float GetTargetLightLevelRangeMultiplier(Entity target)
	{
		if (Config.IgnoreTargetLightLevel)
		{
			return 1f;
		}
		int lightLevel = entity.World.BlockAccessor.GetLightLevel(target.Pos.AsBlockPos, Config.TargetLightLevelType);
		if (lightLevel <= Config.TargetLightLevels[0] || lightLevel >= Config.TargetLightLevels[3])
		{
			return 0f;
		}
		if (lightLevel >= Config.TargetLightLevels[1] && lightLevel <= Config.TargetLightLevels[2])
		{
			return 1f;
		}
		if (lightLevel <= Config.TargetLightLevels[1] && Config.TargetLightLevels[0] != Config.TargetLightLevels[1])
		{
			return (float)(lightLevel - Config.TargetLightLevels[0]) / (float)(Config.TargetLightLevels[1] - Config.TargetLightLevels[0]);
		}
		if (lightLevel >= Config.TargetLightLevels[2] && Config.TargetLightLevels[2] != Config.TargetLightLevels[3])
		{
			return 1f - (float)((lightLevel - Config.TargetLightLevels[2]) / (Config.TargetLightLevels[3] - Config.TargetLightLevels[2]));
		}
		return 1f;
	}

	protected virtual bool CanSense(Entity target, double range)
	{
		if (!target.Alive && Config.IgnoreDeadEntities)
		{
			return false;
		}
		if (target.EntityId == entity.EntityId || (!target.IsInteractable && Config.TargetOnlyInteractableEntities))
		{
			return false;
		}
		if (Config.SkipEntityCodes.Length != 0)
		{
			for (int i = 0; i < Config.SkipEntityCodes.Length; i++)
			{
				if (WildcardUtil.Match(Config.SkipEntityCodes[i], target.Code))
				{
					return false;
				}
			}
		}
		if (target is EntityPlayer target2 && !CanSensePlayer(target2, range))
		{
			return false;
		}
		if (!CheckDetectionRange(target, range))
		{
			return false;
		}
		return true;
	}

	protected virtual bool CanSensePlayer(EntityPlayer target, double range)
	{
		if (!CheckEntityHostility(target))
		{
			return false;
		}
		if (!TargetablePlayerMode(target))
		{
			return false;
		}
		return true;
	}

	protected virtual bool CheckEntityHostility(EntityPlayer target)
	{
		if (!Config.FriendlyTarget && AggressiveTargeting)
		{
			return Config.CreatureHostility switch
			{
				EnumCreatureHostility.Aggressive => true, 
				EnumCreatureHostility.Passive => emotionStatesBehavior == null || !IsInEmotionState(HostileEmotionStates), 
				EnumCreatureHostility.NeverHostile => false, 
				_ => false, 
			};
		}
		return true;
	}

	protected virtual bool CheckDetectionRange(Entity target, double range)
	{
		if (entity.ServerPos.Dimension != target.Pos.Dimension)
		{
			return false;
		}
		float detectionRangeMultiplier = GetDetectionRangeMultiplier(target);
		if (detectionRangeMultiplier <= 0f)
		{
			return false;
		}
		if (detectionRangeMultiplier != 1f && entity.ServerPos.DistanceTo(target.Pos) > range * (double)detectionRangeMultiplier)
		{
			return false;
		}
		return true;
	}

	protected virtual float GetDetectionRangeMultiplier(Entity target)
	{
		float num = 1f;
		if (!Config.IgnoreTargetLightLevel)
		{
			num *= GetTargetLightLevelRangeMultiplier(target);
		}
		if (target is EntityAgent entityAgent && entityAgent.Controls.Sneak && target.OnGround && target.OnGround)
		{
			num *= Config.SneakRangeReduction;
		}
		if (Config.SeekingRangeAffectedByPlayerStat && target is EntityPlayer)
		{
			num *= target.Stats.GetBlended("animalSeekingRange");
		}
		return num;
	}

	protected virtual bool TargetablePlayerMode(EntityPlayer target)
	{
		if (Config.TargetPlayerInAllGameModes)
		{
			return true;
		}
		if (target.Player is IServerPlayer serverPlayer)
		{
			if (serverPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && serverPlayer.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				if (serverPlayer != null)
				{
					return serverPlayer.ConnectionState == EnumClientState.Playing;
				}
				return false;
			}
			return false;
		}
		return true;
	}

	protected virtual Entity? GetGuardedEntity()
	{
		string text = entity.WatchedAttributes.GetString("guardedPlayerUid");
		if (text != null)
		{
			return entity.World.PlayerByUid(text)?.Entity;
		}
		long num = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
		if (num != 0L)
		{
			entity.World.GetEntityById(num);
		}
		return null;
	}

	protected virtual bool IsNonAttackingPlayer(Entity target)
	{
		if (attackedByEntity == null || attackedByEntity.EntityId != target.EntityId)
		{
			return target is EntityPlayer;
		}
		return false;
	}

	protected virtual bool HasDirectContact(Entity targetEntity, float maxDistance, float maxVerticalDistance)
	{
		if (targetEntity.Pos.Dimension != entity.Pos.Dimension)
		{
			return false;
		}
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		tmpPos.Set(entity.ServerPos).Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(tmpPos);
		double num2 = Math.Abs(cuboidd.ShortestVerticalDistanceFrom(tmpPos.Y));
		if (num >= (double)maxDistance || num2 >= (double)maxVerticalDistance)
		{
			return false;
		}
		rayTraceFrom.Set(entity.ServerPos);
		rayTraceFrom.Y += 1.0 / 32.0;
		rayTraceTo.Set(targetEntity.ServerPos);
		rayTraceTo.Y += 1.0 / 32.0;
		bool flag = false;
		entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
		flag = blockSel == null;
		if (!flag)
		{
			rayTraceFrom.Y += entity.SelectionBox.Y2 * 7f / 16f;
			rayTraceTo.Y += targetEntity.SelectionBox.Y2 * 7f / 16f;
			entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
			flag = blockSel == null;
		}
		if (!flag)
		{
			rayTraceFrom.Y += entity.SelectionBox.Y2 * 7f / 16f;
			rayTraceTo.Y += targetEntity.SelectionBox.Y2 * 7f / 16f;
			entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
			flag = blockSel == null;
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	protected virtual float GetFearReductionFactor()
	{
		if (!Config.UseFearReductionFactor)
		{
			return 1f;
		}
		return Math.Max(0f, (Config.TamingGenerations - (float)GetOwnGeneration()) / Config.TamingGenerations);
	}

	protected virtual bool SearchForTarget()
	{
		float seekingRange = GetSeekingRange();
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, seekingRange, (Entity entity) => IsTargetableEntity(entity, seekingRange), Config.SearchType);
		return targetEntity != null;
	}

	protected virtual float GetSeekingRange()
	{
		float fearReductionFactor = GetFearReductionFactor();
		return Config.SeekingRange * fearReductionFactor;
	}

	protected virtual float GetAverageSize(Entity target)
	{
		return target.SelectionBox.XSize / 2f + entity.SelectionBox.XSize / 2f;
	}

	protected virtual bool CheckAndResetSearchCooldown()
	{
		if (RecentlySearchedForTarget)
		{
			return false;
		}
		lastTargetSearchMs = entity.World.ElapsedMilliseconds;
		return true;
	}

	protected virtual bool ShouldRetaliate()
	{
		if (Config.RetaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && CanSense(attackedByEntity, Config.SeekingRange))
		{
			return !entity.ToleratesDamageFrom(attackedByEntity);
		}
		return false;
	}

	protected virtual float MinDistanceToTarget(float extraDistance = 0f)
	{
		return entity.SelectionBox.XSize / 2f + (targetEntity?.SelectionBox.XSize ?? 0f) / 2f + extraDistance;
	}
}
