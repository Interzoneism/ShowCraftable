using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityMeal : BlockEntityContainer, IBlockEntityMealContainer
{
	internal InventoryGeneric inventory;

	internal BlockMeal ownBlock;

	private MeshData currentMesh;

	private bool wasRotten;

	private int tickCnt;

	InventoryBase IBlockEntityMealContainer.inventory => inventory;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "meal";

	public string RecipeCode { get; set; }

	public float QuantityServings { get; set; }

	public CookingRecipe FromRecipe => Api.GetCookingRecipe(RecipeCode);

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

	public BlockEntityMeal()
	{
		inventory = new InventoryGeneric(4, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockMeal;
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(Every100ms, 200);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Block is BlockMeal blockMeal)
		{
			ItemStack[] contents = blockMeal.GetContents(Api.World, byItemStack);
			for (int i = 0; i < contents.Length; i++)
			{
				Inventory[i].Itemstack = contents[i];
			}
			RecipeCode = blockMeal.GetRecipeCode(Api.World, byItemStack);
			QuantityServings = blockMeal.GetQuantityServings(Api.World, byItemStack);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void Every100ms(float dt)
	{
		float num = GetTemperature();
		if (Api.World.Rand.NextDouble() < (double)((num - 50f) / 320f))
		{
			BlockCookedContainer.smokeHeld.MinPos = Pos.ToVec3d().AddCopy(0.45, 0.125, 0.45);
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

	internal MeshData GenMesh()
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
		return (Api as ICoreClientAPI).ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(ownBlock, FromRecipe, nonEmptyContentStacks);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh == null)
		{
			currentMesh = GenMesh();
		}
		mesher.AddMeshData(currentMesh);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RecipeCode = tree.GetString("recipeCode");
		QuantityServings = (float)tree.GetDecimal("quantityServings");
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
		tree.SetString("recipeCode", (RecipeCode == null) ? "" : RecipeCode);
		tree.SetFloat("quantityServings", QuantityServings);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		CookingRecipe fromRecipe = FromRecipe;
		if (fromRecipe == null)
		{
			if (inventory.Count > 0 && !inventory[0].Empty)
			{
				dsc.AppendLine(inventory[0].StackSize + "x " + inventory[0].Itemstack.GetName());
			}
			return;
		}
		dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(QuantityServings, 1), fromRecipe.GetOutputName(forPlayer.Entity.World, GetNonEmptyContentStacks()).UcFirst()));
		if (ownBlock == null)
		{
			return;
		}
		int temperature = GetTemperature();
		string text = Lang.Get("{0}°C", temperature);
		if (temperature < 20)
		{
			text = Lang.Get("Cold");
		}
		dsc.AppendLine(Lang.Get("Temperature: {0}", text));
		string contentNutritionFacts = ownBlock.GetContentNutritionFacts(Api.World, inventory[0], GetNonEmptyContentStacks(cloned: false), forPlayer.Entity);
		if (contentNutritionFacts != null)
		{
			dsc.Append(contentNutritionFacts);
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
