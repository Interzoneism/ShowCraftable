using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorAntlerGrowth : EntityBehaviorContainer, IHarvestableDrops
{
	private InventoryGeneric creatureInv;

	private Item[] variants;

	private float beginGrowMonth;

	private float growDurationMonths;

	private float grownDurationMonths;

	private float shedDurationMonths;

	private bool noItemDrop;

	private float accum3s;

	private int MaxGrowth
	{
		get
		{
			return entity.WatchedAttributes.GetInt("maxGrowth");
		}
		set
		{
			entity.WatchedAttributes.SetInt("maxGrowth", value);
		}
	}

	private double LastShedTotalDays
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastShedTotalDays", -1.0);
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastShedTotalDays", value);
		}
	}

	public override InventoryBase Inventory => creatureInv;

	public override string InventoryClassName => "antlerinv";

	public EntityBehaviorAntlerGrowth(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		string[] array = attributes["variants"].AsArray<string>();
		if (array != null)
		{
			variants = new Item[array.Length];
			string text = attributes["overrideType"]?.ToString() ?? entity.Properties.Variant["type"];
			for (int i = 0; i < array.Length; i++)
			{
				AssetLocation assetLocation = new AssetLocation("antler-" + text + "-" + array[i]);
				if ((variants[i] = entity.Api.World.GetItem(assetLocation)) == null)
				{
					entity.Api.Logger.Warning("Missing antler item of code " + assetLocation.ToShortString() + " for creature " + entity.Code.ToShortString());
				}
			}
		}
		beginGrowMonth = attributes["beginGrowMonth"].AsFloat(-1f);
		growDurationMonths = attributes["growDurationMonths"].AsFloat();
		grownDurationMonths = attributes["grownDurationMonths"].AsFloat();
		shedDurationMonths = attributes["shedDurationMonths"].AsFloat();
		noItemDrop = attributes["noItemDrop"].AsBool();
		creatureInv = new InventoryGeneric(1, InventoryClassName + "-" + entity.EntityId, entity.Api, (int id, InventoryGeneric inv) => new ItemSlot(inv));
		loadInv();
	}

	public override void OnEntitySpawn()
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			ensureHasBirthDate();
			OnGameTick(3.1f);
		}
	}

	public override void OnEntityLoaded()
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			ensureHasBirthDate();
			OnGameTick(3.1f);
		}
	}

	private void ensureHasBirthDate()
	{
		if (!entity.WatchedAttributes.HasAttribute("birthTotalDays"))
		{
			entity.WatchedAttributes.SetDouble("birthTotalDays", entity.World.Calendar.TotalDays - (double)entity.World.Rand.Next(900));
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		accum3s += deltaTime;
		if (accum3s > 3f)
		{
			accum3s = 0f;
			if (beginGrowMonth >= 0f)
			{
				updateAntlerStateYearly();
			}
			else if (growDurationMonths >= 0f)
			{
				updateAntlerStateOnetimeGrowth();
			}
		}
	}

	private void updateAntlerStateOnetimeGrowth()
	{
		double num = (entity.World.Calendar.TotalDays - entity.WatchedAttributes.GetDouble("birthTotalDays")) / (double)entity.World.Calendar.DaysPerMonth;
		if (creatureInv.Empty)
		{
			int num2 = variants.Length;
			MaxGrowth = Math.Min((entity.World.Rand.Next(num2) + entity.World.Rand.Next(num2)) / 2, num2 - 1);
		}
		int antler = GameMath.Clamp((int)(num / (double)(growDurationMonths * (float)MaxGrowth)), 0, MaxGrowth);
		SetAntler(antler);
	}

	private void SetAntler(int stage)
	{
		Item item = variants[GameMath.Clamp(stage, 0, variants.Length - 1)];
		if (item != null)
		{
			ItemStack itemstack = creatureInv[0].Itemstack;
			if (itemstack == null || item != itemstack.Item)
			{
				SetCreatureItemStack(new ItemStack(item));
			}
		}
	}

	private void updateAntlerStateYearly()
	{
		if (variants == null || variants.Length == 0)
		{
			return;
		}
		bool shedNow;
		int growthStage = getGrowthStage(out shedNow);
		if (growthStage < 0)
		{
			SetCreatureItemStack(null);
		}
		else if (!shedNow)
		{
			SetAntler(growthStage);
		}
		else if (!creatureInv[0].Empty)
		{
			if (!noItemDrop)
			{
				entity.World.SpawnItemEntity(creatureInv[0].Itemstack, entity.Pos.XYZ);
			}
			SetCreatureItemStack(null);
			LastShedTotalDays = entity.World.Calendar.TotalDays;
		}
	}

	private int getGrowthStage(out bool shedNow)
	{
		shedNow = false;
		IGameCalendar calendar = entity.World.Calendar;
		EnumHemisphere hemisphere = entity.World.Calendar.GetHemisphere(entity.Pos.AsBlockPos);
		double end = calendar.TotalDays / (double)calendar.DaysPerMonth;
		double num = beginGrowMonth;
		if (hemisphere == EnumHemisphere.South)
		{
			num += 6.0;
		}
		if (num >= 12.0)
		{
			num -= 12.0;
		}
		double num2 = num + (double)(growDurationMonths / 2f);
		double num3 = GameMath.CyclicValueDistance((double)((int)(calendar.TotalDays / (double)calendar.DaysPerYear) * 12) + num2, end, 12.0);
		if (num3 < (double)((0f - growDurationMonths) / 2f))
		{
			num3 += 12.0;
		}
		if (num3 > (double)(growDurationMonths / 2f + grownDurationMonths + shedDurationMonths))
		{
			return -1;
		}
		double num4 = (num3 + (double)(growDurationMonths / 2f)) / (double)growDurationMonths;
		shedNow = num3 > (double)(growDurationMonths / 2f + grownDurationMonths);
		int num5 = variants.Length;
		if (creatureInv.Empty || MaxGrowth < 0)
		{
			MaxGrowth = Math.Min((entity.World.Rand.Next(num5) + entity.World.Rand.Next(num5)) / 2, num5 - 1);
		}
		return (int)GameMath.Clamp(num4 * (double)MaxGrowth, 0.0, MaxGrowth);
	}

	private void SetCreatureItemStack(ItemStack stack)
	{
		if (creatureInv[0].Itemstack != null || stack != null)
		{
			creatureInv[0].Itemstack = stack;
			ToBytes(forClient: true);
		}
	}

	public ItemStack[] GetHarvestableDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		return new ItemStack[1] { creatureInv[0].Itemstack };
	}

	public override bool TryGiveItemStack(ItemStack itemstack, ref EnumHandling handling)
	{
		return false;
	}

	public override string PropertyName()
	{
		return "antlergrowth";
	}
}
