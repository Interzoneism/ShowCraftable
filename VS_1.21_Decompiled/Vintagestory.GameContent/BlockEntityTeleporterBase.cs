using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public abstract class BlockEntityTeleporterBase : BlockEntity
{
	protected TeleporterManager manager;

	protected Dictionary<long, TeleportingEntity> tpingEntities = new Dictionary<long, TeleportingEntity>();

	protected float TeleportWarmupSec = 3f;

	protected bool somebodyIsTeleporting;

	protected bool somebodyDidTeleport;

	private List<long> toremove = new List<long>();

	public long lastEntityCollideMs;

	public long lastOwnPlayerCollideMs;

	public bool tpLocationIsOffset;

	public Vec3d tmpPosVec = new Vec3d();

	public BlockEntityTeleporterBase()
	{
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		manager = api.ModLoader.GetModSystem<TeleporterManager>();
	}

	public abstract Vec3d GetTarget(Entity forEntity);

	public virtual void OnEntityCollide(Entity entity)
	{
		if (!tpingEntities.TryGetValue(entity.EntityId, out var value))
		{
			Dictionary<long, TeleportingEntity> dictionary = tpingEntities;
			long entityId = entity.EntityId;
			TeleportingEntity obj = new TeleportingEntity
			{
				Entity = entity
			};
			value = obj;
			dictionary[entityId] = obj;
		}
		value.LastCollideMs = Api.World.ElapsedMilliseconds;
		if (Api.Side == EnumAppSide.Client)
		{
			lastEntityCollideMs = Api.World.ElapsedMilliseconds;
			if ((Api as ICoreClientAPI).World.Player.Entity == entity)
			{
				lastOwnPlayerCollideMs = Api.World.ElapsedMilliseconds;
			}
		}
	}

	protected virtual void HandleTeleportingServer(float dt)
	{
		if (toremove == null)
		{
			throw new Exception("BETeleporterBase: toremove is null, it shouldn't be!");
		}
		if (tpingEntities == null)
		{
			throw new Exception("BETeleporterBase: tpingEntities is null, it shouldn't be!");
		}
		if (Api == null)
		{
			throw new Exception("BETeleporterBase: Api is null, it shouldn't be!");
		}
		toremove.Clear();
		bool flag = somebodyIsTeleporting;
		somebodyIsTeleporting &= tpingEntities.Count > 0;
		ICoreServerAPI coreServerAPI = Api as ICoreServerAPI;
		foreach (KeyValuePair<long, TeleportingEntity> tpingEntity in tpingEntities)
		{
			if (tpingEntity.Value == null)
			{
				throw new Exception("BETeleporterBase: val.Value is null, it shouldn't be!");
			}
			if (tpingEntity.Value.Entity == null)
			{
				throw new Exception("BETeleporterBase: val.Value.Entity is null, it shouldn't be!");
			}
			if (tpingEntity.Value.Entity.Teleporting)
			{
				continue;
			}
			tpingEntity.Value.SecondsPassed += Math.Min(0.5f, dt);
			if (Api.World.ElapsedMilliseconds - tpingEntity.Value.LastCollideMs > 100)
			{
				tmpPosVec.Set(tpingEntity.Value.Entity.Pos);
				tmpPosVec.Y = Math.Round(tmpPosVec.Y, 3);
				Block collidingBlock = Api.World.CollisionTester.GetCollidingBlock(Api.World.BlockAccessor, tpingEntity.Value.Entity.CollisionBox, tmpPosVec);
				if (collidingBlock == null || collidingBlock.GetType() != base.Block.GetType())
				{
					toremove.Add(tpingEntity.Key);
					continue;
				}
			}
			if ((double)tpingEntity.Value.SecondsPassed > 0.1 && !somebodyIsTeleporting)
			{
				somebodyIsTeleporting = true;
				MarkDirty();
			}
			Vec3d target = GetTarget(tpingEntity.Value.Entity);
			if ((double)tpingEntity.Value.SecondsPassed > 1.5 && target != null)
			{
				Vec3d vec3d = target.Clone();
				if (tpLocationIsOffset)
				{
					vec3d.Add(Pos.X, Pos.Y, Pos.Z);
				}
				IWorldChunk chunkAtBlockPos = Api.World.BlockAccessor.GetChunkAtBlockPos(vec3d.AsBlockPos);
				if (chunkAtBlockPos != null)
				{
					chunkAtBlockPos.MapChunk.MarkFresh();
				}
				else
				{
					coreServerAPI.WorldManager.LoadChunkColumnPriority((int)vec3d.X / 32, (int)vec3d.Z / 32, new ChunkLoadOptions
					{
						KeepLoaded = false
					});
				}
			}
			if (tpingEntity.Value.SecondsPassed > TeleportWarmupSec && target != null)
			{
				Vec3d vec3d2 = target.Clone();
				if (tpLocationIsOffset)
				{
					vec3d2.Add(Pos.X, Pos.Y, Pos.Z);
				}
				tpingEntity.Value.Entity.TeleportTo(vec3d2);
				toremove.Add(tpingEntity.Key);
				Entity entity = tpingEntity.Value.Entity;
				if (entity is EntityPlayer)
				{
					Api.World.Logger.Audit("Teleporting player {0} from {1} to {2}", (entity as EntityPlayer).GetBehavior<EntityBehaviorNameTag>().DisplayName, entity.Pos.AsBlockPos, target);
				}
				else
				{
					Api.World.Logger.Audit("Teleporting entity {0} from {1} to {2}", entity.Code, entity.Pos.AsBlockPos, target);
				}
				didTeleport(tpingEntity.Value.Entity);
				somebodyIsTeleporting = false;
				somebodyDidTeleport = true;
				MarkDirty();
			}
		}
		foreach (long item in toremove)
		{
			tpingEntities.Remove(item);
		}
		if (flag && !somebodyIsTeleporting)
		{
			MarkDirty();
		}
	}

	protected virtual void didTeleport(Entity entity)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		somebodyIsTeleporting = tree.GetBool("somebodyIsTeleporting");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("somebodyIsTeleporting", somebodyIsTeleporting);
		tree.SetBool("somebodyDidTeleport", somebodyDidTeleport);
		somebodyDidTeleport = false;
	}
}
