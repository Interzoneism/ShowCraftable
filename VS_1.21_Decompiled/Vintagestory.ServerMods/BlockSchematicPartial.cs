using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class BlockSchematicPartial : BlockSchematicStructure
{
	public List<Entity> EntitiesDecoded;

	private static BlockPos Zero = new BlockPos(0, 0, 0);

	public virtual int PlacePartial(IServerChunk[] chunks, IWorldGenBlockAccessor blockAccessor, IWorldAccessor worldForResolve, int chunkX, int chunkZ, BlockPos startPos, EnumReplaceMode mode, EnumStructurePlacement? structurePlacement, bool replaceMeta, bool resolveImports, Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps = null, int[] replaceWithBlockLayersBlockids = null, Block rockBlock = null, bool disableSurfaceTerrainBlending = false)
	{
		Unpack(worldForResolve.Api);
		Rectanglei rect = new Rectanglei(chunkX * 32, chunkZ * 32, 32, 32);
		if (!rect.IntersectsOrTouches(startPos.X, startPos.Z, startPos.X + SizeX, startPos.Z + SizeZ))
		{
			return 0;
		}
		int num = 0;
		BlockPos blockPos = new BlockPos();
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		if (replaceWithBlockLayersBlockids != null)
		{
			int num6 = blockAccessor.RegionSize / 32;
			IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
			int num7 = chunkX % num6;
			int num8 = chunkZ % num6;
			float num9 = (float)climateMap.InnerSize / (float)num6;
			num2 = climateMap.GetUnpaddedInt((int)((float)num7 * num9), (int)((float)num8 * num9));
			num3 = climateMap.GetUnpaddedInt((int)((float)num7 * num9 + num9), (int)((float)num8 * num9));
			num4 = climateMap.GetUnpaddedInt((int)((float)num7 * num9), (int)((float)num8 * num9 + num9));
			num5 = climateMap.GetUnpaddedInt((int)((float)num7 * num9 + num9), (int)((float)num8 * num9 + num9));
		}
		if (genBlockLayers == null)
		{
			genBlockLayers = worldForResolve.Api.ModLoader.GetModSystem<GenBlockLayers>();
		}
		int num10 = rockBlock?.BlockId ?? chunks[0].MapChunk.TopRockIdMap[495];
		int num11 = -1;
		foreach (uint index in Indices)
		{
			num11++;
			int num12 = (int)(index & 0x3FF);
			int num13 = startPos.X + num12;
			int num14 = (int)((index >> 10) & 0x3FF);
			int num15 = startPos.Z + num14;
			if (!rect.Contains(num13, num15))
			{
				continue;
			}
			int num16 = (int)((index >> 20) & 0x3FF);
			int num17 = startPos.Y + num16;
			int key = BlockIds[num11];
			AssetLocation code = BlockCodes[key];
			Block newBlock = blockAccessor.GetBlock(code);
			if (newBlock == null || (replaceMeta && (newBlock.Id == BlockSchematic.UndergroundBlockId || newBlock.Id == BlockSchematic.AbovegroundBlockId)))
			{
				continue;
			}
			int blockId = ((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId);
			IChunkBlocks data = chunks[num17 / 32].Data;
			int index3d = (num17 % 32 * 32 + num15 % 32) * 32 + num13 % 32;
			if (structurePlacement.HasValue && structurePlacement.GetValueOrDefault() == EnumStructurePlacement.SurfaceRuin && newBlock.Id == BlockSchematic.FillerBlockId)
			{
				uint item = (uint)((num16 - 1 << 20) | (num14 << 10) | num12);
				if (!Indices.Contains(item))
				{
					int index3d2 = ((num17 - 1) % 32 * 32 + num15 % 32) * 32 + num13 % 32;
					Block newBlock2 = blockAccessor.GetBlock(data[index3d2]);
					if (newBlock2.BlockMaterial == EnumBlockMaterial.Soil)
					{
						int replaceLayerBlockId = GetReplaceLayerBlockId(blockAccessor, worldForResolve, replaceWithBlockLayersBlockids, newBlock2.Id, blockPos, num13, num17, num15, 32, num2, num3, num4, num5, num16, num12, num14, num10, ref newBlock2, topBlockOnly: true);
						data[index3d2] = replaceLayerBlockId;
					}
				}
			}
			blockId = GetReplaceLayerBlockId(blockAccessor, worldForResolve, replaceWithBlockLayersBlockids, blockId, blockPos, num13, num17, num15, 32, num2, num3, num4, num5, num16, num12, num14, num10, ref newBlock);
			blockId = GetRocktypeBlockId(blockAccessor, resolvedRockTypeRemaps, blockId, num10, ref newBlock);
			if (!disableSurfaceTerrainBlending && structurePlacement.HasValue && structurePlacement == EnumStructurePlacement.Surface)
			{
				Block block = blockAccessor.GetBlock(data[index3d]);
				if ((newBlock.Replaceable >= 5500 || newBlock.BlockMaterial == EnumBlockMaterial.Plant) && block.Replaceable < newBlock.Replaceable && !newBlock.IsLiquid())
				{
					continue;
				}
			}
			if (newBlock.ForFluidsLayer && index != Indices[num11 - 1])
			{
				data[index3d] = 0;
			}
			if (newBlock.ForFluidsLayer)
			{
				data.SetFluid(index3d, blockId);
			}
			else
			{
				data.SetFluid(index3d, 0);
				data[index3d] = blockId;
			}
			if (newBlock.LightHsv[2] > 0)
			{
				blockPos.Set(num13, num17, num15);
				blockAccessor.ScheduleBlockLightUpdate(blockPos, 0, newBlock.BlockId);
			}
			num++;
		}
		PlaceDecors(blockAccessor, startPos, rect);
		int schematicSeed = worldForResolve.Rand.Next();
		foreach (KeyValuePair<uint, string> blockEntity2 in BlockEntities)
		{
			uint key2 = blockEntity2.Key;
			int num18 = startPos.X + (int)(key2 & 0x3FF);
			int num19 = startPos.Z + (int)((key2 >> 10) & 0x3FF);
			if (!rect.Contains(num18, num19))
			{
				continue;
			}
			int y = startPos.Y + (int)((key2 >> 20) & 0x3FF);
			blockPos.Set(num18, y, num19);
			BlockEntity blockEntity = blockAccessor.GetBlockEntity(blockPos);
			if (blockEntity == null && blockAccessor != null)
			{
				Block block2 = blockAccessor.GetBlock(blockPos, 1);
				if (block2.EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(block2.EntityClass, blockPos);
					blockEntity = blockAccessor.GetBlockEntity(blockPos);
				}
			}
			if (blockEntity != null)
			{
				Block block3 = blockAccessor.GetBlock(blockPos, 1);
				if (block3.EntityClass != worldForResolve.ClassRegistry.GetBlockEntityClass(blockEntity.GetType()))
				{
					worldForResolve.Logger.Warning("Could not import block entity data for schematic at {0}. There is already {1}, expected {2}. Probably overlapping ruins.", blockPos, blockEntity.GetType(), block3.EntityClass);
					continue;
				}
				ITreeAttribute treeAttribute = DecodeBlockEntityData(blockEntity2.Value);
				treeAttribute.SetInt("posx", blockPos.X);
				treeAttribute.SetInt("posy", blockPos.Y);
				treeAttribute.SetInt("posz", blockPos.Z);
				int num20 = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(num18 % 32) / 32f, 0f, 1f), GameMath.Clamp((float)(num19 % 32) / 32f, 0f, 1f), num2, num3, num4, num5);
				Block blockLayerBlock = GetBlockLayerBlock((num20 >> 8) & 0xFF, (num20 >> 16) & 0xFF, blockPos.Y, num10, 0, null, worldForResolve.Blocks, blockPos, -1);
				blockEntity.FromTreeAttributes(treeAttribute, worldForResolve);
				blockEntity.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
				blockEntity.OnPlacementBySchematic(worldForResolve.Api as ICoreServerAPI, blockAccessor, blockPos, resolvedRockTypeRemaps, num10, blockLayerBlock, resolveImports);
			}
		}
		if (EntitiesDecoded == null)
		{
			DecodeEntities(worldForResolve, startPos, worldForResolve as IServerWorldAccessor);
		}
		foreach (Entity entity in EntitiesDecoded)
		{
			if (!rect.Contains((int)entity.Pos.X, (int)entity.Pos.Z))
			{
				continue;
			}
			if (OriginalPos != null)
			{
				BlockPos blockPos2 = entity.WatchedAttributes.GetBlockPos("importOffset", Zero);
				entity.WatchedAttributes.SetBlockPos("importOffset", startPos - OriginalPos + blockPos2);
			}
			if (blockAccessor != null)
			{
				blockAccessor.AddEntity(entity);
				entity.OnInitialized += delegate
				{
					entity.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
				};
			}
			else
			{
				worldForResolve.SpawnEntity(entity);
				entity.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
			}
		}
		return num;
	}

	private int GetReplaceLayerBlockId(IWorldGenBlockAccessor blockAccessor, IWorldAccessor worldForResolve, int[] replaceWithBlockLayersBlockids, int blockId, BlockPos curPos, int posX, int posY, int posZ, int chunksize, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, int dy, int dx, int dz, int rockblockid, ref Block newBlock, bool topBlockOnly = false)
	{
		if (replaceWithBlockLayersBlockids != null && replaceWithBlockLayersBlockids.Contains(blockId))
		{
			curPos.Set(posX, posY, posZ);
			int num = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(posX % chunksize) / (float)chunksize, 0f, 1f), GameMath.Clamp((float)(posZ % chunksize) / (float)chunksize, 0f, 1f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			int forDepth = 0;
			if (dy + 1 < SizeY && !topBlockOnly)
			{
				Block block = blocksByPos[dx, dy + 1, dz];
				if (block != null && block.SideSolid[BlockFacing.DOWN.Index] && block.BlockMaterial != EnumBlockMaterial.Wood && block.BlockMaterial != EnumBlockMaterial.Snow && block.BlockMaterial != EnumBlockMaterial.Ice)
				{
					forDepth = 1;
				}
			}
			blockId = GetBlockLayerBlock((num >> 8) & 0xFF, (num >> 16) & 0xFF, curPos.Y - 1, rockblockid, forDepth, null, worldForResolve.Blocks, curPos, -1)?.Id ?? rockblockid;
			newBlock = blockAccessor.GetBlock(blockId);
		}
		return blockId;
	}

	private static int GetRocktypeBlockId(IWorldGenBlockAccessor blockAccessor, Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps, int blockId, int rockblockid, ref Block newBlock)
	{
		if (resolvedRockTypeRemaps != null && resolvedRockTypeRemaps.TryGetValue(blockId, out var value) && value.TryGetValue(rockblockid, out var value2))
		{
			blockId = value2;
			newBlock = blockAccessor.GetBlock(blockId);
		}
		return blockId;
	}

	private void DecodeEntities(IWorldAccessor worldForResolve, BlockPos startPos, IServerWorldAccessor serverWorldAccessor)
	{
		EntitiesDecoded = new List<Entity>(Entities.Count);
		foreach (string entity2 in Entities)
		{
			using MemoryStream input = new MemoryStream(Ascii85.Decode(entity2));
			BinaryReader binaryReader = new BinaryReader(input);
			string entityClass = binaryReader.ReadString();
			Entity entity = worldForResolve.ClassRegistry.CreateEntity(entityClass);
			entity.Api = worldForResolve.Api;
			entity.FromBytes(binaryReader, isSync: false, serverWorldAccessor.RemappedEntities);
			entity.DidImportOrExport(startPos);
			EntitiesDecoded.Add(entity);
		}
	}

	public override BlockSchematic ClonePacked()
	{
		return new BlockSchematicPartial
		{
			SizeX = SizeX,
			SizeY = SizeY,
			SizeZ = SizeZ,
			OffsetY = base.OffsetY,
			GameVersion = GameVersion,
			FromFileName = FromFileName,
			BlockCodes = new Dictionary<int, AssetLocation>(BlockCodes),
			ItemCodes = new Dictionary<int, AssetLocation>(ItemCodes),
			Indices = new List<uint>(Indices),
			BlockIds = new List<int>(BlockIds),
			BlockEntities = new Dictionary<uint, string>(BlockEntities),
			Entities = new List<string>(Entities),
			DecorIndices = new List<uint>(DecorIndices),
			DecorIds = new List<long>(DecorIds),
			ReplaceMode = ReplaceMode,
			EntranceRotation = EntranceRotation,
			OriginalPos = OriginalPos
		};
	}
}
