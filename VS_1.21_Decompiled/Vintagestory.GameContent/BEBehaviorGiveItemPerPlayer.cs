using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BEBehaviorGiveItemPerPlayer : BlockEntityBehavior
{
	public Dictionary<string, double> retrievedTotalDaysByPlayerUid = new Dictionary<string, double>();

	private double resetDays;

	private bool selfRetrieved;

	public BEBehaviorGiveItemPerPlayer(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		resetDays = base.Block.Attributes?["resetAfterDays"].AsDouble(-1.0) ?? (-1.0);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("retrievedTotalDaysByPlayerUid");
		if (treeAttribute != null)
		{
			foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
			{
				retrievedTotalDaysByPlayerUid[item.Key] = (item.Value as DoubleAttribute).value;
			}
		}
		if (Api is ICoreClientAPI coreClientAPI)
		{
			selfRetrieved = false;
			if (retrievedTotalDaysByPlayerUid.TryGetValue(coreClientAPI.World.Player.PlayerUID, out var value))
			{
				selfRetrieved = resetDays < 0.0 || Api.World.Calendar.TotalDays - value < resetDays;
			}
		}
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		foreach (KeyValuePair<string, double> item in retrievedTotalDaysByPlayerUid)
		{
			treeAttribute.SetDouble(item.Key, item.Value);
		}
		tree["retrievedTotalDaysByPlayerUid"] = treeAttribute;
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (selfRetrieved)
		{
			ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
			CompositeShape compositeShape = base.Block.Attributes["lootedShape"].AsObject<CompositeShape>();
			if (compositeShape != null)
			{
				ITexPositionSource textureSource = coreClientAPI.Tesselator.GetTextureSource(base.Block);
				coreClientAPI.Tesselator.TesselateShape("lootedShape", compositeShape.Base, compositeShape, out var modeldata, textureSource, 0, 0, 0);
				mesher.AddMeshData(modeldata);
				return true;
			}
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	public void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer == null || Api.Side != EnumAppSide.Server || (retrievedTotalDaysByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var value) && (resetDays < 0.0 || Api.World.Calendar.TotalDays - value < resetDays)))
		{
			return;
		}
		retrievedTotalDaysByPlayerUid[byPlayer.PlayerUID] = Api.World.Calendar.TotalDays;
		JsonItemStack jsonItemStack = base.Block.Attributes["giveItem"].AsObject<JsonItemStack>();
		if (jsonItemStack == null)
		{
			Api.Logger.Warning(string.Concat("Block code ", base.Block.Code, " attribute giveItem has GiveItemPerPlayer behavior but no giveItem defined"));
		}
		else if (jsonItemStack.Resolve(Api.World, string.Concat("Block code ", base.Block.Code, " attribute giveItem")))
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(jsonItemStack.ResolvedItemstack))
			{
				Api.World.SpawnItemEntity(jsonItemStack.ResolvedItemstack, base.Pos);
			}
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}
}
