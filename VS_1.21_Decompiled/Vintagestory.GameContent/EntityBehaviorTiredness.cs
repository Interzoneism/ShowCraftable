using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorTiredness : EntityBehavior
{
	public Random Rand;

	private double hoursTotal;

	private long listenerId;

	public float Tiredness
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("tiredness").GetFloat("tiredness");
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("tiredness").SetFloat("tiredness", value);
			entity.WatchedAttributes.MarkPathDirty("tiredness");
		}
	}

	public bool IsSleeping
	{
		get
		{
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("tiredness");
			if (treeAttribute != null)
			{
				return treeAttribute.GetInt("isSleeping") > 0;
			}
			return false;
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("tiredness").SetInt("isSleeping", value ? 1 : 0);
			entity.WatchedAttributes.MarkPathDirty("tiredness");
		}
	}

	public EntityBehaviorTiredness(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("tiredness");
		if (treeAttribute == null)
		{
			entity.WatchedAttributes.SetAttribute("tiredness", treeAttribute = new TreeAttribute());
			Tiredness = typeAttributes["currenttiredness"].AsFloat();
		}
		listenerId = entity.World.RegisterGameTickListener(SlowTick, 3000);
		hoursTotal = entity.World.Calendar.TotalHours;
	}

	private void SlowTick(float dt)
	{
		bool flag = IsSleeping;
		if (flag && (entity as EntityAgent)?.MountedOn == null)
		{
			flag = (IsSleeping = false);
		}
		if (!flag && entity.World.Side != EnumAppSide.Client)
		{
			float num = (float)(entity.World.Calendar.TotalHours - hoursTotal);
			Tiredness = GameMath.Clamp(Tiredness + num * 0.75f, 0f, entity.World.Calendar.HoursPerDay / 2f);
			hoursTotal = entity.World.Calendar.TotalHours;
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		entity.World.UnregisterGameTickListener(listenerId);
	}

	public override string PropertyName()
	{
		return "tiredness";
	}
}
