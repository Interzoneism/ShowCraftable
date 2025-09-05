using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemIronBloom : Item, IAnvilWorkable
{
	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack.Attributes.HasAttribute("voxels"))
		{
			return Lang.Get("Partially worked iron bloom");
		}
		return base.GetHeldItemName(itemStack);
	}

	public MeshData GenMesh(ICoreClientAPI capi, ItemStack stack)
	{
		return null;
	}

	public int GetWorkItemHashCode(ItemStack stack)
	{
		return stack.Attributes.GetHashCode();
	}

	public int GetRequiredAnvilTier(ItemStack stack)
	{
		return 2;
	}

	public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
	{
		return (from r in api.GetSmithingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(stack)
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
		if (beAnvil.WorkItemStack != null)
		{
			return null;
		}
		if (stack.Attributes.HasAttribute("voxels"))
		{
			try
			{
				beAnvil.Voxels = BlockEntityAnvil.deserializeVoxels(stack.Attributes.GetBytes("voxels"));
				beAnvil.SelectedRecipeId = stack.Attributes.GetInt("selectedRecipeId");
			}
			catch (Exception)
			{
				CreateVoxelsFromIronBloom(ref beAnvil.Voxels);
			}
		}
		else
		{
			CreateVoxelsFromIronBloom(ref beAnvil.Voxels);
		}
		ItemStack itemStack = stack.Clone();
		itemStack.StackSize = 1;
		itemStack.Collectible.SetTemperature(api.World, itemStack, stack.Collectible.GetTemperature(api.World, stack));
		return itemStack.Clone();
	}

	public virtual int VoxelCountForHandbook(ItemStack stack)
	{
		return 42;
	}

	private void CreateVoxelsFromIronBloom(ref byte[,,] voxels)
	{
		ItemIngot.CreateVoxelsFromIngot(api, ref voxels);
		Random rand = api.World.Rand;
		for (int i = -1; i < 8; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				for (int k = -1; k < 5; k++)
				{
					int num = 4 + i;
					int num2 = 6 + k;
					if (j == 0 && voxels[num, j, num2] == 1)
					{
						continue;
					}
					float num3 = (float)(Math.Max(0, Math.Abs(num - 7) - 1) + Math.Max(0, Math.Abs(num2 - 8) - 1)) + Math.Max(0f, (float)j - 1f);
					if (!(rand.NextDouble() < (double)(num3 / 3f - 0.4f + ((float)j - 1.5f) / 4f)))
					{
						if (rand.NextDouble() > (double)(num3 / 2f))
						{
							voxels[num, j, num2] = 1;
						}
						else
						{
							voxels[num, j, num2] = 2;
						}
					}
				}
			}
		}
	}

	public ItemStack GetBaseMaterial(ItemStack stack)
	{
		return stack;
	}

	public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
	{
		return EnumHelveWorkableMode.FullyWorkable;
	}
}
