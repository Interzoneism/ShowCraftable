using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[AddDocumentationProperty("Sides", "A list of sides that this decor block can be placed on.", "System.String[]", "Required", "", false)]
[AddDocumentationProperty("DrawIfCulled", "If true, do not cull even if parent face was culled (used e.g. for medium carpet, which stick out beyond the parent face)", "System.Boolean", "Optional", "False", false)]
[AddDocumentationProperty("AlternateZOffset", "If true, alternates z-offset vertexflag by 1 in odd/even XZ positions to reduce z-fighting (used e.g. for medium carpets overlaying neighbours)", "System.Boolean", "Optional", "False", false)]
[AddDocumentationProperty("NotFullFace", "If true, this decor is NOT (at least) a full opaque face so that the parent block face still needs to be drawn", "System.Boolean", "Optional", "False", false)]
[AddDocumentationProperty("Removable", "If true, this decor is removable using the players hands, without breaking the parent block", "System.Boolean", "Optional", "False", false)]
[AddDocumentationProperty("Thickness", "The thickness of this decor block. Used to adjust selection box of the parent block.", "System.Single", "Optional", "0.03125", false)]
[DocumentAsJson]
public class BlockBehaviorDecor : BlockBehavior
{
	private BlockFacing[] sides;

	[DocumentAsJson("Optional", "False", false)]
	private bool sidedVariants;

	[DocumentAsJson("Optional", "False", false)]
	private bool nwOrientable;

	public BlockBehaviorDecor(Block block)
		: base(block)
	{
		block.decorBehaviorFlags = 1;
	}

	public override void Initialize(JsonObject properties)
	{
		string[] array = properties["sides"].AsArray(Array.Empty<string>());
		sides = new BlockFacing[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				sides[i] = BlockFacing.FromFirstLetter(array[i][0]);
			}
		}
		sidedVariants = properties["sidedVariants"].AsBool();
		nwOrientable = properties["nwOrientable"].AsBool();
		if (properties["drawIfCulled"].AsBool())
		{
			block.decorBehaviorFlags |= 2;
		}
		if (properties["alternateZOffset"].AsBool())
		{
			block.decorBehaviorFlags |= 4;
		}
		if (properties["notFullFace"].AsBool())
		{
			block.decorBehaviorFlags |= 8;
		}
		if (properties["removable"].AsBool())
		{
			block.decorBehaviorFlags |= 16;
		}
		if (sidedVariants)
		{
			block.decorBehaviorFlags |= 32;
		}
		block.DecorThickness = properties["thickness"].AsFloat(1f / 32f);
		base.Initialize(properties);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		for (int i = 0; i < sides.Length; i++)
		{
			if (sides[i] != blockSel.Face)
			{
				continue;
			}
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
			Block block;
			if (sidedVariants)
			{
				block = world.BlockAccessor.GetBlock(base.block.CodeWithParts(blockSel.Face.Opposite.Code));
				if (block == null)
				{
					failureCode = "decorvariantnotfound";
					return false;
				}
			}
			else if (nwOrientable)
			{
				string component = ((Block.SuggestedHVOrientation(byPlayer, blockSel)[0].Axis == EnumAxis.X) ? "we" : "ns");
				block = world.BlockAccessor.GetBlock(base.block.CodeWithParts(component));
				if (block == null)
				{
					failureCode = "decorvariantnotfound";
					return false;
				}
			}
			else
			{
				block = base.block;
			}
			Block block2 = world.BlockAccessor.GetBlock(blockPos);
			IAcceptsDecor acceptsDecor = block2.GetInterface<IAcceptsDecor>(world, blockPos);
			if (acceptsDecor != null && acceptsDecor.CanAccept(block))
			{
				if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival)
				{
					int decor = acceptsDecor.GetDecor(blockSel.Face);
					if (decor > 0)
					{
						Block block3 = world.BlockAccessor.GetBlock(decor);
						ItemStack itemstack2 = new ItemStack(block3.Id, block3.ItemClass, 1, new TreeAttribute(), world);
						world.SpawnItemEntity(itemstack2, blockPos.AddCopy(blockSel.Face).ToVec3d());
					}
				}
				acceptsDecor.SetDecor(block, blockSel.Face);
				return true;
			}
			EnumBlockMaterial blockMaterial = block2.GetBlockMaterial(world.BlockAccessor, blockPos);
			if (!block2.CanAttachBlockAt(world.BlockAccessor, block, blockPos, blockSel.Face) || blockMaterial == EnumBlockMaterial.Snow || blockMaterial == EnumBlockMaterial.Ice)
			{
				failureCode = "decorrequiressolid";
				return false;
			}
			DecorBits decorBits = new DecorBits(blockSel.Face);
			Block decor2 = world.BlockAccessor.GetDecor(blockPos, decorBits);
			if (world.BlockAccessor.SetDecor(block, blockPos, decorBits))
			{
				if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival && decor2 != null && (decor2.decorBehaviorFlags & 0x10) != 0)
				{
					ItemStack itemstack3 = decor2.OnPickBlock(world, blockPos);
					world.SpawnItemEntity(itemstack3, blockPos.AddCopy(blockSel.Face).ToVec3d());
				}
				return true;
			}
			failureCode = "existingdecorinplace";
			return false;
		}
		failureCode = "cannotplacedecorhere";
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		if (nwOrientable)
		{
			handled = EnumHandling.PreventDefault;
			string[] array = new string[2] { "ns", "we" };
			int num = angle / 90;
			if (block.LastCodePart() == "we")
			{
				num++;
			}
			return block.CodeWithParts(array[num % 2]);
		}
		return base.GetRotatedBlockCode(angle, ref handled);
	}
}
