using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common;

public class BlockAccessorMovable : BlockAccessorBase, IMiniDimension, IBlockAccessor
{
	protected double totalMass;

	private BlockAccessorBase parent;

	private Dictionary<long, IWorldChunk> chunks = new Dictionary<long, IWorldChunk>();

	private FastSetOfLongs dirtychunks = new FastSetOfLongs();

	public const int subDimensionSize = 16384;

	public const int subDimensionIndexZMultiplier = 4096;

	public const int originOffset = 8192;

	public const int MaxMiniDimensions = 16777216;

	public const int subDimensionSizeInChunks = 512;

	public const int dimensionSizeY = 32768;

	public const int dimensionId = 1;

	public const int DefaultLightLevel = 18;

	public EntityPos CurrentPos { get; set; }

	public bool Dirty { get; set; }

	public Vec3d CenterOfMass { get; set; }

	public bool TrackSelection { get; set; }

	public int BlocksPreviewSubDimension_Server { get; set; }

	public BlockPos selectionTrackingOriginalPos { get; set; }

	public int subDimensionId { get; set; }

	public BlockAccessorMovable(BlockAccessorBase parent, Vec3d pos)
		: base(parent.worldmap, parent.worldAccessor)
	{
		CurrentPos = new EntityPos(pos.X, pos.Y, pos.Z);
		this.parent = parent;
		BlocksPreviewSubDimension_Server = -1;
	}

	public virtual void SetSubDimensionId(int subId)
	{
		subDimensionId = subId;
	}

	public void SetSelectionTrackingSubId_Server(int subId)
	{
		BlocksPreviewSubDimension_Server = subId;
	}

	public virtual void ClearChunks()
	{
		if (parent.worldAccessor is IServerWorldAccessor serverWorldAccessor)
		{
			foreach (KeyValuePair<long, IWorldChunk> chunk in chunks)
			{
				((ServerChunk)chunk.Value).ClearAll(serverWorldAccessor);
			}
		}
		else
		{
			ChunkRenderer chunkRenderer = ((ClientMain)parent.worldAccessor).chunkRenderer;
			foreach (KeyValuePair<long, IWorldChunk> chunk2 in chunks)
			{
				((ClientChunk)chunk2.Value).RemoveDataPoolLocations(chunkRenderer);
			}
		}
		dirtychunks.Clear();
		if (CenterOfMass == null)
		{
			CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
		}
		else
		{
			CenterOfMass.Set(0.0, 0.0, 0.0);
		}
		totalMass = 0.0;
	}

	public virtual void UnloadUnusedServerChunks()
	{
		List<long> list = new List<long>();
		foreach (KeyValuePair<long, IWorldChunk> chunk in chunks)
		{
			if (chunk.Value.Empty)
			{
				list.Add(chunk.Key);
				ChunkPos ret = parent.worldmap.ChunkPosFromChunkIndex3D(chunk.Key);
				ServerSystemUnloadChunks.TryUnloadChunk(chunk.Key, ret, (ServerChunk)chunk.Value, new List<ServerChunkWithCoord>(), (ServerMain)parent.worldAccessor);
			}
		}
		foreach (long item in list)
		{
			chunks.Remove(item);
		}
	}

	public static bool ChunkCoordsInSameDimension(int cyA, int cyB)
	{
		return cyA / 1024 == cyB / 1024;
	}

	protected virtual IWorldChunk GetChunkAt(int posX, int posY, int posZ)
	{
		chunks.TryGetValue(ChunkIndex(posX, posY, posZ), out var value);
		return value;
	}

	protected virtual long ChunkIndex(int posX, int posY, int posZ)
	{
		int num = posX / 32 % 512 + subDimensionId % 4096 * 512;
		int num2 = posY / 32 + 1024;
		int num3 = posZ / 32 % 512 + subDimensionId / 4096 * 512;
		return ((long)num2 * (long)worldmap.index3dMulZ + num3) * worldmap.index3dMulX + num;
	}

