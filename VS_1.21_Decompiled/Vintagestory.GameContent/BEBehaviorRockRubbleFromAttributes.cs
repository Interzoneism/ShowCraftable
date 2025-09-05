using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BEBehaviorRockRubbleFromAttributes : BEBehaviorShapeFromAttributes, IMaterialExchangeable
{
	public BEBehaviorRockRubbleFromAttributes(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	public bool ExchangeWith(ItemSlot fromSlot, ItemSlot toSlot)
	{
		string rock = toSlot.Itemstack.Collectible.Variant["rock"];
		if (fromSlot.Itemstack.Collectible.Code == base.Block.Code)
		{
			Type = typeWithRockType(rock);
			Blockentity.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		string text = api.World.Blocks[centerrockblockid].Variant["rock"];
		if (text != null && Type != null)
		{
			Type = typeWithRockType(text);
		}
	}

	private string typeWithRockType(string rock)
	{
		string[] array = Type.Split('-');
		if (array.Length < 3)
		{
			return Type;
		}
		return array[0] + "-" + array[1] + "-" + rock;
	}
}
