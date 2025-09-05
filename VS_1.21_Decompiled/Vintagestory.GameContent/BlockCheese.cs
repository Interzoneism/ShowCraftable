using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCheese : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		InteractionHelpYOffset = 0.375f;
		interactions = ObjectCacheUtil.GetOrCreate(api, "cheeseInteractions-", () => new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-cheese-cut",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = BlockUtil.GetKnifeStacks(api),
				GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BECheese { SlicesLeft: >1 }) ? wi.Itemstacks : null
			}
		});
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		if (world.BlockAccessor.GetBlockEntity(pos) is BECheese bECheese)
		{
			Shape cachedShape = coreClientAPI.TesselatorManager.GetCachedShape(bECheese.Inventory[0].Itemstack.Item.Shape.Base);
			coreClientAPI.Tesselator.TesselateShape(this, cachedShape, out blockModelData);
			blockModelData.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.75f, 0.75f, 0.75f);
			coreClientAPI.Tesselator.TesselateShape("cheese decal", cachedShape, out decalModelData, decalTexSource, null, 0, 0, 0);
			decalModelData.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.75f, 0.75f, 0.75f);
		}
		base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BECheese bECheese)
		{
			return bECheese.Inventory[0].Itemstack;
		}
		return base.OnPickBlock(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		EnumTool? enumTool = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible.Tool;
		if (enumTool == EnumTool.Knife || enumTool == EnumTool.Sword)
		{
			BECheese bECheese = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheese;
			if (bECheese.Inventory[0].Itemstack?.Collectible.Variant["type"] == "waxedcheddar")
			{
				ItemStack itemStack = new ItemStack(api.World.GetItem(bECheese.Inventory[0].Itemstack?.Collectible.CodeWithVariant("type", "cheddar")));
				TransitionableProperties transitionableProperties = itemStack.Collectible.GetTransitionableProperties(api.World, itemStack, null).FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
				transitionableProperties.TransitionedStack.Resolve(api.World, "pie perished stack");
				CollectibleObject.CarryOverFreshness(api, bECheese.Inventory[0], itemStack, transitionableProperties);
				bECheese.Inventory[0].Itemstack = itemStack;
				bECheese.Inventory[0].MarkDirty();
				bECheese.MarkDirty(redrawOnClient: true);
				return true;
			}
			ItemStack itemStack2 = bECheese?.TakeSlice();
			if (itemStack2 != null)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack2, slotNotifyEffect: true))
				{
					world.SpawnItemEntity(itemStack2, blockSel.Position);
				}
				world.Logger.Audit("{0} Took 1x{1} from Cheese at {2}.", byPlayer.PlayerName, itemStack2.Collectible.Code, blockSel.Position);
			}
			return true;
		}
		ItemStack itemstack = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheese).Inventory[0].Itemstack;
		if (itemstack != null)
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
			{
				world.SpawnItemEntity(itemstack, blockSel.Position);
			}
			world.Logger.Audit("{0} Took 1x{1} from Cheese at {2}.", byPlayer.PlayerName, itemstack.Collectible.Code, blockSel.Position);
		}
		world.BlockAccessor.SetBlock(0, blockSel.Position);
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
