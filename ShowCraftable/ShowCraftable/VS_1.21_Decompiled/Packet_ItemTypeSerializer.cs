using System;

public class Packet_ItemTypeSerializer
{
	private const int field = 8;

	public static Packet_ItemType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ItemType packet_ItemType = new Packet_ItemType();
		DeserializeLengthDelimited(stream, packet_ItemType);
		return packet_ItemType;
	}

	public static Packet_ItemType DeserializeBuffer(byte[] buffer, int length, Packet_ItemType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ItemType Deserialize(CitoMemoryStream stream, Packet_ItemType instance)
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
			case 8:
				instance.ItemId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 384:
				instance.TagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 314:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 34:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.Durability = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 248:
				instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 56:
				instance.DamagedbyAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 66:
				instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
				break;
			case 74:
				instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
				break;
			case 82:
				if (instance.GuiTransform == null)
				{
					instance.GuiTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GuiTransform);
				}
				break;
			case 90:
				if (instance.FpHandTransform == null)
				{
					instance.FpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.FpHandTransform);
				}
				break;
			case 98:
				if (instance.TpHandTransform == null)
				{
					instance.TpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpHandTransform);
				}
				break;
			case 346:
				if (instance.TpOffHandTransform == null)
				{
					instance.TpOffHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpOffHandTransform);
				}
				break;
			case 178:
				if (instance.GroundTransform == null)
				{
					instance.GroundTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GroundTransform);
				}
				break;
			case 106:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 114:
				if (instance.CombustibleProps == null)
				{
					instance.CombustibleProps = Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance.CombustibleProps);
				}
				break;
			case 122:
				if (instance.NutritionProps == null)
				{
					instance.NutritionProps = Packet_NutritionPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance.NutritionProps);
				}
				break;
			case 258:
				if (instance.GrindingProps == null)
				{
					instance.GrindingProps = Packet_GrindingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.GrindingProps);
				}
				break;
			case 306:
				if (instance.CrushingProps == null)
				{
					instance.CrushingProps = Packet_CrushingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.CrushingProps);
				}
				break;
			case 290:
				instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 130:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 138:
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 146:
				instance.ItemClass = ProtocolParser.ReadString(stream);
				break;
			case 152:
				instance.Tool = ProtocolParser.ReadUInt32(stream);
				break;
			case 160:
				instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.AttackPower = ProtocolParser.ReadUInt32(stream);
				break;
			case 200:
				instance.AttackRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
				break;
			case 192:
				instance.MiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 208:
				instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
				break;
			case 226:
				instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
				break;
			case 234:
				instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 274:
				instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 242:
				instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
				break;
			case 264:
				instance.MatterState = ProtocolParser.ReadUInt32(stream);
				break;
			case 282:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 298:
				if (instance.HeldSounds == null)
				{
					instance.HeldSounds = Packet_HeldSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance.HeldSounds);
				}
				break;
			case 320:
				instance.Width = ProtocolParser.ReadUInt32(stream);
				break;
			case 328:
				instance.Height = ProtocolParser.ReadUInt32(stream);
				break;
			case 336:
				instance.Length = ProtocolParser.ReadUInt32(stream);
				break;
			case 352:
				instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 360:
				instance.IsMissing = ProtocolParser.ReadUInt32(stream);
				break;
			case 370:
				instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
				break;
			case 378:
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

	public static Packet_ItemType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ItemType instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ItemType result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ItemType instance)
	{
		if (instance.ItemId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ItemId);
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Tags != null)
		{
			int[] tags = instance.Tags;
			int tagsCount = instance.TagsCount;
			for (int i = 0; i < tags.Length && i < tagsCount; i++)
			{
				stream.WriteKey(48, 0);
				ProtocolParser.WriteUInt32(stream, tags[i]);
			}
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] behaviors = instance.Behaviors;
			int behaviorsCount = instance.BehaviorsCount;
			for (int j = 0; j < behaviors.Length && j < behaviorsCount; j++)
			{
				stream.WriteKey(39, 2);
				Packet_BehaviorSerializer.SerializeWithSize(stream, behaviors[j]);
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] compositeTextures = instance.CompositeTextures;
			int compositeTexturesCount = instance.CompositeTexturesCount;
			for (int k = 0; k < compositeTextures.Length && k < compositeTexturesCount; k++)
			{
				stream.WriteByte(34);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, compositeTextures[k]);
			}
		}
		if (instance.Durability != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.Miningmaterial != null)
		{
			int[] miningmaterial = instance.Miningmaterial;
			int miningmaterialCount = instance.MiningmaterialCount;
			for (int l = 0; l < miningmaterial.Length && l < miningmaterialCount; l++)
			{
				stream.WriteByte(48);
				ProtocolParser.WriteUInt32(stream, miningmaterial[l]);
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			int[] miningmaterialspeed = instance.Miningmaterialspeed;
			int miningmaterialspeedCount = instance.MiningmaterialspeedCount;
			for (int m = 0; m < miningmaterialspeed.Length && m < miningmaterialspeedCount; m++)
			{
				stream.WriteKey(31, 0);
				ProtocolParser.WriteUInt32(stream, miningmaterialspeed[m]);
			}
		}
		if (instance.Damagedby != null)
		{
			int[] damagedby = instance.Damagedby;
			int damagedbyCount = instance.DamagedbyCount;
			for (int n = 0; n < damagedby.Length && n < damagedbyCount; n++)
			{
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, damagedby[n]);
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			string[] creativeInventoryTabs = instance.CreativeInventoryTabs;
			int creativeInventoryTabsCount = instance.CreativeInventoryTabsCount;
			for (int num = 0; num < creativeInventoryTabs.Length && num < creativeInventoryTabsCount; num++)
			{
				stream.WriteByte(74);
				ProtocolParser.WriteString(stream, creativeInventoryTabs[num]);
			}
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteByte(82);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GuiTransform);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteByte(90);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.FpHandTransform);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteByte(98);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpHandTransform);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(43, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpOffHandTransform);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(22, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GroundTransform);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteByte(114);
			Packet_CombustiblePropertiesSerializer.SerializeWithSize(stream, instance.CombustibleProps);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteByte(122);
			Packet_NutritionPropertiesSerializer.SerializeWithSize(stream, instance.NutritionProps);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(32, 2);
			Packet_GrindingPropertiesSerializer.SerializeWithSize(stream, instance.GrindingProps);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(38, 2);
			Packet_CrushingPropertiesSerializer.SerializeWithSize(stream, instance.CrushingProps);
		}
		if (instance.TransitionableProps != null)
		{
			Packet_TransitionableProperties[] transitionableProps = instance.TransitionableProps;
			int transitionablePropsCount = instance.TransitionablePropsCount;
			for (int num2 = 0; num2 < transitionableProps.Length && num2 < transitionablePropsCount; num2++)
			{
				stream.WriteKey(36, 2);
				Packet_TransitionablePropertiesSerializer.SerializeWithSize(stream, transitionableProps[num2]);
			}
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(16, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.TextureCodes != null)
		{
			string[] textureCodes = instance.TextureCodes;
			int textureCodesCount = instance.TextureCodesCount;
			for (int num3 = 0; num3 < textureCodes.Length && num3 < textureCodesCount; num3++)
			{
				stream.WriteKey(17, 2);
				ProtocolParser.WriteString(stream, textureCodes[num3]);
			}
		}
		if (instance.ItemClass != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteString(stream, instance.ItemClass);
		}
		if (instance.Tool != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(25, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(28, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpHitAnimation);
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(29, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightTpIdleAnimation);
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(34, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftTpIdleAnimation);
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(30, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpUseAnimation);
		}
		if (instance.MatterState != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] variant = instance.Variant;
			int variantCount = instance.VariantCount;
			for (int num4 = 0; num4 < variant.Length && num4 < variantCount; num4++)
			{
				stream.WriteKey(35, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, variant[num4]);
			}
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(37, 2);
			Packet_HeldSoundSetSerializer.SerializeWithSize(stream, instance.HeldSounds);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(40, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(41, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(42, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.LightHsv != null)
		{
			int[] lightHsv = instance.LightHsv;
			int lightHsvCount = instance.LightHsvCount;
			for (int num5 = 0; num5 < lightHsv.Length && num5 < lightHsvCount; num5++)
			{
				stream.WriteKey(44, 0);
				ProtocolParser.WriteUInt32(stream, lightHsv[num5]);
			}
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(45, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(46, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftReadyAnimation);
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(47, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightReadyAnimation);
		}
	}

	public static int GetSize(Packet_ItemType instance)
	{
		int num = 0;
		if (instance.ItemId != 0)
		{
			num += ProtocolParser.GetSize(instance.ItemId) + 1;
		}
		if (instance.MaxStackSize != 0)
		{
			num += ProtocolParser.GetSize(instance.MaxStackSize) + 1;
		}
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Tags != null)
		{
			for (int i = 0; i < instance.TagsCount; i++)
			{
				int v = instance.Tags[i];
				num += ProtocolParser.GetSize(v) + 2;
			}
		}
		if (instance.Behaviors != null)
		{
			for (int j = 0; j < instance.BehaviorsCount; j++)
			{
				int size = Packet_BehaviorSerializer.GetSize(instance.Behaviors[j]);
				num += size + ProtocolParser.GetSize(size) + 2;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int k = 0; k < instance.CompositeTexturesCount; k++)
			{
				int size2 = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[k]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.Durability != 0)
		{
			num += ProtocolParser.GetSize(instance.Durability) + 1;
		}
		if (instance.Miningmaterial != null)
		{
			for (int l = 0; l < instance.MiningmaterialCount; l++)
			{
				int v2 = instance.Miningmaterial[l];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int m = 0; m < instance.MiningmaterialspeedCount; m++)
			{
				int v3 = instance.Miningmaterialspeed[m];
				num += ProtocolParser.GetSize(v3) + 2;
			}
		}
		if (instance.Damagedby != null)
		{
			for (int n = 0; n < instance.DamagedbyCount; n++)
			{
				int v4 = instance.Damagedby[n];
				num += ProtocolParser.GetSize(v4) + 1;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			num += ProtocolParser.GetSize(instance.CreativeInventoryStacks) + 1;
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int num2 = 0; num2 < instance.CreativeInventoryTabsCount; num2++)
			{
				string s = instance.CreativeInventoryTabs[num2];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.GuiTransform != null)
		{
			int size3 = Packet_ModelTransformSerializer.GetSize(instance.GuiTransform);
			num += size3 + ProtocolParser.GetSize(size3) + 1;
		}
		if (instance.FpHandTransform != null)
		{
			int size4 = Packet_ModelTransformSerializer.GetSize(instance.FpHandTransform);
			num += size4 + ProtocolParser.GetSize(size4) + 1;
		}
		if (instance.TpHandTransform != null)
		{
			int size5 = Packet_ModelTransformSerializer.GetSize(instance.TpHandTransform);
			num += size5 + ProtocolParser.GetSize(size5) + 1;
		}
		if (instance.TpOffHandTransform != null)
		{
			int size6 = Packet_ModelTransformSerializer.GetSize(instance.TpOffHandTransform);
			num += size6 + ProtocolParser.GetSize(size6) + 2;
		}
		if (instance.GroundTransform != null)
		{
			int size7 = Packet_ModelTransformSerializer.GetSize(instance.GroundTransform);
			num += size7 + ProtocolParser.GetSize(size7) + 2;
		}
		if (instance.Attributes != null)
		{
			num += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.CombustibleProps != null)
		{
			int size8 = Packet_CombustiblePropertiesSerializer.GetSize(instance.CombustibleProps);
			num += size8 + ProtocolParser.GetSize(size8) + 1;
		}
		if (instance.NutritionProps != null)
		{
			int size9 = Packet_NutritionPropertiesSerializer.GetSize(instance.NutritionProps);
			num += size9 + ProtocolParser.GetSize(size9) + 1;
		}
		if (instance.GrindingProps != null)
		{
			int size10 = Packet_GrindingPropertiesSerializer.GetSize(instance.GrindingProps);
			num += size10 + ProtocolParser.GetSize(size10) + 2;
		}
		if (instance.CrushingProps != null)
		{
			int size11 = Packet_CrushingPropertiesSerializer.GetSize(instance.CrushingProps);
			num += size11 + ProtocolParser.GetSize(size11) + 2;
		}
		if (instance.TransitionableProps != null)
		{
			for (int num3 = 0; num3 < instance.TransitionablePropsCount; num3++)
			{
				int size12 = Packet_TransitionablePropertiesSerializer.GetSize(instance.TransitionableProps[num3]);
				num += size12 + ProtocolParser.GetSize(size12) + 2;
			}
		}
		if (instance.Shape != null)
		{
			int size13 = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			num += size13 + ProtocolParser.GetSize(size13) + 2;
		}
		if (instance.TextureCodes != null)
		{
			for (int num4 = 0; num4 < instance.TextureCodesCount; num4++)
			{
				string s2 = instance.TextureCodes[num4];
				num += ProtocolParser.GetSize(s2) + 2;
			}
		}
		if (instance.ItemClass != null)
		{
			num += ProtocolParser.GetSize(instance.ItemClass) + 2;
		}
		if (instance.Tool != 0)
		{
			num += ProtocolParser.GetSize(instance.Tool) + 2;
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
		if (instance.MatterState != 0)
		{
			num += ProtocolParser.GetSize(instance.MatterState) + 2;
		}
		if (instance.Variant != null)
		{
			for (int num5 = 0; num5 < instance.VariantCount; num5++)
			{
				int size14 = Packet_VariantPartSerializer.GetSize(instance.Variant[num5]);
				num += size14 + ProtocolParser.GetSize(size14) + 2;
			}
		}
		if (instance.HeldSounds != null)
		{
			int size15 = Packet_HeldSoundSetSerializer.GetSize(instance.HeldSounds);
			num += size15 + ProtocolParser.GetSize(size15) + 2;
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
		if (instance.LightHsv != null)
		{
			for (int num6 = 0; num6 < instance.LightHsvCount; num6++)
			{
				int v5 = instance.LightHsv[num6];
				num += ProtocolParser.GetSize(v5) + 2;
			}
		}
		if (instance.IsMissing != 0)
		{
			num += ProtocolParser.GetSize(instance.IsMissing) + 2;
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

	public static void SerializeWithSize(CitoStream stream, Packet_ItemType instance)
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

	public static byte[] SerializeToBytes(Packet_ItemType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ItemType instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
