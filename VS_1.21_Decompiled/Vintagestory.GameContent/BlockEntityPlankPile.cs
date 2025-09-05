using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

internal class BlockEntityPlankPile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private AssetLocation tmpWood;

	private ITexPositionSource tmpTextureSource;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/planks");

	private Dictionary<AssetLocation, MeshData[]> meshesByType => ObjectCacheUtil.GetOrCreate(Api, "plankpile-meshes", () => GenMeshes());

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "plankpile";

	public override int MaxStackSize => 48;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpWood.Path];

	private Dictionary<AssetLocation, MeshData[]> GenMeshes()
	{
		Dictionary<AssetLocation, MeshData[]> dictionary = new Dictionary<AssetLocation, MeshData[]>();
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape shapeBase = Shape.TryGet(Api, "shapes/block/wood/plankpile.json");
		MetalProperty metalProperty = Api.Assets.TryGet("worldproperties/block/wood.json").ToObject<MetalProperty>();
		metalProperty.Variants = metalProperty.Variants.Append(new MetalPropertyVariant
		{
			Code = new AssetLocation("aged")
		});
		for (int i = 0; i < metalProperty.Variants.Length; i++)
		{
			ITesselatorAPI tesselator = ((ICoreClientAPI)Api).Tesselator;
			MeshData[] array = new MeshData[49];
			tmpWood = metalProperty.Variants[i].Code;
			for (int j = 0; j <= 48; j++)
			{
				tesselator.TesselateShape("PlankPile", shapeBase, out array[j], this, null, 0, 0, 0, j);
			}
			dictionary[tmpWood] = array;
		}
		tmpTextureSource = null;
		tmpWood = null;
		tmpBlock = null;
		return dictionary;
	}

	public BlockEntityPlankPile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.ResolveBlocksOrItems();
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		lock (inventoryLock)
		{
			if (inventory[0].Itemstack == null)
			{
				return true;
			}
			string domainAndPath = inventory[0].Itemstack.Collectible.LastCodePart();
			int num = Math.Min(48, inventory[0].StackSize);
			meshdata.AddMeshData(meshesByType[new AssetLocation(domainAndPath)][num]);
		}
		return true;
	}
}
