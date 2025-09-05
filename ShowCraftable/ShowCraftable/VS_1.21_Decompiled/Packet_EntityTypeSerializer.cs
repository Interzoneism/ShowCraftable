using System;

public class Packet_EntityTypeSerializer
{
	private const int field = 8;

	public static Packet_EntityType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityType packet_EntityType = new Packet_EntityType();
		DeserializeLengthDelimited(stream, packet_EntityType);
		return packet_EntityType;
	}

	public static Packet_EntityType DeserializeBuffer(byte[] buffer, int length, Packet_EntityType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityType Deserialize(CitoMemoryStream stream, Packet_EntityType instance)
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
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Class = ProtocolParser.ReadString(stream);
				break;
			case 320:
				instance.TagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 26:
				instance.Renderer = ProtocolParser.ReadString(stream);
				break;
			case 32:
				instance.Habitat = ProtocolParser.ReadUInt32(stream);
				break;
			case 202:
				instance.Drops = ProtocolParser.ReadBytes(stream);
				break;
			case 90:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 42:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 48:
				instance.CollisionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.CollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 208:
				instance.DeadCollisionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.DeadCollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 256:
				instance.SelectionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 264:
				instance.SelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 272:
				instance.DeadSelectionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 280:
				instance.DeadSelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 66:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 74:
				instance.SoundKeysAdd(ProtocolParser.ReadString(stream));
				break;
			case 82:
				instance.SoundNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 112:
				instance.IdleSoundChance = ProtocolParser.ReadUInt32(stream);
				break;
			case 296:
				instance.IdleSoundRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 98:
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 106:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 120:
				instance.Size = ProtocolParser.ReadUInt32(stream);
				break;
			case 128:
				instance.EyeHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 288:
				instance.SwimmingEyeHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 232:
				instance.Weight = ProtocolParser.ReadUInt32(stream);
				break;
			case 136:
				instance.CanClimb = ProtocolParser.ReadUInt32(stream);
				break;
			case 146:
				instance.AnimationMetaData = ProtocolParser.ReadBytes(stream);
				break;
			case 152:
				instance.KnockbackResistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 160:
				instance.GlowLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.CanClimbAnywhere = ProtocolParser.ReadUInt32(stream);
				break;
			case 176:
				instance.ClimbTouchDistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.RotateModelOnClimb = ProtocolParser.ReadUInt32(stream);
				break;
			case 192:
				instance.FallDamage = ProtocolParser.ReadUInt32(stream);
				break;
			case 312:
				instance.FallDamageMultiplier = ProtocolParser.ReadUInt32(stream);
				break;
			case 226:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 240:
				instance.SizeGrowthFactor = ProtocolParser.ReadUInt32(stream);
				break;
			case 248:
				instance.PitchStep = ProtocolParser.ReadUInt32(stream);
				break;
			case 306:
				instance.Color = ProtocolParser.ReadString(stream);
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

	public static Packet_EntityType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityType instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityType result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityType instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Class != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Class);
		}
		if (instance.Tags != null)
		{
			int[] tags = instance.Tags;
			int tagsCount = instance.TagsCount;
			for (int i = 0; i < tags.Length && i < tagsCount; i++)
			{
				stream.WriteKey(40, 0);
				ProtocolParser.WriteUInt32(stream, tags[i]);
			}
		}
		if (instance.Renderer != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Renderer);
		}
		if (instance.Habitat != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Habitat);
		}
		if (instance.Drops != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteBytes(stream, instance.Drops);
		}
		if (instance.Shape != null)
		{
			stream.WriteByte(90);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] behaviors = instance.Behaviors;
			int behaviorsCount = instance.BehaviorsCount;
			for (int j = 0; j < behaviors.Length && j < behaviorsCount; j++)
			{
				stream.WriteByte(42);
				Packet_BehaviorSerializer.SerializeWithSize(stream, behaviors[j]);
			}
		}
		if (instance.CollisionBoxLength != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxLength);
		}
		if (instance.CollisionBoxHeight != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxHeight);
		}
		if (instance.DeadCollisionBoxLength != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxLength);
		}
		if (instance.DeadCollisionBoxHeight != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxHeight);
		}
		if (instance.SelectionBoxLength != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxLength);
		}
		if (instance.SelectionBoxHeight != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxHeight);
		}
		if (instance.DeadSelectionBoxLength != 0)
		{
			stream.WriteKey(34, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxLength);
		}
		if (instance.DeadSelectionBoxHeight != 0)
		{
			stream.WriteKey(35, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxHeight);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.SoundKeys != null)
		{
			string[] soundKeys = instance.SoundKeys;
			int soundKeysCount = instance.SoundKeysCount;
			for (int k = 0; k < soundKeys.Length && k < soundKeysCount; k++)
			{
				stream.WriteByte(74);
				ProtocolParser.WriteString(stream, soundKeys[k]);
			}
		}
		if (instance.SoundNames != null)
		{
			string[] soundNames = instance.SoundNames;
			int soundNamesCount = instance.SoundNamesCount;
			for (int l = 0; l < soundNames.Length && l < soundNamesCount; l++)
			{
				stream.WriteByte(82);
				ProtocolParser.WriteString(stream, soundNames[l]);
			}
		}
		if (instance.IdleSoundChance != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundChance);
		}
		if (instance.IdleSoundRange != 0)
		{
			stream.WriteKey(37, 0);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundRange);
		}
		if (instance.TextureCodes != null)
		{
			string[] textureCodes = instance.TextureCodes;
			int textureCodesCount = instance.TextureCodesCount;
			for (int m = 0; m < textureCodes.Length && m < textureCodesCount; m++)
			{
				stream.WriteByte(98);
				ProtocolParser.WriteString(stream, textureCodes[m]);
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] compositeTextures = instance.CompositeTextures;
			int compositeTexturesCount = instance.CompositeTexturesCount;
			for (int n = 0; n < compositeTextures.Length && n < compositeTexturesCount; n++)
			{
				stream.WriteByte(106);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, compositeTextures[n]);
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.EyeHeight != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.EyeHeight);
		}
		if (instance.SwimmingEyeHeight != 0)
		{
			stream.WriteKey(36, 0);
			ProtocolParser.WriteUInt32(stream, instance.SwimmingEyeHeight);
		}
		if (instance.Weight != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.Weight);
		}
		if (instance.CanClimb != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimb);
		}
		if (instance.AnimationMetaData != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteBytes(stream, instance.AnimationMetaData);
		}
		if (instance.KnockbackResistance != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.KnockbackResistance);
		}
		if (instance.GlowLevel != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.GlowLevel);
		}
		if (instance.CanClimbAnywhere != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimbAnywhere);
		}
		if (instance.ClimbTouchDistance != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.ClimbTouchDistance);
		}
		if (instance.RotateModelOnClimb != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RotateModelOnClimb);
		}
		if (instance.FallDamage != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamage);
		}
		if (instance.FallDamageMultiplier != 0)
		{
			stream.WriteKey(39, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamageMultiplier);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] variant = instance.Variant;
			int variantCount = instance.VariantCount;
			for (int num = 0; num < variant.Length && num < variantCount; num++)
			{
				stream.WriteKey(28, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, variant[num]);
			}
		}
		if (instance.SizeGrowthFactor != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.SizeGrowthFactor);
		}
		if (instance.PitchStep != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.PitchStep);
		}
		if (instance.Color != null)
		{
			stream.WriteKey(38, 2);
			ProtocolParser.WriteString(stream, instance.Color);
		}
	}

	public static int GetSize(Packet_EntityType instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Class != null)
		{
			num += ProtocolParser.GetSize(instance.Class) + 1;
		}
		if (instance.Tags != null)
		{
			for (int i = 0; i < instance.TagsCount; i++)
			{
				int v = instance.Tags[i];
				num += ProtocolParser.GetSize(v) + 2;
			}
		}
		if (instance.Renderer != null)
		{
			num += ProtocolParser.GetSize(instance.Renderer) + 1;
		}
		if (instance.Habitat != 0)
		{
			num += ProtocolParser.GetSize(instance.Habitat) + 1;
		}
		if (instance.Drops != null)
		{
			num += ProtocolParser.GetSize(instance.Drops) + 2;
		}
		if (instance.Shape != null)
		{
			int size = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.Behaviors != null)
		{
			for (int j = 0; j < instance.BehaviorsCount; j++)
			{
				int size2 = Packet_BehaviorSerializer.GetSize(instance.Behaviors[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.CollisionBoxLength != 0)
		{
			num += ProtocolParser.GetSize(instance.CollisionBoxLength) + 1;
		}
		if (instance.CollisionBoxHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.CollisionBoxHeight) + 1;
		}
		if (instance.DeadCollisionBoxLength != 0)
		{
			num += ProtocolParser.GetSize(instance.DeadCollisionBoxLength) + 2;
		}
		if (instance.DeadCollisionBoxHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.DeadCollisionBoxHeight) + 2;
		}
		if (instance.SelectionBoxLength != 0)
		{
			num += ProtocolParser.GetSize(instance.SelectionBoxLength) + 2;
		}
		if (instance.SelectionBoxHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.SelectionBoxHeight) + 2;
		}
		if (instance.DeadSelectionBoxLength != 0)
		{
			num += ProtocolParser.GetSize(instance.DeadSelectionBoxLength) + 2;
		}
		if (instance.DeadSelectionBoxHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.DeadSelectionBoxHeight) + 2;
		}
		if (instance.Attributes != null)
		{
			num += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.SoundKeys != null)
		{
			for (int k = 0; k < instance.SoundKeysCount; k++)
			{
				string s = instance.SoundKeys[k];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.SoundNames != null)
		{
			for (int l = 0; l < instance.SoundNamesCount; l++)
			{
				string s2 = instance.SoundNames[l];
				num += ProtocolParser.GetSize(s2) + 1;
			}
		}
		if (instance.IdleSoundChance != 0)
		{
			num += ProtocolParser.GetSize(instance.IdleSoundChance) + 1;
		}
		if (instance.IdleSoundRange != 0)
		{
			num += ProtocolParser.GetSize(instance.IdleSoundRange) + 2;
		}
		if (instance.TextureCodes != null)
		{
			for (int m = 0; m < instance.TextureCodesCount; m++)
			{
				string s3 = instance.TextureCodes[m];
				num += ProtocolParser.GetSize(s3) + 1;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int n = 0; n < instance.CompositeTexturesCount; n++)
			{
				int size3 = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[n]);
				num += size3 + ProtocolParser.GetSize(size3) + 1;
			}
		}
		if (instance.Size != 0)
		{
			num += ProtocolParser.GetSize(instance.Size) + 1;
		}
		if (instance.EyeHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.EyeHeight) + 2;
		}
		if (instance.SwimmingEyeHeight != 0)
		{
			num += ProtocolParser.GetSize(instance.SwimmingEyeHeight) + 2;
		}
		if (instance.Weight != 0)
		{
			num += ProtocolParser.GetSize(instance.Weight) + 2;
		}
		if (instance.CanClimb != 0)
		{
			num += ProtocolParser.GetSize(instance.CanClimb) + 2;
		}
		if (instance.AnimationMetaData != null)
		{
			num += ProtocolParser.GetSize(instance.AnimationMetaData) + 2;
		}
		if (instance.KnockbackResistance != 0)
		{
			num += ProtocolParser.GetSize(instance.KnockbackResistance) + 2;
		}
		if (instance.GlowLevel != 0)
		{
			num += ProtocolParser.GetSize(instance.GlowLevel) + 2;
		}
		if (instance.CanClimbAnywhere != 0)
		{
			num += ProtocolParser.GetSize(instance.CanClimbAnywhere) + 2;
		}
		if (instance.ClimbTouchDistance != 0)
		{
			num += ProtocolParser.GetSize(instance.ClimbTouchDistance) + 2;
		}
		if (instance.RotateModelOnClimb != 0)
		{
			num += ProtocolParser.GetSize(instance.RotateModelOnClimb) + 2;
		}
		if (instance.FallDamage != 0)
		{
			num += ProtocolParser.GetSize(instance.FallDamage) + 2;
		}
		if (instance.FallDamageMultiplier != 0)
		{
			num += ProtocolParser.GetSize(instance.FallDamageMultiplier) + 2;
		}
		if (instance.Variant != null)
		{
			for (int num2 = 0; num2 < instance.VariantCount; num2++)
			{
				int size4 = Packet_VariantPartSerializer.GetSize(instance.Variant[num2]);
				num += size4 + ProtocolParser.GetSize(size4) + 2;
			}
		}
		if (instance.SizeGrowthFactor != 0)
		{
			num += ProtocolParser.GetSize(instance.SizeGrowthFactor) + 2;
		}
		if (instance.PitchStep != 0)
		{
			num += ProtocolParser.GetSize(instance.PitchStep) + 2;
		}
		if (instance.Color != null)
		{
			num += ProtocolParser.GetSize(instance.Color) + 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityType instance)
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

	public static byte[] SerializeToBytes(Packet_EntityType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityType instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
