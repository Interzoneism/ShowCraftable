using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerChunk : WorldChunk, IServerChunk, IWorldChunk, IWithFastSerialize
{
	public static Stopwatch ReadWriteStopWatch;

	[ThreadStatic]
	private static FastMemoryStream reusableSerializationStream;

	[ThreadStatic]
	private static List<Entity> reusableSerializationList;

	[ProtoMember(6)]
	[CustomFastSerializer]
	private List<byte[]> EntitiesSerialized;

	[ProtoMember(7)]
	[CustomFastSerializer]
	public int BlockEntitiesCount;

	[ProtoMember(8)]
	[CustomFastSerializer]
	private List<byte[]> BlockEntitiesSerialized;

	[ProtoMember(9)]
	private Dictionary<string, byte[]> moddata;

	[ProtoMember(10)]
	protected HashSet<int> lightPositions;

	[ProtoMember(11)]
	public Dictionary<string, byte[]> ServerSideModdata;

	[ProtoMember(12)]
	public string GameVersionCreated;

	[ProtoMember(13)]
	public bool EmptyBeforeSave;

	[ProtoMember(14)]
	[CustomFastSerializer]
	public byte[] DecorsSerialized;

	[ProtoMember(15)]
	public int savedCompressionVersion;

	[ProtoMember(17)]
	public int BlocksPlaced;

	[ProtoMember(18)]
	public int BlocksRemoved;

	public ServerMapChunk serverMapChunk;

	public bool DirtyForSaving;

	[ProtoMember(1)]
	internal new byte[] blocksCompressed
	{
		get
		{
			return base.blocksCompressed;
		}
		set
		{
			base.blocksCompressed = value;
		}
	}

	[ProtoMember(2)]
	internal new byte[] lightCompressed
	{
		get
		{
			return base.lightCompressed;
		}
		set
		{
			base.lightCompressed = value;
		}
	}

	[ProtoMember(3)]
	internal new byte[] lightSatCompressed
	{
		get
		{
			return base.lightSatCompressed;
		}
		set
		{
			base.lightSatCompressed = value;
		}
	}

	[ProtoMember(16)]
	internal byte[] liquidsCompressed
	{
		get
		{
			return base.fluidsCompressed;
		}
		set
		{
			base.fluidsCompressed = value;
		}
	}

	[ProtoMember(5)]
	[CustomFastSerializer]
	public new int EntitiesCount
	{
		get
		{
			return base.EntitiesCount;
		}
		set
		{
			base.EntitiesCount = value;
		}
	}

	public override IMapChunk MapChunk => serverMapChunk;

	public bool NotAtEdge
	{
		get
		{
			if (serverMapChunk != null)
			{
				return serverMapChunk.NeighboursLoaded.Value() == 511;
			}
			return false;
		}
	}

	public override Dictionary<string, byte[]> ModData
	{
		get
		{
			return moddata;
		}
		set
		{
			if (value == null)
			{
				throw new NullReferenceException("ModData must not be set to null");
			}
			moddata = value;
		}
	}

	public override HashSet<int> LightPositions
	{
		get
		{
			return lightPositions;
		}
		set
		{
			lightPositions = value;
		}
	}

	string IServerChunk.GameVersionCreated => GameVersionCreated;

	int IServerChunk.BlocksPlaced => BlocksPlaced;

	int IServerChunk.BlocksRemoved => BlocksRemoved;

	static ServerChunk()
	{
		ReadWriteStopWatch = new Stopwatch();
		ReadWriteStopWatch.Start();
	}

	private ServerChunk()
	{
	}

	public static ServerChunk CreateNew(ChunkDataPool datapool)
	{
		ServerChunk serverChunk = new ServerChunk();
		serverChunk.datapool = datapool;
		serverChunk.PotentialBlockOrLightingChanges = true;
		serverChunk.chunkdataVersion = 2;
		serverChunk.chunkdata = datapool.Request();
		serverChunk.GameVersionCreated = "1.21.0";
		serverChunk.lightPositions = new HashSet<int>();
		serverChunk.moddata = new Dictionary<string, byte[]>();
		serverChunk.ServerSideModdata = new Dictionary<string, byte[]>();
		serverChunk.LiveModData = new Dictionary<string, object>();
		serverChunk.MaybeBlocks = datapool.OnlyAirBlocksData;
		serverChunk.MarkModified();
		return serverChunk;
	}

	public void RemoveEntitiesAndBlockEntities(IServerWorldAccessor server)
	{
		Entity[] entities = Entities;
		if (entities != null)
		{
			EntityDespawnData reason = new EntityDespawnData
			{
				Reason = EnumDespawnReason.Unload
			};
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				if (entity == null)
				{
					if (i >= EntitiesCount)
					{
						break;
					}
				}
				else if (!(entity is EntityPlayer))
				{
					server.DespawnEntity(entity, reason);
				}
			}
		}
		foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in BlockEntities)
		{
			blockEntity.Value.OnBlockUnloaded();
		}
	}

	public void ClearData()
	{
		ChunkData chunkData = chunkdata;
		PotentialBlockOrLightingChanges = true;
		chunkdataVersion = 2;
		chunkdata = datapool.Request();
		GameVersionCreated = "1.21.0";
		lightPositions = new HashSet<int>();
		moddata = new Dictionary<string, byte[]>();
		ServerSideModdata = new Dictionary<string, byte[]>();
		base.MaybeBlocks = datapool.OnlyAirBlocksData;
		MarkModified();
		base.Empty = true;
		if (chunkData != null)
		{
			datapool.Free(chunkData);
		}
	}

	public static ServerChunk FromBytes(byte[] serializedChunk, ChunkDataPool datapool, IWorldAccessor worldForResolve)
	{
		if (datapool == null)
		{
			throw new MissingFieldException("datapool cannot be null");
		}
		ServerChunk serverChunk;
		using (MemoryStream memoryStream = new MemoryStream(serializedChunk))
		{
			serverChunk = Serializer.Deserialize<ServerChunk>((Stream)memoryStream);
		}
		serverChunk.chunkdataVersion = serverChunk.savedCompressionVersion;
		serverChunk.datapool = datapool;
		if (serverChunk.blocksCompressed == null || serverChunk.lightCompressed == null || serverChunk.lightSatCompressed == null)
		{
			serverChunk.Unpack_MaybeNullData();
		}
		serverChunk.AfterDeserialization(worldForResolve);
		if (serverChunk.lightPositions == null)
		{
			serverChunk.lightPositions = new HashSet<int>();
		}
		if (serverChunk.moddata == null)
		{
			serverChunk.moddata = new Dictionary<string, byte[]>();
		}
		if (serverChunk.ServerSideModdata == null)
		{
			serverChunk.ServerSideModdata = new Dictionary<string, byte[]>();
		}
		if (serverChunk.LiveModData == null)
		{
			serverChunk.LiveModData = new Dictionary<string, object>();
		}
		serverChunk.MaybeBlocks = datapool.OnlyAirBlocksData;
		return serverChunk;
	}

	public byte[] ToBytes()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return ToBytes(ms);
	}

	public byte[] ToBytes(FastMemoryStream ms)
	{
		lock (packUnpackLock)
		{
			if (!IsPacked())
			{
				Pack();
				blocksCompressed = blocksCompressedTmp;
				lightCompressed = lightCompressedTmp;
				lightSatCompressed = lightSatCompressedTmp;
				liquidsCompressed = fluidsCompressedTmp;
				chunkdataVersion = 2;
			}
			savedCompressionVersion = chunkdataVersion;
			BeforeSerializationCommon();
			if (reusableSerializationStream == null)
			{
				reusableSerializationStream = new FastMemoryStream();
			}
			return ((IWithFastSerialize)this).FastSerialize(ms);
		}
	}

	protected override void UpdateForVersion()
	{
		chunkdata.UpdateFluids();
		chunkdataVersion = 2;
		DirtyForSaving = true;
		PotentialBlockOrLightingChanges = true;
	}

	public void MarkToPack()
	{
		PotentialBlockOrLightingChanges = true;
	}

	private void BeforeSerializationCommon()
	{
		foreach (KeyValuePair<string, object> liveModDatum in base.LiveModData)
		{
			SetModdata(liveModDatum.Key, liveModDatum.Value);
		}
		EmptyBeforeSave = base.Empty;
	}

	private int GatherEntitiesToSerialize(List<Entity> list)
	{
		int num = 0;
		Entity[] entities = Entities;
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (entity == null)
			{
				if (i >= EntitiesCount)
				{
					break;
				}
			}
			else if (!entity.StoreWithChunk)
			{
				num++;
			}
			else
			{
				list.Add(entity);
				num++;
			}
		}
		return num;
	}

	private void AfterDeserialization(IWorldAccessor worldAccessorForResolve)
	{
		lock (packUnpackLock)
		{
			base.Empty = EmptyBeforeSave;
			if (EntitiesSerialized == null)
			{
				Entities = Array.Empty<Entity>();
				EntitiesCount = 0;
			}
			else
			{
				Entity[] array = new Entity[EntitiesSerialized.Count];
				Dictionary<string, string> remappedEntities = ((IServerWorldAccessor)worldAccessorForResolve).RemappedEntities;
				int entitiesCount = 0;
				for (int i = 0; i < array.Length; i++)
				{
					string text = "unknown";
					try
					{
						using MemoryStream input = new MemoryStream(EntitiesSerialized[i]);
						BinaryReader binaryReader = new BinaryReader(input);
						text = binaryReader.ReadString();
						Entity entity = ServerMain.ClassRegistry.CreateEntity(text);
						entity.FromBytes(binaryReader, isSync: false, remappedEntities);
						array[entitiesCount++] = entity;
					}
					catch (Exception ex)
					{
						ServerMain.Logger.Error("Failed loading an entity (type " + text + ") in a chunk. Will discard, sorry. Exception logged to verbose debug.");
						ServerMain.Logger.VerboseDebug("Failed loading an entity in a chunk. Will discard, sorry. Exception: {0}", LoggerBase.CleanStackTrace(ex.ToString()));
					}
				}
				Entities = array;
				EntitiesCount = entitiesCount;
				EntitiesSerialized = null;
			}
			if (BlockEntitiesSerialized != null)
			{
				foreach (byte[] item in BlockEntitiesSerialized)
				{
					using MemoryStream input2 = new MemoryStream(item);
					using BinaryReader binaryReader2 = new BinaryReader(input2);
					string text2;
					try
					{
						text2 = binaryReader2.ReadString();
					}
					catch (Exception)
					{
						ServerMain.Logger.Error("Badly corrupted BlockEntity data in a chunk. Will discard it. Sorry.");
						goto end_IL_0162;
					}
					string text3 = null;
					try
					{
						TreeAttribute treeAttribute = new TreeAttribute();
						treeAttribute.FromBytes(binaryReader2);
						BlockEntity blockEntity = ServerMain.ClassRegistry.CreateBlockEntity(text2);
						Block block = null;
						text3 = treeAttribute.GetString("blockCode");
						if (text3 != null)
						{
							block = worldAccessorForResolve.GetBlock(new AssetLocation(text3));
						}
						if (block == null)
						{
							block = GetLocalBlockAtBlockPos(worldAccessorForResolve, treeAttribute.GetInt("posx"), treeAttribute.GetInt("posy"), treeAttribute.GetInt("posz"));
							if (block?.Code != null)
							{
								treeAttribute.SetString("blockCode", block.Code.ToShortString());
							}
						}
						if (block?.Code == null)
						{
							int num = treeAttribute.GetInt("posx");
							int num2 = treeAttribute.GetInt("posy");
							int num3 = treeAttribute.GetInt("posz");
							worldAccessorForResolve.Logger.Notification("Block entity with classname {3} at {0}, {1}, {2} has a block that is null or whose code is null o.O? Won't load this block entity!", num, num2, num3, text2);
						}
						else
						{
							blockEntity.CreateBehaviors(block, worldAccessorForResolve);
							blockEntity.FromTreeAttributes(treeAttribute, worldAccessorForResolve);
							BlockEntities[blockEntity.Pos] = blockEntity;
						}
					}
					catch (Exception ex3)
					{
						ServerMain.Logger.Error("Failed loading blockentity {0} for block {1} in a chunk. Will discard it. Sorry. Exception logged to verbose debug.", text2, text3);
						ServerMain.Logger.VerboseDebug("Failed loading a blockentity in a chunk. Will discard it. Sorry. Exception: {0}", LoggerBase.CleanStackTrace(ex3.ToString()));
					}
					end_IL_0162:;
				}
				BlockEntitiesCount = BlockEntities.Count;
				BlockEntitiesSerialized = null;
			}
			if (DecorsSerialized != null && DecorsSerialized.Length != 0)
			{
				Decors = new Dictionary<int, Block>();
				using (MemoryStream input3 = new MemoryStream(DecorsSerialized))
				{
					BinaryReader binaryReader3 = new BinaryReader(input3);
					while (binaryReader3.BaseStream.Position < binaryReader3.BaseStream.Length)
					{
						int key = binaryReader3.ReadInt32();
						int blockId = binaryReader3.ReadInt32();
						Block block2 = worldAccessorForResolve.GetBlock(blockId);
						Decors.Add(key, block2);
					}
				}
				DecorsSerialized = null;
			}
			if (LightPositions == null)
			{
				LightPositions = new HashSet<int>(0);
			}
			if (moddata == null)
			{
				moddata = new Dictionary<string, byte[]>(0);
			}
		}
	}

	public override void AddEntity(Entity entity)
	{
		base.AddEntity(entity);
		MarkModified();
	}

	public override bool RemoveEntity(long entityId)
	{
		bool num = base.RemoveEntity(entityId);
		if (num)
		{
			MarkModified();
		}
		return num;
	}

	internal Packet_ServerChunk ToPacket(int posX, int posY, int posZ, bool withEntities = false)
	{
		Packet_ServerChunk packet_ServerChunk = new Packet_ServerChunk
		{
			X = posX,
			Y = posY,
			Z = posZ
		};
		lock (packUnpackLock)
		{
			foreach (KeyValuePair<string, object> liveModDatum in base.LiveModData)
			{
				SetModdata(liveModDatum.Key, liveModDatum.Value);
			}
			if (chunkdata == null && chunkdataVersion < 2)
			{
				Unpack();
			}
			if (chunkdata != null)
			{
				UpdateEmptyFlag();
				if (PotentialBlockOrLightingChanges)
				{
					chunkdataVersion = 2;
					byte[] array = null;
					byte[] array2 = null;
					byte[] lightPaletteCompressed = null;
					byte[] array3 = null;
					chunkdata.CompressInto(ref array, ref array2, ref lightPaletteCompressed, ref array3, chunkdataVersion);
					base.blocksCompressed = array;
					base.lightCompressed = array2;
					base.lightSatCompressed = lightPaletteCompressed;
					base.fluidsCompressed = array3;
					PotentialBlockOrLightingChanges = false;
				}
				if (Environment.TickCount - lastReadOrWrite > MagicNum.UncompressedChunkTTL)
				{
					datapool.Free(chunkdata);
					base.MaybeBlocks = datapool.OnlyAirBlocksData;
					chunkdata = null;
				}
			}
			packet_ServerChunk.Empty = (base.Empty ? 1 : 0);
			packet_ServerChunk.SetBlocks(blocksCompressed);
			packet_ServerChunk.SetLight(lightCompressed);
			packet_ServerChunk.SetLightSat(lightSatCompressed);
			packet_ServerChunk.SetLiquids(liquidsCompressed);
			packet_ServerChunk.SetCompver(chunkdataVersion);
		}
		packet_ServerChunk.SetModdata(SerializerUtil.Serialize(moddata, reusableSerializationStream ?? (reusableSerializationStream = new FastMemoryStream())));
		if (BlockEntities.Count > 0)
		{
			packet_ServerChunk.SetBlockEntities(GetBlockEntitiesPackets());
		}
		if (LightPositions != null && LightPositions.Count > 0)
		{
			packet_ServerChunk.SetLightPositions(LightPositions.ToArray());
		}
		if (Decors != null && Decors.Count > 0)
		{
			int[] array4 = new int[Decors.Count];
			int[] array5 = new int[Decors.Count];
			int num = 0;
			foreach (KeyValuePair<int, Block> decor in Decors)
			{
				if (num >= array4.Length)
				{
					break;
				}
				array4[num] = decor.Key;
				array5[num] = decor.Value.BlockId;
				num++;
			}
			packet_ServerChunk.SetDecorsPos(array4);
			packet_ServerChunk.SetDecorsIds(array5);
		}
		return packet_ServerChunk;
	}

	internal Packet_Entity[] GetEntitiesPackets()
	{
		Packet_Entity[] array = new Packet_Entity[(Entities != null) ? EntitiesCount : 0];
		if (array.Length != 0)
		{
			using FastMemoryStream fastMemoryStream = reusableSerializationStream ?? (reusableSerializationStream = new FastMemoryStream());
			fastMemoryStream.Reset();
			BinaryWriter writer = new BinaryWriter(fastMemoryStream);
			int num = 0;
			Entity[] entities = Entities;
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				if (entity == null)
				{
					if (i >= EntitiesCount)
					{
						break;
					}
				}
				else
				{
					array[num++] = ServerPackets.GetEntityPacket(entity, fastMemoryStream, writer);
				}
			}
			if (num != array.Length)
			{
				Array.Resize(ref array, num);
			}
		}
		return array;
	}

	internal Packet_BlockEntity[] GetBlockEntitiesPackets()
	{
		Packet_BlockEntity[] array = new Packet_BlockEntity[BlockEntities.Count];
		using FastMemoryStream fastMemoryStream = reusableSerializationStream ?? (reusableSerializationStream = new FastMemoryStream());
		fastMemoryStream.Reset();
		BinaryWriter writer = new BinaryWriter(fastMemoryStream);
		int num = 0;
		Dictionary<Type, string> blockEntityTypeToClassnameMapping = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping;
		foreach (BlockEntity value2 in BlockEntities.Values)
		{
			if (value2 != null && blockEntityTypeToClassnameMapping.TryGetValue(value2.GetType(), out var value))
			{
				array[num++] = ServerPackets.getBlockEntityPacket(value2, value, fastMemoryStream, writer);
			}
		}
		return array;
	}

	public override void MarkModified()
	{
		base.MarkModified();
		DirtyForSaving = true;
	}

	public void SetServerModdata(string key, byte[] data)
	{
		ServerSideModdata[key] = data;
	}

	public byte[] GetServerModdata(string key)
	{
		ServerSideModdata.TryGetValue(key, out var value);
		return value;
	}

	public void ClearAll(IServerWorldAccessor worldAccessor)
	{
		RemoveEntitiesAndBlockEntities(worldAccessor);
		ClearData();
		BlockEntities?.Clear();
		Decors?.Clear();
		Entities = null;
	}

	private void FastSerializeEntitiesCount(FastMemoryStream ms, int idCount, ref int count, ref int savedPosition)
	{
		if (Entities != null && Entities.Length != 0)
		{
			savedPosition = (int)ms.Position + 1;
			List<Entity> list = reusableSerializationList ?? (reusableSerializationList = new List<Entity>(EntitiesCount));
			list.Clear();
			count = GatherEntitiesToSerialize(list);
			FastSerializer.Write(ms, idCount, count);
		}
	}

	private void FastSerializeEntities(FastMemoryStream ms, int idEntitySerialized, ref int count, ref int savedPosition)
	{
		if (Entities == null || Entities.Length == 0)
		{
			return;
		}
		List<Entity> list = reusableSerializationList;
		FastMemoryStream fastMemoryStream = reusableSerializationStream;
		if (list.Count > 0)
		{
			BinaryWriter binaryWriter = new BinaryWriter(fastMemoryStream);
			int num = 0;
			foreach (Entity item in list)
			{
				try
				{
					fastMemoryStream.Reset();
					binaryWriter.Write(ServerMain.ClassRegistry.GetEntityClassName(item.GetType()));
					item.ToBytes(binaryWriter, forClient: false);
					FastSerializer.Write(ms, idEntitySerialized, fastMemoryStream);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Error thrown trying to serialize entity with code {0}, will not save, sorry!", item?.Code);
					ServerMain.Logger.Error(e);
					num++;
				}
			}
			if (num > 0 && num <= count)
			{
				ms.WriteAt(savedPosition, count - num, FastSerializer.GetSize(count));
			}
		}
		list.Clear();
	}

	private void FastSerializeBlockEntitiesCount(FastMemoryStream ms, int idCount, ref int count, ref int savedPosition)
	{
		count = BlockEntities.Count;
		if (count != 0)
		{
			savedPosition = (int)ms.Position + 1;
			FastSerializer.Write(ms, idCount, count);
		}
	}

	private void FastSerializeBlockEntities(FastMemoryStream ms, int idEntitySerialized, ref int count, ref int savedPosition)
	{
		FastMemoryStream fastMemoryStream = reusableSerializationStream;
		BinaryWriter binaryWriter = new BinaryWriter(fastMemoryStream);
		int num = 0;
		foreach (BlockEntity value2 in BlockEntities.Values)
		{
			try
			{
				fastMemoryStream.Reset();
				string value = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[value2.GetType()];
				binaryWriter.Write(value);
				TreeAttribute treeAttribute = new TreeAttribute();
				value2.ToTreeAttributes(treeAttribute);
				treeAttribute.ToBytes(binaryWriter);
				FastSerializer.Write(ms, idEntitySerialized, fastMemoryStream);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Error thrown trying to serialize block entity {0} at {1}, will not save, sorry!", value2?.GetType(), value2?.Pos);
				ServerMain.Logger.Error(e);
				num++;
			}
		}
		if (num > 0 && num <= count)
		{
			ms.WriteAt(savedPosition, count - num, FastSerializer.GetSize(count));
		}
		BlockEntitiesCount = count - num;
	}

	private void FastSerializeDecors(FastMemoryStream ms, int id, ref int count, ref int savedPosition)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return;
		}
		FastSerializer.WriteTagLengthDelim(ms, id, Decors.Count * 8);
		foreach (KeyValuePair<int, Block> decor in Decors)
		{
			Block value = decor.Value;
			ms.WriteInt32(decor.Key);
			ms.WriteInt32(value.BlockId);
		}
	}

	public byte[] FastSerialize(FastMemoryStream ms)
	{
		ms.Reset();
		int count = 0;
		int savedPosition = 0;
		FastSerializer.Write(ms, 1, blocksCompressed);
		FastSerializer.Write(ms, 2, lightCompressed);
		FastSerializer.Write(ms, 3, lightSatCompressed);
		FastSerializeEntitiesCount(ms, 5, ref count, ref savedPosition);
		FastSerializeEntities(ms, 6, ref count, ref savedPosition);
		FastSerializeBlockEntitiesCount(ms, 7, ref count, ref savedPosition);
		FastSerializeBlockEntities(ms, 8, ref count, ref savedPosition);
		FastSerializer.Write(ms, 9, moddata);
		FastSerializer.Write(ms, 10, lightPositions);
		FastSerializer.Write(ms, 11, ServerSideModdata);
		FastSerializer.Write(ms, 12, GameVersionCreated);
		FastSerializer.Write(ms, 13, EmptyBeforeSave);
		FastSerializeDecors(ms, 14, ref count, ref savedPosition);
		FastSerializer.Write(ms, 15, savedCompressionVersion);
		FastSerializer.Write(ms, 16, liquidsCompressed);
		FastSerializer.Write(ms, 17, BlocksPlaced);
		FastSerializer.Write(ms, 18, BlocksRemoved);
		return ms.ToArray();
	}
}
