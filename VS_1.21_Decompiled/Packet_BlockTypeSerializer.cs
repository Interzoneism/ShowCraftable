using System;

public class Packet_BlockTypeSerializer
{
	private const int field = 8;

	public static Packet_BlockType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockType packet_BlockType = new Packet_BlockType();
		DeserializeLengthDelimited(stream, packet_BlockType);
		return packet_BlockType;
	}

	public static Packet_BlockType DeserializeBuffer(byte[] buffer, int length, Packet_BlockType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockType Deserialize(CitoMemoryStream stream, Packet_BlockType instance)
	{
		instance.InitializeValues();
		int num;
		while (true)
		{
			num = stream.ReadByte();
			if ((num & 0x80) != 0)
			{
				num = ProtocolParser.ReadKeyAsInt(num, stream);
				if ((num & 0x4000) != 0)
				{
					break;
				}
			}
			switch (num)
			{
			case 0:
				return null;
			case 10:
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 18:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.InventoryTextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 34:
				instance.InventoryCompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.BlockId = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 466:
				instance.EntityClass = ProtocolParser.ReadString(stream);
				break;
			case 832:
				instance.TagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 58:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 674:
				instance.EntityBehaviors = ProtocolParser.ReadString(stream);
				break;
			case 64:
				instance.RenderPass = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.DrawType = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.MatterState = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.WalkSpeedFloat = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.IsSlipperyWalk = ProtocolParser.ReadBool(stream);
				break;
			case 106:
				if (instance.Sounds == null)
				{
					instance.Sounds = Packet_BlockSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockSoundSetSerializer.DeserializeLengthDelimited(stream, instance.Sounds);
				}
				break;
			case 666:
				if (instance.HeldSounds == null)
				{
					instance.HeldSounds = Packet_HeldSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance.HeldSounds);
				}
				break;
			case 112:
				instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 408:
				instance.VertexFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 120:
				instance.Climbable = ProtocolParser.ReadUInt32(stream);
				break;
			case 130:
				instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
				break;
			case 138:
				instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
				break;
			case 192:
				instance.SideOpaqueFlagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 184:
				instance.FaceCullMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 368:
				instance.SideSolidFlagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 202:
				instance.SeasonColorMap = ProtocolParser.ReadString(stream);
				break;
			case 706:
				instance.ClimateColorMap = ProtocolParser.ReadString(stream);
				break;
			case 208:
				instance.CullFaces = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.Replacable = ProtocolParser.ReadUInt32(stream);
				break;
			case 232:
				instance.LightAbsorption = ProtocolParser.ReadUInt32(stream);
				break;
			case 240:
				instance.HardnessLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 248:
				instance.Resistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 256:
				instance.BlockMaterial = ProtocolParser.ReadUInt32(stream);
				break;
			case 266:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
				break;
			case 274:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 282:
				if (instance.ShapeInventory == null)
				{
					instance.ShapeInventory = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.ShapeInventory);
				}
				break;
			case 304:
				instance.Ambientocclusion = ProtocolParser.ReadUInt32(stream);
				break;
			case 314:
				instance.CollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 322:
				instance.SelectionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 730:
				instance.ParticleCollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 330:
				instance.Blockclass = ProtocolParser.ReadString(stream);
				break;
			case 338:
				if (instance.GuiTransform == null)
				{
					instance.GuiTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GuiTransform);
				}
				break;
			case 346:
				if (instance.FpHandTransform == null)
				{
					instance.FpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.FpHandTransform);
				}
				break;
			case 354:
				if (instance.TpHandTransform == null)
				{
					instance.TpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpHandTransform);
				}
				break;
			case 794:
				if (instance.TpOffHandTransform == null)
				{
					instance.TpOffHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpOffHandTransform);
				}
				break;
			case 362:
				if (instance.GroundTransform == null)
				{
					instance.GroundTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GroundTransform);
				}
				break;
			case 376:
				instance.Fertility = ProtocolParser.ReadUInt32(stream);
				break;
			case 386:
				instance.ParticleProperties = ProtocolParser.ReadBytes(stream);
				break;
			case 392:
				instance.ParticlePropertiesQuantity = ProtocolParser.ReadUInt32(stream);
				break;
			case 400:
				instance.RandomDrawOffset = ProtocolParser.ReadUInt32(stream);
				break;
			case 552:
				instance.RandomizeAxes = ProtocolParser.ReadUInt32(stream);
				break;
			case 696:
				instance.RandomizeRotations = ProtocolParser.ReadUInt32(stream);
				break;
			case 418:
				instance.DropsAdd(Packet_BlockDropSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 424:
				instance.LiquidLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 434:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 442:
				if (instance.CombustibleProps == null)
				{
					instance.CombustibleProps = Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance.CombustibleProps);
				}
				break;
			case 456:
				instance.SideAoAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 632:
				instance.NeighbourSideAo = ProtocolParser.ReadUInt32(stream);
				break;
			case 618:
				if (instance.GrindingProps == null)
				{
					instance.GrindingProps = Packet_GrindingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.GrindingProps);
				}
				break;
			case 474:
				if (instance.NutritionProps == null)
				{
					instance.NutritionProps = Packet_NutritionPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance.NutritionProps);
				}
				break;
			case 682:
				instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 480:
				instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 490:
				instance.CropProps = ProtocolParser.ReadBytes(stream);
				break;
			case 722:
				instance.CropPropBehaviorsAdd(ProtocolParser.ReadString(stream));
				break;
			case 496:
				instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
				break;
			case 504:
				instance.AttackPower = ProtocolParser.ReadUInt32(stream);
				break;
			case 560:
				instance.AttackRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 512:
				instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
				break;
			case 520:
				instance.MiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 528:
				instance.RequiredMiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 536:
				instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 608:
				instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 544:
				instance.DragMultiplierFloat = ProtocolParser.ReadUInt32(stream);
				break;
			case 568:
				instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 576:
				instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
				break;
			case 586:
				instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
				break;
			case 594:
				instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 642:
				instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 602:
				instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
				break;
			case 624:
				instance.RainPermeable = ProtocolParser.ReadUInt32(stream);
				break;
			case 650:
				instance.LiquidCode = ProtocolParser.ReadString(stream);
				break;
			case 658:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 690:
				if (instance.Lod0shape == null)
				{
					instance.Lod0shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod0shape);
				}
				break;
			case 712:
				instance.Frostable = ProtocolParser.ReadUInt32(stream);
				break;
			case 738:
				if (instance.CrushingProps == null)
				{
					instance.CrushingProps = Packet_CrushingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.CrushingProps);
				}
				break;
			case 744:
				instance.RandomSizeAdjust = ProtocolParser.ReadUInt32(stream);
				break;
			case 754:
				if (instance.Lod2shape == null)
				{
					instance.Lod2shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod2shape);
				}
				break;
			case 760:
				instance.DoNotRenderAtLod2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 768:
				instance.Width = ProtocolParser.ReadUInt32(stream);
				break;
			case 776:
				instance.Height = ProtocolParser.ReadUInt32(stream);
				break;
			case 784:
				instance.Length = ProtocolParser.ReadUInt32(stream);
				break;
			case 800:
				instance.IsMissing = ProtocolParser.ReadUInt32(stream);
				break;
			case 808:
				instance.Durability = ProtocolParser.ReadUInt32(stream);
				break;
			case 818:
				instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
				break;
			case 826:
				instance.HeldRightReadyAnimation = ProtocolParser.ReadString(stream);
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(num));
				break;
			}
		}
		if (num >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_BlockType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockType instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockType result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockType instance)
	{
		if (instance.TextureCodes != null)
		{
			string[] textureCodes = instance.TextureCodes;
			int textureCodesCount = instance.TextureCodesCount;
			for (int i = 0; i < textureCodes.Length && i < textureCodesCount; i++)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, textureCodes[i]);
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] compositeTextures = instance.CompositeTextures;
			int compositeTexturesCount = instance.CompositeTexturesCount;
			for (int j = 0; j < compositeTextures.Length && j < compositeTexturesCount; j++)
			{
				stream.WriteByte(18);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, compositeTextures[j]);
			}
		}
		if (instance.InventoryTextureCodes != null)
		{
			string[] inventoryTextureCodes = instance.InventoryTextureCodes;
			int inventoryTextureCodesCount = instance.InventoryTextureCodesCount;
			for (int k = 0; k < inventoryTextureCodes.Length && k < inventoryTextureCodesCount; k++)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, inventoryTextureCodes[k]);
			}
		}
		if (instance.InventoryCompositeTextures != null)
		{
			Packet_CompositeTexture[] inventoryCompositeTextures = instance.InventoryCompositeTextures;
			int inventoryCompositeTexturesCount = instance.InventoryCompositeTexturesCount;
			for (int l = 0; l < inventoryCompositeTextures.Length && l < inventoryCompositeTexturesCount; l++)
			{
				stream.WriteByte(34);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, inventoryCompositeTextures[l]);
			}
		}
		if (instance.BlockId != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockId);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.EntityClass != null)
		{
			stream.WriteKey(58, 2);
			ProtocolParser.WriteString(stream, instance.EntityClass);
		}
		if (instance.Tags != null)
		{
			int[] tags = instance.Tags;
			int tagsCount = instance.TagsCount;
			for (int m = 0; m < tags.Length && m < tagsCount; m++)
			{
				stream.WriteKey(104, 0);
				ProtocolParser.WriteUInt32(stream, tags[m]);
			}
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] behaviors = instance.Behaviors;
			int behaviorsCount = instance.BehaviorsCount;
			for (int n = 0; n < behaviors.Length && n < behaviorsCount; n++)
			{
				stream.WriteByte(58);
				Packet_BehaviorSerializer.SerializeWithSize(stream, behaviors[n]);
			}
		}
		if (instance.EntityBehaviors != null)
		{
			stream.WriteKey(84, 2);
			ProtocolParser.WriteString(stream, instance.EntityBehaviors);
		}
		if (instance.RenderPass != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RenderPass);
		}
		if (instance.DrawType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.DrawType);
		}
		if (instance.MatterState != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.WalkSpeedFloat != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.WalkSpeedFloat);
		}
		if (instance.IsSlipperyWalk)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteBool(stream, instance.IsSlipperyWalk);
		}
		if (instance.Sounds != null)
		{
			stream.WriteByte(106);
			Packet_BlockSoundSetSerializer.SerializeWithSize(stream, instance.Sounds);
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(83, 2);
			Packet_HeldSoundSetSerializer.SerializeWithSize(stream, instance.HeldSounds);
		}
		if (instance.LightHsv != null)
		{
			int[] lightHsv = instance.LightHsv;
			int lightHsvCount = instance.LightHsvCount;
			for (int num = 0; num < lightHsv.Length && num < lightHsvCount; num++)
			{
				stream.WriteByte(112);
				ProtocolParser.WriteUInt32(stream, lightHsv[num]);
			}
		}
		if (instance.VertexFlags != 0)
		{
			stream.WriteKey(51, 0);
			ProtocolParser.WriteUInt32(stream, instance.VertexFlags);
		}
		if (instance.Climbable != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Climbable);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			string[] creativeInventoryTabs = instance.CreativeInventoryTabs;
			int creativeInventoryTabsCount = instance.CreativeInventoryTabsCount;
			for (int num2 = 0; num2 < creativeInventoryTabs.Length && num2 < creativeInventoryTabsCount; num2++)
			{
				stream.WriteKey(16, 2);
				ProtocolParser.WriteString(stream, creativeInventoryTabs[num2]);
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.SideOpaqueFlags != null)
		{
			int[] sideOpaqueFlags = instance.SideOpaqueFlags;
			int sideOpaqueFlagsCount = instance.SideOpaqueFlagsCount;
			for (int num3 = 0; num3 < sideOpaqueFlags.Length && num3 < sideOpaqueFlagsCount; num3++)
			{
				stream.WriteKey(24, 0);
				ProtocolParser.WriteUInt32(stream, sideOpaqueFlags[num3]);
			}
		}
		if (instance.FaceCullMode != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.FaceCullMode);
		}
		if (instance.SideSolidFlags != null)
		{
			int[] sideSolidFlags = instance.SideSolidFlags;
			int sideSolidFlagsCount = instance.SideSolidFlagsCount;
			for (int num4 = 0; num4 < sideSolidFlags.Length && num4 < sideSolidFlagsCount; num4++)
			{
				stream.WriteKey(46, 0);
				ProtocolParser.WriteUInt32(stream, sideSolidFlags[num4]);
			}
		}
		if (instance.SeasonColorMap != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteString(stream, instance.SeasonColorMap);
		}
		if (instance.ClimateColorMap != null)
		{
			stream.WriteKey(88, 2);
			ProtocolParser.WriteString(stream, instance.ClimateColorMap);
		}
		if (instance.CullFaces != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.CullFaces);
		}
		if (instance.Replacable != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.Replacable);
		}
		if (instance.LightAbsorption != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.LightAbsorption);
		}
		if (instance.HardnessLevel != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.HardnessLevel);
		}
		if (instance.Resistance != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.Resistance);
		}
		if (instance.BlockMaterial != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.BlockMaterial);
		}
		if (instance.Moddata != null)
		{
			stream.WriteKey(33, 2);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(34, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.ShapeInventory != null)
		{
			stream.WriteKey(35, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.ShapeInventory);
		}
		if (instance.Ambientocclusion != 0)
		{
			stream.WriteKey(38, 0);
			ProtocolParser.WriteUInt32(stream, instance.Ambientocclusion);
		}
		if (instance.CollisionBoxes != null)
		{
			Packet_Cube[] collisionBoxes = instance.CollisionBoxes;
			int collisionBoxesCount = instance.CollisionBoxesCount;
			for (int num5 = 0; num5 < collisionBoxes.Length && num5 < collisionBoxesCount; num5++)
			{
				stream.WriteKey(39, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, collisionBoxes[num5]);
			}
		}
		if (instance.SelectionBoxes != null)
		{
			Packet_Cube[] selectionBoxes = instance.SelectionBoxes;
			int selectionBoxesCount = instance.SelectionBoxesCount;
			for (int num6 = 0; num6 < selectionBoxes.Length && num6 < selectionBoxesCount; num6++)
			{
				stream.WriteKey(40, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, selectionBoxes[num6]);
			}
		}
		if (instance.ParticleCollisionBoxes != null)
		{
			Packet_Cube[] particleCollisionBoxes = instance.ParticleCollisionBoxes;
			int particleCollisionBoxesCount = instance.ParticleCollisionBoxesCount;
			for (int num7 = 0; num7 < particleCollisionBoxes.Length && num7 < particleCollisionBoxesCount; num7++)
			{
				stream.WriteKey(91, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, particleCollisionBoxes[num7]);
			}
		}
		if (instance.Blockclass != null)
		{
			stream.WriteKey(41, 2);
			ProtocolParser.WriteString(stream, instance.Blockclass);
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteKey(42, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GuiTransform);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteKey(43, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.FpHandTransform);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteKey(44, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpHandTransform);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(99, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpOffHandTransform);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(45, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GroundTransform);
		}
		if (instance.Fertility != 0)
		{
			stream.WriteKey(47, 0);
			ProtocolParser.WriteUInt32(stream, instance.Fertility);
		}
		if (instance.ParticleProperties != null)
		{
			stream.WriteKey(48, 2);
			ProtocolParser.WriteBytes(stream, instance.ParticleProperties);
		}
		if (instance.ParticlePropertiesQuantity != 0)
		{
			stream.WriteKey(49, 0);
			ProtocolParser.WriteUInt32(stream, instance.ParticlePropertiesQuantity);
		}
		if (instance.RandomDrawOffset != 0)
		{
			stream.WriteKey(50, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomDrawOffset);
		}
		if (instance.RandomizeAxes != 0)
		{
			stream.WriteKey(69, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeAxes);
		}
		if (instance.RandomizeRotations != 0)
		{
			stream.WriteKey(87, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeRotations);
		}
		if (instance.Drops != null)
		{
			Packet_BlockDrop[] drops = instance.Drops;
			int dropsCount = instance.DropsCount;
			for (int num8 = 0; num8 < drops.Length && num8 < dropsCount; num8++)
			{
				stream.WriteKey(52, 2);
				Packet_BlockDropSerializer.SerializeWithSize(stream, drops[num8]);
			}
		}
		if (instance.LiquidLevel != 0)
		{
			stream.WriteKey(53, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidLevel);
		}
		if (instance.Attributes != null)
		{
			stream.WriteKey(54, 2);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteKey(55, 2);
			Packet_CombustiblePropertiesSerializer.SerializeWithSize(stream, instance.CombustibleProps);
		}
		if (instance.SideAo != null)
		{
			int[] sideAo = instance.SideAo;
			int sideAoCount = instance.SideAoCount;
			for (int num9 = 0; num9 < sideAo.Length && num9 < sideAoCount; num9++)
			{
				stream.WriteKey(57, 0);
				ProtocolParser.WriteUInt32(stream, sideAo[num9]);
			}
		}
		if (instance.NeighbourSideAo != 0)
		{
			stream.WriteKey(79, 0);
			ProtocolParser.WriteUInt32(stream, instance.NeighbourSideAo);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(77, 2);
			Packet_GrindingPropertiesSerializer.SerializeWithSize(stream, instance.GrindingProps);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteKey(59, 2);
			Packet_NutritionPropertiesSerializer.SerializeWithSize(stream, instance.NutritionProps);
		}
		if (instance.TransitionableProps != null)
		{
			Packet_TransitionableProperties[] transitionableProps = instance.TransitionableProps;
			int transitionablePropsCount = instance.TransitionablePropsCount;
			for (int num10 = 0; num10 < transitionableProps.Length && num10 < transitionablePropsCount; num10++)
			{
				stream.WriteKey(85, 2);
				Packet_TransitionablePropertiesSerializer.SerializeWithSize(stream, transitionableProps[num10]);
			}
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteKey(60, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.CropProps != null)
		{
			stream.WriteKey(61, 2);
			ProtocolParser.WriteBytes(stream, instance.CropProps);
		}
		if (instance.CropPropBehaviors != null)
		{
			string[] cropPropBehaviors = instance.CropPropBehaviors;
			int cropPropBehaviorsCount = instance.CropPropBehaviorsCount;
			for (int num11 = 0; num11 < cropPropBehaviors.Length && num11 < cropPropBehaviorsCount; num11++)
			{
				stream.WriteKey(90, 2);
				ProtocolParser.WriteString(stream, cropPropBehaviors[num11]);
			}
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(62, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(63, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(70, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(64, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(65, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.RequiredMiningTier != 0)
		{
			stream.WriteKey(66, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequiredMiningTier);
		}
		if (instance.Miningmaterial != null)
		{
			int[] miningmaterial = instance.Miningmaterial;
			int miningmaterialCount = instance.MiningmaterialCount;
			for (int num12 = 0; num12 < miningmaterial.Length && num12 < miningmaterialCount; num12++)
			{
				stream.WriteKey(67, 0);
				ProtocolParser.WriteUInt32(stream, miningmaterial[num12]);
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			int[] miningmaterialspeed = instance.Miningmaterialspeed;
			int miningmaterialspeedCount = instance.MiningmaterialspeedCount;
			for (int num13 = 0; num13 < miningmaterialspeed.Length && num13 < miningmaterialspeedCount; num13++)
			{
				stream.WriteKey(76, 0);
				ProtocolParser.WriteUInt32(stream, miningmaterialspeed[num13]);
			}
		}
		if (instance.DragMultiplierFloat != 0)
		{
			stream.WriteKey(68, 0);
			ProtocolParser.WriteUInt32(stream, instance.DragMultiplierFloat);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(71, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(72, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(73, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpHitAnimation);
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(74, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightTpIdleAnimation);
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(80, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftTpIdleAnimation);
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(75, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpUseAnimation);
		}
		if (instance.RainPermeable != 0)
		{
			stream.WriteKey(78, 0);
			ProtocolParser.WriteUInt32(stream, instance.RainPermeable);
		}
		if (instance.LiquidCode != null)
		{
			stream.WriteKey(81, 2);
			ProtocolParser.WriteString(stream, instance.LiquidCode);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] variant = instance.Variant;
			int variantCount = instance.VariantCount;
			for (int num14 = 0; num14 < variant.Length && num14 < variantCount; num14++)
			{
				stream.WriteKey(82, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, variant[num14]);
			}
		}
		if (instance.Lod0shape != null)
		{
			stream.WriteKey(86, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Lod0shape);
		}
		if (instance.Frostable != 0)
		{
			stream.WriteKey(89, 0);
			ProtocolParser.WriteUInt32(stream, instance.Frostable);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(92, 2);
			Packet_CrushingPropertiesSerializer.SerializeWithSize(stream, instance.CrushingProps);
		}
		if (instance.RandomSizeAdjust != 0)
		{
			stream.WriteKey(93, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomSizeAdjust);
		}
		if (instance.Lod2shape != null)
		{
			stream.WriteKey(94, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Lod2shape);
		}
		if (instance.DoNotRenderAtLod2 != 0)
		{
			stream.WriteKey(95, 0);
			ProtocolParser.WriteUInt32(stream, instance.DoNotRenderAtLod2);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(96, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(97, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(98, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(100, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.Durability != 0)
		{
			stream.WriteKey(101, 0);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(102, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftReadyAnimation);
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(103, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightReadyAnimation);
		}
	}

	public static int GetSize(Packet_BlockType instance)
	{
		int num = 0;
		if (instance.TextureCodes != null)
		{
			for (int i = 0; i < instance.TextureCodesCount; i++)
			{
				string s = instance.TextureCodes[i];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int j = 0; j < instance.CompositeTexturesCount; j++)
			{
				int size = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[j]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.InventoryTextureCodes != null)
		{
			for (int k = 0; k < instance.InventoryTextureCodesCount; k++)
			{
				string s2 = instance.InventoryTextureCodes[k];
				num += ProtocolParser.GetSize(s2) + 1;
			}
		}
		if (instance.InventoryCompositeTextures != null)
		{
			for (int l = 0; l < instance.InventoryCompositeTexturesCount; l++)
			{
				int size2 = Packet_CompositeTextureSerializer.GetSize(instance.InventoryCompositeTextures[l]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.BlockId != 0)
		{
			num += ProtocolParser.GetSize(instance.BlockId) + 1;
		}
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.EntityClass != null)
		{
			num += ProtocolParser.GetSize(instance.EntityClass) + 2;
		}
		if (instance.Tags != null)
		{
			for (int m = 0; m < instance.TagsCount; m++)
			{
				int v = instance.Tags[m];
				num += ProtocolParser.GetSize(v) + 2;
			}
		}
		if (instance.Behaviors != null)
		{
			for (int n = 0; n < instance.BehaviorsCount; n++)
			{
				int size3 = Packet_BehaviorSerializer.GetSize(instance.Behaviors[n]);
				num += size3 + ProtocolParser.GetSize(size3) + 1;
			}
		}
		if (instance.EntityBehaviors != null)
		{
			num += ProtocolParser.GetSize(instance.EntityBehaviors) + 2;
		}
		if (instance.RenderPass != 0)
		{
			num += ProtocolParser.GetSize(instance.RenderPass) + 1;
		}
		if (instance.DrawType != 0)
		{
			num += ProtocolParser.GetSize(instance.DrawType) + 1;
		}
		if (instance.MatterState != 0)
		{
			num += ProtocolParser.GetSize(instance.MatterState) + 1;
		}
		if (instance.WalkSpeedFloat != 0)
		{
			num += ProtocolParser.GetSize(instance.WalkSpeedFloat) + 1;
		}
		if (instance.IsSlipperyWalk)
		{
			num += 2;
		}
		if (instance.Sounds != null)
		{
			int size4 = Packet_BlockSoundSetSerializer.GetSize(instance.Sounds);
			num += size4 + ProtocolParser.GetSize(size4) + 1;
		}
		if (instance.HeldSounds != null)
		{
			int size5 = Packet_HeldSoundSetSerializer.GetSize(instance.HeldSounds);
			num += size5 + ProtocolParser.GetSize(size5) + 2;
		}
		if (instance.LightHsv != null)
		{
			for (int num2 = 0; num2 < instance.LightHsvCount; num2++)
			{
				int v2 = instance.LightHsv[num2];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.VertexFlags != 0)
		{
			num += ProtocolParser.GetSize(instance.VertexFlags) + 2;
		}
		if (instance.Climbable != 0)
		{
			num += ProtocolParser.GetSize(instance.Climbable) + 1;
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int num3 = 0; num3 < instance.CreativeInventoryTabsCount; num3++)
			{
				string s3 = instance.CreativeInventoryTabs[num3];
				num += ProtocolParser.GetSize(s3) + 2;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			num += ProtocolParser.GetSize(instance.CreativeInventoryStacks) + 2;
		}
		if (instance.SideOpaqueFlags != null)
		{
			for (int num4 = 0; num4 < instance.SideOpaqueFlagsCount; num4++)
			{
				int v3 = instance.SideOpaqueFlags[num4];
				num += ProtocolParser.GetSize(v3) + 2;
			}
		}
		if (instance.FaceCullMode != 0)
		{
			num += ProtocolParser.GetSize(instance.FaceCullMode) + 2;
		}
		if (instance.SideSolidFlags != null)
		{
			for (int num5 = 0; num5 < instance.SideSolidFlagsCount; num5++)
			{
				int v4 = instance.SideSolidFlags[num5];
				num += ProtocolParser.GetSize(v4) + 2;
			}
		}
		if (instance.SeasonColorMap != null)
		{
			num += ProtocolParser.GetSize(instance.SeasonColorMap) + 2;
		}
		if (instance.ClimateColorMap != null)
		{
			num += ProtocolParser.GetSize(instance.ClimateColorMap) + 2;
		}
		if (instance.CullFaces != 0)
		{
			num += ProtocolParser.GetSize(instance.CullFaces) + 2;
		}
		if (instance.Replacable != 0)
		{
			num += ProtocolParser.GetSize(instance.Replacable) + 2;
		}
		if (instance.LightAbsorption != 0)
		{
			num += ProtocolParser.GetSize(instance.LightAbsorption) + 2;
		}
		if (instance.HardnessLevel != 0)
		{
			num += ProtocolParser.GetSize(instance.HardnessLevel) + 2;
		}
		if (instance.Resistance != 0)
		{
			num += ProtocolParser.GetSize(instance.Resistance) + 2;
		}
		if (instance.BlockMaterial != 0)
		{
			num += ProtocolParser.GetSize(instance.BlockMaterial) + 2;
		}
		if (instance.Moddata != null)
		{
			num += ProtocolParser.GetSize(instance.Moddata) + 2;
		}
		if (instance.Shape != null)
		{
			int size6 = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			num += size6 + ProtocolParser.GetSize(size6) + 2;
		}
		if (instance.ShapeInventory != null)
		{
			int size7 = Packet_CompositeShapeSerializer.GetSize(instance.ShapeInventory);
			num += size7 + ProtocolParser.GetSize(size7) + 2;
		}
		if (instance.Ambientocclusion != 0)
		{
			num += ProtocolParser.GetSize(instance.Ambientocclusion) + 2;
		}
		if (instance.CollisionBoxes != null)
		{
			for (int num6 = 0; num6 < instance.CollisionBoxesCount; num6++)
			{
				int size8 = Packet_CubeSerializer.GetSize(instance.CollisionBoxes[num6]);
				num += size8 + ProtocolParser.GetSize(size8) + 2;
			}
		}
		if (instance.SelectionBoxes != null)
		{
			for (int num7 = 0; num7 < instance.SelectionBoxesCount; num7++)
			{
				int size9 = Packet_CubeSerializer.GetSize(instance.SelectionBoxes[num7]);
				num += size9 + ProtocolParser.GetSize(size9) + 2;
			}
		}
		if (instance.ParticleCollisionBoxes != null)
		{
			for (int num8 = 0; num8 < instance.ParticleCollisionBoxesCount; num8++)
			{
				int size10 = Packet_CubeSerializer.GetSize(instance.ParticleCollisionBoxes[num8]);
				num += size10 + ProtocolParser.GetSize(size10) + 2;
			}
		}
		if (instance.Blockclass != null)
		{
			num += ProtocolParser.GetSize(instance.Blockclass) + 2;
		}
		if (instance.GuiTransform != null)
		{
			int size11 = Packet_ModelTransformSerializer.GetSize(instance.GuiTransform);
			num += size11 + ProtocolParser.GetSize(size11) + 2;
		}
		if (instance.FpHandTransform != null)
		{
			int size12 = Packet_ModelTransformSerializer.GetSize(instance.FpHandTransform);
			num += size12 + ProtocolParser.GetSize(size12) + 2;
		}
		if (instance.TpHandTransform != null)
		{
			int size13 = Packet_ModelTransformSerializer.GetSize(instance.TpHandTransform);
			num += size13 + ProtocolParser.GetSize(size13) + 2;
		}
		if (instance.TpOffHandTransform != null)
		{
			int size14 = Packet_ModelTransformSerializer.GetSize(instance.TpOffHandTransform);
			num += size14 + ProtocolParser.GetSize(size14) + 2;
		}
		if (instance.GroundTransform != null)
		{
			int size15 = Packet_ModelTransformSerializer.GetSize(instance.GroundTransform);
			num += size15 + ProtocolParser.GetSize(size15) + 2;
		}
		if (instance.Fertility != 0)
		{
			num += ProtocolParser.GetSize(instance.Fertility) + 2;
		}
		if (instance.ParticleProperties != null)
		{
			num += ProtocolParser.GetSize(instance.ParticleProperties) + 2;
		}
		if (instance.ParticlePropertiesQuantity != 0)
		{
			num += ProtocolParser.GetSize(instance.ParticlePropertiesQuantity) + 2;
		}
		if (instance.RandomDrawOffset != 0)
		{
			num += ProtocolParser.GetSize(instance.RandomDrawOffset) + 2;
		}
		if (instance.RandomizeAxes != 0)
		{
			num += ProtocolParser.GetSize(instance.RandomizeAxes) + 2;
		}
		if (instance.RandomizeRotations != 0)
		{
			num += ProtocolParser.GetSize(instance.RandomizeRotations) + 2;
		}
		if (instance.Drops != null)
		{
			for (int num9 = 0; num9 < instance.DropsCount; num9++)
			{
				int size16 = Packet_BlockDropSerializer.GetSize(instance.Drops[num9]);
				num += size16 + ProtocolParser.GetSize(size16) + 2;
			}
		}
		if (instance.LiquidLevel != 0)
		{
			num += ProtocolParser.GetSize(instance.LiquidLevel) + 2;
		}
		if (instance.Attributes != null)
		{
			num += ProtocolParser.GetSize(instance.Attributes) + 2;
		}
		if (instance.CombustibleProps != null)
		{
			int size17 = Packet_CombustiblePropertiesSerializer.GetSize(instance.CombustibleProps);
			num += size17 + ProtocolParser.GetSize(size17) + 2;
		}
		if (instance.SideAo != null)
		{
			for (int num10 = 0; num10 < instance.SideAoCount; num10++)
			{
				int v5 = instance.SideAo[num10];
				num += ProtocolParser.GetSize(v5) + 2;
			}
		}
		if (instance.NeighbourSideAo != 0)
		{
			num += ProtocolParser.GetSize(instance.NeighbourSideAo) + 2;
		}
		if (instance.GrindingProps != null)
		{
			int size18 = Packet_GrindingPropertiesSerializer.GetSize(instance.GrindingProps);
			num += size18 + ProtocolParser.GetSize(size18) + 2;
		}
		if (instance.NutritionProps != null)
		{
			int size19 = Packet_NutritionPropertiesSerializer.GetSize(instance.NutritionProps);
			num += size19 + ProtocolParser.GetSize(size19) + 2;
		}
		if (instance.TransitionableProps != null)
		{
			for (int num11 = 0; num11 < instance.TransitionablePropsCount; num11++)
			{
				int size20 = Packet_TransitionablePropertiesSerializer.GetSize(instance.TransitionableProps[num11]);
				num += size20 + ProtocolParser.GetSize(size20) + 2;
			}
		}
		if (instance.MaxStackSize != 0)
		{
			num += ProtocolParser.GetSize(instance.MaxStackSize) + 2;
		}
		if (instance.CropProps != null)
		{
			num += ProtocolParser.GetSize(instance.CropProps) + 2;
		}
		if (instance.CropPropBehaviors != null)
		{
			for (int num12 = 0; num12 < instance.CropPropBehaviorsCount; num12++)
			{
				string s4 = instance.CropPropBehaviors[num12];
				num += ProtocolParser.GetSize(s4) + 2;
			}
		}
		if (instance.MaterialDensity != 0)
		{
			num += ProtocolParser.GetSize(instance.MaterialDensity) + 2;
		}
		if (instance.AttackPower != 0)
		{
			num += ProtocolParser.GetSize(instance.AttackPower) + 2;
		}
		if (instance.AttackRange != 0)
		{
			num += ProtocolParser.GetSize(instance.AttackRange) + 2;
		}
		if (instance.LiquidSelectable != 0)
		{
			num += ProtocolParser.GetSize(instance.LiquidSelectable) + 2;
		}
		if (instance.MiningTier != 0)
		{
			num += ProtocolParser.GetSize(instance.MiningTier) + 2;
		}
		if (instance.RequiredMiningTier != 0)
		{
			num += ProtocolParser.GetSize(instance.RequiredMiningTier) + 2;
		}
		if (instance.Miningmaterial != null)
		{
			for (int num13 = 0; num13 < instance.MiningmaterialCount; num13++)
			{
				int v6 = instance.Miningmaterial[num13];
				num += ProtocolParser.GetSize(v6) + 2;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int num14 = 0; num14 < instance.MiningmaterialspeedCount; num14++)
			{
				int v7 = instance.Miningmaterialspeed[num14];
				num += ProtocolParser.GetSize(v7) + 2;
			}
		}
		if (instance.DragMultiplierFloat != 0)
		{
			num += ProtocolParser.GetSize(instance.DragMultiplierFloat) + 2;
		}
		if (instance.StorageFlags != 0)
		{
			num += ProtocolParser.GetSize(instance.StorageFlags) + 2;
		}
		if (instance.RenderAlphaTest != 0)
		{
			num += ProtocolParser.GetSize(instance.RenderAlphaTest) + 2;
		}
		if (instance.HeldTpHitAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldTpHitAnimation) + 2;
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldRightTpIdleAnimation) + 2;
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldLeftTpIdleAnimation) + 2;
		}
		if (instance.HeldTpUseAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldTpUseAnimation) + 2;
		}
		if (instance.RainPermeable != 0)
		{
			num += ProtocolParser.GetSize(instance.RainPermeable) + 2;
		}
		if (instance.LiquidCode != null)
		{
			num += ProtocolParser.GetSize(instance.LiquidCode) + 2;
		}
		if (instance.Variant != null)
		{
			for (int num15 = 0; num15 < instance.VariantCount; num15++)
			{
				int size21 = Packet_VariantPartSerializer.GetSize(instance.Variant[num15]);
				num += size21 + ProtocolParser.GetSize(size21) + 2;
			}
		}
		if (instance.Lod0shape != null)
		{
			int size22 = Packet_CompositeShapeSerializer.GetSize(instance.Lod0shape);
			num += size22 + ProtocolParser.GetSize(size22) + 2;
		}
		if (instance.Frostable != 0)
		{
			num += ProtocolParser.GetSize(instance.Frostable) + 2;
		}
		if (instance.CrushingProps != null)
		{
			int size23 = Packet_CrushingPropertiesSerializer.GetSize(instance.CrushingProps);
			num += size23 + ProtocolParser.GetSize(size23) + 2;
		}
		if (instance.RandomSizeAdjust != 0)
		{
			num += ProtocolParser.GetSize(instance.RandomSizeAdjust) + 2;
		}
		if (instance.Lod2shape != null)
		{
			int size24 = Packet_CompositeShapeSerializer.GetSize(instance.Lod2shape);
			num += size24 + ProtocolParser.GetSize(size24) + 2;
		}
		if (instance.DoNotRenderAtLod2 != 0)
		{
			num += ProtocolParser.GetSize(instance.DoNotRenderAtLod2) + 2;
		}
		if (instance.Width != 0)
		{
			num += ProtocolParser.GetSize(instance.Width) + 2;
		}
		if (instance.Height != 0)
		{
			num += ProtocolParser.GetSize(instance.Height) + 2;
		}
		if (instance.Length != 0)
		{
			num += ProtocolParser.GetSize(instance.Length) + 2;
		}
		if (instance.IsMissing != 0)
		{
			num += ProtocolParser.GetSize(instance.IsMissing) + 2;
		}
		if (instance.Durability != 0)
		{
			num += ProtocolParser.GetSize(instance.Durability) + 2;
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldLeftReadyAnimation) + 2;
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			num += ProtocolParser.GetSize(instance.HeldRightReadyAnimation) + 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockType instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int num = stream.Position();
		Serialize(stream, instance);
		int num2 = stream.Position() - num;
		if (num2 != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size + " != " + num2);
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockType instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
