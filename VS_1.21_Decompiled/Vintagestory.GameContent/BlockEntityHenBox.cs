using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityHenBox : BlockEntityDisplay, IAnimalNest, IPointOfInterest
{
	protected InventoryGeneric inventory;

	public string inventoryClassName = "nestbox";

	public Entity occupier;

	protected double timeToIncubate;

	protected double occupiedTimeLast;

	protected bool IsOccupiedClientside;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => inventoryClassName;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "nest";

	protected int Capacity => base.Block.Attributes?["quantitySlots"]?.AsInt(1) ?? 1;

	public virtual float DistanceWeighting => 2 / (CountEggs() + 2);

	public BlockEntityHenBox()
	{
		container = new ConstantPerishRateContainer(() => Inventory, "inventory");
	}

	public virtual bool IsSuitableFor(Entity entity, string[] nestTypes)
	{
		return nestTypes?.Contains(((BlockHenbox)base.Block).NestType) ?? false;
	}

	public bool Occupied(Entity entity)
	{
		if (occupier != null)
		{
			return occupier != entity;
		}
		return false;
	}

	public virtual void SetOccupier(Entity entity)
	{
		if (occupier != entity)
		{
			occupier = entity;
			MarkDirty();
		}
	}

	public virtual bool TryAddEgg(ItemStack egg)
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			if (inventory[i].Empty)
			{
				inventory[i].Itemstack = egg;
				inventory.DidModifyItemSlot(inventory[i]);
				double? num = (egg.Attributes["chick"] as TreeAttribute)?.GetDouble("incubationDays");
				if (num.HasValue)
				{
					timeToIncubate = Math.Max(timeToIncubate, num.Value);
				}
				occupiedTimeLast = Api.World.Calendar.TotalDays;
				MarkDirty();
				return true;
			}
		}
		return false;
	}

	public int CountEggs()
	{
		int num = 0;
		for (int i = 0; i < inventory.Count; i++)
		{
			if (!inventory[i].Empty)
			{
				num++;
			}
		}
		return num;
	}

	protected virtual void On1500msTick(float dt)
	{
		if (timeToIncubate == 0.0)
		{
			return;
		}
		double totalDays = Api.World.Calendar.TotalDays;
		if (occupier != null && occupier.Alive && totalDays > occupiedTimeLast)
		{
			timeToIncubate -= totalDays - occupiedTimeLast;
			MarkDirty();
		}
		occupiedTimeLast = totalDays;
		if (!(timeToIncubate <= 0.0))
		{
			return;
		}
		timeToIncubate = 0.0;
		Random rand = Api.World.Rand;
		for (int i = 0; i < inventory.Count; i++)
		{
			TreeAttribute treeAttribute = (TreeAttribute)(inventory[i].Itemstack?.Attributes["chick"]);
			if (treeAttribute == null)
			{
				continue;
			}
			string text = treeAttribute.GetString("code");
			if (text == null || text == "")
			{
				continue;
			}
			EntityProperties entityType = Api.World.GetEntityType(text);
			if (entityType == null)
			{
				continue;
			}
			Entity entity = Api.World.ClassRegistry.CreateEntity(entityType);
			if (entity != null)
			{
				entity.ServerPos.SetFrom(new EntityPos(Position.X + (rand.NextDouble() - 0.5) / 5.0, Position.Y, Position.Z + (rand.NextDouble() - 0.5) / 5.0, (float)rand.NextDouble() * ((float)Math.PI * 2f)));
				entity.ServerPos.Motion.X += (rand.NextDouble() - 0.5) / 200.0;
				entity.ServerPos.Motion.Z += (rand.NextDouble() - 0.5) / 200.0;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "reproduction");
				entity.WatchedAttributes.SetInt("generation", treeAttribute.GetInt("generation"));
				if (entity is EntityAgent entityAgent)
				{
					entityAgent.HerdId = treeAttribute.GetLong("herdID", 0L);
				}
				Api.World.SpawnEntity(entity);
				inventory[i].Itemstack = null;
				inventory.DidModifyItemSlot(inventory[i]);
			}
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		inventoryClassName = base.Block.Attributes?["inventoryClassName"]?.AsString() ?? inventoryClassName;
		int capacity = Capacity;
		if (inventory == null)
		{
			CreateInventory(capacity, api);
		}
		else if (capacity != inventory.Count)
		{
			api.Logger.Warning(string.Concat("Nest ", base.Block.Code, " loaded with ", inventory.Count.ToString(), " capacity when it should be ", capacity.ToString(), "."));
			InventoryGeneric inventoryGeneric = inventory;
			CreateInventory(capacity, api);
			int i = 0;
			for (int j = 0; j < capacity; j++)
			{
				for (; i < inventoryGeneric.Count && inventoryGeneric[i].Empty; i++)
				{
				}
				if (i < inventoryGeneric.Count)
				{
					inventory[j].Itemstack = inventoryGeneric[i].Itemstack;
					inventory.DidModifyItemSlot(inventory[j]);
					i++;
				}
			}
		}
		base.Initialize(api);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		int num = -1;
		if (base.Block.Code.Path.EndsWith("empty"))
		{
			num = 0;
		}
		else if (base.Block.Code.Path.EndsWith("1egg"))
		{
			num = 1;
		}
		else if (base.Block.Code.Path.EndsWith("eggs"))
		{
			int num2 = base.Block.LastCodePart()[0];
			num = ((num2 <= 57 && num2 >= 48) ? (num2 - 48) : 0);
		}
		if (num >= 0)
		{
			Block block = api.World.GetBlock(new AssetLocation(base.Block.FirstCodePart()));
			api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
			MarkDirty();
		}
		for (int k = 0; k < num; k++)
		{
			ItemSlot itemSlot = inventory[k];
			if (itemSlot.Itemstack == null)
			{
				ItemStack itemStack = (itemSlot.Itemstack = new ItemStack(api.World.GetItem("egg-chicken-raw")));
			}
			inventory.DidModifyItemSlot(inventory[k]);
		}
		IsOccupiedClientside = false;
		api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		RegisterGameTickListener(On1500msTick, 1500);
	}

	protected void CreateInventory(int capacity, ICoreAPI api)
	{
		inventory = new InventoryGeneric(capacity, InventoryClassName, Pos?.ToString(), api);
		inventory.Pos = Pos;
		inventory.SlotModified += OnSlotModified;
	}

	protected virtual void OnSlotModified(int slot)
	{
		MarkDirty();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("inc", timeToIncubate);
		tree.SetDouble("occ", occupiedTimeLast);
		tree.SetBool("isOccupied", occupier != null && occupier.Alive);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		TreeAttribute treeAttribute = (TreeAttribute)tree["inventory"];
		if (inventory == null)
		{
			int capacity = treeAttribute?.GetInt("qslots") ?? Capacity;
			CreateInventory(capacity, worldForResolving.Api);
		}
		base.FromTreeAttributes(tree, worldForResolving);
		timeToIncubate = tree.GetDouble("inc");
		occupiedTimeLast = tree.GetDouble("occ");
		for (int i = 0; i < 10; i++)
		{
			string text = tree.GetString("chick" + i);
			if (text != null)
			{
				int value = tree.GetInt("gen" + i);
				inventory[i].Itemstack = new ItemStack(worldForResolving.GetItem("egg-chicken-raw"));
				TreeAttribute treeAttribute2 = new TreeAttribute();
				treeAttribute2.SetString("code", text);
				treeAttribute2.SetInt("generation", value);
				inventory[i].Itemstack.Attributes["chick"] = treeAttribute2;
				inventory.DidModifyItemSlot(inventory[i]);
			}
		}
		IsOccupiedClientside = tree.GetBool("isOccupied");
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public virtual bool CanPlayerPlaceItem(ItemStack itemstack)
	{
		return false;
	}

	public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (CanPlayerPlaceItem(activeHotbarSlot.Itemstack))
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (inventory[i].Empty)
				{
					AssetLocation assetLocation = activeHotbarSlot.Itemstack?.Block?.Sounds?.Place;
					AssetLocation assetLocation2 = activeHotbarSlot.Itemstack?.Collectible?.Code;
					ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, 1);
					if (activeHotbarSlot.TryPutInto(inventory[i], ref op) > 0)
					{
						Api.Logger.Audit(string.Concat(byPlayer.PlayerName, " put 1x", assetLocation2, " into ", base.Block.Code, " at ", Pos?.ToString()));
						Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
						return true;
					}
				}
			}
			return false;
		}
		bool flag = false;
		for (int j = 0; j < inventory.Count; j++)
		{
			if (inventory[j].Empty)
			{
				continue;
			}
			string text = inventory[j].Itemstack.Collectible?.Code;
			int stackSize = inventory[j].Itemstack.StackSize;
			bool flag2 = byPlayer.InventoryManager.TryGiveItemstack(inventory[j].Itemstack);
			int num = stackSize - (inventory[j].Itemstack?.StackSize ?? 0);
			if (flag2)
			{
				if (inventory[j].Itemstack != null && stackSize == inventory[j].Itemstack.StackSize)
				{
					inventory[j].TakeOutWhole();
					num = stackSize;
				}
				flag = true;
				world.Api.Logger.Audit(string.Concat(byPlayer.PlayerName, " took ", num.ToString(), "x ", text, " from ", base.Block.Code, " at ", Pos?.ToString()));
				inventory.DidModifyItemSlot(inventory[j]);
			}
			if (inventory[j].Itemstack != null && inventory[j].Itemstack.StackSize == 0)
			{
				if (!flag2)
				{
					world.Api.Logger.Audit(string.Concat(byPlayer.PlayerName, " voided ", num.ToString(), "x ", text, " from ", base.Block.Code, " at ", Pos?.ToString()));
				}
				inventory[j].Itemstack = null;
				inventory.DidModifyItemSlot(inventory[j]);
			}
		}
		if (flag)
		{
			world.PlaySoundAt(new AssetLocation("sounds/player/collect"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
		}
		return flag;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < inventory.Count; i++)
		{
			if (!inventory[i].Empty)
			{
				num++;
				if (((TreeAttribute)inventory[i].Itemstack.Attributes["chick"])?.GetString("code") != null)
				{
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			if (num2 > 1)
			{
				dsc.AppendLine(Lang.Get("{0} fertile eggs", num2));
			}
			else
			{
				dsc.AppendLine(Lang.Get("1 fertile egg"));
			}
			if (timeToIncubate >= 1.5)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} days", timeToIncubate));
			}
			else if (timeToIncubate >= 0.75)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: 1 day"));
			}
			else if (timeToIncubate > 0.0)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} hours", timeToIncubate * 24.0));
			}
			if (!IsOccupiedClientside && num >= inventory.Count)
			{
				dsc.AppendLine(Lang.Get("A broody hen is needed!"));
			}
		}
		else if (num > 0)
		{
			dsc.AppendLine(Lang.Get("No eggs are fertilized"));
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		ModelTransform[] array = base.Block.Attributes?["displayTransforms"]?.AsArray<ModelTransform>();
		if (array == null)
		{
			capi.Logger.Warning(string.Concat("No display transforms found for ", base.Block.Code, ", placed items may be invisible or in the wrong location."));
			array = new ModelTransform[DisplayedItems];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new ModelTransform();
			}
		}
		if (array.Length != DisplayedItems)
		{
			capi.Logger.Warning(string.Concat("Display transforms for ", base.Block.Code, " block entity do not match number of displayed items, later placed items may be invisible or in the wrong location. Items: ", DisplayedItems.ToString(), ", transforms: ", array.Length.ToString()));
		}
		float[][] array2 = new float[array.Length][];
		for (int j = 0; j < array.Length; j++)
		{
			FastVec3f translation = array[j].Translation;
			FastVec3f rotation = array[j].Rotation;
			array2[j] = new Matrixf().Translate(translation.X, translation.Y, translation.Z).Translate(0.5f, 0f, 0.5f).RotateX(rotation.X * ((float)Math.PI / 180f))
				.RotateY(rotation.Y * ((float)Math.PI / 180f))
				.RotateZ(rotation.Z * ((float)Math.PI / 180f))
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array2;
	}
}