	public virtual void AdjustPosForSubDimension(BlockPos pos)
	{
		pos.X += subDimensionId % 4096 * 16384 + 8192;
		pos.Y += 8192;
		pos.Z += subDimensionId / 4096 * 16384 + 8192;
	}

	protected virtual IWorldChunk CreateChunkAt(int posX, int posY, int posZ)
	{
		long key = ChunkIndex(posX, posY, posZ);
		IWorldChunk worldChunk;
		if (worldAccessor.Side == EnumAppSide.Server)
		{
			ServerMain serverMain = (ServerMain)worldAccessor;
			worldChunk = ServerChunk.CreateNew(serverMain.serverChunkDataPool);
			worldChunk.Lighting.FloodWithSunlight(18);
			serverMain.loadedChunksLock.AcquireWriteLock();
			try
			{
				if (serverMain.loadedChunks.TryGetValue(key, out var value))
				{
					value.Dispose();
				}
				serverMain.loadedChunks[key] = (ServerChunk)worldChunk;
			}
			finally
			{
				serverMain.loadedChunksLock.ReleaseWriteLock();
			}
		}
		else
		{
			worldChunk = ClientChunk.CreateNew(((ClientWorldMap)worldmap).chunkDataPool);
		}
		chunks[key] = worldChunk;
		return worldChunk;
	}

	public virtual void MarkChunkDirty(int x, int y, int z)
	{
		dirtychunks.Add(ChunkIndex(x, y, z));
		Dirty = true;
	}

	public virtual void CollectChunksForSending(IPlayer[] players)
	{
		foreach (long dirtychunk in dirtychunks)
		{
			if (chunks.TryGetValue(dirtychunk, out var value))
			{
				((ServerChunk)value).MarkToPack();
				foreach (IPlayer player in players)
				{
					MarkChunkForSendingToPlayersInRange(value, dirtychunk, player);
				}
			}
		}
		dirtychunks.Clear();
	}

	public virtual void MarkChunkForSendingToPlayersInRange(IWorldChunk chunk, long index, IPlayer player)
	{
		ServerPlayer serverPlayer = player as ServerPlayer;
		if (serverPlayer?.Entity != null && serverPlayer?.client != null)
		{
			ConnectedClient client = serverPlayer.client;
			float num = client.WorldData.Viewdistance + 16;
			if (client.Entityplayer.ServerPos.InHorizontalRangeOf((int)CurrentPos.X, (int)CurrentPos.Z, num * num) || subDimensionId == BlocksPreviewSubDimension_Server)
			{
				client.forceSendChunks.Add(index);
			}
		}
	}

