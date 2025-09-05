using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ItemLocatorMap : Item, ITradeableCollectible
{
	private ModSystemStructureLocator strucLocSys;

	private GenStoryStructures storyStructures;

	private LocatorProps props;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		strucLocSys = api.ModLoader.GetModSystem<ModSystemStructureLocator>();
		storyStructures = api.ModLoader.GetModSystem<GenStoryStructures>();
		props = Attributes["locatorProps"].AsObject<LocatorProps>();
	}

	public bool OnDidTrade(EntityTradingHumanoid trader, ItemStack stack, EnumTradeDirection tradeDir)
	{
		StructureLocation structureLocation = strucLocSys.FindFreshStructureLocation(props.SchematicCode, trader.SidedPos.AsBlockPos, 350);
		stack.Attributes.SetVec3i("position", structureLocation.Position);
		stack.Attributes.SetInt("regionX", structureLocation.RegionX);
		stack.Attributes.SetInt("regionZ", structureLocation.RegionZ);
		strucLocSys.ConsumeStructureLocation(structureLocation);
		return true;
	}

	public EnumTransactionResult OnTryTrade(EntityTradingHumanoid eTrader, ItemSlot tradeSlot, EnumTradeDirection tradeDir)
	{
		if (tradeSlot is ItemSlotTrade itemSlotTrade && strucLocSys.FindFreshStructureLocation(props.SchematicCode, eTrader.SidedPos.AsBlockPos, 350) == null)
		{
			itemSlotTrade.TradeItem.Stock = 0;
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		return EnumTransactionResult.Success;
	}

	public bool ShouldTrade(EntityTradingHumanoid trader, TradeItem tradeIdem, EnumTradeDirection tradeDir)
	{
		return strucLocSys.FindFreshStructureLocation(props.SchematicCode, trader.SidedPos.AsBlockPos, 350) != null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (slot.Empty)
		{
			return;
		}
		handling = EnumHandHandling.Handled;
		if (!((byEntity as EntityPlayer).Player is IServerPlayer serverPlayer))
		{
			return;
		}
		WaypointMapLayer waypointMapLayer = api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
		ITreeAttribute attributes = slot.Itemstack.Attributes;
		Vec3d pos = null;
		if (attributes.HasAttribute("structureIndex") || attributes.HasAttribute("positionX"))
		{
			pos = getStructureCenter(attributes);
		}
		if (pos == null)
		{
			foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructures.storyStructureInstances)
			{
				if (!(storyStructureInstance.Key != props.SchematicCode))
				{
					pos = storyStructureInstance.Value.CenterPos.ToVec3d().Add(0.5, 0.5, 0.5);
					break;
				}
			}
		}
		if (pos == null)
		{
			serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("No location found on this map"), EnumChatType.Notification);
			return;
		}
		if (props.Offset != null)
		{
			pos.Add(props.Offset);
		}
		if (!attributes.HasAttribute("randomX"))
		{
			Random random = new Random(api.World.Seed + Code.GetHashCode());
			attributes.SetFloat("randomX", (float)random.NextDouble() * props.RandomX * 2f - props.RandomX);
			attributes.SetFloat("randomZ", (float)random.NextDouble() * props.RandomZ * 2f - props.RandomZ);
		}
		pos.X += attributes.GetFloat("randomX");
		pos.Z += attributes.GetFloat("randomZ");
		if (!byEntity.World.Config.GetBool("allowMap", defaultValue: true) || waypointMapLayer == null)
		{
			Vec3d vec3d = pos.Sub(byEntity.Pos.XYZ);
			vec3d.Y = 0.0;
			serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("{0} blocks distance", (int)vec3d.Length()), EnumChatType.Notification);
			return;
		}
		string puid = (byEntity as EntityPlayer).PlayerUID;
		if (waypointMapLayer.Waypoints.Where((Waypoint wp) => wp.OwningPlayerUid == puid).FirstOrDefault((Waypoint wp) => wp.Position == pos) != null)
		{
			serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("Location already marked on your map"), EnumChatType.Notification);
			return;
		}
		waypointMapLayer.AddWaypoint(new Waypoint
		{
			Color = ColorUtil.ColorFromRgba((int)(props.WaypointColor[0] * 255.0), (int)(props.WaypointColor[1] * 255.0), (int)(props.WaypointColor[2] * 255.0), (int)(props.WaypointColor[3] * 255.0)),
			Icon = props.WaypointIcon,
			Pinned = true,
			Position = pos,
			OwningPlayerUid = puid,
			Title = Lang.Get(props.WaypointText)
		}, serverPlayer);
		string message = (attributes.HasAttribute("randomX") ? Lang.Get("Approximate location of {0} added to your world map", Lang.Get(props.WaypointText)) : Lang.Get("Location of {0} added to your world map", Lang.Get(props.WaypointText)));
		serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
	}

	private Vec3d getStructureCenter(ITreeAttribute attr)
	{
		GeneratedStructure structure = strucLocSys.GetStructure(new StructureLocation
		{
			StructureIndex = attr.GetInt("structureIndex", -1),
			Position = attr.GetVec3i("position"),
			RegionX = attr.GetInt("regionX"),
			RegionZ = attr.GetInt("regionZ")
		});
		if (structure == null)
		{
			return null;
		}
		Vec3i center = structure.Location.Center;
		return new Vec3d((double)center.X + 0.5, (double)center.Y + 0.5, (double)center.Z + 0.5);
	}
}
