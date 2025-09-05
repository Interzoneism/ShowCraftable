using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface ILandClaimAPI
{
	List<LandClaim> All { get; }

	EnumWorldAccessResponse TestAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag);

	bool TryAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag);

	LandClaim[] Get(BlockPos pos);

	void Add(LandClaim claim);

	bool Remove(LandClaim claim);
}
