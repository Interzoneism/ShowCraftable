using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBlockAccessorRevertable : IBulkBlockAccessor, IBlockAccessor
{
	bool Relight { get; set; }

	int CurrentHistoryState { get; }

	int QuantityHistoryStates { get; set; }

	int AvailableHistoryStates { get; }

	event Action<HistoryState> OnStoreHistoryState;

	event Action<HistoryState, int> OnRestoreHistoryState;

	void ChangeHistoryState(int quantity = 1);

	void SetHistoryStateBlock(int posX, int posY, int posZ, int oldBlockId, int newBlockId);

	void CommitBlockEntityData();

	void BeginMultiEdit();

	void EndMultiEdit();

	void StoreHistoryState(HistoryState state);

	void StoreEntitySpawnToHistory(Entity entity);

	void StoreEntityMoveToHistory(BlockPos start, BlockPos end, Vec3i offset);
}
