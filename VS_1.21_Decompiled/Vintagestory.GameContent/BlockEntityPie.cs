using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityPie : BlockEntityContainer
{
	private InventoryGeneric inv;

	private MealMeshCache? ms;

	private MeshData? mesh;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "pie";

	public bool HasAnyFilling
	{
		get
		{
			ItemStack[] array = (inv[0].Itemstack?.Block as BlockPie)?.GetContents(Api.World, inv[0].Itemstack);
			if (((array != null) ? array[1] : null) == null && ((array != null) ? array[2] : null) == null && ((array != null) ? array[3] : null) == null)
			{
				return ((array != null) ? array[4] : null) != null;
			}
			return true;
		}
	}

	public bool HasAllFilling
	{
		get
		{
			ItemStack[] array = (inv[0].Itemstack?.Block as BlockPie)?.GetContents(Api.World, inv[0].Itemstack);
			if (((array != null) ? array[1] : null) != null && ((array != null) ? array[2] : null) != null && ((array != null) ? array[3] : null) != null)
			{
				return ((array != null) ? array[4] : null) != null;
			}
			return false;
		}
	}

	public bool HasCrust
	{
		get
		{
			BlockPie obj = inv[0].Itemstack?.Block as BlockPie;
			return ((obj != null) ? obj.GetContents(Api.World, inv[0].Itemstack)[5] : null) != null;
		}
	}

	public string? State => (inv[0].Itemstack?.Block as BlockPie)?.State;

	public int SlicesLeft => inv[0].Itemstack?.Attributes.GetAsInt("pieSize") ?? 0;

	public BlockEntityPie()
	{
		inv = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ms = api.ModLoader.GetModSystem<MealMeshCache>();
		loadMesh();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (inv[0].Itemstack?.Collectible.Code.Path == "rot")
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.SpawnItemEntity(inv[0].Itemstack, Pos.ToVec3d().Add(0.5, 0.1, 0.5));
		}
	}

	public override void OnBlockPlaced(ItemStack? byItemStack = null)
	{
		if (byItemStack != null)
		{
			inv[0].Itemstack = byItemStack.Clone();
			inv[0].Itemstack.StackSize = 1;
		}
	}

	public ItemStack? TakeSlice()
	{
		ItemStack itemStack = inv[0].Itemstack?.Clone();
		if (itemStack == null)
		{
			return null;
		}
		int slicesLeft = SlicesLeft;
		float num = inv[0].Itemstack?.Attributes.GetFloat("quantityServings") ?? 0f;
		MarkDirty(redrawOnClient: true);
		if (slicesLeft <= 1)
		{
			if (!itemStack.Attributes.HasAttribute("quantityServings"))
			{
				itemStack.Attributes.SetFloat("quantityServings", 0.25f);
			}
			inv[0].Itemstack = null;
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		else
		{
			inv[0].Itemstack?.Attributes.SetInt("pieSize", slicesLeft - 1);
			ItemStack itemstack = inv[0].Itemstack;
			if (itemstack != null && itemstack.Attributes.HasAttribute("quantityServings"))
			{
				inv[0].Itemstack?.Attributes.SetFloat("quantityServings", num - 0.25f);
			}
			itemStack.Attributes.SetInt("pieSize", 1);
			itemStack.Attributes.SetFloat("quantityServings", 0.25f);
		}
		itemStack.Attributes.SetBool("bakeable", value: false);
		loadMesh();
		MarkDirty(redrawOnClient: true);
		return itemStack;
	}

	public void OnPlaced(IPlayer? byPlayer)
	{
		ItemStack itemStack = byPlayer?.InventoryManager.ActiveHotbarSlot.TakeOut(2);
		if (itemStack != null)
		{
			ItemStack itemStack2 = new ItemStack(base.Block);
			(itemStack2.Block as BlockPie)?.SetContents(itemStack2, new ItemStack[6] { itemStack, null, null, null, null, null });
			itemStack2.Attributes.SetInt("pieSize", 4);
			itemStack2.Attributes.SetBool("bakeable", value: false);
			if (State != "raw" && !itemStack2.Attributes.HasAttribute("quantityServings"))
			{
				itemStack2.Attributes.SetFloat("quantityServings", (float)itemStack2.Attributes.GetAsInt("pieSize") * 0.25f);
			}
			inv[0].Itemstack = itemStack2;
			loadMesh();
		}
	}

	public bool OnInteract(IPlayer byPlayer)
	{
		if (!(inv[0].Itemstack?.Block is BlockPie blockPie))
		{
			return false;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		EnumTool? enumTool = activeHotbarSlot?.Itemstack?.Collectible.Tool;
		if (enumTool == EnumTool.Knife || enumTool == EnumTool.Sword)
		{
			if (blockPie.State != "raw")
			{
				if (Api.Side == EnumAppSide.Server)
				{
					ItemStack itemStack = TakeSlice();
					if (itemStack != null)
					{
						if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
						{
							Api.World.SpawnItemEntity(itemStack, Pos);
						}
						Api.World.Logger.Audit("{0} Took 1x{1} slice from Pie at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
					}
				}
			}
			else
			{
				ItemStack[] contents = blockPie.GetContents(Api.World, inv[0].Itemstack);
				if (HasAnyFilling && contents[5] != null)
				{
					BlockPie.CycleTopCrustType(inv[0].Itemstack);
					MarkDirty(redrawOnClient: true);
				}
			}
			return true;
		}
		if (activeHotbarSlot != null && !activeHotbarSlot.Empty && blockPie.State == "raw")
		{
			bool num = TryAddIngredientFrom(activeHotbarSlot, byPlayer);
			if (num)
			{
				loadMesh();
				MarkDirty(redrawOnClient: true);
			}
			ItemStack itemstack = inv[0].Itemstack;
			if (itemstack != null)
			{
				itemstack.Attributes.SetBool("bakeable", HasAllFilling);
				return num;
			}
			return num;
		}
		if (SlicesLeft == 1)
		{
			ItemStack itemstack2 = inv[0].Itemstack;
			if (itemstack2 == null || !itemstack2.Attributes.HasAttribute("quantityServings"))
			{
				inv[0].Itemstack?.Attributes.SetBool("bakeable", value: false);
				inv[0].Itemstack?.Attributes.SetFloat("quantityServings", 0.25f);
			}
		}
		if (Api.Side == EnumAppSide.Server)
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(inv[0].Itemstack))
			{
				Api.World.SpawnItemEntity(inv[0].Itemstack, Pos.ToVec3d().Add(0.5, 0.25, 0.5));
			}
			Api.World.Logger.Audit("{0} Took 1x{1} at {2}.", byPlayer.PlayerName, inv[0].Itemstack?.Collectible.Code, Pos);
			inv[0].Itemstack = null;
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
		return true;
	}

	private bool TryAddIngredientFrom(ItemSlot slot, IPlayer? byPlayer = null)
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		InPieProperties inPieProperties = slot.Itemstack?.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>(null, slot.Itemstack.Collectible.Code.Domain);
		if (inPieProperties == null)
		{
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "notpieable", Lang.Get("This item can not be added to pies"));
			}
			return false;
		}
		if (slot.StackSize < 2)
		{
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "notpieable", Lang.Get("Need at least 2 items each"));
			}
			return false;
		}
		if (!(inv[0].Itemstack?.Block is BlockPie blockPie))
		{
			return false;
		}
		ItemStack[] contents = blockPie.GetContents(Api.World, inv[0].Itemstack);
		bool num = contents[1] != null && contents[2] != null && contents[3] != null && contents[4] != null;
		bool flag = contents[1] != null || contents[2] != null || contents[3] != null || contents[4] != null;
		if (num)
		{
			if (inPieProperties.PartType == EnumPiePartType.Crust)
			{
				if (contents[5] == null)
				{
					contents[5] = slot.TakeOut(2);
					blockPie.SetContents(inv[0].Itemstack, contents);
				}
				else
				{
					BlockPie.CycleTopCrustType(inv[0].Itemstack);
				}
				return true;
			}
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "piefullfilling", Lang.Get("Can't add more filling - already completely filled pie"));
			}
			return false;
		}
		if (inPieProperties.PartType != EnumPiePartType.Filling)
		{
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "pieneedsfilling", Lang.Get("Need to add a filling next"));
			}
			return false;
		}
		if (!flag)
		{
			contents[1] = slot.TakeOut(2);
			blockPie.SetContents(inv[0].Itemstack, contents);
			return true;
		}
		EnumFoodCategory[] array = contents.Select(BlockPie.FillingFoodCategory).ToArray();
		InPieProperties[] array2 = contents.Select((ItemStack stack) => stack?.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>(null, stack.Collectible.Code.Domain)).ToArray();
		ItemStack itemStack = slot.Itemstack;
		EnumFoodCategory enumFoodCategory = BlockPie.FillingFoodCategory(slot.Itemstack);
		bool flag2 = true;
		bool flag3 = true;
		int num2 = 1;
		while (flag2 && num2 < contents.Length - 1)
		{
			if (itemStack != null)
			{
				flag2 &= contents[num2] == null || itemStack.Equals(Api.World, contents[num2], GlobalConstants.IgnoredStackAttributes);
				flag3 &= contents[num2] == null || array[num2] == enumFoodCategory;
				itemStack = contents[num2];
				enumFoodCategory = array[num2];
			}
			num2++;
		}
		int num3 = 2 + ((contents[2] != null) ? (1 + ((contents[3] != null) ? 1 : 0)) : 0);
		if (flag2)
		{
			contents[num3] = slot.TakeOut(2);
			blockPie.SetContents(inv[0].Itemstack, contents);
			return true;
		}
		if (!flag3)
		{
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "piefullfilling", Lang.Get("Can't mix fillings from different food categories"));
			}
			return false;
		}
		InPieProperties obj = array2[1];
		if (obj != null && !obj.AllowMixing)
		{
			if (byPlayer != null)
			{
				coreClientAPI?.TriggerIngameError(this, "piefullfilling", Lang.Get("You really want to mix these to ingredients?! That would taste horrible!"));
			}
			return false;
		}
		contents[num3] = slot.TakeOut(2);
		blockPie.SetContents(inv[0].Itemstack, contents);
		return true;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (inv[0].Empty)
		{
			return true;
		}
		mesher.AddMeshData(mesh);
		return true;
	}

	private void loadMesh()
	{
		if (Api != null && Api.Side != EnumAppSide.Server && !inv[0].Empty)
		{
			mesh = ms.GetPieMesh(inv[0].Itemstack);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (MealMeshCache.ContentsRotten(inv))
		{
			dsc.Append(Lang.Get("Rotten"));
		}
		else
		{
			dsc.Append(BlockEntityShelf.PerishableInfoCompact(Api, inv[0], 0f, withStackName: false));
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			MarkDirty(redrawOnClient: true);
			loadMesh();
		}
	}

	public override void OnBlockBroken(IPlayer? byPlayer = null)
	{
	}
}
