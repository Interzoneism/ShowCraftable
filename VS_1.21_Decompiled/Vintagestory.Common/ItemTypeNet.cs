using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class ItemTypeNet : CollectibleNet
{
	private static ModelTransform tfDefaultGui = ModelTransform.ItemDefaultGui();

	private static ModelTransform tfDefaultFp = ModelTransform.ItemDefaultFp();

	private static ModelTransform tfDefaultTp = ModelTransform.ItemDefaultTp();

	private static ModelTransform tfDefaultGround = ModelTransform.ItemDefaultGround();

	public static Item ReadItemTypePacket(Packet_ItemType packet, IWorldAccessor world, ClassRegistry registry)
	{
		Item item = registry.CreateItem(packet.ItemClass);
		item.Code = new AssetLocation(packet.Code);
		item.IsMissing = packet.IsMissing > 0;
		item.ItemId = packet.ItemId;
		item.Tags = ((packet.Tags != null) ? new ItemTagArray(packet.Tags.Select((int tag) => (ushort)tag)) : new ItemTagArray());
		item.MaxStackSize = packet.MaxStackSize;
		item.VariantStrict = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount);
		item.Variant = new RelaxedReadOnlyDictionary<string, string>(item.VariantStrict);
		item.Dimensions = ((packet.Width + packet.Height + packet.Length == 0) ? CollectibleObject.DefaultSize : new Size3f(CollectibleNet.DeserializeFloatVeryPrecise(packet.Width), CollectibleNet.DeserializeFloatVeryPrecise(packet.Height), CollectibleNet.DeserializeFloatVeryPrecise(packet.Length)));
		if (packet.LightHsv != null && packet.LightHsvCount > 2)
		{
			item.LightHsv = new byte[3]
			{
				(byte)packet.LightHsv[0],
				(byte)packet.LightHsv[1],
				(byte)packet.LightHsv[2]
			};
		}
		else
		{
			item.LightHsv = new byte[3];
		}
		if (packet.BehaviorsCount > 0)
		{
			List<CollectibleBehavior> list = new List<CollectibleBehavior>();
			for (int num = 0; num < packet.BehaviorsCount; num++)
			{
				Packet_Behavior packet_Behavior = packet.Behaviors[num];
				bool flag = registry.collectibleBehaviorToTypeMapping.ContainsKey(packet_Behavior.Code);
				if (packet_Behavior.ClientSideOptional <= 0 || flag)
				{
					CollectibleBehavior collectibleBehavior = registry.CreateCollectibleBehavior(item, packet_Behavior.Code);
					JsonObject properties = ((!(packet_Behavior.Attributes != "")) ? new JsonObject(JToken.Parse("{}")) : new JsonObject(JToken.Parse(packet_Behavior.Attributes)));
					collectibleBehavior.Initialize(properties);
					list.Add(collectibleBehavior);
				}
			}
			item.CollectibleBehaviors = list.ToArray();
		}
		item.Textures = new Dictionary<string, CompositeTexture>(packet.TextureCodesCount);
		for (int num2 = 0; num2 < packet.TextureCodesCount; num2++)
		{
			item.Textures[packet.TextureCodes[num2]] = CollectibleNet.FromPacket(packet.CompositeTextures[num2]);
		}
		item.CreativeInventoryTabs = new string[packet.CreativeInventoryTabsCount];
		for (int num3 = 0; num3 < item.CreativeInventoryTabs.Length; num3++)
		{
			item.CreativeInventoryTabs[num3] = packet.CreativeInventoryTabs[num3];
		}
		if (item.IsMissing)
		{
			item.GuiTransform = CollectibleNet.DefGuiTransform;
			item.FpHandTransform = CollectibleNet.DefFpHandTransform;
			item.TpHandTransform = CollectibleNet.DefTpHandTransform;
			item.TpOffHandTransform = CollectibleNet.DefTpOffHandTransform;
			item.GroundTransform = CollectibleNet.DefGroundTransform;
		}
		else
		{
			item.GuiTransform = ((packet.GuiTransform == null) ? ModelTransform.ItemDefaultGui() : CollectibleNet.FromTransformPacket(packet.GuiTransform));
			item.FpHandTransform = ((packet.FpHandTransform == null) ? ModelTransform.ItemDefaultFp() : CollectibleNet.FromTransformPacket(packet.FpHandTransform));
			item.TpHandTransform = ((packet.TpHandTransform == null) ? ModelTransform.ItemDefaultTp() : CollectibleNet.FromTransformPacket(packet.TpHandTransform));
			item.TpOffHandTransform = ((packet.TpOffHandTransform == null) ? item.TpHandTransform.Clone() : CollectibleNet.FromTransformPacket(packet.TpOffHandTransform));
			item.GroundTransform = ((packet.GroundTransform == null) ? ModelTransform.ItemDefaultGround() : CollectibleNet.FromTransformPacket(packet.GroundTransform));
		}
		item.MatterState = (EnumMatterState)packet.MatterState;
		if (packet.HeldSounds != null)
		{
			item.HeldSounds = CollectibleNet.FromPacket(packet.HeldSounds);
		}
		if (packet.Miningmaterial != null)
		{
			item.MiningSpeed = new Dictionary<EnumBlockMaterial, float>();
			for (int num4 = 0; num4 < packet.MiningmaterialCount; num4++)
			{
				int key = packet.Miningmaterial[num4];
				float value = CollectibleNet.DeserializeFloat(packet.Miningmaterialspeed[num4]);
				item.MiningSpeed[(EnumBlockMaterial)key] = value;
			}
		}
		item.Durability = packet.Durability;
		item.DamagedBy = new EnumItemDamageSource[packet.DamagedbyCount];
		for (int num5 = 0; num5 < packet.DamagedbyCount; num5++)
		{
			item.DamagedBy[num5] = (EnumItemDamageSource)packet.Damagedby[num5];
		}
		if (packet.Attributes != null && packet.Attributes.Length > 0)
		{
			item.Attributes = new JsonObject(JToken.Parse(packet.Attributes));
		}
		if (packet.CombustibleProps != null)
		{
			item.CombustibleProps = CollectibleNet.FromPacket(packet.CombustibleProps, world);
		}
		if (packet.NutritionProps != null)
		{
			item.NutritionProps = CollectibleNet.FromPacket(packet.NutritionProps, world);
		}
		if (packet.TransitionableProps != null)
		{
			item.TransitionableProps = CollectibleNet.FromPacket(packet.TransitionableProps, world);
		}
		if (packet.GrindingProps != null)
		{
			item.GrindingProps = CollectibleNet.FromPacket(packet.GrindingProps, world);
		}
		if (packet.CrushingProps != null)
		{
			item.CrushingProps = CollectibleNet.FromPacket(packet.CrushingProps, world);
		}
		if (packet.CreativeInventoryStacks != null)
		{
			using MemoryStream input = new MemoryStream(packet.CreativeInventoryStacks);
			BinaryReader binaryReader = new BinaryReader(input);
			int num6 = binaryReader.ReadInt32();
			item.CreativeInventoryStacks = new CreativeTabAndStackList[num6];
			for (int num7 = 0; num7 < num6; num7++)
			{
				item.CreativeInventoryStacks[num7] = new CreativeTabAndStackList();
				item.CreativeInventoryStacks[num7].FromBytes(binaryReader, world.ClassRegistry);
			}
		}
		if (packet.Shape != null)
		{
			item.Shape = CollectibleNet.FromPacket(packet.Shape);
		}
		if (packet.Tool >= 0)
		{
			item.Tool = (EnumTool)packet.Tool;
		}
		item.MaterialDensity = packet.MaterialDensity;
		item.AttackPower = CollectibleNet.DeserializeFloatPrecise(packet.AttackPower);
		item.AttackRange = CollectibleNet.DeserializeFloatPrecise(packet.AttackRange);
		item.LiquidSelectable = packet.LiquidSelectable > 0;
		item.ToolTier = packet.MiningTier;
		item.StorageFlags = (EnumItemStorageFlags)packet.StorageFlags;
		item.RenderAlphaTest = CollectibleNet.DeserializeFloatVeryPrecise(packet.RenderAlphaTest);
		item.HeldTpHitAnimation = packet.HeldTpHitAnimation;
		item.HeldRightTpIdleAnimation = packet.HeldRightTpIdleAnimation;
		item.HeldLeftTpIdleAnimation = packet.HeldLeftTpIdleAnimation;
		item.HeldLeftReadyAnimation = packet.HeldLeftReadyAnimation;
		item.HeldRightReadyAnimation = packet.HeldRightReadyAnimation;
		item.HeldTpUseAnimation = packet.HeldTpUseAnimation;
		return item;
	}

	public static Packet_ItemType GetItemTypePacket(Item item, IClassRegistryAPI registry)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return GetItemTypePacket(item, registry, ms);
	}

	public static Packet_ItemType GetItemTypePacket(Item item, IClassRegistryAPI registry, FastMemoryStream ms)
	{
		Packet_ItemType packet_ItemType = new Packet_ItemType();
		if (item == null)
		{
			return packet_ItemType;
		}
		packet_ItemType.Code = item.Code.ToShortString();
		packet_ItemType.SetVariant(CollectibleNet.ToPacket(item.VariantStrict));
		packet_ItemType.ItemId = item.ItemId;
		packet_ItemType.Tags = item.Tags.ToArray().Select((System.Func<ushort, int>)((ushort tag) => tag)).ToArray();
		packet_ItemType.MaxStackSize = item.MaxStackSize;
		packet_ItemType.IsMissing = (item.IsMissing ? 1 : 0);
		packet_ItemType.ItemClass = registry.ItemClassToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == item.GetType()).Key;
		if (item.Dimensions != null)
		{
			packet_ItemType.Width = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Width);
			packet_ItemType.Height = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Height);
			packet_ItemType.Length = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Length);
		}
		packet_ItemType.SetLightHsv(new int[3]
		{
			item.LightHsv[0],
			item.LightHsv[1],
			item.LightHsv[2]
		});
		if (item.Textures != null)
		{
			packet_ItemType.SetTextureCodes(item.Textures.Keys.ToArray());
			packet_ItemType.SetCompositeTextures(CollectibleNet.ToPackets(item.Textures.Values.ToArray()));
		}
		if (item.CreativeInventoryTabs != null)
		{
			packet_ItemType.SetCreativeInventoryTabs(item.CreativeInventoryTabs);
		}
		if (item.CollectibleBehaviors != null)
		{
			Packet_Behavior[] array = new Packet_Behavior[item.CollectibleBehaviors.Length];
			int num = 0;
			CollectibleBehavior[] collectibleBehaviors = item.CollectibleBehaviors;
			foreach (CollectibleBehavior collectibleBehavior in collectibleBehaviors)
			{
				array[num++] = new Packet_Behavior
				{
					Code = registry.GetCollectibleBehaviorClassName(collectibleBehavior.GetType()),
					Attributes = (collectibleBehavior.propertiesAtString ?? ""),
					ClientSideOptional = (collectibleBehavior.ClientSideOptional ? 1 : 0)
				};
			}
			packet_ItemType.SetBehaviors(array);
		}
		if (!item.IsMissing)
		{
			if (item.GuiTransform != null)
			{
				packet_ItemType.GuiTransform = CollectibleNet.ToTransformPacket(item.GuiTransform, tfDefaultGui);
			}
			if (item.FpHandTransform != null)
			{
				packet_ItemType.FpHandTransform = CollectibleNet.ToTransformPacket(item.FpHandTransform, tfDefaultFp);
			}
			if (item.TpHandTransform != null)
			{
				packet_ItemType.TpHandTransform = CollectibleNet.ToTransformPacket(item.TpHandTransform, tfDefaultTp);
			}
			if (item.TpOffHandTransform != null)
			{
				packet_ItemType.TpOffHandTransform = CollectibleNet.ToTransformPacket(item.TpOffHandTransform, tfDefaultTp);
			}
			if (item.GroundTransform != null)
			{
				packet_ItemType.GroundTransform = CollectibleNet.ToTransformPacket(item.GroundTransform, tfDefaultGround);
			}
		}
		if (item.MiningSpeed != null)
		{
			Enum.GetValues(typeof(EnumBlockMaterial));
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			foreach (KeyValuePair<EnumBlockMaterial, float> item2 in item.MiningSpeed)
			{
				list.Add(CollectibleNet.SerializeFloat(item2.Value));
				list2.Add((int)item2.Key);
			}
			packet_ItemType.SetMiningmaterial(list2.ToArray());
			packet_ItemType.SetMiningmaterialspeed(list.ToArray());
		}
		if (item.HeldSounds != null)
		{
			packet_ItemType.HeldSounds = CollectibleNet.ToPacket(item.HeldSounds);
		}
		packet_ItemType.Durability = item.Durability;
		if (item.DamagedBy != null)
		{
			int[] array2 = new int[item.DamagedBy.Length];
			for (int num3 = 0; num3 < array2.Length; num3++)
			{
				array2[num3] = (int)item.DamagedBy[num3];
			}
			packet_ItemType.SetDamagedby(array2);
		}
		if (item.Attributes != null)
		{
			packet_ItemType.Attributes = item.Attributes.ToString();
		}
		if (item.CombustibleProps != null)
		{
			packet_ItemType.CombustibleProps = CollectibleNet.ToPacket(item.CombustibleProps, ms);
		}
		if (item.NutritionProps != null)
		{
			packet_ItemType.NutritionProps = CollectibleNet.ToPacket(item.NutritionProps, ms);
		}
		if (item.TransitionableProps != null)
		{
			packet_ItemType.SetTransitionableProps(CollectibleNet.ToPacket(item.TransitionableProps, ms));
		}
		if (item.GrindingProps != null)
		{
			packet_ItemType.GrindingProps = CollectibleNet.ToPacket(item.GrindingProps, ms);
		}
		if (item.CrushingProps != null)
		{
			packet_ItemType.CrushingProps = CollectibleNet.ToPacket(item.CrushingProps, ms);
		}
		if (item.CreativeInventoryStacks != null)
		{
			ms.Reset();
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			binaryWriter.Write(item.CreativeInventoryStacks.Length);
			for (int num4 = 0; num4 < item.CreativeInventoryStacks.Length; num4++)
			{
				item.CreativeInventoryStacks[num4].ToBytes(binaryWriter, registry);
			}
			packet_ItemType.SetCreativeInventoryStacks(ms.ToArray());
		}
		if (item.Shape != null)
		{
			packet_ItemType.Shape = CollectibleNet.ToPacket(item.Shape);
		}
		if (!item.Tool.HasValue)
		{
			packet_ItemType.Tool = -1;
		}
		else
		{
			packet_ItemType.Tool = (int)item.Tool.Value;
		}
		packet_ItemType.MaterialDensity = item.MaterialDensity;
		packet_ItemType.AttackPower = CollectibleNet.SerializeFloatPrecise(item.AttackPower);
		packet_ItemType.AttackRange = CollectibleNet.SerializeFloatPrecise(item.AttackRange);
		packet_ItemType.LiquidSelectable = (item.LiquidSelectable ? 1 : 0);
		packet_ItemType.MiningTier = item.ToolTier;
		packet_ItemType.StorageFlags = (int)item.StorageFlags;
		packet_ItemType.RenderAlphaTest = CollectibleNet.SerializeFloatVeryPrecise(item.RenderAlphaTest);
		packet_ItemType.HeldTpHitAnimation = item.HeldTpHitAnimation;
		packet_ItemType.HeldRightTpIdleAnimation = item.HeldRightTpIdleAnimation;
		packet_ItemType.HeldLeftTpIdleAnimation = item.HeldLeftTpIdleAnimation;
		packet_ItemType.HeldLeftReadyAnimation = item.HeldLeftReadyAnimation;
		packet_ItemType.HeldRightReadyAnimation = item.HeldRightReadyAnimation;
		packet_ItemType.HeldTpUseAnimation = item.HeldTpUseAnimation;
		packet_ItemType.MatterState = (int)item.MatterState;
		return packet_ItemType;
	}
}
