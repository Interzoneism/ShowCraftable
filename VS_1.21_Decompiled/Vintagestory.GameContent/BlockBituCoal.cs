using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockBituCoal : BlockOre
{
	private Block clay;

	private static RockStrataConfig rockStrata;

	private static LCGRandom rand;

	private const int chunksize = 32;

	private static int regionChunkSize;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		clay = api.World.BlockAccessor.GetBlock(new AssetLocation("rawclay-fire-none"));
		if (rockStrata == null && api is ICoreServerAPI coreServerAPI)
		{
			regionChunkSize = coreServerAPI.WorldManager.RegionSize / 32;
			rockStrata = BlockLayerConfig.GetInstance(coreServerAPI).RockStrata;
			rand = new LCGRandom(api.World.Seed);
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		rockStrata = null;
		clay = null;
		rand = null;
	}

	public float GetDepositDistortTop(BlockPos pos, int lx, int lz, IMapChunk heremapchunk)
	{
		int num = pos.X / 32 % regionChunkSize;
		int num2 = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float num3 = (float)heremapchunk.MapRegion.OreMapVerticalDistortTop.InnerSize / (float)regionChunkSize;
		return mapRegion.OreMapVerticalDistortTop.GetIntLerpedCorrectly((float)num * num3 + num3 * ((float)lx / 32f), (float)num2 * num3 + num3 * ((float)lz / 32f));
	}

	public float GetDepositDistortBot(BlockPos pos, int lx, int lz, IMapChunk heremapchunk)
	{
		int num = pos.X / 32 % regionChunkSize;
		int num2 = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float num3 = (float)heremapchunk.MapRegion.OreMapVerticalDistortBottom.InnerSize / (float)regionChunkSize;
		return mapRegion.OreMapVerticalDistortBottom.GetIntLerpedCorrectly((float)num * num3 + num3 * ((float)lx / 32f), (float)num2 * num3 + num3 * ((float)lz / 32f));
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		IMapChunk mapChunk = blockAccessor.GetMapChunk(pos.X / 32, pos.Z / 32);
		int lx = pos.X % 32;
		int lz = pos.Z % 32;
		int num = (int)GetDepositDistortTop(pos, lx, lz, mapChunk) / 7;
		int num2 = (int)GetDepositDistortBot(pos, lx, lz, mapChunk) / 7;
		rand.InitPositionSeed(pos.X / 100 + num, pos.Z / 100 + num2);
		BlockPos pos2 = pos.DownCopy();
		Block block = blockAccessor.GetBlock(pos2);
		for (int i = 0; i < rockStrata.Variants.Length; i++)
		{
			if (rockStrata.Variants[i].RockGroup == EnumRockGroup.Sedimentary && rockStrata.Variants[i].BlockCode == block.Code)
			{
				if (rand.NextDouble() > 0.6)
				{
					blockAccessor.SetBlock(clay.BlockId, pos2);
				}
				break;
			}
		}
		blockAccessor.SetBlock(BlockId, pos);
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
	}
}
