using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BELantern : BlockEntity
{
	public string material = "copper";

	public string lining = "plain";

	public string glass = "quartz";

	public float MeshAngle;

	private byte[] origlightHsv = new byte[3] { 7, 4, 18 };

	private byte[] lightHsv = new byte[3] { 7, 4, 18 };

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		origlightHsv = base.Block.LightHsv;
		lightHsv = base.Block.LightHsv;
		setLightColor(origlightHsv, lightHsv, glass);
	}

	public void DidPlace(string material, string lining, string glass)
	{
		this.lining = lining;
		this.material = material;
		this.glass = glass;
		if (glass == null || glass.Length == 0)
		{
			this.glass = "quartz";
		}
		setLightColor(origlightHsv, lightHsv, glass);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		Api.World.BlockAccessor.RemoveBlockLight(lightHsv, Pos);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		material = tree.GetString("material", "copper");
		lining = tree.GetString("lining", "plain");
		glass = tree.GetString("glass", "quartz");
		MeshAngle = tree.GetFloat("meshAngle");
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	internal byte[] GetLightHsv()
	{
		lightHsv[2] = ((lining != "plain") ? ((byte)(origlightHsv[2] + 3)) : origlightHsv[2]);
		return lightHsv;
	}

	private MeshData getMesh(ITesselatorAPI tesselator)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, "blockLanternBlockMeshes", () => new Dictionary<string, MeshData>());
		if (!(Api.World.BlockAccessor.GetBlock(Pos) is BlockLantern blockLantern))
		{
			return null;
		}
		string text = blockLantern.LastCodePart();
		if (orCreate.TryGetValue(material + "-" + lining + "-" + text + "-" + glass, out var value))
		{
			return value;
		}
		return orCreate[material + "-" + lining + "-" + text + "-" + glass] = blockLantern.GenMesh(Api as ICoreClientAPI, material, lining, glass, null, tesselator);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("material", material);
		tree.SetString("lining", lining);
		tree.SetString("glass", glass);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		MeshData meshData = getMesh(tesselator);
		if (meshData == null)
		{
			return false;
		}
		string text = base.Block.LastCodePart();
		if (text == "up" || text == "down")
		{
			meshData = meshData.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
		}
		mesher.AddMeshData(meshData);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (lining == "plain")
		{
			sb.AppendLine(Lang.Get("lantern-materialwithpanels", Lang.Get("material-" + material), Lang.Get("block-glass-" + glass)));
		}
		else
		{
			sb.AppendLine(Lang.Get("lantern-materialwithliningandpanels", Lang.Get("material-" + material), Lang.Get("material-" + lining), Lang.Get("block-glass-" + glass)));
		}
	}

	internal bool Interact(IPlayer byPlayer)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return false;
		}
		CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
		if (collectible.FirstCodePart() == "glass" && collectible.Variant.ContainsKey("color"))
		{
			if (glass != "quartz" && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				ItemStack itemStack = new ItemStack(Api.World.GetBlock(new AssetLocation("glass-" + glass)));
				if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.0, 0.5));
				}
				Api.World.Logger.Audit("{0} Replaced glass {1} with {2} for Lantern at {3}.", byPlayer.PlayerName, itemStack.Collectible.Code, collectible.Code, Pos);
			}
			glass = collectible.Variant["color"];
			if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && glass != "quartz")
			{
				activeHotbarSlot.TakeOut(1);
			}
			if (Api.Side == EnumAppSide.Client)
			{
				(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
			Api.World.PlaySoundAt(Api.World.GetBlock(new AssetLocation("glass-" + glass)).Sounds.Place, Pos, -0.4, byPlayer);
			setLightColor(origlightHsv, lightHsv, glass);
			Api.World.BlockAccessor.ExchangeBlock(base.Block.Id, Pos);
			MarkDirty(redrawOnClient: true);
			return true;
		}
		if (lining == null || (lining == "plain" && collectible is ItemMetalPlate && (collectible.Variant["metal"] == "gold" || collectible.Variant["metal"] == "silver" || collectible.Variant["metal"] == "electrum")))
		{
			lining = collectible.Variant["metal"];
			if (Api.Side == EnumAppSide.Client)
			{
				(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/plate"), Pos, -0.4, byPlayer);
			activeHotbarSlot.TakeOut(1);
			MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public static void setLightColor(byte[] origLightHsv, byte[] lightHsv, string color)
	{
		switch (color)
		{
		case "green":
			lightHsv[0] = 20;
			lightHsv[1] = 4;
			break;
		case "blue":
			lightHsv[0] = 42;
			lightHsv[1] = 4;
			break;
		case "pink":
			lightHsv[0] = 54;
			lightHsv[1] = 4;
			break;
		case "violet":
			lightHsv[0] = 48;
			lightHsv[1] = 4;
			break;
		case "red":
			lightHsv[0] = 0;
			lightHsv[1] = 4;
			break;
		case "yellow":
			lightHsv[0] = 11;
			lightHsv[1] = 4;
			break;
		case "brown":
			lightHsv[0] = 5;
			lightHsv[1] = 4;
			break;
		default:
			lightHsv[1] = origLightHsv[1];
			lightHsv[0] = origLightHsv[0];
			break;
		}
	}
}
