using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorGait : EntityBehavior
{
	public readonly FastSmallDictionary<string, GaitMeta> Gaits = new FastSmallDictionary<string, GaitMeta>(1);

	public GaitMeta IdleGait;

	protected ICoreAPI? api;

	public GaitMeta CurrentGait
	{
		get
		{
			return Gaits[entity.WatchedAttributes.GetString("currentgait")];
		}
		set
		{
			entity.WatchedAttributes.SetString("currentgait", value.Code);
		}
	}

	public GaitMeta FallbackGait
	{
		get
		{
			if (CurrentGait.FallbackGaitCode != null)
			{
				return Gaits[CurrentGait.FallbackGaitCode];
			}
			return IdleGait;
		}
	}

	public bool IsIdle => CurrentGait == IdleGait;

	public bool IsBackward => CurrentGait.Backwards;

	public bool IsForward
	{
		get
		{
			if (!CurrentGait.Backwards)
			{
				return CurrentGait != IdleGait;
			}
			return false;
		}
	}

	public override string PropertyName()
	{
		return "gait";
	}

	public float GetYawMultiplier()
	{
		return CurrentGait?.YawMultiplier ?? 3.5f;
	}

	public void SetIdle()
	{
		CurrentGait = IdleGait;
	}

	public GaitMeta CascadingFallbackGait(int n)
	{
		GaitMeta gaitMeta = CurrentGait;
		while (n > 0)
		{
			if (gaitMeta.FallbackGaitCode == null)
			{
				return IdleGait;
			}
			gaitMeta = Gaits[gaitMeta.FallbackGaitCode];
			n--;
		}
		return gaitMeta;
	}

	public EntityBehaviorGait(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		api = entity.Api;
		GaitMeta[] array = attributes["gaits"].AsArray<GaitMeta>();
		foreach (GaitMeta gaitMeta in array)
		{
			Gaits[gaitMeta.Code] = gaitMeta;
		}
		string key = attributes["idleGait"].AsString("idle");
		if (!Gaits.TryGetValue(key, out IdleGait))
		{
			throw new ArgumentException("JSON error. No idle gait for {0}", entity.Code);
		}
		CurrentGait = IdleGait;
	}
}
