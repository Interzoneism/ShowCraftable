using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFruitTreeBranch : BlockFruitTreePart, ITexPositionSource, ICustomTreeFellingBehavior, ICustomHandbookPageContent
{
	private Block branchBlock;

	private BlockFruitTreeFoliage foliageBlock;

	private ICoreClientAPI capi;

	public FruitTreeWorldGenConds[] WorldGenConds;

	public Dictionary<string, FruitTreeShape> Shapes = new Dictionary<string, FruitTreeShape>();

	public Dictionary<string, FruitTreeTypeProperties> TypeProps;

	private string curTreeType;

	private Shape curTessShape;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			foliageBlock.foliageProps.TryGetValue(curTreeType, out var value);
			if (value != null)
			{
				TextureAtlasPosition orLoadTexture = value.GetOrLoadTexture(capi, textureCode);
				if (orLoadTexture != null)
				{
					return orLoadTexture;
				}
			}
			Shape shape = curTessShape;
			if (shape != null && shape.Textures.TryGetValue(textureCode, out var value2))
			{
				return capi.BlockTextureAtlas[value2];
			}
			return null;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		branchBlock = api.World.GetBlock(CodeWithVariant("type", "branch"));
		foliageBlock = api.World.GetBlock(AssetLocation.Create(Attributes["foliageBlock"].AsString(), Code.Domain)) as BlockFruitTreeFoliage;
		TypeProps = Attributes["fruittreeProperties"].AsObject<Dictionary<string, FruitTreeTypeProperties>>();
		Dictionary<string, CompositeShape> dictionary = Attributes["shapes"].AsObject<Dictionary<string, CompositeShape>>();
		WorldGenConds = Attributes["worldgen"].AsObject<FruitTreeWorldGenConds[]>();
		foreach (KeyValuePair<string, CompositeShape> item in dictionary)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, item.Value.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
			Shapes[item.Key] = new FruitTreeShape
			{
				Shape = shape,
				CShape = item.Value
			};
		}
		LCGRandom rnd = new LCGRandom(api.World.Seed);
		foreach (KeyValuePair<string, FruitTreeTypeProperties> prop in TypeProps)
		{
			BlockDropItemStack[] fruitStacks = prop.Value.FruitStacks;
			for (int i = 0; i < fruitStacks.Length; i++)
			{
				fruitStacks[i].Resolve(api.World, "fruit tree FruitStacks ", Code);
			}
			(api as ICoreServerAPI)?.RegisterTreeGenerator(new AssetLocation("fruittree-" + prop.Key), delegate(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams)
			{
				GrowTree(blockAccessor, pos, prop.Key, treegenParams.size, rnd);
			});
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		bool num = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).Fertility > 0;
		BlockEntityFruitTreeBranch blockEntityFruitTreeBranch = world.BlockAccessor.GetBlockEntity(blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position) as BlockEntityFruitTreeBranch;
		bool flag = blockSel.Face != BlockFacing.DOWN && blockEntityFruitTreeBranch != null && (blockEntityFruitTreeBranch.SideGrowth & (1 << blockSel.Face.Index)) > 0;
		if (!num && !flag)
		{
			failureCode = "fruittreecutting";
			return false;
		}
		if (flag && TypeProps.TryGetValue(blockEntityFruitTreeBranch.TreeType, out var value) && TypeProps.TryGetValue(itemstack.Attributes.GetString("type"), out var value2) && value.CycleType != value2.CycleType)
		{
			failureCode = "fruittreecutting-ctypemix";
			return false;
		}
		return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (itemstack.Collectible.Variant["type"] == "cutting")
		{
			curTreeType = itemstack.Attributes.GetString("type");
			if (curTreeType == null)
			{
				return;
			}
			Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "cuttingMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
			if (!orCreate.TryGetValue(curTreeType, out var value))
			{
				curTessShape = capi.TesselatorManager.GetCachedShape(Shape.Base);
				capi.Tesselator.TesselateShape("fruittreecutting", curTessShape, out var modeldata, this, null, 0, 0, 0);
				orCreate[curTreeType] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(modeldata));
			}
			else
			{
				renderinfo.ModelRef = value;
			}
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (capi == null)
		{
			return;
		}
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "cuttingMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
		if (orCreate == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in orCreate)
		{
			item.Value.Dispose();
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BlockEntityFruitTreeBranch blockEntityFruitTreeBranch = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
		itemStack.Attributes.SetString("type", blockEntityFruitTreeBranch?.TreeType ?? "pinkapple");
		return itemStack;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] dropsForHandbook = base.GetDropsForHandbook(handbookStack, forPlayer);
		BlockDropItemStack[] array = dropsForHandbook;
		foreach (BlockDropItemStack blockDropItemStack in array)
		{
			if (blockDropItemStack.ResolvedItemstack.Collectible is BlockFruitTreeBranch)
			{
				blockDropItemStack.ResolvedItemstack.Attributes.SetString("type", handbookStack.Attributes.GetString("type") ?? "pinkapple");
			}
		}
		return dropsForHandbook;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityFruitTreeBranch blockEntityFruitTreeBranch = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
		bool flag = blockEntityFruitTreeBranch != null && blockEntityFruitTreeBranch.FoliageState != EnumFoliageState.Dead;
		for (int i = 0; i < drops.Length; i++)
		{
			ItemStack itemStack = drops[i];
			if (itemStack.Collectible is BlockFruitTreeBranch)
			{
				itemStack.Attributes.SetString("type", blockEntityFruitTreeBranch?.TreeType);
			}
			if (itemStack.Collectible.Variant["type"] == "cutting" && !flag)
			{
				drops[i] = new ItemStack(world.GetItem(new AssetLocation("firewood")), 2);
			}
		}
		return drops;
	}

	public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		if (this == branchBlock)
		{
			return (facingIndex == 1 || facingIndex == 2 || facingIndex == 4) & (neighbourBlock == this || neighbourBlock == branchBlock);
		}
		return false;
	}

	public EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeBranch { PartType: EnumTreePartType.Branch } blockEntityFruitTreeBranch))
		{
			return EnumTreeFellingBehavior.Chop;
		}
		if (blockEntityFruitTreeBranch.GrowthDir.IsVertical)
		{
			return EnumTreeFellingBehavior.Chop;
		}
		return EnumTreeFellingBehavior.NoChop;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return (blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch)?.GetColSelBox() ?? base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return (blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch)?.GetColSelBox() ?? base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeBranch blockEntityFruitTreeBranch)
		{
			FruitTreeRootBH behavior = blockEntityFruitTreeBranch.GetBehavior<FruitTreeRootBH>();
			if (behavior != null && behavior.IsYoung && blockEntityFruitTreeBranch.PartType != EnumTreePartType.Cutting)
			{
				return Lang.Get("fruittree-young-" + blockEntityFruitTreeBranch.TreeType);
			}
			string text = "fruittree-branch-";
			if (blockEntityFruitTreeBranch.PartType == EnumTreePartType.Cutting)
			{
				text = "fruittree-cutting-";
			}
			else if (blockEntityFruitTreeBranch.PartType == EnumTreePartType.Stem || behavior != null)
			{
				text = "fruittree-stem-";
			}
			return Lang.Get(text + blockEntityFruitTreeBranch.TreeType);
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes?.GetString("type", "unknown") ?? "unknown";
		return Lang.Get("fruittree-cutting-" + text);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos).Fertility <= 20)
		{
			return false;
		}
		ClimateCondition climateAt = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		int num = worldgenRandom.NextInt(WorldGenConds.Length);
		int num2 = WorldGenConds.Length;
		for (int i = 0; i < num2; i++)
		{
			FruitTreeWorldGenConds fruitTreeWorldGenConds = WorldGenConds[(i + num) % num2];
			if (fruitTreeWorldGenConds.MinTemp <= climateAt.Temperature && fruitTreeWorldGenConds.MaxTemp >= climateAt.Temperature && fruitTreeWorldGenConds.MinRain <= climateAt.Rainfall && fruitTreeWorldGenConds.MaxRain >= climateAt.Rainfall && worldgenRandom.NextFloat() <= fruitTreeWorldGenConds.Chance)
			{
				blockAccessor.SetBlock(BlockId, pos);
				blockAccessor.SpawnBlockEntity(EntityClass, pos);
				BlockEntityFruitTreeBranch obj = blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
				obj.TreeType = fruitTreeWorldGenConds.Type;
				obj.FastForwardGrowth = worldgenRandom.NextFloat();
				return true;
			}
		}
		return false;
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, string type, float growthRel, IRandom random)
	{
		pos = pos.UpCopy();
		blockAccessor.SetBlock(BlockId, pos);
		BlockEntityFruitTreeBranch blockEntityFruitTreeBranch = api.ClassRegistry.CreateBlockEntity(EntityClass) as BlockEntityFruitTreeBranch;
		blockEntityFruitTreeBranch.Pos = pos.Copy();
		blockEntityFruitTreeBranch.TreeType = type;
		blockEntityFruitTreeBranch.FastForwardGrowth = growthRel;
		blockAccessor.SpawnBlockEntity(blockEntityFruitTreeBranch);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityFruitTreeBranch blockEntityFruitTreeBranch && blockEntityFruitTreeBranch.FastForwardGrowth.HasValue)
		{
			blockEntityFruitTreeBranch.CreateBehaviors(this, api.World);
			blockEntityFruitTreeBranch.Initialize(api);
			blockEntityFruitTreeBranch.MarkDirty(redrawOnClient: true);
		}
		else
		{
			base.OnBlockPlaced(world, blockPos, byItemStack);
		}
	}

	public void OnHandbookPageComposed(List<RichTextComponentBase> components, ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		string key = inSlot.Itemstack.Attributes?.GetString("type", "unknown") ?? "unknown";
		if (TypeProps.TryGetValue(key, out var value))
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (value.CycleType == EnumTreeCycleType.Deciduous)
			{
				stringBuilder.AppendLine(Lang.Get("Must experience {0} game hours below {1}°C in the cold season to bear fruit in the following year.", value.VernalizationHours.avg, value.VernalizationTemp.avg));
				stringBuilder.AppendLine(Lang.Get("Will die if exposed to {0}°C or colder", value.DieBelowTemp.avg));
			}
			else
			{
				stringBuilder.AppendLine(Lang.Get("Evergreen tree. Will die if exposed to {0} °C or colder", value.DieBelowTemp.avg));
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(Lang.Get("handbook-fruittree-note-averages"));
			float unScaleMarginTop = 7f;
			components.Add(new ClearFloatTextComponent(capi, unScaleMarginTop));
			components.Add(new RichTextComponent(capi, Lang.Get("Growing properties") + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
			components.Add(new RichTextComponent(capi, stringBuilder.ToString(), CairoFont.WhiteSmallText()));
			components.Add(new ClearFloatTextComponent(capi, unScaleMarginTop));
			components.Add(new RichTextComponent(capi, Lang.Get("fruittree-produces") + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
			BlockDropItemStack[] fruitStacks = value.FruitStacks;
			foreach (BlockDropItemStack blockDropItemStack in fruitStacks)
			{
				components.Add(new ItemstackTextComponent(capi, blockDropItemStack.ResolvedItemstack, 40.0, 0.0, EnumFloat.Inline));
			}
		}
	}
}
