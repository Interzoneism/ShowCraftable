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
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFarmland : BlockEntity, IFarmlandBlockEntity, IAnimalFoodSource, IPointOfInterest, ITexPositionSource
{
	protected enum EnumWaterSearchResult
	{
		Found,
		NotFound,
		Deferred
	}

	protected static Random rand = new Random();

	public static OrderedDictionary<string, float> Fertilities = new OrderedDictionary<string, float>
	{
		{ "verylow", 5f },
		{ "low", 25f },
		{ "medium", 50f },
		{ "compost", 65f },
		{ "high", 80f }
	};

	protected HashSet<string> PermaBoosts = new HashSet<string>();

	protected float totalHoursWaterRetention;

	protected BlockPos upPos;

	protected double totalHoursForNextStage;

	protected double totalHoursLastUpdate;

	protected float[] nutrients = new float[3];

	protected float[] slowReleaseNutrients = new float[3];

	protected Dictionary<string, float> fertilizerOverlayStrength;

	protected float moistureLevel;

	protected double lastWaterSearchedTotalHours;

	protected TreeAttribute cropAttrs = new TreeAttribute();

	public int[] originalFertility = new int[3];

	protected bool unripeCropColdDamaged;

	protected bool unripeHeatDamaged;

	protected bool ripeCropColdDamaged;

	protected bool saltExposed;

	protected float[] damageAccum = new float[Enum.GetValues(typeof(EnumCropStressType)).Length];

	private BlockFarmland blockFarmland;

	protected Vec3d tmpPos = new Vec3d();

	protected float lastWaterDistance = 99f;

	protected double lastMoistureLevelUpdateTotalDays;

	public int roomness;

	protected bool allowundergroundfarming;

	protected bool allowcropDeath;

	protected float fertilityRecoverySpeed = 0.25f;

	protected float growthRateMul = 1f;

	protected MeshData fertilizerQuad;

	protected TextureAtlasPosition fertilizerTexturePos;

	private ICoreClientAPI capi;

	private string[] creatureFoodTags;

	private bool farmlandIsAtChunkEdge;

	public bool IsVisiblyMoist => (double)moistureLevel > 0.1;

	public double TotalHoursForNextStage => totalHoursForNextStage;

	public double TotalHoursFertilityCheck => totalHoursLastUpdate;

	public float[] Nutrients => nutrients;

	public float MoistureLevel => moistureLevel;

	public int[] OriginalFertility => originalFertility;

	public BlockPos UpPos => upPos;

	public ITreeAttribute CropAttributes => cropAttrs;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 1.0, 0.5);

	public string Type => "food";

	BlockPos IFarmlandBlockEntity.Pos => Pos;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => fertilizerTexturePos;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		blockFarmland = base.Block as BlockFarmland;
		if (blockFarmland == null)
		{
			return;
		}
		capi = api as ICoreClientAPI;
		totalHoursWaterRetention = Api.World.Calendar.HoursPerDay * 4f;
		upPos = Pos.UpCopy();
		allowundergroundfarming = Api.World.Config.GetBool("allowUndergroundFarming");
		allowcropDeath = Api.World.Config.GetBool("allowCropDeath", defaultValue: true);
		fertilityRecoverySpeed = Api.World.Config.GetFloat("fertilityRecoverySpeed", fertilityRecoverySpeed);
		growthRateMul = (float)Api.World.Config.GetDecimal("cropGrowthRateMul", growthRateMul);
		creatureFoodTags = base.Block.Attributes["foodTags"].AsArray<string>();
		if (api is ICoreServerAPI)
		{
			if (Api.World.Config.GetBool("processCrops", defaultValue: true))
			{
				RegisterGameTickListener(Update, 3300 + rand.Next(400));
			}
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
		updateFertilizerQuad();
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
	}

	public void OnCreatedFromSoil(Block block)
	{
		string key = block.LastCodePart(1);
		if (block is BlockFarmland)
		{
			key = block.LastCodePart();
		}
		originalFertility[0] = (int)Fertilities[key];
		originalFertility[1] = (int)Fertilities[key];
		originalFertility[2] = (int)Fertilities[key];
		nutrients[0] = originalFertility[0];
		nutrients[1] = originalFertility[1];
		nutrients[2] = originalFertility[2];
		totalHoursLastUpdate = Api.World.Calendar.TotalHours;
		tryUpdateMoistureLevel(Api.World.Calendar.TotalDays, searchNearbyWater: true);
	}

	public bool OnBlockInteract(IPlayer byPlayer)
	{
		ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		JsonObject jsonObject = itemstack?.Collectible?.Attributes?["fertilizerProps"];
		if (jsonObject == null || !jsonObject.Exists)
		{
			return false;
		}
		FertilizerProps fertilizerProps = jsonObject.AsObject<FertilizerProps>();
		if (fertilizerProps == null)
		{
			return false;
		}
		float num = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[0]), fertilizerProps.N);
		float num2 = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[1]), fertilizerProps.P);
		float num3 = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[2]), fertilizerProps.K);
		slowReleaseNutrients[0] += num;
		slowReleaseNutrients[1] += num2;
		slowReleaseNutrients[2] += num3;
		if (fertilizerProps.PermaBoost != null && !PermaBoosts.Contains(fertilizerProps.PermaBoost.Code))
		{
			originalFertility[0] += fertilizerProps.PermaBoost.N;
			originalFertility[1] += fertilizerProps.PermaBoost.P;
			originalFertility[2] += fertilizerProps.PermaBoost.K;
			PermaBoosts.Add(fertilizerProps.PermaBoost.Code);
		}
		string text = itemstack.Collectible.Attributes["fertilizerTextureCode"].AsString();
		if (text != null)
		{
			if (fertilizerOverlayStrength == null)
			{
				fertilizerOverlayStrength = new Dictionary<string, float>();
			}
			fertilizerOverlayStrength.TryGetValue(text, out var value);
			fertilizerOverlayStrength[text] = value + Math.Max(num, Math.Max(num3, num2));
		}
		updateFertilizerQuad();
		byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
		byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		Api.World.PlaySoundAt(Api.World.BlockAccessor.GetBlock(Pos).Sounds.Hit, (double)Pos.X + 0.5, (double)Pos.InternalY + 0.75, (double)Pos.Z + 0.5, byPlayer, randomizePitch: true, 12f);
		MarkDirty();
		return true;
	}

	public void OnCropBlockBroken()
	{
		ripeCropColdDamaged = false;
		unripeCropColdDamaged = false;
		unripeHeatDamaged = false;
		for (int i = 0; i < damageAccum.Length; i++)
		{
			damageAccum[i] = 0f;
		}
		MarkDirty(redrawOnClient: true);
	}

	public ItemStack[] GetDrops(ItemStack[] drops)
	{
		BlockEntityDeadCrop blockEntityDeadCrop = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
		bool flag = blockEntityDeadCrop != null;
		if (!ripeCropColdDamaged && !unripeCropColdDamaged && !unripeHeatDamaged && !flag)
		{
			return drops;
		}
		if (!Api.World.Config.GetString("harshWinters").ToBool(defaultValue: true))
		{
			return drops;
		}
		List<ItemStack> list = new List<ItemStack>();
		BlockCropProperties blockCropProperties = GetCrop()?.CropProps;
		if (blockCropProperties == null)
		{
			return drops;
		}
		float num = 1f;
		if (ripeCropColdDamaged)
		{
			num = blockCropProperties.ColdDamageRipeMul;
		}
		if (unripeHeatDamaged || unripeCropColdDamaged)
		{
			num = blockCropProperties.DamageGrowthStuntMul;
		}
		if (flag)
		{
			num = ((blockEntityDeadCrop.deathReason == EnumCropStressType.Eaten) ? 0f : Math.Max(blockCropProperties.ColdDamageRipeMul, blockCropProperties.DamageGrowthStuntMul));
		}
		string[] needles = base.Block.Attributes?["debuffUnaffectedDrops"].AsArray<string>();
		foreach (ItemStack itemStack in drops)
		{
			if (WildcardUtil.Match(needles, itemStack.Collectible.Code.ToShortString()))
			{
				list.Add(itemStack);
				continue;
			}
			float num2 = (float)itemStack.StackSize * num;
			float num3 = num2 - (float)(int)num2;
			itemStack.StackSize = (int)num2 + ((Api.World.Rand.NextDouble() > (double)num3) ? 1 : 0);
			if (itemStack.StackSize > 0)
			{
				list.Add(itemStack);
			}
		}
		MarkDirty(redrawOnClient: true);
		return list.ToArray();
	}

	protected float GetNearbyWaterDistance(out EnumWaterSearchResult result, float hoursPassed)
	{
		float waterDistance = 99f;
		farmlandIsAtChunkEdge = false;
		bool saltWater = false;
		Api.World.BlockAccessor.SearchFluidBlocks(new BlockPos(Pos.X - 4, Pos.Y, Pos.Z - 4), new BlockPos(Pos.X + 4, Pos.Y, Pos.Z + 4), delegate(Block block, BlockPos pos)
		{
			if (block.LiquidCode == "water")
			{
				waterDistance = Math.Min(waterDistance, Math.Max(Math.Abs(pos.X - Pos.X), Math.Abs(pos.Z - Pos.Z)));
			}
			if (block.LiquidCode == "saltwater")
			{
				saltWater = true;
			}
			return true;
		}, delegate
		{
			farmlandIsAtChunkEdge = true;
		});
		if (saltWater)
		{
			damageAccum[4] += hoursPassed;
		}
		result = EnumWaterSearchResult.Deferred;
		if (farmlandIsAtChunkEdge)
		{
			return 99f;
		}
		lastWaterSearchedTotalHours = Api.World.Calendar.TotalHours;
		if (waterDistance < 4f)
		{
			result = EnumWaterSearchResult.Found;
			return waterDistance;
		}
		result = EnumWaterSearchResult.NotFound;
		return 99f;
	}

	private bool tryUpdateMoistureLevel(double totalDays, bool searchNearbyWater)
	{
		float waterDistance = 99f;
		if (searchNearbyWater)
		{
			waterDistance = GetNearbyWaterDistance(out var result, 0f);
			switch (result)
			{
			case EnumWaterSearchResult.Deferred:
				return false;
			default:
				waterDistance = 99f;
				break;
			case EnumWaterSearchResult.Found:
				break;
			}
			lastWaterDistance = waterDistance;
		}
		if (updateMoistureLevel(totalDays, waterDistance))
		{
			UpdateFarmlandBlock();
		}
		return true;
	}

	private bool updateMoistureLevel(double totalDays, float waterDistance)
	{
		bool skyExposed = Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= ((GetCrop() == null) ? Pos.Y : (Pos.Y + 1));
		return updateMoistureLevel(totalDays, waterDistance, skyExposed);
	}

	private bool updateMoistureLevel(double totalDays, float waterDistance, bool skyExposed, ClimateCondition baseClimate = null)
	{
		tmpPos.Set((double)Pos.X + 0.5, (double)Pos.Y + 0.5, (double)Pos.Z + 0.5);
		float num = GameMath.Clamp(1f - waterDistance / 4f, 0f, 1f);
		if (lastMoistureLevelUpdateTotalDays > Api.World.Calendar.TotalDays)
		{
			lastMoistureLevelUpdateTotalDays = Api.World.Calendar.TotalDays;
			return false;
		}
		double num2 = Math.Min((totalDays - lastMoistureLevelUpdateTotalDays) * (double)Api.World.Calendar.HoursPerDay, totalHoursWaterRetention);
		if (num2 < 0.029999999329447746)
		{
			moistureLevel = Math.Max(moistureLevel, num);
			return false;
		}
		moistureLevel = Math.Max(num, moistureLevel - (float)num2 / totalHoursWaterRetention);
		if (skyExposed)
		{
			if (baseClimate == null && num2 > 0.0)
			{
				baseClimate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues, totalDays - num2 / (double)Api.World.Calendar.HoursPerDay / 2.0);
			}
			while (num2 > 0.0)
			{
				double num3 = blockFarmland.wsys.GetPrecipitation(Pos, totalDays - num2 / (double)Api.World.Calendar.HoursPerDay, baseClimate);
				moistureLevel = GameMath.Clamp(moistureLevel + (float)num3 / 3f, 0f, 1f);
				num2 -= 1.0;
			}
		}
		lastMoistureLevelUpdateTotalDays = totalDays;
		return true;
	}

	private void Update(float dt)
	{
		if (!(Api as ICoreServerAPI).World.IsFullyLoadedChunk(Pos))
		{
			return;
		}
		double totalHours = Api.World.Calendar.TotalHours;
		double num = 3.0 + rand.NextDouble();
		Block crop = GetCrop();
		bool flag = crop != null;
		bool flag2 = Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= (flag ? (Pos.Y + 1) : Pos.Y);
		if (totalHours - totalHoursLastUpdate < num)
		{
			if (!(totalHoursLastUpdate > totalHours))
			{
				if (updateMoistureLevel(totalHours / (double)Api.World.Calendar.HoursPerDay, lastWaterDistance, flag2))
				{
					UpdateFarmlandBlock();
				}
				return;
			}
			double num2 = totalHoursLastUpdate - totalHours;
			totalHoursForNextStage -= num2;
			lastMoistureLevelUpdateTotalDays -= num2;
			lastWaterSearchedTotalHours -= num2;
			totalHoursLastUpdate = totalHours;
		}
		int num3 = 0;
		if (!allowundergroundfarming)
		{
			num3 = Math.Max(0, Api.World.SeaLevel - Pos.Y);
		}
		int lightLevel = Api.World.BlockAccessor.GetLightLevel(upPos, EnumLightLevelType.MaxLight);
		double num4 = GameMath.Clamp(1f - (float)(blockFarmland.DelayGrowthBelowSunLight - lightLevel - num3) * blockFarmland.LossPerLevel, 0f, 1f);
		Block block = Api.World.BlockAccessor.GetBlock(upPos);
		Block block2 = Api.World.GetBlock(new AssetLocation("deadcrop"));
		double hoursForNextStage = GetHoursForNextStage();
		double num5 = hoursForNextStage / num4 - hoursForNextStage;
		double num6 = totalHoursForNextStage + num5;
		EnumSoilNutrient? enumSoilNutrient = null;
		if (block.CropProps != null)
		{
			enumSoilNutrient = block.CropProps.RequiredNutrient;
		}
		bool flag3 = false;
		float[] array = new float[3];
		float waterDistance = 99f;
		totalHoursLastUpdate = Math.Max(totalHoursLastUpdate, totalHours - (double)((float)Api.World.Calendar.DaysPerYear * Api.World.Calendar.HoursPerDay));
		bool flag4 = HasRipeCrop();
		if (!flag2)
		{
			Room room = blockFarmland.roomreg?.GetRoomForPosition(upPos);
			roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		}
		else
		{
			roomness = 0;
		}
		bool flag5 = false;
		ClimateCondition climateCondition = null;
		while (totalHours - totalHoursLastUpdate > num)
		{
			if (!flag5)
			{
				waterDistance = GetNearbyWaterDistance(out var result, (float)num);
				switch (result)
				{
				case EnumWaterSearchResult.Deferred:
					return;
				case EnumWaterSearchResult.NotFound:
					waterDistance = 99f;
					break;
				}
				flag5 = true;
				lastWaterDistance = waterDistance;
			}
			totalHoursLastUpdate += num;
			num = 3.0 + rand.NextDouble();
			if (climateCondition == null)
			{
				climateCondition = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay);
				if (climateCondition == null)
				{
					break;
				}
			}
			else
			{
				Api.World.BlockAccessor.GetClimateAt(Pos, climateCondition, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay);
			}
			updateMoistureLevel(totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay, waterDistance, flag2, climateCondition);
			if (roomness > 0)
			{
				climateCondition.Temperature += 5f;
			}
			if (!flag)
			{
				ripeCropColdDamaged = false;
				unripeCropColdDamaged = false;
				unripeHeatDamaged = false;
				for (int i = 0; i < damageAccum.Length; i++)
				{
					damageAccum[i] = 0f;
				}
			}
			else
			{
				if (crop?.CropProps != null && climateCondition.Temperature < crop.CropProps.ColdDamageBelow)
				{
					if (flag4)
					{
						ripeCropColdDamaged = true;
					}
					else
					{
						unripeCropColdDamaged = true;
						damageAccum[2] += (float)num;
					}
				}
				else
				{
					damageAccum[2] = Math.Max(0f, damageAccum[2] - (float)num / 10f);
				}
				if (crop?.CropProps != null && climateCondition.Temperature > crop.CropProps.HeatDamageAbove && flag)
				{
					unripeHeatDamaged = true;
					damageAccum[1] += (float)num;
				}
				else
				{
					damageAccum[1] = Math.Max(0f, damageAccum[1] - (float)num / 10f);
				}
				for (int j = 0; j < damageAccum.Length; j++)
				{
					float num7 = damageAccum[j];
					if (!allowcropDeath)
					{
						num7 = (damageAccum[j] = 0f);
					}
					if (num7 > 48f)
					{
						Api.World.BlockAccessor.SetBlock(block2.Id, upPos);
						BlockEntityDeadCrop obj = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
						obj.Inventory[0].Itemstack = new ItemStack(crop);
						obj.deathReason = (EnumCropStressType)j;
						flag = false;
						break;
					}
				}
			}
			float num8 = GameMath.Clamp(climateCondition.Temperature / 10f, 0f, 10f);
			if (rand.NextDouble() > (double)num8)
			{
				continue;
			}
			flag3 |= rand.NextDouble() < 0.006;
			array[0] = (flag4 ? 0f : fertilityRecoverySpeed);
			array[1] = (flag4 ? 0f : fertilityRecoverySpeed);
			array[2] = (flag4 ? 0f : fertilityRecoverySpeed);
			if (enumSoilNutrient.HasValue)
			{
				array[(int)enumSoilNutrient.Value] /= 3f;
			}
			for (int k = 0; k < 3; k++)
			{
				nutrients[k] += Math.Max(0f, array[k] + Math.Min(0f, (float)originalFertility[k] - nutrients[k] - array[k]));
				if (slowReleaseNutrients[k] > 0f)
				{
					float num9 = Math.Min(0.25f, slowReleaseNutrients[k]);
					nutrients[k] = Math.Min(100f, nutrients[k] + num9);
					slowReleaseNutrients[k] = Math.Max(0f, slowReleaseNutrients[k] - num9);
				}
				else if (nutrients[k] > (float)originalFertility[k])
				{
					nutrients[k] = Math.Max(originalFertility[k], nutrients[k] - 0.05f);
				}
			}
			if (fertilizerOverlayStrength != null && fertilizerOverlayStrength.Count > 0)
			{
				string[] array2 = fertilizerOverlayStrength.Keys.ToArray();
				foreach (string key in array2)
				{
					float num10 = fertilizerOverlayStrength[key] - fertilityRecoverySpeed;
					if (num10 < 0f)
					{
						fertilizerOverlayStrength.Remove(key);
					}
					else
					{
						fertilizerOverlayStrength[key] = num10;
					}
				}
			}
			if (!((double)moistureLevel < 0.1) && num6 <= totalHoursLastUpdate)
			{
				TryGrowCrop(totalHoursForNextStage);
				flag4 = HasRipeCrop();
				totalHoursForNextStage += hoursForNextStage;
				num6 = totalHoursForNextStage + num5;
				hoursForNextStage = GetHoursForNextStage();
			}
		}
		if (flag3 && block.BlockMaterial == EnumBlockMaterial.Air)
		{
			double num11 = rand.NextDouble() * (double)blockFarmland.TotalWeedChance;
			for (int m = 0; m < blockFarmland.WeedNames.Length; m++)
			{
				num11 -= (double)blockFarmland.WeedNames[m].Chance;
				if (num11 <= 0.0)
				{
					Block block3 = Api.World.GetBlock(blockFarmland.WeedNames[m].Code);
					if (block3 != null)
					{
						Api.World.BlockAccessor.SetBlock(block3.BlockId, upPos);
					}
					break;
				}
			}
		}
		updateFertilizerQuad();
		UpdateFarmlandBlock();
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
	}

	public double GetHoursForNextStage()
	{
		Block crop = GetCrop();
		if (crop == null)
		{
			return 99999999.0;
		}
		float totalGrowthDays = crop.CropProps.TotalGrowthDays;
		totalGrowthDays = ((!(totalGrowthDays > 0f)) ? (crop.CropProps.TotalGrowthMonths * (float)Api.World.Calendar.DaysPerMonth) : (totalGrowthDays / 12f * (float)Api.World.Calendar.DaysPerMonth));
		return Api.World.Calendar.HoursPerDay * totalGrowthDays / (float)crop.CropProps.GrowthStages * (1f / GetGrowthRate(crop.CropProps.RequiredNutrient)) * (float)(0.9 + 0.2 * rand.NextDouble()) / growthRateMul;
	}

	public float GetGrowthRate(EnumSoilNutrient nutrient)
	{
		float num = (float)Math.Pow(Math.Max(0.01, (double)(moistureLevel * 100f / 70f) - 0.143), 0.35);
		if (nutrients[(int)nutrient] > 75f)
		{
			return num * 1.1f;
		}
		if (nutrients[(int)nutrient] > 50f)
		{
			return num * 1f;
		}
		if (nutrients[(int)nutrient] > 35f)
		{
			return num * 0.9f;
		}
		if (nutrients[(int)nutrient] > 20f)
		{
			return num * 0.6f;
		}
		if (nutrients[(int)nutrient] > 5f)
		{
			return num * 0.3f;
		}
		return num * 0.1f;
	}

	public float GetGrowthRate()
	{
		BlockCropProperties blockCropProperties = GetCrop()?.CropProps;
		if (blockCropProperties != null)
		{
			return GetGrowthRate(blockCropProperties.RequiredNutrient);
		}
		return 1f;
	}

	public float GetDeathChance(int nutrientIndex)
	{
		if (nutrients[nutrientIndex] <= 5f)
		{
			return 0.5f;
		}
		return 0f;
	}

	public bool TryPlant(Block block, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel)
	{
		if (CanPlant() && block.CropProps != null)
		{
			Api.World.BlockAccessor.SetBlock(block.BlockId, upPos);
			totalHoursForNextStage = Api.World.Calendar.TotalHours + GetHoursForNextStage();
			CropBehavior[] behaviors = block.CropProps.Behaviors;
			for (int i = 0; i < behaviors.Length; i++)
			{
				behaviors[i].OnPlanted(Api, itemslot, byEntity, blockSel);
			}
			return true;
		}
		return false;
	}

	public bool CanPlant()
	{
		Block block = Api.World.BlockAccessor.GetBlock(upPos);
		if (block != null)
		{
			return block.BlockMaterial == EnumBlockMaterial.Air;
		}
		return true;
	}

	public bool HasUnripeCrop()
	{
		Block crop = GetCrop();
		if (crop != null)
		{
			return GetCropStage(crop) < crop.CropProps.GrowthStages;
		}
		return false;
	}

	public bool HasRipeCrop()
	{
		Block crop = GetCrop();
		if (crop != null)
		{
			return GetCropStage(crop) >= crop.CropProps.GrowthStages;
		}
		return false;
	}

	public bool TryGrowCrop(double currentTotalHours)
	{
		Block crop = GetCrop();
		if (crop == null)
		{
			return false;
		}
		int cropStage = GetCropStage(crop);
		if (cropStage < crop.CropProps.GrowthStages)
		{
			int newGrowthStage = cropStage + 1;
			Block block = Api.World.GetBlock(crop.CodeWithParts(newGrowthStage.ToString() ?? ""));
			if (block == null)
			{
				return false;
			}
			if (crop.CropProps.Behaviors != null)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				bool result = false;
				CropBehavior[] behaviors = crop.CropProps.Behaviors;
				for (int i = 0; i < behaviors.Length; i++)
				{
					result = behaviors[i].TryGrowCrop(Api, this, currentTotalHours, newGrowthStage, ref handling);
					if (handling == EnumHandling.PreventSubsequent)
					{
						return result;
					}
				}
				if (handling == EnumHandling.PreventDefault)
				{
					return result;
				}
			}
			if (Api.World.BlockAccessor.GetBlockEntity(upPos) == null)
			{
				Api.World.BlockAccessor.SetBlock(block.BlockId, upPos);
			}
			else
			{
				Api.World.BlockAccessor.ExchangeBlock(block.BlockId, upPos);
			}
			ConsumeNutrients(crop);
			return true;
		}
		return false;
	}

	private void ConsumeNutrients(Block cropBlock)
	{
		float num = cropBlock.CropProps.NutrientConsumption / (float)cropBlock.CropProps.GrowthStages;
		nutrients[(int)cropBlock.CropProps.RequiredNutrient] = Math.Max(0f, nutrients[(int)cropBlock.CropProps.RequiredNutrient] - num);
		UpdateFarmlandBlock();
	}

	private void UpdateFarmlandBlock()
	{
		int fertilityLevel = GetFertilityLevel((originalFertility[0] + originalFertility[1] + originalFertility[2]) / 3);
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		Block block2 = Api.World.GetBlock(block.CodeWithParts(IsVisiblyMoist ? "moist" : "dry", Fertilities.GetKeyAtIndex(fertilityLevel)));
		if (block2 == null)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
		}
		else if (block.BlockId != block2.BlockId)
		{
			Api.World.BlockAccessor.ExchangeBlock(block2.BlockId, Pos);
			Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	internal int GetFertilityLevel(float fertiltyValue)
	{
		int num = 0;
		foreach (KeyValuePair<string, float> fertility in Fertilities)
		{
			if (fertility.Value >= fertiltyValue)
			{
				return num;
			}
			num++;
		}
		return Fertilities.Count - 1;
	}

	internal Block GetCrop()
	{
		Block block = Api.World.BlockAccessor.GetBlock(upPos);
		if (block == null || block.CropProps == null)
		{
			return null;
		}
		return block;
	}

	internal int GetCropStage(Block block)
	{
		int.TryParse(block.LastCodePart(), out var result);
		return result;
	}

	private void updateFertilizerQuad()
	{
		if (capi == null)
		{
			return;
		}
		AssetLocation assetLocation = new AssetLocation();
		if (fertilizerOverlayStrength == null || fertilizerOverlayStrength.Count == 0)
		{
			bool num = fertilizerQuad != null;
			fertilizerQuad = null;
			if (num)
			{
				MarkDirty(redrawOnClient: true);
			}
			return;
		}
		int num2 = 0;
		foreach (KeyValuePair<string, float> item in fertilizerOverlayStrength)
		{
			string text = "low";
			if (item.Value > 50f)
			{
				text = "med";
			}
			if (item.Value > 100f)
			{
				text = "high";
			}
			if (num2 > 0)
			{
				assetLocation.Path += "++0~";
			}
			AssetLocation assetLocation2 = assetLocation;
			assetLocation2.Path = assetLocation2.Path + "block/soil/farmland/fertilizer/" + item.Key + "-" + text;
			num2++;
		}
		capi.BlockTextureAtlas.GetOrInsertTexture(assetLocation, out var _, out var texPos);
		if (fertilizerTexturePos != texPos)
		{
			fertilizerTexturePos = texPos;
			genFertilizerQuad();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void genFertilizerQuad()
	{
		Shape shapeBase = capi.Assets.TryGet(new AssetLocation("shapes/block/farmland-fertilizer.json")).ToObject<Shape>();
		capi.Tesselator.TesselateShape(new TesselationMetaData
		{
			TypeForLogging = "farmland fertilizer quad",
			TexSource = this
		}, shapeBase, out fertilizerQuad);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		nutrients[0] = tree.GetFloat("n");
		nutrients[1] = tree.GetFloat("p");
		nutrients[2] = tree.GetFloat("k");
		slowReleaseNutrients[0] = tree.GetFloat("slowN");
		slowReleaseNutrients[1] = tree.GetFloat("slowP");
		slowReleaseNutrients[2] = tree.GetFloat("slowK");
		moistureLevel = tree.GetFloat("moistureLevel");
		lastWaterSearchedTotalHours = tree.GetDouble("lastWaterSearchedTotalHours");
		if (!tree.HasAttribute("originalFertilityN"))
		{
			originalFertility[0] = tree.GetInt("originalFertility");
			originalFertility[1] = tree.GetInt("originalFertility");
			originalFertility[2] = tree.GetInt("originalFertility");
		}
		else
		{
			originalFertility[0] = tree.GetInt("originalFertilityN");
			originalFertility[1] = tree.GetInt("originalFertilityP");
			originalFertility[2] = tree.GetInt("originalFertilityK");
		}
		if (tree.HasAttribute("totalHoursForNextStage"))
		{
			totalHoursForNextStage = tree.GetDouble("totalHoursForNextStage");
			totalHoursLastUpdate = tree.GetDouble("totalHoursFertilityCheck");
		}
		else
		{
			totalHoursForNextStage = tree.GetDouble("totalDaysForNextStage") * 24.0;
			totalHoursLastUpdate = tree.GetDouble("totalDaysFertilityCheck") * 24.0;
		}
		lastMoistureLevelUpdateTotalDays = tree.GetDouble("lastMoistureLevelUpdateTotalDays");
		cropAttrs = tree["cropAttrs"] as TreeAttribute;
		if (cropAttrs == null)
		{
			cropAttrs = new TreeAttribute();
		}
		lastWaterDistance = tree.GetFloat("lastWaterDistance");
		unripeCropColdDamaged = tree.GetBool("unripeCropExposedToFrost");
		ripeCropColdDamaged = tree.GetBool("ripeCropExposedToFrost");
		unripeHeatDamaged = tree.GetBool("unripeHeatDamaged");
		saltExposed = tree.GetBool("saltExposed");
		roomness = tree.GetInt("roomness");
		string[] stringArray = (tree as TreeAttribute).GetStringArray("permaBoosts");
		if (stringArray != null)
		{
			PermaBoosts.AddRange(stringArray);
		}
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("fertilizerOverlayStrength");
		if (treeAttribute != null)
		{
			fertilizerOverlayStrength = new Dictionary<string, float>();
			foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
			{
				fertilizerOverlayStrength[item.Key] = (item.Value as FloatAttribute).value;
			}
		}
		else
		{
			fertilizerOverlayStrength = null;
		}
		updateFertilizerQuad();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("n", nutrients[0]);
		tree.SetFloat("p", nutrients[1]);
		tree.SetFloat("k", nutrients[2]);
		tree.SetFloat("slowN", slowReleaseNutrients[0]);
		tree.SetFloat("slowP", slowReleaseNutrients[1]);
		tree.SetFloat("slowK", slowReleaseNutrients[2]);
		tree.SetFloat("moistureLevel", moistureLevel);
		tree.SetDouble("lastWaterSearchedTotalHours", lastWaterSearchedTotalHours);
		tree.SetInt("originalFertilityN", originalFertility[0]);
		tree.SetInt("originalFertilityP", originalFertility[1]);
		tree.SetInt("originalFertilityK", originalFertility[2]);
		tree.SetDouble("totalHoursForNextStage", totalHoursForNextStage);
		tree.SetDouble("totalHoursFertilityCheck", totalHoursLastUpdate);
		tree.SetDouble("lastMoistureLevelUpdateTotalDays", lastMoistureLevelUpdateTotalDays);
		tree.SetFloat("lastWaterDistance", lastWaterDistance);
		tree.SetBool("ripeCropExposedToFrost", ripeCropColdDamaged);
		tree.SetBool("unripeCropExposedToFrost", unripeCropColdDamaged);
		tree.SetBool("unripeHeatDamaged", unripeHeatDamaged);
		tree.SetBool("saltExposed", damageAccum[4] > 1f);
		(tree as TreeAttribute).SetStringArray("permaBoosts", PermaBoosts.ToArray());
		tree.SetInt("roomness", roomness);
		tree["cropAttrs"] = cropAttrs;
		if (fertilizerOverlayStrength == null)
		{
			return;
		}
		TreeAttribute treeAttribute = (TreeAttribute)(tree["fertilizerOverlayStrength"] = new TreeAttribute());
		foreach (KeyValuePair<string, float> item in fertilizerOverlayStrength)
		{
			treeAttribute.SetFloat(item.Key, item.Value);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		BlockCropProperties blockCropProperties = GetCrop()?.CropProps;
		if (blockCropProperties != null)
		{
			dsc.AppendLine(Lang.Get("Required Nutrient: {0}", blockCropProperties.RequiredNutrient));
			dsc.AppendLine(Lang.Get("Growth Stage: {0} / {1}", GetCropStage(GetCrop()), blockCropProperties.GrowthStages));
			dsc.AppendLine();
		}
		dsc.AppendLine(Lang.Get("farmland-nutrientlevels", Math.Round(nutrients[0], 1), Math.Round(nutrients[1], 1), Math.Round(nutrients[2], 1)));
		float num = (float)Math.Round(slowReleaseNutrients[0], 1);
		float num2 = (float)Math.Round(slowReleaseNutrients[1], 1);
		float num3 = (float)Math.Round(slowReleaseNutrients[2], 1);
		if (num > 0f || num2 > 0f || num3 > 0f)
		{
			List<string> list = new List<string>();
			if (num > 0f)
			{
				list.Add(Lang.Get("+{0}% N", num));
			}
			if (num2 > 0f)
			{
				list.Add(Lang.Get("+{0}% P", num2));
			}
			if (num3 > 0f)
			{
				list.Add(Lang.Get("+{0}% K", num3));
			}
			dsc.AppendLine(Lang.Get("farmland-activefertilizer", string.Join(", ", list)));
		}
		if (blockCropProperties == null)
		{
			float num4 = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.N), 0);
			float num5 = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.P), 0);
			float num6 = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.K), 0);
			string text = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num4)]);
			string text2 = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num5)]);
			string text3 = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num6)]);
			dsc.AppendLine(Lang.Get("farmland-growthspeeds", text, num4, text2, num5, text3, num6));
		}
		else
		{
			float num7 = (float)Math.Round(100f * GetGrowthRate(blockCropProperties.RequiredNutrient), 0);
			string text4 = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num7)]);
			dsc.AppendLine(Lang.Get("farmland-growthspeed", text4, num7, blockCropProperties.RequiredNutrient));
		}
		float num8 = (float)Math.Round(moistureLevel * 100f, 0);
		string text5 = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num8)]);
		dsc.AppendLine(Lang.Get("farmland-moisture", text5, num8));
		if ((ripeCropColdDamaged || unripeCropColdDamaged || unripeHeatDamaged) && blockCropProperties != null)
		{
			if (ripeCropColdDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-ripecolddamaged", (int)(blockCropProperties.ColdDamageRipeMul * 100f)));
			}
			else if (unripeCropColdDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-unripecolddamaged", (int)(blockCropProperties.DamageGrowthStuntMul * 100f)));
			}
			else if (unripeHeatDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-unripeheatdamaged", (int)(blockCropProperties.DamageGrowthStuntMul * 100f)));
			}
		}
		if (roomness > 0)
		{
			dsc.AppendLine(Lang.Get("greenhousetempbonus"));
		}
		if (saltExposed)
		{
			dsc.AppendLine(Lang.Get("farmland-saltdamage"));
		}
		dsc.ToString();
	}

	public void WaterFarmland(float dt, bool waterNeightbours = true)
	{
		float num = moistureLevel;
		moistureLevel = Math.Min(1f, moistureLevel + dt / 2f);
		if (waterNeightbours)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing facing in hORIZONTALS)
			{
				BlockPos position = Pos.AddCopy(facing);
				if (Api.World.BlockAccessor.GetBlockEntity(position) is BlockEntityFarmland blockEntityFarmland)
				{
					blockEntityFarmland.WaterFarmland(dt / 3f, waterNeightbours: false);
				}
			}
		}
		updateMoistureLevel(Api.World.Calendar.TotalDays, lastWaterDistance);
		UpdateFarmlandBlock();
		if ((double)(moistureLevel - num) > 0.05)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (diet == null)
		{
			return false;
		}
		if (GetCrop() == null)
		{
			return false;
		}
		return diet.Matches(EnumFoodCategory.NoNutrition, creatureFoodTags);
	}

	public float ConsumeOnePortion(Entity entity)
	{
		Block crop = GetCrop();
		if (crop == null)
		{
			return 0f;
		}
		Block block = Api.World.GetBlock(new AssetLocation("deadcrop"));
		Api.World.BlockAccessor.SetBlock(block.Id, upPos);
		BlockEntityDeadCrop obj = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
		obj.Inventory[0].Itemstack = new ItemStack(crop);
		obj.deathReason = EnumCropStressType.Eaten;
		return 1f;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(fertilizerQuad);
		return false;
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
}
