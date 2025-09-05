using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class DepositBlock
{
	public AssetLocation Code;

	public string Name;

	public string[] AllowedVariants;

	public Dictionary<AssetLocation, string[]> AllowedVariantsByInBlock;

	public int MaxGrade;

	public bool IsWildCard => Code.Path.Contains('*');

	internal ResolvedDepositBlock Resolve(string fileForLogging, ICoreServerAPI api, Block inblock, string key, string value)
	{
		AssetLocation assetLocation = Code.Clone();
		assetLocation.Path = assetLocation.Path.Replace("{" + key + "}", value);
		Block[] array = api.World.SearchBlocks(assetLocation);
		if (array.Length == 0)
		{
			api.World.Logger.Warning("Deposit {0}: No block with code/wildcard '{1}' was found (unresolved code: {2})", fileForLogging, assetLocation, Code);
		}
		if (AllowedVariants != null)
		{
			List<Block> list = new List<Block>();
			for (int i = 0; i < array.Length; i++)
			{
				if (WildcardUtil.Match(assetLocation, array[i].Code, AllowedVariants))
				{
					list.Add(array[i]);
				}
			}
			if (list.Count == 0)
			{
				api.World.Logger.Warning("Deposit {0}: AllowedVariants for {1} does not match any block! Please fix", fileForLogging, assetLocation);
			}
			array = list.ToArray();
			MaxGrade = AllowedVariants.Length;
		}
		if (AllowedVariantsByInBlock != null)
		{
			List<Block> list2 = new List<Block>();
			for (int j = 0; j < array.Length; j++)
			{
				if (AllowedVariantsByInBlock[inblock.Code].Contains(WildcardUtil.GetWildcardValue(assetLocation, array[j].Code)))
				{
					list2.Add(array[j]);
				}
			}
			foreach (KeyValuePair<AssetLocation, string[]> item in AllowedVariantsByInBlock)
			{
				MaxGrade = Math.Max(MaxGrade, item.Value.Length);
			}
			if (list2.Count == 0)
			{
				api.World.Logger.Warning("Deposit {0}: AllowedVariantsByInBlock for {1} does not match any block! Please fix", fileForLogging, assetLocation);
			}
			array = list2.ToArray();
		}
		return new ResolvedDepositBlock
		{
			Blocks = array
		};
	}
}
