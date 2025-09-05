using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockType : CollectibleType
{
	public static Cuboidf DefaultCollisionBox = new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f);

	public static RotatableCube DefaultCollisionBoxR = new RotatableCube(0f, 0f, 0f, 1f, 1f, 1f);

	[JsonProperty]
	public string EntityClass;

	[JsonProperty]
	public BlockEntityBehaviorType[] EntityBehaviors = Array.Empty<BlockEntityBehaviorType>();

	[JsonProperty]
	public EnumDrawType DrawType = EnumDrawType.JSON;

	[JsonProperty]
	public EnumRandomizeAxes RandomizeAxes;

	[JsonProperty]
	public bool RandomDrawOffset;

	[JsonProperty]
	public bool RandomizeRotations;

	[JsonProperty]
	public float RandomSizeAdjust;

	[JsonProperty]
	public EnumChunkRenderPass RenderPass;

	[JsonProperty]
	public EnumFaceCullMode FaceCullMode;

	[JsonProperty]
	public CompositeShape ShapeInventory;

	[JsonProperty]
	public CompositeShape Lod0Shape;

	[JsonProperty]
	public CompositeShape Lod2Shape;

	[JsonProperty]
	public bool DoNotRenderAtLod2;

	[JsonProperty]
	public bool Ambientocclusion = true;

	[JsonProperty]
	public BlockSounds Sounds;

	[JsonProperty]
	public Dictionary<string, CompositeTexture> TexturesInventory;

	[JsonProperty]
	public Dictionary<string, bool> SideOpaque;

	[JsonProperty]
	public Dictionary<string, bool> SideAo;

	[JsonProperty]
	public Dictionary<string, bool> EmitSideAo;

	[JsonProperty]
	public Dictionary<string, bool> SideSolid;

	[JsonProperty]
	public Dictionary<string, bool> SideSolidOpaqueAo;

	[JsonProperty]
	public string ClimateColorMap;

	[JsonProperty]
	public string SeasonColorMap;

	[JsonProperty]
	public int Replaceable;

	[JsonProperty]
	public int Fertility;

	[JsonProperty]
	public VertexFlags VertexFlags;

	[JsonProperty]
	public bool Frostable;

	[JsonProperty]
	public ushort LightAbsorption = 99;

	[JsonProperty]
	public float Resistance = 6f;

	[JsonProperty]
	public EnumBlockMaterial BlockMaterial = EnumBlockMaterial.Stone;

	[JsonProperty]
	public int RequiredMiningTier;

	[JsonProperty("CollisionBox")]
	private RotatableCube CollisionBoxR = DefaultCollisionBoxR.Clone();

	[JsonProperty("SelectionBox")]
	private RotatableCube SelectionBoxR = DefaultCollisionBoxR.Clone();

	[JsonProperty("CollisionSelectionBox")]
	private RotatableCube CollisionSelectionBoxR;

	[JsonProperty("ParticleCollisionBox")]
	private RotatableCube ParticleCollisionBoxR;

	[JsonProperty("CollisionBoxes")]
	private RotatableCube[] CollisionBoxesR;

	[JsonProperty("SelectionBoxes")]
	private RotatableCube[] SelectionBoxesR;

	[JsonProperty("CollisionSelectionBoxes")]
	private RotatableCube[] CollisionSelectionBoxesR;

	[JsonProperty("ParticleCollisionBoxes")]
	private RotatableCube[] ParticleCollisionBoxesR;

	public Cuboidf[] CollisionBoxes;

	public Cuboidf[] SelectionBoxes;

	public Cuboidf[] ParticleCollisionBoxes;

	[JsonProperty]
	public bool Climbable;

	[JsonProperty]
	public bool RainPermeable;

	[JsonProperty]
	public int LiquidLevel;

	[JsonProperty]
	public string LiquidCode;

	[JsonProperty]
	public float WalkspeedMultiplier = 1f;

	[JsonProperty]
	public float DragMultiplier = 1f;

	[JsonProperty]
	public BlockDropItemStack[] Drops;

	[JsonProperty]
	public BlockCropPropertiesType CropProps;

	[JsonProperty]
	public string[] AllowSpawnCreatureGroups = Block.DefaultAllowAllSpawns;

	[ThreadStatic]
	private static List<BlockBehavior> reusableBehaviorList;

	[ThreadStatic]
	private static List<CollectibleBehavior> reusableCollectibleBehaviorList;

	[ThreadStatic]
	private static List<string> reusableStringList;

	public BlockType()
	{
		Class = "Block";
		Shape = new CompositeShape
		{
			Base = new AssetLocation("game", "block/basic/cube")
		};
		GuiTransform = ModelTransform.BlockDefaultGui();
		FpHandTransform = ModelTransform.BlockDefaultFp();
		TpHandTransform = ModelTransform.BlockDefaultTp();
		GroundTransform = ModelTransform.BlockDefaultGround();
		MaxStackSize = 64;
	}

	internal override RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		return CreateResolvedType<BlockType>(api, fullcode, jobject, deserializer, variant);
	}

	public Block CreateBlock(ICoreServerAPI api)
	{
		Block block;
		if (api.ClassRegistry.GetBlockClass(Class) == null)
		{
			api.Server.Logger.Error("Block with code {0} has defined a block class {1}, no such class registered. Will ignore.", Code, Class);
			block = new Block();
		}
		else
		{
			block = api.ClassRegistry.CreateBlock(Class);
		}
		if (EntityClass != null)
		{
			if (api.ClassRegistry.GetBlockEntity(EntityClass) != null)
			{
				block.EntityClass = EntityClass;
			}
			else
			{
				api.Server.Logger.Error("Block with code {0} has defined a block entity class {1}, no such class registered. Will ignore.", Code, EntityClass);
			}
		}
		block.Code = Code;
		block.VariantStrict = Variant;
		block.Variant = new RelaxedReadOnlyDictionary<string, string>(Variant);
		block.Class = Class;
		block.LiquidSelectable = LiquidSelectable;
		block.LiquidCode = LiquidCode;
		block.BlockEntityBehaviors = ((BlockEntityBehaviorType[])EntityBehaviors?.Clone()) ?? Array.Empty<BlockEntityBehaviorType>();
		block.Tags = api.TagRegistry.BlockTagsToTagArray(Tags);
		if (block.EntityClass == null && block.BlockEntityBehaviors != null && block.BlockEntityBehaviors.Length != 0)
		{
			block.EntityClass = "Generic";
		}
		block.WalkSpeedMultiplier = WalkspeedMultiplier;
		block.DragMultiplier = DragMultiplier;
		block.Durability = Durability;
		block.Dimensions = Size ?? CollectibleObject.DefaultSize;
		block.DamagedBy = (EnumItemDamageSource[])DamagedBy?.Clone();
		block.Tool = Tool;
		block.DrawType = DrawType;
		block.Replaceable = Replaceable;
		block.Fertility = Fertility;
		block.LightAbsorption = LightAbsorption;
		block.LightHsv = LightHsv;
		block.VertexFlags = VertexFlags?.Clone() ?? new VertexFlags(0);
		block.Frostable = Frostable;
		block.Resistance = Resistance;
		block.BlockMaterial = BlockMaterial;
		block.Shape = Shape;
		block.Lod0Shape = Lod0Shape;
		block.Lod2Shape = Lod2Shape;
		block.ShapeInventory = ShapeInventory;
		block.DoNotRenderAtLod2 = DoNotRenderAtLod2;
		block.TexturesInventory = ((TexturesInventory == null) ? null : new FastSmallDictionary<string, CompositeTexture>(TexturesInventory));
		block.Textures = ((Textures == null) ? null : new FastSmallDictionary<string, CompositeTexture>(Textures));
		block.ClimateColorMap = ClimateColorMap;
		block.SeasonColorMap = SeasonColorMap;
		block.Ambientocclusion = Ambientocclusion;
		block.CollisionBoxes = CollisionBoxes;
		block.SelectionBoxes = SelectionBoxes;
		block.ParticleCollisionBoxes = ParticleCollisionBoxes;
		block.MaterialDensity = MaterialDensity;
		block.GuiTransform = GuiTransform;
		block.FpHandTransform = FpHandTransform;
		block.TpHandTransform = TpHandTransform;
		block.TpOffHandTransform = TpOffHandTransform;
		block.GroundTransform = GroundTransform;
		block.RenderPass = RenderPass;
		block.ParticleProperties = ParticleProperties;
		block.Climbable = Climbable;
		block.RainPermeable = RainPermeable;
		block.FaceCullMode = FaceCullMode;
		block.Drops = Drops;
		block.MaxStackSize = MaxStackSize;
		block.MatterState = MatterState;
		if (Attributes != null)
		{
			block.Attributes = Attributes.Clone();
		}
		block.NutritionProps = NutritionProps;
		block.TransitionableProps = TransitionableProps;
		block.GrindingProps = GrindingProps;
		block.CrushingProps = CrushingProps;
		block.LiquidLevel = LiquidLevel;
		block.AttackPower = AttackPower;
		block.MiningSpeed = MiningSpeed;
		block.ToolTier = ToolTier;
		block.RequiredMiningTier = RequiredMiningTier;
		block.HeldSounds = HeldSounds?.Clone();
		block.AttackRange = AttackRange;
		if (Sounds != null)
		{
			block.Sounds = Sounds.Clone();
		}
		block.RandomDrawOffset = (RandomDrawOffset ? 1 : 0);
		block.RandomizeRotations = RandomizeRotations;
		block.RandomizeAxes = RandomizeAxes;
		block.RandomSizeAdjust = RandomSizeAdjust;
		block.CombustibleProps = CombustibleProps;
		block.StorageFlags = (EnumItemStorageFlags)StorageFlags;
		block.RenderAlphaTest = RenderAlphaTest;
		block.HeldTpHitAnimation = HeldTpHitAnimation;
		block.HeldRightTpIdleAnimation = HeldRightTpIdleAnimation;
		block.HeldLeftTpIdleAnimation = HeldLeftTpIdleAnimation;
		block.HeldLeftReadyAnimation = HeldLeftReadyAnimation;
		block.HeldRightReadyAnimation = HeldRightReadyAnimation;
		block.HeldTpUseAnimation = HeldTpUseAnimation;
		block.CreativeInventoryStacks = ((CreativeInventoryStacks == null) ? null : ((CreativeTabAndStackList[])CreativeInventoryStacks.Clone()));
		block.AllowSpawnCreatureGroups = AllowSpawnCreatureGroups;
		block.AllCreaturesAllowed = AllowSpawnCreatureGroups.Length == 1 && AllowSpawnCreatureGroups[0] == "*";
		EnsureClientServerAccuracy(block.CollisionBoxes);
		EnsureClientServerAccuracy(block.SelectionBoxes);
		EnsureClientServerAccuracy(block.ParticleCollisionBoxes);
		InitBlock(api.ClassRegistry, api.World.Logger, block, Variant);
		return block;
	}

	private void EnsureClientServerAccuracy(Cuboidf[] boxes)
	{
		if (boxes != null)
		{
			for (int i = 0; i < boxes.Length; i++)
			{
				boxes[i].RoundToFracsOfOne10thousand();
			}
		}
	}

	private Cuboidf[] ToCuboidf(params RotatableCube[] cubes)
	{
		Cuboidf[] array = new Cuboidf[cubes.Length];
		for (int i = 0; i < cubes.Length; i++)
		{
			array[i] = cubes[i].RotatedCopy();
		}
		return array;
	}

	internal override void OnDeserialized()
	{
		base.OnDeserialized();
		if (CollisionBoxR != null)
		{
			CollisionBoxes = ToCuboidf(CollisionBoxR);
		}
		if (SelectionBoxR != null)
		{
			SelectionBoxes = ToCuboidf(SelectionBoxR);
		}
		if (ParticleCollisionBoxR != null)
		{
			ParticleCollisionBoxes = ToCuboidf(ParticleCollisionBoxR);
		}
		if (CollisionBoxesR != null)
		{
			CollisionBoxes = ToCuboidf(CollisionBoxesR);
		}
		if (SelectionBoxesR != null)
		{
			SelectionBoxes = ToCuboidf(SelectionBoxesR);
		}
		if (ParticleCollisionBoxesR != null)
		{
			ParticleCollisionBoxes = ToCuboidf(ParticleCollisionBoxesR);
		}
		if (CollisionSelectionBoxR != null)
		{
			CollisionBoxes = ToCuboidf(CollisionSelectionBoxR);
			SelectionBoxes = CloneArray(CollisionBoxes);
		}
		if (CollisionSelectionBoxesR != null)
		{
			CollisionBoxes = ToCuboidf(CollisionSelectionBoxesR);
			SelectionBoxes = CloneArray(CollisionBoxes);
		}
		ResolveStringBoolDictFaces(SideSolidOpaqueAo);
		ResolveStringBoolDictFaces(SideSolid);
		ResolveStringBoolDictFaces(SideOpaque);
		ResolveStringBoolDictFaces(SideAo);
		ResolveStringBoolDictFaces(EmitSideAo);
		if (SideSolidOpaqueAo != null && SideSolidOpaqueAo.Count > 0)
		{
			ResolveDict(SideSolidOpaqueAo, ref SideSolid);
			ResolveDict(SideSolidOpaqueAo, ref SideOpaque);
			ResolveDict(SideSolidOpaqueAo, ref EmitSideAo);
		}
		if (EmitSideAo == null)
		{
			EmitSideAo = new Dictionary<string, bool> { 
			{
				"all",
				LightAbsorption > 0
			} };
			ResolveStringBoolDictFaces(EmitSideAo);
		}
		if (LightHsv == null)
		{
			LightHsv = new byte[3];
		}
		LightHsv[0] = (byte)GameMath.Clamp(LightHsv[0], 0, ColorUtil.HueQuantities - 1);
		LightHsv[1] = (byte)GameMath.Clamp(LightHsv[1], 0, ColorUtil.SatQuantities - 1);
		LightHsv[2] = (byte)GameMath.Clamp(LightHsv[2], 0, ColorUtil.BrightQuantities - 1);
	}

	private Cuboidf[] CloneArray(Cuboidf[] array)
	{
		if (array == null)
		{
			return null;
		}
		int num = array.Length;
		Cuboidf[] array2 = new Cuboidf[num];
		for (int i = 0; i < num; i++)
		{
			array2[i] = array[i].Clone();
		}
		return array2;
	}

	private void ResolveDict(Dictionary<string, bool> sideSolidOpaqueAo, ref Dictionary<string, bool> targetDict)
	{
		bool flag = targetDict == null;
		if (flag)
		{
			targetDict = new Dictionary<string, bool> { { "all", true } };
		}
		foreach (KeyValuePair<string, bool> item in sideSolidOpaqueAo)
		{
			if (flag || !targetDict.ContainsKey(item.Key))
			{
				targetDict[item.Key] = item.Value;
			}
		}
		ResolveStringBoolDictFaces(targetDict);
	}

	public void InitBlock(IClassRegistryAPI instancer, ILogger logger, Block block, OrderedDictionary<string, string> searchReplace)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		CollectibleBehaviorType[] behaviors = Behaviors;
		if (behaviors != null)
		{
			List<BlockBehavior> list = reusableBehaviorList ?? (reusableBehaviorList = new List<BlockBehavior>());
			List<CollectibleBehavior> list2 = reusableCollectibleBehaviorList ?? (reusableCollectibleBehaviorList = new List<CollectibleBehavior>());
			list.Clear();
			list2.Clear();
			foreach (CollectibleBehaviorType collectibleBehaviorType in behaviors)
			{
				CollectibleBehavior collectibleBehavior = null;
				if (instancer.GetCollectibleBehaviorClass(collectibleBehaviorType.name) != null)
				{
					collectibleBehavior = instancer.CreateCollectibleBehavior(block, collectibleBehaviorType.name);
				}
				if (instancer.GetBlockBehaviorClass(collectibleBehaviorType.name) != null)
				{
					collectibleBehavior = instancer.CreateBlockBehavior(block, collectibleBehaviorType.name);
				}
				if (collectibleBehavior == null)
				{
					logger.Warning(Lang.Get("Block or Collectible behavior {0} for block {1} not found", collectibleBehaviorType.name, block.Code));
					continue;
				}
				if (collectibleBehaviorType.properties == null)
				{
					collectibleBehaviorType.properties = new JsonObject((JToken)new JObject());
				}
				try
				{
					collectibleBehavior.Initialize(collectibleBehaviorType.properties);
				}
				catch (Exception e)
				{
					logger.Error("Failed calling Initialize() on collectible or block behavior {0} for block {1}, using properties {2}. Will continue anyway. Exception", collectibleBehaviorType.name, block.Code, collectibleBehaviorType.properties.ToString());
					logger.Error(e);
				}
				list2.Add(collectibleBehavior);
				if (collectibleBehavior is BlockBehavior item)
				{
					list.Add(item);
				}
			}
			block.BlockBehaviors = list.ToArray();
			block.CollectibleBehaviors = list2.ToArray();
			list.Clear();
			list2.Clear();
		}
		if (CropProps != null)
		{
			block.CropProps = new BlockCropProperties();
			block.CropProps.GrowthStages = CropProps.GrowthStages;
			block.CropProps.HarvestGrowthStageLoss = CropProps.HarvestGrowthStageLoss;
			block.CropProps.MultipleHarvests = CropProps.MultipleHarvests;
			block.CropProps.NutrientConsumption = CropProps.NutrientConsumption;
			block.CropProps.RequiredNutrient = CropProps.RequiredNutrient;
			block.CropProps.TotalGrowthDays = CropProps.TotalGrowthDays;
			block.CropProps.TotalGrowthMonths = CropProps.TotalGrowthMonths;
			block.CropProps.ColdDamageBelow = CropProps.ColdDamageBelow;
			block.CropProps.HeatDamageAbove = CropProps.HeatDamageAbove;
			block.CropProps.DamageGrowthStuntMul = CropProps.DamageGrowthStuntMul;
			block.CropProps.ColdDamageRipeMul = CropProps.ColdDamageRipeMul;
			if (CropProps.Behaviors != null)
			{
				block.CropProps.Behaviors = new CropBehavior[CropProps.Behaviors.Length];
				for (int j = 0; j < CropProps.Behaviors.Length; j++)
				{
					CropBehaviorType cropBehaviorType = CropProps.Behaviors[j];
					CropBehavior cropBehavior = instancer.CreateCropBehavior(block, cropBehaviorType.name);
					if (cropBehaviorType.properties != null)
					{
						cropBehavior.Initialize(cropBehaviorType.properties);
					}
					block.CropProps.Behaviors[j] = cropBehavior;
				}
			}
		}
		if (block.Drops == null)
		{
			block.Drops = new BlockDropItemStack[1]
			{
				new BlockDropItemStack
				{
					Code = block.Code,
					Type = EnumItemClass.Block,
					Quantity = NatFloat.One
				}
			};
		}
		block.CreativeInventoryTabs = GetCreativeTabs(block.Code, CreativeInventory, searchReplace);
		if (SideOpaque != null && SideOpaque.Count > 0)
		{
			block.SideOpaque = new SmallBoolArray(63);
		}
		if (SideAo != null && SideAo.Count > 0)
		{
			block.SideAo = new SmallBoolArray(63);
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			if (SideAo != null && SideAo.TryGetValue(blockFacing.Code, out var value))
			{
				block.SideAo[blockFacing.Index] = value;
			}
			if (EmitSideAo != null && EmitSideAo.TryGetValue(blockFacing.Code, out var value2) && value2)
			{
				block.EmitSideAo |= blockFacing.Flag;
			}
			if (SideSolid != null && SideSolid.TryGetValue(blockFacing.Code, out var value3))
			{
				block.SideSolid[blockFacing.Index] = value3;
			}
			if (SideOpaque != null && SideOpaque.TryGetValue(blockFacing.Code, out var value4))
			{
				block.SideOpaque[blockFacing.Index] = value4;
			}
		}
		if (HeldRightReadyAnimation != null && HeldRightReadyAnimation == HeldRightTpIdleAnimation)
		{
			logger.Error("Block {0} HeldRightReadyAnimation and HeldRightTpIdleAnimation is set to the same animation {1}. This invalid and breaks stuff. Will set HeldRightReadyAnimation to null", Code, HeldRightTpIdleAnimation);
			HeldRightReadyAnimation = null;
		}
	}

	public static string[] GetCreativeTabs(AssetLocation code, Dictionary<string, string[]> CreativeInventory, OrderedDictionary<string, string> searchReplace)
	{
		List<string> list = reusableStringList ?? (reusableStringList = new List<string>());
		list.Clear();
		foreach (KeyValuePair<string, string[]> item in CreativeInventory)
		{
			for (int i = 0; i < item.Value.Length; i++)
			{
				if (WildcardUtil.Match(RegistryObject.FillPlaceHolder(item.Value[i], searchReplace), code.Path))
				{
					string key = item.Key;
					list.Add(string.Intern(key));
				}
			}
		}
		string[] result = list.ToArray();
		list.Clear();
		return result;
	}

	private void ResolveStringBoolDictFaces(Dictionary<string, bool> stringBoolDict)
	{
		if (stringBoolDict == null)
		{
			return;
		}
		BlockFacing[] hORIZONTALS;
		if (stringBoolDict.ContainsKey("horizontals"))
		{
			hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				if (!stringBoolDict.ContainsKey(blockFacing.Code))
				{
					stringBoolDict[blockFacing.Code] = stringBoolDict["horizontals"];
				}
			}
		}
		if (stringBoolDict.ContainsKey("verticals"))
		{
			hORIZONTALS = BlockFacing.VERTICALS;
			foreach (BlockFacing blockFacing2 in hORIZONTALS)
			{
				if (!stringBoolDict.ContainsKey(blockFacing2.Code))
				{
					stringBoolDict[blockFacing2.Code] = stringBoolDict["verticals"];
				}
			}
		}
		if (!stringBoolDict.ContainsKey("all"))
		{
			return;
		}
		hORIZONTALS = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing3 in hORIZONTALS)
		{
			if (!stringBoolDict.ContainsKey(blockFacing3.Code))
			{
				stringBoolDict[blockFacing3.Code] = stringBoolDict["all"];
			}
		}
	}
}
