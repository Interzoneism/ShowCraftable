using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCrate : BlockEntityContainer, IRotatable
{
	private InventoryGeneric inventory;

	private BlockCrate ownBlock;

	public string type = "wood-aged";

	public string label;

	public string preferredLidState = "closed";

	public int quantitySlots = 16;

	public bool retrieveOnly;

	private float rotAngleY;

	private MeshData ownMesh;

	private MeshData labelMesh;

	private Cuboidf selBoxCrate;

	private Cuboidf selBoxLabel;

	private int labelColor;

	private ItemStack labelStack;

	private ModSystemLabelMeshCache labelCacheSys;

	private bool requested;

	private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);

	public bool Labelled
	{
		get
		{
			if (label != null)
			{
				return label != "";
			}
			return false;
		}
	}

	public virtual float MeshAngle
	{
		get
		{
			return rotAngleY;
		}
		set
		{
			rotAngleY = value;
		}
	}

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "crate";

	public LabelProps LabelProps
	{
		get
		{
			if (label == null)
			{
				return null;
			}
			ownBlock.Props.Labels.TryGetValue(label, out var value);
			return value;
		}
	}

	public string LidState
	{
		get
		{
			if (preferredLidState == "closed")
			{
				return preferredLidState;
			}
			if (inventory.Empty)
			{
				return preferredLidState;
			}
			ItemStack itemstack = inventory.FirstNonEmptySlot.Itemstack;
			if (itemstack?.Collectible == null || (itemstack.ItemAttributes != null && itemstack.ItemAttributes["inContainerTexture"].Exists))
			{
				return preferredLidState;
			}
			JsonObject itemAttributes = itemstack.ItemAttributes;
			bool? flag = ((itemAttributes == null || !itemAttributes["displayInsideCrate"].Exists) ? ((bool?)null) : itemstack.ItemAttributes?["displayInsideCrate"].AsBool(defaultValue: true));
			if ((itemstack.Block == null || itemstack.Block.DrawType != EnumDrawType.Cube || flag == false) && flag != true)
			{
				return "closed";
			}
			return preferredLidState;
		}
	}

	private float rndScale => 1f + (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 100) - 50) / 1000f;

	public override void Initialize(ICoreAPI api)
	{
		ownBlock = (BlockCrate)base.Block;
		bool flag = inventory == null;
		if (flag)
		{
			InitInventory(base.Block, api);
		}
		base.Initialize(api);
		if (api.Side == EnumAppSide.Client && !flag)
		{
			labelCacheSys = api.ModLoader.GetModSystem<ModSystemLabelMeshCache>();
			loadOrCreateMesh();
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Attributes != null)
		{
			string text = byItemStack.Attributes.GetString("type", ownBlock.Props.DefaultType);
			string text2 = byItemStack.Attributes.GetString("label");
			string text3 = byItemStack.Attributes.GetString("lidState", "closed");
			if (text != type || text2 != label || text3 != preferredLidState)
			{
				label = text2;
				type = text;
				preferredLidState = text3;
				InitInventory(base.Block, Api);
				Inventory.LateInitialize(InventoryClassName + "-" + Pos, Api);
				Inventory.ResolveBlocksOrItems();
				container.LateInit();
				MarkDirty();
			}
		}
		base.OnBlockPlaced((ItemStack)null);
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
	{
		bool shiftKey = byPlayer.Entity.Controls.ShiftKey;
		bool flag = !shiftKey;
		bool ctrlKey = byPlayer.Entity.Controls.CtrlKey;
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot == null)
		{
			throw new Exception("Interact called when byPlayer has null ActiveHotbarSlot");
		}
		if (ctrlKey && Labelled)
		{
			ItemStack itemstack = activeHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.ItemAttributes?["pigment"]?["color"].Exists == true && blockSel.SelectionBoxIndex == 1)
			{
				if (inventory.Empty && labelStack != null)
				{
					FreeAtlasSpace();
					labelStack = null;
					labelMesh = null;
					MarkDirty(redrawOnClient: true);
					return true;
				}
				if (!Inventory.Empty)
				{
					JsonObject jsonObject = activeHotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
					int num = jsonObject["red"].AsInt();
					int num2 = jsonObject["green"].AsInt();
					int num3 = jsonObject["blue"].AsInt();
					int num4 = ColorUtil.ToRgba(255, (int)GameMath.Clamp((float)num * 1.2f, 0f, 255f), (int)GameMath.Clamp((float)num2 * 1.2f, 0f, 255f), (int)GameMath.Clamp((float)num3 * 1.2f, 0f, 255f));
					if (labelStack == null || labelColor != num4)
					{
						FreeAtlasSpace();
						labelColor = num4;
						labelStack = inventory.FirstNonEmptySlot.Itemstack.Clone();
						labelStack.Attributes.RemoveAttribute("temperature");
						labelStack.Attributes.RemoveAttribute("transitionstate");
						labelMesh = null;
						byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw"), (double)blockSel.Position.X + blockSel.HitPosition.X, (double)blockSel.Position.InternalY + blockSel.HitPosition.Y, (double)blockSel.Position.Z + blockSel.HitPosition.Z, byPlayer, randomizePitch: true, 8f);
						MarkDirty(redrawOnClient: true);
						return true;
					}
				}
				else if (flag)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "empty", Lang.Get("Can't draw item symbol on an empty crate. Put something inside the crate first"));
				}
			}
		}
		if (flag)
		{
			int i;
			for (i = 0; i < inventory.Count && inventory[i].Empty; i++)
			{
			}
			if (i >= inventory.Count)
			{
				return true;
			}
			ItemSlot itemSlot = inventory[i];
			int num5 = ((!ctrlKey) ? 1 : itemSlot.Itemstack.Collectible.MaxStackSize);
			for (; i < inventory.Count; i++)
			{
				if (itemSlot.StackSize >= num5)
				{
					break;
				}
				inventory[i].TryPutInto(Api.World, itemSlot, num5 - itemSlot.StackSize);
			}
			ItemStack itemStack = itemSlot.TakeOut(num5);
			int stackSize = itemStack.StackSize;
			bool num6 = byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true);
			int num7 = stackSize - itemStack.StackSize;
			if (num6)
			{
				if (num7 == 0)
				{
					num7 = stackSize;
				}
				if (stackSize > num7)
				{
					new DummySlot(itemStack).TryPutInto(Api.World, itemSlot, stackSize - num7);
				}
				didMoveItems(itemStack, byPlayer);
			}
			else
			{
				new DummySlot(itemStack).TryPutInto(Api.World, itemSlot, stackSize - num7);
			}
			if (num7 == 0)
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "invfull", Lang.Get("item-take-error-invfull"));
			}
			else
			{
				Api.Logger.Audit(string.Concat("{0} Took {1}x{2} from ", base.Block?.Code, " at {3}."), byPlayer.PlayerName, num7, itemStack?.Collectible.Code, Pos);
				itemSlot.MarkDirty();
				MarkDirty();
			}
			return true;
		}
		if (shiftKey && !activeHotbarSlot.Empty)
		{
			ItemSlot firstNonEmptySlot = inventory.FirstNonEmptySlot;
			int num8 = ((!ctrlKey) ? 1 : activeHotbarSlot.StackSize);
			if (firstNonEmptySlot == null)
			{
				if (!activeHotbarSlot.Itemstack.Equals(Api.World, labelStack, GlobalConstants.IgnoredStackAttributes))
				{
					FreeAtlasSpace();
					labelStack = null;
					labelMesh = null;
				}
				if (activeHotbarSlot.TryPutInto(Api.World, inventory[0], num8) > 0)
				{
					didMoveItems(inventory[0].Itemstack, byPlayer);
					Api.World.Logger.Audit("{0} Put {1}x{2} into Crate at {3}.", byPlayer.PlayerName, num8, inventory[0].Itemstack?.Collectible.Code, Pos);
				}
			}
			else if (activeHotbarSlot.Itemstack.Equals(Api.World, firstNonEmptySlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
			{
				List<ItemSlot> list = new List<ItemSlot>();
				while (activeHotbarSlot.StackSize > 0 && list.Count < inventory.Count)
				{
					WeightedSlot bestSuitedSlot = inventory.GetBestSuitedSlot(activeHotbarSlot, null, list);
					if (bestSuitedSlot.slot == null)
					{
						break;
					}
					if (activeHotbarSlot.TryPutInto(Api.World, bestSuitedSlot.slot, num8) > 0)
					{
						didMoveItems(bestSuitedSlot.slot.Itemstack, byPlayer);
						Api.World.Logger.Audit("{0} Put {1}x{2} into Crate at {3}.", byPlayer.PlayerName, num8, bestSuitedSlot.slot.Itemstack?.Collectible.Code, Pos);
						if (!ctrlKey)
						{
							break;
						}
					}
					list.Add(bestSuitedSlot.slot);
				}
			}
			activeHotbarSlot.MarkDirty();
			MarkDirty();
		}
		return true;
	}

	protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			loadOrCreateMesh();
		}
		(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		AssetLocation assetLocation = stack?.Block?.Sounds?.Place;
		Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
	}

	protected virtual void InitInventory(Block block, ICoreAPI api)
	{
		if (block?.Attributes != null)
		{
			JsonObject jsonObject = block.Attributes["properties"][type];
			if (!jsonObject.Exists)
			{
				jsonObject = block.Attributes["properties"]["*"];
			}
			quantitySlots = jsonObject["quantitySlots"].AsInt(quantitySlots);
			retrieveOnly = jsonObject["retrieveOnly"].AsBool();
		}
		inventory = new InventoryGeneric(quantitySlots, null, null);
		inventory.BaseWeight = 1f;
		inventory.OnGetSuitability = (ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) => (isMerge ? (inventory.BaseWeight + 3f) : (inventory.BaseWeight + 1f)) + (float)((sourceSlot.Inventory is InventoryBasePlayer) ? 1 : 0);
		inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
		if (block?.Attributes != null)
		{
			if (block.Attributes["spoilSpeedMulByFoodCat"][type].Exists)
			{
				inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
			}
			if (block.Attributes["transitionSpeedMul"][type].Exists)
			{
				inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMul"][type].AsObject<Dictionary<EnumTransitionType, float>>();
			}
		}
		inventory.PutLocked = retrieveOnly;
		inventory.OnInventoryClosed += OnInvClosed;
		inventory.OnInventoryOpened += OnInvOpened;
		if (api.Side == EnumAppSide.Server)
		{
			inventory.SlotModified += Inventory_SlotModified;
		}
		container.Reset();
	}

	private void Inventory_SlotModified(int obj)
	{
		MarkDirty();
	}

	public Cuboidf[] GetSelectionBoxes()
	{
		if (selBoxCrate == null)
		{
			selBoxCrate = base.Block.SelectionBoxes[0].RotatedCopy(0f, (int)Math.Round(rotAngleY * (180f / (float)Math.PI) / 90f) * 90, 0f, new Vec3d(0.5, 0.0, 0.5));
			selBoxLabel = base.Block.SelectionBoxes[1].RotatedCopy(0f, rotAngleY * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.0, 0.5));
		}
		if (Api.Side == EnumAppSide.Client)
		{
			ItemSlot activeHotbarSlot = ((ICoreClientAPI)Api).World.Player.InventoryManager.ActiveHotbarSlot;
			if (Labelled)
			{
				ItemStack itemstack = activeHotbarSlot.Itemstack;
				if (itemstack != null && itemstack.ItemAttributes?["pigment"]?["color"].Exists == true)
				{
					return new Cuboidf[2] { selBoxCrate, selBoxLabel };
				}
			}
		}
		return new Cuboidf[1] { selBoxCrate };
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		BlockCrate blockCrate = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode"))) as BlockCrate;
		type = tree.GetString("type", blockCrate?.Props.DefaultType);
		label = tree.GetString("label");
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		labelColor = tree.GetInt("labelColor");
		labelStack = tree.GetItemstack("labelStack");
		preferredLidState = tree.GetString("lidState");
		if (labelStack != null && !labelStack.ResolveBlockOrItem(worldForResolving))
		{
			labelStack = null;
		}
		if (inventory == null)
		{
			if (tree.HasAttribute("blockCode"))
			{
				InitInventory(blockCrate, worldForResolving.Api);
			}
			else
			{
				InitInventory(null, worldForResolving.Api);
			}
		}
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			loadOrCreateMesh();
			MarkDirty(redrawOnClient: true);
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (base.Block != null)
		{
			tree.SetString("forBlockCode", base.Block.Code.ToShortString());
		}
		if (type == null)
		{
			type = ownBlock.Props.DefaultType;
		}
		tree.SetString("label", label);
		tree.SetString("type", type);
		tree.SetFloat("meshAngle", MeshAngle);
		tree.SetInt("labelColor", labelColor);
		tree.SetString("lidState", preferredLidState);
		tree.SetItemstack("labelStack", labelStack);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		if (labelStack != null && !labelStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			labelStack = null;
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		labelStack?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(labelStack), blockIdMapping, itemIdMapping);
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		if (atBlockFace == BlockFacing.DOWN)
		{
			return inventory.FirstNonEmptySlot;
		}
		return null;
	}

	private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		ItemSlot firstNonEmptySlot = inventory.FirstNonEmptySlot;
		if (firstNonEmptySlot == null)
		{
			return inventory[0];
		}
		if (firstNonEmptySlot.Itemstack.Equals(Api.World, fromSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			foreach (ItemSlot item in inventory)
			{
				if (item.Itemstack == null || item.StackSize < item.Itemstack.Collectible.MaxStackSize)
				{
					return item;
				}
			}
			return null;
		}
		return null;
	}

	protected virtual void OnInvOpened(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
	}

	protected virtual void OnInvClosed(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly;
	}

	private void loadOrCreateMesh()
	{
		if (base.Block == null)
		{
			Block block = (base.Block = Api.World.BlockAccessor.GetBlock(Pos) as BlockCrate);
		}
		if (!(base.Block is BlockCrate blockCrate))
		{
			return;
		}
		string key = "crateMeshes" + blockCrate.FirstCodePart();
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, key, () => new Dictionary<string, MeshData>());
		CompositeShape shape = ownBlock.Props[type].Shape;
		if (!(shape?.Base == null))
		{
			ItemStack itemStack = inventory.FirstNonEmptySlot?.Itemstack;
			string key2 = type + blockCrate.Subtype + "-" + label + "-" + LidState + "-" + ((LidState == "closed") ? null : (itemStack?.StackSize + "-" + itemStack?.GetHashCode()));
			if (!orCreate.TryGetValue(key2, out var value))
			{
				value = (orCreate[key2] = blockCrate.GenMesh(Api as ICoreClientAPI, itemStack, type, label, LidState, shape, new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ)));
			}
			ownMesh = value.Clone().Rotate(origin, 0f, MeshAngle, 0f).Scale(origin, rndScale, rndScale, rndScale);
		}
	}

	private void genLabelMesh()
	{
		if (LabelProps?.EditableShape == null || labelStack == null || requested)
		{
			return;
		}
		if (labelCacheSys == null)
		{
			labelCacheSys = Api.ModLoader.GetModSystem<ModSystemLabelMeshCache>();
		}
		requested = true;
		labelCacheSys.RequestLabelTexture(labelColor, Pos, labelStack, delegate(int texSubId)
		{
			GenLabelMeshWithItemStack(texSubId);
			((ICoreClientAPI)Api).Event.EnqueueMainThreadTask(delegate
			{
				MarkDirty(redrawOnClient: true);
			}, "markcratedirty");
			requested = false;
		});
	}

	private void GenLabelMeshWithItemStack(int textureSubId)
	{
		ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
		TextureAtlasPosition texPos = coreClientAPI.BlockTextureAtlas.Positions[textureSubId];
		labelMesh = ownBlock.GenLabelMesh(coreClientAPI, label, texPos, editableVariant: true);
		labelMesh.Rotate(origin, 0f, rotAngleY + (float)Math.PI, 0f).Scale(origin, rndScale, rndScale, rndScale);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (base.OnTesselation(mesher, tesselator))
		{
			return true;
		}
		if (ownMesh == null)
		{
			return true;
		}
		if (labelMesh == null)
		{
			genLabelMesh();
		}
		mesher.AddMeshData(ownMesh);
		mesher.AddMeshData(labelMesh);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		int num = 0;
		foreach (ItemSlot item in inventory)
		{
			num += item.StackSize;
		}
		if (num > 0)
		{
			dsc.AppendLine(Lang.Get("Contents: {0}x{1}", num, inventory.FirstNonEmptySlot.GetStackName()));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Empty"));
		}
		base.GetBlockInfo(forPlayer, dsc);
	}

	public override void OnBlockUnloaded()
	{
		FreeAtlasSpace();
		base.OnBlockUnloaded();
	}

	public override void OnBlockRemoved()
	{
		FreeAtlasSpace();
		base.OnBlockRemoved();
	}

	private void FreeAtlasSpace()
	{
		if (labelStack != null)
		{
			labelCacheSys?.FreeLabelTexture(labelStack, labelColor, Pos);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		ownMesh = null;
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
