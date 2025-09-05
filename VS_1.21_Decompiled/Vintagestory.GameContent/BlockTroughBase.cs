using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTroughBase : Block
{
	public ContentConfig[] contentConfigs;

	public WorldInteraction[] placeInteractionHelp;

	public Vec3i RootOffset = new Vec3i(0, 0, 0);

	protected string[] unsuitableEntityCodesBeginsWith = Array.Empty<string>();

	protected string[] unsuitableEntityCodesExact;

	protected string unsuitableEntityFirstLetters = "";

	public void init()
	{
		CanStep = false;
		contentConfigs = ObjectCacheUtil.GetOrCreate(api, "troughContentConfigs-" + Code, delegate
		{
			ContentConfig[] array3 = Attributes?["contentConfig"]?.AsObject<ContentConfig[]>();
			if (array3 == null)
			{
				return (ContentConfig[])null;
			}
			ContentConfig[] array4 = array3;
			foreach (ContentConfig contentConfig in array4)
			{
				if (!contentConfig.Content.Code.Path.Contains('*'))
				{
					contentConfig.Content.Resolve(api.World, "troughcontentconfig");
				}
			}
			return array3;
		});
		List<ItemStack> list = new List<ItemStack>();
		ContentConfig[] array = contentConfigs;
		foreach (ContentConfig val in array)
		{
			if (val.Content.Code.Path.Contains('*'))
			{
				if (val.Content.Type == EnumItemClass.Block)
				{
					list.AddRange(from block in api.World.SearchBlocks(val.Content.Code)
						select new ItemStack(block, val.QuantityPerFillLevel));
				}
				else
				{
					list.AddRange(from item in api.World.SearchItems(val.Content.Code)
						select new ItemStack(item, val.QuantityPerFillLevel));
				}
			}
			else if (val.Content.ResolvedItemstack != null)
			{
				ItemStack itemStack = val.Content.ResolvedItemstack.Clone();
				itemStack.StackSize = val.QuantityPerFillLevel;
				list.Add(itemStack);
			}
		}
		placeInteractionHelp = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-trough-addfeed",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = list.ToArray(),
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (!(api.World.BlockAccessor.GetBlockEntity(bs.Position.AddCopy(RootOffset)) is BlockEntityTrough { IsFull: false } blockEntityTrough))
					{
						return (ItemStack[])null;
					}
					ItemStack[] stacks = blockEntityTrough.GetNonEmptyContentStacks();
					return (stacks != null && stacks.Length != 0) ? wi.Itemstacks.Where((ItemStack stack) => stack.Equals(api.World, stacks[0], GlobalConstants.IgnoredStackAttributes)).ToArray() : wi.Itemstacks;
				}
			}
		};
		string[] array2 = Attributes?["unsuitableFor"].AsArray(Array.Empty<string>());
		if (array2.Length != 0)
		{
			AiTaskBaseTargetable.InitializeTargetCodes(array2, ref unsuitableEntityCodesExact, ref unsuitableEntityCodesBeginsWith, ref unsuitableEntityFirstLetters);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return placeInteractionHelp.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public virtual bool UnsuitableForEntity(string testPath)
	{
		if (unsuitableEntityFirstLetters.IndexOf(testPath[0]) < 0)
		{
			return false;
		}
		for (int i = 0; i < unsuitableEntityCodesExact.Length; i++)
		{
			if (testPath == unsuitableEntityCodesExact[i])
			{
				return true;
			}
		}
		for (int j = 0; j < unsuitableEntityCodesBeginsWith.Length; j++)
		{
			if (testPath.StartsWithFast(unsuitableEntityCodesBeginsWith[j]))
			{
				return true;
			}
		}
		return false;
	}
}
