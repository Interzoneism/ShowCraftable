using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorRevertable : BlockAccessorRelaxedBulkUpdate, IBlockAccessorRevertable, IBulkBlockAccessor, IBlockAccessor
{
	private readonly List<HistoryState> _historyStates = new List<HistoryState>();

	private int _currentHistoryStateIndex;

	private int _maxQuantityStates = 35;

	private bool _multiedit;

	private List<BlockUpdate> _blockUpdates = new List<BlockUpdate>();

	public int CurrentHistoryState => _currentHistoryStateIndex;

	public bool Relight
	{
		get
		{
			return relight;
		}
		set
		{
			relight = value;
		}
	}

	public int QuantityHistoryStates
	{
		get
		{
			return _maxQuantityStates;
		}
		set
		{
			_maxQuantityStates = value;
			while (_historyStates.Count > _maxQuantityStates)
			{
				_historyStates.RemoveAt(_historyStates.Count - 1);
			}
		}
	}

	public int AvailableHistoryStates => _historyStates.Count;

	public event Action<HistoryState> OnStoreHistoryState;

	public event Action<HistoryState, int> OnRestoreHistoryState;

	public BlockAccessorRevertable(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight, debug)
	{
		storeOldBlockEntityData = true;
	}

	public void SetHistoryStateBlock(int posX, int posY, int posZ, int oldBlockId, int newBlockId)
	{
		BlockPos blockPos = new BlockPos(posX, posY, posZ);
		byte[] oldBlockEntityData = null;
		if (worldAccessor.Blocks[oldBlockId].EntityClass != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			GetBlockEntity(new BlockPos(posX, posY, posZ)).ToTreeAttributes(treeAttribute);
			oldBlockEntityData = treeAttribute.ToBytes();
		}
		ItemStack byStack = null;
		if (StagedBlocks.TryGetValue(blockPos, out var value) && value.NewSolidBlockId == newBlockId)
		{
			byStack = value.ByStack;
		}
		StagedBlocks[blockPos] = new BlockUpdate
		{
			OldBlockId = oldBlockId,
			NewSolidBlockId = newBlockId,
			NewFluidBlockId = (value?.NewFluidBlockId ?? (-1)),
			Pos = blockPos,
			ByStack = byStack,
			OldBlockEntityData = oldBlockEntityData
		};
	}

	public void BeginMultiEdit()
	{
		_multiedit = true;
		synchronize = false;
	}

	public override List<BlockUpdate> Commit()
	{
		if (_multiedit)
		{
			_blockUpdates.AddRange(base.Commit());
			return _blockUpdates;
		}
		List<BlockUpdate> list = base.Commit();
		HistoryState historyState = new HistoryState
		{
			BlockUpdates = list.ToArray()
		};
		StoreHistoryState(historyState);
		return list;
	}

	public void StoreHistoryState(HistoryState historyState)
	{
		if (_historyStates.Count >= _maxQuantityStates)
		{
			_historyStates.RemoveAt(_historyStates.Count - 1);
		}
		while (_currentHistoryStateIndex > 0)
		{
			_currentHistoryStateIndex--;
			_historyStates.RemoveAt(0);
		}
		_historyStates.Insert(0, historyState);
		this.OnStoreHistoryState?.Invoke(historyState);
	}

	public void StoreEntitySpawnToHistory(Entity entity)
	{
		HistoryState historyState2;
		HistoryState historyState = (historyState2 = _historyStates[_currentHistoryStateIndex]);
		if (historyState2.EntityUpdates == null)
		{
			historyState2.EntityUpdates = new List<EntityUpdate>();
		}
		historyState.EntityUpdates.Add(new EntityUpdate
		{
			EntityId = entity.EntityId,
			EntityProperties = entity.Properties,
			NewPosition = entity.ServerPos.Copy()
		});
	}

	public void StoreEntityMoveToHistory(BlockPos start, BlockPos end, Vec3i offset)
	{
		HistoryState historyState = _historyStates[_currentHistoryStateIndex];
		HistoryState historyState2 = historyState;
		if (historyState2.EntityUpdates == null)
		{
			historyState2.EntityUpdates = new List<EntityUpdate>();
		}
		Entity[] entitiesInsideCuboid = worldAccessor.GetEntitiesInsideCuboid(start, end, (Entity e) => !(e is EntityPlayer));
		foreach (Entity entity in entitiesInsideCuboid)
		{
			EntityPos entityPos = entity.ServerPos.Copy().Add(offset.X, offset.Y, offset.Z);
			historyState.EntityUpdates.Add(new EntityUpdate
			{
				EntityId = entity.EntityId,
				OldPosition = entity.ServerPos.Copy(),
				NewPosition = entityPos
			});
			entity.TeleportTo(entityPos);
		}
	}

	public void EndMultiEdit()
	{
		_multiedit = false;
		if (_blockUpdates.Count > 0)
		{
			HistoryState historyState = new HistoryState
			{
				BlockUpdates = _blockUpdates.ToArray()
			};
			worldmap.SendBlockUpdateBulk(_blockUpdates.Select((BlockUpdate bu) => bu.Pos), relight);
			worldmap.SendDecorUpdateBulk(from bu in _blockUpdates
				where bu.Decors != null && bu.Pos != null
				select bu.Pos);
			StoreHistoryState(historyState);
		}
		CommitBlockEntityData();
		_blockUpdates.Clear();
		synchronize = true;
	}

	public void CommitBlockEntityData()
	{
		if (_multiedit)
		{
			return;
		}
		BlockUpdate[] blockUpdates = _historyStates[0].BlockUpdates;
		foreach (BlockUpdate blockUpdate in blockUpdates)
		{
			if (blockUpdate.NewSolidBlockId >= 0 && worldAccessor.Blocks[blockUpdate.NewSolidBlockId].EntityClass != null)
			{
				TreeAttribute treeAttribute = new TreeAttribute();
				BlockEntity blockEntity = GetBlockEntity(blockUpdate.Pos);
				blockEntity?.ToTreeAttributes(treeAttribute);
				blockEntity?.MarkDirty(redrawOnClient: true);
				blockUpdate.NewBlockEntityData = treeAttribute.ToBytes();
			}
		}
	}

	public void ChangeHistoryState(int quantity = 1)
	{
		bool flag = quantity < 0;
		quantity = Math.Abs(quantity);
		while (quantity > 0)
		{
			_currentHistoryStateIndex += ((!flag) ? 1 : (-1));
			if (_currentHistoryStateIndex < 0)
			{
				_currentHistoryStateIndex = 0;
				break;
			}
			if (_currentHistoryStateIndex > AvailableHistoryStates)
			{
				_currentHistoryStateIndex = AvailableHistoryStates;
				break;
			}
			HistoryState historyState;
			if (flag)
			{
				historyState = _historyStates[_currentHistoryStateIndex];
				RedoUpdate(historyState);
			}
			else
			{
				historyState = _historyStates[_currentHistoryStateIndex - 1];
				UndoUpdate(historyState);
			}
			quantity--;
			List<BlockUpdate> updatedBlocks = base.Commit();
			if (!flag)
			{
				PostCommitCleanup(updatedBlocks);
			}
			this.OnRestoreHistoryState?.Invoke(historyState, flag ? 1 : (-1));
			for (int i = 0; i < historyState.BlockUpdates.Length; i++)
			{
				BlockUpdate blockUpdate = historyState.BlockUpdates[i];
				BlockEntity blockEntity = null;
				TreeAttribute tree = null;
				if (flag)
				{
					if (blockUpdate.NewSolidBlockId >= 0 && worldAccessor.Blocks[blockUpdate.NewSolidBlockId].EntityClass != null && blockUpdate.NewBlockEntityData != null)
					{
						tree = TreeAttribute.CreateFromBytes(blockUpdate.NewBlockEntityData);
						blockEntity = GetBlockEntity(blockUpdate.Pos);
					}
				}
				else if (blockUpdate.OldBlockId >= 0 && worldAccessor.Blocks[blockUpdate.OldBlockId].EntityClass != null)
				{
					tree = TreeAttribute.CreateFromBytes(blockUpdate.OldBlockEntityData);
					blockEntity = GetBlockEntity(blockUpdate.Pos);
				}
				blockEntity?.FromTreeAttributes(tree, worldAccessor);
				blockEntity?.HistoryStateRestore();
				blockEntity?.MarkDirty(redrawOnClient: true);
			}
		}
	}

	private void RedoUpdate(HistoryState state)
	{
		BlockUpdate[] blockUpdates = state.BlockUpdates;
		foreach (BlockUpdate blockUpdate in blockUpdates)
		{
			BlockPos blockPos = blockUpdate.Pos.Copy();
			if (StagedBlocks.TryGetValue(blockPos, out var value))
			{
				value.NewSolidBlockId = blockUpdate.NewSolidBlockId;
				value.NewFluidBlockId = blockUpdate.NewFluidBlockId;
				value.ByStack = blockUpdate.ByStack;
			}
			else
			{
				StagedBlocks[blockPos] = new BlockUpdate
				{
					NewSolidBlockId = blockUpdate.NewSolidBlockId,
					NewFluidBlockId = blockUpdate.NewFluidBlockId,
					ByStack = blockUpdate.ByStack,
					Pos = blockPos
				};
			}
			if (blockUpdate.Decors == null)
			{
				continue;
			}
			BlockUpdate blockUpdate2 = StagedBlocks[blockPos];
			List<DecorUpdate> list = blockUpdate2.Decors ?? (blockUpdate2.Decors = new List<DecorUpdate>());
			foreach (DecorUpdate decor in blockUpdate.Decors)
			{
				list.Add(decor);
			}
		}
		if (state.EntityUpdates == null)
		{
			return;
		}
		foreach (EntityUpdate item in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
		{
			worldAccessor.GetEntityById(item.EntityId)?.TeleportTo(item.NewPosition);
		}
		foreach (EntityUpdate item2 in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
		{
			Entity entityById = worldAccessor.GetEntityById(item2.EntityId);
			if (entityById != null)
			{
				entityById?.Die(EnumDespawnReason.Removed);
			}
			else if (item2.EntityProperties != null && item2.NewPosition != null)
			{
				Entity entity = worldAccessor.ClassRegistry.CreateEntity(item2.EntityProperties);
				entity.DidImportOrExport(item2.NewPosition.AsBlockPos);
				entity.ServerPos.SetFrom(item2.NewPosition);
				worldAccessor.SpawnEntity(entity);
				item2.EntityId = entity.EntityId;
			}
		}
	}

	private void UndoUpdate(HistoryState state)
	{
		BlockUpdate[] blockUpdates = state.BlockUpdates;
		for (int num = blockUpdates.Length - 1; num >= 0; num--)
		{
			BlockUpdate blockUpdate = blockUpdates[num];
			BlockPos blockPos = blockUpdate.Pos.Copy();
			if (StagedBlocks.TryGetValue(blockPos, out var value))
			{
				value.NewSolidBlockId = blockUpdate.OldBlockId;
				value.NewFluidBlockId = blockUpdate.OldFluidBlockId;
				value.ByStack = blockUpdate.ByStack;
			}
			else
			{
				StagedBlocks[blockPos] = new BlockUpdate
				{
					NewSolidBlockId = blockUpdate.OldBlockId,
					NewFluidBlockId = blockUpdate.OldFluidBlockId,
					ByStack = blockUpdate.ByStack,
					Pos = blockPos
				};
			}
			if (blockUpdate.OldDecors != null)
			{
				BlockUpdate blockUpdate2 = StagedBlocks[blockPos];
				List<DecorUpdate> list = blockUpdate2.Decors ?? (blockUpdate2.Decors = new List<DecorUpdate>());
				foreach (DecorUpdate oldDecor in blockUpdate.OldDecors)
				{
					list.Add(oldDecor);
				}
			}
		}
		if (state.EntityUpdates == null)
		{
			return;
		}
		foreach (EntityUpdate item in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
		{
			worldAccessor.GetEntityById(item.EntityId)?.TeleportTo(item.OldPosition);
		}
		foreach (EntityUpdate item2 in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
		{
			Entity entityById = worldAccessor.GetEntityById(item2.EntityId);
			if (entityById != null)
			{
				entityById?.Die(EnumDespawnReason.Removed);
			}
			else if (item2.EntityProperties != null && item2.NewPosition != null)
			{
				Entity entity = worldAccessor.ClassRegistry.CreateEntity(item2.EntityProperties);
				entity.DidImportOrExport(item2.NewPosition.AsBlockPos);
				entity.ServerPos.SetFrom(item2.NewPosition);
				worldAccessor.SpawnEntity(entity);
				item2.EntityId = entity.EntityId;
			}
		}
	}
}
