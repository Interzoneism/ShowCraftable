using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public abstract class ChiselMode
{
	public virtual int ChiselSize => 1;

	public abstract DrawSkillIconDelegate DrawAction(ICoreClientAPI capi);

	public virtual bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		Vec3i vec3i = voxelPos.Clone().Add(ChiselSize * facing.Normali.X, ChiselSize * facing.Normali.Y, ChiselSize * facing.Normali.Z);
		if (isBreak)
		{
			return chiselEntity.SetVoxel(voxelPos, add: false, byPlayer, currentMaterialIndex);
		}
		if (vec3i.X >= 0 && vec3i.X < 16 && vec3i.Y >= 0 && vec3i.Y < 16 && vec3i.Z >= 0 && vec3i.Z < 16)
		{
			return chiselEntity.SetVoxel(vec3i, add: true, byPlayer, currentMaterialIndex);
		}
		return false;
	}
}
