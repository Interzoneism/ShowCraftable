using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityCookedContainer : BlockEntityContainer, IBlockEntityMealContainer
{
	internal InventoryGeneric inventory;

	internal BlockCookedContainer? ownBlock;

	private MeshData? currentMesh;

	private bool wasRotten;

	private int tickCnt;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "cookedcontainer";

	public float QuantityServings { get; set; }

	public string? RecipeCode { get; set; }

	InventoryBase IBlockEntityMealContainer.inventory => inventory;

	public bool Rotten
	{
		get
		{
			bool flag = false;
			for (int i = 0; i < inventory.Count; i++)
			{
				flag |= inventory[i].Itemstack?.Collectible.Code.Path == "rot";
			}
			return flag;
		}
	}

	public CookingRecipe? FromRecipe => Api.GetCookingRecipe(RecipeCode);

	public BlockEntityCookedContainer()
	{
		inventory = new InventoryGeneric(4, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockCookedContainer;
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(Every100ms, 200);
		}
	}

	private void Every100ms(float dt)
	{
		float num = GetTemperature();
		if (Api.World.Rand.NextDouble() < (double)((num - 50f) / 160f))
		{
			BlockCookedContainer.smokeHeld.MinPos = Pos.ToVec3d().AddCopy(0.45, 0.3125, 0.45);
			Api.World.SpawnParticles(BlockCookedContainer.smokeHeld);
		}
		if (tickCnt++ % 20 == 0 && !wasRotten && Rotten)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
			wasRotten = true;
		}
	}

	private int GetTemperature()
	{
		ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks(cloned: false);
		if (nonEmptyContentStacks.Length == 0 || nonEmptyContentStacks[0] == null)
		{
			return 0;
		}
		return (int)nonEmptyContentStacks[0].Collectible.GetTemperature(Api.World, nonEmptyContentStacks[0]);
	}

	public override void OnBlockPlaced(ItemStack? byItemStack = null)
	{
		if (byItemStack?.Block is BlockCookedContainer blockCookedContainer)
		{
			TreeAttribute treeAttribute = byItemStack?.Attributes?["temperature"] as TreeAttribute;
			ItemStack[] nonEmptyContents = blockCookedContainer.GetNonEmptyContents(Api.World, byItemStack);
			if (nonEmptyContents != null)
			{
				for (int i = 0; i < nonEmptyContents.Length; i++)
				{
					ItemStack itemStack = nonEmptyContents[i].Clone();
					Inventory[i].Itemstack = itemStack;
					if (treeAttribute != null)
					{
						itemStack.Attributes["temperature"] = treeAttribute.Clone();
					}
				}
			}
			RecipeCode = blockCookedContainer.GetRecipeCode(Api.World, byItemStack);
			QuantityServings = blockCookedContainer.GetServings(Api.World, byItemStack);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockBroken(IPlayer? byPlayer = null)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		QuantityServings = (float)tree.GetDecimal("quantityServings", 1.0);
		RecipeCode = tree.GetString("recipeCode");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && currentMesh == null)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("quantityServings", QuantityServings);
		tree.SetString("recipeCode", (RecipeCode == null) ? "" : RecipeCode);
	}

	public bool ServeInto(IPlayer player, ItemSlot slot)
	{
		if (slot.Itemstack == null)
		{
			return false;
		}
		int num = slot.Itemstack.Collectible.Attributes["servingCapacity"].AsInt();
		float num2 = Math.Min(QuantityServings, num);
		ItemStack itemStack;
		if (slot.Itemstack.Collectible is IBlockMealContainer blockMealContainer && blockMealContainer.GetQuantityServings(Api.World, slot.Itemstack) > 0f)
		{
			float quantityServings = blockMealContainer.GetQuantityServings(Api.World, slot.Itemstack);
			ItemStack[] nonEmptyContents = blockMealContainer.GetNonEmptyContents(Api.World, slot.Itemstack);
			num2 = Math.Min(num2, (float)num - quantityServings);
			ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks();
			if (num2 == 0f)
			{
				return false;
			}
			if (nonEmptyContents.Length != nonEmptyContentStacks.Length)
			{
				return false;
			}
			for (int i = 0; i < nonEmptyContents.Length; i++)
			{
				if (!nonEmptyContents[i].Equals(Api.World, nonEmptyContentStacks[i], GlobalConstants.IgnoredStackAttributes))
				{
					return false;
				}
			}
			if (slot.StackSize == 1)
			{
				itemStack = slot.Itemstack;
				blockMealContainer.SetContents(RecipeCode, slot.Itemstack, GetNonEmptyContentStacks(), quantityServings + num2);
			}
			else
			{
				itemStack = slot.Itemstack.Clone();
				blockMealContainer.SetContents(RecipeCode, itemStack, GetNonEmptyContentStacks(), quantityServings + num2);
			}
		}
		else
		{
			itemStack = new ItemStack(Api.World.GetBlock(AssetLocation.Create(slot.Itemstack.Collectible.Attributes["mealBlockCode"].AsString(), slot.Itemstack.Collectible.Code.Domain)));
			itemStack.StackSize = 1;
			(itemStack.Collectible as IBlockMealContainer)?.SetContents(RecipeCode, itemStack, GetNonEmptyContentStacks(), num2);
		}
		if (slot.StackSize == 1)
		{
			slot.Itemstack = itemStack;
			slot.MarkDirty();
		}
		else
		{
			slot.TakeOut(1);
			if (!player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} Meal from {2} at {3}.", player.PlayerName, itemStack.Collectible.Code, base.Block.Code, Pos);
			slot.MarkDirty();
		}
		QuantityServings -= num2;
		if (QuantityServings <= 0f)
		{
			AssetLocation blockCode = AssetLocation.CreateOrNull(base.Block.Attributes?["emptiedBlockCode"]?.AsString()) ?? base.Block.CodeWithVariant("type", "fired");
			Block block = Api.World.GetBlock(blockCode);
			Api.World.BlockAccessor.SetBlock(block?.BlockId ?? 0, Pos);
			return true;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
		}
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public MeshData? GenMesh()
	{
		if (ownBlock == null)
		{
			return null;
		}
		ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks();
		if (nonEmptyContentStacks == null || nonEmptyContentStacks.Length == 0)
		{
			return null;
		}
		return (Api as ICoreClientAPI)?.ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(ownBlock, FromRecipe, nonEmptyContentStacks, new Vec3f(0f, 5f / 32f, 0f));
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh == null)
		{
			currentMesh = GenMesh();
		}
		if (currentMesh != null)
		{
			mesher.AddMeshData(currentMesh);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		CookingRecipe cookingRecipe = Api.GetCookingRecipe(RecipeCode);
		if (cookingRecipe == null)
		{
			return;
		}
		ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks();
		float quantityServings = QuantityServings;
		int temperature = GetTemperature();
		string text = Lang.Get("{0}°C", temperature);
		if (temperature < 20)
		{
			text = Lang.Get("Cold");
		}
		BlockMeal[]? array = BlockMeal.AllMealBowls(Api);
		string text2 = ((array == null) ? null : array[0]?.GetContentNutritionFacts(Api.World, inventory[0], nonEmptyContentStacks, forPlayer.Entity));
		if (quantityServings == 1f)
		{
			dsc.Append(Lang.Get("cookedcontainer-servingstemp-singular", Math.Round(quantityServings, 1), cookingRecipe.GetOutputName(forPlayer.Entity.World, nonEmptyContentStacks), text, (text2 != null) ? "\n" : "", text2));
		}
		else
		{
			dsc.Append(Lang.Get("cookedcontainer-servingstemp-plural", Math.Round(quantityServings, 1), cookingRecipe.GetOutputName(forPlayer.Entity.World, nonEmptyContentStacks), text, (text2 != null) ? "\n" : "", text2));
		}
		foreach (ItemSlot item in inventory)
		{
			if (!item.Empty)
			{
				TransitionableProperties[] transitionableProperties = item.Itemstack.Collectible.GetTransitionableProperties(Api.World, item.Itemstack, null);
				if (transitionableProperties != null && transitionableProperties.Length != 0)
				{
					item.Itemstack.Collectible.AppendPerishableInfoText(item, dsc, Api.World);
					break;
				}
			}
		}
	}
}
