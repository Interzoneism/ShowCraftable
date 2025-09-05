using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockForestFloor : Block
{
	protected string[] growthStages = new string[8] { "0", "1", "2", "3", "4", "5", "6", "7" };

	protected int growthLightLevel;

	protected const int chunksize = 32;

	protected float growthChanceOnTick = 0.16f;

	private int mapColorTextureSubId;

	private CompositeTexture grassTex;

	public static int MaxStage { get; set; }

	public int CurrentLevel()
	{
		return MaxStage - (Code.Path[Code.Path.Length - 1] - 48);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api is ICoreClientAPI)
		{
			Block block = api.World.GetBlock(CodeWithParts("7"));
			mapColorTextureSubId = block.Textures["specialSecondTexture"].Baked.TextureSubId;
			Block block2 = api.World.GetBlock(new AssetLocation("soil-low-normal"));
			if (block2.Textures == null || !block2.Textures.TryGetValue("specialSecondTexture", out grassTex))
			{
				grassTex = block2.Textures?.First().Value;
			}
		}
	}

	internal static int[] InitialiseForestBlocks(IWorldAccessor world)
	{
		MaxStage = 8;
		int[] array = new int[MaxStage];
		for (int i = 0; i < MaxStage; i++)
		{
			array[i] = world.GetBlock(new AssetLocation("forestfloor-" + i)).Id;
		}
		return array;
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (offThreadRandom.NextDouble() > (double)growthChanceOnTick)
		{
			return false;
		}
		if (world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
		{
			return false;
		}
		return extra != null;
	}

	protected bool isSmotheringBlock(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos, 2);
		if (block is BlockLakeIce || block.LiquidLevel > 1)
		{
			return true;
		}
		block = world.BlockAccessor.GetBlock(pos);
		if (!block.SideSolid[BlockFacing.DOWN.Index] || !block.SideOpaque[BlockFacing.DOWN.Index])
		{
			return block is BlockLava;
		}
		return true;
	}

	protected Block tryGetBlockForGrowing(IWorldAccessor world, BlockPos pos)
	{
		return null;
	}

	protected Block tryGetBlockForDying(IWorldAccessor world)
	{
		return null;
	}

	protected int getClimateSuitedGrowthStage(IWorldAccessor world, BlockPos pos, ClimateCondition climate)
	{
		return CurrentLevel();
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		float num = (float)Variant["grass"].ToInt() / 7f;
		if (num == 0f)
		{
			return base.GetColorWithoutTint(capi, pos);
		}
		int? num2 = grassTex?.Baked.TextureSubId;
		if (!num2.HasValue)
		{
			return -1;
		}
		int num3 = capi.BlockTextureAtlas.GetAverageColor(num2.Value);
		if (ClimateColorMapResolved != null)
		{
			num3 = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num3, pos.X, pos.Y, pos.Z, flipRb: false);
		}
		return ColorUtil.ColorOverlay(capi.BlockTextureAtlas.GetAverageColor(Textures["up"].Baked.TextureSubId), num3, num);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (facing == BlockFacing.UP)
		{
			return capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, capi.BlockTextureAtlas.GetRandomColor(mapColorTextureSubId, rndIndex), pos.X, pos.Y, pos.Z);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}
}
