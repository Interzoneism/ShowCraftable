using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

internal class TreeGeneratorsUtil
{
	private ICoreServerAPI sapi;

	public ForestFloorSystem forestFloorSystem;

	public TreeGeneratorsUtil(ICoreServerAPI api)
	{
		sapi = api;
		forestFloorSystem = new ForestFloorSystem(api);
	}

	public void ReloadTreeGenerators()
	{
		int num = sapi.Assets.Reload(new AssetLocation("worldgen/treegen"));
		sapi.Server.LogNotification("{0} tree generators reloaded", num);
		LoadTreeGenerators();
	}

	public void LoadTreeGenerators()
	{
		Dictionary<AssetLocation, TreeGenConfig> many = sapi.Assets.GetMany<TreeGenConfig>(sapi.Server.Logger, "worldgen/treegen");
		WoodWorldProperty woodWorldProperty = sapi.Assets.Get<WoodWorldProperty>(new AssetLocation("worldproperties/block/wood.json"));
		Dictionary<string, EnumTreeType> dictionary = new Dictionary<string, EnumTreeType>();
		WorldWoodPropertyVariant[] variants = woodWorldProperty.Variants;
		foreach (WorldWoodPropertyVariant worldWoodPropertyVariant in variants)
		{
			dictionary[worldWoodPropertyVariant.Code.Path] = worldWoodPropertyVariant.TreeType;
		}
		bool flag = sapi.World.Config.GetAsString("potatoeMode", "false").ToBool();
		string text = "";
		foreach (KeyValuePair<AssetLocation, TreeGenConfig> item in many)
		{
			AssetLocation assetLocation = item.Key.Clone();
			if (text.Length > 0)
			{
				text += ", ";
			}
			text += assetLocation;
			assetLocation.Path = item.Key.Path.Substring("worldgen/treegen/".Length);
			assetLocation.RemoveEnding();
			if (flag)
			{
				item.Value.treeBlocks.mossDecorCode = null;
			}
			item.Value.Init(item.Key, sapi.Server.Logger);
			sapi.RegisterTreeGenerator(assetLocation, new TreeGen(item.Value, sapi.WorldManager.Seed, forestFloorSystem));
			item.Value.treeBlocks.ResolveBlockNames(sapi, assetLocation.Path);
			dictionary.TryGetValue(sapi.World.GetBlock(item.Value.treeBlocks.logBlockId).Variant["wood"], out item.Value.Treetype);
		}
		sapi.Server.LogNotification("Reloaded {0} tree generators", many.Count);
	}

	public ITreeGenerator GetGenerator(AssetLocation generatorCode)
	{
		sapi.World.TreeGenerators.TryGetValue(generatorCode, out var value);
		return value;
	}

	public KeyValuePair<AssetLocation, ITreeGenerator> GetGenerator(int index)
	{
		AssetLocation keyAtIndex = sapi.World.TreeGenerators.GetKeyAtIndex(index);
		if (keyAtIndex != null)
		{
			return new KeyValuePair<AssetLocation, ITreeGenerator>(keyAtIndex, sapi.World.TreeGenerators[keyAtIndex]);
		}
		return new KeyValuePair<AssetLocation, ITreeGenerator>(null, null);
	}

	public void RunGenerator(AssetLocation treeName, IBlockAccessor api, BlockPos pos, TreeGenParams treeGenParams)
	{
		sapi.World.TreeGenerators[treeName].GrowTree(api, pos, treeGenParams, new NormalRandom());
	}
}
