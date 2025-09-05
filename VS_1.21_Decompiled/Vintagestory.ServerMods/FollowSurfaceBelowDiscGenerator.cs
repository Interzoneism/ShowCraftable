using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class FollowSurfaceBelowDiscGenerator : DiscDepositGenerator
{
	public FollowSurfaceBelowDiscGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	protected override void beforeGenDeposit(IMapChunk mapChunk, BlockPos pos)
	{
		ypos = Depth.nextFloat(1f, DepositRand);
		posyi = (int)ypos;
		int num = pos.X % 32;
		int num2 = pos.Z % 32;
		if (num >= 0 && num2 >= 0)
		{
			currentRelativeDepth = ypos / (float)(int)mapChunk.WorldGenTerrainHeightMap[num2 * 32 + num];
		}
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		float num = 0f;
		for (int i = 0; i < 100; i++)
		{
			num = Math.Max(num, Depth.nextFloat(1f, DepositRand));
		}
		miny = (float)pos.Y - num;
		maxy = (float)pos.Y;
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos targetPos, double distanceToEdge)
	{
		double num = (double)depoitThickness * GameMath.Clamp(distanceToEdge * 2.0 - 0.2, 0.0, 1.0);
		hereThickness = (int)num + ((DepositRand.NextDouble() < num - (double)(int)num) ? 1 : 0);
		targetPos.Y = heremapchunk.WorldGenTerrainHeightMap[lz * 32 + lx] - posyi;
	}
}
