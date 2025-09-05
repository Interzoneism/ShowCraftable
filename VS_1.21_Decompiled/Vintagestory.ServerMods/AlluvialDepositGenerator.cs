using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AlluvialDepositGenerator : DepositGeneratorBase
{
	[JsonProperty]
	public NatFloat Radius;

	[JsonProperty]
	public NatFloat Thickness;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public int MaxYRoughness = 999;

	protected int worldheight;

	protected int radiusX;

	protected int radiusZ;

	private Random avgQRand = new Random();

	public AlluvialDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
		worldheight = api.World.BlockAccessor.MapSizeY;
	}

	public override void Init()
	{
		if (Radius == null)
		{
			Api.Server.LogWarning("Alluvial Deposit {0} has no radius property defined. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (variant.Climate != null && Radius.avg + Radius.var >= 32f)
		{
			Api.Server.LogWarning("Alluvial Deposit {0} has CheckClimate=true and radius > 32 blocks - this is not supported, sorry. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
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
		float num5 = Thickness.nextFloat(1f, DepositRand);
		float num6 = (int)num5 + ((DepositRand.NextFloat() < num5 - (float)(int)num5) ? 1 : 0);
		float num7 = 1f / (float)(radiusX * radiusX);
		float num8 = 1f / (float)(radiusZ * radiusZ);
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
		IList<Block> blocks = Api.World.Blocks;
		double num9 = (double)Api.World.BlockAccessor.MapSizeY * 0.8;
		bool flag = (double)depoCenterPos.Y > num9 || (double)DepositRand.NextFloat() > 0.33;
		int num10 = -1;
		Block block = null;
		for (int i = min; i < max; i++)
		{
			int num11 = i - num3;
			int num12 = i - depoCenterPos.X;
			float num13 = (float)(num12 * num12) * num7;
			for (int j = min2; j < max2; j++)
			{
				int num14 = j - num4;
				int num15 = j - depoCenterPos.Z;
				int num16 = mapChunk.WorldGenTerrainHeightMap[num14 * 32 + num11];
				if (num16 >= worldheight || Math.Abs(depoCenterPos.Y - num16) > MaxYRoughness)
				{
					continue;
				}
				int num17 = mapChunk.TopRockIdMap[num14 * 32 + num11];
				if (num17 != num10)
				{
					num10 = num17;
					Block block2 = blocks[num17];
					block = (block2.Variant.ContainsKey("rock") ? Api.World.GetBlock(new AssetLocation((flag ? "gravel-" : "sand-") + block2.Variant["rock"])) : null);
				}
				if (block == null || 1.0 - DistortNoiseGen.Noise((double)i / 3.0, (double)j / 3.0) * 1.5 + 0.15 - (double)(num13 + (float)(num15 * num15) * num8) < 0.0)
				{
					continue;
				}
				for (int k = 0; (float)k < num6; k++)
				{
					if (num16 > 1)
					{
						int index3d = (num16 % 32 * 32 + num14) * 32 + num11;
						IChunkBlocks data = chunks[num16 / 32].Data;
						int blockIdUnsafe = data.GetBlockIdUnsafe(index3d);
						Block block3 = blocks[blockIdUnsafe];
						if (block.BlockMaterial != EnumBlockMaterial.Soil || block3.BlockMaterial == EnumBlockMaterial.Soil)
						{
							data.SetBlockUnsafe(index3d, block.BlockId);
							data.SetFluid(index3d, 0);
							num16--;
						}
					}
				}
			}
		}
	}

	public float GetAbsAvgQuantity()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < 100; i++)
		{
			num += Radius.nextFloat(1f, avgQRand);
			num2 += Thickness.nextFloat(1f, avgQRand);
		}
		num /= 100f;
		num2 /= 100f;
		return num2 * num * num * (float)Math.PI * variant.TriesPerChunk;
	}

	public int[] GetBearingBlocks()
	{
		return Array.Empty<int>();
	}

	public override float GetMaxRadius()
	{
		return (Radius.avg + Radius.var) * 1.3f;
	}

	public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		throw new NotImplementedException();
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		throw new NotImplementedException();
	}
}
