using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockShelf : Block
{
	private WorldInteraction[]? interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactions = ObjectCacheUtil.GetOrCreate(api, "shelfInteractions", delegate
		{
			List<ItemStack> usableItemStacklist = new List<ItemStack>();
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				bool flag = collectible != null && collectible.Attributes?["mealContainer"]?.AsBool() == true;
				if (!flag)
				{
					bool flag2 = ((collectible is IContainedInteractable || collectible is IBlockMealContainer) ? true : false);
					flag = flag2;
				}
				if (flag || (collectible != null && collectible.Attributes?["canSealCrock"]?.AsBool() == true))
				{
					usableItemStacklist.Add(new ItemStack(collectible));
				}
				if (BlockEntityShelf.GetShelvableLayout(new ItemStack(collectible)).HasValue)
				{
					if (collectible is BlockPie blockPie)
					{
						ItemStack itemStack = new ItemStack(collectible);
						itemStack.Attributes.SetInt("pieSize", 4);
						itemStack.Attributes.SetString("topCrustType", "square");
						ITreeAttribute attributes = itemStack.Attributes;
						attributes.SetInt("bakeLevel", blockPie.Variant["state"] switch
						{
							"raw" => 0, 
							"partbaked" => 1, 
							"perfect" => 2, 
							"charred" => 3, 
							_ => 0, 
						});
						ItemStack itemStack2 = new ItemStack(api.World.GetItem("dough-spelt"), 2);
						ItemStack itemStack3 = new ItemStack(api.World.GetItem("fruit-redapple"), 2);
						blockPie.SetContents(itemStack, new ItemStack[6] { itemStack2, itemStack3, itemStack3, itemStack3, itemStack3, itemStack2 });
						itemStack.Attributes.SetFloat("quantityServings", 1f);
						list.Add(itemStack);
					}
					else
					{
						list.Add(new ItemStack(collectible));
					}
				}
			}
			ItemStack[] itemstacks = list.ToArray();
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-shelf-use",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = itemstacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityShelf beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityShelf;
						return usableItemStacklist.Where((ItemStack stack) => beshelf?.CanUse(stack, bs) ?? false)?.ToArray();
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-shelf-place",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = itemstacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityShelf beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityShelf;
						bool canTake;
						return usableItemStacklist.All(delegate(ItemStack stack)
						{
							BlockEntityShelf blockEntityShelf = beshelf;
							return blockEntityShelf != null && !blockEntityShelf.CanUse(stack, bs);
						}) ? usableItemStacklist.Where((ItemStack stack) => beshelf?.CanPlace(stack, bs, out canTake) ?? false).ToArray() : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-shelf-place",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = itemstacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityShelf beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityShelf;
						bool canTake;
						return usableItemStacklist.Any((ItemStack stack) => beshelf?.CanUse(stack, bs) ?? false) ? usableItemStacklist.Where((ItemStack stack) => beshelf?.CanPlace(stack, bs, out canTake) ?? false).ToArray() : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-shelf-take",
					MouseButton = EnumMouseButton.Right,
					RequireFreeHand = true,
					ShouldApply = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityShelf obj = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityShelf;
						bool canTake = false;
						obj?.CanPlace(null, bs, out canTake);
						return canTake;
					}
				}
			};
		});
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityShelf blockEntityShelf)
		{
			return blockEntityShelf.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		WorldInteraction[] placedBlockInteractionHelp = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		if (world.Claims.TestAccess(forPlayer, selection.Position, EnumBlockAccessFlags.Use) == EnumWorldAccessResponse.Granted)
		{
			placedBlockInteractionHelp.Append<WorldInteraction>(interactions);
		}
		return placedBlockInteractionHelp;
	}
}
