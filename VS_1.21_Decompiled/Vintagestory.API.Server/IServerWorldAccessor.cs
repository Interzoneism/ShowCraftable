using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IServerWorldAccessor : IWorldAccessor
{
	ConcurrentDictionary<long, Entity> LoadedEntities { get; }

	OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators { get; }

	Dictionary<string, string> RemappedEntities { get; }

	void DespawnEntity(Entity entity, EntityDespawnData reason);

	void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1f, string ignitedByPlayerUid = null);

	bool IsFullyLoadedChunk(BlockPos pos);
}
