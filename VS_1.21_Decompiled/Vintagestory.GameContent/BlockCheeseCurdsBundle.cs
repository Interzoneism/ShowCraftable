using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCheeseCurdsBundle : Block
{
	public Dictionary<string, MeshData> meshes = new Dictionary<string, MeshData>();

	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ItemStack[] stickStack = new ItemStack[1]
		{
			new ItemStack(api.World.GetItem(new AssetLocation("stick")))
		};
		ItemStack[] saltStack = new ItemStack[1]
		{
			new ItemStack(api.World.GetItem(new AssetLocation("salt")), 5)
		};
		interactions = new WorldInteraction[5]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-curdbundle-addstick",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = stickStack,
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
				{
					BECheeseCurdsBundle obj = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BECheeseCurdsBundle;
					return (obj == null || obj.State != EnumCurdsBundleState.Bundled) ? null : stickStack;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-curdbundle-squeeze",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = null,
				ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) => api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is BECheeseCurdsBundle { State: EnumCurdsBundleState.BundledStick } bECheeseCurdsBundle && !bECheeseCurdsBundle.Squuezed
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-curdbundle-open",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = null,
				ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) => api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is BECheeseCurdsBundle { State: EnumCurdsBundleState.BundledStick } bECheeseCurdsBundle && bECheeseCurdsBundle.Squuezed
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-curdbundle-addsalt",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = saltStack,
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
				{
					BECheeseCurdsBundle obj = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BECheeseCurdsBundle;
					return (obj == null || obj.State != EnumCurdsBundleState.Opened) ? null : saltStack;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-curdbundle-pickupcheese",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = null,
				ShouldApply = delegate(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
				{
					BECheeseCurdsBundle obj = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BECheeseCurdsBundle;
					return obj != null && obj.State == EnumCurdsBundleState.OpenedSalted;
				}
			}
		};
	}

	public Shape GetShape(EnumCurdsBundleState state)
	{
		string shapePath = "shapes/block/food/curdbundle-plain.json";
		switch (state)
		{
		case EnumCurdsBundleState.BundledStick:
			shapePath = "shapes/block/food/curdbundle-stick.json";
			break;
		case EnumCurdsBundleState.Opened:
			shapePath = "shapes/item/food/dairy/cheese/linen-raw.json";
			break;
		case EnumCurdsBundleState.OpenedSalted:
			shapePath = "shapes/item/food/dairy/cheese/linen-salted.json";
			break;
		}
		return Vintagestory.API.Common.Shape.TryGet(api, shapePath);
	}

	public MeshData GetMesh(EnumCurdsBundleState state, float angle)
	{
		int num = (int)state;
		string key = num + "-" + angle;
		if (!meshes.ContainsKey(key))
		{
			Shape shape = GetShape(state);
			(api as ICoreClientAPI).Tesselator.TesselateShape(this, shape, out var modeldata, new Vec3f(0f, angle * (180f / (float)Math.PI), 0f));
			meshes[key] = modeldata;
		}
		return meshes[key];
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECheeseCurdsBundle bECheeseCurdsBundle)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			float num3 = (float)Math.PI / 8f;
			float meshAngle = (float)(int)Math.Round(num2 / num3) * num3;
			bECheeseCurdsBundle.MeshAngle = meshAngle;
		}
		return num;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BECheeseCurdsBundle bECheeseCurdsBundle = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheeseCurdsBundle;
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (bECheeseCurdsBundle == null)
		{
			return false;
		}
		if (bECheeseCurdsBundle.State == EnumCurdsBundleState.Bundled)
		{
			if (activeHotbarSlot.Itemstack?.Collectible.Code.Path == "stick")
			{
				bECheeseCurdsBundle.State = EnumCurdsBundleState.BundledStick;
				activeHotbarSlot.TakeOut(1);
				activeHotbarSlot.MarkDirty();
			}
			return true;
		}
		if (bECheeseCurdsBundle.State == EnumCurdsBundleState.BundledStick && !bECheeseCurdsBundle.Squuezed)
		{
			bECheeseCurdsBundle.StartSqueeze(byPlayer);
			return true;
		}
		if (bECheeseCurdsBundle.Rotten || bECheeseCurdsBundle.Inventory.Empty)
		{
			bECheeseCurdsBundle.Inventory.DropAll(bECheeseCurdsBundle.Pos.ToVec3d().Add(0.5, 0.2, 0.5));
			api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("linen-normal-down")).Id, blockSel.Position);
			return true;
		}
		if (bECheeseCurdsBundle.State == EnumCurdsBundleState.BundledStick && bECheeseCurdsBundle.Squuezed)
		{
			bECheeseCurdsBundle.State = EnumCurdsBundleState.Opened;
			api.World.PlaySoundAt(Sounds.Place, blockSel.Position, -0.5, byPlayer);
			ItemStack itemStack = new ItemStack(api.World.GetItem(new AssetLocation("stick")));
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				api.World.SpawnItemEntity(itemStack, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
			}
			api.World.Logger.Audit("{0} Took 1x{1} at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, blockSel.Position);
			return true;
		}
		if (bECheeseCurdsBundle.State == EnumCurdsBundleState.Opened)
		{
			if (activeHotbarSlot.Itemstack?.Collectible.Code.Path == "salt" && activeHotbarSlot.StackSize >= 5)
			{
				bECheeseCurdsBundle.State = EnumCurdsBundleState.OpenedSalted;
				activeHotbarSlot.TakeOut(5);
				activeHotbarSlot.MarkDirty();
			}
			return true;
		}
		if (bECheeseCurdsBundle.State == EnumCurdsBundleState.OpenedSalted)
		{
			ItemStack itemStack2 = new ItemStack(api.World.GetItem(new AssetLocation("rawcheese-salted")));
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack2, slotNotifyEffect: true))
			{
				api.World.SpawnItemEntity(itemStack2, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
			}
			api.World.Logger.Audit("{0} Took 1x{1} at {2}.", byPlayer.PlayerName, itemStack2.Collectible.Code, blockSel.Position);
			api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("linen-normal-down")).Id, blockSel.Position);
			return true;
		}
		return true;
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
	}

	public void SetContents(ItemStack blockstack, ItemStack contents)
	{
		blockstack.Attributes.SetItemstack("contents", contents);
	}

	public ItemStack GetContents(ItemStack blockstack)
	{
		ItemStack itemstack = blockstack.Attributes.GetItemstack("contents");
		if (itemstack != null)
		{
			itemstack.ResolveBlockOrItem(api.World);
			return itemstack;
		}
		return itemstack;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}
}
