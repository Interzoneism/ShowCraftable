using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityTrough : BlockEntityContainer, ITexPositionSource, IAnimalFoodSource, IPointOfInterest
{
	internal InventoryGeneric inventory;

	private ITexPositionSource blockTexPosSource;

	private MeshData currentMesh;

	private string contentCode = "";

	private DoubleTroughPoiDummy dummypoi;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "trough";

	public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	public ContentConfig[] contentConfigs => Api.ObjectCache["troughContentConfigs-" + base.Block.Code] as ContentConfig[];

	public bool IsFull
	{
		get
		{
			ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks();
			ContentConfig contentConfig = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
			if (contentConfig == null)
			{
				return false;
			}
			if (nonEmptyContentStacks.Length != 0)
			{
				return nonEmptyContentStacks[0].StackSize >= contentConfig.QuantityPerFillLevel * contentConfig.MaxFillLevels;
			}
			return false;
		}
	}

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode != "contents")
			{
				return blockTexPosSource[textureCode];
			}
			string text = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode)?.TextureCode;
			if (text != null && text.Equals("*"))
			{
				text = "contents-" + Inventory.FirstNonEmptySlot.Itemstack.Collectible.Code.ToShortString();
			}
			if (text == null)
			{
				return blockTexPosSource[textureCode];
			}
			return blockTexPosSource[text];
		}
	}

	public BlockEntityTrough()
	{
		inventory = new InventoryGeneric(4, null, null, (int id, InventoryGeneric inv) => new ItemSlotTrough(this, inv));
		inventory.OnGetAutoPushIntoSlot = (BlockFacing face, ItemSlot slot) => IsFull ? null : inventory.GetBestSuitedSlot(slot).slot;
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (inventory.Empty || diet == null)
		{
			return false;
		}
		ContentConfig contentConfig = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		ItemStack itemStack = contentConfig?.Content?.ResolvedItemstack ?? ResolveWildcardContent(contentConfig, entity.World);
		if (itemStack == null)
		{
			return false;
		}
		if (diet.Matches(itemStack) && inventory[0].StackSize >= contentConfig.QuantityPerFillLevel && base.Block is BlockTroughBase blockTroughBase)
		{
			return !blockTroughBase.UnsuitableForEntity(entity.Code.Path);
		}
		return false;
	}

	private ItemStack ResolveWildcardContent(ContentConfig config, IWorldAccessor worldAccessor)
	{
		if (config?.Content?.Code == null)
		{
			return null;
		}
		List<CollectibleObject> list = new List<CollectibleObject>();
		switch (config.Content.Type)
		{
		case EnumItemClass.Block:
			list.AddRange(worldAccessor.SearchBlocks(config.Content.Code));
			break;
		case EnumItemClass.Item:
			list.AddRange(worldAccessor.SearchItems(config.Content.Code));
			break;
		default:
			throw new ArgumentOutOfRangeException("Type");
		}
		foreach (CollectibleObject item in list)
		{
			if (item.Code.Equals(Inventory.FirstNonEmptySlot?.Itemstack?.Item?.Code))
			{
				return new ItemStack(item);
			}
		}
		return null;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		ContentConfig contentConfig = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		if (contentConfig == null || inventory.Empty)
		{
			return 0f;
		}
		inventory[0].TakeOut(contentConfig.QuantityPerFillLevel);
		if (inventory[0].Empty)
		{
			contentCode = "";
		}
		inventory[0].MarkDirty();
		MarkDirty(redrawOnClient: true);
		return 1f;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (Api.Side == EnumAppSide.Client)
		{
			_ = (ICoreClientAPI)api;
			if (currentMesh == null)
			{
				currentMesh = GenMesh();
			}
		}
		else
		{
			Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			if (base.Block is BlockTroughDoubleBlock blockTroughDoubleBlock)
			{
				dummypoi = new DoubleTroughPoiDummy(this)
				{
					Position = blockTroughDoubleBlock.OtherPartPos(Pos).ToVec3d().Add(0.5, 0.5, 0.5)
				};
				Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(dummypoi);
			}
		}
		inventory.SlotModified += Inventory_SlotModified;
	}

	private void Inventory_SlotModified(int id)
	{
		contentCode = ItemSlotTrough.getContentConfig(Api.World, contentConfigs, inventory[id])?.Code;
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
		}
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
			if (dummypoi != null)
			{
				Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(dummypoi);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
			if (dummypoi != null)
			{
				Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(dummypoi);
			}
		}
	}

	internal MeshData GenMesh()
	{
		if (base.Block == null)
		{
			return null;
		}
		ItemStack itemstack = inventory[0].Itemstack;
		if (itemstack == null)
		{
			return null;
		}
		string text = "";
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		if (contentCode == "" || contentConfigs == null)
		{
			if (!(itemstack.Collectible.Code.Path == "rot"))
			{
				return null;
			}
			text = "block/wood/trough/" + ((base.Block.Variant["part"] == "small") ? "small" : "large") + "/rotfill" + GameMath.Clamp(itemstack.StackSize / 4, 1, 4);
		}
		else
		{
			ContentConfig contentConfig = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
			if (contentConfig == null)
			{
				return null;
			}
			int val = Math.Max(0, itemstack.StackSize / contentConfig.QuantityPerFillLevel - 1);
			text = contentConfig.ShapesPerFillLevel[Math.Min(contentConfig.ShapesPerFillLevel.Length - 1, val)];
		}
		Vec3f vec3f = new Vec3f(base.Block.Shape.rotateX, base.Block.Shape.rotateY, base.Block.Shape.rotateZ);
		blockTexPosSource = coreClientAPI.Tesselator.GetTextureSource(base.Block);
		Shape shapeBase = Shape.TryGet(Api, "shapes/" + text + ".json");
		coreClientAPI.Tesselator.TesselateShape("betroughcontentsleft", shapeBase, out var modeldata, this, vec3f, 0, 0, 0);
		if (base.Block is BlockTroughDoubleBlock blockTroughDoubleBlock)
		{
			coreClientAPI.Tesselator.TesselateShape("betroughcontentsright", shapeBase, out var modeldata2, this, vec3f.Add(0f, 180f, 0f), 0, 0, 0);
			BlockFacing blockFacing = blockTroughDoubleBlock.OtherPartFacing();
			modeldata2.Translate(blockFacing.Normalf);
			modeldata.AddMeshData(modeldata2);
		}
		return modeldata;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh);
		return false;
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return false;
		}
		ItemStack[] nonEmptyContentStacks = GetNonEmptyContentStacks();
		ContentConfig contentConfig = ItemSlotTrough.getContentConfig(Api.World, contentConfigs, activeHotbarSlot);
		if (contentConfig == null)
		{
			return false;
		}
		if (nonEmptyContentStacks.Length == 0)
		{
			if (activeHotbarSlot.StackSize >= contentConfig.QuantityPerFillLevel)
			{
				inventory[0].Itemstack = activeHotbarSlot.TakeOut(contentConfig.QuantityPerFillLevel);
				inventory[0].MarkDirty();
				return true;
			}
			return false;
		}
		if (activeHotbarSlot.Itemstack.Equals(Api.World, nonEmptyContentStacks[0], GlobalConstants.IgnoredStackAttributes) && activeHotbarSlot.StackSize >= contentConfig.QuantityPerFillLevel && nonEmptyContentStacks[0].StackSize < contentConfig.QuantityPerFillLevel * contentConfig.MaxFillLevels)
		{
			activeHotbarSlot.TakeOut(contentConfig.QuantityPerFillLevel);
			inventory[0].Itemstack.StackSize += contentConfig.QuantityPerFillLevel;
			inventory[0].MarkDirty();
			return true;
		}
		return false;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("contentCode", contentCode);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		contentCode = tree.GetString("contentCode");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack itemstack = inventory[0].Itemstack;
		if (contentConfigs == null)
		{
			return;
		}
		ContentConfig contentConfig = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		if (contentConfig == null && itemstack != null)
		{
			dsc.AppendLine(itemstack.StackSize + "x " + itemstack.GetName());
		}
		if (contentConfig == null || itemstack == null)
		{
			return;
		}
		int num = itemstack.StackSize / contentConfig.QuantityPerFillLevel;
		dsc.AppendLine(Lang.Get("Portions: {0}", num));
		ItemStack itemStack = contentConfig.Content.ResolvedItemstack ?? ResolveWildcardContent(contentConfig, forPlayer.Entity.World);
		if (itemStack == null)
		{
			return;
		}
		dsc.AppendLine(Lang.Get(itemStack.GetName()));
		HashSet<string> hashSet = new HashSet<string>();
		foreach (EntityProperties entityType in Api.World.EntityTypes)
		{
			JsonObject attributes = entityType.Attributes;
			if (attributes != null && attributes["creatureDiet"].AsObject<CreatureDiet>()?.Matches(itemStack) == true)
			{
				BlockTroughBase obj = base.Block as BlockTroughBase;
				if (obj == null || !obj.UnsuitableForEntity(entityType.Code.Path))
				{
					string key = attributes?["creatureDietGroup"].AsString() ?? attributes?["handbook"]["groupcode"].AsString() ?? (entityType.Code.Domain + ":item-creature-" + entityType.Code.Path);
					hashSet.Add(Lang.Get(key));
				}
			}
		}
		if (hashSet.Count <= 0)
		{
			dsc.AppendLine(Lang.Get("trough-unsuitable"));
			return;
		}
		dsc.AppendLine(Lang.Get("trough-suitable", string.Join(", ", hashSet)));
	}
}
