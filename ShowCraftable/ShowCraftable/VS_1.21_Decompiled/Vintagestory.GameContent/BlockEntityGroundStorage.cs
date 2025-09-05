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
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityGroundStorage : BlockEntityDisplay, IBlockEntityContainer, IRotatable, IHeatSource, ITemperatureSensitive
{
	private static SimpleParticleProperties smokeParticles;

	public object inventoryLock = new object();

	protected InventoryGeneric inventory;

	public bool forceStorageProps;

	protected EnumGroundStorageLayout? overrideLayout;

	protected Cuboidf[] colBoxes;

	protected Cuboidf[] selBoxes;

	private ItemSlot isUsingSlot;

	public bool clientsideFirstPlacement;

	private GroundStorageRenderer renderer;

	public bool UseRenderer;

	public bool NeedsRetesselation;

	public MultiTextureMeshRef[] MeshRefs = new MultiTextureMeshRef[4];

	public ModelTransform[] ModelTransformsRenderer = new ModelTransform[4];

	private bool burning;

	private double burnStartTotalHours;

	private ILoadedSound ambientSound;

	private long listenerId;

	private float burnHoursPerItem;

	private BlockFacing[] facings = (BlockFacing[])BlockFacing.ALLFACES.Clone();

	public GroundStorageProperties StorageProps { get; protected set; }

	public int TransferQuantity => StorageProps?.TransferQuantity ?? 1;

	public int BulkTransferQuantity
	{
		get
		{
			if (StorageProps.Layout != EnumGroundStorageLayout.Stacking)
			{
				return 1;
			}
			return StorageProps.BulkTransferQuantity;
		}
	}

	protected virtual int invSlotCount => 4;

	private Dictionary<string, MultiTextureMeshRef> UploadedMeshCache => ObjectCacheUtil.GetOrCreate(Api, "groundStorageUMC", () => new Dictionary<string, MultiTextureMeshRef>());

	public virtual bool CanIgnite
	{
		get
		{
			if (burnHoursPerItem > 0f)
			{
				ItemStack itemstack = inventory[0].Itemstack;
				if (itemstack == null)
				{
					return false;
				}
				return itemstack.Collectible.CombustibleProps?.BurnTemperature > 200;
			}
			return false;
		}
	}

	public int Layers
	{
		get
		{
			if (inventory[0].StackSize != 1)
			{
				return (int)((float)inventory[0].StackSize * StorageProps.ModelItemsToStackSizeRatio);
			}
			return 1;
		}
	}

	public bool IsBurning => burning;

	public bool IsHot => burning;

	public override int DisplayedItems
	{
		get
		{
			if (StorageProps == null)
			{
				return 0;
			}
			return StorageProps.Layout switch
			{
				EnumGroundStorageLayout.SingleCenter => 1, 
				EnumGroundStorageLayout.Halves => 2, 
				EnumGroundStorageLayout.WallHalves => 2, 
				EnumGroundStorageLayout.Quadrants => 4, 
				EnumGroundStorageLayout.Messy12 => 1, 
				EnumGroundStorageLayout.Stacking => 1, 
				_ => 0, 
			};
		}
	}

	public int TotalStackSize
	{
		get
		{
			int num = 0;
			foreach (ItemSlot item in inventory)
			{
				num += item.StackSize;
			}
			return num;
		}
	}

	public int Capacity => StorageProps.Layout switch
	{
		EnumGroundStorageLayout.SingleCenter => 1, 
		EnumGroundStorageLayout.Halves => 2, 
		EnumGroundStorageLayout.WallHalves => 2, 
		EnumGroundStorageLayout.Quadrants => 4, 
		EnumGroundStorageLayout.Messy12 => 12, 
		EnumGroundStorageLayout.Stacking => StorageProps.StackingCapacity, 
		_ => 1, 
	};

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "groundstorage";

	public override string AttributeTransformCode => "groundStorageTransform";

	public float MeshAngle { get; set; }

	public BlockFacing AttachFace { get; set; }

	public override TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (StorageProps.Layout == EnumGroundStorageLayout.Stacking && StorageProps.StackingTextures != null && StorageProps.StackingTextures.TryGetValue(textureCode, out var value))
			{
				return getOrCreateTexPos(value);
			}
			return base[textureCode];
		}
	}

	static BlockEntityGroundStorage()
	{
		smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(150, 40, 40, 40), new Vec3d(), new Vec3d(1.0, 0.0, 1.0), new Vec3f(-1f / 32f, 0.1f, -1f / 32f), new Vec3f(1f / 32f, 0.1f, 1f / 32f), 2f, -1f / 160f, 0.2f, 1f, EnumParticleModel.Quad);
		smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		smokeParticles.SelfPropelled = true;
		smokeParticles.AddPos.Set(1.0, 0.0, 1.0);
	}

	public bool CanAttachBlockAt(BlockFacing blockFace, Cuboidi attachmentArea)
	{
		if (StorageProps == null)
		{
			return false;
		}
		if (blockFace == BlockFacing.UP && StorageProps.Layout == EnumGroundStorageLayout.Stacking && inventory[0].StackSize == Capacity)
		{
			return StorageProps.UpSolid;
		}
		return false;
	}

	public BlockEntityGroundStorage()
	{
		inventory = new InventoryGeneric(invSlotCount, null, null, (int slotId, InventoryGeneric inv) => new ItemSlot(inv));
		foreach (ItemSlot item in inventory)
		{
			item.StorageType |= EnumItemStorageFlags.Backpack;
		}
		inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
		inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		colBoxes = new Cuboidf[1]
		{
			new Cuboidf(0f, 0f, 0f, 1f, 0.25f, 1f)
		};
		selBoxes = new Cuboidf[1]
		{
			new Cuboidf(0f, 0f, 0f, 1f, 0.25f, 1f)
		};
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		return null;
	}

	private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		return null;
	}

	public void ForceStorageProps(GroundStorageProperties storageProps)
	{
		StorageProps = storageProps;
		forceStorageProps = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		capi = api as ICoreClientAPI;
		base.Initialize(api);
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		if (behavior != null)
		{
			behavior.FirePos = Pos.Copy();
			behavior.FuelPos = Pos.Copy();
			behavior.OnFireDeath = delegate
			{
				Extinguish();
			};
		}
		UpdateIgnitable();
		DetermineStorageProperties(null);
		if (capi != null)
		{
			float num = 0f;
			if (!Inventory.Empty)
			{
				foreach (ItemSlot item in Inventory)
				{
					num = Math.Max(num, item.Itemstack?.Collectible.GetTemperature(capi.World, item.Itemstack) ?? 0f);
				}
			}
			if (num >= 450f)
			{
				renderer = new GroundStorageRenderer(capi, this);
			}
			updateMeshes();
		}
		UpdateBurningState();
	}

	public void CoolNow(float amountRel)
	{
		if (Inventory.Empty)
		{
			return;
		}
		for (int i = 0; i < Inventory.Count; i++)
		{
			ItemSlot itemSlot = Inventory[i];
			ItemStack itemstack = itemSlot.Itemstack;
			if (itemstack?.Collectible == null)
			{
				continue;
			}
			float temperature = itemstack.Collectible.GetTemperature(Api.World, itemstack);
			float num = Math.Max(0f, amountRel - 0.6f) * Math.Max(temperature - 250f, 0f) / 5000f;
			if ((itemstack.Collectible.Code.Path.Contains("burn") || itemstack.Collectible.Code.Path.Contains("fired")) && Api.World.Rand.NextDouble() < (double)num)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
				base.Block.SpawnBlockBrokenParticles(Pos);
				base.Block.SpawnBlockBrokenParticles(Pos);
				int stackSize = itemstack.StackSize;
				itemSlot.Itemstack = GetShatteredStack(itemstack);
				StorageProps = null;
				DetermineStorageProperties(itemSlot);
				forceStorageProps = true;
				itemSlot.Itemstack.StackSize = stackSize;
				itemSlot.MarkDirty();
			}
			else
			{
				if (temperature > 120f)
				{
					Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
				}
				itemstack.Collectible.SetTemperature(Api.World, itemstack, Math.Max(20f, temperature - amountRel * 20f), delayCooldown: false);
			}
		}
		MarkDirty(redrawOnClient: true);
		if (Inventory.Empty)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
	}

	private void UpdateIgnitable()
	{
		CollectibleBehaviorGroundStorable collectibleBehaviorGroundStorable = Inventory[0].Itemstack?.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>();
		if (collectibleBehaviorGroundStorable != null)
		{
			burnHoursPerItem = JsonObject.FromJson(collectibleBehaviorGroundStorable.propertiesAtString)["burnHoursPerItem"].AsFloat(burnHoursPerItem);
		}
	}

	protected ItemStack GetShatteredStack(ItemStack contents)
	{
		JsonItemStack jsonItemStack = contents.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
		if (jsonItemStack != null)
		{
			jsonItemStack.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
			if (jsonItemStack.ResolvedItemstack != null)
			{
				return jsonItemStack.ResolvedItemstack;
			}
		}
		jsonItemStack = base.Block.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
		if (jsonItemStack != null)
		{
			jsonItemStack.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
			if (jsonItemStack.ResolvedItemstack != null)
			{
				return jsonItemStack.ResolvedItemstack;
			}
		}
		return null;
	}

	public Cuboidf[] GetSelectionBoxes()
	{
		return selBoxes;
	}

	public Cuboidf[] GetCollisionBoxes()
	{
		return colBoxes;
	}

	public virtual bool OnPlayerInteractStart(IPlayer player, BlockSelection bs)
	{
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty && !activeHotbarSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>())
		{
			return false;
		}
		if (!BlockBehaviorReinforcable.AllowRightClickPickup(Api.World, Pos, player))
		{
			return false;
		}
		DetermineStorageProperties(activeHotbarSlot);
		bool flag = false;
		if (StorageProps != null)
		{
			if (!activeHotbarSlot.Empty && StorageProps.CtrlKey && !player.Entity.Controls.CtrlKey)
			{
				return false;
			}
			Vec3f vec3f = rotatedOffset(bs.HitPosition.ToVec3f(), 0f - MeshAngle);
			if (StorageProps.Layout == EnumGroundStorageLayout.Quadrants && inventory.Empty)
			{
				double num = Math.Abs((double)vec3f.X - 0.5);
				double num2 = Math.Abs((double)vec3f.Z - 0.5);
				if (num < 0.125 && num2 < 0.125)
				{
					overrideLayout = EnumGroundStorageLayout.SingleCenter;
					DetermineStorageProperties(activeHotbarSlot);
				}
			}
			switch (StorageProps.Layout)
			{
			case EnumGroundStorageLayout.SingleCenter:
				if (StorageProps.RandomizeCenterRotation)
				{
					double y = Api.World.Rand.NextDouble() * 6.28 - 3.14;
					double x = Api.World.Rand.NextDouble() * 6.28 - 3.14;
					MeshAngle = (float)Math.Atan2(y, x);
				}
				flag = putOrGetItemSingle(inventory[0], player, bs);
				break;
			case EnumGroundStorageLayout.Halves:
			case EnumGroundStorageLayout.WallHalves:
				flag = ((!((double)vec3f.X < 0.5)) ? putOrGetItemSingle(inventory[1], player, bs) : putOrGetItemSingle(inventory[0], player, bs));
				break;
			case EnumGroundStorageLayout.Quadrants:
			{
				int slotId = (((double)vec3f.X > 0.5) ? 2 : 0) + (((double)vec3f.Z > 0.5) ? 1 : 0);
				flag = putOrGetItemSingle(inventory[slotId], player, bs);
				break;
			}
			case EnumGroundStorageLayout.Stacking:
			case EnumGroundStorageLayout.Messy12:
				flag = putOrGetItemStacking(player, bs);
				break;
			}
		}
		UpdateIgnitable();
		renderer?.UpdateTemps();
		if (flag)
		{
			MarkDirty();
		}
		if (inventory.Empty && !clientsideFirstPlacement)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
		return flag;
	}

	public bool OnPlayerInteractStep(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (isUsingSlot?.Itemstack?.Collectible is IContainedInteractable containedInteractable)
		{
			return containedInteractable.OnContainedInteractStep(secondsUsed, this, isUsingSlot, byPlayer, blockSel);
		}
		isUsingSlot = null;
		return false;
	}

	public void OnPlayerInteractStop(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (isUsingSlot?.Itemstack.Collectible is IContainedInteractable containedInteractable)
		{
			containedInteractable.OnContainedInteractStop(secondsUsed, this, isUsingSlot, byPlayer, blockSel);
		}
		isUsingSlot = null;
	}

	public ItemSlot GetSlotAt(BlockSelection bs)
	{
		if (StorageProps == null)
		{
			return null;
		}
		Vec3f vec3f = rotatedOffset(bs.HitPosition.ToVec3f(), 0f - MeshAngle);
		switch (StorageProps.Layout)
		{
		case EnumGroundStorageLayout.Halves:
		case EnumGroundStorageLayout.WallHalves:
			if ((double)vec3f.X < 0.5)
			{
				return inventory[0];
			}
			return inventory[1];
		case EnumGroundStorageLayout.Quadrants:
		{
			int slotId = (((double)vec3f.X > 0.5) ? 2 : 0) + (((double)vec3f.Z > 0.5) ? 1 : 0);
			return inventory[slotId];
		}
		case EnumGroundStorageLayout.SingleCenter:
		case EnumGroundStorageLayout.Stacking:
		case EnumGroundStorageLayout.Messy12:
			return inventory[0];
		default:
			return null;
		}
	}

	public bool OnTryCreateKiln()
	{
		ItemStack itemstack = inventory.FirstNonEmptySlot.Itemstack;
		if (itemstack == null)
		{
			return false;
		}
		if (itemstack.StackSize > StorageProps.MaxFireable)
		{
			capi?.TriggerIngameError(this, "overfull", Lang.Get("Can only fire up to {0} at once.", StorageProps.MaxFireable));
			return false;
		}
		if (itemstack.Collectible.CombustibleProps == null || itemstack.Collectible.CombustibleProps.SmeltingType != EnumSmeltType.Fire)
		{
			capi?.TriggerIngameError(this, "notfireable", Lang.Get("This is not a fireable block or item", StorageProps.MaxFireable));
			return false;
		}
		return true;
	}

	public virtual void DetermineStorageProperties(ItemSlot sourceSlot)
	{
		ItemStack itemStack = inventory.FirstNonEmptySlot?.Itemstack ?? sourceSlot?.Itemstack;
		if (!forceStorageProps && StorageProps == null)
		{
			if (itemStack == null)
			{
				return;
			}
			StorageProps = itemStack.Collectible?.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
		}
		if (StorageProps != null)
		{
			if (StorageProps.CollisionBox != null)
			{
				colBoxes[0] = (selBoxes[0] = StorageProps.CollisionBox.Clone());
			}
			else if (itemStack?.Block != null)
			{
				colBoxes[0] = (selBoxes[0] = itemStack.Block.CollisionBoxes[0].Clone());
			}
			if (StorageProps.SelectionBox != null)
			{
				selBoxes[0] = StorageProps.SelectionBox.Clone();
			}
			if (StorageProps.CbScaleYByLayer != 0f)
			{
				colBoxes[0] = colBoxes[0].Clone();
				colBoxes[0].Y2 *= (int)Math.Ceiling(StorageProps.CbScaleYByLayer * (float)inventory[0].StackSize) * 8 / 8;
				selBoxes[0] = colBoxes[0];
			}
			FixBrokenStorageLayout();
			if (overrideLayout.HasValue)
			{
				StorageProps = StorageProps.Clone();
				StorageProps.Layout = overrideLayout.Value;
			}
		}
	}

	protected virtual void FixBrokenStorageLayout()
	{
		EnumGroundStorageLayout layout = StorageProps.Layout;
		bool flag = ((layout == EnumGroundStorageLayout.WallHalves || layout == EnumGroundStorageLayout.Stacking) ? true : false);
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3;
			switch (overrideLayout)
			{
			case EnumGroundStorageLayout.WallHalves:
			case EnumGroundStorageLayout.Stacking:
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		if (flag2)
		{
			overrideLayout = null;
		}
		EnumGroundStorageLayout enumGroundStorageLayout = overrideLayout ?? StorageProps.Layout;
		int num = UsableSlots(enumGroundStorageLayout);
		switch (num)
		{
		default:
			return;
		case 1:
			if (!inventory[0].Empty)
			{
				return;
			}
			break;
		case 2:
		case 3:
			break;
		}
		if (num == 2 && !inventory[0].Empty && !inventory[1].Empty)
		{
			return;
		}
		ItemSlot[] fullSlots = inventory.Where((ItemSlot slot) => !slot.Empty).ToArray();
		if (fullSlots.Length == 0)
		{
			return;
		}
		if (fullSlots.Length == 1)
		{
			inventory[0].TryFlipWith(fullSlots[0]);
			if (!inventory[0].Empty)
			{
				return;
			}
		}
		if (enumGroundStorageLayout == EnumGroundStorageLayout.Stacking && inventory.All((ItemSlot slot) => slot.Empty || slot.Itemstack.Equals(Api.World, fullSlots[0].Itemstack, GlobalConstants.IgnoredStackAttributes)))
		{
			for (int num2 = 0; num2 < fullSlots.Length; num2++)
			{
				fullSlots[num2].TryPutInto(Api.World, inventory[0]);
			}
			fullSlots = inventory.Where((ItemSlot slot) => !slot.Empty).ToArray();
			if (fullSlots.Length == 1)
			{
				return;
			}
		}
		if (num == 2 && fullSlots.Length == 2)
		{
			fullSlots[0].TryPutInto(Api.World, inventory[0]);
			fullSlots[1].TryPutInto(Api.World, inventory[1]);
			if (!inventory[0].Empty && !inventory[1].Empty)
			{
				return;
			}
		}
		if (fullSlots.Length > 2)
		{
			EnumGroundStorageLayout? enumGroundStorageLayout2 = overrideLayout;
			if (enumGroundStorageLayout2.HasValue && enumGroundStorageLayout2 == EnumGroundStorageLayout.Halves)
			{
				overrideLayout = null;
			}
		}
		if (StorageProps.Layout != EnumGroundStorageLayout.Quadrants)
		{
			layout = overrideLayout.GetValueOrDefault();
			if (!overrideLayout.HasValue)
			{
				layout = EnumGroundStorageLayout.Quadrants;
				overrideLayout = layout;
			}
		}
	}

	public int UsableSlots(EnumGroundStorageLayout layout)
	{
		return layout switch
		{
			EnumGroundStorageLayout.SingleCenter => 1, 
			EnumGroundStorageLayout.Halves => 2, 
			EnumGroundStorageLayout.WallHalves => 2, 
			EnumGroundStorageLayout.Quadrants => 4, 
			EnumGroundStorageLayout.Messy12 => 1, 
			EnumGroundStorageLayout.Stacking => 1, 
			_ => 0, 
		};
	}

	protected bool putOrGetItemStacking(IPlayer byPlayer, BlockSelection bs)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool shiftKey = byPlayer.Entity.Controls.ShiftKey;
		bool flag = inventory[0].Empty || !shiftKey || (activeHotbarSlot.Itemstack != null && activeHotbarSlot.Itemstack.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes));
		BlockPos blockPos = Pos.UpCopy();
		BlockEntityGroundStorage blockEntity = base.Block.GetBlockEntity<BlockEntityGroundStorage>(blockPos);
		if (TotalStackSize >= Capacity && ((blockEntity != null && flag) || (activeHotbarSlot.Empty && blockEntity != null && blockEntity.inventory[0].Itemstack?.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes) == true)))
		{
			return blockEntity.OnPlayerInteractStart(byPlayer, bs);
		}
		if (shiftKey && activeHotbarSlot.Empty)
		{
			return false;
		}
		if (shiftKey && TotalStackSize >= Capacity)
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos);
			if (Api.World.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block))
			{
				if (!flag && bs.Face != BlockFacing.UP)
				{
					return false;
				}
				int num = 1;
				if (StorageProps.MaxStackingHeight > 0)
				{
					BlockPos blockPos2 = Pos.Copy();
					while (true)
					{
						BlockEntityGroundStorage blockEntity2 = base.Block.GetBlockEntity<BlockEntityGroundStorage>(blockPos2.Down());
						if (blockEntity2 == null || blockEntity2.inventory[0].Itemstack?.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes) != true)
						{
							break;
						}
						num++;
					}
				}
				if (StorageProps.MaxStackingHeight < 0 || num < StorageProps.MaxStackingHeight || !flag)
				{
					BlockGroundStorage obj = block as BlockGroundStorage;
					BlockSelection blockSelection = bs.Clone();
					blockSelection.Position = Pos;
					blockSelection.Face = BlockFacing.UP;
					return obj.CreateStorage(Api.World, blockSelection, byPlayer);
				}
			}
			return false;
		}
		if (shiftKey && !flag)
		{
			return false;
		}
		lock (inventoryLock)
		{
			if (shiftKey)
			{
				return TryPutItem(byPlayer);
			}
			return TryTakeItem(byPlayer);
		}
	}

	public virtual bool TryPutItem(IPlayer player)
	{
		if (TotalStackSize >= Capacity)
		{
			return false;
		}
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null)
		{
			return false;
		}
		ItemSlot itemSlot = inventory[0];
		if (itemSlot.Empty)
		{
			bool ctrlKey = player.Entity.Controls.CtrlKey;
			if (activeHotbarSlot.TryPutInto(Api.World, itemSlot, ctrlKey ? BulkTransferQuantity : TransferQuantity) > 0)
			{
				Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound.WithPathPrefixOnce("sounds/"), (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
			}
			Api.World.Logger.Audit("{0} Put {1}x{2} into new Ground storage at {3}.", player.PlayerName, TransferQuantity, itemSlot.Itemstack.Collectible.Code, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
			return true;
		}
		if (itemSlot.Itemstack.Equals(Api.World, activeHotbarSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			bool ctrlKey2 = player.Entity.Controls.CtrlKey;
			int num = GameMath.Min(activeHotbarSlot.StackSize, ctrlKey2 ? BulkTransferQuantity : TransferQuantity, Capacity - TotalStackSize);
			int stackSize = itemSlot.Itemstack.StackSize;
			itemSlot.Itemstack.StackSize += num;
			if (stackSize + num > 0)
			{
				float temperature = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack);
				float temperature2 = activeHotbarSlot.Itemstack.Collectible.GetTemperature(Api.World, activeHotbarSlot.Itemstack);
				itemSlot.Itemstack.Collectible.SetTemperature(Api.World, itemSlot.Itemstack, (temperature * (float)stackSize + temperature2 * (float)num) / (float)(stackSize + num), delayCooldown: false);
			}
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				activeHotbarSlot.TakeOut(num);
				activeHotbarSlot.OnItemSlotModified(null);
			}
			Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound.WithPathPrefixOnce("sounds/"), (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
			Api.World.Logger.Audit("{0} Put {1}x{2} into Ground storage at {3}.", player.PlayerName, num, itemSlot.Itemstack.Collectible.Code, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
			MarkDirty();
			Cuboidf[] collisionBoxes = Api.World.BlockAccessor.GetBlock(Pos).GetCollisionBoxes(Api.World.BlockAccessor, Pos);
			if (collisionBoxes != null && collisionBoxes.Length != 0 && CollisionTester.AabbIntersect(collisionBoxes[0], Pos.X, Pos.Y, Pos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
			{
				player.Entity.SidedPos.Y += (double)collisionBoxes[0].Y2 - (player.Entity.SidedPos.Y - (double)(int)player.Entity.SidedPos.Y);
			}
			return true;
		}
		return false;
	}

	public bool TryTakeItem(IPlayer player)
	{
		int num = GameMath.Min(player.Entity.Controls.CtrlKey ? BulkTransferQuantity : TransferQuantity, TotalStackSize);
		if (inventory[0]?.Itemstack != null)
		{
			ItemStack itemStack = inventory[0].TakeOut(num);
			player.InventoryManager.TryGiveItemstack(itemStack);
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from Ground storage at {3}.", player.PlayerName, num, itemStack.Collectible.Code, Pos);
		}
		if (TotalStackSize == 0)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		else
		{
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
		Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
		MarkDirty();
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public bool putOrGetItemSingle(ItemSlot ourSlot, IPlayer player, BlockSelection bs)
	{
		isUsingSlot = null;
		if (!ourSlot.Empty && ourSlot.Itemstack.Collectible is IContainedInteractable containedInteractable && containedInteractable.OnContainedInteractStart(this, ourSlot, player, bs))
		{
			BlockGroundStorage.IsUsingContainedBlock = true;
			isUsingSlot = ourSlot;
			return true;
		}
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (ourSlot?.Itemstack?.Collectible is ILiquidInterface && activeHotbarSlot?.Itemstack?.Collectible is ILiquidInterface)
		{
			BlockLiquidContainerBase blockLiquidContainerBase = activeHotbarSlot.Itemstack.Collectible as BlockLiquidContainerBase;
			CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
			bool shiftKey = player.WorldData.EntityControls.ShiftKey;
			bool ctrlKey = player.WorldData.EntityControls.CtrlKey;
			ILiquidSource liquidSource = collectible as ILiquidSource;
			if (liquidSource != null && liquidSource.AllowHeldLiquidTransfer && !shiftKey)
			{
				ItemStack content = liquidSource.GetContent(activeHotbarSlot.Itemstack);
				int moved = blockLiquidContainerBase.TryPutLiquid(ourSlot.Itemstack, content, ctrlKey ? liquidSource.TransferSizeLitres : liquidSource.CapacityLitres);
				if (moved > 0)
				{
					blockLiquidContainerBase.SplitStackAndPerformAction(player.Entity, activeHotbarSlot, delegate(ItemStack stack)
					{
						liquidSource.TryTakeContent(stack, moved);
						return moved;
					});
					blockLiquidContainerBase.DoLiquidMovedEffects(player, content, moved, BlockLiquidContainerBase.EnumLiquidDirection.Pour);
					BlockGroundStorage.IsUsingContainedBlock = true;
					isUsingSlot = ourSlot;
					return true;
				}
			}
			ILiquidSink liquidSink = collectible as ILiquidSink;
			if (liquidSink != null && liquidSink.AllowHeldLiquidTransfer && !ctrlKey)
			{
				ItemStack owncontentStack = blockLiquidContainerBase.GetContent(ourSlot.Itemstack);
				if (owncontentStack != null)
				{
					ItemStack contentStack = owncontentStack.Clone();
					float litres = (shiftKey ? liquidSink.TransferSizeLitres : liquidSink.CapacityLitres);
					int num = blockLiquidContainerBase.SplitStackAndPerformAction(player.Entity, activeHotbarSlot, (ItemStack stack) => liquidSink.TryPutLiquid(stack, owncontentStack, litres));
					if (num > 0)
					{
						blockLiquidContainerBase.TryTakeContent(ourSlot.Itemstack, num);
						blockLiquidContainerBase.DoLiquidMovedEffects(player, contentStack, num, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
						BlockGroundStorage.IsUsingContainedBlock = true;
						isUsingSlot = ourSlot;
						return true;
					}
				}
			}
		}
		if (!activeHotbarSlot.Empty && !inventory.Empty)
		{
			EnumGroundStorageLayout? enumGroundStorageLayout = activeHotbarSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps.Layout;
			bool flag = StorageProps.Layout == enumGroundStorageLayout;
			if (StorageProps.Layout == EnumGroundStorageLayout.Quadrants && enumGroundStorageLayout == EnumGroundStorageLayout.Messy12)
			{
				flag = true;
				overrideLayout = EnumGroundStorageLayout.Quadrants;
			}
			if (!flag)
			{
				return false;
			}
		}
		lock (inventoryLock)
		{
			if (ourSlot.Empty)
			{
				if (activeHotbarSlot.Empty)
				{
					return false;
				}
				if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
				{
					ItemStack itemStack = activeHotbarSlot.Itemstack.Clone();
					itemStack.StackSize = 1;
					if (new DummySlot(itemStack).TryPutInto(Api.World, ourSlot, TransferQuantity) > 0)
					{
						Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
						Api.World.Logger.Audit("{0} Put 1x{1} into Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack.Collectible.Code, Pos);
					}
				}
				else if (activeHotbarSlot.TryPutInto(Api.World, ourSlot, TransferQuantity) > 0)
				{
					Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
					Api.World.Logger.Audit("{0} Put 1x{1} into Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack.Collectible.Code, Pos);
				}
			}
			else
			{
				if (!player.InventoryManager.TryGiveItemstack(ourSlot.Itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(ourSlot.Itemstack, Pos);
				}
				Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
				Api.World.Logger.Audit("{0} Took 1x{1} from Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack?.Collectible.Code, Pos);
				ourSlot.Itemstack = null;
				ourSlot.MarkDirty();
			}
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		clientsideFirstPlacement = false;
		forceStorageProps = tree.GetBool("forceStorageProps");
		if (forceStorageProps)
		{
			StorageProps = JsonUtil.FromString<GroundStorageProperties>(tree.GetString("storageProps"));
		}
		overrideLayout = null;
		if (tree.HasAttribute("overrideLayout"))
		{
			overrideLayout = (EnumGroundStorageLayout)tree.GetInt("overrideLayout");
		}
		if (Api != null)
		{
			DetermineStorageProperties(null);
		}
		MeshAngle = tree.GetFloat("meshAngle");
		AttachFace = BlockFacing.ALLFACES[tree.GetInt("attachFace")];
		bool flag = burning;
		burning = tree.GetBool("burning");
		burnStartTotalHours = tree.GetDouble("lastTickTotalHours");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && !flag && burning)
		{
			UpdateBurningState();
		}
		if (!burning)
		{
			if (listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			ambientSound?.Stop();
			listenerId = 0L;
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("forceStorageProps", forceStorageProps);
		if (forceStorageProps)
		{
			tree.SetString("storageProps", JsonUtil.ToString(StorageProps));
		}
		if (overrideLayout.HasValue)
		{
			tree.SetInt("overrideLayout", (int)overrideLayout.Value);
		}
		tree.SetBool("burning", burning);
		tree.SetDouble("lastTickTotalHours", burnStartTotalHours);
		tree.SetFloat("meshAngle", MeshAngle);
		tree.SetInt("attachFace", AttachFace?.Index ?? 0);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public virtual string GetBlockName()
	{
		if (StorageProps == null || inventory.Empty)
		{
			return Lang.Get("Empty pile");
		}
		string[] contentSummary = getContentSummary();
		if (contentSummary.Length == 1)
		{
			ItemSlot firstNonEmptySlot = inventory.FirstNonEmptySlot;
			ItemStack itemstack = firstNonEmptySlot.Itemstack;
			int num = inventory.Sum((ItemSlot s) => s.StackSize);
			string text = firstNonEmptySlot.Itemstack.Collectible.GetCollectibleInterface<IContainedCustomName>()?.GetContainedName(firstNonEmptySlot, num);
			if (text != null)
			{
				return text;
			}
			if (num == 1)
			{
				return itemstack.GetName();
			}
			return contentSummary[0];
		}
		return Lang.Get("Ground Storage");
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (inventory.Empty)
		{
			return;
		}
		string[] contentSummary = getContentSummary();
		ItemStack itemstack = inventory.FirstNonEmptySlot.Itemstack;
		if (contentSummary.Length == 1 && itemstack.Collectible.GetCollectibleInterface<IContainedCustomName>() == null && itemstack.Class == EnumItemClass.Block && ((Block)itemstack.Collectible).EntityClass == null)
		{
			string placedBlockInfo = itemstack.Block.GetPlacedBlockInfo(Api.World, Pos, forPlayer);
			if (placedBlockInfo != null && placedBlockInfo.Length > 0)
			{
				dsc.Append(placedBlockInfo);
			}
		}
		else
		{
			string[] array = contentSummary;
			foreach (string value in array)
			{
				dsc.AppendLine(value);
			}
		}
		if (inventory.Empty)
		{
			return;
		}
		foreach (ItemSlot item in inventory)
		{
			float num = item.Itemstack?.Collectible.GetTemperature(Api.World, item.Itemstack) ?? 0f;
			if (num > 20f)
			{
				float num2 = item.Itemstack?.Attributes.GetFloat("hoursHeatReceived") ?? 0f;
				dsc.AppendLine(Lang.Get("temperature-precise", num));
				if (num2 > 0f)
				{
					dsc.AppendLine(Lang.Get("Fired for {0:0.##} hours", num2));
				}
			}
		}
	}

	public virtual string[] getContentSummary()
	{
		OrderedDictionary<string, int> orderedDictionary = new OrderedDictionary<string, int>();
		foreach (ItemSlot item in inventory)
		{
			if (!item.Empty)
			{
				string name = item.Itemstack.GetName();
				name = item.Itemstack.Collectible.GetCollectibleInterface<IContainedCustomName>()?.GetContainedInfo(item) ?? name;
				if (!orderedDictionary.TryGetValue(name, out var value))
				{
					value = 0;
				}
				orderedDictionary[name] = value + item.StackSize;
			}
		}
		return orderedDictionary.Select((KeyValuePair<string, int> elem) => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		float num = 0f;
		if (!Inventory.Empty)
		{
			foreach (ItemSlot item in Inventory)
			{
				num = Math.Max(num, item.Itemstack?.Collectible.GetTemperature(capi.World, item.Itemstack) ?? 0f);
			}
		}
		UseRenderer = num >= 500f;
		if (renderer == null && num >= 450f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				if (renderer == null)
				{
					renderer = new GroundStorageRenderer(capi, this);
				}
			}, "groundStorageRendererE");
		}
		if (UseRenderer)
		{
			return true;
		}
		if (renderer != null && num < 450f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				if (renderer != null)
				{
					renderer.Dispose();
					renderer = null;
				}
			}, "groundStorageRendererD");
		}
		NeedsRetesselation = false;
		lock (inventoryLock)
		{
			return base.OnTesselation(mesher, tesselator);
		}
	}

	private Vec3f rotatedOffset(Vec3f offset, float radY)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.Translate(0.5f, 0.5f, 0.5f).RotateY(radY).Translate(-0.5f, -0.5f, -0.5f);
		return matrixf.TransformVector(new Vec4f(offset.X, offset.Y, offset.Z, 1f)).XYZ;
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[DisplayedItems][];
		Vec3f[] array2 = new Vec3f[DisplayedItems];
		lock (inventoryLock)
		{
			GetLayoutOffset(array2);
		}
		for (int i = 0; i < array.Length; i++)
		{
			Vec3f vec3f = array2[i];
			vec3f = new Matrixf().RotateY(MeshAngle).TransformVector(vec3f.ToVec4f(0f)).XYZ;
			array[i] = new Matrixf().Translate(vec3f.X, vec3f.Y, vec3f.Z).Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array;
	}

	public void GetLayoutOffset(Vec3f[] offs)
	{
		switch (StorageProps.Layout)
		{
		case EnumGroundStorageLayout.SingleCenter:
		case EnumGroundStorageLayout.Stacking:
		case EnumGroundStorageLayout.Messy12:
			offs[0] = new Vec3f();
			break;
		case EnumGroundStorageLayout.Halves:
		case EnumGroundStorageLayout.WallHalves:
			offs[0] = new Vec3f(-0.25f, 0f, 0f);
			offs[1] = new Vec3f(0.25f, 0f, 0f);
			break;
		case EnumGroundStorageLayout.Quadrants:
			offs[0] = new Vec3f(-0.25f, 0f, -0.25f);
			offs[1] = new Vec3f(-0.25f, 0f, 0.25f);
			offs[2] = new Vec3f(0.25f, 0f, -0.25f);
			offs[3] = new Vec3f(0.25f, 0f, 0.25f);
			break;
		}
	}

	protected override string getMeshCacheKey(ItemStack stack)
	{
		return ((StorageProps.Layout == EnumGroundStorageLayout.Messy12) ? "messy12-" : "") + ((!(StorageProps.ModelItemsToStackSizeRatio > 0f)) ? 1 : stack.StackSize) + "x" + base.getMeshCacheKey(stack);
	}

	protected override MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		if (stack.Class == EnumItemClass.Block)
		{
			if (stack.Block is IBlockMealContainer blockMealContainer)
			{
				MealMeshCache modSystem = capi.ModLoader.GetModSystem<MealMeshCache>();
				int mealHashCode = modSystem.GetMealHashCode(stack);
				if (capi.ObjectCache.TryGetValue("cookedMeshRefs", out var value) && !((value as Dictionary<int, MultiTextureMeshRef>) ?? new Dictionary<int, MultiTextureMeshRef>()).TryGetValue(mealHashCode, out MeshRefs[index]))
				{
					if (blockMealContainer is BlockCookedContainer blockCookedContainer)
					{
						MeshRefs[index] = modSystem.GetOrCreateMealInContainerMeshRef(stack.Block, blockMealContainer.GetCookingRecipe(capi.World, stack), blockMealContainer.GetNonEmptyContents(capi.World, stack), new Vec3f(0f, blockCookedContainer.yoff / 16f, 0f));
					}
					else
					{
						MeshRefs[index] = modSystem.GetOrCreateMealInContainerMeshRef(stack.Block, blockMealContainer.GetCookingRecipe(capi.World, stack), blockMealContainer.GetNonEmptyContents(capi.World, stack));
					}
				}
			}
			else
			{
				MeshRefs[index] = capi.TesselatorManager.GetDefaultBlockMeshRef(stack.Block);
			}
		}
		else if (stack.Class == EnumItemClass.Item && StorageProps.Layout != EnumGroundStorageLayout.Stacking)
		{
			MeshRefs[index] = capi.TesselatorManager.GetDefaultItemMeshRef(stack.Item);
		}
		if (StorageProps.Layout == EnumGroundStorageLayout.Messy12)
		{
			string meshCacheKey = getMeshCacheKey(stack);
			MeshData mesh = getMesh(stack);
			if (mesh != null)
			{
				return mesh;
			}
			MeshData defaultMesh = getDefaultMesh(stack);
			applyDefaultTranforms(stack, defaultMesh);
			Random random = new Random(0);
			MeshData meshData = defaultMesh.Clone().Clear();
			for (int i = 0; i < stack.StackSize; i++)
			{
				float xOffset = (float)random.NextDouble() * 0.9f - 0.45f;
				float zOffset = (float)random.NextDouble() * 0.9f - 0.45f;
				defaultMesh.Rotate(new Vec3f(0.5f, 0f, 0.5f), 0f, (float)Math.PI * 2f * (float)random.NextDouble(), 0f);
				float num = 1f + ((float)random.NextDouble() * 0.02f - 0.01f);
				defaultMesh.Scale(new Vec3f(0.5f, 0f, 0.5f), 1f * num, 1f * num, 1f * num);
				meshData.AddMeshData(defaultMesh, xOffset, 0f, zOffset);
			}
			base.MeshCache[meshCacheKey] = meshData;
			return meshData;
		}
		if (StorageProps.Layout == EnumGroundStorageLayout.Stacking)
		{
			string meshCacheKey2 = getMeshCacheKey(stack);
			MeshData modeldata = getMesh(stack);
			if (modeldata != null)
			{
				UploadedMeshCache.TryGetValue(meshCacheKey2, out MeshRefs[index]);
				return modeldata;
			}
			AssetLocation shapePath = StorageProps.StackingModel.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			nowTesselatingShape = Shape.TryGet(capi, shapePath);
			nowTesselatingObj = stack.Collectible;
			if (nowTesselatingShape == null)
			{
				capi.Logger.Error(string.Concat("Stacking model shape for collectible ", stack.Collectible.Code, " not found. Block will be invisible!"));
				return null;
			}
			capi.Tesselator.TesselateShape("storagePile", nowTesselatingShape, out modeldata, this, null, 0, 0, 0, (int)Math.Ceiling(StorageProps.ModelItemsToStackSizeRatio * (float)stack.StackSize));
			base.MeshCache[meshCacheKey2] = modeldata;
			if (UploadedMeshCache.TryGetValue(meshCacheKey2, out var value2))
			{
				value2.Dispose();
			}
			UploadedMeshCache[meshCacheKey2] = capi.Render.UploadMultiTextureMesh(modeldata);
			MeshRefs[index] = UploadedMeshCache[meshCacheKey2];
			return modeldata;
		}
		MeshData orCreateMesh = base.getOrCreateMesh(stack, index);
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes[AttributeTransformCode].Exists)
		{
			ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
			ModelTransformsRenderer[index] = modelTransform;
			return orCreateMesh;
		}
		ModelTransformsRenderer[index] = null;
		return orCreateMesh;
	}

	public void TryIgnite()
	{
		if (!burning && CanIgnite)
		{
			burning = true;
			burnStartTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty();
			UpdateBurningState();
		}
	}

	public void Extinguish()
	{
		if (burning)
		{
			burning = false;
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.0, null, randomizePitch: false, 16f);
		}
	}

	public float GetHoursLeft(double startTotalHours)
	{
		double num = startTotalHours - burnStartTotalHours;
		return (float)((double)((float)inventory[0].StackSize / 2f * burnHoursPerItem) - num);
	}

	private void UpdateBurningState()
	{
		if (!burning)
		{
			return;
		}
		if (Api.World.Side == EnumAppSide.Client)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/held/torch-idle.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				if (ambientSound != null)
				{
					ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
					ambientSound.Start();
				}
			}
			listenerId = RegisterGameTickListener(OnBurningTickClient, 100);
		}
		else
		{
			listenerId = RegisterGameTickListener(OnBurningTickServer, 10000);
		}
	}

	private void OnBurningTickClient(float dt)
	{
		if (!burning || !(Api.World.Rand.NextDouble() < 0.93))
		{
			return;
		}
		float num = (float)Layers / ((float)StorageProps.StackingCapacity * StorageProps.ModelItemsToStackSizeRatio);
		Vec3d vec3d = Pos.ToVec3d().Add(0.0, num, 0.0);
		Random rand = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			AdvancedParticleProperties advancedParticleProperties = Entity.FireParticleProps[i];
			advancedParticleProperties.Velocity[1].avg = (float)(rand.NextDouble() - 0.5);
			advancedParticleProperties.basePos.Set(vec3d.X + 0.5, vec3d.Y, vec3d.Z + 0.5);
			if (i == 0)
			{
				advancedParticleProperties.Quantity.avg = 1f;
			}
			if (i == 1)
			{
				advancedParticleProperties.Quantity.avg = 2f;
				advancedParticleProperties.PosOffset[0].var = 0.39f;
				advancedParticleProperties.PosOffset[1].var = 0.39f;
				advancedParticleProperties.PosOffset[2].var = 0.39f;
			}
			Api.World.SpawnParticles(advancedParticleProperties);
		}
	}

	private void OnBurningTickServer(float dt)
	{
		facings.Shuffle(Api.World.Rand);
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		BlockFacing[] array = facings;
		foreach (BlockFacing facing in array)
		{
			BlockPos blockPos = Pos.AddCopy(facing);
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(blockPos);
			if (blockEntity is BlockEntityCoalPile blockEntityCoalPile)
			{
				blockEntityCoalPile.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (blockEntity is BlockEntityGroundStorage blockEntityGroundStorage)
			{
				blockEntityGroundStorage.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (Api.World.Config.GetBool("allowFireSpread") && 0.5 > Api.World.Rand.NextDouble() && behavior.TrySpreadTo(blockPos) && Api.World.Rand.NextDouble() < 0.75)
			{
				break;
			}
		}
		bool flag = false;
		while (Api.World.Calendar.TotalHours - burnStartTotalHours > (double)burnHoursPerItem)
		{
			burnStartTotalHours += burnHoursPerItem;
			inventory[0].TakeOut(1);
			if (inventory[0].Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				break;
			}
			flag = true;
		}
		if (flag)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
		AttachFace = BlockFacing.ALLFACES[tree.GetInt("attachFace")];
		AttachFace = AttachFace.FaceWhenRotatedBy(0f, (float)(-degreeRotation) * ((float)Math.PI / 180f), 0f);
		tree.SetInt("attachFace", AttachFace?.Index ?? 0);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		Dispose();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		Dispose();
	}

	protected virtual void Dispose()
	{
		if (UploadedMeshCache != null)
		{
			foreach (MultiTextureMeshRef value in UploadedMeshCache.Values)
			{
				value?.Dispose();
			}
		}
		renderer?.Dispose();
		ambientSound?.Stop();
	}

	public virtual float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return IsBurning ? 10 : 0;
	}
}
