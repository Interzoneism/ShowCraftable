using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class RoomRegistry : ModSystem
{
	protected Dictionary<long, ChunkRooms> roomsByChunkIndex = new Dictionary<long, ChunkRooms>();

	protected object roomsByChunkIndexLock = new object();

	private const int chunksize = 32;

	private int chunkMapSizeX;

	private int chunkMapSizeZ;

	private ICoreAPI api;

	[ThreadStatic]
	private static ICachingBlockAccessor blockAccessor;

	private ConcurrentDictionary<int, ICachingBlockAccessor> disposableBlockAccessors = new ConcurrentDictionary<int, ICachingBlockAccessor>();

	private const int ARRAYSIZE = 29;

	private readonly int[] currentVisited = new int[24389];

	private readonly int[] skyLightXZChecked = new int[841];

	private const int MAXROOMSIZE = 14;

	private const int MAXCELLARSIZE = 7;

	private const int ALTMAXCELLARSIZE = 9;

	private const int ALTMAXCELLARVOLUME = 150;

	private int iteration;

	private ICachingBlockAccessor blockAccess
	{
		get
		{
			if (blockAccessor != null)
			{
				return blockAccessor;
			}
			blockAccessor = api.World.GetCachingBlockAccessor(synchronize: false, relight: false);
			disposableBlockAccessors[Thread.CurrentThread.ManagedThreadId] = blockAccessor;
			return blockAccessor;
		}
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Event.ChunkDirty += Event_ChunkDirty;
	}

	public override void Dispose()
	{
		blockAccessor?.Dispose();
		blockAccessor = null;
		foreach (ICachingBlockAccessor value in disposableBlockAccessors.Values)
		{
			value?.Dispose();
		}
		disposableBlockAccessors.Clear();
		disposableBlockAccessors = null;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Event.BlockTexturesLoaded += init;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.SaveGameLoaded += init;
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("rooms").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("list")
			.HandleWith(onRoomRegDbgCmdList)
			.EndSubCommand()
			.BeginSubCommand("hi")
			.WithArgs(api.ChatCommands.Parsers.OptionalInt("rindex"))
			.RequiresPlayer()
			.HandleWith(onRoomRegDbgCmdHi)
			.EndSubCommand()
			.BeginSubCommand("unhi")
			.RequiresPlayer()
			.HandleWith(onRoomRegDbgCmdUnhi)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult onRoomRegDbgCmdHi(TextCommandCallingArgs args)
	{
		int num = (int)args.Parsers[0].GetValue();
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		BlockPos asBlockPos = serverPlayer.Entity.Pos.XYZ.AsBlockPos;
		long key = MapUtil.Index3dL(asBlockPos.X / 32, asBlockPos.Y / 32, asBlockPos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms value;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(key, out value);
		}
		if (value == null || value.Rooms.Count == 0)
		{
			return TextCommandResult.Success("No rooms in this chunk");
		}
		if (value.Rooms.Count - 1 < num || num < 0)
		{
			if (num == 0)
			{
				return TextCommandResult.Success("No room at this index");
			}
			return TextCommandResult.Success("Wrong index, select a number between 0 and " + (value.Rooms.Count - 1));
		}
		Room room = value.Rooms[num];
		if (args.Parsers[0].IsMissing)
		{
			room = null;
			foreach (Room room2 in value.Rooms)
			{
				if (room2.Contains(asBlockPos))
				{
					room = room2;
					break;
				}
			}
			if (room == null)
			{
				return TextCommandResult.Success("No room at your location");
			}
		}
		List<BlockPos> list = new List<BlockPos>();
		List<int> list2 = new List<int>();
		int num2 = room.Location.X2 - room.Location.X1 + 1;
		int num3 = room.Location.Y2 - room.Location.Y1 + 1;
		int num4 = room.Location.Z2 - room.Location.Z1 + 1;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num3; j++)
			{
				for (int k = 0; k < num4; k++)
				{
					int num5 = (j * num4 + k) * num2 + i;
					if ((room.PosInRoom[num5 / 8] & (1 << num5 % 8)) > 0)
					{
						list.Add(new BlockPos(room.Location.X1 + i, room.Location.Y1 + j, room.Location.Z1 + k));
						list2.Add(ColorUtil.ColorFromRgba((room.ExitCount != 0) ? 100 : 0, (room.ExitCount == 0) ? 100 : 0, Math.Min(255, num * 30), 150));
					}
				}
			}
		}
		api.World.HighlightBlocks(serverPlayer, 50, list, list2);
		return TextCommandResult.Success();
	}

	private TextCommandResult onRoomRegDbgCmdUnhi(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		api.World.HighlightBlocks(player, 50, new List<BlockPos>(), new List<int>());
		return TextCommandResult.Success();
	}

	private TextCommandResult onRoomRegDbgCmdList(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = (args.Caller.Player as IServerPlayer).Entity.Pos.XYZ.AsBlockPos;
		long key = MapUtil.Index3dL(asBlockPos.X / 32, asBlockPos.Y / 32, asBlockPos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms value;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(key, out value);
		}
		if (value == null || value.Rooms.Count == 0)
		{
			return TextCommandResult.Success("No rooms here");
		}
		string text = value.Rooms.Count + " Rooms here \n";
		lock (value.roomsLock)
		{
			for (int i = 0; i < value.Rooms.Count; i++)
			{
				Room room = value.Rooms[i];
				int num = room.Location.X2 - room.Location.X1 + 1;
				int num2 = room.Location.Y2 - room.Location.Y1 + 1;
				int num3 = room.Location.Z2 - room.Location.Z1 + 1;
				text += $"{i} - bbox dim: {num}/{num2}/{num3}, mid: {(float)room.Location.X1 + (float)num / 2f}/{(float)room.Location.Y1 + (float)num2 / 2f}/{(float)room.Location.Z1 + (float)num3 / 2f}\n";
			}
		}
		return TextCommandResult.Success(text);
	}

	private void init()
	{
		chunkMapSizeX = api.World.BlockAccessor.MapSizeX / 32;
		chunkMapSizeZ = api.World.BlockAccessor.MapSizeZ / 32;
	}

	private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		long num = MapUtil.Index3dL(chunkCoord.X, chunkCoord.Y, chunkCoord.Z, chunkMapSizeX, chunkMapSizeZ);
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		fastSetOfLongs.Add(num);
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(num, out var value);
			if (value != null)
			{
				fastSetOfLongs.Add(num);
				for (int i = 0; i < value.Rooms.Count; i++)
				{
					Cuboidi location = value.Rooms[i].Location;
					int num2 = location.Start.X / 32;
					int num3 = location.End.X / 32;
					int num4 = location.Start.Y / 32;
					int num5 = location.End.Y / 32;
					int num6 = location.Start.Z / 32;
					int num7 = location.End.Z / 32;
					fastSetOfLongs.Add(MapUtil.Index3dL(num2, num4, num6, chunkMapSizeX, chunkMapSizeZ));
					if (num7 != num6)
					{
						fastSetOfLongs.Add(MapUtil.Index3dL(num2, num4, num7, chunkMapSizeX, chunkMapSizeZ));
					}
					if (num5 != num4)
					{
						fastSetOfLongs.Add(MapUtil.Index3dL(num2, num5, num6, chunkMapSizeX, chunkMapSizeZ));
						if (num7 != num6)
						{
							fastSetOfLongs.Add(MapUtil.Index3dL(num2, num5, num7, chunkMapSizeX, chunkMapSizeZ));
						}
					}
					if (num3 == num2)
					{
						continue;
					}
					fastSetOfLongs.Add(MapUtil.Index3dL(num3, num4, num6, chunkMapSizeX, chunkMapSizeZ));
					if (num7 != num6)
					{
						fastSetOfLongs.Add(MapUtil.Index3dL(num3, num4, num7, chunkMapSizeX, chunkMapSizeZ));
					}
					if (num5 != num4)
					{
						fastSetOfLongs.Add(MapUtil.Index3dL(num3, num5, num6, chunkMapSizeX, chunkMapSizeZ));
						if (num7 != num6)
						{
							fastSetOfLongs.Add(MapUtil.Index3dL(num3, num5, num7, chunkMapSizeX, chunkMapSizeZ));
						}
					}
				}
			}
			foreach (long item in fastSetOfLongs)
			{
				roomsByChunkIndex.Remove(item);
			}
		}
	}

	public Room GetRoomForPosition(BlockPos pos)
	{
		long key = MapUtil.Index3dL(pos.X / 32, pos.Y / 32, pos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms value;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(key, out value);
		}
		Room room3;
		if (value != null)
		{
			Room room = null;
			Room room2 = null;
			for (int i = 0; i < value.Rooms.Count; i++)
			{
				room3 = value.Rooms[i];
				if (room3.Contains(pos))
				{
					if (room == null && room3.ExitCount == 0)
					{
						room = room3;
					}
					if (room2 == null && room3.ExitCount > 0)
					{
						room2 = room3;
					}
				}
			}
			if (room != null && room.IsFullyLoaded(value))
			{
				return room;
			}
			if (room2 != null && room2.IsFullyLoaded(value))
			{
				return room2;
			}
			room3 = FindRoomForPosition(pos, value);
			value.AddRoom(room3);
			return room3;
		}
		ChunkRooms chunkRooms = new ChunkRooms();
		room3 = FindRoomForPosition(pos, chunkRooms);
		chunkRooms.AddRoom(room3);
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex[key] = chunkRooms;
			return room3;
		}
	}

	private Room FindRoomForPosition(BlockPos pos, ChunkRooms otherRooms)
	{
		QueueOfInt queueOfInt = new QueueOfInt();
		int num = 14;
		int num2 = num + num;
		queueOfInt.Enqueue((num << 10) | (num << 5) | num);
		int num3 = (num * 29 + num) * 29 + num;
		int num4 = ++iteration;
		currentVisited[num3] = num4;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		blockAccess.Begin();
		bool flag = true;
		int num10 = num;
		int num11 = num;
		int num12 = num;
		int num13 = num;
		int num14 = num;
		int num15 = num;
		int num16 = pos.X - num;
		int num17 = pos.Y - num;
		int num18 = pos.Z - num;
		BlockPos blockPos = new BlockPos();
		BlockPos blockPos2 = new BlockPos();
		while (queueOfInt.Count > 0)
		{
			int num19 = queueOfInt.Dequeue();
			int num20 = num19 >> 10;
			int num21 = (num19 >> 5) & 0x1F;
			int num22 = num19 & 0x1F;
			blockPos.Set(num16 + num20, num17 + num21, num18 + num22);
			blockPos2.Set(blockPos);
			if (num20 < num10)
			{
				num10 = num20;
			}
			else if (num20 > num13)
			{
				num13 = num20;
			}
			if (num21 < num11)
			{
				num11 = num21;
			}
			else if (num21 > num14)
			{
				num14 = num21;
			}
			if (num22 < num12)
			{
				num12 = num22;
			}
			else if (num22 > num15)
			{
				num15 = num22;
			}
			Block block = blockAccess.GetBlock(blockPos2);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				blockFacing.IterateThruFacingOffsets(blockPos);
				int retention = block.GetRetention(blockPos2, blockFacing, EnumRetentionType.Heat);
				if (block.Id != 0 && retention != 0)
				{
					if (retention < 0)
					{
						num5 -= retention;
					}
					else
					{
						num6 += retention;
					}
					continue;
				}
				if (!blockAccess.IsValidPos(blockPos))
				{
					num6++;
					continue;
				}
				Block block2 = blockAccess.GetBlock(blockPos);
				flag &= blockAccess.LastChunkLoaded;
				retention = block2.GetRetention(blockPos, blockFacing.Opposite, EnumRetentionType.Heat);
				if (retention != 0)
				{
					if (retention < 0)
					{
						num5 -= retention;
					}
					else
					{
						num6 += retention;
					}
					continue;
				}
				num20 = blockPos.X - num16;
				num21 = blockPos.Y - num17;
				num22 = blockPos.Z - num18;
				bool flag2 = false;
				switch (blockFacing.Index)
				{
				case 0:
					if (num22 < num12)
					{
						flag2 = num22 < 0 || num15 - num12 + 1 >= 14;
					}
					break;
				case 1:
					if (num20 > num13)
					{
						flag2 = num20 > num2 || num13 - num10 + 1 >= 14;
					}
					break;
				case 2:
					if (num22 > num15)
					{
						flag2 = num22 > num2 || num15 - num12 + 1 >= 14;
					}
					break;
				case 3:
					if (num20 < num10)
					{
						flag2 = num20 < 0 || num13 - num10 + 1 >= 14;
					}
					break;
				case 4:
					if (num21 > num14)
					{
						flag2 = num21 > num2 || num14 - num11 + 1 >= 14;
					}
					break;
				case 5:
					if (num21 < num11)
					{
						flag2 = num21 < 0 || num14 - num11 + 1 >= 14;
					}
					break;
				}
				if (flag2)
				{
					num9++;
					continue;
				}
				num3 = (num20 * 29 + num21) * 29 + num22;
				if (currentVisited[num3] == num4)
				{
					continue;
				}
				currentVisited[num3] = num4;
				int num23 = num20 * 29 + num22;
				if (skyLightXZChecked[num23] < num4)
				{
					skyLightXZChecked[num23] = num4;
					if (blockAccess.GetLightLevel(blockPos, EnumLightLevelType.OnlySunLight) >= api.World.SunBrightness - 1)
					{
						num7++;
					}
					else
					{
						num8++;
					}
				}
				queueOfInt.Enqueue((num20 << 10) | (num21 << 5) | num22);
			}
		}
		int num24 = num13 - num10 + 1;
		int num25 = num14 - num11 + 1;
		int num26 = num15 - num12 + 1;
		byte[] array = new byte[(num24 * num25 * num26 + 7) / 8];
		int num27 = 0;
		for (int num20 = 0; num20 < num24; num20++)
		{
			for (int num21 = 0; num21 < num25; num21++)
			{
				num3 = ((num20 + num10) * 29 + (num21 + num11)) * 29 + num12;
				for (int num22 = 0; num22 < num26; num22++)
				{
					if (currentVisited[num3 + num22] == num4)
					{
						int num28 = (num21 * num26 + num22) * num24 + num20;
						array[num28 / 8] = (byte)(array[num28 / 8] | (1 << num28 % 8));
						num27++;
					}
				}
			}
		}
		bool flag3 = num24 <= 7 && num25 <= 7 && num26 <= 7;
		if (!flag3 && num27 <= 150)
		{
			flag3 = (num24 <= 9 && num25 <= 7 && num26 <= 7) || (num24 <= 7 && num25 <= 9 && num26 <= 7) || (num24 <= 7 && num25 <= 7 && num26 <= 9);
		}
		return new Room
		{
			CoolingWallCount = num5,
			NonCoolingWallCount = num6,
			SkylightCount = num7,
			NonSkylightCount = num8,
			ExitCount = num9,
			AnyChunkUnloaded = ((!flag) ? 1 : 0),
			Location = new Cuboidi(num16 + num10, num17 + num11, num18 + num12, num16 + num13, num17 + num14, num18 + num15),
			PosInRoom = array,
			IsSmallRoom = (flag3 && num9 == 0)
		};
	}
}
