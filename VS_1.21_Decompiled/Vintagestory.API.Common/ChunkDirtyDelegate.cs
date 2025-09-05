using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public delegate void ChunkDirtyDelegate(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason);
