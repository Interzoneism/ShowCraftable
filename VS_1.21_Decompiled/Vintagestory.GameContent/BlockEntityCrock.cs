using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCrock : BlockEntityContainer, IBlockEntityMealContainer
{
	private InventoryGeneric inv;

	private MeshData currentMesh;

	private BlockCrock ownBlock;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "crock";

	public string RecipeCode { get; set; } = "";

	public InventoryBase inventory => inv;

	public float QuantityServings { get; set; }

	public bool Sealed { get; set; }

	public BlockEntityCrock()
	{
		inv = new InventoryGeneric(6, "crock-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockCrock;
		inv.OnAcquireTransitionSpeed += Inv_OnAcquireTransitionSpeed;
	}

	private float Inv_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float mulByConfig)
	{
		return mulByConfig * (ownBlock?.GetContainingTransitionModifierPlaced(Api.World, Pos, transType) ?? 1f);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			RecipeCode = byItemStack.Attributes.GetString("recipeCode", "");
			QuantityServings = (float)byItemStack.Attributes.GetDecimal("quantityServings");
			Sealed = byItemStack.Attributes.GetBool("sealed");
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	private MeshData getMesh(ITesselatorAPI tesselator)
	{
		if (!(Api.World.BlockAccessor.GetBlock(Pos) is BlockCrock blockCrock))
		{
			return null;
		}
		ItemStack[] stacks = (from slot in inventory
			where !slot.Empty
			select slot.Itemstack).ToArray();
		Vec3f rot = new Vec3f(0f, blockCrock.Shape.rotateY, 0f);
		return GetMesh(tesselator, Api, blockCrock, stacks, RecipeCode, rot);
	}

	public static MeshData GetMesh(ITesselatorAPI tesselator, ICoreAPI api, BlockCrock block, ItemStack[] stacks, string recipeCode, Vec3f rot)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(api, "blockCrockMeshes", () => new Dictionary<string, MeshData>());
		AssetLocation assetLocation = block.LabelForContents(recipeCode, stacks);
		if (assetLocation == null)
		{
			return null;
		}
		string key = assetLocation.ToShortString() + block.Code.ToShortString() + "/" + rot.Y + "/" + rot.X + "/" + rot.Z;
		if (orCreate.TryGetValue(key, out var value))
		{
			return value;
		}
		return orCreate[key] = block.GenMesh(api as ICoreClientAPI, assetLocation, rot);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		QuantityServings = (float)tree.GetDecimal("quantityServings");
		RecipeCode = tree.GetString("recipeCode", "");
		Sealed = tree.GetBool("sealed");
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			currentMesh = null;
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("quantityServings", QuantityServings);
		tree.SetBool("sealed", Sealed);
		if (RecipeCode != null && RecipeCode != "")
		{
			tree.SetString("recipeCode", RecipeCode);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh == null)
		{
			currentMesh = getMesh(tesselator);
			if (Sealed && base.Block is BlockCrock blockCrock)
			{
				MeshData meshData = blockCrock.GenSealMesh(Api as ICoreClientAPI);
				if (meshData != null)
				{
					currentMesh.AddMeshData(meshData);
				}
			}
			if (currentMesh == null)
			{
				return false;
			}
		}
		mesher.AddMeshData(currentMesh);
		return true;
	}

	public void ServeInto(IPlayer player, ItemSlot slot)
	{
		float num = Math.Min(QuantityServings, slot.Itemstack.Collectible.Attributes["servingCapacity"].AsInt());
		if (inv[0].Empty && inv[1].Empty && inv[2].Empty && inv[3].Empty)
		{
			return;
		}
		Block block = Api.World.GetBlock(AssetLocation.Create(slot.Itemstack.Collectible.Attributes["mealBlockCode"].AsString(), slot.Itemstack.Collectible.Code.Domain));
		ItemStack itemStack = new ItemStack(block)
		{
			StackSize = 1
		};
		(block as IBlockMealContainer).SetContents(RecipeCode, itemStack, GetNonEmptyContentStacks(), num);
		if (slot.StackSize == 1)
		{
			slot.Itemstack = itemStack;
		}
		else
		{
			slot.TakeOut(1);
			if (!player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			slot.MarkDirty();
		}
		QuantityServings -= num;
		if (QuantityServings <= 0f)
		{
			QuantityServings = 0f;
			inventory.DiscardAll();
			RecipeCode = "";
		}
		Sealed = false;
		currentMesh = null;
		MarkDirty(redrawOnClient: true);
	}
}
