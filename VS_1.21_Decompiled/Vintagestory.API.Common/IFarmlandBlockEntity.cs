using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IFarmlandBlockEntity
{
	double TotalHoursForNextStage { get; }

	double TotalHoursFertilityCheck { get; }

	float[] Nutrients { get; }

	float MoistureLevel { get; }

	bool IsVisiblyMoist { get; }

	int[] OriginalFertility { get; }

	BlockPos Pos { get; }

	BlockPos UpPos { get; }

	ITreeAttribute CropAttributes { get; }
}
