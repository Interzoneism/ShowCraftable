using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class BlockLayerConfig
{
	public float blockLayerTransitionSize;

	public BlockLayer[] Blocklayers;

	public TallGrassProperties Tallgrass;

	public SnowLayerProperties SnowLayer;

	public BeachLayerProperties BeachLayer;

	public LakeBedLayerProperties LakeBedLayer;

	public LakeBedLayerProperties OceanBedLayer;

	public RockStrataConfig RockStrata;

	public static readonly string cacheKey = "BlockLayerConfig";

	public static BlockLayerConfig GetInstance(ICoreServerAPI api)
	{
		if (api.ObjectCache.TryGetValue(cacheKey, out var value))
		{
			return value as BlockLayerConfig;
		}
		IAsset asset = api.Assets.Get("worldgen/blocklayers.json");
		BlockLayerConfig blockLayerConfig = asset.ToObject<BlockLayerConfig>();
		asset = api.Assets.Get("worldgen/rockstrata.json");
		blockLayerConfig.RockStrata = asset.ToObject<RockStrataConfig>();
		blockLayerConfig.ResolveBlockIds(api);
		api.ObjectCache[cacheKey] = blockLayerConfig;
		return blockLayerConfig;
	}

	public BlockLayer GetBlockLayerById(IWorldAccessor world, string blockLayerId)
	{
		BlockLayer[] blocklayers = Blocklayers;
		foreach (BlockLayer blockLayer in blocklayers)
		{
			if (blockLayerId.Equals(blockLayer.ID))
			{
				return blockLayer;
			}
		}
		return null;
	}

	public void ResolveBlockIds(ICoreServerAPI api)
	{
		for (int i = 0; i < Blocklayers.Length; i++)
		{
			Random rnd = new Random(api.WorldManager.Seed + i);
			Blocklayers[i].Init(api, RockStrata, rnd);
		}
		SnowLayer.BlockId = api.WorldManager.GetBlockId(SnowLayer.BlockCode);
		for (int j = 0; j < Tallgrass.BlockCodeByMin.Length; j++)
		{
			Tallgrass.BlockCodeByMin[j].BlockId = api.WorldManager.GetBlockId(Tallgrass.BlockCodeByMin[j].BlockCode);
		}
		for (int k = 0; k < LakeBedLayer.BlockCodeByMin.Length; k++)
		{
			Random rnd2 = new Random(api.WorldManager.Seed + k);
			LakeBedLayer.BlockCodeByMin[k].Init(api, RockStrata, rnd2);
		}
		for (int l = 0; l < OceanBedLayer.BlockCodeByMin.Length; l++)
		{
			Random rnd3 = new Random(api.WorldManager.Seed + l);
			OceanBedLayer.BlockCodeByMin[l].Init(api, RockStrata, rnd3);
		}
		BeachLayer.ResolveBlockIds(api, RockStrata);
	}
}
