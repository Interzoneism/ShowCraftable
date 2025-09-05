using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorHarvestable : EntityBehaviorContainer
{
	private const float minimumWeight = 0.5f;

	protected BlockDropItemStack[] jsonDrops;

	protected InventoryGeneric inv;

	protected GuiDialogCreatureContents dlg;

	private float baseHarvestDuration;

	private bool harshWinters;

	private bool fixedWeight;

	private float accum;

	private WorldInteraction[] interactions;

	public override bool ThreadSafe => true;

	private bool GotCrushed
	{
		get
		{
			if (!entity.WatchedAttributes.HasAttribute("deathReason") || entity.WatchedAttributes.GetInt("deathReason") != 2)
			{
				if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
				{
					return entity.WatchedAttributes.GetInt("deathDamageType") == 9;
				}
				return false;
			}
			return true;
		}
	}

	private bool GotElectrocuted
	{
		get
		{
			if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
			{
				return entity.WatchedAttributes.GetInt("deathDamageType") == 11;
			}
			return false;
		}
	}

	private bool GotAcidified
	{
		get
		{
			if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
			{
				return entity.WatchedAttributes.GetInt("deathDamageType") == 14;
			}
			return false;
		}
	}

	public float AnimalWeight
	{
		get
		{
			return entity.WatchedAttributes.GetFloat("animalWeight", 1f);
		}
		set
		{
			entity.WatchedAttributes.SetFloat("animalWeight", value);
		}
	}

	public double LastWeightUpdateTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastWeightUpdateTotalHours", 1.0);
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastWeightUpdateTotalHours", value);
		}
	}

	protected float dropQuantityMultiplier
	{
		get
		{
			if (GotCrushed)
			{
				return 0.5f;
			}
			if (GotAcidified)
			{
				return 0.25f;
			}
			string text = entity.WatchedAttributes.GetString("deathByEntity");
			if (text != null && !entity.WatchedAttributes.HasAttribute("deathByPlayer"))
			{
				return Api.World.GetEntityType(new AssetLocation(text))?.Attributes?["deathByMultiplier"]?.AsFloat(0.4f) ?? 0.4f;
			}
			return 1f;
		}
	}

	public bool Harvestable
	{
		get
		{
			if (!entity.Alive)
			{
				return !IsHarvested;
			}
			return false;
		}
	}

	public bool IsHarvested => entity.WatchedAttributes.GetBool("harvested");

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "harvestableInv";

	public bool DropsGenerated
	{
		get
		{
			return entity.WatchedAttributes.GetBool("dropsgenerated");
		}
		set
		{
			entity.WatchedAttributes.SetBool("dropsgenerated", value);
		}
	}

	public float GetHarvestDuration(Entity forEntity)
	{
		return baseHarvestDuration * forEntity.Stats.GetBlended("animalHarvestingTime");
	}

	public EntityBehaviorHarvestable(Entity entity)
		: base(entity)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			entity.WatchedAttributes.RegisterModifiedListener("harvestableInv", onDropsModified);
		}
		harshWinters = entity.World.Config.GetString("harshWinters").ToBool(defaultValue: true);
	}

	public override void AfterInitialized(bool onSpawn)
	{
		if (onSpawn)
		{
			LastWeightUpdateTotalHours = Math.Max(1.0, entity.World.Calendar.TotalHours - 168.0);
			AnimalWeight = (fixedWeight ? 1f : (0.66f + 0.2f * (float)entity.World.Rand.NextDouble()));
		}
		else if (fixedWeight)
		{
			AnimalWeight = 1f;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.Side != EnumAppSide.Server)
		{
			return;
		}
		accum += deltaTime;
		if (!(accum > 1.5f))
		{
			return;
		}
		accum = 0f;
		if (!harshWinters || fixedWeight)
		{
			AnimalWeight = 1f;
			return;
		}
		double totalHours = entity.World.Calendar.TotalHours;
		double num = LastWeightUpdateTotalHours;
		double num2 = entity.World.Calendar.HoursPerDay;
		totalHours = Math.Min(totalHours, num + num2 * (double)entity.World.Calendar.DaysPerMonth);
		if (num < totalHours - 1.0)
		{
			double num3 = entity.WatchedAttributes.GetDouble("lastMealEatenTotalHours", -9999.0);
			double num4 = (double)(4 * entity.World.Calendar.DaysPerMonth) * num2;
			double num5 = 7.0 * num2;
			BlockPos asBlockPos = entity.Pos.AsBlockPos;
			float num6 = AnimalWeight;
			float num7 = num6;
			float num8 = 3f;
			float temperature = 0f;
			ClimateCondition climateCondition = null;
			do
			{
				num += (double)num8;
				double num9 = num - num3;
				if (num9 < 0.0)
				{
					num9 = num4;
				}
				if (!(num9 < num4))
				{
					if (num6 <= 0.5f)
					{
						num = totalHours;
						break;
					}
					if (climateCondition == null)
					{
						climateCondition = entity.World.BlockAccessor.GetClimateAt(asBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, num / num2);
						if (climateCondition == null)
						{
							base.OnGameTick(deltaTime);
							return;
						}
						temperature = climateCondition.WorldGenTemperature;
					}
					else
					{
						climateCondition.Temperature = temperature;
						entity.World.BlockAccessor.GetClimateAt(asBlockPos, climateCondition, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, num / num2);
					}
					if (climateCondition.Temperature <= 0f)
					{
						num6 = Math.Max(0.5f, num6 - num8 * 0.001f);
					}
				}
				else
				{
					bool flag = num9 < num5;
					num6 = Math.Min(1f, num6 + num8 * (0.001f + (flag ? 0.05f : 0f)));
				}
			}
			while (num < totalHours - 1.0);
			if (num6 != num7)
			{
				AnimalWeight = num6;
			}
		}
		LastWeightUpdateTotalHours = num;
	}

	private void Inv_SlotModified(int slotid)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		inv.ToTreeAttributes(treeAttribute);
		entity.WatchedAttributes["harvestableInv"] = treeAttribute;
		entity.WatchedAttributes.MarkPathDirty("harvestableInv");
	}

	private void Inv_OnInventoryClosed(IPlayer player)
	{
		if (inv.Empty && entity.GetBehavior<EntityBehaviorDeadDecay>() != null)
		{
			entity.GetBehavior<EntityBehaviorDeadDecay>().DecayNow();
		}
	}

	private void onDropsModified()
	{
		if (entity.WatchedAttributes["harvestableInv"] is TreeAttribute treeAttribute)
		{
			int num = treeAttribute.GetInt("qslots") - inv.Count;
			inv.AddSlots(num);
			inv.FromTreeAttributes(treeAttribute);
			if (num > 0)
			{
				GuiDialogCreatureContents guiDialogCreatureContents = dlg;
				if (guiDialogCreatureContents != null && guiDialogCreatureContents.IsOpened())
				{
					dlg.Compose("carcasscontents");
				}
			}
		}
		entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.XYZ.AsBlockPos)?.MarkModified();
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		inv = new InventoryGeneric(typeAttributes["quantitySlots"].AsInt(4), "harvestableContents-" + entity.EntityId, entity.Api);
		if (entity.WatchedAttributes["harvestableInv"] is TreeAttribute tree)
		{
			inv.FromTreeAttributes(tree);
		}
		inv.PutLocked = true;
		if (entity.World.Side == EnumAppSide.Server)
		{
			inv.SlotModified += Inv_SlotModified;
			inv.OnInventoryClosed += Inv_OnInventoryClosed;
		}
		base.Initialize(properties, typeAttributes);
		if (entity.World.Side == EnumAppSide.Server)
		{
			jsonDrops = typeAttributes["drops"].AsObject<BlockDropItemStack[]>();
		}
		baseHarvestDuration = typeAttributes["duration"].AsFloat(5f);
		fixedWeight = typeAttributes["fixedweight"].AsBool();
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		_ = entity.Pos.XYZ;
		bool flag = entity.Pos.XYZ.Add(hitPosition).DistanceTo(byEntity.SidedPos.XYZ.Add(byEntity.LocalEyePos)) <= (float)(5 + ((Api.Side == EnumAppSide.Server) ? 1 : 0));
		if (!IsHarvested || !flag)
		{
			return;
		}
		EntityPlayer entityPlayer = byEntity as EntityPlayer;
		IPlayer player = entity.World.PlayerByUid(entityPlayer.PlayerUID);
		player.InventoryManager.OpenInventory(inv);
		if (entity.World.Side == EnumAppSide.Client && dlg == null)
		{
			dlg = new GuiDialogCreatureContents(inv, entity, entity.Api as ICoreClientAPI, "carcasscontents");
			if (dlg.TryOpen())
			{
				(entity.World.Api as ICoreClientAPI).Network.SendPacketClient(inv.Open(player));
			}
			dlg.OnClosed += delegate
			{
				dlg.Dispose();
				dlg = null;
			};
		}
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		if (packetid < 1000 && inv.HasOpened(player))
		{
			inv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			handled = EnumHandling.PreventSubsequent;
		}
	}

	public void SetHarvested(IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!entity.WatchedAttributes.GetBool("harvested"))
		{
			entity.WatchedAttributes.SetBool("harvested", value: true);
			GenerateDrops(byPlayer);
		}
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		if (DropsGenerated && inv.Empty && entity.GetBehavior<EntityBehaviorDeadDecay>() != null)
		{
			entity.WatchedAttributes.SetBool("harvested", value: true);
		}
	}

	public void GenerateDrops(IPlayer byPlayer)
	{
		if (entity.World.Side == EnumAppSide.Client || DropsGenerated)
		{
			return;
		}
		DropsGenerated = true;
		float num = 1f;
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes == null || !attributes["isMechanical"].AsBool())
		{
			num *= byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
		}
		List<ItemStack> todrop = new List<ItemStack>();
		for (int i = 0; i < jsonDrops.Length; i++)
		{
			BlockDropItemStack blockDropItemStack = jsonDrops[i];
			if (blockDropItemStack.Tool.HasValue && (byPlayer == null || blockDropItemStack.Tool != byPlayer.InventoryManager.ActiveTool))
			{
				continue;
			}
			blockDropItemStack.Resolve(entity.World, "BehaviorHarvestable ", entity.Code);
			float num2 = 1f;
			if (blockDropItemStack.DropModbyStat != null)
			{
				num2 = (byPlayer?.Entity?.Stats.GetBlended(blockDropItemStack.DropModbyStat)).GetValueOrDefault();
			}
			if (blockDropItemStack.ResolvedItemstack.Collectible.NutritionProps != null || blockDropItemStack.ResolvedItemstack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible?.NutritionProps != null)
			{
				num2 *= AnimalWeight;
			}
			ItemStack itemStack = blockDropItemStack.GetNextItemStack(dropQuantityMultiplier * num * num2);
			if (itemStack != null && itemStack.StackSize != 0)
			{
				if (itemStack.Collectible is IResolvableCollectible resolvableCollectible)
				{
					DummySlot dummySlot = new DummySlot(itemStack);
					resolvableCollectible.Resolve(dummySlot, entity.World);
					itemStack = dummySlot.Itemstack;
				}
				while (itemStack.StackSize > itemStack.Collectible.MaxStackSize)
				{
					ItemStack emptyClone = itemStack.GetEmptyClone();
					emptyClone.StackSize = itemStack.Collectible.MaxStackSize;
					itemStack.StackSize -= itemStack.Collectible.MaxStackSize;
					todrop.Add(emptyClone);
				}
				todrop.Add(itemStack);
				if (blockDropItemStack.LastDrop)
				{
					break;
				}
			}
		}
		entity.GetInterfaces<IHarvestableDrops>()?.ForEach(delegate(IHarvestableDrops hInterface)
		{
			hInterface.GetHarvestableDrops(entity.World, entity.ServerPos.AsBlockPos, byPlayer)?.Foreach(delegate(ItemStack stack)
			{
				todrop.Add(stack);
			});
		});
		inv.AddSlots(todrop.Count - inv.Count);
		ItemStack[] array = todrop.ToArray();
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			inv[num3].Itemstack = array[num3];
		}
		inv.ToTreeAttributes(treeAttribute);
		entity.WatchedAttributes["harvestableInv"] = treeAttribute;
		entity.WatchedAttributes.MarkPathDirty("harvestableInv");
		entity.WatchedAttributes.MarkPathDirty("harvested");
		if (entity.World.Side == EnumAppSide.Server)
		{
			entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.AsBlockPos).MarkModified();
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		interactions = ObjectCacheUtil.GetOrCreate(world.Api, "harvestableEntityInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item item in world.Items)
			{
				if (!(item.Code == null) && item.Tool == EnumTool.Knife)
				{
					list.Add(new ItemStack(item));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-creature-harvest",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray()
				}
			};
		});
		if (entity.Alive || IsHarvested)
		{
			return null;
		}
		return interactions;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (!entity.Alive)
		{
			if (GotCrushed)
			{
				infotext.AppendLine(Lang.Get("Looks crushed. Won't be able to harvest as much from this carcass."));
			}
			if (GotElectrocuted)
			{
				infotext.AppendLine(Lang.Get("Looks partially charred, perhaps due to a lightning strike."));
			}
			if (GotAcidified)
			{
				infotext.AppendLine(Lang.Get("deadcreature-aciddissolved"));
			}
			string text = entity.WatchedAttributes.GetString("deathByEntity");
			if (text != null && !entity.WatchedAttributes.HasAttribute("deathByPlayer"))
			{
				string key = "deadcreature-killed";
				EntityProperties entityType = entity.World.GetEntityType(new AssetLocation(text));
				if (entityType != null)
				{
					JsonObject attributes = entityType.Attributes;
					if (attributes != null && attributes["killedByInfoText"].Exists)
					{
						key = entityType.Attributes["killedByInfoText"].AsString();
					}
				}
				infotext.AppendLine(Lang.Get(key));
			}
		}
		if (!fixedWeight)
		{
			if (AnimalWeight >= 0.95f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-good"));
			}
			else if (AnimalWeight >= 0.75f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-ok"));
			}
			else if (AnimalWeight >= 0.5f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-low"));
			}
			else
			{
				infotext.AppendLine(Lang.Get("creature-weight-starving"));
			}
		}
		base.GetInfoText(infotext);
	}

	public override string PropertyName()
	{
		return "harvestable";
	}
}
