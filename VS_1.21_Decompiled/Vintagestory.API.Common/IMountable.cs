using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public interface IMountable
{
	IMountableSeat[] Seats { get; }

	EntityPos Position { get; }

	double StepPitch { get; }

	Entity Controller { get; }

	Entity OnEntity { get; }

	EntityControls ControllingControls { get; }

	bool AnyMounted();
}
