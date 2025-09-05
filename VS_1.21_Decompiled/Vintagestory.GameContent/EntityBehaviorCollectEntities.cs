using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorCollectEntities : EntityBehavior
{
	private int waitTicks;

	private int lastCollectedEntityIndex;

	private Vec3d tmp = new Vec3d();

	private float itemsPerSecond = 23f;

	private float unconsumedDeltaTime;

	public EntityBehaviorCollectEntities(Entity entity)
		: base(entity)
	{
	}

	public override void OnGameTick(float deltaTime)
	{
		if (base.entity.State != EnumEntityState.Active || !base.entity.Alive)
		{
			return;
		}
		IPlayer player = (base.entity as EntityPlayer)?.Player;
		IServerPlayer obj = player as IServerPlayer;
		if (obj != null && obj.ItemCollectMode == 1)
		{
			EntityAgent obj2 = base.entity as EntityAgent;
			if (obj2 != null && !obj2.Controls.Sneak)
			{
				return;
			}
		}
		if (base.entity.IsActivityRunning("invulnerable"))
		{
			waitTicks = 3;
		}
		else
		{
			if (waitTicks-- > 0 || (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator))
			{
				return;
			}
			tmp.Set(base.entity.ServerPos.X, base.entity.ServerPos.InternalY + (double)base.entity.SelectionBox.Y1 + (double)(base.entity.SelectionBox.Y2 / 2f), base.entity.ServerPos.Z);
			Entity[] entitiesAround = base.entity.World.GetEntitiesAround(tmp, 1.5f, 1.5f, entityMatcher);
			if (entitiesAround.Length == 0)
			{
				unconsumedDeltaTime = 0f;
				return;
			}
			deltaTime = Math.Min(1f, deltaTime + unconsumedDeltaTime);
			while (deltaTime - 1f / itemsPerSecond > 0f)
			{
				Entity entity = null;
				int i;
				for (i = 0; i < entitiesAround.Length; i++)
				{
					if (entitiesAround[i] != null && i >= lastCollectedEntityIndex)
					{
						entity = entitiesAround[i];
						break;
					}
				}
				if (entity == null)
				{
					entity = entitiesAround[0];
					i = 0;
				}
				if (entity == null)
				{
					return;
				}
				if (!OnFoundCollectible(entity))
				{
					lastCollectedEntityIndex = (lastCollectedEntityIndex + 1) % entitiesAround.Length;
				}
				else
				{
					entitiesAround[i] = null;
				}
				deltaTime -= 1f / itemsPerSecond;
			}
			unconsumedDeltaTime = deltaTime;
		}
	}

	public virtual bool OnFoundCollectible(Entity foundEntity)
	{
		ItemStack itemStack = foundEntity.OnCollected(entity);
		bool flag = false;
		ItemStack itemStack2 = itemStack.Clone();
		if (itemStack != null && itemStack.StackSize > 0)
		{
			flag = entity.TryGiveItemStack(itemStack);
		}
		if (itemStack != null && itemStack.StackSize <= 0)
		{
			foundEntity.Die(EnumDespawnReason.PickedUp);
		}
		if (flag)
		{
			itemStack.Collectible.OnCollected(itemStack, entity);
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute["itemstack"] = new ItemstackAttribute(itemStack2.Clone());
			treeAttribute["byentityid"] = new LongAttribute(entity.EntityId);
			entity.World.Api.Event.PushEvent("onitemcollected", treeAttribute);
			entity.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), entity);
			return true;
		}
		return false;
	}

	private bool entityMatcher(Entity foundEntity)
	{
		return foundEntity.CanCollect(entity);
	}

	public override string PropertyName()
	{
		return "collectitems";
	}
}
