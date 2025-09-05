using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorFruiting : BlockEntityBehavior
{
	private int positionsCount;

	private int maxFruit = 5;

	private int fruitStages = 6;

	private float maxGerminationDays = 6f;

	private float transitionDays = 1f;

	private float successfulGrowthChance = 0.75f;

	private string[] fruitCodeBases;

	private int ripeStage;

	private AssetLocation dropCode;

	private double[] points;

	private FruitingSystem manager;

	protected Vec3d[] positions;

	protected FruitData[] fruitPoints;

	private double dateLastChecked;

	public static float[] randomRotations;

	public static float[][] randomRotMatrices;

	public Vec4f LightRgba { get; internal set; }

	static BEBehaviorFruiting()
	{
		randomRotations = new float[8] { -22.5f, 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f };
		randomRotMatrices = new float[randomRotations.Length][];
		for (int i = 0; i < randomRotations.Length; i++)
		{
			float[] array = Mat4f.Create();
			Mat4f.Translate(array, array, 0.5f, 0.5f, 0.5f);
			Mat4f.RotateY(array, array, randomRotations[i] * ((float)Math.PI / 180f));
			Mat4f.Translate(array, array, -0.5f, -0.5f, -0.5f);
			randomRotMatrices[i] = array;
		}
	}

	public BEBehaviorFruiting(BlockEntity be)
		: base(be)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		dateLastChecked = Api.World.Calendar.TotalDays;
		fruitCodeBases = properties["fruitCodeBases"].AsArray(Array.Empty<string>());
		if (fruitCodeBases.Length == 0)
		{
			return;
		}
		positionsCount = properties["positions"].AsInt();
		if (positionsCount <= 0)
		{
			return;
		}
		string text = properties["maturePlant"].AsString();
		if (text == null || !(api.World.GetBlock(new AssetLocation(text)) is BlockFruiting blockFruiting))
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			points = blockFruiting.GetFruitingPoints();
		}
		maxFruit = properties["maxFruit"].AsInt(5);
		fruitStages = properties["fruitStages"].AsInt(6);
		maxGerminationDays = properties["maxGerminationDays"].AsFloat(6f);
		transitionDays = properties["transitionDays"].AsFloat(1f);
		successfulGrowthChance = properties["successfulGrowthChance"].AsFloat(0.75f);
		ripeStage = properties["ripeStage"].AsInt(fruitStages - 1);
		dropCode = new AssetLocation(properties["dropCode"].AsString());
		manager = Api.ModLoader.GetModSystem<FruitingSystem>();
		bool flag = false;
		if (Api.Side == EnumAppSide.Client && fruitPoints != null)
		{
			LightRgba = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
			flag = true;
		}
		InitializeArrays();
		if (flag)
		{
			for (int i = 0; i < positionsCount; i++)
			{
				FruitData fruitData = fruitPoints[i];
				if (fruitData.variant >= fruitCodeBases.Length)
				{
					fruitData.variant %= fruitCodeBases.Length;
				}
				if (fruitData.variant >= 0 && fruitData.currentStage > 0)
				{
					fruitData.SetRandomRotation(Api.World, i, positions[i], Blockentity.Pos);
					manager.AddFruit(new AssetLocation(fruitCodeBases[fruitData.variant] + fruitData.currentStage), positions[i], fruitData);
				}
			}
		}
		if (Api.Side == EnumAppSide.Server)
		{
			Blockentity.RegisterGameTickListener(CheckForGrowth, 2250);
		}
	}

	private void CheckForGrowth(float dt)
	{
		double num = GameMath.Clamp(Api.World.Calendar.SpeedOfTime / 60f, 0.1, 5.0);
		double totalDays = Api.World.Calendar.TotalDays;
		bool flag = totalDays > dateLastChecked + 0.5;
		dateLastChecked = totalDays;
		if (Api.World.Rand.NextDouble() > 0.2 * num && !flag)
		{
			return;
		}
		int num2 = 0;
		bool flag2 = false;
		FruitData[] array = fruitPoints;
		foreach (FruitData fruitData in array)
		{
			if (fruitData.variant >= 0)
			{
				if (fruitData.transitionDate == 0.0)
				{
					fruitData.transitionDate = GetGerminationDate();
					flag2 = true;
				}
				if (fruitData.currentStage > 0)
				{
					num2++;
				}
			}
		}
		bool flag3 = false;
		if (Blockentity.Block is BlockCrop blockCrop)
		{
			flag3 = blockCrop.CurrentCropStage == blockCrop.CropProps.GrowthStages;
		}
		array = fruitPoints;
		foreach (FruitData fruitData2 in array)
		{
			if (fruitData2.variant >= 0 && totalDays > fruitData2.transitionDate && (fruitData2.currentStage != 0 || num2 < maxFruit) && (!flag3 || fruitData2.currentStage >= fruitStages - 3))
			{
				if (++fruitData2.currentStage > fruitStages)
				{
					fruitData2.transitionDate = double.MaxValue;
					fruitData2.currentStage = fruitStages;
				}
				else
				{
					fruitData2.transitionDate = totalDays + (double)transitionDays * (1.0 + Api.World.Rand.NextDouble()) / 1.5 / PlantHealth() * ((fruitData2.currentStage == fruitStages - 1) ? 2.5 : 1.0);
				}
				flag2 = true;
			}
		}
		if (flag2)
		{
			Blockentity.MarkDirty();
		}
	}

	public void InitializeArrays()
	{
		if (fruitPoints == null)
		{
			fruitPoints = new FruitData[positionsCount];
			int num = Math.Abs(Blockentity.Pos.GetHashCode()) % fruitCodeBases.Length;
			for (int i = 0; i < positionsCount; i++)
			{
				int variant = i;
				if (i >= fruitCodeBases.Length)
				{
					variant = num++ % fruitCodeBases.Length;
				}
				fruitPoints[i] = new FruitData(variant, GetGerminationDate(), this, null);
			}
		}
		positions = new Vec3d[positionsCount];
		Vec3f vec3f = new Vec3f();
		float[] array = null;
		if (Blockentity.Block.RandomizeRotations)
		{
			int k = GameMath.MurmurHash3(-Blockentity.Pos.X, (Blockentity.Block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? Blockentity.Pos.Y : 0, Blockentity.Pos.Z);
			array = randomRotMatrices[GameMath.Mod(k, randomRotations.Length)];
		}
		for (int j = 0; j < positionsCount; j++)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				positions[j] = new Vec3d(points[j * 3], points[j * 3 + 1], points[j * 3 + 2]);
				if (array != null)
				{
					Mat4f.MulWithVec3_Position(array, (float)positions[j].X, (float)positions[j].Y, (float)positions[j].Z, vec3f);
					positions[j].X = vec3f.X;
					positions[j].Y = vec3f.Y;
					positions[j].Z = vec3f.Z;
				}
			}
			else
			{
				positions[j] = new Vec3d((j + 1) / positionsCount, (j + 1) / positionsCount, (j + 1) / positionsCount);
			}
			positions[j].Add(Blockentity.Pos);
		}
	}

	private double GetGerminationDate()
	{
		double num = (1.0 / PlantHealth() + 0.25) / 1.25;
		double num2 = Api.World.Rand.NextDouble() / (double)successfulGrowthChance * num;
		if (!(num2 > 1.0))
		{
			return Api.World.Calendar.TotalDays + num2 * (double)maxGerminationDays;
		}
		return double.MaxValue;
	}

	private double PlantHealth()
	{
		BlockPos position = Blockentity.Pos.DownCopy();
		if (Blockentity.Api.World.BlockAccessor.GetBlockEntity(position) is BlockEntityFarmland blockEntityFarmland)
		{
			return blockEntityFarmland.GetGrowthRate();
		}
		return 1.0;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		LightRgba = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (positionsCount == 0)
		{
			positionsCount = tree.GetInt("count");
		}
		if (positionsCount == 0)
		{
			positionsCount = 10;
		}
		if (fruitPoints == null)
		{
			fruitPoints = new FruitData[positionsCount];
		}
		for (int i = 0; i < positionsCount; i++)
		{
			double num = tree.GetDouble("td" + i);
			int variant = tree.GetInt("var" + i);
			int currentStage = tree.GetInt("tc" + i);
			FruitData fruitData = fruitPoints[i];
			if (fruitData == null)
			{
				fruitData = new FruitData(-1, num, this, null);
				fruitPoints[i] = fruitData;
			}
			if (Api is ICoreClientAPI && fruitData.variant >= 0)
			{
				manager.RemoveFruit(fruitCodeBases[fruitData.variant] + fruitData.currentStage, positions[i]);
			}
			fruitData.variant = variant;
			fruitData.currentStage = currentStage;
			fruitData.transitionDate = num;
			if (Api is ICoreClientAPI && fruitData.variant >= 0 && fruitData.currentStage > 0)
			{
				fruitData.SetRandomRotation(Api.World, i, positions[i], Blockentity.Pos);
				manager.AddFruit(new AssetLocation(fruitCodeBases[fruitData.variant] + fruitData.currentStage), positions[i], fruitData);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("count", positionsCount);
		for (int i = 0; i < positionsCount; i++)
		{
			FruitData fruitData = fruitPoints[i];
			if (fruitData != null)
			{
				tree.SetDouble("td" + i, fruitData.transitionDate);
				tree.SetInt("var" + i, fruitData.variant);
				tree.SetInt("tc" + i, fruitData.currentStage);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		if (Api is ICoreClientAPI)
		{
			RemoveRenderedFruits();
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api is ICoreClientAPI)
		{
			RemoveRenderedFruits();
		}
		int num = 0;
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData fruitData = fruitPoints[i];
			if (fruitData.variant < 0 || fruitData.currentStage == 0)
			{
				continue;
			}
			Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[fruitData.variant] + fruitData.currentStage));
			if (item != null && (item.Attributes == null || !item.Attributes["onGround"].AsBool()))
			{
				if (fruitData.currentStage == ripeStage)
				{
					num++;
				}
				else if (Math.Abs(fruitData.currentStage - ripeStage) == 1 && Api.World.Rand.NextDouble() > 0.5)
				{
					num++;
				}
			}
		}
		if (num > 0)
		{
			ItemStack itemstack = new ItemStack(Api.World.GetItem(dropCode), num);
			Api.World.SpawnItemEntity(itemstack, Blockentity.Pos.ToVec3d().Add(0.5, 0.25, 0.5));
		}
	}

	public virtual void RemoveRenderedFruits()
	{
		if (positions == null || fruitCodeBases == null)
		{
			return;
		}
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData fruitData = fruitPoints[i];
			if (fruitData.variant >= 0 && fruitData.currentStage > 0)
			{
				manager.RemoveFruit(fruitCodeBases[fruitData.variant] + fruitData.currentStage, positions[i]);
			}
		}
	}

	public virtual bool OnPlayerInteract(float secondsUsed, IPlayer player, Vec3d hit)
	{
		if (player == null || player.InventoryManager?.ActiveTool != EnumTool.Knife)
		{
			return false;
		}
		if (Api.Side == EnumAppSide.Server)
		{
			return true;
		}
		bool flag = false;
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData fruitData = fruitPoints[i];
			if (fruitData.variant >= 0 && fruitData.currentStage >= ripeStage)
			{
				Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[fruitData.variant] + fruitData.currentStage));
				if (item != null && (item.Attributes == null || !item.Attributes["onGround"].AsBool()))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			return secondsUsed < 0.3f;
		}
		return false;
	}

	public virtual void OnPlayerInteractStop(float secondsUsed, IPlayer player, Vec3d hit)
	{
		if (secondsUsed < 0.2f)
		{
			return;
		}
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData fruitData = fruitPoints[i];
			if (fruitData.variant < 0 || fruitData.currentStage < ripeStage)
			{
				continue;
			}
			Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[fruitData.variant] + fruitData.currentStage));
			if (item == null || (item.Attributes != null && item.Attributes["onGround"].AsBool()))
			{
				continue;
			}
			if (Api.Side == EnumAppSide.Client)
			{
				manager.RemoveFruit(fruitCodeBases[fruitData.variant] + fruitData.currentStage, positions[i]);
			}
			fruitData.variant = -1;
			fruitData.transitionDate = double.MaxValue;
			if (fruitData.currentStage >= ripeStage)
			{
				double posx = (double)Blockentity.Pos.X + hit.X;
				double posy = (double)Blockentity.Pos.Y + hit.Y;
				double posz = (double)Blockentity.Pos.Z + hit.Z;
				player.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/squish1"), posx, posy, posz, player, 1.1f + (float)Api.World.Rand.NextDouble() * 0.4f, 16f, 0.25f);
				double totalDays = Api.World.Calendar.TotalDays;
				for (int j = 0; j < fruitPoints.Length; j++)
				{
					fruitData = fruitPoints[j];
					if (fruitData.variant >= 0 && fruitData.currentStage == 0 && fruitData.transitionDate < totalDays)
					{
						fruitData.transitionDate = totalDays + Api.World.Rand.NextDouble() * (double)maxGerminationDays / 2.0;
					}
				}
				ItemStack itemStack = new ItemStack(Api.World.GetItem(dropCode));
				if (!player.InventoryManager.TryGiveItemstack(itemStack))
				{
					Api.World.SpawnItemEntity(itemStack, Blockentity.Pos.ToVec3d().Add(0.5, 0.25, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from {2} at {3}.", player.PlayerName, itemStack.Collectible.Code, Blockentity.Block.Code, Blockentity.Pos);
			}
			Blockentity.MarkDirty();
			break;
		}
	}
}
