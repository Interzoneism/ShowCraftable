using System;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

[DocumentAsJson]
public class BlockBehaviorOmniRotatable : BlockBehavior
{
	private bool rotateH;

	private bool rotateV;

	private bool rotateV4;

	[DocumentAsJson("Optional", "player", false)]
	private string facing = "player";

	[DocumentAsJson("Optional", "False", false)]
	private bool rotateSides;

	[DocumentAsJson("Optional", "1", false)]
	private float dropChance = 1f;

	public string Rot => block.Variant["rot"];

	public BlockBehaviorOmniRotatable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		rotateH = properties["rotateH"].AsBool(rotateH);
		rotateV = properties["rotateV"].AsBool(rotateV);
		rotateV4 = properties["rotateV4"].AsBool(rotateV4);
		rotateSides = properties["rotateSides"].AsBool(rotateSides);
		facing = properties["facing"].AsString(facing);
		dropChance = properties["dropChance"].AsFloat(1f);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		AssetLocation assetLocation = null;
		switch ((EnumSlabPlaceMode)((itemstack.Attributes != null) ? itemstack.Attributes.GetInt("slabPlaceMode") : 0))
		{
		case EnumSlabPlaceMode.Horizontal:
		{
			string value = ((blockSel.HitPosition.Y < 0.5) ? "down" : "up");
			if (blockSel.Face.IsVertical)
			{
				value = blockSel.Face.Opposite.Code;
			}
			assetLocation = base.block.CodeWithVariant("rot", value);
			Block block = world.BlockAccessor.GetBlock(assetLocation);
			if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		case EnumSlabPlaceMode.Vertical:
		{
			string code = Block.SuggestedHVOrientation(byPlayer, blockSel)[0].Code;
			if (blockSel.Face.IsHorizontal)
			{
				code = blockSel.Face.Opposite.Code;
			}
			assetLocation = base.block.CodeWithVariant("rot", code);
			Block block = world.BlockAccessor.GetBlock(assetLocation);
			if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		default:
		{
			if (rotateSides)
			{
				if (!facing.Equals("block", StringComparison.CurrentCultureIgnoreCase))
				{
					assetLocation = ((!blockSel.Face.IsVertical) ? base.block.CodeWithVariant("rot", BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code) : base.block.CodeWithVariant("rot", blockSel.Face.Opposite.Code));
				}
				else
				{
					double num = Math.Abs(blockSel.HitPosition.X - 0.5);
					double num2 = Math.Abs(blockSel.HitPosition.Y - 0.5);
					double num3 = Math.Abs(blockSel.HitPosition.Z - 0.5);
					switch (blockSel.Face.Axis)
					{
					case EnumAxis.X:
						assetLocation = ((num3 < 0.3 && num2 < 0.3) ? base.block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(num3 > num2)) ? base.block.CodeWithVariant("rot", (blockSel.HitPosition.Y < 0.5) ? "down" : "up") : base.block.CodeWithVariant("rot", (blockSel.HitPosition.Z < 0.5) ? "north" : "south")));
						break;
					case EnumAxis.Y:
						assetLocation = ((num3 < 0.3 && num < 0.3) ? base.block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(num3 > num)) ? base.block.CodeWithVariant("rot", (blockSel.HitPosition.X < 0.5) ? "west" : "east") : base.block.CodeWithVariant("rot", (blockSel.HitPosition.Z < 0.5) ? "north" : "south")));
						break;
					case EnumAxis.Z:
						assetLocation = ((num < 0.3 && num2 < 0.3) ? base.block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(num > num2)) ? base.block.CodeWithVariant("rot", (blockSel.HitPosition.Y < 0.5) ? "down" : "up") : base.block.CodeWithVariant("rot", (blockSel.HitPosition.X < 0.5) ? "west" : "east")));
						break;
					}
				}
			}
			else if (rotateH || rotateV)
			{
				string text = "north";
				string text2 = "up";
				if (blockSel.Face.IsVertical)
				{
					text2 = blockSel.Face.Code;
					text = BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code;
				}
				else if (rotateV4)
				{
					text = ((!(facing == "block")) ? BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code : blockSel.Face.Opposite.Code);
					switch (blockSel.Face.Axis)
					{
					case EnumAxis.X:
						text2 = ((!(Math.Abs(blockSel.HitPosition.Z - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))) ? ((blockSel.HitPosition.Y < 0.5) ? "up" : "down") : ((blockSel.HitPosition.Z < 0.5) ? "left" : "right"));
						break;
					case EnumAxis.Z:
						text2 = ((!(Math.Abs(blockSel.HitPosition.X - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))) ? ((blockSel.HitPosition.Y < 0.5) ? "up" : "down") : ((blockSel.HitPosition.X < 0.5) ? "left" : "right"));
						break;
					}
				}
				else
				{
					text2 = ((blockSel.HitPosition.Y < 0.5) ? "up" : "down");
				}
				if (rotateH && rotateV)
				{
					assetLocation = base.block.CodeWithVariants(new string[2] { "v", "rot" }, new string[2] { text2, text });
				}
				else if (rotateH)
				{
					assetLocation = base.block.CodeWithVariant("rot", text);
				}
				else if (rotateV)
				{
					assetLocation = base.block.CodeWithVariant("rot", text2);
				}
			}
			if (assetLocation == null)
			{
				assetLocation = base.block.Code;
			}
			Block block = world.BlockAccessor.GetBlock(assetLocation);
			if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling handled)
	{
		ItemSlot itemSlot = allInputslots.FirstOrDefault((ItemSlot s) => !s.Empty);
		Block block = itemSlot.Itemstack.Block;
		if (block == null || !block.HasBehavior<BlockBehaviorOmniRotatable>())
		{
			base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
			return;
		}
		int num = (itemSlot.Itemstack.Attributes.GetInt("slabPlaceMode") + 1) % 3;
		if (num == 0)
		{
			outputSlot.Itemstack.Attributes.RemoveAttribute("slabPlaceMode");
		}
		else
		{
			outputSlot.Itemstack.Attributes.SetInt("slabPlaceMode", num);
		}
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		ItemStack[] drops = block.GetDrops(world, pos, null);
		if (drops == null || drops.Length == 0)
		{
			return new ItemStack(block);
		}
		return drops[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		if (dropChance < 1f && world.Rand.NextDouble() > (double)dropChance)
		{
			handling = EnumHandling.PreventDefault;
			return Array.Empty<ItemStack>();
		}
		return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null)
	{
		if (Rot == "down")
		{
			handling = EnumHandling.PreventDefault;
			if (blockFace != BlockFacing.DOWN)
			{
				if (attachmentArea != null)
				{
					return attachmentArea.Y2 < 8;
				}
				return false;
			}
			return true;
		}
		if (Rot == "up")
		{
			handling = EnumHandling.PreventDefault;
			if (blockFace != BlockFacing.UP)
			{
				if (attachmentArea != null)
				{
					return attachmentArea.Y1 > 7;
				}
				return false;
			}
			return true;
		}
		return base.CanAttachBlockAt(world, block, pos, blockFace, ref handling, attachmentArea);
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handling)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (blockFacing.IsVertical)
		{
			return block.Code;
		}
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + blockFacing.HorizontalAngleIndex) % 4];
		if (rotateV4)
		{
			string text = block.Variant["v"];
			if ((angle == 90 && (blockFacing == BlockFacing.WEST || blockFacing == BlockFacing.EAST)) || (angle == 270 && blockFacing == BlockFacing.SOUTH))
			{
				if (block.Variant["v"] == "left")
				{
					text = "right";
				}
				if (block.Variant["v"] == "right")
				{
					text = "left";
				}
			}
			return block.CodeWithVariants(new string[2] { "rot", "v" }, new string[2] { blockFacing2.Code, text });
		}
		return block.CodeWithVariant("rot", blockFacing2.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithVariant("rot", blockFacing.Opposite.Code);
		}
		return block.Code;
	}

	public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (blockFacing.IsVertical)
		{
			return block.CodeWithVariant("rot", blockFacing.Opposite.Code);
		}
		blockFacing = BlockFacing.FromCode(block.Variant["v"]);
		if (blockFacing != null && blockFacing.IsVertical)
		{
			return block.CodeWithParts(blockFacing.Opposite.Code, block.LastCodePart());
		}
		return block.Code;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		int num = itemstack.Attributes.GetInt("slabPlaceMode");
		if (num == 2)
		{
			renderinfo.Transform = renderinfo.Transform.Clone();
			renderinfo.Transform.Rotation.X = -80f;
			renderinfo.Transform.Rotation.Y = 0f;
			renderinfo.Transform.Rotation.Z = -22.5f;
		}
		if (num == 1)
		{
			renderinfo.Transform = renderinfo.Transform.Clone();
			renderinfo.Transform.Rotation.X = 5f;
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetHeldBlockInfo(IWorldAccessor world, ItemSlot inSlot)
	{
		return (EnumSlabPlaceMode)inSlot.Itemstack.Attributes.GetInt("slabPlaceMode") switch
		{
			EnumSlabPlaceMode.Auto => Lang.Get("slab-placemode-auto") + "\n", 
			EnumSlabPlaceMode.Horizontal => Lang.Get("slab-placemode-horizontal") + "\n", 
			EnumSlabPlaceMode.Vertical => Lang.Get("slab-placemode-vertical") + "\n", 
			_ => base.GetHeldBlockInfo(world, inSlot), 
		};
	}
}
