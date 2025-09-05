using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBoatSeat : EntityRideableSeat
{
	public string actionAnim;

	public override EnumMountAngleMode AngleMode => config.AngleMode;

	private Dictionary<string, string> animations => (Entity as EntityBoat).MountAnimations;

	public override AnimationMetaData SuggestedAnimation
	{
		get
		{
			if (actionAnim == null)
			{
				return null;
			}
			Entity passenger = base.Passenger;
			AnimationMetaData value = default(AnimationMetaData);
			if (passenger != null && passenger.Properties?.Client.AnimationsByMetaCode?.TryGetValue(actionAnim, out value) == true)
			{
				return value;
			}
			return null;
		}
	}

	public EntityBoatSeat(IMountable mountablesupplier, string seatId, SeatConfig config)
		: base(mountablesupplier, seatId, config)
	{
		RideableClassName = "boat";
	}

	public override bool CanMount(EntityAgent entityAgent)
	{
		JsonObject attributes = config.Attributes;
		if (attributes != null && attributes["ropeTieablesOnly"].AsBool())
		{
			return entityAgent.HasBehavior<EntityBehaviorRopeTieable>();
		}
		return base.CanMount(entityAgent);
	}

	public override void DidMount(EntityAgent entityAgent)
	{
		base.DidMount(entityAgent);
		entityAgent.AnimManager.StartAnimation(config.Animation ?? animations["idle"]);
	}

	public override void DidUnmount(EntityAgent entityAgent)
	{
		if (base.Passenger != null)
		{
			base.Passenger.AnimManager?.StopAnimation(animations["ready"]);
			base.Passenger.AnimManager?.StopAnimation(animations["forwards"]);
			base.Passenger.AnimManager?.StopAnimation(animations["backwards"]);
			base.Passenger.AnimManager?.StopAnimation(animations["idle"]);
			base.Passenger.AnimManager?.StopAnimation(config.Animation);
			base.Passenger.SidedPos.Roll = 0f;
		}
		base.DidUnmount(entityAgent);
	}

	protected override void tryTeleportToFreeLocation()
	{
		IWorldAccessor world = base.Passenger.World;
		IBlockAccessor blockAccessor = base.Passenger.World.BlockAccessor;
		double num = 99.0;
		Vec3d vec3d = null;
		for (int i = -4; i <= 4; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				for (int k = -4; k <= 4; k++)
				{
					Vec3d vec3d2 = base.Passenger.ServerPos.XYZ.AsBlockPos.ToVec3d().Add((double)i + 0.5, (double)j + 0.1, (double)k + 0.5);
					Block blockRaw = blockAccessor.GetBlockRaw((int)vec3d2.X, (int)(vec3d2.Y - 0.15), (int)vec3d2.Z, 4);
					if (blockAccessor.GetBlockRaw((int)vec3d2.X, (int)vec3d2.Y, (int)vec3d2.Z, 2).Id == 0 && blockRaw.SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(blockAccessor, base.Passenger.CollisionBox, vec3d2, alsoCheckTouch: false))
					{
						float num2 = vec3d2.DistanceTo(base.Passenger.ServerPos.XYZ);
						if ((double)num2 < num)
						{
							num = num2;
							vec3d = vec3d2;
						}
					}
				}
			}
		}
		if (vec3d != null)
		{
			base.Passenger.TeleportTo(vec3d);
			return;
		}
		bool flag = false;
		int num3 = -1;
		while (!flag && num3 <= 1)
		{
			int num4 = -1;
			while (!flag && num4 <= 1)
			{
				Vec3d vec3d3 = base.Passenger.ServerPos.XYZ.AsBlockPos.ToVec3d().Add((double)num3 + 0.5, 1.1, (double)num4 + 0.5);
				if (!world.CollisionTester.IsColliding(blockAccessor, base.Passenger.CollisionBox, vec3d3, alsoCheckTouch: false))
				{
					base.Passenger.TeleportTo(vec3d3);
					flag = true;
					break;
				}
				num4++;
			}
			num3++;
		}
	}
}
