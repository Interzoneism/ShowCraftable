using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IMountableSeat
{
	SeatConfig Config { get; set; }

	string SeatId { get; set; }

	long PassengerEntityIdForInit { get; set; }

	bool DoTeleportOnUnmount { get; set; }

	Entity Entity { get; }

	Entity Passenger { get; }

	IMountable MountSupplier { get; }

	bool CanControl { get; }

	EnumMountAngleMode AngleMode { get; }

	AnimationMetaData SuggestedAnimation { get; }

	bool SkipIdleAnimation { get; }

	float FpHandPitchFollow { get; }

	Vec3f LocalEyePos { get; }

	EntityPos SeatPosition { get; }

	Matrixf RenderTransform { get; }

	EntityControls Controls { get; }

	void MountableToTreeAttributes(TreeAttribute tree);

	void DidUnmount(EntityAgent entityAgent);

	void DidMount(EntityAgent entityAgent);

	bool CanUnmount(EntityAgent entityAgent);

	bool CanMount(EntityAgent entityAgent);
}
