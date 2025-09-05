using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockAntlerMount : BlockShapeMaterialFromAttributes
{
	public override string MeshKey => "AntlerMount";

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		float degY = ((!(blockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount blockEntityAntlerMount)) ? 0f : (blockEntityAntlerMount.MeshAngleRad * (180f / (float)Math.PI)));
		return new Cuboidf[1] { SelectionBoxes[0].RotatedCopy(0f, degY, 0f, new Vec3d(0.5, 0.5, 0.5)) };
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (blockSel.Face.IsHorizontal)
		{
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
			if (failureCode == "entityintersecting")
			{
				return false;
			}
		}
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		blockSel = blockSel.Clone();
		for (int i = 0; i < hORIZONTALS.Length; i++)
		{
			blockSel.Face = hORIZONTALS[i];
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
		}
		failureCode = "requirehorizontalattachable";
		return false;
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
	{
		BlockFacing opposite = blockSel.Face.Opposite;
		BlockPos pos = blockSel.Position.AddCopy(opposite);
		if (world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, blockSel.Face) && CanPlaceBlock(world, player, blockSel, ref failureCode))
		{
			DoPlaceBlock(world, player, blockSel, itemstack);
			return true;
		}
		return false;
	}

	private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.HorizontalFromAngle(((world.BlockAccessor.GetBlockEntity(pos) as BlockEntityAntlerMount)?.MeshAngleRad ?? 0f) + (float)Math.PI / 2f);
		return world.BlockAccessor.GetBlock(pos.AddCopy(blockFacing)).CanAttachBlockAt(world.BlockAccessor, this, pos.AddCopy(blockFacing), blockFacing.Opposite);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi? attachmentArea = null)
	{
		return false;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (flag)
		{
			BEBehaviorShapeMaterialFromAttributes behavior = world.BlockAccessor.GetBlockEntity(blockSel.Position).GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
			if (behavior != null)
			{
				for (int i = 0; i < 4; i++)
				{
					int num = (blockSel.Face.HorizontalAngleIndex + i) % 4;
					BlockPos pos = blockSel.Position.AddCopy(BlockFacing.HORIZONTALS_ANGLEORDER[num]);
					if (world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, blockSel.Face))
					{
						behavior.MeshAngleY = (float)(num * 90) * ((float)Math.PI / 180f) - (float)Math.PI / 2f;
						behavior.OnBlockPlaced(byItemStack);
					}
				}
			}
		}
		return flag;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityAntlerMount blockEntity = GetBlockEntity<BlockEntityAntlerMount>(pos);
		if (blockEntity != null && blockEntity.Type != null && blockEntity.Material != null)
		{
			float[] values = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntity.MeshAngleRad)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
			blockModelData = GetOrCreateMesh(blockEntity.Type, blockEntity.Material).Clone().MatrixTransform(values);
			decalModelData = GetOrCreateMesh(blockEntity.Type, blockEntity.Material, null, decalTexSource).Clone().MatrixTransform(values);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
		if (!CanBlockStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAntlerMount blockEntityAntlerMount)
		{
			return blockEntityAntlerMount.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount blockEntityAntlerMount))
		{
			return base.GetPlacedBlockName(world, pos);
		}
		return Lang.Get("block-antlermount-" + blockEntityAntlerMount.Type);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount blockEntityAntlerMount))
		{
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer) + "\n" + Lang.Get("Material: {0}", Lang.Get("material-" + blockEntityAntlerMount.Material));
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("type", "square");
		return Lang.Get("block-" + Code.Path + "-" + text);
	}
}
