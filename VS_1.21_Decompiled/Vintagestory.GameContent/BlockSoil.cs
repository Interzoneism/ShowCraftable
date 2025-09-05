using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockSoil : BlockWithGrassOverlay
{
	protected List<AssetLocation> tallGrassCodes = new List<AssetLocation>();

	protected string[] growthStages = new string[4] { "none", "verysparse", "sparse", "normal" };

	protected string[] tallGrassGrowthStages = new string[6] { "veryshort", "short", "mediumshort", "medium", "tall", "verytall" };

	protected int growthLightLevel;

	protected string growthBlockLayer;

	protected float tallGrassGrowthChance;

	protected BlockLayerConfig blocklayerconfig;

	protected const int chunksize = 32;

	protected float growthChanceOnTick = 0.16f;

	public bool growOnlyWhereRainfallExposed;

	private GenBlockLayers genBlockLayers;

	private const int FullyGrownStage = 3;

	protected int currentStage;

	protected virtual int MaxStage => 3;

	private int GrowthStage(string stage)
	{
		return stage switch
		{
			"normal" => 3, 
			"sparse" => 2, 
			"verysparse" => 1, 
			_ => 0, 
		};
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		growthLightLevel = ((Attributes?["growthLightLevel"] != null) ? Attributes["growthLightLevel"].AsInt(7) : 7);
		growthBlockLayer = Attributes?["growthBlockLayer"]?.AsString("l1soilwithgrass");
		tallGrassGrowthChance = ((Attributes?["tallGrassGrowthChance"] != null) ? Attributes["tallGrassGrowthChance"].AsFloat(0.3f) : 0.3f);
		growthChanceOnTick = ((Attributes?["growthChanceOnTick"] != null) ? Attributes["growthChanceOnTick"].AsFloat(0.33f) : 0.33f);
		growOnlyWhereRainfallExposed = Attributes?["growOnlyWhereRainfallExposed"] != null && Attributes["growOnlyWhereRainfallExposed"].AsBool();
		tallGrassCodes.Add(new AssetLocation("tallgrass-veryshort-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-short-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-mediumshort-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-medium-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-tall-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-verytall-free"));
		if (api.Side == EnumAppSide.Server)
		{
			(api as ICoreServerAPI).Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
			{
				genBlockLayers = api.ModLoader.GetModSystem<GenBlockLayers>();
				blocklayerconfig = genBlockLayers.blockLayerConfig;
			});
		}
		currentStage = GrowthStage(Variant["grasscoverage"]);
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		base.OnServerGameTick(world, pos, extra);
		GrassTick grassTick = extra as GrassTick;
		world.BlockAccessor.ExchangeBlock(grassTick.Grass.BlockId, pos);
		BlockPos pos2 = pos.UpCopy();
		if (grassTick.TallGrass != null && world.BlockAccessor.GetBlock(pos2).BlockId == 0)
		{
			world.BlockAccessor.SetBlock(grassTick.TallGrass.BlockId, pos2);
		}
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (offThreadRandom.NextDouble() > (double)growthChanceOnTick)
		{
			return false;
		}
		if (growOnlyWhereRainfallExposed && world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
		{
			return false;
		}
		bool flag = false;
		Block block = null;
		BlockPos blockPos = pos.UpCopy();
		bool flag2 = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) < growthLightLevel && (world.BlockAccessor.GetLightLevel(blockPos, EnumLightLevelType.MaxLight) < growthLightLevel || world.BlockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.DOWN.Index]);
		bool flag3 = isSmotheringBlock(world, blockPos);
		int overheatingAmount = 0;
		world.BlockAccessor.WalkBlocks(pos.AddCopy(-3, 0, -3), pos.AddCopy(3, 1, 3), delegate(Block block2, int x, int y, int z)
		{
			if (block2.Attributes != null)
			{
				overheatingAmount = Math.Max(overheatingAmount, block2.Attributes["killPlantRadius"].AsInt() - Math.Max(0, (int)pos.DistanceTo(x, y, z) - 1));
			}
		});
		bool flag4 = (overheatingAmount >= 1 && currentStage == 3) || (overheatingAmount >= 2 && currentStage == 2) || (overheatingAmount >= 3 && currentStage == 1);
		if ((flag2 || flag3 || flag4) && currentStage > 0)
		{
			block = tryGetBlockForDying(world);
		}
		else if (overheatingAmount <= 0 && !flag3 && !flag2 && currentStage < MaxStage)
		{
			flag = true;
			block = tryGetBlockForGrowing(world, pos);
		}
		if (block != null)
		{
			extra = new GrassTick
			{
				Grass = block,
				TallGrass = (flag ? getTallGrassBlock(world, blockPos, offThreadRandom) : null)
			};
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
		ClimateCondition climateAt = GetClimateAt(world.BlockAccessor, pos);
		int climateSuitedGrowthStage;
		if (currentStage != MaxStage && (climateSuitedGrowthStage = getClimateSuitedGrowthStage(world, pos, climateAt)) != currentStage)
		{
			int num = GameMath.Clamp(climateSuitedGrowthStage, currentStage - 1, currentStage + 1);
			return world.GetBlock(CodeWithParts(growthStages[num]));
		}
		return null;
	}

	private ClimateCondition GetClimateAt(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (genBlockLayers == null)
		{
			return blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		}
		double distx;
		double distz;
		int num = genBlockLayers.RandomlyAdjustPosition(pos, out distx, out distz);
		int num2 = (int)Math.Round(distx, 0);
		int num3 = (int)Math.Round(distz, 0);
		pos.Add(num2, num, num3);
		ClimateCondition climateAt = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		pos.Add(-num2, -num, -num3);
		return climateAt;
	}

	protected Block tryGetBlockForDying(IWorldAccessor world)
	{
		int num = Math.Max(currentStage - 1, 0);
		if (num != currentStage)
		{
			return world.GetBlock(CodeWithParts(growthStages[num]));
		}
		return null;
	}

	protected Block getTallGrassBlock(IWorldAccessor world, BlockPos abovePos, Random offthreadRandom)
	{
		if (offthreadRandom.NextDouble() > (double)tallGrassGrowthChance)
		{
			return null;
		}
		Block block = world.BlockAccessor.GetBlock(abovePos);
		int index = Math.Min(((block.FirstCodePart() == "tallgrass") ? Array.IndexOf(tallGrassGrowthStages, block.Variant["tallgrass"]) : 0) + 1 + offthreadRandom.Next(3), tallGrassGrowthStages.Length - 1);
		return world.GetBlock(tallGrassCodes[index]);
	}

	protected bool canGrassGrowHere(IWorldAccessor world, BlockPos pos)
	{
		if (currentStage != 3 && world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) >= growthLightLevel && !world.BlockAccessor.IsSideSolid(pos.X, pos.Y + 1, pos.Z, BlockFacing.DOWN))
		{
			return getClimateSuitedGrowthStage(world, pos, GetClimateAt(world.BlockAccessor, pos)) != currentStage;
		}
		return false;
	}

	protected int getClimateSuitedGrowthStage(IWorldAccessor world, BlockPos pos, ClimateCondition climate)
	{
		if (climate == null)
		{
			return currentStage;
		}
		IMapChunk mapChunkAtBlockPos = world.BlockAccessor.GetMapChunkAtBlockPos(pos);
		if (mapChunkAtBlockPos == null)
		{
			return 0;
		}
		int mapSizeY = ((ICoreServerAPI)world.Api).WorldManager.MapSizeY;
		float blockLayerTransitionSize = blocklayerconfig.blockLayerTransitionSize;
		int firstBlockId = mapChunkAtBlockPos.TopRockIdMap[pos.Z % 32 * 32 + pos.X % 32];
		double num = (double)GameMath.MurmurHash3(pos.X, 1, pos.Z) / 2147483647.0;
		num = (num + 1.0) * (double)blockLayerTransitionSize;
		int posY = pos.Y + (int)(genBlockLayers.distort2dx.Noise(-pos.X, -pos.Z) / 4.0);
		for (int i = 0; i < blocklayerconfig.Blocklayers.Length; i++)
		{
			BlockLayer blockLayer = blocklayerconfig.Blocklayers[i];
			float num2 = blockLayer.CalcTrfDistance(climate.Temperature, climate.WorldgenRainfall, climate.Fertility);
			float num3 = blockLayer.CalcYDistance(posY, mapSizeY);
			if ((double)(num2 + num3) <= num)
			{
				int blockId = blockLayer.GetBlockId(num, climate.Temperature, climate.WorldgenRainfall, climate.Fertility, firstBlockId, pos, mapSizeY);
				if (world.Blocks[blockId] is BlockSoil blockSoil)
				{
					return blockSoil.currentStage;
				}
			}
		}
		return 0;
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		return base.GetColor(capi, pos);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (facing == BlockFacing.UP && Variant["grasscoverage"] != "none")
		{
			return capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, capi.BlockTextureAtlas.GetRandomColor(Textures["specialSecondTexture"].Baked.TextureSubId, rndIndex), pos.X, pos.Y, pos.Z);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (!Variant.ContainsKey("fertility"))
		{
			return;
		}
		string text = inSlot.Itemstack.Block.Variant["fertility"];
		Block block = world.GetBlock(new AssetLocation("farmland-dry-" + text));
		if (block != null)
		{
			int fertility = block.Fertility;
			if (fertility > 0)
			{
				dsc.Append(Lang.Get("Fertility when tilled:") + " " + fertility + "%\n");
			}
		}
	}
}
