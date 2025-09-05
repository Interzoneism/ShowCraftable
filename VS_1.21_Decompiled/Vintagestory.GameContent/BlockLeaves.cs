using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLeaves : BlockWithLeavesMotion
{
	private string climateColorMapInt;

	private string seasonColorMapInt;

	public override string ClimateColorMapForMap => climateColorMapInt;

	public override string SeasonColorMapForMap => seasonColorMapInt;

	public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		base.OnCollectTextures(api, textureDict);
		climateColorMapInt = ClimateColorMap;
		seasonColorMapInt = SeasonColorMap;
		string text = Code.SecondCodePart();
		if (text.StartsWithOrdinal("grown") && !int.TryParse(text.Substring(5), out ExtraColorBits))
		{
			ExtraColorBits = 0;
		}
		if (api.Side == EnumAppSide.Client && SeasonColorMap == null)
		{
			Shape cachedShape = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(Shape.Base);
			climateColorMapInt = ((cachedShape != null) ? cachedShape.Elements[0].ClimateColorMap : null);
			Shape cachedShape2 = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(Shape.Base);
			seasonColorMapInt = ((cachedShape2 != null) ? cachedShape2.Elements[0].SeasonColorMap : null);
		}
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		return offThreadRandom.NextDouble() < 0.15;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		treeAttribute.SetInt("x", pos.X);
		treeAttribute.SetInt("y", pos.Y);
		treeAttribute.SetInt("z", pos.Z);
		world.Api.Event.PushEvent("testForDecay", treeAttribute);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.GetBlock(CodeWithParts("placed", LastCodePart())));
	}

	public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		return false;
	}
}
