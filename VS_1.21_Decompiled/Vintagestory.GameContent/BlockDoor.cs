using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockDoor : BlockBaseDoor
{
	private bool airtight;

	public override void OnLoaded(ICoreAPI api)
	{
		airtight = Variant["material"] != "log";
		base.OnLoaded(api);
	}

	public override string GetKnobOrientation()
	{
		return GetKnobOrientation(this);
	}

	public override BlockFacing GetDirection()
	{
		return BlockFacing.FromCode(Variant["horizontalorientation"]);
	}

	public bool IsSideSolid(BlockFacing facing)
	{
		BlockFacing opposite = GetDirection().Opposite;
		BlockFacing blockFacing = ((GetKnobOrientation() == "left") ? opposite.GetCCW() : opposite.GetCW());
		if (open || opposite != facing)
		{
			if (open)
			{
				return blockFacing == facing;
			}
			return false;
		}
		return true;
	}

	public string GetKnobOrientation(Block block)
	{
		return Variant["knobOrientation"];
	}

	public bool IsUpperHalf()
	{
		return Variant["part"] == "up";
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockPos pos = blockSel.Position.AddCopy(0, 1, 0);
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (CanPlaceBlock(world, byPlayer, blockSel.AddPosCopy(0, 1, 0), ref failureCode) && CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			string suggestedKnobOrientation = GetSuggestedKnobOrientation(blockAccessor, blockSel.Position, array[0]);
			AssetLocation code = CodeWithVariants(new Dictionary<string, string>
			{
				{
					"horizontalorientation",
					array[0].Code
				},
				{ "part", "down" },
				{ "state", "closed" },
				{ "knobOrientation", suggestedKnobOrientation }
			});
			Block block = blockAccessor.GetBlock(code);
			AssetLocation code2 = block.CodeWithVariant("part", "up");
			Block block2 = blockAccessor.GetBlock(code2);
			blockAccessor.SetBlock(block.BlockId, blockSel.Position);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			}
			blockAccessor.SetBlock(block2.BlockId, pos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			}
			return true;
		}
		return false;
	}

	private string GetSuggestedKnobOrientation(IBlockAccessor ba, BlockPos pos, BlockFacing facing)
	{
		string result = "left";
		Block block = ba.GetBlock(pos.AddCopy(facing.GetCW()));
		Block block2 = ba.GetBlock(pos.AddCopy(facing.GetCCW()));
		bool flag = IsSameDoor(block);
		bool flag2 = IsSameDoor(block2);
		if (flag && flag2)
		{
			result = "left";
		}
		else if (flag)
		{
			result = ((GetKnobOrientation(block) == "right") ? "left" : "right");
		}
		else if (flag2)
		{
			result = ((GetKnobOrientation(block2) == "right") ? "left" : "right");
		}
		return result;
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		base.OnBlockRemoved(world, pos);
		BlockPos pos2 = pos.AddCopy(0, (!IsUpperHalf()) ? 1 : (-1), 0);
		Block block = world.BlockAccessor.GetBlock(pos2);
		if (block is BlockDoor && ((BlockDoor)block).IsUpperHalf() != IsUpperHalf())
		{
			world.BlockAccessor.SetBlock(0, pos2);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(pos2);
			}
		}
		if (world.BlockAccessor.GetBlock(pos) is BlockDoor)
		{
			world.BlockAccessor.SetBlock(0, pos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			}
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		AssetLocation code = CodeWithVariants(new Dictionary<string, string>
		{
			{ "horizontalorientation", "north" },
			{ "part", "down" },
			{ "state", "closed" },
			{ "knobOrientation", "left" }
		});
		return new ItemStack(world.BlockAccessor.GetBlock(code));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position)
	{
		float num = Attributes["breakOnTriggerChance"].AsFloat();
		if (world.Side == EnumAppSide.Server && world.Rand.NextDouble() < (double)num && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			world.BlockAccessor.BreakBlock(position, byPlayer);
			world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), position, 0.0);
			return;
		}
		AssetLocation code = CodeWithVariant("state", IsOpened() ? "closed" : "opened");
		Block block = world.BlockAccessor.GetBlock(code);
		AssetLocation code2 = block.CodeWithVariant("part", IsUpperHalf() ? "down" : "up");
		world.BlockAccessor.ExchangeBlock(block.BlockId, position);
		world.BlockAccessor.MarkBlockDirty(position);
		if (world.Side == EnumAppSide.Server)
		{
			world.BlockAccessor.TriggerNeighbourBlockUpdate(position);
		}
		BlockPos pos = position.AddCopy(0, (!IsUpperHalf()) ? 1 : (-1), 0);
		Block block2 = world.BlockAccessor.GetBlock(pos);
		if (block2 is BlockDoor && ((BlockDoor)block2).IsUpperHalf() != IsUpperHalf())
		{
			world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(code2).BlockId, pos);
			world.BlockAccessor.MarkBlockDirty(pos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			}
		}
	}

	protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
	{
		string knobOrientation = GetKnobOrientation();
		BlockFacing direction = GetDirection();
		if (!(knobOrientation == "left"))
		{
			return pos.AddCopy(direction.GetCW());
		}
		return pos.AddCopy(direction.GetCCW());
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int num = GameMath.Mod(BlockFacing.FromCode(LastCodePart(3)).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return CodeWithVariant("horizontalorientation", blockFacing.Code);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		if (type == EnumRetentionType.Sound)
		{
			if (!IsSideSolid(facing))
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
			if (!IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!IsSideSolid(facing) && !IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	private int getInsulation(BlockPos pos)
	{
		EnumBlockMaterial blockMaterial = GetBlockMaterial(api.World.BlockAccessor, pos);
		if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
		{
			return -1;
		}
		return 1;
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		if (!IsSideSolid(face))
		{
			return 0f;
		}
		if (!airtight)
		{
			return 0f;
		}
		JsonObject attributes = Attributes;
		if (attributes == null)
		{
			if (!IsUpperHalf())
			{
				return 1f;
			}
			return 0f;
		}
		return attributes["liquidBarrierHeight"].AsFloat(IsUpperHalf() ? 0f : 1f);
	}
}
