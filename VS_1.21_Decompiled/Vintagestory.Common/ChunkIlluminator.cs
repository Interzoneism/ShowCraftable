using System;
using System.Collections.Generic;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class ChunkIlluminator
{
	private ushort defaultSunLight;

	private const int MAXLIGHTSPREAD = 31;

	private const int VISITED_WIDTH = 63;

	private int mapsizex;

	private int mapsizey;

	private int mapsizez;

	private int XPlus = 1;

	private int YPlus;

	private int ZPlus;

	private IList<Block> blockTypes;

	private int chunkSize;

	internal IChunkProvider chunkProvider;

	private IBlockAccessor readBlockAccess;

	private Dictionary<Vec3i, LightSourcesAtBlock> VisitedNodes = new Dictionary<Vec3i, LightSourcesAtBlock>();

	private List<NearbyLightSource> nearbyLightSources = new List<NearbyLightSource>();

	private BlockPos tmpDiPos = new BlockPos();

	private BlockPos tmpPos = new BlockPos();

	private BlockPos tmpPosDimensionAware = new BlockPos();

	private int[] currentVisited;

	private int iteration;

	public bool IsValidPos(int x, int y, int z)
	{
		if (x >= 0 && y >= 0 && z >= 0 && x < mapsizex && y % 32768 <= mapsizey)
		{
			return z <= mapsizez;
		}
		return false;
	}

	public ChunkIlluminator(IChunkProvider chunkProvider, IBlockAccessor readBlockAccess, int chunkSize)
	{
		this.readBlockAccess = readBlockAccess;
		this.chunkProvider = chunkProvider;
		this.chunkSize = chunkSize;
		YPlus = chunkSize * chunkSize;
		ZPlus = chunkSize;
		currentVisited = new int[250047];
	}

	public void InitForWorld(IList<Block> blockTypes, ushort defaultSunLight, int mapsizex, int mapsizey, int mapsizez)
	{
		this.blockTypes = blockTypes;
		this.defaultSunLight = defaultSunLight;
		this.mapsizex = mapsizex;
		this.mapsizey = mapsizey;
		this.mapsizez = mapsizez;
	}

	public void FullRelight(BlockPos minPos, BlockPos maxPos)
	{
		int num = chunkSize;
		Dictionary<Vec3i, IWorldChunk> dictionary = new Dictionary<Vec3i, IWorldChunk>();
		int num2 = GameMath.Clamp(Math.Min(minPos.X, maxPos.X) - num, 0, mapsizex - 1);
		int num3 = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y) - num, 0, mapsizey - 1);
		int num4 = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z) - num, 0, mapsizez - 1);
		int num5 = GameMath.Clamp(Math.Max(minPos.X, maxPos.X) + num, 0, mapsizex - 1);
		int num6 = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y) + num, 0, mapsizey - 1);
		int num7 = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z) + num, 0, mapsizez - 1);
		int num8 = num2 / num;
		int num9 = num3 / num;
		int num10 = num4 / num;
		int num11 = num5 / num;
		int num12 = num6 / num;
		int num13 = num7 / num;
		int num14 = minPos.dimension * 1024;
		for (int i = num8; i <= num11; i++)
		{
			for (int j = num9; j <= num12; j++)
			{
				for (int k = num10; k <= num13; k++)
				{
					IWorldChunk chunk = chunkProvider.GetChunk(i, j + num14, k);
					if (chunk != null)
					{
						chunk.Unpack();
						dictionary[new Vec3i(i, j, k)] = chunk;
					}
				}
			}
		}
		foreach (IWorldChunk value2 in dictionary.Values)
		{
			value2?.Lighting.ClearLight();
		}
		IWorldChunk[] array = new IWorldChunk[mapsizey / num];
		for (int l = num8; l <= num11; l++)
		{
			for (int m = num10; m <= num13; m++)
			{
				bool flag = false;
				for (int n = 0; n < array.Length; n++)
				{
					IWorldChunk chunk2 = chunkProvider.GetChunk(l, n + num14, m);
					if (chunk2 == null)
					{
						flag = true;
					}
					array[n] = chunk2;
				}
				if (!flag)
				{
					Sunlight(array, l, array.Length - 1, m, minPos.dimension);
					SunlightFlood(array, l, array.Length - 1, m);
					SunLightFloodNeighbourChunks(array, l, array.Length - 1, m, minPos.dimension);
				}
			}
		}
		Dictionary<BlockPos, Block> dictionary2 = new Dictionary<BlockPos, Block>();
		foreach (KeyValuePair<Vec3i, IWorldChunk> item in dictionary)
		{
			Vec3i key = item.Key;
			IWorldChunk value = item.Value;
			if (value == null)
			{
				continue;
			}
			int num15 = key.X * num;
			int num16 = key.Y * num;
			int num17 = key.Z * num;
			foreach (int lightPosition in value.LightPositions)
			{
				int num18 = key.Y * num + lightPosition / (num * num);
				int num19 = key.Z * num + lightPosition / num % num;
				int num20 = key.X * num + lightPosition % num;
				dictionary2[new BlockPos(num15 + num20, num16 + num18, num17 + num19, minPos.dimension)] = blockTypes[value.Data[lightPosition]];
			}
		}
		foreach (KeyValuePair<BlockPos, Block> item2 in dictionary2)
		{
			byte[] lightHsv = item2.Value.GetLightHsv(readBlockAccess, item2.Key);
			PlaceBlockLight(lightHsv, item2.Key.X, item2.Key.InternalY, item2.Key.Z);
		}
	}

	public void Sunlight(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ, int dim)
	{
		tmpPosDimensionAware.dimension = dim;
		int num = chunkSize;
		if (chunkY != chunks.Length - 1)
		{
			chunks[chunkY + 1].Unpack();
		}
		for (int num2 = chunkY; num2 >= 0; num2--)
		{
			chunks[num2].Unpack();
		}
		int num3 = chunkX * num;
		int num4 = chunkZ * num;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num5 = defaultSunLight;
				if (chunkY != chunks.Length - 1)
				{
					num5 = chunks[chunkY + 1].Lighting.GetSunlight(j * num + i);
				}
				for (int num6 = chunkY; num6 >= 0; num6--)
				{
					int num7 = ((num - 1) * num + j) * num + i;
					IWorldChunk worldChunk = chunks[num6];
					IChunkLight lighting = chunks[num6].Lighting;
					tmpPosDimensionAware.Set(num3 + i, num6 * num + num - 1, num4 + j);
					for (int num8 = num - 1; num8 >= 0; num8--)
					{
						int lightAbsorptionAt = worldChunk.GetLightAbsorptionAt(num7, tmpPosDimensionAware, blockTypes);
						lighting.SetSunlight(num7, num5);
						num7 -= YPlus;
						if (lightAbsorptionAt > num5)
						{
							num6 = -1;
							break;
						}
						num5 -= (ushort)lightAbsorptionAt;
						tmpPosDimensionAware.Y--;
					}
				}
			}
		}
	}

	public void SunlightFlood(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ)
	{
		int num = chunkSize;
		Stack<BlockPos> stack = new Stack<BlockPos>();
		int num2 = chunkX * num;
		int num3 = chunkZ * num;
		for (int num4 = chunkY; num4 >= 0; num4--)
		{
			IWorldChunk worldChunk = chunks[num4];
			worldChunk.Unpack();
			_ = worldChunk.Data;
			IChunkLight lighting = worldChunk.Lighting;
			for (int i = 0; i < num; i++)
			{
				tmpPosDimensionAware.Set(num2 + i, num4 * num + num, num3);
				for (int j = 0; j < num; j++)
				{
					int num5 = (num * num + j) * num + i;
					tmpPosDimensionAware.Z = num3 + j;
					for (int num6 = num - 1; num6 >= 0; num6--)
					{
						num5 -= YPlus;
						tmpPosDimensionAware.Y--;
						int num7 = lighting.GetSunlight(num5) - 1;
						if (num7 <= 0)
						{
							break;
						}
						int lightAbsorptionAt = worldChunk.GetLightAbsorptionAt(num5, tmpPosDimensionAware, blockTypes);
						num7 -= lightAbsorptionAt;
						if (num7 > 0 && ((i < num - 1 && lighting.GetSunlight(num5 + XPlus) < num7) || (j < num - 1 && lighting.GetSunlight(num5 + ZPlus) < num7) || (i > 0 && lighting.GetSunlight(num5 - XPlus) < num7) || (j > 0 && lighting.GetSunlight(num5 - ZPlus) < num7)))
						{
							stack.Push(new BlockPos(num2 + i, num4 * num + num6, num3 + j, tmpPosDimensionAware.dimension));
							if (stack.Count > 50)
							{
								SpreadSunLightInColumn(stack, chunks);
							}
						}
					}
				}
			}
		}
		SpreadSunLightInColumn(stack, chunks);
	}

	public byte SunLightFloodNeighbourChunks(IWorldChunk[] curChunks, int chunkX, int chunkY, int chunkZ, int dimension)
	{
		tmpPosDimensionAware.dimension = dimension;
		int num = chunkSize;
		byte b = 0;
		Stack<BlockPos> stack = new Stack<BlockPos>();
		Stack<BlockPos> stack2 = new Stack<BlockPos>();
		int[] array = new int[2];
		int[] array2 = new int[3];
		IWorldChunk[] array3 = new IWorldChunk[curChunks.Length];
		int num2 = chunkX * num;
		int num3 = chunkZ * num;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing blockFacing in hORIZONTALS)
		{
			bool flag = true;
			int x = blockFacing.Normali.X;
			int z = blockFacing.Normali.Z;
			for (int j = 0; j < curChunks.Length; j++)
			{
				array3[j] = chunkProvider.GetChunk(chunkX + x, j + dimension * 1024, chunkZ + z);
				if (array3[j] == null)
				{
					flag = false;
					if (j != 0)
					{
						chunkProvider.Logger.Error("not full column loaded @{0} {1} {2}, lighting error will probably happen", chunkX, j, chunkZ);
					}
					break;
				}
				array3[j].Unpack();
				curChunks[j].Unpack();
			}
			if (!flag)
			{
				continue;
			}
			int y = blockFacing.Normali.Y;
			array2[0] = (num - 1) * Math.Max(0, x);
			array2[1] = (num - 1) * Math.Max(0, y);
			array2[2] = (num - 1) * Math.Max(0, z);
			int num4 = (chunkX + x) * num;
			int num5 = 0;
			if (x == 0)
			{
				array[num5++] = 0;
			}
			if (y == 0)
			{
				array[num5++] = 1;
			}
			if (z == 0)
			{
				array[num5++] = 2;
			}
			for (int num6 = chunkY; num6 >= 0; num6--)
			{
				IWorldChunk worldChunk = array3[num6];
				IWorldChunk worldChunk2 = curChunks[num6];
				IChunkLight lighting = worldChunk.Lighting;
				IChunkLight lighting2 = worldChunk2.Lighting;
				for (int num7 = num - 1; num7 >= 0; num7--)
				{
					array2[array[0]] = num7;
					for (int num8 = num - 1; num8 >= 0; num8--)
					{
						array2[array[1]] = num8;
						int index3d = (array2[1] * num + array2[2]) * num + array2[0];
						int num9 = GameMath.Mod(array2[0] + x, num);
						int num10 = GameMath.Mod(array2[2] + z, num);
						int index3d2 = (array2[1] * num + num10) * num + num9;
						int num11 = lighting.GetSunlight(index3d2) - 1;
						int num12 = lighting2.GetSunlight(index3d) - 1;
						tmpPosDimensionAware.Set(num2 + array2[0], num6 * num + array2[1], num3 + array2[2]);
						int lightAbsorptionAt = worldChunk2.GetLightAbsorptionAt(index3d, tmpPosDimensionAware, blockTypes);
						tmpPosDimensionAware.Set(num4 + num9, num6 * num + array2[1], num4 + num10);
						int lightAbsorptionAt2 = worldChunk.GetLightAbsorptionAt(index3d2, tmpPosDimensionAware, blockTypes);
						int num13 = num11 - lightAbsorptionAt2;
						int num14 = num12 - lightAbsorptionAt;
						if (num14 > num11)
						{
							lighting.SetSunlight(index3d2, num14);
							stack2.Push(new BlockPos(num2 + num9, num6 * num + array2[1], num3 + num10, dimension));
							b |= blockFacing.Flag;
						}
						else if (num13 > num12)
						{
							lighting2.SetSunlight(index3d, num13);
							stack.Push(new BlockPos(num2 + array2[0], num6 * num + array2[1], num3 + array2[2], dimension));
						}
					}
				}
			}
			if (stack2.Count > 0)
			{
				SpreadSunLightInColumn(stack2, array3);
				for (int k = 0; k < array3.Length; k++)
				{
					array3[k].MarkModified();
				}
			}
			if (stack.Count > 0)
			{
				SpreadSunLightInColumn(stack, curChunks);
			}
		}
		return b;
	}

	public void SpreadSunLightInColumn(Stack<BlockPos> stack, IWorldChunk[] chunks)
	{
		int num = chunkSize;
		while (stack.Count > 0)
		{
			BlockPos blockPos = stack.Pop();
			int num2 = blockPos.X / num;
			int num3 = blockPos.Y / num;
			int num4 = blockPos.Z / num;
			int num5 = blockPos.X % num;
			int num6 = blockPos.Y % num;
			int num7 = blockPos.Z % num;
			int index3d = (num6 * num + num7) * num + num5;
			IWorldChunk worldChunk = chunks[num3];
			int lightAbsorptionAt = worldChunk.GetLightAbsorptionAt(index3d, blockPos, blockTypes);
			int num8 = worldChunk.Lighting.GetSunlight(index3d) - lightAbsorptionAt - 1;
			if (num8 <= 0)
			{
				continue;
			}
			int num9 = num3;
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num10 = blockPos.Y + vec3i.Y;
				int num11 = num5 + vec3i.X;
				int num12 = num7 + vec3i.Z;
				if (num11 >= 0 && num10 >= 0 && num12 >= 0 && num11 < num && num10 < mapsizey && num12 < num)
				{
					num3 = num10 / num;
					if (num3 != num9)
					{
						worldChunk = chunks[num3];
						worldChunk.Unpack();
						num9 = num3;
					}
					index3d = (num10 % num * num + num12) * num + num11;
					if (worldChunk.Lighting.GetSunlight(index3d) < num8)
					{
						worldChunk.Lighting.SetSunlight(index3d, num8);
						stack.Push(new BlockPos(num2 * num + num11, num10, num4 * num + num12, blockPos.dimension));
					}
				}
			}
		}
	}

	private int SunLightLevelAt(int posX, int posY, int posZ, bool substractAbsorb = false)
	{
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return defaultSunLight;
		}
		int index3d = (posY % num * num + posZ % num) * num + posX % num;
		return unpackedChunkFast.Lighting.GetSunlight(index3d) - (substractAbsorb ? unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes) : 0);
	}

	private void SetSunLightLevelAt(int posX, int posY, int posZ, int level)
	{
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast != null)
		{
			int index3d = (posY % num * num + posZ % num) * num + posX % num;
			unpackedChunkFast.Lighting.SetSunlight(index3d, level);
		}
	}

	private void ClearSunLightLevelAt(int posX, int posY, int posZ)
	{
		SetSunLightLevelAt(posX, posY, posZ, 0);
	}

	private int GetSunLightFromNeighbour(int posX, int posY, int posZ, bool directlyIlluminated)
	{
		int num = posY / 32768 * 32768;
		int num2 = 0;
		for (int i = 0; i < 6; i++)
		{
			Vec3i vec3i = BlockFacing.ALLNORMALI[i];
			int num3 = posX + vec3i.X;
			int num4 = posY + vec3i.Y;
			int num5 = posZ + vec3i.Z;
			if ((num3 | num5) >= 0 && num4 >= num && num3 < mapsizex && num4 < mapsizey + num && num5 < mapsizez)
			{
				IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num3 / chunkSize, num4 / chunkSize, num5 / chunkSize);
				if (unpackedChunkFast != null)
				{
					int index3d = (num4 % chunkSize * chunkSize + num5 % chunkSize) * chunkSize + num3 % chunkSize;
					int lightAbsorptionAt = unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(num3, num4, num5), blockTypes);
					int val = unpackedChunkFast.Lighting.GetSunlight(index3d) - lightAbsorptionAt - ((!(i == 4 && directlyIlluminated)) ? 1 : 0);
					num2 = Math.Max(num2, val);
				}
			}
		}
		return num2;
	}

	public FastSetOfLongs UpdateSunLight(int posX, int posY, int posZ, int oldAbsorb, int newAbsorb)
	{
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		if (newAbsorb == oldAbsorb)
		{
			return fastSetOfLongs;
		}
		int num = posY / 32768 * 32768;
		if (posX < 0 || posY < 0 || posZ < 0 || posX >= mapsizex || posY >= num + mapsizey || posZ >= mapsizez)
		{
			return fastSetOfLongs;
		}
		QueueOfInt queueOfInt = new QueueOfInt();
		bool flag = IsDirectlyIlluminated(posX, posY, posZ);
		BlockPos centerPos = new BlockPos(posX, posY, posZ);
		if (newAbsorb > oldAbsorb)
		{
			IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
			if (unpackedChunkFast == null)
			{
				return fastSetOfLongs;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			int sunlight = unpackedChunkFast.Lighting.GetSunlight(index3d);
			unpackedChunkFast.Lighting.SetSunlight_Buffered(index3d, 0);
			QueueOfInt queueOfInt2 = new QueueOfInt();
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num2 = posX + vec3i.X;
				int num3 = posY + vec3i.Y;
				int num4 = posZ + vec3i.Z;
				if (num2 >= 0 && num3 >= num && num4 >= 0 && num2 < mapsizex && num3 < num + mapsizey && num4 < mapsizez)
				{
					int num5 = sunlight - oldAbsorb - 1 + ((flag && i == 5) ? 1 : 0);
					int num6 = SunLightLevelAt(num2, num3, num4);
					if (num5 >= num6)
					{
						queueOfInt2.Enqueue(vec3i.X, vec3i.Y, vec3i.Z, num5 + (TileSideEnum.GetOpposite(i) + 1 << 5));
					}
				}
			}
			ClearSunlightAt(queueOfInt2, centerPos, flag, queueOfInt, fastSetOfLongs);
		}
		queueOfInt.Enqueue(0, 0, 0, GetSunLightFromNeighbour(posX, posY, posZ, flag));
		SpreadSunlightAt(queueOfInt, centerPos, flag, fastSetOfLongs);
		if (posY > 0)
		{
			SetSunLightLevelAt(posX, posY - 1, posZ, GetSunLightFromNeighbour(posX, posY - 1, posZ, flag));
		}
		if (newAbsorb > oldAbsorb)
		{
			for (int j = 0; j < 6; j++)
			{
				Vec3i vec3i2 = BlockFacing.ALLNORMALI[j];
				int num7 = posX + vec3i2.X;
				int num8 = posY + vec3i2.Y;
				int num9 = posZ + vec3i2.Z;
				if (IsValidPos(num7, num8, num9))
				{
					int sunLightFromNeighbour = GetSunLightFromNeighbour(num7, num8, num9, IsDirectlyIlluminated(num7, num8, num9));
					if (sunLightFromNeighbour > SunLightLevelAt(num7, num8, num9))
					{
						SetSunLightLevelAt(num7, num8, num9, sunLightFromNeighbour);
					}
				}
			}
		}
		return fastSetOfLongs;
	}

	public bool IsDirectlyIlluminated(int posX, int posY, int posZ)
	{
		int num = chunkSize;
		int num2 = 0;
		int num3 = SunLightLevelAt(posX, posY, posZ);
		while (posY < mapsizey)
		{
			posY++;
			IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num);
			if (unpackedChunkFast == null)
			{
				break;
			}
			int index3d = (posY % num * num + posZ % num) * num + posX % num;
			int sunlight = unpackedChunkFast.Lighting.GetSunlight(index3d);
			tmpDiPos.Set(posX, posY, posZ);
			num2 += unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpDiPos, blockTypes);
			if (defaultSunLight - num2 < num3)
			{
				return false;
			}
			if (sunlight == defaultSunLight)
			{
				return true;
			}
			if (num3 > sunlight)
			{
				return false;
			}
		}
		return defaultSunLight - num2 == num3;
	}

	public void SpreadSunlightAt(QueueOfInt unhandledPositions, BlockPos centerPos, bool isDirectlyIlluminated, FastSetOfLongs touchedChunks)
	{
		int num = chunkSize;
		tmpPos.dimension = centerPos.dimension;
		while (unhandledPositions.Count > 0)
		{
			int num2 = unhandledPositions.Dequeue();
			int num3 = (num2 >> 24) & 0x1F;
			if (num3 == 0)
			{
				continue;
			}
			int num4 = (num2 & 0xFF) - 128 + centerPos.X;
			int num5 = ((num2 >> 8) & 0xFF) - 128 + centerPos.Y;
			int num6 = ((num2 >> 16) & 0xFF) - 128 + centerPos.Z;
			IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num4 / num, num5 / num + centerPos.dimension * 1024, num6 / num);
			if (unpackedChunkFast == null)
			{
				continue;
			}
			int index3d = (num5 % num * num + num6 % num) * num + num4 % num;
			unpackedChunkFast.Lighting.SetSunlight_Buffered(index3d, num3);
			int lightAbsorptionAt = unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(num4, num5, num6), blockTypes);
			if (num3 - lightAbsorptionAt <= 0)
			{
				continue;
			}
			int num7 = ((num2 >> 29) & 7) - 1;
			for (int i = 0; i < 6; i++)
			{
				if (i == num7)
				{
					continue;
				}
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num8 = num4 + vec3i.X;
				int num9 = num5 + vec3i.Y;
				int num10 = num6 + vec3i.Z;
				if ((num8 | num9 | num10) < 0 || num8 >= mapsizex || num9 >= mapsizey || num10 >= mapsizez)
				{
					continue;
				}
				unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num8 / num, num9 / num + centerPos.dimension * 1024, num10 / num);
				if (unpackedChunkFast != null)
				{
					touchedChunks.Add(chunkProvider.ChunkIndex3D(num8 / num, num9 / num + centerPos.dimension * 1024, num10 / num));
					index3d = (num9 % num * num + num10 % num) * num + num8 % num;
					int num11 = num3 - lightAbsorptionAt - ((!isDirectlyIlluminated || num8 != centerPos.X || num10 != centerPos.Z || i != 5) ? 1 : 0);
					if (unpackedChunkFast.Lighting.GetSunlight(index3d) < num11)
					{
						unhandledPositions.EnqueueIfLarger(num8 - centerPos.X, num9 - centerPos.Y, num10 - centerPos.Z, num11 + (TileSideEnum.GetOpposite(i) + 1 << 5));
					}
				}
			}
		}
		tmpPos.dimension = 0;
	}

	public void ClearSunlightAt(QueueOfInt positionsToClear, BlockPos centerPos, bool isDirectlyIlluminated, QueueOfInt needTospreadQueue, FastSetOfLongs touchedChunks)
	{
		int num = chunkSize;
		FastSetOfInts fastSetOfInts = new FastSetOfInts();
		tmpPos.dimension = centerPos.dimension;
		while (positionsToClear.Count > 0)
		{
			int num2 = positionsToClear.Dequeue();
			int num3 = (num2 & 0xFF) - 128 + centerPos.X;
			int num4 = ((num2 >> 8) & 0xFF) - 128 + centerPos.Y;
			int num5 = ((num2 >> 16) & 0xFF) - 128 + centerPos.Z;
			IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num3 / num, num4 / num + centerPos.dimension * 1024, num5 / num);
			if (unpackedChunkFast == null)
			{
				continue;
			}
			int index3d = (num4 % num * num + num5 % num) * num + num3 % num;
			unpackedChunkFast.Lighting.SetSunlight_Buffered(index3d, 0);
			int lightAbsorptionAt = unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(num3, num4, num5), blockTypes);
			int num6 = ((num2 >> 24) & 0x1F) - lightAbsorptionAt;
			if (num6 <= 0)
			{
				continue;
			}
			int num7 = ((num2 >> 29) & 7) - 1;
			for (int i = 0; i < 6; i++)
			{
				if (i == num7)
				{
					continue;
				}
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num8 = num3 + vec3i.X;
				int num9 = num4 + vec3i.Y;
				int num10 = num5 + vec3i.Z;
				if ((num8 | num9 | num10) < 0 || num8 >= mapsizex || num9 >= mapsizey || num10 >= mapsizez)
				{
					continue;
				}
				unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num8 / num, num9 / num + centerPos.dimension * 1024, num10 / num);
				if (unpackedChunkFast == null)
				{
					continue;
				}
				touchedChunks.Add(chunkProvider.ChunkIndex3D(num8 / num, num9 / num + centerPos.dimension * 1024, num10 / num));
				int num11 = num6 - 1 + ((isDirectlyIlluminated && num8 == centerPos.X && num10 == centerPos.Z && i == 5) ? 1 : 0);
				if (num11 <= 0)
				{
					continue;
				}
				index3d = (num9 % num * num + num10 % num) * num + num8 % num;
				int sunlight = unpackedChunkFast.Lighting.GetSunlight(index3d);
				if (sunlight != 0)
				{
					if (sunlight <= num11)
					{
						fastSetOfInts.RemoveIfMatches(num8 - centerPos.X, num9 - centerPos.Y, num10 - centerPos.Z, sunlight);
						positionsToClear.EnqueueIfLarger(num8 - centerPos.X, num9 - centerPos.Y, num10 - centerPos.Z, num11 + (TileSideEnum.GetOpposite(i) + 1 << 5));
					}
					else
					{
						fastSetOfInts.Add(num8 - centerPos.X, num9 - centerPos.Y, num10 - centerPos.Z, sunlight);
					}
				}
			}
		}
		foreach (int item in fastSetOfInts)
		{
			needTospreadQueue.Enqueue(item);
		}
		tmpPos.dimension = 0;
	}

	public FastSetOfLongs PlaceBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
	{
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		IWorldChunk chunkAtPos = GetChunkAtPos(posX, posY, posZ);
		if (chunkAtPos == null)
		{
			return fastSetOfLongs;
		}
		chunkAtPos.LightPositions.Add(InChunkIndex(posX, posY, posZ));
		UpdateLightAt(lightHsv[2], posX, posY, posZ, fastSetOfLongs);
		return fastSetOfLongs;
	}

	public void PlaceNonBlendingBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
	{
		SetBlockLightLevel(lightHsv[0], lightHsv[1], lightHsv[2], posX, posY, posZ);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing face in aLLFACES)
		{
			NonBlendingLightAxis(lightHsv[0], lightHsv[1], lightHsv[2], posX, posY, posZ, face);
		}
	}

	private void SetBlockLightLevel(byte hue, byte saturation, int value, int posX, int posY, int posZ)
	{
		IWorldChunk chunkAtPos = GetChunkAtPos(posX, posY, posZ);
		if (chunkAtPos != null && GetBlock(posX, posY, posZ) != null)
		{
			chunkAtPos.LightPositions.Add(InChunkIndex(posX, posY, posZ));
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunkAtPos.Lighting.SetBlocklight_Buffered(index3d, (value << 5) | (hue << 10) | (saturation << 16));
		}
	}

	private int GetBlockLight(int x, int y, int z)
	{
		IWorldChunk chunkAtPos = GetChunkAtPos(x, y, z);
		if (chunkAtPos != null)
		{
			int index3d = (y % chunkSize * chunkSize + z % chunkSize) * chunkSize + x % chunkSize;
			chunkAtPos.Unpack_ReadOnly();
			return chunkAtPos.Lighting.GetBlocklight(index3d);
		}
		return 0;
	}

	private void NonBlendingLightAxis(byte hue, byte saturation, int lightLevel, int x, int y, int z, BlockFacing face)
	{
		int num = lightLevel - 1;
		while (num > 0)
		{
			x += face.Normali.X;
			y += face.Normali.Y;
			z += face.Normali.Z;
			if (y >= 0 && y <= mapsizey)
			{
				Block block = GetBlock(x, y, z);
				if (block != null && block.BlockId == 0 && GetBlockLight(x, y, z) < num)
				{
					SetBlockLightLevel(hue, saturation, num, x, y, z);
					if (face.Axis == EnumAxis.X)
					{
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.UP);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.DOWN);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.NORTH);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.SOUTH);
					}
					else if (face.Axis == EnumAxis.Y)
					{
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.WEST);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.EAST);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.NORTH);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.SOUTH);
					}
					else if (face.Axis == EnumAxis.Z)
					{
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.UP);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.DOWN);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.WEST);
						NonBlendingLightAxis(hue, saturation, num, x, y, z, BlockFacing.EAST);
					}
					num--;
					continue;
				}
				break;
			}
			break;
		}
	}

	public FastSetOfLongs RemoveBlockLight(byte[] oldLightHsv, int posX, int posY, int posZ)
	{
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		IWorldChunk chunkAtPos = GetChunkAtPos(posX, posY, posZ);
		if (chunkAtPos == null)
		{
			return fastSetOfLongs;
		}
		chunkAtPos.LightPositions.Remove(InChunkIndex(posX, posY, posZ));
		int num = oldLightHsv[2];
		if (num == 18)
		{
			num = 20;
		}
		int rangeNext = num - chunkAtPos.GetLightAbsorptionAt(InChunkIndex(posX, posY, posZ), tmpPos.Set(posX, posY, posZ), blockTypes) - 1;
		SpreadDarkness(rangeNext, posX, posY, posZ, fastSetOfLongs);
		UpdateLightAt(num, posX, posY, posZ, fastSetOfLongs);
		return fastSetOfLongs;
	}

	public FastSetOfLongs UpdateBlockLight(int oldLightAbsorb, int newLightAbsorb, int posX, int posY, int posZ)
	{
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return fastSetOfLongs;
		}
		int index3d = (posY % num * num + posZ % num) * num + posX % num;
		int blocklight = unpackedChunkFast.Lighting.GetBlocklight(index3d);
		if (oldLightAbsorb == newLightAbsorb)
		{
			return fastSetOfLongs;
		}
		if (blocklight == 0)
		{
			return fastSetOfLongs;
		}
		if (newLightAbsorb > oldLightAbsorb)
		{
			int rangeNext = blocklight - oldLightAbsorb - 1;
			SpreadDarkness(rangeNext, posX, posY, posZ, fastSetOfLongs);
		}
		UpdateLightAt(blocklight, posX, posY, posZ, fastSetOfLongs);
		return fastSetOfLongs;
	}

	private void UpdateLightAt(int range, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
	{
		VisitedNodes.Clear();
		int num = chunkSize;
		LoadNearbyLightSources(posX, posY, posZ, range);
		foreach (NearbyLightSource nearbyLightSource in nearbyLightSources)
		{
			CollectLightValuesForLightSource(nearbyLightSource.posX, nearbyLightSource.posY, nearbyLightSource.posZ, posX, posY, posZ, range);
		}
		foreach (KeyValuePair<Vec3i, LightSourcesAtBlock> visitedNode in VisitedNodes)
		{
			RecalcBlockLightAtPos(visitedNode.Key, visitedNode.Value);
			touchedChunks.Add(chunkProvider.ChunkIndex3D(visitedNode.Key.X / num, visitedNode.Key.Y / num, visitedNode.Key.Z / num));
		}
	}

	private void SpreadDarkness(int rangeNext, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
	{
		if (rangeNext <= 0)
		{
			return;
		}
		int num = chunkSize;
		QueueOfInt queueOfInt = new QueueOfInt();
		queueOfInt.Enqueue(0x1F1F1F | (rangeNext << 24));
		bool flag = posX < rangeNext - 1 || posZ < rangeNext - 1 || posX >= mapsizex - rangeNext + 1 || posZ >= mapsizez - rangeNext + 1;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return;
		}
		int index3d = (posY % num * num + posZ % num) * num + posX % num;
		unpackedChunkFast.Lighting.SetBlocklight(index3d, 0);
		touchedChunks.Add(chunkProvider.ChunkIndex3D(posX / num, posY / num, posZ / num));
		int num2 = ++iteration;
		posX -= 31;
		posY -= 31;
		posZ -= 31;
		int num3 = 125023;
		currentVisited[num3] = num2;
		while (queueOfInt.Count > 0)
		{
			int num4 = queueOfInt.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num5 = (num4 & 0xFF) + vec3i.X;
				int num6 = ((num4 >> 8) & 0xFF) + vec3i.Y;
				int num7 = ((num4 >> 16) & 0xFF) + vec3i.Z;
				num3 = num5 + (num6 * 63 + num7) * 63;
				if (currentVisited[num3] == num2)
				{
					continue;
				}
				currentVisited[num3] = num2;
				int num8 = num5 + posX;
				int num9 = num6 + posY;
				int num10 = num7 + posZ;
				if (num9 < 0 || num9 % 32768 >= mapsizey || (flag && (num8 < 0 || num10 < 0 || num8 >= mapsizex || num10 >= mapsizez)))
				{
					continue;
				}
				unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num8 / num, num9 / num, num10 / num);
				if (unpackedChunkFast != null)
				{
					index3d = (num9 % num * num + num10 % num) * num + num8 % num;
					if (unpackedChunkFast.Lighting.GetBlocklight(index3d) > 0)
					{
						touchedChunks.Add(chunkProvider.ChunkIndex3D(num8 / num, num9 / num, num10 / num));
						unpackedChunkFast.Lighting.SetBlocklight_Buffered(index3d, 0);
					}
					int num11 = (num4 >> 24) - unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(num8, num9, num10), blockTypes) - 1;
					if (num11 > 0)
					{
						queueOfInt.Enqueue(num5 | (num6 << 8) | (num7 << 16) | (num11 << 24));
					}
				}
			}
		}
	}

	private void CollectLightValuesForLightSource(int posX, int posY, int posZ, int forPosX, int forPosY, int forPosZ, int forRange)
	{
		int num = chunkSize;
		QueueOfInt queueOfInt = new QueueOfInt();
		Block block = GetBlock(posX, posY, posZ);
		if (block == null)
		{
			return;
		}
		byte[] lightHsv = block.GetLightHsv(readBlockAccess, tmpPos.Set(posX, posY, posZ));
		byte h = lightHsv[0];
		byte s = lightHsv[1];
		byte b = lightHsv[2];
		queueOfInt.Enqueue(0x1F1F1F | (b << 24));
		Vec3i key = new Vec3i(posX, posY, posZ);
		VisitedNodes.TryGetValue(key, out var value);
		if (value == null)
		{
			value = (VisitedNodes[key] = new LightSourcesAtBlock());
		}
		value.AddHsv(h, s, b);
		bool flag = posX < b - 1 || posZ < b - 1 || posX >= mapsizex - b + 1 || posZ >= mapsizez - b + 1;
		int num2 = ++iteration;
		posX -= 31;
		posY -= 31;
		posZ -= 31;
		int num3 = 125023;
		currentVisited[num3] = num2;
		while (queueOfInt.Count > 0)
		{
			int num4 = queueOfInt.Dequeue();
			int num5 = (num4 & 0xFF) + posX;
			int num6 = ((num4 >> 8) & 0xFF) + posY;
			int num7 = ((num4 >> 16) & 0xFF) + posZ;
			IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(num5 / num, num6 / num, num7 / num);
			if (unpackedChunkFast == null)
			{
				continue;
			}
			int index3d = (num6 % num * num + num7 % num) * num + num5 % num;
			int num8 = (num4 >> 24) - unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(num5, num6, num7), blockTypes) - 1;
			if (num8 <= 0)
			{
				continue;
			}
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				int num9 = num5 + vec3i.X;
				int num10 = num6 + vec3i.Y;
				int num11 = num7 + vec3i.Z;
				num3 = ((num10 - posY) * 63 + num11 - posZ) * 63 + num9 - posX;
				if (currentVisited[num3] == num2)
				{
					continue;
				}
				currentVisited[num3] = num2;
				if (num10 >= 0 && num10 % 32768 < mapsizey && (!flag || (num9 >= 0 && num11 >= 0 && num9 < mapsizex && num11 < mapsizez)) && Math.Abs(num9 - forPosX) + Math.Abs(num10 - forPosY) + Math.Abs(num11 - forPosZ) < forRange + num8)
				{
					queueOfInt.Enqueue((num9 - posX) | (num10 - posY << 8) | (num11 - posZ << 16) | (num8 << 24));
					key = new Vec3i(num9, num10, num11);
					VisitedNodes.TryGetValue(key, out value);
					if (value == null)
					{
						value = (VisitedNodes[key] = new LightSourcesAtBlock());
					}
					value.AddHsv(h, s, (byte)num8);
				}
			}
		}
	}

	private void RecalcBlockLightAtPos(Vec3i pos, LightSourcesAtBlock lsab)
	{
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(pos.X / num, pos.Y / num, pos.Z / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return;
		}
		int index3d = (pos.Y % num * num + pos.Z % num) * num + pos.X % num;
		float num2 = 0f;
		int num3 = 0;
		int lightCount = lsab.lightCount;
		for (int i = 0; i < lightCount; i++)
		{
			int num4 = lsab.lightHsvs[i * 3 + 2];
			num3 = Math.Max(num3, num4);
			num2 += (float)num4;
		}
		if (num3 == 0)
		{
			unpackedChunkFast.Lighting.SetBlocklight(index3d, 0);
			return;
		}
		float num5 = 0.5f;
		float num6 = 0.5f;
		float num7 = 0.5f;
		for (int j = 0; j < lightCount; j++)
		{
			int num8 = lsab.lightHsvs[j * 3 + 2];
			int num9 = ColorUtil.HsvToRgb(lsab.lightHsvs[j * 3] * 4, lsab.lightHsvs[j * 3 + 1] * 32, num8 * 8);
			float num10 = (float)num8 / num2;
			num5 += (float)(num9 >> 16) * num10;
			num6 += (float)((num9 >> 8) & 0xFF) * num10;
			num7 += (float)(num9 & 0xFF) * num10;
		}
		int num11 = ColorUtil.Rgb2Hsv(num5, num6, num7);
		int num12 = Math.Min((int)((float)(num11 & 0xFF) / 4f + 0.5f), ColorUtil.HueQuantities - 1);
		int num13 = Math.Min((int)((float)((num11 >> 8) & 0xFF) / 32f + 0.5f), ColorUtil.SatQuantities - 1);
		unpackedChunkFast.Lighting.SetBlocklight(index3d, (num3 << 5) | (num12 << 10) | (num13 << 16));
	}

	private Block GetBlock(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) < 0)
		{
			return null;
		}
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return null;
		}
		int index3d = (posY % num * num + posZ % num) * num + posX % num;
		return blockTypes[unpackedChunkFast.Data[index3d]];
	}

	private int GetBlockLightAbsorb(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) < 0)
		{
			return 0;
		}
		int num = chunkSize;
		IWorldChunk unpackedChunkFast = chunkProvider.GetUnpackedChunkFast(posX / num, posY / num, posZ / num, notRecentlyAccessed: true);
		if (unpackedChunkFast == null)
		{
			return 0;
		}
		int index3d = (posY % num * num + posZ % num) * num + posX % num;
		return unpackedChunkFast.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes);
	}

	private IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
	{
		return chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
	}

	private int InChunkIndex(int posX, int posY, int posZ)
	{
		return (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
	}

	internal long GetChunkIndexForPos(int posX, int posY, int posZ)
	{
		return chunkProvider.ChunkIndex3D(posX / chunkSize, posY / chunkSize, posZ / chunkSize);
	}

	private void LoadNearbyLightSources(int posX, int posY, int posZ, int range)
	{
		nearbyLightSources.Clear();
		int num = posX / chunkSize;
		int num2 = posY / chunkSize;
		int num3 = posZ / chunkSize;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					IWorldChunk chunk = chunkProvider.GetChunk(num + i, num2 + j, num3 + k);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack_ReadOnly();
					foreach (int lightPosition in chunk.LightPositions)
					{
						int num4 = (num2 + j) * chunkSize + lightPosition / (chunkSize * chunkSize);
						int num5 = (num3 + k) * chunkSize + lightPosition / chunkSize % chunkSize;
						int num6 = (num + i) * chunkSize + lightPosition % chunkSize;
						int num7 = Math.Abs(posX - num6) + Math.Abs(posY - num4) + Math.Abs(posZ - num5);
						Block block = blockTypes[chunk.Data[lightPosition]];
						if (block.GetLightHsv(readBlockAccess, tmpPos.Set(num6, num4, num5))[2] + range > num7)
						{
							nearbyLightSources.Add(new NearbyLightSource
							{
								block = block,
								posX = num6,
								posY = num4,
								posZ = num5
							});
						}
					}
				}
			}
		}
	}
}
