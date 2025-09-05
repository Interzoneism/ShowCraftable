using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Config;

public class Dimensions
{
	public const int NormalWorld = 0;

	public const int MiniDimensions = 1;

	public const int AltWorld = 2;

	public static int BlocksPreviewSubDimension_Client = -1;

	public const int subDimensionSize = 16384;

	public const int subDimensionIndexZMultiplier = 4096;

	public static int SubDimensionIdForPos(int posX, int posZ)
	{
		return posZ / 16384 * 4096 + posX / 16384;
	}

	public static bool ShouldNotTick(BlockPos pos, ICoreAPI api)
	{
		if (pos.dimension != 1)
		{
			return false;
		}
		int num = SubDimensionIdForPos(pos.X, pos.Z);
		if (!(api is ICoreServerAPI coreServerAPI))
		{
			return num == BlocksPreviewSubDimension_Client;
		}
		IMiniDimension miniDimension = coreServerAPI.Server.GetMiniDimension(num);
		if (miniDimension == null)
		{
			return false;
		}
		return miniDimension.BlocksPreviewSubDimension_Server == num;
	}

	public static bool ShouldNotTick(EntityPos pos, ICoreAPI api)
	{
		if (pos.Dimension != 1)
		{
			return false;
		}
		int num = SubDimensionIdForPos(pos.XInt, pos.ZInt);
		if (!(api is ICoreServerAPI coreServerAPI))
		{
			return num == BlocksPreviewSubDimension_Client;
		}
		IMiniDimension miniDimension = coreServerAPI.Server.GetMiniDimension(num);
		if (miniDimension == null)
		{
			return false;
		}
		return miniDimension.BlocksPreviewSubDimension_Server == num;
	}
}
