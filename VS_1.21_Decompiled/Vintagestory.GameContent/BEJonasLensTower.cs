using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BEJonasLensTower : BlockEntity
{
	private Dictionary<string, double> totalDaysCollectedByPlrUid = new Dictionary<string, double>();

	private double expireDays = 14.0;

	private ICoreClientAPI capi;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		totalDaysCollectedByPlrUid.Clear();
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("totalDaysCollectedByPlrUid");
		if (treeAttribute == null)
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			totalDaysCollectedByPlrUid[item.Key] = (item.Value as DoubleAttribute).value;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		TreeAttribute treeAttribute = (TreeAttribute)(tree["totalDaysCollectedByPlrUid"] = new TreeAttribute());
		foreach (KeyValuePair<string, double> item in totalDaysCollectedByPlrUid)
		{
			treeAttribute[item.Key] = new DoubleAttribute(item.Value);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (RecentlyCollectedBy(capi.World.Player))
		{
			return true;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	internal void OnInteract(IPlayer byPlayer)
	{
		if (Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().ErelAnnoyed)
		{
			if (!RecentlyCollectedBy(byPlayer) && Api.Side != EnumAppSide.Client)
			{
				totalDaysCollectedByPlrUid[byPlayer.PlayerUID] = Api.World.Calendar.TotalDays;
				MarkDirty(redrawOnClient: true);
				ItemStack itemstack = new ItemStack(Api.World.GetBlock("jonaslens-north"));
				if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(itemstack, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
				}
			}
		}
		else
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "cantpicklens", Lang.Get("devastation-cantpicklens"));
		}
	}

	internal bool RecentlyCollectedBy(IPlayer forPlayer)
	{
		if (totalDaysCollectedByPlrUid.TryGetValue(forPlayer.PlayerUID, out var value))
		{
			return Api.World.Calendar.TotalDays - value < expireDays;
		}
		return false;
	}
}
