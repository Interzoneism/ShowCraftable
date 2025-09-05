using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public static class AnimationCache
{
	public static void ClearCache(ICoreAPI api)
	{
		api.ObjectCache["animCache"] = null;
	}

	public static void ClearCache(ICoreAPI api, Entity entity)
	{
		Dictionary<string, AnimCacheEntry> orCreate = ObjectCacheUtil.GetOrCreate(api, "animCache", () => new Dictionary<string, AnimCacheEntry>());
		string key = string.Concat(entity.Code, "-", entity.Properties.Client.ShapeForEntity.Base.ToString());
		orCreate.Remove(key);
	}

	public static IAnimationManager LoadAnimatorCached(this IAnimationManager manager, ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		return InitManager(api, manager, entity, entityShape, copyOverAnims, requirePosesOnServer, requireJointsForElements);
	}

	[Obsolete("Use manager.LoadAnimator() or manager.LoadAnimatorCached() instead")]
	public static IAnimationManager InitManager(ICoreAPI api, IAnimationManager manager, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		if (entityShape == null)
		{
			return new NoAnimationManager();
		}
		string key = string.Concat(entity.Code, "-", entity.Properties.Client.ShapeForEntity.Base.ToString());
		Dictionary<string, AnimCacheEntry> orCreate = ObjectCacheUtil.GetOrCreate(api, "animCache", () => new Dictionary<string, AnimCacheEntry>());
		entityShape.InitForAnimations(api.Logger, entity.Properties.Client.ShapeForEntity.Base.ToString(), requireJointsForElements);
		IAnimator animator = null;
		if (orCreate.TryGetValue(key, out var value))
		{
			manager.Init(entity.Api, entity);
			animator = (manager.Animator = ((api.Side == EnumAppSide.Client) ? ClientAnimator.CreateForEntity(entity, value.RootPoses, value.Animations, value.RootElems, entityShape.JointsById) : ServerAnimator.CreateForEntity(entity, value.RootPoses, value.Animations, value.RootElems, entityShape.JointsById, requirePosesOnServer)));
			manager.CopyOverAnimStates(copyOverAnims, animator);
		}
		else
		{
			animator = manager.LoadAnimator(api, entity, entityShape, copyOverAnims, requirePosesOnServer, requireJointsForElements);
			orCreate[key] = new AnimCacheEntry
			{
				Animations = entityShape.Animations,
				RootElems = (animator as AnimatorBase).RootElements,
				RootPoses = (animator as AnimatorBase).RootPoses
			};
		}
		return manager;
	}
}
