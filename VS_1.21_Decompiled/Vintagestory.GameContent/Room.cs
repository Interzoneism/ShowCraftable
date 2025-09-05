using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class Room
{
	public int ExitCount;

	public bool IsSmallRoom;

	public int SkylightCount;

	public int NonSkylightCount;

	public int CoolingWallCount;

	public int NonCoolingWallCount;

	public Cuboidi Location;

	public byte[] PosInRoom;

	public int AnyChunkUnloaded;

	public bool IsFullyLoaded(ChunkRooms roomsList)
	{
		if (AnyChunkUnloaded == 0)
		{
			return true;
		}
		if (++AnyChunkUnloaded > 10)
		{
			roomsList.RemoveRoom(this);
		}
		return false;
	}

	public bool Contains(BlockPos pos)
	{
		if (!Location.ContainsOrTouches(pos))
		{
			return false;
		}
		int num = Location.Z2 - Location.Z1 + 1;
		int num2 = Location.X2 - Location.X1 + 1;
		int num3 = pos.X - Location.X1;
		int num4 = pos.Y - Location.Y1;
		int num5 = pos.Z - Location.Z1;
		int num6 = (num4 * num + num5) * num2 + num3;
		return (PosInRoom[num6 / 8] & (1 << num6 % 8)) > 0;
	}
}
