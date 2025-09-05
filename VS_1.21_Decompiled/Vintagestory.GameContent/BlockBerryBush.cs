using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBerryBush : BlockPlant
{
	private MeshData[] prunedmeshes;

	private WorldInteraction[] interactions;

	public string State => Variant["state"];

	public string Type => Variant["type"];

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactions = ObjectCacheUtil.GetOrCreate(api, "berryBushInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item item in api.World.Items)
			{
				if (item.Tool == EnumTool.Shears)
				{
					list.Add(new ItemStack(item));
				}
			}
			ItemStack[] sstacks = list.ToArray();
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-berrybush-prune",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = sstacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBerryBush { Pruned: false }) ? sstacks : null
				}
			};
		});
	}

	public MeshData GetPrunedMesh(BlockPos pos)
	{
		if (api == null)
		{
			return null;
		}
		if (prunedmeshes == null)
		{
			genPrunedMeshes();
		}
		int num = ((RandomizeAxes == EnumRandomizeAxes.XYZ) ? GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, prunedmeshes.Length) : GameMath.MurmurHash3Mod(pos.X, 0, pos.Z, prunedmeshes.Length));
		return prunedmeshes[num];
	}

	private void genPrunedMeshes()
	{
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		prunedmeshes = new MeshData[Shape.BakedAlternates.Length];
		string[] array = new string[4] { "Berries", "branchesN", "branchesS", "Leaves" };
		if (State == "empty")
		{
			array = array.Remove("Berries");
		}
		for (int i = 0; i < Shape.BakedAlternates.Length; i++)
		{
			CompositeShape compositeShape = Shape.BakedAlternates[i];
			Shape cachedShape = coreClientAPI.TesselatorManager.GetCachedShape(compositeShape.Base);
			coreClientAPI.Tesselator.TesselateShape(this, cachedShape, out prunedmeshes[i], Shape.RotateXYZCopy, null, array);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBerryBush { Pruned: false } blockEntityBerryBush && byPlayer != null && byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Shears)
		{
			blockEntityBerryBush.Prune();
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlock(pos.DownCopy());
		if (block.Fertility > 0)
		{
			return true;
		}
		if (!(block is BlockBerryBush))
		{
			return false;
		}
		if (blockAccessor.GetBlock(pos.DownCopy(2)).Fertility > 0)
		{
			JsonObject attributes = Attributes;
			if (attributes != null && attributes.IsTrue("stackable"))
			{
				return block.Attributes?.IsTrue("stackable") ?? false;
			}
		}
		return false;
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		BakedCompositeTexture bakedCompositeTexture = Textures?.First().Value?.Baked;
		if (bakedCompositeTexture == null)
		{
			return 0;
		}
		int randomColor = capi.BlockTextureAtlas.GetRandomColor(bakedCompositeTexture.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", randomColor, pos.X, pos.Y, pos.Z);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int colorWithoutTint = base.GetColorWithoutTint(capi, pos);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", colorWithoutTint, pos.X, pos.Y, pos.Z);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		ItemStack[] array = drops;
		foreach (ItemStack itemStack in array)
		{
			if (!(itemStack.Collectible is BlockBerryBush))
			{
				float num = 1f;
				JsonObject attributes = Attributes;
				if (attributes != null && attributes.IsTrue("forageStatAffected"))
				{
					num *= byPlayer?.Entity.Stats.GetBlended("forageDropRate") ?? 1f;
				}
				itemStack.StackSize = GameMath.RoundRandom(api.World.Rand, (float)itemStack.StackSize * num);
			}
		}
		return drops;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(interactions);
	}
}
