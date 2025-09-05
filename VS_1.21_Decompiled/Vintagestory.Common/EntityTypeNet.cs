using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public static class EntityTypeNet
{
	public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return EntityPropertiesToPacket(properties, ms);
	}

	public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties, FastMemoryStream ms)
	{
		Packet_EntityType packet_EntityType = new Packet_EntityType
		{
			Class = properties.Class,
			Habitat = (int)properties.Habitat,
			Code = properties.Code.ToShortString(),
			Tags = properties.Tags.ToArray().Select((System.Func<ushort, int>)((ushort tag) => tag)).ToArray(),
			Drops = getDropsPacket(properties.Drops, ms),
			Color = properties.Color,
			Shape = ((properties.Client?.Shape != null) ? CollectibleNet.ToPacket(properties.Client.Shape) : null),
			Renderer = properties.Client?.RendererName,
			GlowLevel = ((properties.Client != null) ? properties.Client.GlowLevel : 0),
			PitchStep = (properties.Client.PitchStep ? 1 : 0),
			Attributes = properties.Attributes?.ToString(),
			CollisionBoxLength = CollectibleNet.SerializePlayerPos(properties.CollisionBoxSize.X),
			CollisionBoxHeight = CollectibleNet.SerializePlayerPos(properties.CollisionBoxSize.Y),
			DeadCollisionBoxLength = CollectibleNet.SerializePlayerPos(properties.DeadCollisionBoxSize.X),
			DeadCollisionBoxHeight = CollectibleNet.SerializePlayerPos(properties.DeadCollisionBoxSize.Y),
			SelectionBoxLength = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.X)),
			SelectionBoxHeight = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.Y)),
			DeadSelectionBoxLength = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.X)),
			DeadSelectionBoxHeight = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.Y)),
			IdleSoundChance = CollectibleNet.SerializeFloatPrecise(100f * properties.IdleSoundChance),
			IdleSoundRange = CollectibleNet.SerializeFloatPrecise(properties.IdleSoundRange),
			Size = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 1f : properties.Client.Size),
			SizeGrowthFactor = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 0f : properties.Client.SizeGrowthFactor),
			EyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.EyeHeight),
			SwimmingEyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.SwimmingEyeHeight),
			Weight = CollectibleNet.SerializeFloatPrecise(properties.Weight),
			CanClimb = (properties.CanClimb ? 1 : 0),
			CanClimbAnywhere = (properties.CanClimbAnywhere ? 1 : 0),
			FallDamage = (properties.FallDamage ? 1 : 0),
			FallDamageMultiplier = CollectibleNet.SerializeFloatPrecise(properties.FallDamageMultiplier),
			RotateModelOnClimb = (properties.RotateModelOnClimb ? 1 : 0),
			ClimbTouchDistance = CollectibleNet.SerializeFloatVeryPrecise(properties.ClimbTouchDistance),
			KnockbackResistance = CollectibleNet.SerializeFloatPrecise(properties.KnockbackResistance)
		};
		packet_EntityType.SetVariant(CollectibleNet.ToPacket(properties.Variant));
		if (properties.Client?.Textures != null)
		{
			packet_EntityType.SetTextureCodes(properties.Client.Textures.Keys.ToArray());
			packet_EntityType.SetCompositeTextures(CollectibleNet.ToPackets(properties.Client.Textures.Values.ToArray()));
		}
		if (properties.Client?.Animations != null)
		{
			ms.Reset();
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			AnimationMetaData[] animations = properties.Client.Animations;
			binaryWriter.Write(animations.Length);
			for (int num = 0; num < animations.Length; num++)
			{
				animations[num].ToBytes(binaryWriter);
			}
			packet_EntityType.SetAnimationMetaData(ms.ToArray());
		}
		if (properties.Client?.BehaviorsAsJsonObj != null)
		{
			JsonObject[] behaviorsAsJsonObj = properties.Client.BehaviorsAsJsonObj;
			Packet_Behavior[] array = new Packet_Behavior[behaviorsAsJsonObj.Length];
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				array[num2] = new Packet_Behavior
				{
					Attributes = behaviorsAsJsonObj[num2].ToString()
				};
			}
			packet_EntityType.SetBehaviors(array);
		}
		if (properties.Sounds != null)
		{
			packet_EntityType.SetSoundKeys(properties.Sounds.Keys.ToArray());
			AssetLocation[] array2 = properties.Sounds.Values.ToArray();
			string[] array3 = new string[properties.Sounds.Count];
			for (int num3 = 0; num3 < array3.Length; num3++)
			{
				array3[num3] = array2[num3].ToString();
			}
			packet_EntityType.SetSoundNames(array3);
		}
		return packet_EntityType;
	}

	public static EntityProperties FromPacket(Packet_EntityType packet, IWorldAccessor worldForResolve)
	{
		JsonObject[] array = new JsonObject[packet.BehaviorsCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new JsonObject(JToken.Parse(packet.Behaviors[i].Attributes));
		}
		Dictionary<string, AssetLocation> dictionary = new Dictionary<string, AssetLocation>();
		if (packet.SoundKeys != null)
		{
			for (int j = 0; j < packet.SoundKeysCount; j++)
			{
				dictionary[packet.SoundKeys[j]] = new AssetLocation(packet.SoundNames[j]);
			}
		}
		AssetLocation code = new AssetLocation(packet.Code);
		EntityProperties entityProperties = new EntityProperties
		{
			Class = packet.Class,
			Variant = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount),
			Code = code,
			Tags = ((packet.Tags != null) ? new EntityTagArray(packet.Tags.Select((int tag) => (ushort)tag)) : new EntityTagArray()),
			Color = packet.Color,
			Habitat = (EnumHabitat)packet.Habitat,
			DropsPacket = packet.Drops,
			Client = new EntityClientProperties(array, null)
			{
				GlowLevel = packet.GlowLevel,
				PitchStep = (packet.PitchStep > 0),
				RendererName = packet.Renderer,
				Shape = ((packet.Shape != null) ? CollectibleNet.FromPacket(packet.Shape) : null),
				Size = CollectibleNet.DeserializeFloatPrecise(packet.Size),
				SizeGrowthFactor = CollectibleNet.DeserializeFloatPrecise(packet.SizeGrowthFactor)
			},
			CollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxHeight)),
			DeadCollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxHeight)),
			SelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxHeight)),
			DeadSelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxHeight)),
			Attributes = ((packet.Attributes == null) ? null : new JsonObject(JToken.Parse(packet.Attributes))),
			Sounds = dictionary,
			IdleSoundChance = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundChance) / 100f,
			IdleSoundRange = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundRange),
			EyeHeight = CollectibleNet.DeserializeFloatPrecise(packet.EyeHeight),
			SwimmingEyeHeight = CollectibleNet.DeserializeFloatPrecise(packet.SwimmingEyeHeight),
			Weight = CollectibleNet.DeserializeFloatPrecise(packet.Weight),
			CanClimb = (packet.CanClimb > 0),
			CanClimbAnywhere = (packet.CanClimbAnywhere > 0),
			FallDamage = (packet.FallDamage > 0),
			FallDamageMultiplier = CollectibleNet.DeserializeFloatPrecise(packet.FallDamageMultiplier),
			RotateModelOnClimb = (packet.RotateModelOnClimb > 0),
			ClimbTouchDistance = CollectibleNet.DeserializeFloatVeryPrecise(packet.ClimbTouchDistance),
			KnockbackResistance = CollectibleNet.DeserializeFloatPrecise(packet.KnockbackResistance)
		};
		if (entityProperties.SelectionBoxSize.X < 0f)
		{
			entityProperties.SelectionBoxSize = null;
		}
		if (entityProperties.DeadSelectionBoxSize.X < 0f)
		{
			entityProperties.DeadSelectionBoxSize = null;
		}
		if (packet.AnimationMetaData != null)
		{
			using MemoryStream input = new MemoryStream(packet.AnimationMetaData);
			BinaryReader binaryReader = new BinaryReader(input);
			int num = binaryReader.ReadInt32();
			entityProperties.Client.Animations = new AnimationMetaData[num];
			for (int num2 = 0; num2 < num; num2++)
			{
				entityProperties.Client.Animations[num2] = AnimationMetaData.FromBytes(binaryReader, "1.21.0");
			}
		}
		entityProperties.Client.Init(entityProperties.Code, worldForResolve);
		entityProperties.Client.Textures = new FastSmallDictionary<string, CompositeTexture>(packet.TextureCodesCount);
		for (int num3 = 0; num3 < packet.TextureCodesCount; num3++)
		{
			entityProperties.Client.Textures.Add(packet.TextureCodes[num3], CollectibleNet.FromPacket(packet.CompositeTextures[num3]));
		}
		CompositeTexture[] array2 = entityProperties.Client.FirstTexture?.Alternates;
		entityProperties.Client.TexturesAlternatesCount = ((array2 != null) ? array2.Length : 0);
		return entityProperties;
	}

	private static byte[] getDropsPacket(BlockDropItemStack[] drops, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter binaryWriter = new BinaryWriter(ms);
		if (drops == null)
		{
			binaryWriter.Write(0);
		}
		else
		{
			binaryWriter.Write(drops.Length);
			for (int i = 0; i < drops.Length; i++)
			{
				drops[i].ToBytes(binaryWriter);
			}
		}
		return ms.ToArray();
	}
}
