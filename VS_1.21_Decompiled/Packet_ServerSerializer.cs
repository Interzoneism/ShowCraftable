using System;

public class Packet_ServerSerializer
{
	private const int field = 8;

	public static Packet_Server DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Server packet_Server = new Packet_Server();
		DeserializeLengthDelimited(stream, packet_Server);
		return packet_Server;
	}

	public static Packet_Server DeserializeBuffer(byte[] buffer, int length, Packet_Server instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Server Deserialize(CitoMemoryStream stream, Packet_Server instance)
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
			case 720:
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 618:
				if (instance.Token == null)
				{
					instance.Token = Packet_LoginTokenAnswerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LoginTokenAnswerSerializer.DeserializeLengthDelimited(stream, instance.Token);
				}
				break;
			case 10:
				if (instance.Identification == null)
				{
					instance.Identification = Packet_ServerIdentificationSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerIdentificationSerializer.DeserializeLengthDelimited(stream, instance.Identification);
				}
				break;
			case 18:
				if (instance.LevelInitialize == null)
				{
					instance.LevelInitialize = Packet_ServerLevelInitializeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelInitializeSerializer.DeserializeLengthDelimited(stream, instance.LevelInitialize);
				}
				break;
			case 26:
				if (instance.LevelDataChunk == null)
				{
					instance.LevelDataChunk = Packet_ServerLevelProgressSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelProgressSerializer.DeserializeLengthDelimited(stream, instance.LevelDataChunk);
				}
				break;
			case 34:
				if (instance.LevelFinalize == null)
				{
					instance.LevelFinalize = Packet_ServerLevelFinalizeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelFinalizeSerializer.DeserializeLengthDelimited(stream, instance.LevelFinalize);
				}
				break;
			case 42:
				if (instance.SetBlock == null)
				{
					instance.SetBlock = Packet_ServerSetBlockSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlockSerializer.DeserializeLengthDelimited(stream, instance.SetBlock);
				}
				break;
			case 58:
				if (instance.Chatline == null)
				{
					instance.Chatline = Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance.Chatline);
				}
				break;
			case 66:
				if (instance.DisconnectPlayer == null)
				{
					instance.DisconnectPlayer = Packet_ServerDisconnectPlayerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerDisconnectPlayerSerializer.DeserializeLengthDelimited(stream, instance.DisconnectPlayer);
				}
				break;
			case 74:
				if (instance.Chunks == null)
				{
					instance.Chunks = Packet_ServerChunksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerChunksSerializer.DeserializeLengthDelimited(stream, instance.Chunks);
				}
				break;
			case 82:
				if (instance.UnloadChunk == null)
				{
					instance.UnloadChunk = Packet_UnloadServerChunkSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UnloadServerChunkSerializer.DeserializeLengthDelimited(stream, instance.UnloadChunk);
				}
				break;
			case 90:
				if (instance.Calendar == null)
				{
					instance.Calendar = Packet_ServerCalendarSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerCalendarSerializer.DeserializeLengthDelimited(stream, instance.Calendar);
				}
				break;
			case 122:
				if (instance.MapChunk == null)
				{
					instance.MapChunk = Packet_ServerMapChunkSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerMapChunkSerializer.DeserializeLengthDelimited(stream, instance.MapChunk);
				}
				break;
			case 130:
				if (instance.Ping == null)
				{
					instance.Ping = Packet_ServerPingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerPingSerializer.DeserializeLengthDelimited(stream, instance.Ping);
				}
				break;
			case 138:
				if (instance.PlayerPing == null)
				{
					instance.PlayerPing = Packet_ServerPlayerPingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerPlayerPingSerializer.DeserializeLengthDelimited(stream, instance.PlayerPing);
				}
				break;
			case 146:
				if (instance.Sound == null)
				{
					instance.Sound = Packet_ServerSoundSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSoundSerializer.DeserializeLengthDelimited(stream, instance.Sound);
				}
				break;
			case 154:
				if (instance.Assets == null)
				{
					instance.Assets = Packet_ServerAssetsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerAssetsSerializer.DeserializeLengthDelimited(stream, instance.Assets);
				}
				break;
			case 170:
				if (instance.WorldMetaData == null)
				{
					instance.WorldMetaData = Packet_WorldMetaDataSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_WorldMetaDataSerializer.DeserializeLengthDelimited(stream, instance.WorldMetaData);
				}
				break;
			case 226:
				if (instance.QueryAnswer == null)
				{
					instance.QueryAnswer = Packet_ServerQueryAnswerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerQueryAnswerSerializer.DeserializeLengthDelimited(stream, instance.QueryAnswer);
				}
				break;
			case 234:
				if (instance.Redirect == null)
				{
					instance.Redirect = Packet_ServerRedirectSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerRedirectSerializer.DeserializeLengthDelimited(stream, instance.Redirect);
				}
				break;
			case 242:
				if (instance.InventoryContents == null)
				{
					instance.InventoryContents = Packet_InventoryContentsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryContentsSerializer.DeserializeLengthDelimited(stream, instance.InventoryContents);
				}
				break;
			case 250:
				if (instance.InventoryUpdate == null)
				{
					instance.InventoryUpdate = Packet_InventoryUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryUpdateSerializer.DeserializeLengthDelimited(stream, instance.InventoryUpdate);
				}
				break;
			case 258:
				if (instance.InventoryDoubleUpdate == null)
				{
					instance.InventoryDoubleUpdate = Packet_InventoryDoubleUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryDoubleUpdateSerializer.DeserializeLengthDelimited(stream, instance.InventoryDoubleUpdate);
				}
				break;
			case 274:
				if (instance.Entity == null)
				{
					instance.Entity = Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitySerializer.DeserializeLengthDelimited(stream, instance.Entity);
				}
				break;
			case 282:
				if (instance.EntitySpawn == null)
				{
					instance.EntitySpawn = Packet_EntitySpawnSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitySpawnSerializer.DeserializeLengthDelimited(stream, instance.EntitySpawn);
				}
				break;
			case 290:
				if (instance.EntityDespawn == null)
				{
					instance.EntityDespawn = Packet_EntityDespawnSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityDespawnSerializer.DeserializeLengthDelimited(stream, instance.EntityDespawn);
				}
				break;
			case 306:
				if (instance.EntityAttributes == null)
				{
					instance.EntityAttributes = Packet_EntityAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityAttributesSerializer.DeserializeLengthDelimited(stream, instance.EntityAttributes);
				}
				break;
			case 314:
				if (instance.EntityAttributeUpdate == null)
				{
					instance.EntityAttributeUpdate = Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimited(stream, instance.EntityAttributeUpdate);
				}
				break;
			case 538:
				if (instance.EntityPacket == null)
				{
					instance.EntityPacket = Packet_EntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance.EntityPacket);
				}
				break;
			case 322:
				if (instance.Entities == null)
				{
					instance.Entities = Packet_EntitiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitiesSerializer.DeserializeLengthDelimited(stream, instance.Entities);
				}
				break;
			case 330:
				if (instance.PlayerData == null)
				{
					instance.PlayerData = Packet_PlayerDataSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerDataSerializer.DeserializeLengthDelimited(stream, instance.PlayerData);
				}
				break;
			case 338:
				if (instance.MapRegion == null)
				{
					instance.MapRegion = Packet_MapRegionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MapRegionSerializer.DeserializeLengthDelimited(stream, instance.MapRegion);
				}
				break;
			case 354:
				if (instance.BlockEntityMessage == null)
				{
					instance.BlockEntityMessage = Packet_BlockEntityMessageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntityMessageSerializer.DeserializeLengthDelimited(stream, instance.BlockEntityMessage);
				}
				break;
			case 362:
				if (instance.PlayerDeath == null)
				{
					instance.PlayerDeath = Packet_PlayerDeathSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerDeathSerializer.DeserializeLengthDelimited(stream, instance.PlayerDeath);
				}
				break;
			case 370:
				if (instance.ModeChange == null)
				{
					instance.ModeChange = Packet_PlayerModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance.ModeChange);
				}
				break;
			case 378:
				if (instance.SetBlocks == null)
				{
					instance.SetBlocks = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocks);
				}
				break;
			case 386:
				if (instance.BlockEntities == null)
				{
					instance.BlockEntities = Packet_BlockEntitiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntitiesSerializer.DeserializeLengthDelimited(stream, instance.BlockEntities);
				}
				break;
			case 394:
				if (instance.PlayerGroups == null)
				{
					instance.PlayerGroups = Packet_PlayerGroupsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerGroupsSerializer.DeserializeLengthDelimited(stream, instance.PlayerGroups);
				}
				break;
			case 402:
				if (instance.PlayerGroup == null)
				{
					instance.PlayerGroup = Packet_PlayerGroupSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerGroupSerializer.DeserializeLengthDelimited(stream, instance.PlayerGroup);
				}
				break;
			case 410:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 418:
				if (instance.HighlightBlocks == null)
				{
					instance.HighlightBlocks = Packet_HighlightBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HighlightBlocksSerializer.DeserializeLengthDelimited(stream, instance.HighlightBlocks);
				}
				break;
			case 426:
				if (instance.SelectedHotbarSlot == null)
				{
					instance.SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance.SelectedHotbarSlot);
				}
				break;
			case 442:
				if (instance.CustomPacket == null)
				{
					instance.CustomPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.CustomPacket);
				}
				break;
			case 450:
				if (instance.NetworkChannels == null)
				{
					instance.NetworkChannels = Packet_NetworkChannelsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NetworkChannelsSerializer.DeserializeLengthDelimited(stream, instance.NetworkChannels);
				}
				break;
			case 458:
				if (instance.GotoGroup == null)
				{
					instance.GotoGroup = Packet_GotoGroupSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GotoGroupSerializer.DeserializeLengthDelimited(stream, instance.GotoGroup);
				}
				break;
			case 466:
				if (instance.ExchangeBlock == null)
				{
					instance.ExchangeBlock = Packet_ServerExchangeBlockSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerExchangeBlockSerializer.DeserializeLengthDelimited(stream, instance.ExchangeBlock);
				}
				break;
			case 474:
				if (instance.BulkEntityAttributes == null)
				{
					instance.BulkEntityAttributes = Packet_BulkEntityAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityAttributesSerializer.DeserializeLengthDelimited(stream, instance.BulkEntityAttributes);
				}
				break;
			case 482:
				if (instance.SpawnParticles == null)
				{
					instance.SpawnParticles = Packet_SpawnParticlesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SpawnParticlesSerializer.DeserializeLengthDelimited(stream, instance.SpawnParticles);
				}
				break;
			case 490:
				if (instance.BulkEntityDebugAttributes == null)
				{
					instance.BulkEntityDebugAttributes = Packet_BulkEntityDebugAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityDebugAttributesSerializer.DeserializeLengthDelimited(stream, instance.BulkEntityDebugAttributes);
				}
				break;
			case 498:
				if (instance.SetBlocksNoRelight == null)
				{
					instance.SetBlocksNoRelight = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocksNoRelight);
				}
				break;
			case 514:
				if (instance.BlockDamage == null)
				{
					instance.BlockDamage = Packet_BlockDamageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance.BlockDamage);
				}
				break;
			case 522:
				if (instance.Ambient == null)
				{
					instance.Ambient = Packet_AmbientSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_AmbientSerializer.DeserializeLengthDelimited(stream, instance.Ambient);
				}
				break;
			case 530:
				if (instance.NotifySlot == null)
				{
					instance.NotifySlot = Packet_NotifySlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NotifySlotSerializer.DeserializeLengthDelimited(stream, instance.NotifySlot);
				}
				break;
			case 546:
				if (instance.IngameError == null)
				{
					instance.IngameError = Packet_IngameErrorSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IngameErrorSerializer.DeserializeLengthDelimited(stream, instance.IngameError);
				}
				break;
			case 554:
				if (instance.IngameDiscovery == null)
				{
					instance.IngameDiscovery = Packet_IngameDiscoverySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IngameDiscoverySerializer.DeserializeLengthDelimited(stream, instance.IngameDiscovery);
				}
				break;
			case 562:
				if (instance.SetBlocksMinimal == null)
				{
					instance.SetBlocksMinimal = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocksMinimal);
				}
				break;
			case 570:
				if (instance.SetDecors == null)
				{
					instance.SetDecors = Packet_ServerSetDecorsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetDecorsSerializer.DeserializeLengthDelimited(stream, instance.SetDecors);
				}
				break;
			case 578:
				if (instance.RemoveBlockLight == null)
				{
					instance.RemoveBlockLight = Packet_RemoveBlockLightSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RemoveBlockLightSerializer.DeserializeLengthDelimited(stream, instance.RemoveBlockLight);
				}
				break;
			case 586:
				if (instance.ServerReady == null)
				{
					instance.ServerReady = Packet_ServerReadySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerReadySerializer.DeserializeLengthDelimited(stream, instance.ServerReady);
				}
				break;
			case 594:
				if (instance.UnloadMapRegion == null)
				{
					instance.UnloadMapRegion = Packet_UnloadMapRegionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UnloadMapRegionSerializer.DeserializeLengthDelimited(stream, instance.UnloadMapRegion);
				}
				break;
			case 602:
				if (instance.LandClaims == null)
				{
					instance.LandClaims = Packet_LandClaimsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LandClaimsSerializer.DeserializeLengthDelimited(stream, instance.LandClaims);
				}
				break;
			case 610:
				if (instance.Roles == null)
				{
					instance.Roles = Packet_RolesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RolesSerializer.DeserializeLengthDelimited(stream, instance.Roles);
				}
				break;
			case 626:
				if (instance.UdpPacket == null)
				{
					instance.UdpPacket = Packet_UdpPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance.UdpPacket);
				}
				break;
			case 634:
				if (instance.QueuePacket == null)
				{
					instance.QueuePacket = Packet_QueuePacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_QueuePacketSerializer.DeserializeLengthDelimited(stream, instance.QueuePacket);
				}
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

	public static Packet_Server DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Server instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Server result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Server instance)
	{
		if (instance.Id != 1)
		{
			stream.WriteKey(90, 0);
			ProtocolParser.WriteUInt32(stream, instance.Id);
		}
		if (instance.Token != null)
		{
			stream.WriteKey(77, 2);
			Packet_LoginTokenAnswer token = instance.Token;
			Packet_LoginTokenAnswerSerializer.GetSize(token);
			Packet_LoginTokenAnswerSerializer.SerializeWithSize(stream, token);
		}
		if (instance.Identification != null)
		{
			stream.WriteByte(10);
			Packet_ServerIdentification identification = instance.Identification;
			Packet_ServerIdentificationSerializer.GetSize(identification);
			Packet_ServerIdentificationSerializer.SerializeWithSize(stream, identification);
		}
		if (instance.LevelInitialize != null)
		{
			stream.WriteByte(18);
			Packet_ServerLevelInitialize levelInitialize = instance.LevelInitialize;
			Packet_ServerLevelInitializeSerializer.GetSize(levelInitialize);
			Packet_ServerLevelInitializeSerializer.SerializeWithSize(stream, levelInitialize);
		}
		if (instance.LevelDataChunk != null)
		{
			stream.WriteByte(26);
			Packet_ServerLevelProgress levelDataChunk = instance.LevelDataChunk;
			Packet_ServerLevelProgressSerializer.GetSize(levelDataChunk);
			Packet_ServerLevelProgressSerializer.SerializeWithSize(stream, levelDataChunk);
		}
		if (instance.LevelFinalize != null)
		{
			stream.WriteByte(34);
			Packet_ServerLevelFinalize levelFinalize = instance.LevelFinalize;
			Packet_ServerLevelFinalizeSerializer.GetSize(levelFinalize);
			Packet_ServerLevelFinalizeSerializer.SerializeWithSize(stream, levelFinalize);
		}
		if (instance.SetBlock != null)
		{
			stream.WriteByte(42);
			Packet_ServerSetBlock setBlock = instance.SetBlock;
			Packet_ServerSetBlockSerializer.GetSize(setBlock);
			Packet_ServerSetBlockSerializer.SerializeWithSize(stream, setBlock);
		}
		if (instance.Chatline != null)
		{
			stream.WriteByte(58);
			Packet_ChatLine chatline = instance.Chatline;
			Packet_ChatLineSerializer.GetSize(chatline);
			Packet_ChatLineSerializer.SerializeWithSize(stream, chatline);
		}
		if (instance.DisconnectPlayer != null)
		{
			stream.WriteByte(66);
			Packet_ServerDisconnectPlayer disconnectPlayer = instance.DisconnectPlayer;
			Packet_ServerDisconnectPlayerSerializer.GetSize(disconnectPlayer);
			Packet_ServerDisconnectPlayerSerializer.SerializeWithSize(stream, disconnectPlayer);
		}
		if (instance.Chunks != null)
		{
			stream.WriteByte(74);
			Packet_ServerChunks chunks = instance.Chunks;
			Packet_ServerChunksSerializer.GetSize(chunks);
			Packet_ServerChunksSerializer.SerializeWithSize(stream, chunks);
		}
		if (instance.UnloadChunk != null)
		{
			stream.WriteByte(82);
			Packet_UnloadServerChunk unloadChunk = instance.UnloadChunk;
			Packet_UnloadServerChunkSerializer.GetSize(unloadChunk);
			Packet_UnloadServerChunkSerializer.SerializeWithSize(stream, unloadChunk);
		}
		if (instance.Calendar != null)
		{
			stream.WriteByte(90);
			Packet_ServerCalendar calendar = instance.Calendar;
			Packet_ServerCalendarSerializer.GetSize(calendar);
			Packet_ServerCalendarSerializer.SerializeWithSize(stream, calendar);
		}
		if (instance.MapChunk != null)
		{
			stream.WriteByte(122);
			Packet_ServerMapChunk mapChunk = instance.MapChunk;
			Packet_ServerMapChunkSerializer.GetSize(mapChunk);
			Packet_ServerMapChunkSerializer.SerializeWithSize(stream, mapChunk);
		}
		if (instance.Ping != null)
		{
			stream.WriteKey(16, 2);
			Packet_ServerPing ping = instance.Ping;
			Packet_ServerPingSerializer.GetSize(ping);
			Packet_ServerPingSerializer.SerializeWithSize(stream, ping);
		}
		if (instance.PlayerPing != null)
		{
			stream.WriteKey(17, 2);
			Packet_ServerPlayerPing playerPing = instance.PlayerPing;
			Packet_ServerPlayerPingSerializer.GetSize(playerPing);
			Packet_ServerPlayerPingSerializer.SerializeWithSize(stream, playerPing);
		}
		if (instance.Sound != null)
		{
			stream.WriteKey(18, 2);
			Packet_ServerSound sound = instance.Sound;
			Packet_ServerSoundSerializer.GetSize(sound);
			Packet_ServerSoundSerializer.SerializeWithSize(stream, sound);
		}
		if (instance.Assets != null)
		{
			stream.WriteKey(19, 2);
			Packet_ServerAssets assets = instance.Assets;
			Packet_ServerAssetsSerializer.GetSize(assets);
			Packet_ServerAssetsSerializer.SerializeWithSize(stream, assets);
		}
		if (instance.WorldMetaData != null)
		{
			stream.WriteKey(21, 2);
			Packet_WorldMetaData worldMetaData = instance.WorldMetaData;
			Packet_WorldMetaDataSerializer.GetSize(worldMetaData);
			Packet_WorldMetaDataSerializer.SerializeWithSize(stream, worldMetaData);
		}
		if (instance.QueryAnswer != null)
		{
			stream.WriteKey(28, 2);
			Packet_ServerQueryAnswer queryAnswer = instance.QueryAnswer;
			Packet_ServerQueryAnswerSerializer.GetSize(queryAnswer);
			Packet_ServerQueryAnswerSerializer.SerializeWithSize(stream, queryAnswer);
		}
		if (instance.Redirect != null)
		{
			stream.WriteKey(29, 2);
			Packet_ServerRedirect redirect = instance.Redirect;
			Packet_ServerRedirectSerializer.GetSize(redirect);
			Packet_ServerRedirectSerializer.SerializeWithSize(stream, redirect);
		}
		if (instance.InventoryContents != null)
		{
			stream.WriteKey(30, 2);
			Packet_InventoryContents inventoryContents = instance.InventoryContents;
			Packet_InventoryContentsSerializer.GetSize(inventoryContents);
			Packet_InventoryContentsSerializer.SerializeWithSize(stream, inventoryContents);
		}
		if (instance.InventoryUpdate != null)
		{
			stream.WriteKey(31, 2);
			Packet_InventoryUpdate inventoryUpdate = instance.InventoryUpdate;
			Packet_InventoryUpdateSerializer.GetSize(inventoryUpdate);
			Packet_InventoryUpdateSerializer.SerializeWithSize(stream, inventoryUpdate);
		}
		if (instance.InventoryDoubleUpdate != null)
		{
			stream.WriteKey(32, 2);
			Packet_InventoryDoubleUpdate inventoryDoubleUpdate = instance.InventoryDoubleUpdate;
			Packet_InventoryDoubleUpdateSerializer.GetSize(inventoryDoubleUpdate);
			Packet_InventoryDoubleUpdateSerializer.SerializeWithSize(stream, inventoryDoubleUpdate);
		}
		if (instance.Entity != null)
		{
			stream.WriteKey(34, 2);
			Packet_Entity entity = instance.Entity;
			Packet_EntitySerializer.GetSize(entity);
			Packet_EntitySerializer.SerializeWithSize(stream, entity);
		}
		if (instance.EntitySpawn != null)
		{
			stream.WriteKey(35, 2);
			Packet_EntitySpawn entitySpawn = instance.EntitySpawn;
			Packet_EntitySpawnSerializer.GetSize(entitySpawn);
			Packet_EntitySpawnSerializer.SerializeWithSize(stream, entitySpawn);
		}
		if (instance.EntityDespawn != null)
		{
			stream.WriteKey(36, 2);
			Packet_EntityDespawn entityDespawn = instance.EntityDespawn;
			Packet_EntityDespawnSerializer.GetSize(entityDespawn);
			Packet_EntityDespawnSerializer.SerializeWithSize(stream, entityDespawn);
		}
		if (instance.EntityAttributes != null)
		{
			stream.WriteKey(38, 2);
			Packet_EntityAttributes entityAttributes = instance.EntityAttributes;
			Packet_EntityAttributesSerializer.GetSize(entityAttributes);
			Packet_EntityAttributesSerializer.SerializeWithSize(stream, entityAttributes);
		}
		if (instance.EntityAttributeUpdate != null)
		{
			stream.WriteKey(39, 2);
			Packet_EntityAttributeUpdate entityAttributeUpdate = instance.EntityAttributeUpdate;
			Packet_EntityAttributeUpdateSerializer.GetSize(entityAttributeUpdate);
			Packet_EntityAttributeUpdateSerializer.SerializeWithSize(stream, entityAttributeUpdate);
		}
		if (instance.EntityPacket != null)
		{
			stream.WriteKey(67, 2);
			Packet_EntityPacket entityPacket = instance.EntityPacket;
			Packet_EntityPacketSerializer.GetSize(entityPacket);
			Packet_EntityPacketSerializer.SerializeWithSize(stream, entityPacket);
		}
		if (instance.Entities != null)
		{
			stream.WriteKey(40, 2);
			Packet_Entities entities = instance.Entities;
			Packet_EntitiesSerializer.GetSize(entities);
			Packet_EntitiesSerializer.SerializeWithSize(stream, entities);
		}
		if (instance.PlayerData != null)
		{
			stream.WriteKey(41, 2);
			Packet_PlayerData playerData = instance.PlayerData;
			Packet_PlayerDataSerializer.GetSize(playerData);
			Packet_PlayerDataSerializer.SerializeWithSize(stream, playerData);
		}
		if (instance.MapRegion != null)
		{
			stream.WriteKey(42, 2);
			Packet_MapRegion mapRegion = instance.MapRegion;
			Packet_MapRegionSerializer.GetSize(mapRegion);
			Packet_MapRegionSerializer.SerializeWithSize(stream, mapRegion);
		}
		if (instance.BlockEntityMessage != null)
		{
			stream.WriteKey(44, 2);
			Packet_BlockEntityMessage blockEntityMessage = instance.BlockEntityMessage;
			Packet_BlockEntityMessageSerializer.GetSize(blockEntityMessage);
			Packet_BlockEntityMessageSerializer.SerializeWithSize(stream, blockEntityMessage);
		}
		if (instance.PlayerDeath != null)
		{
			stream.WriteKey(45, 2);
			Packet_PlayerDeath playerDeath = instance.PlayerDeath;
			Packet_PlayerDeathSerializer.GetSize(playerDeath);
			Packet_PlayerDeathSerializer.SerializeWithSize(stream, playerDeath);
		}
		if (instance.ModeChange != null)
		{
			stream.WriteKey(46, 2);
			Packet_PlayerMode modeChange = instance.ModeChange;
			Packet_PlayerModeSerializer.GetSize(modeChange);
			Packet_PlayerModeSerializer.SerializeWithSize(stream, modeChange);
		}
		if (instance.SetBlocks != null)
		{
			stream.WriteKey(47, 2);
			Packet_ServerSetBlocks setBlocks = instance.SetBlocks;
			Packet_ServerSetBlocksSerializer.GetSize(setBlocks);
			Packet_ServerSetBlocksSerializer.SerializeWithSize(stream, setBlocks);
		}
		if (instance.BlockEntities != null)
		{
			stream.WriteKey(48, 2);
			Packet_BlockEntities blockEntities = instance.BlockEntities;
			Packet_BlockEntitiesSerializer.GetSize(blockEntities);
			Packet_BlockEntitiesSerializer.SerializeWithSize(stream, blockEntities);
		}
		if (instance.PlayerGroups != null)
		{
			stream.WriteKey(49, 2);
			Packet_PlayerGroups playerGroups = instance.PlayerGroups;
			Packet_PlayerGroupsSerializer.GetSize(playerGroups);
			Packet_PlayerGroupsSerializer.SerializeWithSize(stream, playerGroups);
		}
		if (instance.PlayerGroup != null)
		{
			stream.WriteKey(50, 2);
			Packet_PlayerGroup playerGroup = instance.PlayerGroup;
			Packet_PlayerGroupSerializer.GetSize(playerGroup);
			Packet_PlayerGroupSerializer.SerializeWithSize(stream, playerGroup);
		}
		if (instance.EntityPosition != null)
		{
			stream.WriteKey(51, 2);
			Packet_EntityPosition entityPosition = instance.EntityPosition;
			Packet_EntityPositionSerializer.GetSize(entityPosition);
			Packet_EntityPositionSerializer.SerializeWithSize(stream, entityPosition);
		}
		if (instance.HighlightBlocks != null)
		{
			stream.WriteKey(52, 2);
			Packet_HighlightBlocks highlightBlocks = instance.HighlightBlocks;
			Packet_HighlightBlocksSerializer.GetSize(highlightBlocks);
			Packet_HighlightBlocksSerializer.SerializeWithSize(stream, highlightBlocks);
		}
		if (instance.SelectedHotbarSlot != null)
		{
			stream.WriteKey(53, 2);
			Packet_SelectedHotbarSlot selectedHotbarSlot = instance.SelectedHotbarSlot;
			Packet_SelectedHotbarSlotSerializer.GetSize(selectedHotbarSlot);
			Packet_SelectedHotbarSlotSerializer.SerializeWithSize(stream, selectedHotbarSlot);
		}
		if (instance.CustomPacket != null)
		{
			stream.WriteKey(55, 2);
			Packet_CustomPacket customPacket = instance.CustomPacket;
			Packet_CustomPacketSerializer.GetSize(customPacket);
			Packet_CustomPacketSerializer.SerializeWithSize(stream, customPacket);
		}
		if (instance.NetworkChannels != null)
		{
			stream.WriteKey(56, 2);
			Packet_NetworkChannels networkChannels = instance.NetworkChannels;
			Packet_NetworkChannelsSerializer.GetSize(networkChannels);
			Packet_NetworkChannelsSerializer.SerializeWithSize(stream, networkChannels);
		}
		if (instance.GotoGroup != null)
		{
			stream.WriteKey(57, 2);
			Packet_GotoGroup gotoGroup = instance.GotoGroup;
			Packet_GotoGroupSerializer.GetSize(gotoGroup);
			Packet_GotoGroupSerializer.SerializeWithSize(stream, gotoGroup);
		}
		if (instance.ExchangeBlock != null)
		{
			stream.WriteKey(58, 2);
			Packet_ServerExchangeBlock exchangeBlock = instance.ExchangeBlock;
			Packet_ServerExchangeBlockSerializer.GetSize(exchangeBlock);
			Packet_ServerExchangeBlockSerializer.SerializeWithSize(stream, exchangeBlock);
		}
		if (instance.BulkEntityAttributes != null)
		{
			stream.WriteKey(59, 2);
			Packet_BulkEntityAttributes bulkEntityAttributes = instance.BulkEntityAttributes;
			Packet_BulkEntityAttributesSerializer.GetSize(bulkEntityAttributes);
			Packet_BulkEntityAttributesSerializer.SerializeWithSize(stream, bulkEntityAttributes);
		}
		if (instance.SpawnParticles != null)
		{
			stream.WriteKey(60, 2);
			Packet_SpawnParticles spawnParticles = instance.SpawnParticles;
			Packet_SpawnParticlesSerializer.GetSize(spawnParticles);
			Packet_SpawnParticlesSerializer.SerializeWithSize(stream, spawnParticles);
		}
		if (instance.BulkEntityDebugAttributes != null)
		{
			stream.WriteKey(61, 2);
			Packet_BulkEntityDebugAttributes bulkEntityDebugAttributes = instance.BulkEntityDebugAttributes;
			Packet_BulkEntityDebugAttributesSerializer.GetSize(bulkEntityDebugAttributes);
			Packet_BulkEntityDebugAttributesSerializer.SerializeWithSize(stream, bulkEntityDebugAttributes);
		}
		if (instance.SetBlocksNoRelight != null)
		{
			stream.WriteKey(62, 2);
			Packet_ServerSetBlocks setBlocksNoRelight = instance.SetBlocksNoRelight;
			Packet_ServerSetBlocksSerializer.GetSize(setBlocksNoRelight);
			Packet_ServerSetBlocksSerializer.SerializeWithSize(stream, setBlocksNoRelight);
		}
		if (instance.BlockDamage != null)
		{
			stream.WriteKey(64, 2);
			Packet_BlockDamage blockDamage = instance.BlockDamage;
			Packet_BlockDamageSerializer.GetSize(blockDamage);
			Packet_BlockDamageSerializer.SerializeWithSize(stream, blockDamage);
		}
		if (instance.Ambient != null)
		{
			stream.WriteKey(65, 2);
			Packet_Ambient ambient = instance.Ambient;
			Packet_AmbientSerializer.GetSize(ambient);
			Packet_AmbientSerializer.SerializeWithSize(stream, ambient);
		}
		if (instance.NotifySlot != null)
		{
			stream.WriteKey(66, 2);
			Packet_NotifySlot notifySlot = instance.NotifySlot;
			Packet_NotifySlotSerializer.GetSize(notifySlot);
			Packet_NotifySlotSerializer.SerializeWithSize(stream, notifySlot);
		}
		if (instance.IngameError != null)
		{
			stream.WriteKey(68, 2);
			Packet_IngameError ingameError = instance.IngameError;
			Packet_IngameErrorSerializer.GetSize(ingameError);
			Packet_IngameErrorSerializer.SerializeWithSize(stream, ingameError);
		}
		if (instance.IngameDiscovery != null)
		{
			stream.WriteKey(69, 2);
			Packet_IngameDiscovery ingameDiscovery = instance.IngameDiscovery;
			Packet_IngameDiscoverySerializer.GetSize(ingameDiscovery);
			Packet_IngameDiscoverySerializer.SerializeWithSize(stream, ingameDiscovery);
		}
		if (instance.SetBlocksMinimal != null)
		{
			stream.WriteKey(70, 2);
			Packet_ServerSetBlocks setBlocksMinimal = instance.SetBlocksMinimal;
			Packet_ServerSetBlocksSerializer.GetSize(setBlocksMinimal);
			Packet_ServerSetBlocksSerializer.SerializeWithSize(stream, setBlocksMinimal);
		}
		if (instance.SetDecors != null)
		{
			stream.WriteKey(71, 2);
			Packet_ServerSetDecors setDecors = instance.SetDecors;
			Packet_ServerSetDecorsSerializer.GetSize(setDecors);
			Packet_ServerSetDecorsSerializer.SerializeWithSize(stream, setDecors);
		}
		if (instance.RemoveBlockLight != null)
		{
			stream.WriteKey(72, 2);
			Packet_RemoveBlockLight removeBlockLight = instance.RemoveBlockLight;
			Packet_RemoveBlockLightSerializer.GetSize(removeBlockLight);
			Packet_RemoveBlockLightSerializer.SerializeWithSize(stream, removeBlockLight);
		}
		if (instance.ServerReady != null)
		{
			stream.WriteKey(73, 2);
			Packet_ServerReady serverReady = instance.ServerReady;
			Packet_ServerReadySerializer.GetSize(serverReady);
			Packet_ServerReadySerializer.SerializeWithSize(stream, serverReady);
		}
		if (instance.UnloadMapRegion != null)
		{
			stream.WriteKey(74, 2);
			Packet_UnloadMapRegion unloadMapRegion = instance.UnloadMapRegion;
			Packet_UnloadMapRegionSerializer.GetSize(unloadMapRegion);
			Packet_UnloadMapRegionSerializer.SerializeWithSize(stream, unloadMapRegion);
		}
		if (instance.LandClaims != null)
		{
			stream.WriteKey(75, 2);
			Packet_LandClaims landClaims = instance.LandClaims;
			Packet_LandClaimsSerializer.GetSize(landClaims);
			Packet_LandClaimsSerializer.SerializeWithSize(stream, landClaims);
		}
		if (instance.Roles != null)
		{
			stream.WriteKey(76, 2);
			Packet_Roles roles = instance.Roles;
			Packet_RolesSerializer.GetSize(roles);
			Packet_RolesSerializer.SerializeWithSize(stream, roles);
		}
		if (instance.UdpPacket != null)
		{
			stream.WriteKey(78, 2);
			Packet_UdpPacket udpPacket = instance.UdpPacket;
			Packet_UdpPacketSerializer.GetSize(udpPacket);
			Packet_UdpPacketSerializer.SerializeWithSize(stream, udpPacket);
		}
		if (instance.QueuePacket != null)
		{
			stream.WriteKey(79, 2);
			Packet_QueuePacket queuePacket = instance.QueuePacket;
			Packet_QueuePacketSerializer.GetSize(queuePacket);
			Packet_QueuePacketSerializer.SerializeWithSize(stream, queuePacket);
		}
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Server instance)
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

	public static byte[] SerializeToBytes(Packet_Server instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Server instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
