using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockPatch
{
	[JsonProperty]
	public AssetLocation[] blockCodes;

	[JsonProperty]
	public float Chance = 0.05f;

	[JsonProperty]
	public int MinTemp = -30;

	[JsonProperty]
	public int MaxTemp = 40;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public float MinForest;

	[JsonProperty]
	public float MaxForest = 1f;

	[JsonProperty]
	public float MinShrub;

	[JsonProperty]
	public float MaxShrub = 1f;

	[JsonProperty]
	public float MinFertility;

	[JsonProperty]
	public float MaxFertility = 1f;

	[JsonProperty]
	public float MinY = -0.3f;

	[JsonProperty]
	public float MaxY = 1f;

	[JsonProperty]
	public EnumBlockPatchPlacement Placement = EnumBlockPatchPlacement.OnSurface;

	[JsonProperty]
	public EnumTreeType TreeType;

	[JsonProperty]
	public NatFloat OffsetX = NatFloat.createGauss(0f, 5f);

	[JsonProperty]
	public NatFloat OffsetZ = NatFloat.createGauss(0f, 5f);

	[JsonProperty]
	public NatFloat BlockCodeIndex;

	[JsonProperty]
	public NatFloat Quantity = NatFloat.createGauss(7f, 7f);

	[JsonProperty]
	public string MapCode;

	[JsonProperty]
	public string[] RandomMapCodePool;

	[JsonProperty]
	public int MinWaterDepth;

	[JsonProperty]
	public float MinWaterDepthP;

	[JsonProperty]
	public int MaxWaterDepth;

	[JsonProperty]
	public float MaxWaterDepthP;

	[JsonProperty]
	public int MaxHeightDifferential = 8;

	[JsonProperty]
	public bool PostPass;

	[JsonProperty]
	public bool PrePass;

	[JsonProperty]
	public BlockPatchAttributes Attributes;

	public Block[] Blocks;

	public Dictionary<int, Block[]> BlocksByRockType;

	private BlockPos pos = new BlockPos();

	private BlockPos tempPos = new BlockPos();

	public void Init(ICoreServerAPI api, RockStrataConfig rockstrata, LCGRandom rnd, int i)
	{
		List<Block> list = new List<Block>();
		for (int j = 0; j < blockCodes.Length; j++)
		{
			AssetLocation assetLocation = blockCodes[j];
			if (assetLocation.Path.Contains("{rocktype}"))
			{
				if (BlocksByRockType == null)
				{
					BlocksByRockType = new Dictionary<int, Block[]>();
				}
				for (int k = 0; k < rockstrata.Variants.Length; k++)
				{
					string newValue = rockstrata.Variants[k].BlockCode.Path.Split('-')[1];
					AssetLocation blockCode = assetLocation.CopyWithPath(assetLocation.Path.Replace("{rocktype}", newValue));
					Block block = api.World.GetBlock(rockstrata.Variants[k].BlockCode);
					if (block != null)
					{
						Block block2 = api.World.GetBlock(blockCode);
						BlocksByRockType[block.BlockId] = new Block[1] { block2 };
					}
				}
				continue;
			}
			Block block3 = api.World.GetBlock(assetLocation);
			if (block3 != null)
			{
				list.Add(block3);
			}
			else if (assetLocation.Path.Contains('*'))
			{
				Block[] array = api.World.SearchBlocks(assetLocation);
				if (array != null)
				{
					list.AddRange(array);
					continue;
				}
				api.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve block with code {1}. Will ignore.", i, assetLocation);
			}
			else
			{
				api.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve block with code {1}. Will ignore.", i, assetLocation);
			}
		}
		Blocks = list.ToArray();
		if (BlockCodeIndex == null)
		{
			BlockCodeIndex = NatFloat.createUniform(0f, Blocks.Length);
		}
		if (RandomMapCodePool != null)
		{
			int num = rnd.NextInt(RandomMapCodePool.Length);
			MapCode = RandomMapCodePool[num];
		}
		if (Attributes != null)
		{
			Attributes.Init(api, i);
		}
		if (MinWaterDepth == 0 && MinWaterDepthP != 0f)
		{
			MinWaterDepth = (int)((float)api.World.SeaLevel * Math.Clamp(MinWaterDepthP, 0f, 1f));
		}
		if (MaxWaterDepth == 0 && MaxWaterDepthP != 0f)
		{
			MaxWaterDepth = (int)((float)api.World.SeaLevel * Math.Clamp(MaxWaterDepthP, 0f, 1f));
		}
	}

	public void Generate(IBlockAccessor blockAccessor, IRandom rnd, int posX, int posY, int posZ, int firstBlockId, bool isStoryPatch)
	{
		float num = Quantity.nextFloat(1f, rnd) + 1f;
		Block[] blocks = getBlocks(firstBlockId);
		if (blocks.Length == 0)
		{
			return;
		}
		ModStdWorldGen modStdWorldGen = null;
		if (blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor)
		{
			modStdWorldGen = worldGenBlockAccessor.WorldgenWorldAccessor.Api.ModLoader.GetModSystem<GenVegetationAndPatches>();
		}
		while (num-- > 0f && (!(num < 1f) || !(rnd.NextFloat() > num)))
		{
			pos.X = posX + (int)OffsetX.nextFloat(1f, rnd);
			pos.Z = posZ + (int)OffsetZ.nextFloat(1f, rnd);
			if (!blockAccessor.IsValidPos(pos) || (modStdWorldGen != null && !isStoryPatch && modStdWorldGen.GetIntersectingStructure(pos, ModStdWorldGen.SkipPatchesgHashCode) != null))
			{
				continue;
			}
			int num2 = GameMath.Mod((int)BlockCodeIndex.nextFloat(1f, rnd), blocks.Length);
			IServerChunk serverChunk = (IServerChunk)blockAccessor.GetChunk(pos.X / 32, 0, pos.Z / 32);
			if (serverChunk == null)
			{
				break;
			}
			int num3 = GameMath.Mod(pos.X, 32);
			int num4 = GameMath.Mod(pos.Z, 32);
			if (Placement == EnumBlockPatchPlacement.Underground)
			{
				pos.Y = rnd.NextInt(Math.Max(1, serverChunk.MapChunk.WorldGenTerrainHeightMap[num4 * 32 + num3] - 1));
			}
			else
			{
				pos.Y = serverChunk.MapChunk.RainHeightMap[num4 * 32 + num3] + 1;
				if (Math.Abs(pos.Y - posY) > MaxHeightDifferential || pos.Y >= blockAccessor.MapSizeY - 1)
				{
					continue;
				}
				if (Placement == EnumBlockPatchPlacement.UnderWater || Placement == EnumBlockPatchPlacement.UnderSeaWater)
				{
					tempPos.Set(pos.X, pos.Y - 2, pos.Z);
					if (!blockAccessor.GetBlock(tempPos, 2).IsLiquid())
					{
						continue;
					}
					tempPos.Y = pos.Y - GameMath.Max(1, MinWaterDepth);
					Block block = blockAccessor.GetBlock(tempPos, 2);
					if ((Placement == EnumBlockPatchPlacement.UnderWater && block.LiquidCode != "water") || (Placement == EnumBlockPatchPlacement.UnderSeaWater && block.LiquidCode != "saltwater"))
					{
						continue;
					}
					if (MaxWaterDepth > 0)
					{
						tempPos.Set(pos.X, pos.Y - (MaxWaterDepth + 1), pos.Z);
						block = blockAccessor.GetBlock(tempPos, 2);
						if ((Placement == EnumBlockPatchPlacement.UnderWater && block.LiquidCode == "water") || (Placement == EnumBlockPatchPlacement.UnderSeaWater && block.LiquidCode == "saltwater"))
						{
							continue;
						}
					}
				}
			}
			if (Placement == EnumBlockPatchPlacement.UnderWater || Placement == EnumBlockPatchPlacement.UnderSeaWater)
			{
				blocks[num2].TryPlaceBlockForWorldGenUnderwater(blockAccessor, pos, BlockFacing.UP, rnd, MinWaterDepth, MaxWaterDepth, Attributes);
			}
			else
			{
				blocks[num2].TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, rnd, Attributes);
			}
		}
	}

	private Block[] getBlocks(int firstBlockId)
	{
		if (BlocksByRockType == null || !BlocksByRockType.TryGetValue(firstBlockId, out var value))
		{
			return Blocks;
		}
		return value;
	}
}
