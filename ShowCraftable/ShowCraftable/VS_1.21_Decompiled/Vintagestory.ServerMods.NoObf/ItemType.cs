using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class ItemType : CollectibleType
{
	public ItemType()
	{
		Class = "Item";
		GuiTransform = ModelTransform.ItemDefaultGui();
		FpHandTransform = ModelTransform.ItemDefaultFp();
		TpHandTransform = ModelTransform.ItemDefaultTp();
		TpOffHandTransform = null;
		GroundTransform = ModelTransform.ItemDefaultGround();
	}

	internal override RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		ItemType itemType = CreateResolvedType<ItemType>(api, fullcode, jobject, deserializer, variant);
		if (itemType.Shape != null && !itemType.Shape.VoxelizeTexture)
		{
			JToken obj = jobject["guiTransform"];
			if (((obj != null) ? obj[(object)"rotate"] : null) == null)
			{
				GuiTransform = ModelTransform.ItemDefaultGui();
				GuiTransform.Rotate = true;
			}
		}
		return itemType;
	}

	public void InitItem(IClassRegistryAPI instancer, ILogger logger, Item item, OrderedDictionary<string, string> searchReplace)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		item.CreativeInventoryTabs = BlockType.GetCreativeTabs(item.Code, CreativeInventory, searchReplace);
		CollectibleBehaviorType[] behaviors = Behaviors;
		if (behaviors == null)
		{
			return;
		}
		List<CollectibleBehavior> list = new List<CollectibleBehavior>();
		foreach (CollectibleBehaviorType collectibleBehaviorType in behaviors)
		{
			if (instancer.GetCollectibleBehaviorClass(collectibleBehaviorType.name) != null)
			{
				CollectibleBehavior collectibleBehavior = instancer.CreateCollectibleBehavior(item, collectibleBehaviorType.name);
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
					logger.Error("Failed calling Initialize() on collectible behavior {0} for item {1}, using properties {2}. Will continue anyway.", collectibleBehaviorType.name, item.Code, collectibleBehaviorType.properties.ToString());
					logger.Error(e);
				}
				list.Add(collectibleBehavior);
			}
			else
			{
				logger.Warning(Lang.Get("Collectible behavior {0} for item {1} not found", collectibleBehaviorType.name, item.Code));
			}
		}
		item.CollectibleBehaviors = list.ToArray();
	}

	public Item CreateItem(ICoreServerAPI api)
	{
		Item item;
		if (api.ClassRegistry.GetItemClass(Class) == null)
		{
			api.Server.Logger.Error("Item with code {0} has defined an item class {1}, but no such class registered. Will ignore.", Code, Class);
			item = new Item();
		}
		else
		{
			item = api.ClassRegistry.CreateItem(Class);
		}
		item.Code = Code;
		item.VariantStrict = Variant;
		item.Variant = new RelaxedReadOnlyDictionary<string, string>(Variant);
		item.Class = Class;
		item.Textures = Textures;
		item.MaterialDensity = MaterialDensity;
		item.Tags = api.TagRegistry.ItemTagsToTagArray(Tags);
		item.GuiTransform = GuiTransform;
		item.FpHandTransform = FpHandTransform;
		item.TpHandTransform = TpHandTransform;
		item.TpOffHandTransform = TpOffHandTransform;
		item.GroundTransform = GroundTransform;
		item.LightHsv = LightHsv;
		item.DamagedBy = (EnumItemDamageSource[])DamagedBy?.Clone();
		item.MaxStackSize = MaxStackSize;
		if (Attributes != null)
		{
			item.Attributes = Attributes;
		}
		item.CombustibleProps = CombustibleProps;
		item.NutritionProps = NutritionProps;
		item.TransitionableProps = TransitionableProps;
		item.GrindingProps = GrindingProps;
		item.CrushingProps = CrushingProps;
		item.Shape = Shape;
		item.Tool = Tool;
		item.AttackPower = AttackPower;
		item.LiquidSelectable = LiquidSelectable;
		item.ToolTier = ToolTier;
		item.HeldSounds = HeldSounds?.Clone();
		item.Durability = Durability;
		item.Dimensions = Size ?? CollectibleObject.DefaultSize;
		item.MiningSpeed = MiningSpeed;
		item.AttackRange = AttackRange;
		item.StorageFlags = (EnumItemStorageFlags)StorageFlags;
		item.RenderAlphaTest = RenderAlphaTest;
		item.HeldTpHitAnimation = HeldTpHitAnimation;
		item.HeldRightTpIdleAnimation = HeldRightTpIdleAnimation;
		item.HeldLeftTpIdleAnimation = HeldLeftTpIdleAnimation;
		item.HeldLeftReadyAnimation = HeldLeftReadyAnimation;
		item.HeldRightReadyAnimation = HeldRightReadyAnimation;
		item.HeldTpUseAnimation = HeldTpUseAnimation;
		item.CreativeInventoryStacks = ((CreativeInventoryStacks == null) ? null : ((CreativeTabAndStackList[])CreativeInventoryStacks.Clone()));
		item.MatterState = MatterState;
		item.ParticleProperties = ParticleProperties;
		InitItem(api.ClassRegistry, api.World.Logger, item, Variant);
		return item;
	}
}
