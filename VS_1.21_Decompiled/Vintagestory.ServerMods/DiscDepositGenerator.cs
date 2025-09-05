using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public abstract class DiscDepositGenerator : DepositGeneratorBase
{
	[JsonProperty]
	public DepositBlock InBlock;

	[JsonProperty]
	public DepositBlock PlaceBlock;

	[JsonProperty]
	public DepositBlock SurfaceBlock;

	[JsonProperty]
	public NatFloat Radius;

	[JsonProperty]
	public NatFloat Thickness;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public float SurfaceBlockChance = 0.05f;

	[JsonProperty]
	public float GenSurfaceBlockChance = 1f;

	[JsonProperty]
	public bool IgnoreParentTestPerBlock;

	[JsonProperty]
	public int MaxYRoughness = 999;

	[JsonProperty]
	public bool WithLastLayerBlockCallback;

	[JsonProperty]
	public EnumGradeDistribution GradeDistribution;

	protected float currentRelativeDepth;

	protected Dictionary<int, ResolvedDepositBlock> placeBlockByInBlockId = new Dictionary<int, ResolvedDepositBlock>();

	protected Dictionary<int, ResolvedDepositBlock> surfaceBlockByInBlockId = new Dictionary<int, ResolvedDepositBlock>();

	public MapLayerBase OreMap;

	protected int worldheight;

	protected int regionChunkSize;

	protected int noiseSizeClimate;

	protected int noiseSizeOre;

	protected int regionSize;

	protected BlockPos targetPos = new BlockPos();

	protected int radiusX;

	protected int radiusZ;

	protected float ypos;

	protected int posyi;

	protected int depoitThickness;

	protected int hereThickness;

	public double absAvgQuantity;

	protected DiscDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
		worldheight = api.World.BlockAccessor.MapSizeY;
		regionSize = api.WorldManager.RegionSize;
		regionChunkSize = api.WorldManager.RegionSize / 32;
		noiseSizeClimate = regionSize / TerraGenConfig.climateMapScale;
		noiseSizeOre = regionSize / TerraGenConfig.oreMapScale;
	}

	public override void Init()
	{
		if (Radius == null)
		{
			Api.Server.LogWarning("Deposit {0} has no radius property defined. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (variant.Climate != null && Radius.avg + Radius.var >= 32f)
		{
			Api.Server.LogWarning("Deposit {0} has CheckClimate=true and radius > 32 blocks - this is not supported, sorry. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (InBlock != null)
		{
			Block[] array = Api.World.SearchBlocks(InBlock.Code);
			if (array.Length == 0)
			{
				Api.Server.LogWarning("Deposit in file {0}, no such blocks found by code/wildcard '{1}'. Deposit will never spawn.", variant.fromFile, InBlock.Code);
			}
			Block[] array2 = array;
			foreach (Block block in array2)
			{
				if ((InBlock.AllowedVariants != null && !WildcardUtil.Match(InBlock.Code, block.Code, InBlock.AllowedVariants)) || (InBlock.AllowedVariantsByInBlock != null && !InBlock.AllowedVariantsByInBlock.ContainsKey(block.Code)))
				{
					continue;
				}
				string name = InBlock.Name;
				string wildcardValue = WildcardUtil.GetWildcardValue(InBlock.Code, block.Code);
				ResolvedDepositBlock resolvedDepositBlock = (placeBlockByInBlockId[block.BlockId] = PlaceBlock.Resolve(variant.fromFile, Api, block, name, wildcardValue));
				if (SurfaceBlock != null)
				{
					surfaceBlockByInBlockId[block.BlockId] = SurfaceBlock.Resolve(variant.fromFile, Api, block, name, wildcardValue);
				}
				Block[] blocks = resolvedDepositBlock.Blocks;
				if (variant.ChildDeposits != null)
				{
					DepositVariant[] childDeposits = variant.ChildDeposits;
					foreach (DepositVariant depositVariant in childDeposits)
					{
						if (depositVariant.GeneratorInst == null)
						{
							depositVariant.InitWithoutGenerator(Api);
							depositVariant.GeneratorInst = new ChildDepositGenerator(Api, depositVariant, DepositRand, DistortNoiseGen);
							depositVariant.Attributes.Token.Populate(depositVariant.GeneratorInst);
						}
						Block[] array3 = blocks;
						foreach (Block inblock in array3)
						{
							(depositVariant.GeneratorInst as ChildDepositGenerator).ResolveAdd(inblock, name, wildcardValue);
						}
					}
				}
				if (block.Id == 0 || !variant.addHandbookAttributes)
				{
					continue;
				}
				if (block.Attributes == null)
				{
					block.Attributes = new JsonObject(JToken.Parse("{}"));
				}
				int[] array4 = block.Attributes["hostRockFor"].AsArray(Array.Empty<int>());
				array4 = array4.Append(blocks.Select((Block b) => b.BlockId).ToArray());
				block.Attributes.Token[(object)"hostRockFor"] = JToken.FromObject((object)array4);
				foreach (Block block2 in blocks)
				{
					if (block2.Attributes == null)
					{
						block2.Attributes = new JsonObject(JToken.Parse("{}"));
					}
					array4 = block2.Attributes["hostRock"].AsArray(Array.Empty<int>());
					array4 = array4.Append(block.BlockId);
					block2.Attributes.Token[(object)"hostRock"] = JToken.FromObject((object)array4);
				}
			}
		}
		else
		{
			Api.Server.LogWarning("Deposit in file {0} has no inblock defined, it will never spawn.", variant.fromFile);
		}
		LCGRandom rnd = new LCGRandom(Api.World.Seed);
		absAvgQuantity = GetAbsAvgQuantity(rnd);
	}

	public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
	{
		int num = Math.Min(64, (int)Radius.nextFloat(1f, DepositRand));
		if (num <= 0)
		{
			return;
		}
		float num2 = GameMath.Clamp(DepositRand.NextFloat() - 0.5f, -0.25f, 0.25f);
		radiusX = num - (int)((float)num * num2);
		radiusZ = num + (int)((float)num * num2);
		int num3 = chunkX * 32;
		int num4 = chunkZ * 32;
		if (depoCenterPos.X + radiusX < num3 - 6 || depoCenterPos.Z + radiusZ < num4 - 6 || depoCenterPos.X - radiusX >= num3 + 32 + 6 || depoCenterPos.Z - radiusZ >= num4 + 32 + 6)
		{
			return;
		}
		IMapChunk mapChunk = chunks[0].MapChunk;
		beforeGenDeposit(mapChunk, depoCenterPos);
		if (!shouldGenDepositHere(depoCenterPos))
		{
			return;
		}
		int num5 = ((GradeDistribution == EnumGradeDistribution.RandomPlusDepthBonus) ? GameMath.RoundRandom(DepositRand, GameMath.Clamp(1f - currentRelativeDepth, 0f, 1f)) : 0);
		int val = ((PlaceBlock.MaxGrade != 0) ? Math.Min(PlaceBlock.MaxGrade - 1, DepositRand.NextInt(PlaceBlock.MaxGrade) + num5) : 0);
		float num6 = Thickness.nextFloat(1f, DepositRand);
		depoitThickness = (int)num6 + ((DepositRand.NextFloat() < num6 - (float)(int)num6) ? 1 : 0);
		float num7 = 1f / (float)(radiusX * radiusX);
		float num8 = 1f / (float)(radiusZ * radiusZ);
		bool flag = false;
		ResolvedDepositBlock value = null;
		bool flag2 = DepositRand.NextFloat() <= GenSurfaceBlockChance && SurfaceBlock != null;
		int min = num3 - 6;
		int max = num3 + 32 + 6;
		int min2 = num4 - 6;
		int max2 = num4 + 32 + 6;
		min = GameMath.Clamp(depoCenterPos.X - radiusX, min, max);
		max = GameMath.Clamp(depoCenterPos.X + radiusX, min, max);
		min2 = GameMath.Clamp(depoCenterPos.Z - radiusZ, min2, max2);
		max2 = GameMath.Clamp(depoCenterPos.Z + radiusZ, min2, max2);
		if (min < num3)
		{
			min = num3;
		}
		if (max > num3 + 32)
		{
			max = num3 + 32;
		}
		if (min2 < num4)
		{
			min2 = num4;
		}
		if (max2 > num4 + 32)
		{
			max2 = num4 + 32;
		}
		float num9 = 0.0009765625f;
		for (int i = min; i < max; i++)
		{
			int num10 = i - num3;
			int num11 = i - depoCenterPos.X;
			float num12 = (float)(num11 * num11) * num7;
			for (int j = min2; j < max2; j++)
			{
				int y = depoCenterPos.Y;
				int num13 = j - num4;
				int num14 = j - depoCenterPos.Z;
				double num15 = 1.0 - ((num > 3) ? (DistortNoiseGen.Noise((double)i / 3.0, (double)j / 3.0) * 0.2) : 0.0) - (double)(num12 + (float)(num14 * num14) * num8);
				if (num15 < 0.0)
				{
					continue;
				}
				targetPos.Set(i, y, j);
				loadYPosAndThickness(mapChunk, num10, num13, targetPos, num15);
				y = targetPos.Y;
				if (y >= worldheight || Math.Abs(depoCenterPos.Y - y) > MaxYRoughness)
				{
					continue;
				}
				for (int k = 0; k < hereThickness; k++)
				{
					if (y <= 1)
					{
						continue;
					}
					int index3d = (y % 32 * 32 + num13) * 32 + num10;
					int blockIdUnsafe = chunks[y / 32].Data.GetBlockIdUnsafe(index3d);
					if (!IgnoreParentTestPerBlock || !flag)
					{
						flag = placeBlockByInBlockId.TryGetValue(blockIdUnsafe, out value);
					}
					if (flag && value.Blocks.Length != 0)
					{
						int num16 = Math.Min(value.Blocks.Length - 1, val);
						Block block = value.Blocks[num16];
						if (variant.WithBlockCallback || (WithLastLayerBlockCallback && k == hereThickness - 1))
						{
							targetPos.Y = y;
							block.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP, DepositRand);
						}
						else
						{
							IChunkBlocks data = chunks[y / 32].Data;
							data.SetBlockUnsafe(index3d, block.BlockId);
							data.SetFluid(index3d, 0);
						}
						DepositVariant[] childDeposits = variant.ChildDeposits;
						if (childDeposits != null)
						{
							for (int l = 0; l < childDeposits.Length; l++)
							{
								float num17 = DepositRand.NextFloat();
								float num18 = childDeposits[l].TriesPerChunk * num9;
								if (num18 > num17 && ShouldPlaceAdjustedForOreMap(childDeposits[l], i, j, num18, num17))
								{
									subDepositsToPlace[new BlockPos(i, y, j)] = childDeposits[l];
								}
							}
						}
						if (flag2)
						{
							int num19 = mapChunk.RainHeightMap[num13 * 32 + num10];
							int num20 = num19 - y;
							float num21 = 9f * ((float)TerraGenConfig.seaLevel / 110f);
							float num22 = SurfaceBlockChance * Math.Max(0f, 1.11f - (float)num20 / num21);
							if (num19 < worldheight - 1 && DepositRand.NextFloat() < num22 && Api.World.Blocks[chunks[num19 / 32].Data.GetBlockIdUnsafe((num19 % 32 * 32 + num13) * 32 + num10)].SideSolid[BlockFacing.UP.Index])
							{
								index3d = ((num19 + 1) % 32 * 32 + num13) * 32 + num10;
								IChunkBlocks data2 = chunks[(num19 + 1) / 32].Data;
								if (data2.GetBlockIdUnsafe(index3d) == 0)
								{
									data2[index3d] = surfaceBlockByInBlockId[blockIdUnsafe].Blocks[0].BlockId;
								}
							}
						}
					}
					y--;
				}
			}
		}
	}

	protected virtual bool shouldGenDepositHere(BlockPos depoCenterPos)
	{
		return true;
	}

	protected abstract void beforeGenDeposit(IMapChunk mapChunk, BlockPos pos);

	protected abstract void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos pos, double distanceToEdge);

	public float getDepositYDistort(BlockPos pos, int lx, int lz, float step, IMapChunk heremapchunk)
	{
		int num = pos.X / 32 % regionChunkSize;
		int num2 = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float num3 = mapRegion.OreMapVerticalDistortTop.GetIntLerpedCorrectly((float)num * step + step * ((float)lx / 32f), (float)num2 * step + step * ((float)lz / 32f)) - 20f;
		float num4 = mapRegion.OreMapVerticalDistortBottom.GetIntLerpedCorrectly((float)num * step + step * ((float)lx / 32f), (float)num2 * step + step * ((float)lz / 32f)) - 20f;
		float num5 = (float)pos.Y / (float)worldheight;
		return num4 * (1f - num5) + num3 * num5;
	}

	private bool ShouldPlaceAdjustedForOreMap(DepositVariant variant, int posX, int posZ, float quantity, float rndVal)
	{
		if (variant.WithOreMap)
		{
			return variant.GetOreMapFactor(posX / 32, posZ / 32) * quantity > rndVal;
		}
		return true;
	}

	public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		int num = Api.World.BlockAccessor.GetTerrainMapheightAt(pos) * 32 * 32;
		double num2 = (double)(oreDist & 0xFF) / 255.0;
		double num3 = oreBearingBlockQuantityRelative(pos, variant.Code, blockColumn);
		totalFactor = num2 * num3;
		double num4 = totalFactor * absAvgQuantity / (double)num;
		ppt = num4 * 1000.0;
	}

	private double oreBearingBlockQuantityRelative(BlockPos pos, string oreCode, int[] blockColumn)
	{
		HashSet<int> hashSet = new HashSet<int>();
		if (variant == null)
		{
			return 0.0;
		}
		int[] bearingBlocks = GetBearingBlocks();
		if (bearingBlocks == null)
		{
			return 1.0;
		}
		int[] array = bearingBlocks;
		foreach (int item in array)
		{
			hashSet.Add(item);
		}
		GetYMinMax(pos, out var miny, out var maxy);
		int num = 0;
		for (int j = 0; j < blockColumn.Length; j++)
		{
			if (!((double)j < miny) && !((double)j > maxy) && hashSet.Contains(blockColumn[j]))
			{
				num++;
			}
		}
		return (double)num / (double)blockColumn.Length;
	}

	[Obsolete("Use GetAbsAvgQuantity(LCGRandom rnd) instead to ensure your code is seed deterministic.")]
	public float GetAbsAvgQuantity()
	{
		return GetAbsAvgQuantity(new LCGRandom(Api.World.Seed));
	}

	public float GetAbsAvgQuantity(LCGRandom rnd)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < 100; i++)
		{
			num += Radius.nextFloat(1f, rnd);
			num2 += Thickness.nextFloat(1f, rnd);
		}
		num /= 100f;
		num2 /= 100f;
		return num2 * num * num * (float)Math.PI * variant.TriesPerChunk;
	}

	public int[] GetBearingBlocks()
	{
		return placeBlockByInBlockId.Keys.ToArray();
	}

	public override float GetMaxRadius()
	{
		return (Radius.avg + Radius.var) * 1.3f;
	}
}
