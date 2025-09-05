using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class TrackedPlayerProperties
{
	public int EyesInWaterColorShift;

	public int EyesInLavaColorShift;

	public float EyesInLavaDepth;

	public float EyesInWaterDepth;

	public float DayLight = 1f;

	public float DistanceToSpawnPoint;

	public float MoonLight;

	public double FallSpeed;

	public BlockPos PlayerChunkPos = new BlockPos();

	public BlockPos PlayerPosDiv8 = new BlockPos();

	public float posY;

	public float sunSlight = 21f;

	public string Playstyle;

	public string PlayListCode;
}
