using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBamboo : Block, ITreeGenerator, ICustomTreeFellingBehavior
{
	private static Random rand = new Random();

	private bool isSegmentWithLeaves;

	private IBlockAccessor? lockFreeBa;

	private Dictionary<int, int[]> windModeByFlagCount = new Dictionary<int, int[]>();

	private Vec3i windDir = new Vec3i();

	public int MaxPlantHeight { get; private set; }

	private string? domain
	{
		get
		{
			if (!(Code.Domain == "game"))
			{
				return Code.Domain;
			}
			return null;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		MaxPlantHeight = Attributes?["maxPlantHeight"].AsInt(15) ?? 15;
		if (api.Side == EnumAppSide.Client)
		{
			lockFreeBa = api.World.GetLockFreeBlockAccessor();
		}
		if (Variant["type"] == "grown" && Variant["part"] == "segment1")
		{
			(api as ICoreServerAPI)?.RegisterTreeGenerator(AssetLocation.Create(FirstCodePart() + "-grown-" + Variant["color"], domain), this);
		}
		if (RandomDrawOffset > 0)
		{
			JsonObject jsonObject = Attributes?["overrideRandomDrawOffset"];
			if (jsonObject != null && jsonObject.Exists)
			{
				RandomDrawOffset = jsonObject.AsInt(1);
			}
		}
		isSegmentWithLeaves = Variant["part"] == "segment2" || Variant["part"] == "segment3";
	}

	public string? Type()
	{
		return Variant["color"];
	}

	public Block? NextSegment(IBlockAccessor blockAccess)
	{
		string text = Variant["part"];
		int num = text[text.Length - 1].ToString().ToInt() + 1;
		if (num > 3 || num < 1)
		{
			return null;
		}
		return blockAccess.GetBlock(CodeWithVariant("part", "segment" + num));
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams, IRandom random)
	{
		float value = ((treegenParams.otherBlockChance == 0f) ? (3f + (float)rand.NextDouble() * 6f) : ((3f + (float)rand.NextDouble() * 4f) * 3f * 3f));
		int num = GameMath.RoundRandom(rand, value);
		BlockPos blockPos = pos.Copy();
		float num2 = GameMath.Mix(treegenParams.size, 1f, 0.5f);
		num2 *= 1f + (float)rand.NextDouble() * 0.5f;
		while (num-- > 0)
		{
			float centerDist = Math.Max(1f, pos.DistanceTo(blockPos) - 2f);
			GrowStalk(blockAccessor, blockPos.UpCopy(), centerDist, num2, treegenParams.vinesGrowthChance);
			blockPos.Set(pos);
			blockPos.X += rand.Next(8) - 4;
			blockPos.Z += rand.Next(8) - 4;
			bool flag = false;
			for (int num3 = 2; num3 >= -2; num3--)
			{
				if (blockAccessor.GetBlock(blockPos.X, blockPos.Y + num3, blockPos.Z).Fertility > 0)
				{
					blockPos.Y += num3;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
	}

	private void GrowStalk(IBlockAccessor blockAccessor, BlockPos upos, float centerDist, float sizeModifier, float vineGrowthChance)
	{
		Block block = this;
		float num = (float)(8 + rand.Next(5)) * sizeModifier;
		num = Math.Max(1f, num - centerDist);
		int num2 = (int)num;
		int num3 = num2 / 3;
		BlockPos blockPos = upos.Copy();
		Block block2 = blockAccessor.GetBlock(AssetLocation.Create("sapling-" + Variant["color"] + FirstCodePart() + "shoots-free", domain));
		Block block3 = blockAccessor.GetBlock(AssetLocation.Create(FirstCodePart() + "leaves-" + Variant["color"] + "-grown", domain));
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			if (rand.NextDouble() > 0.75)
			{
				BlockPos blockPos2 = blockPos.Set(upos).Add(facing);
				if (blockAccessor.GetBlock(blockPos2).Replaceable >= block2.Replaceable && blockAccessor.GetBlock(blockPos2.X, blockPos2.Y - 1, blockPos2.Z).Fertility > 0 && blockAccessor.GetBlock(blockPos2, 2).BlockId == 0)
				{
					blockAccessor.SetBlock(block2.BlockId, blockPos2);
				}
			}
		}
		if (num2 < 4)
		{
			block = (block as BlockBamboo)?.NextSegment(blockAccessor);
			if (block == null)
			{
				return;
			}
		}
		for (int j = 0; j < num2; j++)
		{
			if (!blockAccessor.GetBlock(upos).IsReplacableBy(block))
			{
				break;
			}
			blockAccessor.SetBlock(block.BlockId, upos);
			if (num3 <= j)
			{
				block = (block as BlockBamboo)?.NextSegment(blockAccessor);
				num3 += num2 / 3;
			}
			if (block == null)
			{
				break;
			}
			if (block.Variant["part"] == "segment3")
			{
				hORIZONTALS = BlockFacing.ALLFACES;
				foreach (BlockFacing blockFacing in hORIZONTALS)
				{
					if (blockFacing == BlockFacing.DOWN)
					{
						continue;
					}
					float num4 = ((blockFacing == BlockFacing.UP) ? 0f : 0.25f);
					if (!(rand.NextDouble() > (double)num4))
					{
						continue;
					}
					blockPos.Set(upos.X + blockFacing.Normali.X, upos.Y + blockFacing.Normali.Y, upos.Z + blockFacing.Normali.Z);
					if (rand.NextDouble() > 0.33)
					{
						BlockPos pos = blockPos.DownCopy();
						if (blockAccessor.GetBlock(pos).Replaceable >= block3.Replaceable)
						{
							blockAccessor.SetBlock(block3.BlockId, pos);
						}
					}
					if (blockAccessor.GetBlock(blockPos).Replaceable < block3.Replaceable)
					{
						continue;
					}
					blockAccessor.SetBlock(block3.BlockId, blockPos);
					BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
					foreach (BlockFacing blockFacing2 in aLLFACES)
					{
						if (rand.NextDouble() > 0.5)
						{
							blockPos.Set(upos.X + blockFacing.Normali.X + blockFacing2.Normali.X, upos.Y + blockFacing.Normali.Y + blockFacing2.Normali.Y, upos.Z + blockFacing.Normali.Z + blockFacing2.Normali.Z);
							if (blockAccessor.GetBlock(blockPos).Replaceable >= block3.Replaceable)
							{
								blockAccessor.SetBlock(block3.BlockId, blockPos);
							}
							break;
						}
					}
				}
			}
			upos.Up();
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (!isSegmentWithLeaves || Variant["part"] != "segment3")
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		if (!Textures.TryGetValue(facing.Code, out var value))
		{
			value = Textures.First().Value;
		}
		if (value?.Baked == null)
		{
			return 0;
		}
		int randomColor = capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", SeasonColorMap, randomColor, pos.X, pos.Y, pos.Z);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		bool enableWind = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) >= 14;
		windDir.X = Math.Sign(GlobalConstants.CurrentWindSpeedClient.X);
		windDir.Z = 0;
		applyWindSwayToMesh(decalMesh, enableWind, pos, windDir);
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		bool enableWind = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
		windDir.X = Math.Sign(GlobalConstants.CurrentWindSpeedClient.X);
		windDir.Z = 0;
		applyWindSwayToMesh(sourceMesh, enableWind, pos, windDir);
	}

	private void applyWindSwayToMesh(MeshData sourceMesh, bool enableWind, BlockPos pos, Vec3i windDir)
	{
		if (lockFreeBa == null)
		{
			return;
		}
		if (!windModeByFlagCount.TryGetValue(sourceMesh.FlagsCount, out int[] value))
		{
			int[] array = (windModeByFlagCount[sourceMesh.FlagsCount] = new int[sourceMesh.FlagsCount]);
			value = array;
			for (int i = 0; i < value.Length; i++)
			{
				value[i] = sourceMesh.Flags[i] & 0x1E000000;
			}
		}
		bool flag = false;
		Block block = lockFreeBa.GetBlock(pos.X, pos.Y - 1, pos.Z);
		if (block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[4])
		{
			flag = true;
		}
		else if (block is BlockBamboo)
		{
			block = lockFreeBa.GetBlock(pos.X + windDir.X, pos.Y - 1, pos.Z + windDir.Z);
			if (block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[3])
			{
				flag = true;
			}
		}
		int j = 1;
		block = lockFreeBa.GetBlock(pos.X + windDir.X, pos.Y, pos.Z + windDir.Z);
		if (block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[3])
		{
			enableWind = false;
		}
		if (enableWind)
		{
			bool flag2 = isSegmentWithLeaves;
			bool flag3 = true;
			for (; j < 8; j++)
			{
				Block blockBelow = api.World.BlockAccessor.GetBlockBelow(pos, j);
				Block block2 = ((blockBelow is BlockBamboo) ? api.World.BlockAccessor.GetBlock(pos.X + windDir.X, pos.Y - j, pos.Z + windDir.Z) : null);
				if ((blockBelow.VertexFlags.WindMode == EnumWindBitMode.NoWind && blockBelow.SideSolid[4]) || (block2 != null && block2.VertexFlags.WindMode == EnumWindBitMode.NoWind && block2.SideSolid[3]))
				{
					break;
				}
				if (block2 == null)
				{
					flag3 = false;
				}
				if (!flag2 && flag3 && blockBelow is BlockBamboo { isSegmentWithLeaves: not false })
				{
					flag2 = true;
				}
			}
			int num = pos.Y;
			while (!flag2 && num - pos.Y < MaxPlantHeight)
			{
				Block blockBelow = api.World.BlockAccessor.GetBlock(pos.X, ++num, pos.Z);
				if (blockBelow is BlockBamboo blockBamboo2)
				{
					flag2 = blockBamboo2.isSegmentWithLeaves;
					continue;
				}
				if (blockBelow is BlockWithLeavesMotion)
				{
					flag2 = true;
				}
				break;
			}
			if (!flag2)
			{
				enableWind = false;
			}
		}
		int num2 = 33554431;
		int verticesCount = sourceMesh.VerticesCount;
		if (!enableWind)
		{
			for (int k = 0; k < verticesCount; k++)
			{
				sourceMesh.Flags[k] &= num2;
			}
			return;
		}
		for (int l = 0; l < verticesCount; l++)
		{
			int num3 = sourceMesh.Flags[l] & num2;
			float num4 = sourceMesh.xyz[l * 3 + 1];
			if (num4 > 0.05f || !flag)
			{
				num3 |= value[l] | (GameMath.Clamp(j + ((num4 < 0.95f) ? (-1) : 0), 0, 7) << 29);
			}
			sourceMesh.Flags[l] = num3;
		}
	}

	public EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex)
	{
		return EnumTreeFellingBehavior.ChopSpreadVertical;
	}
}
