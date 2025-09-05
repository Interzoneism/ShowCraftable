using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityIngotPile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private string tmpMetalCode;

	private ITexPositionSource tmpTextureSource;

	private ICoreClientAPI capi;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/ingot");

	private Dictionary<string, MeshData[]> meshesByType
	{
		get
		{
			Api.ObjectCache.TryGetValue("ingotpile-meshes", out var value);
			return (Dictionary<string, MeshData[]>)value;
		}
		set
		{
			Api.ObjectCache["ingotpile-meshes"] = value;
		}
	}

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "ingotpile";

	public override int MaxStackSize => 64;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpMetalCode];

	public string MetalType => inventory?[0]?.Itemstack?.Collectible?.LastCodePart();

	internal void EnsureMeshExists()
	{
		if (meshesByType == null)
		{
			meshesByType = new Dictionary<string, MeshData[]>();
		}
		if (MetalType == null || meshesByType.ContainsKey(MetalType) || Api.Side != EnumAppSide.Client)
		{
			return;
		}
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape orCreate = ObjectCacheUtil.GetOrCreate(Api, "ingotpileshape", () => Shape.TryGet(Api, "shapes/block/metal/ingotpile.json"));
		if (orCreate == null)
		{
			return;
		}
		foreach (string key in base.Block.Textures.Keys)
		{
			ITesselatorAPI tesselator = ((ICoreClientAPI)Api).Tesselator;
			MeshData[] array = new MeshData[65];
			tmpMetalCode = key;
			for (int num = 0; num <= 64; num++)
			{
				MeshData modeldata = array[num];
				tesselator.TesselateShape("ingotPile", orCreate, out modeldata, this, null, 0, 0, 0, num);
			}
			meshesByType[tmpMetalCode] = array;
		}
		tmpTextureSource = null;
		tmpMetalCode = null;
		tmpBlock = null;
	}

	public override bool TryPutItem(IPlayer player)
	{
		return base.TryPutItem(player);
	}

	public BlockEntityIngotPile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.ResolveBlocksOrItems();
		capi = api as ICoreClientAPI;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		lock (inventoryLock)
		{
			if (inventory[0].Itemstack == null)
			{
				return true;
			}
			EnsureMeshExists();
			if (MetalType != null && meshesByType.TryGetValue(MetalType, out var value))
			{
				meshdata.AddMeshData(value[inventory[0].StackSize]);
			}
		}
		return true;
	}
}
