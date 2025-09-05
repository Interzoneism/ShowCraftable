using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IGroundStoredParticleEmitter
{
	bool ShouldSpawnGSParticles(IWorldAccessor world, ItemStack stack);

	void DoSpawnGSParticles(IAsyncParticleManager manager, BlockPos pos, Vec3f offset);
}
