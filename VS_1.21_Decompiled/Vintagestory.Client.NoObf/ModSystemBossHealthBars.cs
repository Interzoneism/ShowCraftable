using System.Collections.Generic;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class ModSystemBossHealthBars : ModSystem
{
	private ICoreClientAPI capi;

	private EntityPartitioning partUtil;

	private List<HudBosshealthBars> trackedBosses = new List<HudBosshealthBars>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		partUtil = api.ModLoader.GetModSystem<EntityPartitioning>();
		api.Event.RegisterGameTickListener(onTick, 200, 12);
	}

	private void onTick(float dt)
	{
		List<EntityAgent> foundBosses = new List<EntityAgent>();
		Vec3d xYZ = capi.World.Player.Entity.Pos.XYZ;
		partUtil.WalkEntities(xYZ, 60.0, delegate(Entity e)
		{
			EntityBehaviorBoss behavior;
			if (e.Alive && e.IsInteractable && (behavior = e.GetBehavior<EntityBehaviorBoss>()) != null)
			{
				double distance = getDistance(capi.World.Player.Entity, e);
				if (behavior.ShowHealthBar && distance <= (double)behavior.BossHpbarRange)
				{
					foundBosses.Add(e as EntityAgent);
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		int num = 0;
		num = ((capi.World.Player.Entity.Pos.Dimension != 0) ? (-capi.World.Player.Entity.Pos.Dimension) : 2);
		xYZ.Y += num * 32768;
		partUtil.WalkEntities(xYZ, 60.0, delegate(Entity e)
		{
			EntityBehaviorBoss behavior;
			if (e.Alive && e.IsInteractable && (behavior = e.GetBehavior<EntityBehaviorBoss>()) != null)
			{
				double distance = getDistance(capi.World.Player.Entity, e);
				if (behavior.ShowHealthBar && distance <= (double)behavior.BossHpbarRange)
				{
					foundBosses.Add(e as EntityAgent);
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		int num2 = -1;
		for (int num3 = 0; num3 < trackedBosses.Count; num3++)
		{
			HudBosshealthBars hudBosshealthBars = trackedBosses[num3];
			if (foundBosses.Contains(hudBosshealthBars.TargetEntity))
			{
				foundBosses.Remove(hudBosshealthBars.TargetEntity);
				continue;
			}
			trackedBosses[num3].TryClose();
			trackedBosses[num3].Dispose();
			trackedBosses.RemoveAt(num3);
			num2 = num3;
			num3--;
		}
		foreach (EntityAgent item in foundBosses)
		{
			trackedBosses.Add(new HudBosshealthBars(capi, item, trackedBosses.Count));
		}
		if (num2 >= 0)
		{
			for (int num4 = num2; num4 < trackedBosses.Count; num4++)
			{
				trackedBosses[num4].barIndex = num4;
				trackedBosses[num4].ComposeGuis();
			}
		}
		foreach (HudBosshealthBars trackedBoss in trackedBosses)
		{
			int dimension = trackedBoss.Dimension;
			int dimension2 = trackedBoss.TargetEntity.ServerPos.Dimension;
			if (dimension2 != dimension)
			{
				trackedBoss.ComposeGuis();
				trackedBoss.Dimension = dimension2;
			}
		}
	}

	private double getDistance(Entity player, Entity entity)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		Vector3d val = new Vector3d(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
		Vector3d val2 = default(Vector3d);
		((Vector3d)(ref val2))._002Ector(player.Pos.X, player.Pos.Y, player.Pos.Z);
		return Vector3d.Distance(val, val2);
	}
}
