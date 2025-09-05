using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBeehive : BlockEntity, IAnimalFoodSource, IPointOfInterest
{
	private int scanIteration;

	private int quantityNearbyFlowers;

	private int quantityNearbyHives;

	private List<BlockPos> emptySkeps = new List<BlockPos>();

	private bool isWildHive;

	private BlockPos skepToPop;

	private double beginPopStartTotalHours;

	private float popHiveAfterHours;

	private double cooldownUntilTotalHours;

	private double harvestableAtTotalHours;

	private double lastCheckedAtTotalHours;

	public bool Harvestable;

	private int scanQuantityNearbyFlowers;

	private int scanQuantityNearbyHives;

	private List<BlockPos> scanEmptySkeps = new List<BlockPos>();

	private EnumHivePopSize hivePopSize;

	private bool wasPlaced;

	public static SimpleParticleProperties Bees;

	private string orientation;

	private string material;

	private Vec3d startPos = new Vec3d();

	private Vec3d endPos = new Vec3d();

	private Vec3f minVelo = new Vec3f();

	private Vec3f maxVelo = new Vec3f();

	private float activityLevel;

	private RoomRegistry roomreg;

	private float roomness;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	static BlockEntityBeehive()
	{
		Bees = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 215, 156, 65), new Vec3d(), new Vec3d(), new Vec3f(0f, 0f, 0f), new Vec3f(0f, 0f, 0f), 1f, 0f, 0.5f, 0.5f);
		Bees.RandomVelocityChange = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(TestHarvestable, 3000);
		RegisterGameTickListener(OnScanForEmptySkep, api.World.Rand.Next(5000) + 30000);
		roomreg = Api.ModLoader.GetModSystem<RoomRegistry>();
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(SpawnBeeParticles, 300);
		}
		if (wasPlaced)
		{
			harvestableAtTotalHours = api.World.Calendar.TotalHours + 12.0 * (3.0 + api.World.Rand.NextDouble() * 8.0);
		}
		orientation = base.Block.Variant["side"];
		material = base.Block.Variant["material"];
		isWildHive = base.Block is BlockBeehive;
		if (!isWildHive && api.Side == EnumAppSide.Client && !api.ObjectCache.ContainsKey("beehive-" + material + "-harvestablemesh-" + orientation))
		{
			ICoreClientAPI obj = api as ICoreClientAPI;
			Block block = api.World.GetBlock(base.Block.CodeWithVariant("type", "populated"));
			obj.Tesselator.TesselateShape(block, Shape.TryGet(api, "shapes/block/beehive/skep-harvestable.json"), out var modeldata, new Vec3f(0f, BlockFacing.FromCode(orientation).HorizontalAngleIndex * 90 - 90, 0f));
			api.ObjectCache["beehive-" + material + "-harvestablemesh-" + orientation] = modeldata;
		}
		if (!isWildHive && api.Side == EnumAppSide.Server)
		{
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
	}

	private void SpawnBeeParticles(float dt)
	{
		float dayLightStrength = Api.World.Calendar.GetDayLightStrength(Pos.X, Pos.Z);
		if (!(Api.World.Rand.NextDouble() > (double)(2f * dayLightStrength) - 0.5))
		{
			Random rand = Api.World.Rand;
			Bees.MinQuantity = activityLevel;
			if (Api.World.Rand.NextDouble() > 0.5)
			{
				startPos.Set((float)Pos.X + 0.5f, (float)Pos.Y + 0.5f, (float)Pos.Z + 0.5f);
				minVelo.Set((float)rand.NextDouble() * 3f - 1.5f, (float)rand.NextDouble() * 1f - 0.5f, (float)rand.NextDouble() * 3f - 1.5f);
				Bees.MinPos = startPos;
				Bees.MinVelocity = minVelo;
				Bees.LifeLength = 1f;
				Bees.WithTerrainCollision = true;
			}
			else
			{
				startPos.Set((double)Pos.X + rand.NextDouble() * 5.0 - 2.5, (double)Pos.Y + rand.NextDouble() * 2.0 - 1.0, (double)Pos.Z + rand.NextDouble() * 5.0 - 2.5);
				endPos.Set((float)Pos.X + 0.5f, (float)Pos.Y + 0.5f, (float)Pos.Z + 0.5f);
				minVelo.Set((float)(endPos.X - startPos.X), (float)(endPos.Y - startPos.Y), (float)(endPos.Z - startPos.Z));
				minVelo /= 2f;
				Bees.MinPos = startPos;
				Bees.MinVelocity = minVelo;
				Bees.WithTerrainCollision = true;
			}
			Api.World.SpawnParticles(Bees);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		wasPlaced = true;
		if (Api?.World != null)
		{
			harvestableAtTotalHours = Api.World.Calendar.TotalHours + 12.0 * (3.0 + Api.World.Rand.NextDouble() * 8.0);
		}
	}

	private void TestHarvestable(float dt)
	{
		double num = Api.World.Calendar.TotalHours - lastCheckedAtTotalHours;
		float num2 = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
		if (roomness > 0f)
		{
			num2 += 5f;
		}
		activityLevel = GameMath.Clamp(num2 / 5f, 0f, 1f);
		if (num2 <= 0f)
		{
			harvestableAtTotalHours += num;
			cooldownUntilTotalHours += num;
			beginPopStartTotalHours += num;
		}
		lastCheckedAtTotalHours = Api.World.Calendar.TotalHours;
		if (num2 <= -10f)
		{
			harvestableAtTotalHours = Api.World.Calendar.TotalHours + 12.0 * (3.0 + Api.World.Rand.NextDouble() * 8.0);
			cooldownUntilTotalHours = Api.World.Calendar.TotalHours + 48.0;
		}
		if (!Harvestable && !isWildHive && Api.World.Calendar.TotalHours > harvestableAtTotalHours && hivePopSize > EnumHivePopSize.Poor)
		{
			Harvestable = true;
			MarkDirty(redrawOnClient: true);
		}
	}

	private void OnScanForEmptySkep(float dt)
	{
		Room room = roomreg?.GetRoomForPosition(Pos);
		roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		if (activityLevel <= 0f || Api.Side == EnumAppSide.Client || Api.World.Calendar.TotalHours < cooldownUntilTotalHours)
		{
			return;
		}
		if (scanIteration == 0)
		{
			scanQuantityNearbyFlowers = 0;
			scanQuantityNearbyHives = 0;
			scanEmptySkeps.Clear();
		}
		int num = -8 + 8 * (scanIteration / 2);
		int num2 = -8 + 8 * (scanIteration % 2);
		int num3 = 8;
		Api.World.BlockAccessor.WalkBlocks(Pos.AddCopy(num, -7, num2), Pos.AddCopy(num + num3 - 1, 4, num2 + num3 - 1), delegate(Block block, int x, int y, int z)
		{
			if (block.Id != 0)
			{
				if (block.BlockMaterial == EnumBlockMaterial.Plant)
				{
					JsonObject attributes = block.Attributes;
					if (attributes != null && attributes.IsTrue("beeFeed"))
					{
						scanQuantityNearbyFlowers++;
					}
				}
				else
				{
					CollectibleObject collectibleObject = (block as BlockPlantContainer)?.GetContents(Api.World, new BlockPos(x, y, z))?.Collectible;
					if (collectibleObject != null)
					{
						JsonObject attributes2 = collectibleObject.Attributes;
						if (attributes2 != null && attributes2.IsTrue("beeFeed"))
						{
							scanQuantityNearbyFlowers++;
						}
					}
					else if (block is BlockSkep || block is BlockBeehive)
					{
						if (!block.Variant["type"].EqualsFast("empty"))
						{
							scanQuantityNearbyHives++;
						}
						else
						{
							scanEmptySkeps.Add(new BlockPos(x, y, z));
						}
					}
				}
			}
		});
		scanIteration++;
		if (scanIteration == 4)
		{
			scanIteration = 0;
			OnScanComplete();
		}
	}

	private void OnScanComplete()
	{
		quantityNearbyFlowers = scanQuantityNearbyFlowers;
		quantityNearbyHives = scanQuantityNearbyHives;
		emptySkeps = new List<BlockPos>(scanEmptySkeps);
		if (emptySkeps.Count == 0)
		{
			skepToPop = null;
		}
		hivePopSize = (EnumHivePopSize)GameMath.Clamp(quantityNearbyFlowers - 3 * quantityNearbyHives, 0, 2);
		MarkDirty();
		if (3 * quantityNearbyHives + 3 > quantityNearbyFlowers)
		{
			skepToPop = null;
			MarkDirty();
			return;
		}
		if (skepToPop != null && Api.World.Calendar.TotalHours > beginPopStartTotalHours + (double)popHiveAfterHours)
		{
			TryPopCurrentSkep();
			cooldownUntilTotalHours = Api.World.Calendar.TotalHours + 48.0;
			MarkDirty();
			return;
		}
		float num = (float)GameMath.Clamp(quantityNearbyFlowers - 3 - 3 * quantityNearbyHives, 0, 20) / 5f;
		float num2 = (4f - num) * 2.5f;
		if (num <= 0f)
		{
			skepToPop = null;
		}
		if (skepToPop != null)
		{
			float num3 = 24f * num2;
			popHiveAfterHours = (float)(0.75 * (double)popHiveAfterHours + 0.25 * (double)num3);
			if (!emptySkeps.Contains(skepToPop))
			{
				skepToPop = null;
				MarkDirty();
			}
			return;
		}
		popHiveAfterHours = 24f * num2;
		beginPopStartTotalHours = Api.World.Calendar.TotalHours;
		float num4 = 999f;
		BlockPos blockPos = new BlockPos();
		foreach (BlockPos emptySkep in emptySkeps)
		{
			float num5 = emptySkep.DistanceTo(Pos);
			if (num5 < num4)
			{
				num4 = num5;
				blockPos = emptySkep;
			}
		}
		skepToPop = blockPos;
	}

	private void TryPopCurrentSkep()
	{
		if (!(Api.World.BlockAccessor.GetBlock(skepToPop) is BlockSkep blockSkep))
		{
			skepToPop = null;
			return;
		}
		AssetLocation assetLocation = blockSkep.CodeWithVariant("type", "populated");
		if (!(Api.World.GetBlock(assetLocation) is BlockSkep blockSkep2))
		{
			Api.World.Logger.Warning("BEBeehive.TryPopSkep() - block with code {0} does not exist?", assetLocation.ToShortString());
		}
		else
		{
			Api.World.BlockAccessor.SetBlock(blockSkep2.BlockId, skepToPop);
			hivePopSize = EnumHivePopSize.Poor;
			skepToPop = null;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("scanIteration", scanIteration);
		tree.SetInt("quantityNearbyFlowers", quantityNearbyFlowers);
		tree.SetInt("quantityNearbyHives", quantityNearbyHives);
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int i = 0; i < emptySkeps.Count; i++)
		{
			treeAttribute.SetInt("posX-" + i, emptySkeps[i].X);
			treeAttribute.SetInt("posY-" + i, emptySkeps[i].Y);
			treeAttribute.SetInt("posZ-" + i, emptySkeps[i].Z);
		}
		tree["emptyskeps"] = treeAttribute;
		tree.SetInt("scanQuantityNearbyFlowers", scanQuantityNearbyFlowers);
		tree.SetInt("scanQuantityNearbyHives", scanQuantityNearbyHives);
		TreeAttribute treeAttribute2 = new TreeAttribute();
		for (int j = 0; j < scanEmptySkeps.Count; j++)
		{
			treeAttribute2.SetInt("posX-" + j, scanEmptySkeps[j].X);
			treeAttribute2.SetInt("posY-" + j, scanEmptySkeps[j].Y);
			treeAttribute2.SetInt("posZ-" + j, scanEmptySkeps[j].Z);
		}
		tree["scanEmptySkeps"] = treeAttribute2;
		tree.SetInt("isWildHive", isWildHive ? 1 : 0);
		tree.SetInt("harvestable", Harvestable ? 1 : 0);
		tree.SetInt("skepToPopX", (!(skepToPop == null)) ? skepToPop.X : 0);
		tree.SetInt("skepToPopY", (!(skepToPop == null)) ? skepToPop.Y : 0);
		tree.SetInt("skepToPopZ", (!(skepToPop == null)) ? skepToPop.Z : 0);
		tree.SetDouble("beginPopStartTotalHours", beginPopStartTotalHours);
		tree.SetFloat("popHiveAfterHours", popHiveAfterHours);
		tree.SetDouble("cooldownUntilTotalHours", cooldownUntilTotalHours);
		tree.SetDouble("harvestableAtTotalHours", harvestableAtTotalHours);
		tree.SetInt("hiveHealth", (int)hivePopSize);
		tree.SetFloat("roomness", roomness);
		tree.SetDouble("lastCheckedAtTotalHours", lastCheckedAtTotalHours);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bool harvestable = Harvestable;
		scanIteration = tree.GetInt("scanIteration");
		quantityNearbyFlowers = tree.GetInt("quantityNearbyFlowers");
		quantityNearbyHives = tree.GetInt("quantityNearbyHives");
		emptySkeps.Clear();
		TreeAttribute treeAttribute = tree["emptyskeps"] as TreeAttribute;
		for (int i = 0; i < treeAttribute.Count / 3; i++)
		{
			emptySkeps.Add(new BlockPos(treeAttribute.GetInt("posX-" + i), treeAttribute.GetInt("posY-" + i), treeAttribute.GetInt("posZ-" + i)));
		}
		scanQuantityNearbyFlowers = tree.GetInt("scanQuantityNearbyFlowers");
		scanQuantityNearbyHives = tree.GetInt("scanQuantityNearbyHives");
		scanEmptySkeps.Clear();
		TreeAttribute treeAttribute2 = tree["scanEmptySkeps"] as TreeAttribute;
		int num = 0;
		while (treeAttribute2 != null && num < treeAttribute2.Count / 3)
		{
			scanEmptySkeps.Add(new BlockPos(treeAttribute2.GetInt("posX-" + num), treeAttribute2.GetInt("posY-" + num), treeAttribute2.GetInt("posZ-" + num)));
			num++;
		}
		isWildHive = tree.GetInt("isWildHive") > 0;
		Harvestable = tree.GetInt("harvestable") > 0;
		int num2 = tree.GetInt("skepToPopX");
		int num3 = tree.GetInt("skepToPopY");
		int num4 = tree.GetInt("skepToPopZ");
		if (num2 != 0 || num3 != 0 || num4 != 0)
		{
			skepToPop = new BlockPos(num2, num3, num4);
		}
		else
		{
			skepToPop = null;
		}
		beginPopStartTotalHours = tree.GetDouble("beginPopStartTotalHours");
		popHiveAfterHours = tree.GetFloat("popHiveAfterHours");
		cooldownUntilTotalHours = tree.GetDouble("cooldownUntilTotalHours");
		harvestableAtTotalHours = tree.GetDouble("harvestableAtTotalHours");
		hivePopSize = (EnumHivePopSize)tree.GetInt("hiveHealth");
		roomness = tree.GetFloat("roomness");
		lastCheckedAtTotalHours = tree.GetDouble("lastCheckedAtTotalHours");
		if (Harvestable != harvestable && Api != null)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (Harvestable)
		{
			mesher.AddMeshData(Api.ObjectCache["beehive-" + material + "-harvestablemesh-" + orientation] as MeshData);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		string text = Lang.Get("population-" + hivePopSize);
		if (Api.World.EntityDebugMode && forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			dsc.AppendLine(Lang.Get("Nearby flowers: {0}, Nearby Hives: {1}, Empty Hives: {2}, Pop after hours: {3}. harvest in {4}, repop cooldown: {5}", quantityNearbyFlowers, quantityNearbyHives, emptySkeps.Count, (beginPopStartTotalHours + (double)popHiveAfterHours - Api.World.Calendar.TotalHours).ToString("#.##"), (harvestableAtTotalHours - Api.World.Calendar.TotalHours).ToString("#.##"), (cooldownUntilTotalHours - Api.World.Calendar.TotalHours).ToString("#.##")) + "\n" + Lang.Get("Population Size:") + text);
		}
		string text2 = Lang.Get("beehive-flowers-pop", quantityNearbyFlowers, text);
		if (skepToPop != null && Api.World.Calendar.TotalHours > cooldownUntilTotalHours)
		{
			double num = (beginPopStartTotalHours + (double)popHiveAfterHours - Api.World.Calendar.TotalHours) / (double)Api.World.Calendar.HoursPerDay;
			text2 = ((num > 1.5) ? (text2 + "\n" + Lang.Get("Will swarm in approx. {0} days", Math.Round(num))) : ((!(num > 0.5)) ? (text2 + "\n" + Lang.Get("Will swarm in less than a day")) : (text2 + "\n" + Lang.Get("Will swarm in approx. one day"))));
		}
		if (roomness > 0f)
		{
			text2 = text2 + "\n" + Lang.Get("greenhousetempbonus");
		}
		dsc.AppendLine(text2);
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (isWildHive || !Harvestable)
		{
			return false;
		}
		if (diet == null)
		{
			return false;
		}
		return diet.WeightedFoodTags?.Contains((WeightedFoodTag wf) => wf.Code == "lootableSweet") == true;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		Api.World.BlockAccessor.BreakBlock(Pos, null);
		return 1f;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (!isWildHive && Api.Side == EnumAppSide.Server)
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
