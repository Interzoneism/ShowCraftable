using System;
using System.Collections.Generic;
using System.Linq;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class AiTaskBaseTargetable : AiTaskBase, IWorldIntersectionSupplier
{
	protected EntityTagRule[] EntityTags = Array.Empty<EntityTagRule>();

	protected EntityTagRule[] SkipEntityTags = Array.Empty<EntityTagRule>();

	protected bool noTags = true;

	protected bool reverseTagsCheck;

	protected float MinTargetWeight;

	protected float MaxTargetWeight;

	protected string[] targetEntityCodesBeginsWith = Array.Empty<string>();

	protected string[] targetEntityCodesExact;

	protected AssetLocation[] skipEntityCodes;

	protected string targetEntityFirstLetters = "";

	protected EnumCreatureHostility creatureHostility;

	protected bool friendlyTarget;

	public Entity targetEntity;

	protected Entity attackedByEntity;

	protected long attackedByEntityMs;

	protected bool retaliateAttacks = true;

	public string triggerEmotionState;

	protected float tamingGenerations = 10f;

	protected EntityPartitioning partitionUtil;

	protected EntityBehaviorControlledPhysics bhPhysics;

	protected BlockSelection blockSel = new BlockSelection();

	protected EntitySelection entitySel = new EntitySelection();

	protected readonly Vec3d rayTraceFrom = new Vec3d();

	protected readonly Vec3d rayTraceTo = new Vec3d();

	protected readonly Vec3d tmpPos = new Vec3d();

	private Vec3d tmpVec = new Vec3d();

	protected Vec3d collTmpVec = new Vec3d();

	protected float stepHeight;

	public virtual bool AggressiveTargeting => true;

	public Entity TargetEntity => targetEntity;

	protected bool noEntityCodes
	{
		get
		{
			if (targetEntityCodesExact.Length == 0)
			{
				return targetEntityCodesBeginsWith.Length == 0;
			}
			return false;
		}
	}

	protected bool RecentlyAttacked => entity.World.ElapsedMilliseconds - attackedByEntityMs < 30000;

	public Vec3i MapSize => entity.World.BlockAccessor.MapSize;

	public IBlockAccessor blockAccessor => entity.World.BlockAccessor;

	protected AiTaskBaseTargetable(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		creatureHostility = entity.World.Config.GetString("creatureHostility") switch
		{
			"aggressive" => EnumCreatureHostility.Aggressive, 
			"passive" => EnumCreatureHostility.Passive, 
			"off" => EnumCreatureHostility.NeverHostile, 
			_ => EnumCreatureHostility.Aggressive, 
		};
		tamingGenerations = taskConfig["tamingGenerations"].AsFloat(10f);
		friendlyTarget = taskConfig["friendlyTarget"].AsBool();
		retaliateAttacks = taskConfig["retaliateAttacks"].AsBool(defaultValue: true);
		triggerEmotionState = taskConfig["triggerEmotionState"].AsString();
		skipEntityCodes = taskConfig["skipEntityCodes"].AsArray<string>()?.Select((string str) => AssetLocation.Create(str, entity.Code.Domain)).ToArray();
		InitializeTargetCodes(taskConfig["entityCodes"].AsArray(new string[1] { "player" }), ref targetEntityCodesExact, ref targetEntityCodesBeginsWith, ref targetEntityFirstLetters);
		List<List<string>> list = taskConfig["entityTags"].AsObject(new List<List<string>>());
		List<List<string>> list2 = taskConfig["skipEntityTags"].AsObject(new List<List<string>>());
		if (list != null)
		{
			EntityTags = list.Select((List<string> tagList) => new EntityTagRule(entity.Api, tagList)).ToArray();
		}
		if (list2 != null)
		{
			SkipEntityTags = list2.Select((List<string> tagList) => new EntityTagRule(entity.Api, tagList)).ToArray();
		}
		reverseTagsCheck = taskConfig["reverseTagsCheck"].AsBool();
		noTags = EntityTags.Length == 0 && SkipEntityTags.Length == 0;
		MinTargetWeight = taskConfig["MinTargetWeight"].AsFloat();
		MaxTargetWeight = taskConfig["MaxTargetWeight"].AsFloat(float.MaxValue);
	}

	public static void InitializeTargetCodes(string[] codes, ref string[] targetEntityCodesExact, ref string[] targetEntityCodesBeginsWith, ref string targetEntityFirstLetters)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string text in codes)
		{
			if (text.EndsWith('*'))
			{
				list2.Add(text.Substring(0, text.Length - 1));
			}
			else
			{
				list.Add(text);
			}
		}
		targetEntityCodesBeginsWith = list2.ToArray();
		targetEntityCodesExact = new string[list.Count];
		int num = 0;
		foreach (string item in list)
		{
			if (item.Length != 0)
			{
				targetEntityCodesExact[num++] = item;
				char c = item[0];
				if (targetEntityFirstLetters.IndexOf(c) < 0)
				{
					targetEntityFirstLetters += c;
				}
			}
		}
		string[] array = targetEntityCodesBeginsWith;
		foreach (string text2 in array)
		{
			if (text2.Length == 0)
			{
				targetEntityFirstLetters = "";
				break;
			}
			char c2 = text2[0];
			if (targetEntityFirstLetters.IndexOf(c2) < 0)
			{
				targetEntityFirstLetters += c2;
			}
		}
	}

	public override void AfterInitialize()
	{
		bhPhysics = entity.GetBehavior<EntityBehaviorControlledPhysics>();
	}

	public override void StartExecute()
	{
		stepHeight = bhPhysics?.StepHeight ?? 0.6f;
		base.StartExecute();
		if (triggerEmotionState != null)
		{
			entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState(triggerEmotionState, 1.0, targetEntity?.EntityId ?? 0);
		}
		EntityBehaviorControlledPhysics behavior = entity.GetBehavior<EntityBehaviorControlledPhysics>();
		if (behavior != null)
		{
			stepHeight = behavior.StepHeight;
		}
	}

	protected virtual bool CheckTargetWeight(float weight)
	{
		float num = ((entity.Properties.Weight > 0f) ? (weight / entity.Properties.Weight) : float.MaxValue);
		if (MinTargetWeight > 0f && num < MinTargetWeight)
		{
			return false;
		}
		if (MaxTargetWeight > 0f && num > MaxTargetWeight)
		{
			return false;
		}
		return true;
	}

	protected virtual bool CheckTargetTags(EntityTagArray tags)
	{
		if (!reverseTagsCheck)
		{
			if (EntityTagRule.IntersectsWithEach(tags, EntityTags))
			{
				if (SkipEntityTags.Length == 0)
				{
					return true;
				}
				if (!reverseTagsCheck)
				{
					if (!EntityTagRule.IntersectsWithEach(tags, SkipEntityTags))
					{
						return true;
					}
				}
				else if (!EntityTagRule.ContainsAllFromAtLeastOne(tags, SkipEntityTags))
				{
					return true;
				}
			}
		}
		else if (EntityTagRule.ContainsAllFromAtLeastOne(tags, EntityTags))
		{
			if (SkipEntityTags.Length == 0)
			{
				return true;
			}
			if (!reverseTagsCheck)
			{
				if (!EntityTagRule.IntersectsWithEach(tags, SkipEntityTags))
				{
					return true;
				}
			}
			else if (!EntityTagRule.ContainsAllFromAtLeastOne(tags, SkipEntityTags))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool IsTargetableEntityWithTags(Entity e, float range)
	{
		if (CheckTargetTags(e.Tags) && CheckTargetWeight(e.Properties.Weight))
		{
			if (e.Alive)
			{
				return CanSense(e, range);
			}
			return false;
		}
		if (targetEntityFirstLetters.Length == 0 || IsTargetEntity(e.Code.Path))
		{
			if (e.Alive)
			{
				return CanSense(e, range);
			}
			return false;
		}
		return false;
	}

	public virtual bool IsTargetableEntityNoTagsNoAll(Entity e, float range)
	{
		if (IsTargetEntity(e.Code.Path))
		{
			if (e.Alive)
			{
				return CanSense(e, range);
			}
			return false;
		}
		return false;
	}

	public virtual bool IsTargetableEntityNoTagsAll(Entity e, float range)
	{
		if (e.Alive)
		{
			return CanSense(e, range);
		}
		return false;
	}

	public virtual bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
	{
		if (!e.Alive)
		{
			return false;
		}
		if (ignoreEntityCode)
		{
			return CanSense(e, range);
		}
		if (!noTags && CheckTargetTags(e.Tags) && CheckTargetWeight(e.Properties.Weight))
		{
			return CanSense(e, range);
		}
		if (targetEntityFirstLetters.Length == 0 || IsTargetEntity(e.Code.Path))
		{
			return CanSense(e, range);
		}
		return false;
	}

	protected bool IsTargetEntity(string testPath)
	{
		if (targetEntityFirstLetters.IndexOf(testPath[0]) < 0)
		{
			return false;
		}
		string[] array = targetEntityCodesExact;
		for (int i = 0; i < array.Length; i++)
		{
			if (testPath == array[i])
			{
				return true;
			}
		}
		array = targetEntityCodesBeginsWith;
		for (int j = 0; j < array.Length; j++)
		{
			if (testPath.StartsWithFast(array[j]))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanSense(Entity e, double range)
	{
		if (e.EntityId == entity.EntityId || !e.IsInteractable)
		{
			return false;
		}
		if (e is EntityPlayer eplr)
		{
			return CanSensePlayer(eplr, range);
		}
		if (skipEntityCodes != null)
		{
			for (int i = 0; i < skipEntityCodes.Length; i++)
			{
				if (WildcardUtil.Match(skipEntityCodes[i], e.Code))
				{
					return false;
				}
			}
		}
		return true;
	}

	public virtual bool CanSensePlayer(EntityPlayer eplr, double range)
	{
		if (!friendlyTarget && AggressiveTargeting)
		{
			if (creatureHostility == EnumCreatureHostility.NeverHostile)
			{
				return false;
			}
			if (creatureHostility == EnumCreatureHostility.Passive && (bhEmo == null || (!IsInEmotionState("aggressiveondamage") && !IsInEmotionState("aggressivearoundentities"))))
			{
				return false;
			}
		}
		float num = eplr.Stats.GetBlended("animalSeekingRange");
		IPlayer player = eplr.Player;
		if (eplr.Controls.Sneak && eplr.OnGround)
		{
			num *= 0.6f;
		}
		if ((num == 1f || entity.ServerPos.DistanceTo(eplr.Pos) < range * (double)num) && targetablePlayerMode(player) && entity.ServerPos.Dimension == eplr.Pos.Dimension)
		{
			return true;
		}
		return false;
	}

	protected virtual bool targetablePlayerMode(IPlayer player)
	{
		if (player != null)
		{
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				return (player as IServerPlayer).ConnectionState == EnumClientState.Playing;
			}
			return false;
		}
		return true;
	}

	protected virtual bool hasDirectContact(Entity targetEntity, float minDist, float minVerDist)
	{
		if (targetEntity.Pos.Dimension != entity.Pos.Dimension)
		{
			return false;
		}
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		tmpPos.Set(entity.ServerPos).Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(tmpPos);
		double num2 = Math.Abs(cuboidd.ShortestVerticalDistanceFrom(tmpPos.Y));
		if (num >= (double)minDist || num2 >= (double)minVerDist)
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

	protected void updateTargetPosFleeMode(Vec3d targetPos, float yaw)
	{
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw - (float)Math.PI / 2f);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw - (float)Math.PI / 2f);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw + (float)Math.PI / 2f);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw + (float)Math.PI / 2f);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw + (float)Math.PI);
		targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw + (float)Math.PI);
	}

	protected bool traversable(Vec3d pos)
	{
		if (world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, pos, alsoCheckTouch: false))
		{
			return !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, collTmpVec.Set(pos).Add(0.0, Math.Min(1f, stepHeight), 0.0), alsoCheckTouch: false);
		}
		return true;
	}

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

	public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		return Array.Empty<Entity>();
	}

	public Entity GetGuardedEntity()
	{
		string text = entity.WatchedAttributes.GetString("guardedPlayerUid");
		if (text != null)
		{
			return entity.World.PlayerByUid(text)?.Entity;
		}
		long entityId = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
		return entity.World.GetEntityById(entityId);
	}

	public int GetOwnGeneration()
	{
		int num = entity.WatchedAttributes.GetInt("generation");
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes != null && attributes.IsTrue("tamed"))
		{
			num += 10;
		}
		return num;
	}

	protected bool isNonAttackingPlayer(Entity e)
	{
		if (attackedByEntity == null || (attackedByEntity != null && attackedByEntity.EntityId != e.EntityId))
		{
			return e is EntityPlayer;
		}
		return false;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		attackedByEntity = source.GetCauseEntity();
		attackedByEntityMs = entity.World.ElapsedMilliseconds;
		base.OnEntityHurt(source, damage);
	}

	public void ClearAttacker()
	{
		attackedByEntity = null;
		attackedByEntityMs = -9999L;
	}
}
