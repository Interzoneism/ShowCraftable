using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemEntityOwnership : ModSystem
{
	public Dictionary<string, Dictionary<string, EntityOwnership>> OwnerShipsByPlayerUid;

	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	private IServerNetworkChannel serverNetworkChannel;

	public Dictionary<string, EntityOwnership> SelfOwnerShips { get; set; } = new Dictionary<string, EntityOwnership>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("entityownership").RegisterMessageType<EntityOwnershipPacket>();
		api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<OwnedEntityMapLayer>("ownedcreatures", 2.0);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.OnEntityDeath += Event_EntityDeath;
		serverNetworkChannel = sapi.Network.GetChannel("entityownership");
		AiTaskRegistry.Register<AiTaskComeToOwner>("cometoowner");
	}

	private void Event_PlayerJoin(IServerPlayer player)
	{
		sendOwnerShips(player);
	}

	private void sendOwnerShips(IServerPlayer player)
	{
		if (OwnerShipsByPlayerUid.TryGetValue(player.PlayerUID, out var value))
		{
			serverNetworkChannel.SendPacket(new EntityOwnershipPacket
			{
				OwnerShipByGroup = value
			}, player);
		}
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("entityownership", OwnerShipsByPlayerUid);
	}

	private void Event_SaveGameLoaded()
	{
		OwnerShipsByPlayerUid = sapi.WorldManager.SaveGame.GetData("entityownership", new Dictionary<string, Dictionary<string, EntityOwnership>>());
	}

	private void Event_EntityDeath(Entity entity, DamageSource damageSource)
	{
		RemoveOwnership(entity);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("entityownership").SetMessageHandler<EntityOwnershipPacket>(onPacket);
	}

	private void onPacket(EntityOwnershipPacket packet)
	{
		SelfOwnerShips = packet.OwnerShipByGroup ?? new Dictionary<string, EntityOwnership>();
		WorldMapManager modSystem = capi.ModLoader.GetModSystem<WorldMapManager>();
		if (modSystem != null && modSystem.worldMapDlg != null && modSystem.worldMapDlg.IsOpened())
		{
			(modSystem.worldMapDlg.MapLayers.FirstOrDefault((MapLayer ml) => ml is OwnedEntityMapLayer) as OwnedEntityMapLayer)?.Reload();
		}
	}

	public void ClaimOwnership(Entity toEntity, EntityAgent byEntity)
	{
		if (sapi == null)
		{
			return;
		}
		string text = toEntity.GetBehavior<EntityBehaviorOwnable>()?.Group;
		if (text != null)
		{
			IServerPlayer serverPlayer = (byEntity as EntityPlayer).Player as IServerPlayer;
			OwnerShipsByPlayerUid.TryGetValue(serverPlayer.PlayerUID, out var value);
			if (value == null)
			{
				value = (OwnerShipsByPlayerUid[serverPlayer.PlayerUID] = new Dictionary<string, EntityOwnership>());
			}
			if (value.TryGetValue(text, out var value2))
			{
				sapi.World.GetEntityById(value2.EntityId)?.WatchedAttributes.RemoveAttribute("ownedby");
			}
			value[text] = new EntityOwnership
			{
				EntityId = toEntity.EntityId,
				Pos = toEntity.ServerPos,
				Name = toEntity.GetName(),
				Color = "#0e9d51"
			};
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetString("uid", serverPlayer.PlayerUID);
			treeAttribute.SetString("name", serverPlayer.PlayerName);
			toEntity.WatchedAttributes["ownedby"] = treeAttribute;
			toEntity.WatchedAttributes.MarkPathDirty("ownedby");
			sendOwnerShips(serverPlayer);
		}
	}

	public void RemoveOwnership(Entity fromEntity)
	{
		ITreeAttribute treeAttribute = fromEntity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (treeAttribute == null)
		{
			return;
		}
		string text = treeAttribute.GetString("uid");
		string key = fromEntity.GetBehavior<EntityBehaviorOwnable>().Group;
		if (OwnerShipsByPlayerUid.TryGetValue(text, out var value) && value != null && value.TryGetValue(key, out var value2) && value2?.EntityId == fromEntity.EntityId)
		{
			value.Remove(key);
			IPlayer player = sapi.World.PlayerByUid(text);
			if (player != null)
			{
				sendOwnerShips(player as IServerPlayer);
			}
			fromEntity.WatchedAttributes.RemoveAttribute("ownedby");
		}
	}
}
