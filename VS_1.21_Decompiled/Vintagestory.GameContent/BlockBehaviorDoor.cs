using System;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("TriggerSound", "Sets both OpenSound & CloseSound.", "Vintagestory.API.Common.AssetLocation", "Optional", "sounds/block/door", true)]
public class BlockBehaviorDoor : StrongBlockBehavior, IMultiBlockColSelBoxes, IMultiBlockBlockProperties, IClaimTraverseable
{
	[DocumentAsJson("Optional", "sounds/block/door", true)]
	public AssetLocation OpenSound;

	[DocumentAsJson("Optional", "sounds/block/door", true)]
	public AssetLocation CloseSound;

	[DocumentAsJson("Optional", "1", true)]
	public int width;

	[DocumentAsJson("Optional", "1", true)]
	public int height;

	[DocumentAsJson("Optional", "True", true)]
	public bool handopenable;

	[DocumentAsJson("Optional", "True", true)]
	public bool airtight;

	private ICoreAPI api;

	public MeshData animatableOrigMesh;

	public Shape animatableShape;

	public string animatableDictKey;

	public BlockBehaviorDoor(Block block)
		: base(block)
	{
		airtight = block.Attributes["airtight"].AsBool(defaultValue: true);
		width = block.Attributes["width"].AsInt(1);
		height = block.Attributes["height"].AsInt(1);
		handopenable = block.Attributes["handopenable"].AsBool(defaultValue: true);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
		OpenSound = (CloseSound = AssetLocation.Create(block.Attributes["triggerSound"].AsString("sounds/block/door")));
		if (block.Attributes["openSound"].Exists)
		{
			OpenSound = AssetLocation.Create(block.Attributes["openSound"].AsString("sounds/block/door"));
		}
		if (block.Attributes["closeSound"].Exists)
		{
			CloseSound = AssetLocation.Create(block.Attributes["closeSound"].AsString("sounds/block/door"));
		}
		base.OnLoaded(api);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorDoor>();
		bool flag = !bEBehaviorDoor.Opened;
		if (activationArgs != null)
		{
			flag = activationArgs.GetBool("opened", flag);
		}
		if (bEBehaviorDoor.Opened != flag)
		{
			bEBehaviorDoor.ToggleDoorState(null, flag);
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor != null)
		{
			decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bEBehaviorDoor.RotateYRad, 0f);
		}
	}

	public static BEBehaviorDoor getDoorAt(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor != null)
		{
			return bEBehaviorDoor;
		}
		if (world.BlockAccessor.GetBlock(pos) is BlockMultiblock blockMultiblock)
		{
			return world.BlockAccessor.GetBlockEntity(pos.AddCopy(blockMultiblock.OffsetInv))?.GetBehavior<BEBehaviorDoor>();
		}
		return null;
	}

	public static bool HasCombinableLeftDoor(IWorldAccessor world, float RotateYRad, BlockPos pos, int width, out BEBehaviorDoor leftDoor, out int leftOffset)
	{
		leftOffset = 0;
		leftDoor = null;
		BlockFacing cW = BlockFacing.HorizontalFromYaw(RotateYRad).GetCW();
		BlockPos blockPos = pos.AddCopy(cW);
		leftDoor = getDoorAt(world, blockPos);
		if (width > 1)
		{
			if (leftDoor == null)
			{
				for (int i = 2; i <= width; i++)
				{
					blockPos = pos.AddCopy(cW, i);
					leftDoor = getDoorAt(world, blockPos);
					if (leftDoor != null)
					{
						break;
					}
				}
			}
			if (leftDoor != null)
			{
				BlockPos pos2 = leftDoor.Pos.AddCopy(cW.Opposite, leftDoor.InvertHandles ? width : (width + leftDoor.doorBh.width - 1));
				leftOffset = (int)pos.DistanceTo(pos2);
				if ((leftDoor.facingWhenClosed.Axis == EnumAxis.X && blockPos.X != leftDoor.Pos.X) || (leftDoor.facingWhenClosed.Axis == EnumAxis.Z && blockPos.Z != leftDoor.Pos.Z))
				{
					leftDoor = null;
					leftOffset = 0;
				}
			}
		}
		if (leftDoor != null && leftDoor.LeftDoor == null && leftDoor.RightDoor == null && leftDoor.facingWhenClosed == BlockFacing.HorizontalFromYaw(RotateYRad))
		{
			return true;
		}
		return false;
	}

	public static bool HasCombinableRightDoor(IWorldAccessor world, float RotateYRad, BlockPos pos, int width, out BEBehaviorDoor rightDoor, out int rightOffset)
	{
		rightOffset = 0;
		rightDoor = null;
		BlockFacing cCW = BlockFacing.HorizontalFromYaw(RotateYRad).GetCCW();
		BlockPos blockPos = pos.AddCopy(cCW);
		rightDoor = getDoorAt(world, blockPos);
		if (width > 1)
		{
			if (rightDoor == null)
			{
				for (int i = 2; i <= width; i++)
				{
					blockPos = pos.AddCopy(cCW, i);
					rightDoor = getDoorAt(world, blockPos);
					if (rightDoor != null)
					{
						break;
					}
				}
			}
			if (rightDoor != null)
			{
				BlockPos pos2 = rightDoor.Pos.AddCopy(cCW.Opposite, (!rightDoor.InvertHandles) ? width : (width + rightDoor.doorBh.width - 1));
				rightOffset = (int)pos.DistanceTo(pos2);
				if ((rightDoor.facingWhenClosed.Axis == EnumAxis.X && blockPos.X != rightDoor.Pos.X) || (rightDoor.facingWhenClosed.Axis == EnumAxis.Z && blockPos.Z != rightDoor.Pos.Z))
				{
					rightDoor = null;
					rightOffset = 0;
				}
			}
		}
		if (rightDoor != null && rightDoor.RightDoor == null && rightDoor.LeftDoor == null && rightDoor.facingWhenClosed == BlockFacing.HorizontalFromYaw(RotateYRad))
		{
			return true;
		}
		return false;
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		BlockPos blockPos = blockSel.Position.Copy();
		float rotateYRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
		BlockFacing blockFacing = BlockFacing.HorizontalFromYaw(rotateYRad);
		bool blocked = false;
		BEBehaviorDoor leftDoor;
		int leftOffset;
		bool flag = HasCombinableLeftDoor(world, rotateYRad, blockSel.Position, width, out leftDoor, out leftOffset);
		if (flag && width > 1 && leftOffset != 0)
		{
			blockPos.Add(blockFacing.GetCCW(), leftOffset);
		}
		if (!flag && HasCombinableRightDoor(world, rotateYRad, blockSel.Position, width, out leftDoor, out leftOffset) && width > 1 && leftOffset != 0)
		{
			blockPos.Add(blockFacing.GetCW(), leftOffset);
		}
		IterateOverEach(blockPos, rotateYRad, flag, delegate(BlockPos mpos)
		{
			if (!world.BlockAccessor.GetBlock(mpos, 1).IsReplacableBy(block))
			{
				blocked = true;
				return false;
			}
			return true;
		});
		if (blocked)
		{
			handling = EnumHandling.PreventDefault;
			failureCode = "notenoughspace";
			return false;
		}
		return base.CanPlaceBlock(world, byPlayer, blockSel, ref handling, ref failureCode);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockPos blockPos = blockSel.Position.Copy();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			float rotateYRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
			BlockFacing blockFacing = BlockFacing.HorizontalFromYaw(rotateYRad);
			if (HasCombinableLeftDoor(world, rotateYRad, blockSel.Position, width, out var leftDoor, out var leftOffset))
			{
				if (width > 1 && leftOffset != 0)
				{
					blockPos.Add(blockFacing.GetCCW(), leftOffset);
				}
			}
			else if (HasCombinableRightDoor(world, rotateYRad, blockSel.Position, width, out leftDoor, out leftOffset) && width > 1 && leftOffset != 0)
			{
				blockPos.Add(blockFacing.GetCW(), leftOffset);
			}
			return placeDoor(world, byPlayer, itemstack, blockSel, blockPos, blockAccessor);
		}
		return false;
	}

	public bool placeDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
	{
		ba.SetBlock(block.BlockId, pos);
		(ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()).OnBlockPlaced(itemstack, byPlayer, blockSel);
		if (world.Side == EnumAppSide.Server)
		{
			placeMultiblockParts(world, pos);
		}
		return true;
	}

	public void placeMultiblockParts(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		float yRotRad = bEBehaviorDoor?.RotateYRad ?? 0f;
		IterateOverEach(pos, yRotRad, bEBehaviorDoor?.InvertHandles ?? false, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			int num = mpos.X - pos.X;
			int num2 = mpos.Y - pos.Y;
			int num3 = mpos.Z - pos.Z;
			string text = ((num < 0) ? "n" : ((num > 0) ? "p" : "")) + Math.Abs(num);
			string text2 = ((num2 < 0) ? "n" : ((num2 > 0) ? "p" : "")) + Math.Abs(num2);
			string text3 = ((num3 < 0) ? "n" : ((num3 > 0) ? "p" : "")) + Math.Abs(num3);
			AssetLocation blockCode = new AssetLocation("multiblock-monolithic-" + text + "-" + text2 + "-" + text3);
			Block block = world.GetBlock(blockCode);
			world.BlockAccessor.SetBlock(block.Id, mpos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(mpos);
			}
			return true;
		});
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side == EnumAppSide.Client)
		{
			return;
		}
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		float yRotRad = bEBehaviorDoor?.RotateYRad ?? 0f;
		IterateOverEach(pos, yRotRad, bEBehaviorDoor?.InvertHandles ?? false, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			if (world.BlockAccessor.GetBlock(mpos) is BlockMultiblock)
			{
				world.BlockAccessor.SetBlock(0, mpos);
				if (world.Side == EnumAppSide.Server)
				{
					world.BlockAccessor.TriggerNeighbourBlockUpdate(mpos);
				}
			}
			return true;
		});
		base.OnBlockRemoved(world, pos, ref handling);
	}

	public void IterateOverEach(BlockPos pos, float yRotRad, bool invertHandle, ActionConsumable<BlockPos> onBlock)
	{
		BlockPos blockPos = new BlockPos(pos.dimension);
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				for (int k = 0; k < width; k++)
				{
					Vec3i adjacentOffset = BEBehaviorDoor.getAdjacentOffset(i, k, j, yRotRad, invertHandle);
					blockPos.Set(pos.X + adjacentOffset.X, pos.Y + adjacentOffset.Y, pos.Z + adjacentOffset.Z);
					if (!onBlock(blockPos))
					{
						return;
					}
				}
			}
		}
	}

	public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return getColSelBoxes(blockAccessor, pos, offset);
	}

	public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return getColSelBoxes(blockAccessor, pos, offset);
	}

	private static Cuboidf[] getColSelBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		BEBehaviorDoor bEBehaviorDoor = blockAccessor.GetBlockEntity(pos.AddCopy(offset.X, offset.Y, offset.Z))?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor == null)
		{
			return null;
		}
		Vec3i adjacentOffset = bEBehaviorDoor.getAdjacentOffset(-1, -1);
		if (offset.X == adjacentOffset.X && offset.Z == adjacentOffset.Z)
		{
			return null;
		}
		if (bEBehaviorDoor.Opened)
		{
			Vec3i adjacentOffset2 = bEBehaviorDoor.getAdjacentOffset(-1);
			if (offset.X == adjacentOffset2.X && offset.Z == adjacentOffset2.Z)
			{
				return null;
			}
		}
		else
		{
			Vec3i adjacentOffset3 = bEBehaviorDoor.getAdjacentOffset(0, -1);
			if (offset.X == adjacentOffset3.X && offset.Z == adjacentOffset3.Z)
			{
				return null;
			}
		}
		return bEBehaviorDoor.ColSelBoxes;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing, ref EnumHandling handled)
	{
		return base.GetParticleBreakBox(blockAccess, pos, facing, ref handled);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
	{
		BEBehaviorDoor bEBehaviorDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor.Opened)
		{
			float num = (bEBehaviorDoor.InvertHandles ? 90 : (-90));
			decalModelData = decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, num * ((float)Math.PI / 180f), 0f);
			if (!bEBehaviorDoor.InvertHandles)
			{
				decalModelData = decalModelData.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1f, 1f, -1f);
			}
		}
		base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData, ref handled);
	}

	public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
	{
		doorNameWithMaterial(sb);
	}

	public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
	{
	}

	private void doorNameWithMaterial(StringBuilder sb)
	{
		if (block.Variant.ContainsKey("wood"))
		{
			string text = sb.ToString();
			sb.Clear();
			sb.Append(Lang.Get("doorname-with-material", text, Lang.Get("material-" + block.Variant["wood"])));
		}
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor bEBehavior = block.GetBEBehavior<BEBehaviorDoor>(pos);
		if (bEBehavior == null)
		{
			return 0f;
		}
		if (!bEBehavior.IsSideSolid(face))
		{
			return 0f;
		}
		if (block.Variant["style"] == "sleek-windowed")
		{
			return 1f;
		}
		if (!airtight)
		{
			return 0f;
		}
		return 1f;
	}

	public float MBGetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, Vec3i offset)
	{
		BEBehaviorDoor bEBehavior = block.GetBEBehavior<BEBehaviorDoor>(pos.AddCopy(offset.X, offset.Y, offset.Z));
		if (bEBehavior == null)
		{
			return 0f;
		}
		if (!bEBehavior.IsSideSolid(face))
		{
			return 0f;
		}
		if (block.Variant["style"] == "sleek-windowed")
		{
			if (offset.Y != -1)
			{
				return 1f;
			}
			return 0f;
		}
		if (!airtight)
		{
			return 0f;
		}
		return 1f;
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor bEBehavior = block.GetBEBehavior<BEBehaviorDoor>(pos);
		if (bEBehavior == null)
		{
			return 0;
		}
		if (type == EnumRetentionType.Sound)
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!bEBehavior.IsSideSolid(facing) && !bEBehavior.IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	public int MBGetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, Vec3i offset)
	{
		BEBehaviorDoor bEBehavior = block.GetBEBehavior<BEBehaviorDoor>(pos.AddCopy(offset.X, offset.Y, offset.Z));
		if (bEBehavior == null)
		{
			return 0;
		}
		if (type == EnumRetentionType.Sound)
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!bEBehavior.IsSideSolid(facing) && !bEBehavior.IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	private int getInsulation(BlockPos pos)
	{
		EnumBlockMaterial blockMaterial = block.GetBlockMaterial(api.World.BlockAccessor, pos);
		if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
		{
			return -1;
		}
		return 1;
	}

	public bool MBCanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea, Vec3i offsetInv)
	{
		return false;
	}

	public JsonObject MBGetAttributes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return null;
	}
}
