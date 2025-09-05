using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class TradeHandbookInfo : ModSystem
{
	private ICoreClientAPI capi;

	public override double ExecuteOrder()
	{
		return 0.15;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	private void Event_LevelFinalize()
	{
		foreach (EntityProperties entityType in capi.World.EntityTypes)
		{
			TradeProperties tradeProperties = null;
			string text = entityType.Attributes?["tradePropsFile"].AsString();
			AssetLocation assetLocation = null;
			JsonObject attributes = entityType.Attributes;
			if ((attributes != null && attributes["tradeProps"].Exists) || text != null)
			{
				try
				{
					assetLocation = ((text == null) ? null : AssetLocation.Create(text, entityType.Code.Domain));
					tradeProperties = ((!(assetLocation != null)) ? entityType.Attributes?["tradeProps"].AsObject<TradeProperties>(null, entityType.Code.Domain) : capi.Assets.Get(assetLocation.WithPathAppendixOnce(".json")).ToObject<TradeProperties>());
				}
				catch (Exception e)
				{
					capi.World.Logger.Error("Failed deserializing tradeProps attribute for entitiy {0}, exception logged to verbose debug", entityType.Code);
					capi.World.Logger.Error(e);
					capi.World.Logger.VerboseDebug("Failed deserializing TradeProperties:");
					capi.World.Logger.VerboseDebug("=================");
					capi.World.Logger.VerboseDebug("Tradeprops json:");
					if (assetLocation != null)
					{
						capi.World.Logger.VerboseDebug("File path {0}:", assetLocation);
					}
					capi.World.Logger.VerboseDebug("{0}", entityType.Server?.Attributes["tradeProps"].ToJsonToken());
				}
			}
			if (tradeProperties != null)
			{
				string traderName = Lang.Get(entityType.Code.Domain + ":item-creature-" + entityType.Code.Path);
				string title = Lang.Get("Sold by");
				TradeItem[] list = tradeProperties.Selling.List;
				foreach (TradeItem val in list)
				{
					AddTraderHandbookInfo(val, traderName, title);
				}
				title = Lang.Get("Purchased by");
				list = tradeProperties.Buying.List;
				foreach (TradeItem val2 in list)
				{
					AddTraderHandbookInfo(val2, traderName, title);
				}
			}
		}
		capi.Logger.VerboseDebug("Done traders handbook stuff");
	}

	private void AddTraderHandbookInfo(TradeItem val, string traderName, string title)
	{
		if (!val.Resolve(capi.World, "tradehandbookinfo " + traderName))
		{
			return;
		}
		CollectibleObject collectible = val.ResolvedItemstack.Collectible;
		if (collectible.Attributes == null)
		{
			collectible.Attributes = new JsonObject(JToken.Parse("{}"));
		}
		CollectibleBehaviorHandbookTextAndExtraInfo behavior = collectible.GetBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>();
		ExtraHandbookSection extraHandbookSection = behavior.ExtraHandBookSections?.FirstOrDefault((ExtraHandbookSection ele) => ele.Title == title);
		if (extraHandbookSection == null)
		{
			extraHandbookSection = new ExtraHandbookSection
			{
				Title = title,
				TextParts = Array.Empty<string>()
			};
			if (behavior.ExtraHandBookSections != null)
			{
				behavior.ExtraHandBookSections.Append(extraHandbookSection);
			}
			else
			{
				behavior.ExtraHandBookSections = new ExtraHandbookSection[1] { extraHandbookSection };
			}
		}
		if (!extraHandbookSection.TextParts.Contains<string>(traderName))
		{
			extraHandbookSection.TextParts = extraHandbookSection.TextParts.Append<string>(traderName);
		}
	}
}
