using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemMetalPlate : Item, IAnvilWorkable
{
	public string GetMetalType()
	{
		return LastCodePart();
	}

	public int GetRequiredAnvilTier(ItemStack stack)
	{
		string key = Variant["metal"];
		int num = 0;
		if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(key, out var value))
		{
			num = value.Tier - 1;
		}
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["requiresAnvilTier"].Exists)
		{
			num = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(num);
		}
		return num;
	}

	public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
	{
		ItemStack basemat = new ItemStack(api.World.GetItem(new AssetLocation("ingot-" + Variant["metal"])));
		return (from r in api.GetSmithingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(basemat)
			where r.Output.ResolvedItemstack.Collectible.Code != stack.Collectible.Code
			orderby r.Output.ResolvedItemstack.Collectible.Code
			select r).ToList();
	}

	public bool CanWork(ItemStack stack)
	{
		float temperature = stack.Collectible.GetTemperature(api.World, stack);
		float meltingPoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["workableTemperature"].Exists)
		{
			return stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2f) <= temperature;
		}
		return temperature >= meltingPoint / 2f;
	}

	public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
	{
		if (!CanWork(stack))
		{
			return null;
		}
		ItemStack itemStack = new ItemStack(api.World.GetItem(new AssetLocation("workitem-" + Variant["metal"])));
		itemStack.Collectible.SetTemperature(api.World, itemStack, stack.Collectible.GetTemperature(api.World, stack));
		if (beAnvil.WorkItemStack == null)
		{
			CreateVoxels(ref beAnvil.Voxels);
		}
		else
		{
			if (!string.Equals(beAnvil.WorkItemStack.Collectible.Variant["metal"], stack.Collectible.Variant["metal"]))
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
				}
				return null;
			}
			if (AddVoxels(ref beAnvil.Voxels) == 0)
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
				}
				return null;
			}
		}
		return itemStack;
	}

	public virtual int VoxelCountForHandbook(ItemStack stack)
	{
		return 81;
	}

	public static void CreateVoxels(ref byte[,,] voxels)
	{
		voxels = new byte[16, 6, 16];
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 1; j++)
			{
				for (int k = 0; k < 9; k++)
				{
					voxels[3 + i, j, 3 + k] = 1;
				}
			}
		}
	}

	public static int AddVoxels(ref byte[,,] voxels)
	{
		int num = 0;
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				int k = 0;
				int num2 = 0;
				for (; k < 6; k++)
				{
					if (num2 >= 1)
					{
						break;
					}
					if (voxels[3 + i, k, 3 + j] == 0)
					{
						voxels[3 + i, k, 3 + j] = 1;
						num2++;
						num++;
					}
				}
			}
		}
		return num;
	}

	public ItemStack GetBaseMaterial(ItemStack stack)
	{
		return stack;
	}

	public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
	{
		return EnumHelveWorkableMode.NotWorkable;
	}
}
