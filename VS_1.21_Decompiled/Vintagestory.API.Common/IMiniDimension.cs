using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IMiniDimension : IBlockAccessor
{
	int subDimensionId { get; set; }

	EntityPos CurrentPos { get; set; }

	bool Dirty { get; set; }

	bool TrackSelection { get; set; }

	BlockPos selectionTrackingOriginalPos { get; set; }

	int BlocksPreviewSubDimension_Server { get; set; }

	void CollectChunksForSending(IPlayer[] players);

	void ClearChunks();

	void UnloadUnusedServerChunks();

	FastVec3d GetRenderOffset(float dt);

	void SetRenderOffsetY(int offsetY);

	float[] GetRenderTransformMatrix(float[] currentModelViewMatrix, Vec3d playerPos);

	void ReceiveClientChunk(long chunkIndex3d, IWorldChunk chunk, IWorldAccessor world);

	void SetSubDimensionId(int dimensionId);

	void SetSelectionTrackingSubId_Server(int dimensionId);

	void AdjustPosForSubDimension(BlockPos pos);
}
