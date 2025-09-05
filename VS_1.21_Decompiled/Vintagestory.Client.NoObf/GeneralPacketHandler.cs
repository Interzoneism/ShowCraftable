using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GeneralPacketHandler : ClientSystem
{
	public override string Name => "gph";

	public GeneralPacketHandler(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[2] = HandlePing;
		game.PacketHandlers[3] = HandlePlayerPing;
		game.PacketHandlers[58] = HandleExchangeBlock;
		game.PacketHandlers[46] = HandleModeChange;
		game.PacketHandlers[45] = HandlePlayerDeath;
		game.PacketHandlers[8] = HandleChatLine;
		game.PacketHandlers[9] = HandleDisconnectPlayer;
		game.PacketHandlers[18] = HandleSound;
		game.PacketHandlers[29] = HandleServerRedirect;
		game.PacketHandlers[41] = HandlePlayerData;
		game.PacketHandlers[30] = HandleInventoryContents;
		game.PacketHandlers[31] = HandleInventoryUpdate;
		game.PacketHandlers[32] = HandleInventoryDoubleUpdate;
		game.PacketHandlers[66] = HandleNotifyItemSlot;
		game.PacketHandlers[7] = HandleSetBlock;
		game.PacketHandlers[48] = HandleBlockEntities;
		game.PacketHandlers[44] = HandleBlockEntityMessage;
		game.PacketHandlers[51] = HandleSpawnPosition;
		game.PacketHandlers[53] = HandleSelectedHotbarSlot;
		game.PacketHandlers[59] = HandleStopMovement;
		game.PacketHandlers[61] = HandleSpawnParticles;
		game.PacketHandlers[64] = HandleBlockDamage;
		game.PacketHandlers[65] = HandleAmbient;
		game.PacketHandlers[68] = HandleIngameError;
		game.PacketHandlers[69] = HandleIngameDiscovery;
		game.PacketHandlers[72] = RemoveBlockLight;
		game.PacketHandlers[75] = HandleLandClaims;
		game.PacketHandlers[76] = HandleRoles;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	private void HandlePing(Packet_Server packet)
	{
		game.SendPingReply();
		game.ServerInfo.ServerPing.OnSend(game.Platform.EllapsedMs);
	}

	private void HandlePlayerPing(Packet_Server packet)
	{
		game.ServerInfo.ServerPing.OnReceive(game.Platform.EllapsedMs);
		Packet_ServerPlayerPing playerPing = packet.PlayerPing;
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		for (int i = 0; i < packet.PlayerPing.ClientIdsCount; i++)
		{
			int key = playerPing.ClientIds[i];
			dictionary[key] = (float)playerPing.Pings[i] / 1000f;
		}
		foreach (KeyValuePair<string, ClientPlayer> item in game.PlayersByUid)
		{
			if (dictionary.TryGetValue(item.Value.ClientId, out var value))
			{
				item.Value.Ping = value;
			}
		}
	}

	private void HandleSetBlock(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.SetBlock.X, packet.SetBlock.Y, packet.SetBlock.Z);
		int blockType = packet.SetBlock.BlockType;
		if (blockType < 0)
		{
			int blockId = game.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2).BlockId;
			blockType = -(blockType + 1);
			if (blockType != blockId)
			{
				game.WorldMap.RelaxedBlockAccess.SetBlock(blockType, pos, 2);
			}
		}
		else
		{
			int blockId2 = game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
			if (blockType != blockId2)
			{
				game.WorldMap.RelaxedBlockAccess.SetBlock(blockType, pos);
				game.eventManager?.TriggerBlockChanged(game, pos, game.WorldMap.Blocks[blockId2]);
			}
		}
	}

	private void HandleExchangeBlock(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.ExchangeBlock.X, packet.ExchangeBlock.Y, packet.ExchangeBlock.Z);
		int blockId = game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
		int blockType = packet.ExchangeBlock.BlockType;
		game.WorldMap.RelaxedBlockAccess.ExchangeBlock(blockType, pos);
		game.eventManager?.TriggerBlockChanged(game, pos, game.WorldMap.Blocks[blockId]);
	}

	private void HandleModeChange(Packet_Server packet)
	{
		game.PlayersByUid.TryGetValue(packet.ModeChange.PlayerUID, out var value);
		value?.UpdateFromPacket(game, packet.ModeChange);
	}

	private void HandlePlayerDeath(Packet_Server packet)
	{
		if (game.EntityPlayer != null)
		{
			game.EntityPlayer.TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Death);
			game.eventManager?.TriggerPlayerDeath(packet.PlayerDeath.ClientId, packet.PlayerDeath.LivesLeft);
		}
	}

	private void HandleChatLine(Packet_Server packet)
	{
		game.eventManager?.TriggerNewServerChatLine(packet.Chatline.Groupid, packet.Chatline.Message, (EnumChatType)packet.Chatline.ChatType, packet.Chatline.Data);
		game.Logger.Chat("{0} @ {1}", packet.Chatline.Message, packet.Chatline.Groupid);
	}

	private void HandleDisconnectPlayer(Packet_Server packet)
	{
		game.Logger.Notification("Disconnected by the server ({0})", packet.DisconnectPlayer.DisconnectReason);
		string text = packet.DisconnectPlayer.DisconnectReason;
		game.exitReason = "exit command by server";
		if ((text != null && text.Contains("Bad game session")) || text == Lang.Get("Bad game session, try relogging"))
		{
			text += "\n\nThis error can be caused when trying to connect to a server on version 1.18.3 or older. Please ask the server owner to update.";
		}
		game.disconnectReason = text;
		game.DestroyGameSession(gotDisconnected: true);
	}

	private void HandleSound(Packet_Server packet)
	{
		game.PlaySoundAt(new AssetLocation(packet.Sound.Name), CollectibleNet.DeserializeFloat(packet.Sound.X), CollectibleNet.DeserializeFloat(packet.Sound.Y), CollectibleNet.DeserializeFloat(packet.Sound.Z), null, (EnumSoundType)packet.Sound.SoundType, CollectibleNet.DeserializeFloatPrecise(packet.Sound.Pitch), CollectibleNet.DeserializeFloat(packet.Sound.Range), CollectibleNet.DeserializeFloatPrecise(packet.Sound.Volume));
	}

	private void HandleServerRedirect(Packet_Server packet)
	{
		game.Logger.Notification("Received server redirect");
		game.SendLeave(0);
		game.ExitAndSwitchServer(new MultiplayerServerEntry
		{
			host = packet.Redirect.Host,
			name = packet.Redirect.Name
		});
		game.Logger.VerboseDebug("Received server redirect packet");
	}

	private void HandlePlayerData(Packet_Server packet)
	{
		if (!game.BlocksReceivedAndLoaded)
		{
			game.Logger.VerboseDebug("Startup sequence wrong, playerdata packet handled before BlocksReceivedAndLoaded; player may be null");
			return;
		}
		string playerUID = packet.PlayerData.PlayerUID;
		if (packet.PlayerData.ClientId <= -99)
		{
			game.Logger.VerboseDebug("Received player data deletion for playeruid " + playerUID);
			if (game.PlayersByUid.TryGetValue(playerUID, out var value))
			{
				game.api.eventapi.TriggerPlayerEntityDespawn(value);
				value.worlddata.EntityPlayer = null;
				game.api.eventapi.TriggerPlayerLeave(value);
				game.PlayersByUid.Remove(playerUID);
			}
			return;
		}
		game.Logger.VerboseDebug("Received player data for playeruid " + playerUID);
		ClientPlayer value2;
		bool flag = !game.PlayersByUid.TryGetValue(playerUID, out value2);
		if (flag)
		{
			value2 = (game.PlayersByUid[playerUID] = new ClientPlayer(game));
		}
		else
		{
			value2.WarnIfEntityChanged(packet.PlayerData.EntityId, "playerData");
		}
		value2.UpdateFromPacket(game, packet.PlayerData);
		if (ClientSettings.PlayerUID == playerUID && !game.Spawned)
		{
			game.player = value2;
			game.mouseYaw = game.EntityPlayer.SidedPos.Yaw;
			game.mousePitch = game.EntityPlayer.SidedPos.Pitch;
			game.Logger.VerboseDebug("Informing clientsystems playerdata received");
			game.OnOwnPlayerDataReceived();
			game.Spawned = true;
			game.SendPacketClient(new Packet_Client
			{
				Id = 26
			});
		}
		if (packet.PlayerData.Privileges != null)
		{
			string[] privileges = packet.PlayerData.Privileges;
			int privilegesCount = packet.PlayerData.PrivilegesCount;
			string[] array = (value2.Privileges = new string[privilegesCount]);
			for (int i = 0; i < privilegesCount; i++)
			{
				array[i] = privileges[i];
			}
		}
		if (packet.PlayerData.RoleCode != null)
		{
			value2.RoleCode = packet.PlayerData.RoleCode;
		}
		if (flag)
		{
			game.api.eventapi.TriggerPlayerJoin(value2);
			if (value2.Entity != null)
			{
				game.api.eventapi.TriggerPlayerEntitySpawn(value2);
			}
		}
		if (ClientSettings.PlayerUID == playerUID)
		{
			game.eventManager?.TriggerPlayerModeChange();
			if (game.player.worlddata.CurrentGameMode != EnumGameMode.Creative)
			{
				ClientSettings.RenderMetaBlocks = false;
			}
			if (game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
			{
				game.MainCamera.SetMode(EnumCameraMode.FirstPerson);
			}
			if (!game.clientPlayingFired && game.api.eventapi.TriggerIsPlayerReady())
			{
				game.clientPlayingFired = true;
				game.SendPacketClient(new Packet_Client
				{
					Id = 29
				});
			}
		}
		game.Logger.VerboseDebug("Done handling playerdata packet");
	}

	private void HandleInventoryContents(Packet_Server packet)
	{
		string inventoryId = packet.InventoryContents.InventoryId;
		game.Logger.VerboseDebug("Received inventory contents " + inventoryId);
		ClientPlayer playerFromClientId = game.GetPlayerFromClientId(packet.InventoryContents.ClientId);
		if (playerFromClientId == null)
		{
			game.Logger.Error("Server sent me inventory contents for a player that i don't have? Ignoring. Clientid was " + packet.InventoryContents.ClientId);
			return;
		}
		if (!playerFromClientId.inventoryMgr.Inventories.ContainsKey(inventoryId))
		{
			if (!ClientMain.ClassRegistry.inventoryClassToTypeMapping.ContainsKey(packet.InventoryContents.InventoryClass))
			{
				game.Logger.Error("Server sent me inventory contents from with an inventory class name '{0}' - no idea how to instantiate that. Ignoring.", packet.InventoryContents.InventoryClass);
				return;
			}
			playerFromClientId.inventoryMgr.Inventories[inventoryId] = ClientMain.ClassRegistry.CreateInventory(packet.InventoryContents.InventoryClass, packet.InventoryContents.InventoryId, game.api);
			playerFromClientId.inventoryMgr.Inventories[inventoryId].AfterBlocksLoaded(game);
		}
		(playerFromClientId.inventoryMgr.Inventories[inventoryId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet.InventoryContents);
	}

	private void HandleInventoryUpdate(Packet_Server packet)
	{
		string inventoryId = packet.InventoryUpdate.InventoryId;
		ClientPlayer playerFromClientId = game.GetPlayerFromClientId(packet.InventoryUpdate.ClientId);
		if (playerFromClientId != null && playerFromClientId.inventoryMgr.Inventories.ContainsKey(inventoryId))
		{
			(playerFromClientId.inventoryMgr.Inventories[inventoryId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet.InventoryUpdate);
		}
	}

	private void HandleNotifyItemSlot(Packet_Server packet)
	{
		string inventoryId = packet.NotifySlot.InventoryId;
		if (game.player?.inventoryMgr?.Inventories != null && game.player.inventoryMgr.Inventories.ContainsKey(inventoryId))
		{
			game.player.inventoryMgr.Inventories[inventoryId]?.PerformNotifySlot(packet.NotifySlot.SlotId);
		}
	}

	private void HandleInventoryDoubleUpdate(Packet_Server packet)
	{
		if (packet?.InventoryDoubleUpdate == null)
		{
			game.Logger.Warning("Received inventory double update with packet set to null?");
			return;
		}
		string inventoryId = packet.InventoryDoubleUpdate.InventoryId1;
		string inventoryId2 = packet.InventoryDoubleUpdate.InventoryId2;
		ClientPlayerInventoryManager clientPlayerInventoryManager = game.GetPlayerFromClientId(packet.InventoryDoubleUpdate.ClientId)?.inventoryMgr;
		if (clientPlayerInventoryManager == null)
		{
			game.Logger.Warning("Received inventory double update for a client whose inventory i dont have? for clientid " + packet?.InventoryContents?.ClientId);
			return;
		}
		if (clientPlayerInventoryManager.GetInventory(inventoryId, out var invFound))
		{
			(invFound.InvNetworkUtil as InventoryNetworkUtil)?.UpdateFromPacket(game, packet.InventoryDoubleUpdate);
		}
		if (inventoryId != inventoryId2 && clientPlayerInventoryManager.GetInventory(inventoryId, out var invFound2))
		{
			(invFound2.InvNetworkUtil as InventoryNetworkUtil)?.UpdateFromPacket(game, packet.InventoryDoubleUpdate);
		}
	}

	private void HandleBlockEntities(Packet_Server packet)
	{
		Packet_BlockEntity[] blockEntitites = packet.BlockEntities.BlockEntitites;
		for (int i = 0; i < packet.BlockEntities.BlockEntititesCount; i++)
		{
			Packet_BlockEntity packet_BlockEntity = blockEntitites[i];
			ClientChunk chunkAtBlockPos = game.WorldMap.GetChunkAtBlockPos(packet_BlockEntity.PosX, packet_BlockEntity.PosY, packet_BlockEntity.PosZ);
			if (chunkAtBlockPos != null)
			{
				chunkAtBlockPos.AddOrUpdateBlockEntityFromPacket(packet_BlockEntity, game);
				BlockPos pos = new BlockPos(packet_BlockEntity.PosX, packet_BlockEntity.PosY, packet_BlockEntity.PosZ);
				game.eventManager?.TriggerBlockChanged(game, pos, game.BlockAccessor.GetBlock(pos));
			}
		}
	}

	private void HandleBlockEntityMessage(Packet_Server packet)
	{
		Packet_BlockEntityMessage blockEntityMessage = packet.BlockEntityMessage;
		game.WorldMap.GetBlockEntity(new BlockPos(blockEntityMessage.X, blockEntityMessage.Y, blockEntityMessage.Z))?.OnReceivedServerPacket(blockEntityMessage.PacketId, blockEntityMessage.Data);
	}

	private void HandleSpawnPosition(Packet_Server packet)
	{
		EntityPos spawnPosition = ClientSystemEntities.entityPosFromPacket(packet.EntityPosition);
		game.SpawnPosition = spawnPosition;
	}

	private void HandleSelectedHotbarSlot(Packet_Server packet)
	{
		int clientId = packet.SelectedHotbarSlot.ClientId;
		try
		{
			foreach (ClientPlayer value in game.PlayersByUid.Values)
			{
				if (value.ClientId == clientId)
				{
					Packet_SelectedHotbarSlot selectedHotbarSlot = packet.SelectedHotbarSlot;
					value.inventoryMgr.SetActiveHotbarSlotNumberFromServer(selectedHotbarSlot.SlotNumber);
					ItemStack itemstack = null;
					if (selectedHotbarSlot.Itemstack != null && selectedHotbarSlot.Itemstack.ItemClass != -1 && selectedHotbarSlot.Itemstack.ItemId != 0)
					{
						itemstack = StackConverter.FromPacket(selectedHotbarSlot.Itemstack, game);
					}
					ItemStack itemstack2 = null;
					if (selectedHotbarSlot.OffhandStack != null && selectedHotbarSlot.OffhandStack.ItemClass != -1 && selectedHotbarSlot.OffhandStack.ItemId != 0)
					{
						itemstack2 = StackConverter.FromPacket(selectedHotbarSlot.OffhandStack, game);
					}
					value.inventoryMgr.ActiveHotbarSlot.Itemstack = itemstack;
					if (value.Entity?.LeftHandItemSlot != null)
					{
						value.Entity.LeftHandItemSlot.Itemstack = itemstack2;
					}
					break;
				}
			}
		}
		catch (Exception e)
		{
			string text = "Handling server packet HandleSelectedHotbarSlot threw an exception while trying to update the slot of clientid " + clientId + " with itemstack " + packet.SelectedHotbarSlot.Itemstack;
			text += "Exception thrown: ";
			game.Logger.Fatal(text);
			game.Logger.Fatal(e);
		}
	}

	private void HandleStopMovement(Packet_Server packet)
	{
		if (game.EntityPlayer?.Controls != null)
		{
			game.EntityPlayer.Controls.StopAllMovement();
		}
	}

	private void HandleSpawnParticles(Packet_Server packet)
	{
		Packet_SpawnParticles spawnParticles = packet.SpawnParticles;
		IParticlePropertiesProvider particlePropertiesProvider = ClientMain.ClassRegistry.CreateParticlePropertyProvider(spawnParticles.ParticlePropertyProviderClassName);
		using (MemoryStream input = new MemoryStream(spawnParticles.Data))
		{
			BinaryReader reader = new BinaryReader(input);
			particlePropertiesProvider.FromBytes(reader, game);
		}
		game.SpawnParticles(particlePropertiesProvider);
	}

	private void HandleBlockDamage(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.BlockDamage.PosX, packet.BlockDamage.PosY, packet.BlockDamage.PosZ);
		game.WorldMap.DamageBlock(pos, BlockFacing.ALLFACES[packet.BlockDamage.Facing], CollectibleNet.DeserializeFloat(packet.BlockDamage.Damage));
	}

	private void HandleAmbient(Packet_Server packet)
	{
		using MemoryStream input = new MemoryStream(packet.Ambient.Data);
		AmbientModifier ambientModifier = new AmbientModifier().EnsurePopulated();
		ambientModifier.FromBytes(new BinaryReader(input));
		game.AmbientManager.CurrentModifiers["serverambient"] = ambientModifier.EnsurePopulated();
	}

	private void HandleIngameError(Packet_Server packet)
	{
		Packet_IngameError ingameError = packet.IngameError;
		string text = ingameError.Message;
		if (text == null)
		{
			if (ingameError.LangParams == null)
			{
				text = Lang.Get("ingameerror-" + ingameError.Code);
			}
			else
			{
				string key = "ingameerror-" + ingameError.Code;
				object[] langParams = ingameError.LangParams;
				text = Lang.Get(key, langParams);
			}
		}
		game.eventManager?.TriggerIngameError(this, ingameError.Code, text);
	}

	private void HandleIngameDiscovery(Packet_Server packet)
	{
		Packet_IngameDiscovery ingameDiscovery = packet.IngameDiscovery;
		string text = ingameDiscovery.Message;
		if (text == null)
		{
			string key = "ingamediscovery-" + ingameDiscovery.Code;
			object[] langParams = ingameDiscovery.LangParams;
			text = Lang.Get(key, langParams);
		}
		game.eventManager?.TriggerIngameDiscovery(this, ingameDiscovery.Code, text);
	}

	private void RemoveBlockLight(Packet_Server packet)
	{
		Packet_RemoveBlockLight removeBlockLight = packet.RemoveBlockLight;
		game.BlockAccessor.RemoveBlockLight(new byte[3]
		{
			(byte)removeBlockLight.LightH,
			(byte)removeBlockLight.LightS,
			(byte)removeBlockLight.LightV
		}, new BlockPos(removeBlockLight.PosX, removeBlockLight.PosY, removeBlockLight.PosZ));
	}

	private void HandleLandClaims(Packet_Server packet)
	{
		Packet_LandClaims landClaims = packet.LandClaims;
		if (landClaims.Allclaims != null && landClaims.Allclaims.Length != 0)
		{
			game.WorldMap.LandClaims = (from claim in landClaims.Allclaims
				where claim != null
				select SerializerUtil.Deserialize<LandClaim>(claim.Data)).ToList();
		}
		else if (landClaims.Addclaims != null)
		{
			game.WorldMap.LandClaims.AddRange(from claim in landClaims.Addclaims
				where claim != null
				select SerializerUtil.Deserialize<LandClaim>(claim.Data));
		}
		game.WorldMap.RebuildLandClaimPartitions();
	}

	private void HandleRoles(Packet_Server packet)
	{
		Packet_Roles roles = packet.Roles;
		game.WorldMap.RolesByCode = new Dictionary<string, PlayerRole>();
		for (int i = 0; i < roles.RolesCount; i++)
		{
			Packet_Role packet_Role = roles.Roles[i];
			game.WorldMap.RolesByCode[packet_Role.Code] = new PlayerRole
			{
				Code = packet_Role.Code,
				PrivilegeLevel = packet_Role.PrivilegeLevel
			};
		}
	}
}
