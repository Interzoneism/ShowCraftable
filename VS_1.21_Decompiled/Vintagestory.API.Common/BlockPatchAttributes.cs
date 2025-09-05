using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class BlockPatchAttributes
{
	public string[]? CoralBase;

	public string[]? CoralStructure;

	public string[]? CoralShelve;

	public string[]? CoralDecor;

	public string[]? StructureDecor;

	public string[]? Coral;

	public int CoralMinSize = -1;

	public int CoralRandomSize = -1;

	public float CoralVerticalGrowChance = -1f;

	public float CoralPlantsChance = -1f;

	public Dictionary<string, CoralPlantConfig>? CoralPlants;

	public float CoralShelveChance = -1f;

	public float CoralReplaceOtherPatches = -1f;

	public float CoralStructureChance = -1f;

	public float CoralDecorChance = -1f;

	public float CoralChance = 0.5f;

	public int CoralBaseHeight;

	public float FlowerChance = -1f;

	public NatFloat? Height;

	[JsonIgnore]
	public Block[]? CoralBaseBlock;

	[JsonIgnore]
	public Block[]? CoralStructureBlock;

	[JsonIgnore]
	public Block[][]? CoralShelveBlock;

	[JsonIgnore]
	public Block[]? CoralBlock;

	[JsonIgnore]
	public Block[]? CoralDecorBlock;

	[JsonIgnore]
	public Block[]? StructureDecorBlock;

	public void Init(ICoreServerAPI sapi, int i)
	{
		List<Block> list = new List<Block>();
		string[] coralBase;
		if (CoralBase != null)
		{
			coralBase = CoralBase;
			foreach (string text in coralBase)
			{
				Block[] array = sapi.World.SearchBlocks(new AssetLocation(text));
				if (array != null)
				{
					list.AddRange(array);
					continue;
				}
				sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralBaseBlocks block with code {1}. Will ignore.", i, text);
			}
			CoralBaseBlock = list.ToArray();
			list.Clear();
		}
		if (CoralStructure != null)
		{
			coralBase = CoralStructure;
			foreach (string text2 in coralBase)
			{
				Block[] array2 = sapi.World.SearchBlocks(new AssetLocation(text2));
				if (array2 != null)
				{
					list.AddRange(array2);
					continue;
				}
				sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralStructure block with code {1}. Will ignore.", i, text2);
			}
			CoralStructureBlock = list.ToArray();
			list.Clear();
		}
		if (CoralShelve != null)
		{
			List<Block[]> list2 = new List<Block[]>();
			coralBase = CoralShelve;
			foreach (string text3 in coralBase)
			{
				Block[] array3 = sapi.World.SearchBlocks(new AssetLocation(text3));
				if (array3 != null)
				{
					List<Block[]> list3 = new List<Block[]>();
					Block[] array4 = array3;
					foreach (Block block in array4)
					{
						string codeWithoutParts = block.CodeWithoutParts(1);
						if (!list2.Any((Block[] c) => c[0].Code.Path.Equals(codeWithoutParts + "-north")))
						{
							Block[] item = new Block[4]
							{
								sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-north")),
								sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-east")),
								sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-south")),
								sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-west"))
							};
							list3.Add(item);
						}
					}
					list2.AddRange(list3);
				}
				else
				{
					sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralShelve block with code {1}. Will ignore.", i, text3);
				}
			}
			CoralShelveBlock = list2.ToArray();
		}
		if (Coral != null)
		{
			coralBase = Coral;
			foreach (string text4 in coralBase)
			{
				Block[] array5 = sapi.World.SearchBlocks(new AssetLocation(text4));
				if (array5 != null)
				{
					list.AddRange(array5);
					continue;
				}
				sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve Coral block with code {1}. Will ignore.", i, text4);
			}
			CoralBlock = list.ToArray();
			list.Clear();
		}
		if (CoralPlants != null)
		{
			foreach (KeyValuePair<string, CoralPlantConfig> coralPlant in CoralPlants)
			{
				coralPlant.Deconstruct(out var key, out var value);
				string text5 = key;
				CoralPlantConfig coralPlantConfig = value;
				Block[] array6 = sapi.World.SearchBlocks(new AssetLocation(text5));
				if (array6 != null)
				{
					coralPlantConfig.Block = array6;
					continue;
				}
				sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralPlants block with code {1}. Will ignore.", i, text5);
			}
		}
		if (CoralDecor != null)
		{
			coralBase = CoralDecor;
			foreach (string text6 in coralBase)
			{
				Block[] array7 = sapi.World.SearchBlocks(new AssetLocation(text6));
				if (array7 != null)
				{
					list.AddRange(array7);
					continue;
				}
				sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralDecor block with code {1}. Will ignore.", i, text6);
			}
			CoralDecorBlock = list.ToArray();
			list.Clear();
		}
		if (StructureDecor == null)
		{
			return;
		}
		coralBase = StructureDecor;
		foreach (string text7 in coralBase)
		{
			Block[] array8 = sapi.World.SearchBlocks(new AssetLocation(text7));
			if (array8 != null)
			{
				list.AddRange(array8);
				continue;
			}
			sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve StructureDecor block with code {1}. Will ignore.", i, text7);
		}
		StructureDecorBlock = list.ToArray();
		list.Clear();
	}
}