	protected virtual int Index3d(int posX, int posY, int posZ)
	{
		return worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F);
	}

	protected virtual bool SetBlock(int blockId, BlockPos pos, int layer, ItemStack byItemstack)
	{
		pos.SetDimension(1);
		IWorldChunk worldChunk = GetChunkAt(pos.X, pos.Y, pos.Z);
		if (worldChunk == null)
		{
			if (blockId == 0)
			{
				return false;
			}
			worldChunk = CreateChunkAt(pos.X, pos.Y, pos.Z);
		}
		else
		{
			worldChunk.Unpack();
			if (worldChunk.Empty)
			{
				worldChunk.Lighting.FloodWithSunlight(18);
			}
		}
		Block block = worldmap.Blocks[blockId];
		if (layer == 2 || (layer == 0 && block.ForFluidsLayer))
		{
			if (layer == 0)
			{
				SetSolidBlock(0, pos, worldChunk, byItemstack);
			}
			SetFluidBlock(blockId, pos, worldChunk);
			return true;
		}
		if (layer != 0 && layer != 1)
		{
			throw new ArgumentException("Layer must be solid or fluid");
		}
		return SetSolidBlock(blockId, pos, worldChunk, byItemstack);
	}

	protected virtual bool SetSolidBlock(int blockId, BlockPos pos, IWorldChunk chunk, ItemStack byItemstack)
	{
		int index3d = Index3d(pos.X, pos.Y, pos.Z);
		int solidBlock = (chunk.Data as ChunkData).GetSolidBlock(index3d);
		if (blockId == solidBlock)
		{
			return false;
		}
		Block block = worldmap.Blocks[blockId];
		Block block2 = worldmap.Blocks[solidBlock];
		if (solidBlock > 0)
		{
			AddToCenterOfMass(block2, pos, -1);
		}
		if (blockId > 0)
		{
			AddToCenterOfMass(block, pos, 1);
		}
		chunk.Data[index3d] = blockId;
		if (blockId != 0)
		{
			chunk.Empty = false;
		}
		chunk.BreakAllDecorFast(worldAccessor, pos, index3d);
		block2.OnBlockRemoved(worldmap.World, pos);
		block.OnBlockPlaced(worldmap.World, pos, byItemstack);
		if (block.DisplacesLiquids(this, pos))
		{
			chunk.Data.SetFluid(index3d, 0);
		}
		else
		{
			int fluid = chunk.Data.GetFluid(index3d);
			if (fluid != 0)
			{
				worldAccessor.GetBlock(fluid);
			}
		}
		return true;
	}

	protected virtual bool SetFluidBlock(int fluidBlockid, BlockPos pos, IWorldChunk chunk)
	{
		int index3d = Index3d(pos.X, pos.Y, pos.Z);
		int fluid = chunk.Data.GetFluid(index3d);
		if (fluidBlockid == fluid)
		{
			return false;
		}
		chunk.Data.SetFluid(index3d, fluidBlockid);
		if (fluidBlockid != 0)
		{
			chunk.Empty = false;
		}
		return true;
	}

	protected virtual void AddToCenterOfMass(Block block, BlockPos pos, int sign)
	{
		double num = (double)Math.Max(block.MaterialDensity, 1) / 1000.0;
		double num2 = (double)pos.X + 0.5 - 8192.0;
		double num3 = (double)pos.Y + 0.5 - 8192.0;
		double num4 = (double)pos.Z + 0.5 - 8192.0;
		if (CenterOfMass == null)
		{
			CenterOfMass = new Vec3d(num2, num3, num4);
		}
		else
		{
			CenterOfMass.X = (CenterOfMass.X * totalMass + num2 * num * (double)sign) / (totalMass + num * (double)sign);
			CenterOfMass.Y = (CenterOfMass.Y * totalMass + num3 * num * (double)sign) / (totalMass + num * (double)sign);
			CenterOfMass.Z = (CenterOfMass.Z * totalMass + num4 * num * (double)sign) / (totalMass + num * (double)sign);
		}
		totalMass += num * (double)sign;
	}

	public virtual FastVec3d GetRenderOffset(float dt)
	{
		FastVec3d fastVec3d = new FastVec3d(-(subDimensionId % 4096) * 16384, 0.0, -(subDimensionId / 4096) * 16384).Add(-8192.0);
		if (TrackSelection)
		{
			BlockSelection blockSelection = ((ClientMain)parent.worldAccessor).BlockSelection;
			if (blockSelection != null && blockSelection.Position != null)
			{
				return fastVec3d.Add(blockSelection.Position).Add(blockSelection.Face.Normali);
			}
		}
		return fastVec3d.Add(selectionTrackingOriginalPos.X, selectionTrackingOriginalPos.InternalY, selectionTrackingOriginalPos.Z);
	}

	public virtual void SetRenderOffsetY(int offset)
	{
		selectionTrackingOriginalPos.Y = offset;
	}

	public virtual float[] GetRenderTransformMatrix(float[] currentModelViewMatrix, Vec3d playerPos)
	{
		if (CurrentPos.Yaw == 0f && CurrentPos.Pitch == 0f && CurrentPos.Roll == 0f)
		{
			return currentModelViewMatrix;
		}
		float[] array = new float[currentModelViewMatrix.Length];
		float num = (float)(CurrentPos.X + CenterOfMass.X - playerPos.X);
		float num2 = (float)(CurrentPos.Y + CenterOfMass.Y - playerPos.Y);
		float num3 = (float)(CurrentPos.Z + CenterOfMass.Z - playerPos.Z);
		Mat4f.Translate(array, currentModelViewMatrix, num, num2, num3);
		ApplyCurrentRotation(array);
		return Mat4f.Translate(array, array, 0f - num, 0f - num2, 0f - num3);
	}

	public virtual void ApplyCurrentRotation(float[] result)
	{
		Mat4f.RotateY(result, result, CurrentPos.Yaw);
		Mat4f.RotateZ(result, result, CurrentPos.Pitch);
		Mat4f.RotateX(result, result, CurrentPos.Roll);
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0)
		{
			return 0;
		}
		if (posY >= 32768)
		{
			posX %= 16384;
			posY %= 32768;
			posZ %= 16384;
		}
		return GetChunkAt(posX, posY, posZ)?.UnpackAndReadBlock(Index3d(posX, posY, posZ), layer) ?? 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0)
		{
			return null;
		}
		if (posY >= 32768)
		{
			posX %= 16384;
			posY %= 32768;
			posZ %= 16384;
		}
		IWorldChunk chunkAt = GetChunkAt(posX, posY, posZ);
		if (chunkAt == null)
		{
			return null;
		}
		return worldmap.Blocks[chunkAt.UnpackAndReadBlock(Index3d(posX, posY, posZ), layer)];
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack)
	{
		if (SetBlock(blockId, pos, 0, byItemstack))
		{
			MarkChunkDirty(pos.X, pos.Y, pos.Z);
		}
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		if (SetBlock(blockId, pos, 0, null))
		{
			MarkChunkDirty(pos.X, pos.Y, pos.Z);
		}
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
	}

	public virtual void ReceiveClientChunk(long cindex, IWorldChunk chunk, IWorldAccessor world)
	{
		chunks[cindex] = chunk;
		RecalculateCenterOfMass(world);
	}

	public virtual void RecalculateCenterOfMass(IWorldAccessor world)
	{
		CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
		totalMass = 0.0;
		BlockPos blockPos = new BlockPos();
		foreach (KeyValuePair<long, IWorldChunk> chunk in chunks)
		{
			ChunkPos chunkPos = worldmap.ChunkPosFromChunkIndex3D(chunk.Key);
			int num = chunkPos.X * 32 % 16384;
			int num2 = chunkPos.Y * 32 % 16384;
			int num3 = chunkPos.Z * 32 % 16384;
			IWorldChunk value = chunk.Value;
			value.Unpack_ReadOnly();
			IChunkBlocks data = value.Data;
			for (int i = 0; i < 32768; i++)
			{
				int blockId = data.GetBlockId(i, 1);
				if (blockId > 0)
				{
					blockPos.X = num + i % 32;
					blockPos.Y = num2 + i / 1024;
					blockPos.Z = num3 + i / 32 % 32;
					AddToCenterOfMass(world.GetBlock(blockId), blockPos, 1);
				}
			}
		}
	}

	internal static int CalcSubDimensionId(int cx, int cz)
	{
		return cx / 512 + cz / 512 * 4096;
	}

	internal static int CalcSubDimensionId(Vec3i vec)
	{
		return CalcSubDimensionId(vec.X / 32, vec.Z / 32);
	}

	internal static bool IsTransparent(Vec3i chunkOrigin)
	{
		return CalcSubDimensionId(chunkOrigin) == Dimensions.BlocksPreviewSubDimension_Client;
	}
}
