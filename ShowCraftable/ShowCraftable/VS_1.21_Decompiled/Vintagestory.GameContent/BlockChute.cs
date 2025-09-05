using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockChute : Block, IBlockItemFlow
{
	public string Type { get; set; }

	public string Side { get; set; }

	public string Vertical { get; set; }

	public string[] PullFaces => Attributes["pullFaces"].AsArray(Array.Empty<string>());

	public string[] PushFaces => Attributes["pushFaces"].AsArray(Array.Empty<string>());

	public string[] AcceptFaces => Attributes["acceptFromFaces"].AsArray(Array.Empty<string>());

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string text = Variant["type"];
		Type = ((text != null) ? string.Intern(text) : null);
		string text2 = Variant["side"];
		Side = ((text2 != null) ? string.Intern(text2) : null);
		string text3 = Variant["vertical"];
		Vertical = ((text3 != null) ? string.Intern(text3) : null);
	}

	public bool HasItemFlowConnectorAt(BlockFacing facing)
	{
		if (!PullFaces.Contains(facing.Code) && !PushFaces.Contains(facing.Code))
		{
			return AcceptFaces.Contains(facing.Code);
		}
		return true;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockChute blockChute = null;
		BlockFacing[] array = OrientForPlacement(world.BlockAccessor, byPlayer, blockSel);
		if (Type == "elbow" || Type == "3way")
		{
			string text = ((array[1] == BlockFacing.UP) ? "down" : "up");
			BlockFacing blockFacing = array[0];
			if (text == "up" && (Type == "3way" || blockFacing == BlockFacing.NORTH || blockFacing == BlockFacing.SOUTH))
			{
				blockFacing = blockFacing.Opposite;
			}
			AssetLocation blockCode = CodeWithVariants(new string[2] { "vertical", "side" }, new string[2] { text, blockFacing.Code });
			blockChute = api.World.GetBlock(blockCode) as BlockChute;
			int num = 0;
			while (blockChute != null && !blockChute.CanStay(world, blockSel.Position))
			{
				if (num >= BlockFacing.HORIZONTALS.Length)
				{
					blockChute = null;
					break;
				}
				blockChute = api.World.GetBlock(CodeWithVariants(new string[2] { "vertical", "side" }, new string[2]
				{
					text,
					BlockFacing.HORIZONTALS[num++].Code
				})) as BlockChute;
			}
		}
		else if (Type == "t")
		{
			string value = ((array[0].Axis == EnumAxis.X) ? "we" : "ns");
			if (blockSel.Face.IsVertical)
			{
				value = "ud-" + array[0].Opposite.Code[0];
			}
			blockChute = api.World.GetBlock(CodeWithVariant("side", value)) as BlockChute;
			if (!blockChute.CanStay(world, blockSel.Position))
			{
				blockChute = api.World.GetBlock(CodeWithVariant("side", (array[0].Axis == EnumAxis.X) ? "we" : "ns")) as BlockChute;
			}
		}
		else if (Type == "straight")
		{
			string value2 = ((array[0].Axis == EnumAxis.X) ? "we" : "ns");
			if (blockSel.Face.IsVertical)
			{
				value2 = "ud";
			}
			blockChute = api.World.GetBlock(CodeWithVariant("side", value2)) as BlockChute;
		}
		else if (Type == "cross")
		{
			string value3 = ((array[0].Axis != EnumAxis.X) ? "ns" : "we");
			if (blockSel.Face.IsVertical)
			{
				value3 = "ground";
			}
			blockChute = api.World.GetBlock(CodeWithVariant("side", value3)) as BlockChute;
		}
		if (blockChute != null && blockChute.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && blockChute.CanStay(world, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(blockChute.BlockId, blockSel.Position);
			world.Logger.Audit("{0} placed a chute at {1}", byPlayer.PlayerName, blockSel.Position);
			return true;
		}
		if (Type == "cross")
		{
			blockChute = api.World.GetBlock(CodeWithVariant("side", "ground")) as BlockChute;
		}
		if (blockChute != null && blockChute.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && blockChute.CanStay(world, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(blockChute.BlockId, blockSel.Position);
			world.Logger.Audit("{0} placed a chute at {1}", byPlayer.PlayerName, blockSel.Position);
			return true;
		}
		return false;
	}

	protected virtual BlockFacing[] OrientForPlacement(IBlockAccessor worldmap, IPlayer player, BlockSelection bs)
	{
		BlockFacing[] array = Block.SuggestedHVOrientation(player, bs);
		BlockPos position = bs.Position;
		BlockFacing blockFacing = null;
		BlockFacing opposite = bs.Face.Opposite;
		BlockFacing vert = null;
		if (opposite.IsHorizontal)
		{
			if (HasConnector(worldmap, position.AddCopy(opposite), bs.Face, out vert))
			{
				blockFacing = opposite;
			}
			else
			{
				opposite = opposite.GetCW();
				if (HasConnector(worldmap, position.AddCopy(opposite), opposite.Opposite, out vert))
				{
					blockFacing = opposite;
				}
				else if (HasConnector(worldmap, position.AddCopy(opposite.Opposite), opposite, out vert))
				{
					blockFacing = opposite.Opposite;
				}
				else if (HasConnector(worldmap, position.AddCopy(bs.Face), bs.Face.Opposite, out vert))
				{
					blockFacing = bs.Face;
				}
			}
			if (Type == "3way" && blockFacing != null)
			{
				opposite = blockFacing.GetCW();
				if (HasConnector(worldmap, position.AddCopy(opposite), opposite.Opposite, out var vert2) && !HasConnector(worldmap, position.AddCopy(opposite.Opposite), opposite, out vert2))
				{
					blockFacing = opposite;
				}
			}
		}
		else
		{
			vert = opposite;
			bool flag = false;
			blockFacing = (HasConnector(worldmap, position.EastCopy(), BlockFacing.WEST, out vert) ? BlockFacing.EAST : null);
			if (HasConnector(worldmap, position.WestCopy(), BlockFacing.EAST, out vert))
			{
				flag = blockFacing != null;
				blockFacing = BlockFacing.WEST;
			}
			if (HasConnector(worldmap, position.NorthCopy(), BlockFacing.SOUTH, out vert))
			{
				flag = blockFacing != null;
				blockFacing = BlockFacing.NORTH;
			}
			if (HasConnector(worldmap, position.SouthCopy(), BlockFacing.NORTH, out vert))
			{
				flag = blockFacing != null;
				blockFacing = BlockFacing.SOUTH;
			}
			if (flag)
			{
				blockFacing = null;
			}
		}
		if (vert == null)
		{
			BlockFacing vert3;
			bool flag2 = HasConnector(worldmap, position.UpCopy(), BlockFacing.DOWN, out vert3);
			bool flag3 = HasConnector(worldmap, position.DownCopy(), BlockFacing.UP, out vert3);
			if (flag2 && !flag3)
			{
				vert = BlockFacing.UP;
			}
			else if (flag3 && !flag2)
			{
				vert = BlockFacing.DOWN;
			}
		}
		if (vert != null)
		{
			array[1] = vert;
		}
		array[0] = blockFacing ?? array[0].Opposite;
		return array;
	}

	protected virtual bool HasConnector(IBlockAccessor ba, BlockPos pos, BlockFacing face, out BlockFacing vert)
	{
		if (ba.GetBlock(pos) is BlockChute blockChute)
		{
			if (blockChute.HasItemFlowConnectorAt(BlockFacing.UP) && !blockChute.HasItemFlowConnectorAt(BlockFacing.DOWN))
			{
				vert = BlockFacing.DOWN;
			}
			else if (blockChute.HasItemFlowConnectorAt(BlockFacing.DOWN) && !blockChute.HasItemFlowConnectorAt(BlockFacing.UP))
			{
				vert = BlockFacing.UP;
			}
			else
			{
				vert = null;
			}
			return blockChute.HasItemFlowConnectorAt(face);
		}
		vert = null;
		return ba.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(pos) != null;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!CanStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool CanStay(IWorldAccessor world, BlockPos pos)
	{
		BlockPos blockPos = new BlockPos();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (PullFaces != null)
		{
			string[] pullFaces = PullFaces;
			int num = 0;
			while (num < pullFaces.Length)
			{
				BlockFacing blockFacing = BlockFacing.FromCode(pullFaces[num]);
				Block block = world.BlockAccessor.GetBlock(blockPos.Set(pos).Add(blockFacing));
				if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos, blockFacing))
				{
					IBlockItemFlow obj = block as IBlockItemFlow;
					if ((obj == null || !obj.HasItemFlowConnectorAt(blockFacing.Opposite)) && blockAccessor.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(blockPos) == null)
					{
						num++;
						continue;
					}
				}
				return true;
			}
		}
		if (PushFaces != null)
		{
			string[] pullFaces = PushFaces;
			int num = 0;
			while (num < pullFaces.Length)
			{
				BlockFacing blockFacing2 = BlockFacing.FromCode(pullFaces[num]);
				Block block2 = world.BlockAccessor.GetBlock(blockPos.Set(pos).Add(blockFacing2));
				if (!block2.CanAttachBlockAt(world.BlockAccessor, this, pos, blockFacing2))
				{
					IBlockItemFlow obj2 = block2 as IBlockItemFlow;
					if ((obj2 == null || !obj2.HasItemFlowConnectorAt(blockFacing2.Opposite)) && blockAccessor.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(blockPos) == null)
					{
						num++;
						continue;
					}
				}
				return true;
			}
		}
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = null;
		if (Type == "elbow" || Type == "3way")
		{
			block = api.World.GetBlock(CodeWithVariants(new string[2] { "vertical", "side" }, new string[2] { "down", "east" }));
		}
		if (Type == "t" || Type == "straight")
		{
			block = api.World.GetBlock(CodeWithVariant("side", "ns"));
		}
		if (Type == "cross")
		{
			block = api.World.GetBlock(CodeWithVariant("side", "ground"));
		}
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return GetDrops(world, pos, null)[0];
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int num = GameMath.Mod(angle / 90, 4);
		switch (Type)
		{
		case "elbow":
		{
			BlockFacing blockFacing = BlockFacing.FromCode(Side);
			return CodeWithVariant("side", BlockFacing.HORIZONTALS[GameMath.Mod(blockFacing.Index + num + 2, 4)].Code.ToLowerInvariant());
		}
		case "3way":
		{
			BlockFacing blockFacing3 = BlockFacing.FromCode(Side);
			return CodeWithVariant("side", BlockFacing.HORIZONTALS[GameMath.Mod(blockFacing3.Index + num, 4)].Code.ToLowerInvariant());
		}
		case "t":
		{
			if ((Side.Equals("ns") || Side.Equals("we")) && (num == 1 || num == 3))
			{
				return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
			}
			BlockFacing blockFacing2 = Side switch
			{
				"ud-n" => BlockFacing.NORTH, 
				"ud-e" => BlockFacing.EAST, 
				"ud-s" => BlockFacing.SOUTH, 
				"ud-w" => BlockFacing.WEST, 
				_ => BlockFacing.NORTH, 
			};
			return CodeWithVariant("side", "ud-" + BlockFacing.HORIZONTALS[GameMath.Mod(blockFacing2.Index + num, 4)].Code.ToLowerInvariant()[0]);
		}
		case "straight":
			if (Side.Equals("ud") || num == 0 || num == 2)
			{
				return Code;
			}
			return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
		case "cross":
			if ((Side.Equals("ns") || Side.Equals("we")) && (num == 1 || num == 3))
			{
				return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
			}
			return Code;
		default:
			return Code;
		}
	}

	public override AssetLocation GetVerticallyFlippedBlockCode()
	{
		return base.GetVerticallyFlippedBlockCode();
	}
}
