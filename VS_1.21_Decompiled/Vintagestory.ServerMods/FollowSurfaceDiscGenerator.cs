using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class FollowSurfaceDiscGenerator : DiscDepositGenerator
{
	[JsonProperty]
	public NatFloat YPosRel;

	private float step;

	public FollowSurfaceDiscGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	protected override void beforeGenDeposit(IMapChunk mapChunk, BlockPos pos)
	{
		ypos = YPosRel.nextFloat(1f, DepositRand);
		pos.Y = (int)ypos;
		int num = pos.X % 32;
		int num2 = pos.Z % 32;
		if (num < 0 || num2 < 0)
		{
			currentRelativeDepth = 0f;
		}
		else
		{
			currentRelativeDepth = ypos / (float)(int)mapChunk.WorldGenTerrainHeightMap[num2 * 32 + num];
		}
		step = (float)mapChunk.MapRegion.OreMapVerticalDistortTop.InnerSize / (float)regionChunkSize;
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		float num = 9999f;
		for (int i = 0; i < 100; i++)
		{
			num = Math.Min(num, YPosRel.nextFloat(1f, DepositRand));
		}
		miny = num * (float)pos.Y;
		maxy = (float)pos.Y;
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos pos, double distanceToEdge)
	{
		hereThickness = depoitThickness;
		pos.Y = (int)(ypos * (float)(int)heremapchunk.WorldGenTerrainHeightMap[lz * 32 + lx]);
		pos.Y -= (int)getDepositYDistort(pos, lx, lz, step, heremapchunk);
		double num = (double)depoitThickness * GameMath.Clamp(distanceToEdge * 2.0 - 0.2, 0.0, 1.0);
		hereThickness = (int)num + ((DepositRand.NextDouble() < num - (double)(int)num) ? 1 : 0);
	}
}
