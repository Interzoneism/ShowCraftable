using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemNPCHairStyling : ModSystem
{
	private GuiDialogHairStyling hairStylingDialog;

	private ICoreAPI Api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		Api = api;
		api.Network.RegisterChannel("hairstyling").RegisterMessageType<PacketHairStyle>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Network.GetChannel("hairstyling").SetMessageHandler<PacketHairStyle>(onPacketHairstyle);
	}

	private void onPacketHairstyle(IServerPlayer fromPlayer, PacketHairStyle packet)
	{
		if (!(Api.World.GetEntityById(packet.HairstylingNpcEntityId) is EntityTradingHumanoid entityTradingHumanoid) || !entityTradingHumanoid.interactingWithPlayer.Contains(fromPlayer.Entity))
		{
			return;
		}
		int cost = getCost(fromPlayer, packet.Hairstyle, GetPricesByCode(entityTradingHumanoid));
		if (InventoryTrader.GetPlayerAssets(fromPlayer.Entity) >= cost)
		{
			InventoryTrader.DeductFromEntity(Api, fromPlayer.Entity, cost);
			EntityBehaviorExtraSkinnable behavior = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
			foreach (KeyValuePair<string, string> item in packet.Hairstyle)
			{
				behavior.selectSkinPart(item.Key, item.Value, retesselateShape: false, playVoice: false);
			}
			fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
		}
		else
		{
			fromPlayer.SendIngameError("notenoughmoney", Lang.GetL(fromPlayer.LanguageCode, "Not enough money"));
		}
	}

	public int getCost(IServerPlayer player, Dictionary<string, string> hairstyle, Dictionary<string, int> costs)
	{
		int num = 0;
		EntityBehaviorExtraSkinnable behavior = player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (KeyValuePair<string, string> val in hairstyle)
		{
			AppliedSkinnablePartVariant appliedSkinnablePartVariant = behavior.AppliedSkinParts.FirstOrDefault((AppliedSkinnablePartVariant sp) => sp.PartCode == val.Key);
			if (hairstyle[val.Key] != appliedSkinnablePartVariant.Code)
			{
				num += costs[val.Key];
			}
		}
		return num;
	}

	public void handleHairstyling(EntityTradingHumanoid hairstylingNpc, EntityAgent triggeringEntity, string[] hairStylingCategories)
	{
		EntityPlayer eplr = triggeringEntity as EntityPlayer;
		if (hairstylingNpc.Alive && triggeringEntity.Pos.SquareDistanceTo(hairstylingNpc.Pos) <= 7f && !hairstylingNpc.interactingWithPlayer.Contains(eplr))
		{
			hairstylingNpc.interactingWithPlayer.Add(triggeringEntity as EntityPlayer);
			ICoreAPI api = Api;
			ICoreClientAPI capi = api as ICoreClientAPI;
			if (capi != null)
			{
				hairStylingDialog = new GuiDialogHairStyling(capi, hairstylingNpc.EntityId, hairStylingCategories, GetPricesByCode(triggeringEntity));
				hairStylingDialog.TryOpen();
				hairStylingDialog.OnClosed += delegate
				{
					hairstylingNpc.interactingWithPlayer.Remove(eplr);
					capi.Network.SendEntityPacket(hairstylingNpc.EntityId, 1212);
				};
			}
		}
		else
		{
			hairStylingDialog?.TryClose();
			hairstylingNpc.interactingWithPlayer.Remove(eplr);
			if (Api is ICoreServerAPI coreServerAPI)
			{
				coreServerAPI.Network.SendEntityPacket(eplr.Player as IServerPlayer, hairstylingNpc.EntityId, 1212);
			}
		}
	}

	public Dictionary<string, int> GetPricesByCode(Entity hairstylingNpc)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>
		{
			{ "hairbase", 2 },
			{ "hairextra", 2 },
			{ "mustache", 2 },
			{ "beard", 2 }
		};
		return hairstylingNpc?.Properties?.Attributes["hairstylingCosts"].AsObject(dictionary) ?? dictionary;
	}
}
