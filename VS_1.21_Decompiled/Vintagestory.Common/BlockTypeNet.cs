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

public class BlockTypeNet : CollectibleNet
{
	public static Block ReadBlockTypePacket(Packet_BlockType packet, IWorldAccessor world, ClassRegistry registry)
	{
		Block block = registry.CreateBlock(packet.Blockclass);
		block.IsMissing = packet.IsMissing > 0;
		block.Code = new AssetLocation(packet.Code);
		block.Tags = ((packet.Tags != null) ? new BlockTagArray(packet.Tags.Select((int tag) => (ushort)tag)) : new BlockTagArray());
		block.Class = packet.Blockclass;
		block.VariantStrict = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount);
		block.Variant = new RelaxedReadOnlyDictionary<string, string>(block.VariantStrict);
		block.EntityClass = packet.EntityClass;
		block.MaxStackSize = packet.MaxStackSize;
		block.StorageFlags = (EnumItemStorageFlags)packet.StorageFlags;
		block.RainPermeable = packet.RainPermeable > 0;
		block.Dimensions = ((packet.Width + packet.Height + packet.Length == 0) ? CollectibleObject.DefaultSize : new Size3f(CollectibleNet.DeserializeFloatVeryPrecise(packet.Width), CollectibleNet.DeserializeFloatVeryPrecise(packet.Height), CollectibleNet.DeserializeFloatVeryPrecise(packet.Length)));
		block.Durability = packet.Durability;
		block.BlockEntityBehaviors = JsonUtil.FromString<BlockEntityBehaviorType[]>(packet.EntityBehaviors);
		block.BlockId = packet.BlockId;
		block.DrawType = (EnumDrawType)packet.DrawType;
		block.RenderPass = (EnumChunkRenderPass)packet.RenderPass;
		block.VertexFlags = new VertexFlags(packet.VertexFlags);
		block.Frostable = packet.Frostable > 0;
		if (packet.LightHsv != null && packet.LightHsvCount > 2)
		{
			block.LightHsv = new byte[3]
			{
				(byte)packet.LightHsv[0],
				(byte)packet.LightHsv[1],
				(byte)packet.LightHsv[2]
			};
		}
		else
		{
			block.LightHsv = new byte[3];
		}
		if (packet.Sounds != null)
		{
			Packet_BlockSoundSet sounds = packet.Sounds;
			block.Sounds = new BlockSounds
			{
				Break = AssetLocation.CreateOrNull(sounds.Break),
				Hit = AssetLocation.CreateOrNull(sounds.Hit),
				Place = AssetLocation.CreateOrNull(sounds.Place),
				Walk = AssetLocation.CreateOrNull(sounds.Walk),
				Inside = AssetLocation.CreateOrNull(sounds.Inside),
				Ambient = AssetLocation.CreateOrNull(sounds.Ambient),
				AmbientBlockCount = CollectibleNet.DeserializeFloat(sounds.AmbientBlockCount),
				AmbientSoundType = (EnumSoundType)sounds.AmbientSoundType,
				AmbientMaxDistanceMerge = (float)sounds.AmbientMaxDistanceMerge / 100f
			};
			for (int num = 0; num < sounds.ByToolSoundCount; num++)
			{
				if (num == 0)
				{
					BlockSounds sounds2 = block.Sounds;
					if (sounds2.ByTool == null)
					{
						Dictionary<EnumTool, BlockSounds> dictionary = (sounds2.ByTool = new Dictionary<EnumTool, BlockSounds>());
					}
				}
				Packet_BlockSoundSet packet_BlockSoundSet = sounds.ByToolSound[num];
				block.Sounds.ByTool[(EnumTool)sounds.ByToolTool[num]] = new BlockSounds
				{
					Break = AssetLocation.CreateOrNull(packet_BlockSoundSet.Break),
					Hit = AssetLocation.CreateOrNull(packet_BlockSoundSet.Hit)
				};
			}
		}
		int textureCodesCount = packet.TextureCodesCount;
		block.Textures = new TextureDictionary(textureCodesCount);
		if (textureCodesCount > 0)
		{
			string[] textureCodes = packet.TextureCodes;
			for (int num2 = 0; num2 < textureCodes.Length && num2 < textureCodesCount; num2++)
			{
				block.Textures.Add(textureCodes[num2], CollectibleNet.FromPacket(packet.CompositeTextures[num2]));
			}
		}
		textureCodesCount = packet.InventoryTextureCodesCount;
		block.TexturesInventory = new TextureDictionary(textureCodesCount);
		if (textureCodesCount > 0)
		{
			string[] inventoryTextureCodes = packet.InventoryTextureCodes;
			for (int num3 = 0; num3 < inventoryTextureCodes.Length && num3 < textureCodesCount; num3++)
			{
				block.TexturesInventory.Add(inventoryTextureCodes[num3], CollectibleNet.FromPacket(packet.InventoryCompositeTextures[num3]));
			}
		}
		if (packet.Attributes != null && packet.Attributes.Length > 0)
		{
			block.Attributes = new JsonObject(JToken.Parse(packet.Attributes));
		}
		block.MatterState = (EnumMatterState)packet.MatterState;
		block.WalkSpeedMultiplier = CollectibleNet.DeserializeFloatVeryPrecise(packet.WalkSpeedFloat);
		block.DragMultiplier = CollectibleNet.DeserializeFloatVeryPrecise(packet.DragMultiplierFloat);
		block.Climbable = packet.Climbable > 0;
		block.SideOpaque = ((packet.SideOpaqueFlags == null) ? new SmallBoolArray(63) : new SmallBoolArray(packet.SideOpaqueFlags));
		block.SideAo = ((packet.SideAo == null) ? new SmallBoolArray(63) : new SmallBoolArray(packet.SideAo));
		block.EmitSideAo = (byte)packet.NeighbourSideAo;
		if (packet.SideSolidFlags != null)
		{
			block.SideSolid = new SmallBoolArray(packet.SideSolidFlags);
		}
		block.SeasonColorMap = packet.SeasonColorMap;
		block.ClimateColorMap = packet.ClimateColorMap;
		block.Fertility = packet.Fertility;
		block.Replaceable = packet.Replacable;
		block.LightAbsorption = (ushort)packet.LightAbsorption;
		block.Resistance = CollectibleNet.DeserializeFloat(packet.Resistance);
		block.BlockMaterial = (EnumBlockMaterial)packet.BlockMaterial;
		if (packet.Shape != null)
		{
			block.Shape = CollectibleNet.FromPacket(packet.Shape);
		}
		if (packet.ShapeInventory != null)
		{
			block.ShapeInventory = CollectibleNet.FromPacket(packet.ShapeInventory);
		}
		if (packet.Lod0shape != null)
		{
			block.Lod0Shape = CollectibleNet.FromPacket(packet.Lod0shape);
		}
		if (packet.Lod2shape != null)
		{
			block.Lod2Shape = CollectibleNet.FromPacket(packet.Lod2shape);
		}
		block.DoNotRenderAtLod2 = packet.DoNotRenderAtLod2 > 0;
		block.Ambientocclusion = packet.Ambientocclusion > 0;
		if (packet.SelectionBoxes != null)
		{
			Cuboidf[] array = (block.SelectionBoxes = new Cuboidf[packet.SelectionBoxesCount]);
			for (int num4 = 0; num4 < array.Length; num4++)
			{
				array[num4] = DeserializeCuboid(packet.SelectionBoxes[num4]);
			}
		}
		else
		{
			block.SelectionBoxes = null;
		}
		if (packet.CollisionBoxes != null)
		{
			Cuboidf[] array2 = (block.CollisionBoxes = new Cuboidf[packet.CollisionBoxesCount]);
			for (int num5 = 0; num5 < array2.Length; num5++)
			{
				array2[num5] = DeserializeCuboid(packet.CollisionBoxes[num5]);
			}
		}
		else
		{
			block.CollisionBoxes = null;
		}
		if (packet.ParticleCollisionBoxes != null)
		{
			Cuboidf[] array3 = (block.ParticleCollisionBoxes = new Cuboidf[packet.ParticleCollisionBoxesCount]);
			for (int num6 = 0; num6 < array3.Length; num6++)
			{
				array3[num6] = DeserializeCuboid(packet.ParticleCollisionBoxes[num6]);
			}
		}
		else
		{
			block.ParticleCollisionBoxes = null;
		}
		block.CreativeInventoryTabs = new string[packet.CreativeInventoryTabsCount];
		if (packet.CreativeInventoryTabs != null)
		{
			for (int num7 = 0; num7 < block.CreativeInventoryTabs.Length; num7++)
			{
				block.CreativeInventoryTabs[num7] = packet.CreativeInventoryTabs[num7];
			}
		}
		if (block.IsMissing)
		{
			block.GuiTransform = CollectibleNet.DefGuiTransform;
			block.FpHandTransform = CollectibleNet.DefFpHandTransform;
			block.TpHandTransform = CollectibleNet.DefTpHandTransform;
			block.TpOffHandTransform = CollectibleNet.DefTpOffHandTransform;
			block.GroundTransform = CollectibleNet.DefGroundTransform;
		}
		else
		{
			block.GuiTransform = ((packet.GuiTransform == null) ? ModelTransform.BlockDefaultGui() : CollectibleNet.FromTransformPacket(packet.GuiTransform).EnsureDefaultValues());
			block.FpHandTransform = ((packet.FpHandTransform == null) ? ModelTransform.BlockDefaultFp() : CollectibleNet.FromTransformPacket(packet.FpHandTransform).EnsureDefaultValues());
			block.TpHandTransform = ((packet.TpHandTransform == null) ? ModelTransform.BlockDefaultTp() : CollectibleNet.FromTransformPacket(packet.TpHandTransform).EnsureDefaultValues());
			block.TpOffHandTransform = ((packet.TpOffHandTransform == null) ? block.TpHandTransform.Clone() : CollectibleNet.FromTransformPacket(packet.TpOffHandTransform).EnsureDefaultValues());
			block.GroundTransform = ((packet.GroundTransform == null) ? ModelTransform.BlockDefaultGround() : CollectibleNet.FromTransformPacket(packet.GroundTransform).EnsureDefaultValues());
		}
		if (packet.ParticleProperties != null && packet.ParticleProperties.Length != 0)
		{
			block.ParticleProperties = new AdvancedParticleProperties[packet.ParticlePropertiesQuantity];
			using MemoryStream input = new MemoryStream(packet.ParticleProperties);
			BinaryReader reader = new BinaryReader(input);
			for (int num8 = 0; num8 < packet.ParticlePropertiesQuantity; num8++)
			{
				block.ParticleProperties[num8] = new AdvancedParticleProperties();
				block.ParticleProperties[num8].FromBytes(reader, world);
				if (block.ParticleProperties[num8].ColorByBlock)
				{
					block.ParticleProperties[num8].block = block;
				}
			}
		}
		block.RandomDrawOffset = packet.RandomDrawOffset;
		block.RandomizeAxes = (EnumRandomizeAxes)packet.RandomizeAxes;
		block.RandomizeRotations = packet.RandomizeRotations > 0;
		block.RandomSizeAdjust = CollectibleNet.DeserializeFloatVeryPrecise(packet.RandomSizeAdjust);
		block.LiquidLevel = packet.LiquidLevel;
		block.LiquidCode = packet.LiquidCode;
		block.FaceCullMode = (EnumFaceCullMode)packet.FaceCullMode;
		if (packet.CombustibleProps != null)
		{
			block.CombustibleProps = CollectibleNet.FromPacket(packet.CombustibleProps, world);
		}
		if (packet.NutritionProps != null)
		{
			block.NutritionProps = CollectibleNet.FromPacket(packet.NutritionProps, world);
		}
		if (packet.TransitionableProps != null)
		{
			block.TransitionableProps = CollectibleNet.FromPacket(packet.TransitionableProps, world);
		}
		if (packet.GrindingProps != null)
		{
			block.GrindingProps = CollectibleNet.FromPacket(packet.GrindingProps, world);
		}
		if (packet.CrushingProps != null)
		{
			block.CrushingProps = CollectibleNet.FromPacket(packet.CrushingProps, world);
		}
		if (packet.CreativeInventoryStacks != null)
		{
			using MemoryStream input2 = new MemoryStream(packet.CreativeInventoryStacks);
			BinaryReader binaryReader = new BinaryReader(input2);
			int num9 = binaryReader.ReadInt32();
			block.CreativeInventoryStacks = new CreativeTabAndStackList[num9];
			for (int num10 = 0; num10 < num9; num10++)
			{
				block.CreativeInventoryStacks[num10] = new CreativeTabAndStackList();
				block.CreativeInventoryStacks[num10].FromBytes(binaryReader, world.ClassRegistry);
			}
		}
		if (packet.Drops != null)
		{
			block.Drops = new BlockDropItemStack[packet.DropsCount];
			for (int num11 = 0; num11 < block.Drops.Length; num11++)
			{
				block.Drops[num11] = FromPacket(packet.Drops[num11], world);
			}
		}
		if (packet.CropProps != null)
		{
			block.CropProps = SerializerUtil.Deserialize<BlockCropProperties>(packet.CropProps);
			int cropPropBehaviorsCount = packet.CropPropBehaviorsCount;
			if (cropPropBehaviorsCount > 0)
			{
				block.CropProps.Behaviors = new CropBehavior[cropPropBehaviorsCount];
				for (int num12 = 0; num12 < cropPropBehaviorsCount; num12++)
				{
					block.CropProps.Behaviors[num12] = registry.createCropBehavior(block, packet.CropPropBehaviors[num12]);
				}
			}
		}
		block.MaterialDensity = packet.MaterialDensity;
		block.AttackPower = CollectibleNet.DeserializeFloatPrecise(packet.AttackPower);
		block.AttackRange = CollectibleNet.DeserializeFloatPrecise(packet.AttackRange);
		block.LiquidSelectable = packet.LiquidSelectable > 0;
		if (packet.HeldSounds != null)
		{
			block.HeldSounds = CollectibleNet.FromPacket(packet.HeldSounds);
		}
		if (packet.Miningmaterial != null)
		{
			block.MiningSpeed = new Dictionary<EnumBlockMaterial, float>();
			for (int num13 = 0; num13 < packet.MiningmaterialCount; num13++)
			{
				int key = packet.Miningmaterial[num13];
				float value = CollectibleNet.DeserializeFloat(packet.Miningmaterialspeed[num13]);
				block.MiningSpeed[(EnumBlockMaterial)key] = value;
			}
		}
		block.ToolTier = packet.MiningTier;
		block.RequiredMiningTier = packet.RequiredMiningTier;
		block.RenderAlphaTest = CollectibleNet.DeserializeFloatVeryPrecise(packet.RenderAlphaTest);
		block.HeldTpHitAnimation = packet.HeldTpHitAnimation;
		block.HeldRightTpIdleAnimation = packet.HeldRightTpIdleAnimation;
		block.HeldLeftTpIdleAnimation = packet.HeldLeftTpIdleAnimation;
		block.HeldLeftReadyAnimation = packet.HeldLeftReadyAnimation;
		block.HeldRightReadyAnimation = packet.HeldRightReadyAnimation;
		block.HeldTpUseAnimation = packet.HeldTpUseAnimation;
		if (packet.BehaviorsCount > 0)
		{
			List<BlockBehavior> list = new List<BlockBehavior>();
			List<CollectibleBehavior> list2 = new List<CollectibleBehavior>();
			for (int num14 = 0; num14 < packet.BehaviorsCount; num14++)
			{
				Packet_Behavior packet_Behavior = packet.Behaviors[num14];
				bool flag = registry.blockbehaviorToTypeMapping.ContainsKey(packet_Behavior.Code);
				bool flag2 = registry.collectibleBehaviorToTypeMapping.ContainsKey(packet_Behavior.Code);
				if (packet_Behavior.ClientSideOptional <= 0 || flag || flag2)
				{
					CollectibleBehavior collectibleBehavior = (flag ? registry.CreateBlockBehavior(block, packet_Behavior.Code) : registry.CreateCollectibleBehavior(block, packet_Behavior.Code));
					JsonObject properties = ((!(packet_Behavior.Attributes != "")) ? new JsonObject(JToken.Parse("{}")) : new JsonObject(JToken.Parse(packet_Behavior.Attributes)));
					collectibleBehavior.Initialize(properties);
					list2.Add(collectibleBehavior);
					if (collectibleBehavior is BlockBehavior item)
					{
						list.Add(item);
					}
				}
			}
			block.BlockBehaviors = list.ToArray();
			block.CollectibleBehaviors = list2.ToArray();
		}
		return block;
	}

	public static Packet_BlockType GetBlockTypePacket(Block block, IClassRegistryAPI registry)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return GetBlockTypePacket(block, registry, ms);
	}

	public static Packet_BlockType GetBlockTypePacket(Block block, IClassRegistryAPI registry, FastMemoryStream ms)
	{
		Packet_BlockType packet_BlockType = new Packet_BlockType();
		if (block == null)
		{
			return packet_BlockType;
		}
		packet_BlockType.Blockclass = registry.BlockClassToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == block.GetType()).Key;
		packet_BlockType.Code = block.Code.ToShortString();
		packet_BlockType.Tags = block.Tags.ToArray().Select((System.Func<ushort, int>)((ushort tag) => tag)).ToArray();
		packet_BlockType.IsMissing = (block.IsMissing ? 1 : 0);
		packet_BlockType.EntityClass = block.EntityClass;
		packet_BlockType.MaxStackSize = block.MaxStackSize;
		packet_BlockType.RainPermeable = (block.RainPermeable ? 1 : 0);
		packet_BlockType.SetVariant(CollectibleNet.ToPacket(block.VariantStrict));
		if (block.Dimensions != null)
		{
			packet_BlockType.Width = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Width);
			packet_BlockType.Height = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Height);
			packet_BlockType.Length = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Length);
		}
		if (block.BlockBehaviors != null)
		{
			Packet_Behavior[] array = new Packet_Behavior[block.CollectibleBehaviors.Length];
			int num = 0;
			CollectibleBehavior[] collectibleBehaviors = block.CollectibleBehaviors;
			foreach (CollectibleBehavior collectibleBehavior in collectibleBehaviors)
			{
				array[num++] = new Packet_Behavior
				{
					Code = ((collectibleBehavior is BlockBehavior) ? registry.GetBlockBehaviorClassName(collectibleBehavior.GetType()) : registry.GetCollectibleBehaviorClassName(collectibleBehavior.GetType())),
					Attributes = (collectibleBehavior.propertiesAtString ?? ""),
					ClientSideOptional = (collectibleBehavior.ClientSideOptional ? 1 : 0)
				};
			}
			packet_BlockType.SetBehaviors(array);
		}
		packet_BlockType.EntityBehaviors = JsonUtil.ToString(block.BlockEntityBehaviors);
		packet_BlockType.BlockId = block.BlockId;
		packet_BlockType.DrawType = (int)block.DrawType;
		packet_BlockType.RenderPass = (int)block.RenderPass;
		if (block.CreativeInventoryTabs != null)
		{
			packet_BlockType.SetCreativeInventoryTabs(block.CreativeInventoryTabs);
		}
		packet_BlockType.VertexFlags = ((block.VertexFlags != null) ? block.VertexFlags.All : 0);
		packet_BlockType.Frostable = (block.Frostable ? 1 : 0);
		packet_BlockType.SetLightHsv(new int[3]
		{
			block.LightHsv[0],
			block.LightHsv[1],
			block.LightHsv[2]
		});
		if (block.Sounds != null)
		{
			BlockSounds sounds = block.Sounds;
			packet_BlockType.Sounds = new Packet_BlockSoundSet
			{
				Break = sounds.Break.ToNonNullString(),
				Hit = sounds.Hit.ToNonNullString(),
				Walk = sounds.Walk.ToNonNullString(),
				Place = sounds.Place.ToNonNullString(),
				Inside = sounds.Inside.ToNonNullString(),
				Ambient = sounds.Ambient.ToNonNullString(),
				AmbientBlockCount = CollectibleNet.SerializeFloat(sounds.AmbientBlockCount),
				AmbientSoundType = (int)sounds.AmbientSoundType,
				AmbientMaxDistanceMerge = (int)(sounds.AmbientMaxDistanceMerge * 100f)
			};
			if (sounds.ByTool != null)
			{
				int[] array2 = new int[sounds.ByTool.Count];
				Packet_BlockSoundSet[] array3 = new Packet_BlockSoundSet[sounds.ByTool.Count];
				int num3 = 0;
				foreach (KeyValuePair<EnumTool, BlockSounds> item in sounds.ByTool)
				{
					array2[num3] = (int)item.Key;
					array3[num3] = new Packet_BlockSoundSet
					{
						Break = item.Value.Break.ToNonNullString(),
						Hit = item.Value.Hit.ToNonNullString()
					};
					num3++;
				}
				packet_BlockType.Sounds.SetByToolTool(array2);
				packet_BlockType.Sounds.SetByToolSound(array3);
			}
			else
			{
				packet_BlockType.Sounds.SetByToolTool(Array.Empty<int>());
				packet_BlockType.Sounds.SetByToolSound(Array.Empty<Packet_BlockSoundSet>());
			}
		}
		if (block.Textures != null)
		{
			packet_BlockType.SetTextureCodes(block.Textures.Keys.ToArray());
			packet_BlockType.SetCompositeTextures(CollectibleNet.ToPackets(block.Textures.Values.ToArray()));
		}
		if (block.TexturesInventory != null)
		{
			packet_BlockType.SetInventoryTextureCodes(block.TexturesInventory.Keys.ToArray());
			packet_BlockType.SetInventoryCompositeTextures(CollectibleNet.ToPackets(block.TexturesInventory.Values.ToArray()));
		}
		packet_BlockType.MatterState = (int)block.MatterState;
		packet_BlockType.WalkSpeedFloat = CollectibleNet.SerializeFloatVeryPrecise(block.WalkSpeedMultiplier);
		packet_BlockType.DragMultiplierFloat = CollectibleNet.SerializeFloatVeryPrecise(block.DragMultiplier);
		SmallBoolArray smallBoolArray = new SmallBoolArray(block.SideAo);
		if (!smallBoolArray.All)
		{
			packet_BlockType.SetSideAo(smallBoolArray.ToIntArray(6));
		}
		packet_BlockType.SetNeighbourSideAo(block.EmitSideAo);
		SmallBoolArray smallBoolArray2 = new SmallBoolArray(block.SideOpaque);
		if (!smallBoolArray2.All)
		{
			packet_BlockType.SetSideOpaqueFlags(smallBoolArray2.ToIntArray(6));
		}
		packet_BlockType.SetSideSolidFlags(block.SideSolid.ToIntArray(6));
		packet_BlockType.SeasonColorMap = block.SeasonColorMap;
		packet_BlockType.ClimateColorMap = block.ClimateColorMap;
		packet_BlockType.Fertility = block.Fertility;
		packet_BlockType.Replacable = block.Replaceable;
		packet_BlockType.LightAbsorption = block.LightAbsorption;
		packet_BlockType.Resistance = CollectibleNet.SerializeFloat(block.Resistance);
		packet_BlockType.BlockMaterial = (int)block.BlockMaterial;
		if (block.Shape != null)
		{
			packet_BlockType.Shape = CollectibleNet.ToPacket(block.Shape);
		}
		if (block.ShapeInventory != null)
		{
			packet_BlockType.ShapeInventory = CollectibleNet.ToPacket(block.ShapeInventory);
		}
		if (block.Lod0Shape != null)
		{
			packet_BlockType.Lod0shape = CollectibleNet.ToPacket(block.Lod0Shape);
		}
		if (block.Lod2Shape != null)
		{
			packet_BlockType.Lod2shape = CollectibleNet.ToPacket(block.Lod2Shape);
		}
		packet_BlockType.DoNotRenderAtLod2 = (block.DoNotRenderAtLod2 ? 1 : 0);
		packet_BlockType.Ambientocclusion = (block.Ambientocclusion ? 1 : 0);
		if (block.SelectionBoxes != null)
		{
			Packet_Cube[] array4 = new Packet_Cube[block.SelectionBoxes.Length];
			for (int num4 = 0; num4 < array4.Length; num4++)
			{
				array4[num4] = SerializeCuboid(block.SelectionBoxes[num4]);
			}
			packet_BlockType.SetSelectionBoxes(array4);
		}
		if (block.CollisionBoxes != null)
		{
			Packet_Cube[] array5 = new Packet_Cube[block.CollisionBoxes.Length];
			for (int num5 = 0; num5 < array5.Length; num5++)
			{
				array5[num5] = SerializeCuboid(block.CollisionBoxes[num5]);
			}
			packet_BlockType.SetCollisionBoxes(array5);
		}
		if (block.ParticleCollisionBoxes != null)
		{
			Packet_Cube[] array6 = new Packet_Cube[block.ParticleCollisionBoxes.Length];
			for (int num6 = 0; num6 < array6.Length; num6++)
			{
				array6[num6] = SerializeCuboid(block.ParticleCollisionBoxes[num6]);
			}
			packet_BlockType.SetParticleCollisionBoxes(array6);
		}
		if (!block.IsMissing)
		{
			if (block.GuiTransform != null)
			{
				packet_BlockType.GuiTransform = CollectibleNet.ToTransformPacket(block.GuiTransform, BlockList.guitf);
			}
			if (block.FpHandTransform != null)
			{
				packet_BlockType.FpHandTransform = CollectibleNet.ToTransformPacket(block.FpHandTransform, BlockList.fptf);
			}
			if (block.TpHandTransform != null)
			{
				packet_BlockType.TpHandTransform = CollectibleNet.ToTransformPacket(block.TpHandTransform, BlockList.tptf);
			}
			if (block.TpOffHandTransform != null)
			{
				packet_BlockType.TpOffHandTransform = CollectibleNet.ToTransformPacket(block.TpOffHandTransform, BlockList.tptf);
			}
			if (block.GroundTransform != null)
			{
				packet_BlockType.GroundTransform = CollectibleNet.ToTransformPacket(block.GroundTransform, BlockList.gndtf);
			}
		}
		if (block.ParticleProperties != null && block.ParticleProperties.Length != 0)
		{
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			for (int num7 = 0; num7 < block.ParticleProperties.Length; num7++)
			{
				block.ParticleProperties[num7].ToBytes(writer);
			}
			packet_BlockType.SetParticleProperties(ms.ToArray());
			packet_BlockType.ParticlePropertiesQuantity = block.ParticleProperties.Length;
		}
		packet_BlockType.RandomDrawOffset = block.RandomDrawOffset;
		packet_BlockType.RandomizeAxes = (int)block.RandomizeAxes;
		packet_BlockType.RandomizeRotations = (block.RandomizeRotations ? 1 : 0);
		packet_BlockType.RandomSizeAdjust = CollectibleNet.SerializeFloatVeryPrecise(block.RandomSizeAdjust);
		packet_BlockType.Climbable = (block.Climbable ? 1 : 0);
		packet_BlockType.LiquidLevel = block.LiquidLevel;
		packet_BlockType.LiquidCode = block.LiquidCode;
		packet_BlockType.FaceCullMode = (int)block.FaceCullMode;
		if (block.CombustibleProps != null)
		{
			packet_BlockType.CombustibleProps = CollectibleNet.ToPacket(block.CombustibleProps, ms);
		}
		if (block.NutritionProps != null)
		{
			packet_BlockType.NutritionProps = CollectibleNet.ToPacket(block.NutritionProps, ms);
		}
		if (block.TransitionableProps != null)
		{
			packet_BlockType.SetTransitionableProps(CollectibleNet.ToPacket(block.TransitionableProps, ms));
		}
		if (block.GrindingProps != null)
		{
			packet_BlockType.GrindingProps = CollectibleNet.ToPacket(block.GrindingProps, ms);
		}
		if (block.CrushingProps != null)
		{
			packet_BlockType.CrushingProps = CollectibleNet.ToPacket(block.CrushingProps, ms);
		}
		if (block.CreativeInventoryStacks != null)
		{
			ms.Reset();
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			binaryWriter.Write(block.CreativeInventoryStacks.Length);
			for (int num8 = 0; num8 < block.CreativeInventoryStacks.Length; num8++)
			{
				block.CreativeInventoryStacks[num8].ToBytes(binaryWriter, registry);
			}
			packet_BlockType.SetCreativeInventoryStacks(ms.ToArray());
		}
		if (block.Drops != null)
		{
			List<Packet_BlockDrop> list = new List<Packet_BlockDrop>();
			for (int num9 = 0; num9 < block.Drops.Length; num9++)
			{
				if (block.Drops[num9].ResolvedItemstack != null)
				{
					list.Add(ToPacket(block.Drops[num9], ms));
				}
			}
			packet_BlockType.SetDrops(list.ToArray());
		}
		if (block.CropProps != null)
		{
			packet_BlockType.CropProps = SerializerUtil.Serialize(block.CropProps);
		}
		packet_BlockType.MaterialDensity = block.MaterialDensity;
		packet_BlockType.AttackPower = CollectibleNet.SerializeFloatPrecise(block.AttackPower);
		packet_BlockType.AttackRange = CollectibleNet.SerializeFloatPrecise(block.AttackRange);
		packet_BlockType.Durability = block.Durability;
		if (block.Attributes != null)
		{
			packet_BlockType.Attributes = block.Attributes.ToString();
		}
		packet_BlockType.LiquidSelectable = (block.LiquidSelectable ? 1 : 0);
		packet_BlockType.RequiredMiningTier = block.RequiredMiningTier;
		packet_BlockType.MiningTier = block.ToolTier;
		if (block.HeldSounds != null)
		{
			packet_BlockType.HeldSounds = CollectibleNet.ToPacket(block.HeldSounds);
		}
		if (block.MiningSpeed != null)
		{
			Enum.GetValues(typeof(EnumBlockMaterial));
			List<int> list2 = new List<int>();
			List<int> list3 = new List<int>();
			foreach (KeyValuePair<EnumBlockMaterial, float> item2 in block.MiningSpeed)
			{
				list2.Add(CollectibleNet.SerializeFloat(item2.Value));
				list3.Add((int)item2.Key);
			}
			packet_BlockType.SetMiningmaterial(list3.ToArray());
			packet_BlockType.SetMiningmaterialspeed(list2.ToArray());
		}
		packet_BlockType.StorageFlags = (int)block.StorageFlags;
		packet_BlockType.RenderAlphaTest = CollectibleNet.SerializeFloatVeryPrecise(block.RenderAlphaTest);
		packet_BlockType.HeldTpHitAnimation = block.HeldTpHitAnimation;
		packet_BlockType.HeldRightTpIdleAnimation = block.HeldRightTpIdleAnimation;
		packet_BlockType.HeldLeftTpIdleAnimation = block.HeldLeftTpIdleAnimation;
		packet_BlockType.HeldTpUseAnimation = block.HeldTpUseAnimation;
		packet_BlockType.HeldLeftReadyAnimation = block.HeldLeftReadyAnimation;
		packet_BlockType.HeldRightReadyAnimation = block.HeldRightReadyAnimation;
		return packet_BlockType;
	}

	public static byte[] PackSetBlocksList(List<BlockPos> positions, IBlockAccessor blockAccessor)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(positions.Count);
		int[] array = new int[positions.Count];
		for (int i = 0; i < positions.Count; i++)
		{
			binaryWriter.Write(positions[i].X);
		}
		for (int j = 0; j < positions.Count; j++)
		{
			binaryWriter.Write(positions[j].InternalY);
		}
		for (int k = 0; k < positions.Count; k++)
		{
			binaryWriter.Write(positions[k].Z);
		}
		for (int l = 0; l < positions.Count; l++)
		{
			int blockId = blockAccessor.GetBlockId(positions[l]);
			int blockId2 = blockAccessor.GetBlock(positions[l], 2).BlockId;
			binaryWriter.Write((blockId != blockId2) ? blockId : 0);
			array[l] = blockId2;
		}
		for (int m = 0; m < array.Length; m++)
		{
			binaryWriter.Write(array[m]);
		}
		return Compression.Compress(memoryStream.ToArray());
	}

	public static byte[] PackSetDecorsList(WorldChunk chunk, long chunkIndex, IBlockAccessor blockAccessor)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(chunkIndex);
		lock (chunk.Decors)
		{
			binaryWriter.Write(chunk.Decors.Count);
			foreach (KeyValuePair<int, Block> decor in chunk.Decors)
			{
				Block value = decor.Value;
				binaryWriter.Write(decor.Key);
				binaryWriter.Write(value?.Id ?? 0);
			}
		}
		return Compression.Compress(memoryStream.ToArray());
	}

	public static Dictionary<int, Block> UnpackSetDecors(byte[] data, IWorldAccessor worldAccessor, out long chunkIndex)
	{
		using MemoryStream input = new MemoryStream(Compression.Decompress(data));
		BinaryReader binaryReader = new BinaryReader(input);
		chunkIndex = binaryReader.ReadInt64();
		int num = binaryReader.ReadInt32();
		Dictionary<int, Block> dictionary = new Dictionary<int, Block>(num);
		for (int i = 0; i < num; i++)
		{
			int key = binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			if (num2 != 0)
			{
				dictionary.Add(key, worldAccessor.GetBlock(num2));
			}
		}
		return dictionary;
	}

	public static KeyValuePair<BlockPos[], int[]> UnpackSetBlocks(byte[] setBlocks, out int[] liquidsLayer)
	{
		using MemoryStream input = new MemoryStream(Compression.Decompress(setBlocks));
		BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadInt32();
		BlockPos[] array = new BlockPos[num];
		int[] array2 = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new BlockPos(binaryReader.ReadInt32(), 0, 0);
		}
		for (int j = 0; j < num; j++)
		{
			int num2 = binaryReader.ReadInt32();
			array[j].Y = num2 % 32768;
			array[j].dimension = num2 / 32768;
		}
		for (int k = 0; k < num; k++)
		{
			array[k].Z = binaryReader.ReadInt32();
		}
		for (int l = 0; l < num; l++)
		{
			array2[l] = binaryReader.ReadInt32();
		}
		if (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position)
		{
			liquidsLayer = new int[num];
			for (int m = 0; m < num; m++)
			{
				liquidsLayer[m] = binaryReader.ReadInt32();
			}
		}
		else
		{
			liquidsLayer = null;
		}
		return new KeyValuePair<BlockPos[], int[]>(array, array2);
	}

	public static byte[] PackBlocksPositions(List<BlockPos> positions)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(positions.Count);
		for (int i = 0; i < positions.Count; i++)
		{
			binaryWriter.Write(positions[i].X);
		}
		for (int j = 0; j < positions.Count; j++)
		{
			binaryWriter.Write(positions[j].InternalY);
		}
		for (int k = 0; k < positions.Count; k++)
		{
			binaryWriter.Write(positions[k].Z);
		}
		return Compression.Compress(memoryStream.ToArray());
	}

	public static BlockPos[] UnpackBlockPositions(byte[] setBlocks)
	{
		using MemoryStream input = new MemoryStream(Compression.Decompress(setBlocks));
		BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadInt32();
		BlockPos[] array = new BlockPos[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new BlockPos(binaryReader.ReadInt32(), 0, 0);
		}
		for (int j = 0; j < num; j++)
		{
			int num2 = binaryReader.ReadInt32();
			array[j].Y = num2 % 32768;
			array[j].dimension = num2 / 32768;
		}
		for (int k = 0; k < num; k++)
		{
			array[k].Z = binaryReader.ReadInt32();
		}
		return array;
	}

	private static BlockDropItemStack FromPacket(Packet_BlockDrop packet, IWorldAccessor world)
	{
		BlockDropItemStack blockDropItemStack = new BlockDropItemStack();
		blockDropItemStack.Quantity = new NatFloat(CollectibleNet.DeserializeFloat(packet.QuantityAvg), CollectibleNet.DeserializeFloat(packet.QuantityVar), (EnumDistribution)packet.QuantityDist);
		if (packet.Tool < 99 && packet.Tool >= 0)
		{
			blockDropItemStack.Tool = (EnumTool)packet.Tool;
		}
		using MemoryStream input = new MemoryStream(packet.DroppedStack);
		BinaryReader reader = new BinaryReader(input);
		blockDropItemStack.ResolvedItemstack = new ItemStack(reader);
		return blockDropItemStack;
	}

	private static Packet_BlockDrop ToPacket(BlockDropItemStack drop, FastMemoryStream ms)
	{
		Packet_BlockDrop packet_BlockDrop = new Packet_BlockDrop
		{
			QuantityAvg = CollectibleNet.SerializeFloat(drop.Quantity.avg),
			QuantityDist = (int)drop.Quantity.dist,
			QuantityVar = CollectibleNet.SerializeFloat(drop.Quantity.var)
		};
		if (drop.Tool.HasValue)
		{
			packet_BlockDrop.Tool = (int)drop.Tool.Value;
		}
		else
		{
			packet_BlockDrop.Tool = 99;
		}
		ms.Reset();
		BinaryWriter stream = new BinaryWriter(ms);
		drop.ResolvedItemstack.ToBytes(stream);
		packet_BlockDrop.SetDroppedStack(ms.ToArray());
		return packet_BlockDrop;
	}

	private static Cuboidf DeserializeCuboid(Packet_Cube packet)
	{
		return new Cuboidf
		{
			X1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Minx),
			Y1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Miny),
			Z1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Minz),
			X2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxx),
			Y2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxy),
			Z2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxz)
		};
	}

	private static Packet_Cube SerializeCuboid(Cuboidf cube)
	{
		return new Packet_Cube
		{
			Minx = CollectibleNet.SerializeFloatVeryPrecise(cube.X1),
			Miny = CollectibleNet.SerializeFloatVeryPrecise(cube.Y1),
			Minz = CollectibleNet.SerializeFloatVeryPrecise(cube.Z1),
			Maxx = CollectibleNet.SerializeFloatVeryPrecise(cube.X2),
			Maxy = CollectibleNet.SerializeFloatVeryPrecise(cube.Y2),
			Maxz = CollectibleNet.SerializeFloatVeryPrecise(cube.Z2)
		};
	}
}
