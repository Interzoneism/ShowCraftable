using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityToolrack : BlockEntity, ITexPositionSource
{
	public InventoryGeneric inventory;

	private MeshData[] toolMeshes = new MeshData[4];

	private CollectibleObject tmpItem;

	public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (BlockToolRack.ToolTextureSubIds(Api).TryGetValue((Item)tmpItem, out var value))
			{
				if (value.TextureSubIdsByCode.TryGetValue(textureCode, out var value2))
				{
					return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[value2];
				}
				return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[value.TextureSubIdsByCode.First().Value];
			}
			Api.Logger.Debug("Could not get item texture! textureCode: {0} Item: {1}", textureCode, tmpItem.Code);
			return ((ICoreClientAPI)Api).BlockTextureAtlas.UnknownTexturePosition;
		}
	}

	public BlockEntityToolrack()
	{
		inventory = new InventoryGeneric(4, "toolrack", null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.LateInitialize("toolrack-" + Pos.ToString(), api);
		inventory.ResolveBlocksOrItems();
		inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
		if (api is ICoreClientAPI)
		{
			loadToolMeshes();
			api.Event.RegisterEventBusListener(OnEventBusEvent);
		}
	}

	private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
	{
		if (!(eventname != "genjsontransform") || !(eventname != "oncloseedittransforms") || !(eventname != "onapplytransforms"))
		{
			loadToolMeshes();
			MarkDirty(redrawOnClient: true);
		}
	}

	protected virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
	{
		return baseMul;
	}

	private void loadToolMeshes()
	{
		BlockFacing cCW = getFacing().GetCCW();
		if (cCW == null)
		{
			return;
		}
		Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
		ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
		for (int i = 0; i < 4; i++)
		{
			toolMeshes[i] = null;
			ItemStack itemstack = inventory[i].Itemstack;
			if (itemstack == null)
			{
				continue;
			}
			tmpItem = itemstack.Collectible;
			IContainedMeshSource containedMeshSource = itemstack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
			if (containedMeshSource != null)
			{
				toolMeshes[i] = containedMeshSource.GenMesh(itemstack, coreClientAPI.BlockTextureAtlas, Pos);
			}
			else if (itemstack.Class == EnumItemClass.Item)
			{
				coreClientAPI.Tesselator.TesselateItem(itemstack.Item, out toolMeshes[i], this);
			}
			else
			{
				coreClientAPI.Tesselator.TesselateBlock(itemstack.Block, out toolMeshes[i]);
			}
			JsonObject attributes = tmpItem.Attributes;
			if (attributes != null && attributes["toolrackTransform"].Exists)
			{
				ModelTransform modelTransform = tmpItem.Attributes["toolrackTransform"].AsObject<ModelTransform>();
				modelTransform.EnsureDefaultValues();
				toolMeshes[i].ModelTransform(modelTransform);
			}
			float num = ((i > 1) ? (-0.1125f) : 0f);
			if (itemstack.Class == EnumItemClass.Item)
			{
				CompositeShape shape = itemstack.Item.Shape;
				if (shape != null && shape.VoxelizeTexture)
				{
					toolMeshes[i].Scale(origin, 0.33f, 0.33f, 0.33f);
					toolMeshes[i].Translate((i % 2 == 0) ? 0.23f : (-0.3f), ((i > 1) ? 0.2f : (-0.3f)) + num, 0.433f * (float)((cCW.Axis != EnumAxis.X) ? 1 : (-1)));
					toolMeshes[i].Rotate(origin, 0f, (float)(cCW.HorizontalAngleIndex * 90) * ((float)Math.PI / 180f), 0f);
					toolMeshes[i].Rotate(origin, (float)Math.PI, 0f, 0f);
					continue;
				}
			}
			toolMeshes[i].Scale(origin, 0.6f, 0.6f, 0.6f);
			float x = ((i > 1) ? (-0.2f) : 0.3f);
			float z = ((i % 2 == 0) ? 0.23f : (-0.2f)) * ((cCW.Axis == EnumAxis.X) ? 1f : (-1f));
			toolMeshes[i].Translate(x, 0.433f + num, z);
			toolMeshes[i].Rotate(origin, 0f, (float)(cCW.HorizontalAngleIndex * 90) * ((float)Math.PI / 180f), (float)Math.PI / 2f);
			toolMeshes[i].Rotate(origin, 0f, (float)Math.PI / 2f, 0f);
		}
	}

	internal bool OnPlayerInteract(IPlayer byPlayer, Vec3d hit)
	{
		BlockFacing facing = getFacing();
		int num = ((hit.Y < 0.5) ? 2 : 0);
		if (facing == BlockFacing.NORTH && hit.X > 0.5)
		{
			num++;
		}
		if (facing == BlockFacing.SOUTH && hit.X < 0.5)
		{
			num++;
		}
		if (facing == BlockFacing.WEST && hit.Z > 0.5)
		{
			num++;
		}
		if (facing == BlockFacing.EAST && hit.Z < 0.5)
		{
			num++;
		}
		if (inventory[num].Itemstack != null)
		{
			return TakeFromSlot(byPlayer, num);
		}
		return PutInSlot(byPlayer, num);
	}

	private bool PutInSlot(IPlayer player, int slot)
	{
		IItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (itemstack != null)
		{
			if (!itemstack.Collectible.Tool.HasValue)
			{
				JsonObject attributes = itemstack.Collectible.Attributes;
				if (attributes == null || !attributes["rackable"].AsBool())
				{
					goto IL_004d;
				}
			}
			AssetLocation assetLocation = player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code;
			player.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[slot]);
			Api.World.Logger.Audit("{0} Put 1x{1} into Tool rack at {2}.", player.PlayerName, assetLocation, Pos);
			didInteract(player);
			return true;
		}
		goto IL_004d;
		IL_004d:
		return false;
	}

	private bool TakeFromSlot(IPlayer player, int slot)
	{
		ItemStack itemStack = inventory[slot].TakeOutWhole();
		if (!player.InventoryManager.TryGiveItemstack(itemStack))
		{
			Api.World.SpawnItemEntity(itemStack, Pos);
		}
		AssetLocation assetLocation = itemStack?.Collectible.Code;
		Api.World.Logger.Audit("{0} Took 1x{1} from Tool rack at {2}.", player.PlayerName, assetLocation, Pos);
		didInteract(player);
		return true;
	}

	private void didInteract(IPlayer player)
	{
		Api.World.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), Pos, 0.0, player, randomizePitch: false);
		if (Api is ICoreClientAPI)
		{
			loadToolMeshes();
		}
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		for (int i = 0; i < 4; i++)
		{
			ItemStack itemstack = inventory[i].Itemstack;
			if (itemstack != null)
			{
				Api.World.SpawnItemEntity(itemstack, Pos);
			}
		}
	}

	private BlockFacing getFacing()
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Api.World.BlockAccessor.GetBlock(Pos).LastCodePart());
		if (blockFacing != null)
		{
			return blockFacing;
		}
		return BlockFacing.NORTH;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		ICoreClientAPI obj = (ICoreClientAPI)Api;
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		MeshData defaultBlockMesh = obj.TesselatorManager.GetDefaultBlockMesh(block);
		if (defaultBlockMesh == null)
		{
			return true;
		}
		mesher.AddMeshData(defaultBlockMesh);
		for (int i = 0; i < 4; i++)
		{
			if (toolMeshes[i] != null)
			{
				mesher.AddMeshData(toolMeshes[i]);
			}
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
		if (Api != null)
		{
			inventory.Api = Api;
			inventory.ResolveBlocksOrItems();
		}
		if (Api is ICoreClientAPI)
		{
			loadToolMeshes();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ITreeAttribute treeAttribute = new TreeAttribute();
		inventory.ToTreeAttributes(treeAttribute);
		tree["inventory"] = treeAttribute;
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot item in inventory)
		{
			if (item.Itemstack != null)
			{
				if (item.Itemstack.Class == EnumItemClass.Item)
				{
					itemIdMapping[item.Itemstack.Item.Id] = item.Itemstack.Item.Code;
				}
				else
				{
					blockIdMapping[item.Itemstack.Block.BlockId] = item.Itemstack.Block.Code;
				}
				item.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, item, blockIdMapping, itemIdMapping);
			}
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (ItemSlot item in inventory)
		{
			if (item.Itemstack != null)
			{
				if (!item.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
				{
					item.Itemstack = null;
				}
				else
				{
					item.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, item, oldBlockIdMapping, oldItemIdMapping, resolveImports);
				}
			}
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		int num = 0;
		ItemStack itemstack = null;
		foreach (ItemSlot item in inventory)
		{
			if (num % 2 == 0)
			{
				itemstack = item.Itemstack;
			}
			else
			{
				AddSlotItemInfo(sb, num - 1, item.Itemstack);
				AddSlotItemInfo(sb, num, itemstack);
			}
			num++;
		}
		sb.AppendLineOnce();
		sb.ToString();
	}

	private void AddSlotItemInfo(StringBuilder sb, int i, ItemStack itemstack)
	{
		if (i == 2 && sb.Length > 0)
		{
			sb.Append("\n");
		}
		if (itemstack != null)
		{
			if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
			{
				sb.Append(", ");
			}
			sb.Append(itemstack.GetName());
		}
	}
}
