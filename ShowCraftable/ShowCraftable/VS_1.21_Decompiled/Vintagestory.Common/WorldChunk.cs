using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public abstract class WorldChunk : IWorldChunk
{
	public bool WasModified;

	protected ChunkDataPool datapool;

	protected ChunkData chunkdata;

	protected int chunkdataVersion;

	public long lastReadOrWrite;

	protected bool PotentialBlockOrLightingChanges;

	public byte[] blocksCompressedTmp;

	public byte[] lightCompressedTmp;

	public byte[] lightSatCompressedTmp;

	public byte[] fluidsCompressedTmp;

	public Entity[] Entities;

	public Dictionary<BlockPos, BlockEntity> BlockEntities = new Dictionary<BlockPos, BlockEntity>();

	public Dictionary<int, Block> Decors;

	internal object packUnpackLock = new object();

	private int _disposed;

	public byte[] blocksCompressed { get; set; }

	public byte[] lightCompressed { get; set; }

	public byte[] lightSatCompressed { get; set; }

	public byte[] fluidsCompressed { get; set; }

	public int EntitiesCount { get; set; }

	[Obsolete("Use Data field")]
	public IChunkBlocks Blocks => chunkdata;

	public IChunkBlocks Data => chunkdata;

	public IChunkLight Lighting => chunkdata;

	public IChunkBlocks MaybeBlocks { get; set; }

	public bool Empty { get; set; }

	public abstract IMapChunk MapChunk { get; }

	Entity[] IWorldChunk.Entities => Entities;

	Dictionary<BlockPos, BlockEntity> IWorldChunk.BlockEntities
	{
		get
		{
			return BlockEntities;
		}
		set
		{
			BlockEntities = value;
		}
	}

	public abstract HashSet<int> LightPositions { get; set; }

	public abstract Dictionary<string, byte[]> ModData { get; set; }

	public bool Disposed
	{
		get
		{
			return _disposed != 0;
		}
		set
		{
			_disposed = (value ? 1 : 0);
		}
	}

	public Dictionary<string, object> LiveModData { get; set; }

	public virtual void MarkModified()
	{
		WasModified = true;
		lastReadOrWrite = Environment.TickCount;
	}

	public virtual bool IsPacked()
	{
		return chunkdata == null;
	}

	public virtual void TryPackAndCommit(int chunkTTL = 8000)
	{
		if (Environment.TickCount - lastReadOrWrite >= chunkTTL)
		{
			Pack();
			TryCommitPackAndFree(chunkTTL);
		}
	}

	public virtual void Pack()
	{
		if (Disposed)
		{
			return;
		}
		lock (packUnpackLock)
		{
			if (chunkdata != null)
			{
				if (PotentialBlockOrLightingChanges)
				{
					chunkdata.CompressInto(ref blocksCompressedTmp, ref lightCompressedTmp, ref lightSatCompressedTmp, ref fluidsCompressedTmp, 2);
					return;
				}
				blocksCompressedTmp = blocksCompressed;
				lightCompressedTmp = lightCompressed;
				lightSatCompressedTmp = lightSatCompressed;
				fluidsCompressedTmp = fluidsCompressed;
			}
		}
	}

	public virtual bool TryCommitPackAndFree(int chunkTTL = 8000)
	{
		if (Disposed)
		{
			return false;
		}
		lock (packUnpackLock)
		{
			if (blocksCompressedTmp == null)
			{
				return false;
			}
			if (Environment.TickCount - lastReadOrWrite < chunkTTL)
			{
				blocksCompressedTmp = null;
				lightCompressedTmp = null;
				lightSatCompressedTmp = null;
				fluidsCompressedTmp = null;
				return false;
			}
			blocksCompressed = blocksCompressedTmp;
			blocksCompressedTmp = null;
			lightCompressed = lightCompressedTmp;
			lightCompressedTmp = null;
			lightSatCompressed = lightSatCompressedTmp;
			lightSatCompressedTmp = null;
			fluidsCompressed = fluidsCompressedTmp;
			fluidsCompressedTmp = null;
			if (chunkdata != null && blocksCompressed != null)
			{
				if (WasModified)
				{
					UpdateEmptyFlag();
				}
				datapool.Free(chunkdata);
				MaybeBlocks = datapool.OnlyAirBlocksData;
				chunkdata = null;
			}
			chunkdataVersion = 2;
			WasModified = false;
			PotentialBlockOrLightingChanges = false;
		}
		return true;
	}

	public virtual void Unpack()
	{
		if (Disposed)
		{
			return;
		}
		lock (packUnpackLock)
		{
			bool num = chunkdata == null;
			unpackNoLock();
			if (num)
			{
				blocksCompressed = null;
				lightCompressed = null;
				lightSatCompressed = null;
				fluidsCompressed = null;
			}
			PotentialBlockOrLightingChanges = true;
		}
	}

	protected virtual void UpdateForVersion()
	{
		PotentialBlockOrLightingChanges = true;
	}

	public virtual bool Unpack_ReadOnly()
	{
		if (Disposed)
		{
			return false;
		}
		lock (packUnpackLock)
		{
			bool result = chunkdata == null;
			unpackNoLock();
			return result;
		}
	}

	public virtual int UnpackAndReadBlock(int index, int layer)
	{
		if (Disposed)
		{
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.GetBlockId(index, layer);
		}
	}

	public virtual ushort Unpack_AndReadLight(int index)
	{
		if (Disposed)
		{
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.ReadLight(index);
		}
	}

	public virtual ushort Unpack_AndReadLight(int index, out int lightSat)
	{
		if (Disposed)
		{
			lightSat = 0;
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.ReadLight(index, out lightSat);
		}
	}

	public virtual void Unpack_MaybeNullData()
	{
		lock (packUnpackLock)
		{
			lastReadOrWrite = Environment.TickCount;
			bool num = chunkdata == null;
			unpackNoLock();
			if (num)
			{
				blocksCompressed = null;
				lightCompressed = null;
				lightSatCompressed = null;
				fluidsCompressed = null;
			}
		}
	}

	private void unpackNoLock()
	{
		lastReadOrWrite = Environment.TickCount;
		if (chunkdata == null)
		{
			chunkdata = datapool.Request();
			chunkdata.DecompressFrom(blocksCompressed, lightCompressed, lightSatCompressed, fluidsCompressed, chunkdataVersion);
			MaybeBlocks = chunkdata;
			if (chunkdataVersion < 2)
			{
				UpdateForVersion();
			}
		}
	}

	public void AcquireBlockReadLock()
	{
		Unpack_ReadOnly();
		Data.TakeBulkReadLock();
	}

	public void ReleaseBlockReadLock()
	{
		Data.ReleaseBulkReadLock();
	}

	public virtual void UpdateEmptyFlag()
	{
		Empty = chunkdata.IsEmpty();
	}

	public virtual void MarkFresh()
	{
		lastReadOrWrite = Environment.TickCount;
	}

	internal virtual void AddBlockEntity(BlockEntity blockEntity)
	{
		lock (packUnpackLock)
		{
			BlockEntities[blockEntity.Pos] = blockEntity;
		}
	}

	public virtual bool RemoveBlockEntity(BlockPos pos)
	{
		lock (packUnpackLock)
		{
			return BlockEntities.Remove(pos);
		}
	}

	public virtual void AddEntity(Entity entity)
	{
		lock (packUnpackLock)
		{
			Entity[] array = Entities;
			if (array == null)
			{
				array = (Entities = new Entity[32]);
				EntitiesCount = 0;
			}
			else
			{
				for (int i = 0; i < array.Length; i++)
				{
					Entity entity2 = array[i];
					if (entity2 == null)
					{
						if (i >= EntitiesCount)
						{
							break;
						}
					}
					else if (entity2.EntityId == entity.EntityId)
					{
						array[i] = entity;
						return;
					}
				}
				if (EntitiesCount >= array.Length)
				{
					Array.Resize(ref Entities, EntitiesCount + 32);
					array = Entities;
				}
			}
			array[EntitiesCount++] = entity;
		}
	}

	public virtual bool RemoveEntity(long entityId)
	{
		lock (packUnpackLock)
		{
			Entity[] entities;
			if ((entities = Entities) == null)
			{
				return false;
			}
			int entitiesCount = EntitiesCount;
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				if (entity == null)
				{
					if (i >= entitiesCount)
					{
						break;
					}
				}
				else if (entity.EntityId == entityId)
				{
					for (int j = i + 1; j < entities.Length && j < entitiesCount; j++)
					{
						entities[j - 1] = entities[j];
					}
					entities[entitiesCount - 1] = null;
					EntitiesCount--;
					return true;
				}
			}
		}
		return false;
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
		MarkModified();
	}

	public void RemoveModdata(string key)
	{
		ModData.Remove(key);
		MarkModified();
	}

	public byte[] GetModdata(string key)
	{
		ModData.TryGetValue(key, out var value);
		return value;
	}

	public void SetModdata<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModdata<T>(string key, T defaultValue = default(T))
	{
		byte[] moddata = GetModdata(key);
		if (moddata == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(moddata);
	}

	public void Dispose()
	{
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
		{
			return;
		}
		lock (packUnpackLock)
		{
			ChunkData chunkData = chunkdata;
			chunkdata = datapool.BlackHoleData;
			MaybeBlocks = datapool.OnlyAirBlocksData;
			Empty = true;
			if (chunkData != null)
			{
				datapool.Free(chunkData);
			}
		}
	}

	public Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position)
	{
		return GetLocalBlockAtBlockPos(world, position.X, position.Y, position.Z);
	}

	public Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer = 0)
	{
		int num = posX % 32;
		int num2 = posY % 32;
		int num3 = posZ % 32;
		return world.Blocks[UnpackAndReadBlock((num2 * 32 + num3) * 32 + num, layer)];
	}

	public Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos pos, int layer = 0)
	{
		int num = pos.X % 32;
		int num2 = pos.Y % 32;
		int num3 = pos.Z % 32;
		int blockIdUnsafe = chunkdata.GetBlockIdUnsafe((num2 * 32 + num3) * 32 + num, layer);
		return world.Blocks[blockIdUnsafe];
	}

	public BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos position)
	{
		BlockEntities.TryGetValue(position, out var value);
		return value;
	}

	public virtual void FinishLightDoubleBuffering()
	{
	}

	public int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes)
	{
		int solidBlock = chunkdata.GetSolidBlock(index3d);
		int fluid = chunkdata.GetFluid(index3d);
		if (solidBlock == 0)
		{
			return blockTypes[fluid].LightAbsorption;
		}
		int lightAbsorption = blockTypes[solidBlock].GetLightAbsorption(this, blockPos);
		if (fluid == 0)
		{
			return lightAbsorption;
		}
		int lightAbsorption2 = blockTypes[fluid].LightAbsorption;
		return Math.Max(lightAbsorption, lightAbsorption2);
	}

	public bool SetDecor(Block block, int index3d, BlockFacing onFace)
	{
		if (block == null)
		{
			return false;
		}
		index3d += DecorBits.FaceToIndex(onFace);
		SetDecorInternal(index3d, block);
		return true;
	}

	public bool SetDecor(Block block, int index3d, int faceAndSubposition)
	{
		if (block == null)
		{
			return false;
		}
		int packedIndex = index3d + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		SetDecorInternal(packedIndex, block);
		return true;
	}

	private void SetDecorInternal(int packedIndex, Block block)
	{
		if (Decors == null)
		{
			Decors = new Dictionary<int, Block>();
		}
		lock (Decors)
		{
			if (block.Id == 0)
			{
				Decors.Remove(packedIndex);
			}
			else
			{
				Decors[packedIndex] = block;
			}
		}
	}

	public Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int num = ToIndex3d(position);
		Dictionary<int, Block> dictionary = new Dictionary<int, Block>();
		foreach (KeyValuePair<int, Block> decor in Decors)
		{
			int key = decor.Key;
			if (DecorBits.Index3dFromIndex(key) == num)
			{
				dictionary[DecorBits.FaceAndSubpositionFromIndex(key)] = decor.Value;
			}
		}
		return dictionary;
	}

	public Block[] GetDecors(IBlockAccessor blockAccessor, BlockPos position)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int num = ToIndex3d(position);
		Block[] array = new Block[6];
		foreach (KeyValuePair<int, Block> decor in Decors)
		{
			int key = decor.Key;
			if (DecorBits.Index3dFromIndex(key) == num)
			{
				array[DecorBits.FaceFromIndex(key)] = decor.Value;
			}
		}
		return array;
	}

	public Block GetDecor(IBlockAccessor blockAccessor, BlockPos position, int faceAndSubposition)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int index = ToIndex3d(position) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		return TryGetDecor(ref index, BlockFacing.NORTH);
	}

	public bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return false;
		}
		int num = ToIndex3d(pos);
		if (side == null && !faceAndSubposition.HasValue)
		{
			List<int> list = new List<int>();
			foreach (KeyValuePair<int, Block> decor in Decors)
			{
				int key = decor.Key;
				if (DecorBits.Index3dFromIndex(key) == num)
				{
					Block value = decor.Value;
					list.Add(key);
					value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(key));
				}
			}
			lock (Decors)
			{
				foreach (int item in list)
				{
					Decors.Remove(item);
				}
			}
			return true;
		}
		num += (faceAndSubposition.HasValue ? DecorBits.FaceAndSubpositionToIndex(faceAndSubposition.Value) : DecorBits.FaceToIndex(side));
		Block block = TryGetDecor(ref num, BlockFacing.NORTH);
		if (block == null)
		{
			return false;
		}
		block.OnBrokenAsDecor(world, pos, side);
		lock (Decors)
		{
			Decors.Remove(num);
		}
		return true;
	}

	public bool BreakDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
	{
		return setDecorPart(world, pos, side, faceAndSubposition, callBlockBroken: true);
	}

	public bool RemoveDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
	{
		return setDecorPart(world, pos, side, faceAndSubposition, callBlockBroken: false);
	}

	private bool setDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition, bool callBlockBroken)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return false;
		}
		int index = ToIndex3d(pos) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		Block block = TryGetDecor(ref index, BlockFacing.NORTH);
		if (block == null)
		{
			return false;
		}
		if (callBlockBroken)
		{
			block.OnBrokenAsDecor(world, pos, side);
		}
		lock (Decors)
		{
			Decors.Remove(index);
		}
		return true;
	}

	public void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true)
	{
		if (Decors == null)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Block> decor in Decors)
		{
			int key = decor.Key;
			if (DecorBits.Index3dFromIndex(key) == index3d)
			{
				list.Add(key);
				if (callOnBrokenAsDecor)
				{
					decor.Value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(key));
				}
			}
		}
		lock (Decors)
		{
			foreach (int item in list)
			{
				Decors.Remove(item);
			}
		}
	}

	public Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos position, Cuboidf[] orig)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return orig;
		}
		Cuboidf cuboidf = orig[0];
		int num = ToIndex3d(position);
		bool flag = false;
		foreach (KeyValuePair<int, Block> decor in Decors)
		{
			int key = decor.Key;
			if (DecorBits.Index3dFromIndex(key) != num)
			{
				continue;
			}
			float decorThickness = decor.Value.DecorThickness;
			if (decorThickness > 0f)
			{
				if (!flag)
				{
					flag = true;
					cuboidf = cuboidf.Clone();
				}
				cuboidf.Expand(DecorBits.FacingFromIndex(key), decorThickness);
			}
		}
		if (!flag)
		{
			return orig;
		}
		return new Cuboidf[1] { cuboidf };
	}

	public List<Cuboidf> GetDecorSelectionBoxes(IBlockAccessor blockAccessor, BlockPos position)
	{
		int num = 31;
		int num2 = position.X % 32;
		int num3 = position.Y % 32;
		int num4 = position.Z % 32;
		List<Cuboidf> result = new List<Cuboidf>();
		int num5 = (num3 * 32 + num4) * 32 + num2;
		if (num4 == 0)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z - 1) / 32))?.AddDecorSelectionBox(result, num5 + num * 32, BlockFacing.NORTH);
		}
		else
		{
			AddDecorSelectionBox(result, num5 - 32, BlockFacing.NORTH);
		}
		if (num4 == num)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z + 1) / 32))?.AddDecorSelectionBox(result, num5 - num * 32, BlockFacing.SOUTH);
		}
		else
		{
			AddDecorSelectionBox(result, num5 + 32, BlockFacing.SOUTH);
		}
		if (num2 == 0)
		{
			((WorldChunk)blockAccessor.GetChunk((position.X - 1) / 32, position.InternalY / 32, position.Z / 32))?.AddDecorSelectionBox(result, num5 + num, BlockFacing.WEST);
		}
		else
		{
			AddDecorSelectionBox(result, num5 - 1, BlockFacing.WEST);
		}
		if (num2 == num)
		{
			((WorldChunk)blockAccessor.GetChunk((position.X + 1) / 32, position.InternalY / 32, position.Z / 32))?.AddDecorSelectionBox(result, num5 - num, BlockFacing.EAST);
		}
		else
		{
			AddDecorSelectionBox(result, num5 + 1, BlockFacing.EAST);
		}
		if (num3 == 0)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY - 1) / 32, position.Z / 32))?.AddDecorSelectionBox(result, num5 + num * 32 * 32, BlockFacing.DOWN);
		}
		else
		{
			AddDecorSelectionBox(result, num5 - 1024, BlockFacing.DOWN);
		}
		if (num3 == num)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY + 1) / 32, position.Z / 32))?.AddDecorSelectionBox(result, num4 * 32 + num2, BlockFacing.UP);
		}
		else
		{
			AddDecorSelectionBox(result, num5 + 1024, BlockFacing.UP);
		}
		return result;
	}

	private void AddDecorSelectionBox(List<Cuboidf> result, int index, BlockFacing face)
	{
		if (Decors == null)
		{
			return;
		}
		Block block = TryGetDecor(ref index, face.Opposite);
		if (block != null)
		{
			float decorThickness = block.DecorThickness;
			if (decorThickness != 0f)
			{
				DecorSelectionBox decorSelectionBox = face.Index switch
				{
					0 => new DecorSelectionBox(0f, 0f, 0f, 1f, 1f, decorThickness), 
					1 => new DecorSelectionBox(1f - decorThickness, 0f, 0f, 1f, 1f, 1f), 
					2 => new DecorSelectionBox(0f, 0f, 1f - decorThickness, 1f, 1f, 1f), 
					3 => new DecorSelectionBox(0f, 0f, 0f, decorThickness, 1f, 1f), 
					4 => new DecorSelectionBox(0f, 1f - decorThickness, 0f, 1f, 1f, 1f), 
					5 => new DecorSelectionBox(0f, 0f, 0f, 1f, decorThickness, 1f), 
					_ => null, 
				};
				decorSelectionBox.PosAdjust = face.Normali;
				result.Add(decorSelectionBox);
			}
		}
	}

	public Block TryGetDecor(ref int index, BlockFacing face)
	{
		int num = (index & -458753) + DecorBits.FaceToIndex(face);
		for (int i = 0; i <= 7; i++)
		{
			if (Decors.TryGetValue(num + (i << 16), out var value) && value != null)
			{
				index = num + (i << 16);
				return value;
			}
		}
		return null;
	}

	public void SetDecors(Dictionary<int, Block> newDecors)
	{
		Decors = newDecors;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ToIndex3d(BlockPos pos)
	{
		int num = pos.X % 32;
		int num2 = pos.Y % 32;
		int num3 = pos.Z % 32;
		return (num2 * 32 + num3) * 32 + num;
	}
}
