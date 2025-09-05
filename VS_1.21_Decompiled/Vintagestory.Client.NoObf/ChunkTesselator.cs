using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ChunkTesselator : IMeshPoolSupplier
{
	public const int LODPOOLS = 4;

	internal int[] TextureIdToReturnNum;

	private const int chunkSize = 32;

	internal ClientMain game;

	internal readonly Block[] currentChunkBlocksExt;

	internal readonly Block[] currentChunkFluidBlocksExt;

	internal readonly int[] currentChunkRgbsExt;

	internal byte[] currentChunkDraw32;

	internal byte[] currentChunkDrawFluids;

	internal int[] currentClimateRegionMap;

	internal float currentOceanityMapTL;

	internal float currentOceanityMapTR;

	internal float currentOceanityMapBL;

	internal float currentOceanityMapBR;

	internal bool started;

	internal int mapsizex;

	internal int mapsizey;

	internal int mapsizez;

	internal int mapsizeChunksx;

	internal int mapsizeChunksy;

	internal int mapsizeChunksz;

	private int quantityAtlasses;

	internal bool[] isPartiallyTransparent;

	internal bool[] isLiquidBlock;

	internal MeshData[][][] currentModeldataByRenderPassByLodLevel;

	internal MeshData[][][] centerModeldataByRenderPassByLodLevel;

	internal MeshData[][][] edgeModeldataByRenderPassByLodLevel;

	private int[][] fastBlockTextureSubidsByBlockAndFace;

	private TesselatedChunkPart[] ret;

	private TesselatedChunkPart[] emptyParts = Array.Empty<TesselatedChunkPart>();

	internal static readonly float[] waterLevels = new float[9] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f, 1f };

	private int seaLevel;

	internal int regionSize;

	internal const int extChunkSize = 34;

	internal const int maxX = 31;

	internal bool AoAndSmoothShadows;

	internal Block[] blocksFast;

	internal readonly TCTCache vars;

	private ColorUtil.LightUtil lightConverter;

	private readonly IBlockTesselator[] blockTesselators = new IBlockTesselator[40];

	public JsonTesselator jsonTesselator;

	internal ITesselatorAPI offthreadTesselator;

	internal readonly ClientChunk[] chunksNearby;

	internal readonly ClientChunkData[] chunkdatasNearby;

	public object ReloadLock = new object();

	private ColorMapData defaultColorMapData;

	private float[][] decorRotationMatrices = new float[24][];

	private bool lightsGo;

	private bool blockTexturesGo;

	private EnumChunkRenderPass[] passes = (EnumChunkRenderPass[])Enum.GetValues(typeof(EnumChunkRenderPass));

	private BlockPos tmpPos = new BlockPos();

	public ChunkTesselator(ClientMain game)
	{
		this.game = game;
		vars = new TCTCache(this);
		int num = 39304;
		currentChunkRgbsExt = new int[num];
		currentChunkBlocksExt = new Block[num];
		currentChunkFluidBlocksExt = new Block[num];
		chunksNearby = new ClientChunk[27];
		chunkdatasNearby = new ClientChunkData[27];
		blockTesselators[1] = new CubeTesselator(0.125f);
		blockTesselators[2] = new CubeTesselator(0.25f);
		blockTesselators[3] = new CubeTesselator(0.375f);
		blockTesselators[4] = new CubeTesselator(0.5f);
		blockTesselators[5] = new CubeTesselator(0.625f);
		blockTesselators[6] = new CubeTesselator(0.75f);
		blockTesselators[7] = new CubeTesselator(0.875f);
		blockTesselators[8] = (jsonTesselator = new JsonTesselator());
		blockTesselators[10] = new CubeTesselator(1f);
		blockTesselators[11] = new CrossTesselator();
		blockTesselators[12] = new CubeTesselator(1f);
		blockTesselators[13] = new LiquidTesselator(this);
		blockTesselators[14] = new TopsoilTesselator();
		blockTesselators[15] = new CrossAndSnowlayerTesselator(0.125f);
		blockTesselators[18] = new CrossAndSnowlayerTesselator(0.25f);
		blockTesselators[19] = new CrossAndSnowlayerTesselator(0.375f);
		blockTesselators[20] = new CrossAndSnowlayerTesselator(0.5f);
		blockTesselators[21] = new SurfaceLayerTesselator();
		blockTesselators[16] = new JsonAndLiquidTesselator(this);
		blockTesselators[17] = new JsonAndSnowLayerTesselator();
		ClientSettings.Inst.AddWatcher<bool>("smoothShadows", OnSmoothShadowsChanged);
		AoAndSmoothShadows = ClientSettings.SmoothShadows;
		SetUpDecorRotationMatrices();
	}

	private void OnSmoothShadowsChanged(bool newValue)
	{
		AoAndSmoothShadows = ClientSettings.SmoothShadows;
	}

	public void LightlevelsReceived()
	{
		lightsGo = true;
		Start();
	}

	public void BlockTexturesLoaded()
	{
		blockTexturesGo = true;
		Start();
	}

	public void Start()
	{
		if (!lightsGo || !blockTexturesGo)
		{
			return;
		}
		lightConverter = new ColorUtil.LightUtil(game.WorldMap.BlockLightLevels, game.WorldMap.SunLightLevels, game.WorldMap.hueLevels, game.WorldMap.satLevels);
		regionSize = game.WorldMap.RegionSize;
		seaLevel = ClientWorldMap.seaLevel;
		vars.Start(game);
		blocksFast = (game.Blocks as BlockList).BlocksFast;
		for (int i = 0; i < blocksFast.Length; i++)
		{
			if (blocksFast[i] == null)
			{
				game.Logger.Debug("BlockList null at position " + i);
				blocksFast[i] = blocksFast[0];
			}
		}
		offthreadTesselator = game.TesselatorManager.GetNewTesselator();
		TileSideEnum.MoveIndex[0] = -34;
		TileSideEnum.MoveIndex[1] = 1;
		TileSideEnum.MoveIndex[2] = 34;
		TileSideEnum.MoveIndex[3] = -1;
		TileSideEnum.MoveIndex[4] = 1156;
		TileSideEnum.MoveIndex[5] = -1156;
		currentChunkDraw32 = new byte[32768];
		currentChunkDrawFluids = new byte[32768];
		mapsizex = game.WorldMap.MapSizeX;
		mapsizey = game.WorldMap.MapSizeY;
		mapsizez = game.WorldMap.MapSizeZ;
		mapsizeChunksx = mapsizex / 32;
		mapsizeChunksy = mapsizey / 32;
		mapsizeChunksz = mapsizez / 32;
		Array values = Enum.GetValues(typeof(EnumChunkRenderPass));
		centerModeldataByRenderPassByLodLevel = new MeshData[4][][];
		edgeModeldataByRenderPassByLodLevel = new MeshData[4][][];
		for (int j = 0; j < 4; j++)
		{
			centerModeldataByRenderPassByLodLevel[j] = new MeshData[values.Length][];
			edgeModeldataByRenderPassByLodLevel[j] = new MeshData[values.Length][];
		}
		ReloadTextures();
		int count = game.Blocks.Count;
		isPartiallyTransparent = new bool[count];
		isLiquidBlock = new bool[count];
		for (int k = 0; k < count; k++)
		{
			Block block = game.Blocks[k];
			isPartiallyTransparent[k] = !block.AllSidesOpaque;
			isLiquidBlock[k] = block.MatterState == EnumMatterState.Liquid;
		}
		ClientEventManager eventManager = game.eventManager;
		if (eventManager != null)
		{
			eventManager.OnReloadTextures += ReloadTextures;
		}
		started = true;
	}

	public int[] RuntimeCreateNewBlockTextureAtlas(int textureId)
	{
		UpdateForAtlasses(TextureIdToReturnNum.Append(textureId));
		return TextureIdToReturnNum;
	}

	public void ReloadTextures()
	{
		List<LoadedTexture> atlasTextures = game.BlockAtlasManager.AtlasTextures;
		int count = atlasTextures.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = atlasTextures[i].TextureId;
		}
		UpdateForAtlasses(array);
	}

	private void UpdateForAtlasses(int[] textureIDs)
	{
		lock (ReloadLock)
		{
			quantityAtlasses = textureIDs.Length;
			TextureIdToReturnNum = textureIDs;
			fastBlockTextureSubidsByBlockAndFace = game.FastBlockTextureSubidsByBlockAndFace;
			Array values = Enum.GetValues(typeof(EnumChunkRenderPass));
			ret = new TesselatedChunkPart[values.Length * quantityAtlasses];
			foreach (EnumChunkRenderPass item in values)
			{
				for (int i = 0; i < 4; i++)
				{
					MeshData[][] array = centerModeldataByRenderPassByLodLevel[i];
					MeshData[][] array2 = edgeModeldataByRenderPassByLodLevel[i];
					array[(int)item] = new MeshData[quantityAtlasses];
					array2[(int)item] = new MeshData[quantityAtlasses];
					InitialiseRenderPassPools(array[(int)item], item, 1024);
					InitialiseRenderPassPools(array2[(int)item], item, 1024);
				}
			}
		}
	}

	private void InitialiseRenderPassPools(MeshData[] renderPassModeldata, EnumChunkRenderPass pass, int startCapacity)
	{
		for (int i = 0; i < quantityAtlasses; i++)
		{
			renderPassModeldata[i] = new MeshData();
			renderPassModeldata[i].xyz = new float[startCapacity * 3];
			renderPassModeldata[i].Uv = new float[startCapacity * 2];
			renderPassModeldata[i].Rgba = new byte[startCapacity * 4];
			renderPassModeldata[i].Flags = new int[startCapacity];
			renderPassModeldata[i].Indices = new int[startCapacity];
			renderPassModeldata[i].VerticesMax = startCapacity;
			renderPassModeldata[i].IndicesMax = startCapacity;
			if (pass == EnumChunkRenderPass.Liquid)
			{
				renderPassModeldata[i].CustomFloats = new CustomMeshDataPartFloat(startCapacity * 2);
				renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity * 2);
				continue;
			}
			renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity);
			if (pass == EnumChunkRenderPass.TopSoil)
			{
				renderPassModeldata[i].CustomShorts = new CustomMeshDataPartShort(startCapacity * 2);
			}
		}
	}

	public bool BeginProcessChunk(int chunkX, int chunkY, int chunkZ, ClientChunk chunk, bool skipChunkCenter)
	{
		if (!started)
		{
			throw new Exception("not started");
		}
		vars.aoAndSmoothShadows = AoAndSmoothShadows;
		vars.xMin = 32f;
		vars.xMax = 0f;
		vars.yMin = 32f;
		vars.yMax = 0f;
		vars.zMin = 32f;
		vars.zMax = 0f;
		vars.SetDimension(chunkY / 1024);
		try
		{
			BuildExtendedChunkData(chunk, chunkX, chunkY, chunkZ, chunkX < 1 || chunkZ < 1 || chunkX >= game.WorldMap.ChunkMapSizeX - 1 || chunkZ >= game.WorldMap.ChunkMapSizeZ - 1, skipChunkCenter);
		}
		catch (ThreadAbortException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			if (game.Platform.IsShuttingDown)
			{
				return false;
			}
			throw new Exception($"Exception thrown when trying to tesselate chunk {chunkX}/{chunkY}/{chunkZ}. Exception: {ex2}");
		}
		bool num = CalculateVisibleFaces_Fluids(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32) | CalculateVisibleFaces(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32);
		if (num)
		{
			currentClimateRegionMap = game.WorldMap.LoadOrCreateLerpedClimateMap(chunkX, chunkZ);
			float[] array = game.WorldMap.LoadOceanityCorners(chunkX, chunkZ);
			if (array != null)
			{
				currentOceanityMapTL = array[0];
				currentOceanityMapTR = array[1];
				currentOceanityMapBL = array[2];
				currentOceanityMapBR = array[3];
			}
		}
		return num;
	}

	public int NowProcessChunk(int chunkX, int chunkY, int chunkZ, TesselatedChunk tessChunk, bool skipChunkCenter)
	{
		if (chunkX < 0 || chunkY < 0 || chunkZ < 0 || (chunkY < 1024 && (chunkX >= mapsizeChunksx || chunkZ >= mapsizeChunksz)))
		{
			return 0;
		}
		if (!BeginProcessChunk(chunkX, chunkY, chunkZ, tessChunk.chunk, skipChunkCenter))
		{
			if (!skipChunkCenter)
			{
				tessChunk.centerParts = emptyParts;
			}
			tessChunk.edgeParts = emptyParts;
			return 0;
		}
		tmpPos.dimension = chunkY / 1024;
		Dictionary<int, Block> dictionary = null;
		if (tessChunk.chunk.Decors != null)
		{
			dictionary = new Dictionary<int, Block>();
			lock (tessChunk.chunk.Decors)
			{
				CullVisibleFacesWithDecor(tessChunk.chunk.Decors, dictionary);
			}
		}
		lock (ReloadLock)
		{
			vars.textureAtlasPositionsByTextureSubId = game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId;
			EnumChunkRenderPass[] array = passes;
			foreach (EnumChunkRenderPass enumChunkRenderPass in array)
			{
				for (int j = 0; j < quantityAtlasses; j++)
				{
					for (int k = 0; k < 4; k++)
					{
						edgeModeldataByRenderPassByLodLevel[k][(int)enumChunkRenderPass][j].Clear();
						if (!skipChunkCenter)
						{
							centerModeldataByRenderPassByLodLevel[k][(int)enumChunkRenderPass][j].Clear();
						}
					}
				}
			}
			try
			{
				if (skipChunkCenter)
				{
					BuildBlockPolygons_EdgeOnly(chunkX, chunkY, chunkZ);
				}
				else
				{
					BuildBlockPolygons(chunkX, chunkY, chunkZ);
				}
			}
			catch (Exception e)
			{
				game.Logger.Error(e);
			}
			if (dictionary != null)
			{
				vars.blockEntitiesOfChunk = null;
				BuildDecorPolygons(chunkX, chunkY, chunkZ, dictionary, skipChunkCenter);
			}
			int num = 0;
			if (!skipChunkCenter)
			{
				num += populateTesselatedChunkPart(centerModeldataByRenderPassByLodLevel, out tessChunk.centerParts);
			}
			num += populateTesselatedChunkPart(edgeModeldataByRenderPassByLodLevel, out tessChunk.edgeParts);
			tessChunk.SetBounds(vars.xMin, vars.xMax, vars.yMin, vars.yMax, vars.zMin, vars.zMax);
			return num;
		}
	}

	private int populateTesselatedChunkPart(MeshData[][][] modeldataByRenderPassByLodLevel, out TesselatedChunkPart[] tessChunkParts)
	{
		int num = 0;
		int num2 = 0;
		MeshData.Recycler.DoRecycling();
		EnumChunkRenderPass[] array = passes;
		foreach (EnumChunkRenderPass enumChunkRenderPass in array)
		{
			for (int j = 0; j < quantityAtlasses; j++)
			{
				MeshData meshData = modeldataByRenderPassByLodLevel[0][(int)enumChunkRenderPass][j];
				MeshData meshData2 = modeldataByRenderPassByLodLevel[1][(int)enumChunkRenderPass][j];
				MeshData meshData3 = modeldataByRenderPassByLodLevel[2][(int)enumChunkRenderPass][j];
				MeshData meshData4 = modeldataByRenderPassByLodLevel[3][(int)enumChunkRenderPass][j];
				int verticesCount = meshData.VerticesCount;
				int verticesCount2 = meshData2.VerticesCount;
				int verticesCount3 = meshData3.VerticesCount;
				int verticesCount4 = meshData4.VerticesCount;
				if (verticesCount + verticesCount2 + verticesCount3 + verticesCount4 > 0)
				{
					ret[num++] = new TesselatedChunkPart
					{
						atlasNumber = j,
						modelDataLod0 = ((verticesCount == 0) ? null : meshData.CloneUsingRecycler()),
						modelDataLod1 = ((verticesCount2 == 0) ? null : meshData2.CloneUsingRecycler()),
						modelDataNotLod2Far = ((verticesCount3 == 0) ? null : meshData3.CloneUsingRecycler()),
						modelDataLod2Far = ((verticesCount4 == 0) ? null : meshData4.CloneUsingRecycler()),
						pass = enumChunkRenderPass
					};
					num2 += verticesCount + verticesCount2;
				}
			}
		}
		if (num > 0)
		{
			Array.Copy(ret, tessChunkParts = new TesselatedChunkPart[num], num);
			for (int k = 0; k < num; k++)
			{
				ret[k] = null;
			}
		}
		else
		{
			tessChunkParts = emptyParts;
		}
		return num2;
	}

	public bool CalculateVisibleFaces(bool skipChunkCenter, int baseX, int baseY, int baseZ)
	{
		byte[] array = currentChunkDraw32;
		int num = 0;
		Block block = blocksFast[0];
		for (int i = 0; i < 32; i++)
		{
			int num2 = i * 32 * 32;
			for (int j = 0; j < 32; j++)
			{
				int num3 = (i * 34 + j) * 34 + 1191;
				int num4 = i * (i ^ 0x1F) * j * (j ^ 0x1F);
				for (int k = 0; k < 32; k++)
				{
					Block block2;
					if ((block2 = currentChunkBlocksExt[num3 + k]) == block)
					{
						array[num2 + k] = 0;
					}
					else
					{
						if (skipChunkCenter && k * (k ^ 0x1F) * num4 != 0)
						{
							continue;
						}
						num = num3 + k;
						int num5 = 0;
						EnumFaceCullMode faceCullMode = block2.FaceCullMode;
						SmallBoolArray sideOpaque = block2.SideOpaque;
						int num6 = 5;
						do
						{
							num5 <<= 1;
							Block block3 = currentChunkBlocksExt[num + TileSideEnum.MoveIndex[num6]];
							int opposite = TileSideEnum.GetOpposite(num6);
							bool flag = block3.SideOpaque[opposite];
							if (num6 == 4 && block3.DrawType == EnumDrawType.JSONAndSnowLayer && flag && !block2.AllowSnowCoverage(game, tmpPos.Set(baseX + k, baseY + i, baseZ + j)))
							{
								flag = false;
							}
							switch (faceCullMode)
							{
							case EnumFaceCullMode.Default:
								if (!flag || (!sideOpaque[num6] && block2.DrawType != EnumDrawType.JSON && block2.DrawType != EnumDrawType.JSONAndSnowLayer))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.NeverCull:
								num5++;
								break;
							case EnumFaceCullMode.Merge:
								if (block3 != block2 && (!sideOpaque[num6] || !flag))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.Collapse:
								if ((block3 == block2 && (num6 == 4 || num6 == 0 || num6 == 3)) || (block3 != block2 && (!sideOpaque[num6] || !flag)))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.MergeMaterial:
								if (!block2.SideSolid[num6] || (block3.BlockMaterial != block2.BlockMaterial && (!sideOpaque[num6] || !flag)) || !block3.SideSolid[opposite])
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.CollapseMaterial:
								if (block3.BlockMaterial == block2.BlockMaterial)
								{
									if (num6 == 0 || num6 == 3)
									{
										num5++;
									}
								}
								else if (!flag || (num6 < 4 && !sideOpaque[num6]))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.Liquid:
							{
								if (block3.BlockMaterial == block2.BlockMaterial)
								{
									break;
								}
								if (num6 == 4)
								{
									num5++;
									break;
								}
								FastVec3i fastVec3i = TileSideEnum.OffsetByTileSide[num6];
								if (!block3.SideIsSolid(tmpPos.Set(baseX + k + fastVec3i.X, baseY + i + fastVec3i.Y, baseZ + j + fastVec3i.Z), opposite))
								{
									num5++;
								}
								break;
							}
							case EnumFaceCullMode.Callback:
								if (!block2.ShouldMergeFace(num6, block3, num2 + k))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.MergeSnowLayer:
							{
								int num7 = num + TileSideEnum.MoveIndex[num6] - 1156;
								if (num6 == 4 || (!flag && (num6 == 5 || block3.GetSnowLevel(null) < block2.GetSnowLevel(null))) || (block3.DrawType == EnumDrawType.JSONAndSnowLayer && num7 >= 0 && num7 < currentChunkBlocksExt.Length && !currentChunkBlocksExt[num7].AllowSnowCoverage(game, tmpPos.Set(baseX + k, baseY + i, baseZ + j))))
								{
									num5++;
								}
								break;
							}
							case EnumFaceCullMode.FlushExceptTop:
								if (num6 == 4 || ((num6 == 5 || block3 != block2) && !flag))
								{
									num5++;
								}
								break;
							case EnumFaceCullMode.Stairs:
								if ((!flag && (block3 != block2 || block2.SideOpaque[num6])) || num6 == 4)
								{
									num5++;
								}
								break;
							}
						}
						while (num6-- != 0);
						if (block2.DrawType == EnumDrawType.JSONAndWater)
						{
							num5 |= 0x40;
						}
						array[num2 + k] = (byte)num5;
					}
				}
				num2 += 32;
			}
		}
		return num > 0;
	}

	public bool CalculateVisibleFaces_Fluids(bool skipChunkCenter, int baseX, int baseY, int baseZ)
	{
		byte[] array = currentChunkDrawFluids;
		int num = 0;
		Block block = blocksFast[0];
		for (int i = 0; i < 32; i++)
		{
			int num2 = i * 32 * 32;
			for (int j = 0; j < 32; j++)
			{
				int num3 = (i * 34 + j) * 34 + 1191;
				int num4 = i * (i ^ 0x1F) * j * (j ^ 0x1F);
				for (int k = 0; k < 32; k++)
				{
					Block block2;
					if ((block2 = currentChunkFluidBlocksExt[num3 + k]) == block)
					{
						array[num2 + k] = 0;
					}
					else
					{
						if (skipChunkCenter && k * (k ^ 0x1F) * num4 != 0)
						{
							continue;
						}
						num = num3 + k;
						int num5 = 0;
						EnumFaceCullMode faceCullMode = block2.FaceCullMode;
						int num6 = 5;
						if (faceCullMode == EnumFaceCullMode.Liquid)
						{
							do
							{
								num5 <<= 1;
								if (currentChunkFluidBlocksExt[num + TileSideEnum.MoveIndex[num6]].BlockMaterial == block2.BlockMaterial)
								{
									continue;
								}
								if (num6 == 4)
								{
									num5++;
									continue;
								}
								Block obj = currentChunkBlocksExt[num + TileSideEnum.MoveIndex[num6]];
								FastVec3i fastVec3i = TileSideEnum.OffsetByTileSide[num6];
								if (!obj.SideIsSolid(tmpPos.Set(baseX + k + fastVec3i.X, baseY + i + fastVec3i.Y, baseZ + j + fastVec3i.Z), TileSideEnum.GetOpposite(num6)))
								{
									num5++;
								}
							}
							while (num6-- != 0);
						}
						else
						{
							do
							{
								num5 <<= 1;
								Block neighbourBlock = currentChunkFluidBlocksExt[num + TileSideEnum.MoveIndex[num6]];
								if (!block2.ShouldMergeFace(num6, neighbourBlock, num2 + k))
								{
									num5++;
								}
							}
							while (num6-- != 0);
						}
						array[num2 + k] = (byte)num5;
					}
				}
				num2 += 32;
			}
		}
		return num > 0;
	}

	public void BuildBlockPolygons(int chunkX, int chunkY, int chunkZ)
	{
		int baseX = chunkX * 32;
		int num = chunkY * 32 % 32768;
		int num2 = chunkZ * 32;
		if (num == 0 && chunkY / 1024 != 1)
		{
			int num3 = 1024;
			for (int i = 0; i < num3; i++)
			{
				currentChunkDraw32[i] &= 223;
				currentChunkDrawFluids[i] &= 223;
			}
		}
		TCTCache tCTCache = vars;
		currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
		int num4 = -1;
		for (int j = 0; j < 32; j++)
		{
			int num5 = (j + 1) * 34 + 1;
			tCTCache.posY = num + j;
			tCTCache.finalY = j;
			tCTCache.ly = j;
			int num6 = j * (j ^ 0x1F);
			int num7 = 0;
			do
			{
				tCTCache.lz = num7;
				int posZ = (tCTCache.posZ = num2 + num7);
				MeshData[][][] array = ((num6 * num7 * (num7 ^ 0x1F) != 0) ? centerModeldataByRenderPassByLodLevel : edgeModeldataByRenderPassByLodLevel);
				int extIndex3dBase = (num5 + num7) * 34 + 1;
				TesselateBlock(++num4, extIndex3dBase, 0, baseX, posZ);
				currentModeldataByRenderPassByLodLevel = array;
				int num8 = 1;
				do
				{
					TesselateBlock(++num4, extIndex3dBase, num8, baseX, posZ);
				}
				while (++num8 < 31);
				currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
				TesselateBlock(++num4, extIndex3dBase, num8, baseX, posZ);
			}
			while (++num7 < 32);
		}
	}

	public void BuildBlockPolygons_EdgeOnly(int chunkX, int chunkY, int chunkZ)
	{
		int baseX = chunkX * 32;
		int num = chunkY * 32 % 32768;
		int num2 = chunkZ * 32;
		if (num == 0)
		{
			int num3 = 1024;
			for (int i = 0; i < num3; i++)
			{
				currentChunkDraw32[i] &= 223;
			}
		}
		currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
		TCTCache tCTCache = vars;
		int num4 = -1;
		for (int j = 0; j < 32; j++)
		{
			int num5 = (j + 1) * 34 + 1;
			tCTCache.posY = num + j;
			tCTCache.finalY = j;
			tCTCache.ly = j;
			int num6 = j * (j ^ 0x1F);
			int num7 = 0;
			do
			{
				tCTCache.lz = num7;
				int posZ = (tCTCache.posZ = num2 + num7);
				int extIndex3dBase = (num5 + num7) * 34 + 1;
				if (num6 * num7 * (num7 ^ 0x1F) == 0)
				{
					int num8 = 0;
					do
					{
						TesselateBlock(++num4, extIndex3dBase, num8, baseX, posZ);
					}
					while (++num8 < 32);
				}
				else
				{
					TesselateBlock(++num4, extIndex3dBase, 0, baseX, posZ);
					num4 += 31;
					TesselateBlock(num4, extIndex3dBase, 31, baseX, posZ);
				}
			}
			while (++num7 < 32);
		}
	}

	private void CullVisibleFacesWithDecor(Dictionary<int, Block> decors, Dictionary<int, Block> drawnDecors)
	{
		foreach (KeyValuePair<int, Block> decor in decors)
		{
			Block value = decor.Value;
			if (value == null)
			{
				continue;
			}
			int num = (value.IsMissing ? 1 : value.decorBehaviorFlags);
			if ((num & 1) != 0)
			{
				int key = decor.Key;
				int num2 = DecorBits.Index3dFromIndex(key);
				BlockFacing blockFacing = DecorBits.FacingFromIndex(key);
				if ((currentChunkDraw32[num2] & blockFacing.Flag) != 0 || (num & 2) != 0)
				{
					drawnDecors[key] = value;
				}
			}
		}
	}

	private void BuildDecorPolygons(int chunkX, int chunkY, int chunkZ, Dictionary<int, Block> decors, bool edgeonly)
	{
		int num = 31;
		int num2 = chunkX * 32;
		int num3 = chunkY * 32 % 32768;
		int num4 = chunkZ * 32;
		TCTCache tCTCache = vars;
		foreach (KeyValuePair<int, Block> decor in decors)
		{
			int key = decor.Key;
			Block value = decor.Value;
			BlockFacing blockFacing = DecorBits.FacingFromIndex(key);
			int num5 = DecorBits.Index3dFromIndex(key);
			int num6 = num5 % 32;
			int num7 = num5 / 32 / 32;
			int num8 = num5 / 32 % 32;
			if (num6 * (num6 ^ num) * num7 * (num7 ^ num) * num8 * (num8 ^ num) == 0)
			{
				currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
			}
			else
			{
				if (edgeonly)
				{
					continue;
				}
				currentModeldataByRenderPassByLodLevel = centerModeldataByRenderPassByLodLevel;
			}
			tCTCache.extIndex3d = ((num7 + 1) * 34 + num8 + 1) * 34 + num6 + 1;
			tCTCache.index3d = num5;
			Vec3i normali = blockFacing.Normali;
			num7 += normali.Y;
			num8 += normali.Z;
			num6 += normali.X;
			tCTCache.posX = num2 + num6;
			tCTCache.posY = num3 + num7;
			tCTCache.posZ = num4 + num8;
			tCTCache.finalY = num7;
			if (value is IDrawYAdjustable drawYAdjustable)
			{
				tCTCache.finalY += drawYAdjustable.AdjustYPosition(new BlockPos(tCTCache.posX, tCTCache.posY, tCTCache.posZ), currentChunkBlocksExt, tCTCache.extIndex3d);
			}
			tCTCache.ly = num7;
			tCTCache.lz = num8;
			int faceflags = 63 - blockFacing.Opposite.Flag;
			tCTCache.decorSubPosition = DecorBits.SubpositionFromIndex(key);
			tCTCache.decorRotationData = DecorBits.RotationFromIndex(key);
			int num9 = (int)(value.IsMissing ? EnumDrawType.SurfaceLayer : value.DrawType);
			if (num9 == 8)
			{
				int num10 = blockFacing.Index;
				int num11 = tCTCache.decorRotationData % 4;
				if ((value.decorBehaviorFlags & 0x20) != 0)
				{
					if (num11 > 0)
					{
						switch (blockFacing.Index)
						{
						case 0:
							num10 = (num11 * 2 + 1) % 6;
							break;
						case 1:
							if (num11 == 2)
							{
								num11 = 0;
								num10 = 5;
							}
							else
							{
								num11--;
								num10 = num11;
							}
							break;
						case 2:
							num11 = 4 - num11;
							num10 = (num11 * 2 + 1) % 6;
							break;
						case 3:
							num11 = 4 - num11;
							if (num11 == 2)
							{
								num11 = 0;
								num10 = 5;
							}
							else
							{
								num11--;
								num10 = num11;
							}
							break;
						case 5:
							num10 = 4;
							break;
						}
					}
					else
					{
						num10 = 4;
					}
				}
				tCTCache.preRotationMatrix = decorRotationMatrices[num10 + num11 * 6];
			}
			else
			{
				tCTCache.preRotationMatrix = null;
			}
			if ((value.decorBehaviorFlags & 4) != 0 && ((num8 & 1) ^ (num6 & 1)) == 1)
			{
				byte zOffset = value.VertexFlags.ZOffset;
				value.VertexFlags.ZOffset = (byte)(zOffset + 2);
				TesselateBlock(value, num6, faceflags, num2 + num6, num4 + num8, num9);
				value.VertexFlags.ZOffset = zOffset;
			}
			else
			{
				TesselateBlock(value, num6, faceflags, num2 + num6, num4 + num8, num9);
			}
			tCTCache.decorSubPosition = 0;
			tCTCache.decorRotationData = 0;
			tCTCache.preRotationMatrix = null;
		}
	}

	private void SetUpDecorRotationMatrices()
	{
		for (int i = 0; i < 4; i++)
		{
			float[] array = Mat4f.Create();
			Mat4f.Translate(array, array, 0f, 0.5f, 0.5f);
			Mat4f.RotateX(array, array, 4.712389f);
			Mat4f.Translate(array, array, 0f, -0.5f, -0.5f);
			SetDecorRotationMatrix(array, i, 0);
			for (int j = 1; j < 4; j++)
			{
				array = Mat4f.Create();
				Mat4f.Translate(array, array, 0.5f, 0.5f, 0.5f);
				Mat4f.RotateY(array, array, (float)Math.PI / 2f * (float)(4 - j));
				Mat4f.RotateX(array, array, 4.712389f);
				Mat4f.Translate(array, array, -0.5f, -0.5f, -0.5f);
				SetDecorRotationMatrix(array, i, j);
			}
			SetDecorRotationMatrix((i == 0) ? null : Mat4f.Create(), i, 4);
			array = Mat4f.Create();
			Mat4f.Translate(array, array, 0f, 0.5f, 0.5f);
			Mat4f.RotateX(array, array, (float)Math.PI);
			Mat4f.Translate(array, array, 0f, -0.5f, -0.5f);
			SetDecorRotationMatrix(array, i, 5);
		}
	}

	private void SetDecorRotationMatrix(float[] matrix, int rot, int i)
	{
		if (rot > 0)
		{
			Mat4f.Translate(matrix, matrix, 0.5f, 0f, 0.5f);
			Mat4f.RotateY(matrix, matrix, (float)(4 - rot) * ((float)Math.PI / 2f));
			Mat4f.Translate(matrix, matrix, -0.5f, 0f, -0.5f);
		}
		decorRotationMatrices[rot * 6 + i] = matrix;
	}

	private void TesselateBlock(int index3d, int extIndex3dBase, int lX, int baseX, int posZ)
	{
		int faceflags;
		if ((faceflags = currentChunkDraw32[index3d]) != 0)
		{
			vars.index3d = index3d;
			Block block = currentChunkBlocksExt[vars.extIndex3d = extIndex3dBase + lX];
			TesselateBlock(block, lX, faceflags, baseX + lX, posZ, (int)block.DrawType);
		}
		if ((faceflags = currentChunkDrawFluids[index3d]) != 0)
		{
			vars.index3d = index3d;
			Block block2 = currentChunkFluidBlocksExt[vars.extIndex3d = extIndex3dBase + lX];
			TesselateBlock(block2, lX, faceflags, baseX + lX, posZ, (int)block2.DrawType);
		}
	}

	private void TesselateBlock(Block block, int lX, int faceflags, int posX, int posZ, int drawType)
	{
		if (block.DrawType == EnumDrawType.Empty)
		{
			return;
		}
		vars.block = block;
		vars.drawFaceFlags = faceflags;
		vars.posX = posX;
		vars.lx = lX;
		vars.finalX = lX;
		vars.finalY = vars.ly;
		if (block is IDrawYAdjustable drawYAdjustable)
		{
			vars.finalY += drawYAdjustable.AdjustYPosition(new BlockPos(vars.posX, vars.posY, vars.posZ), currentChunkBlocksExt, vars.extIndex3d);
		}
		vars.finalZ = vars.lz;
		int num = (vars.blockId = block.BlockId);
		vars.textureSubId = 0;
		vars.VertexFlags = block.VertexFlags.All;
		vars.RenderPass = block.RenderPass;
		vars.fastBlockTextureSubidsByFace = fastBlockTextureSubidsByBlockAndFace[num];
		if (block.RandomDrawOffset != 0)
		{
			vars.finalX += (float)(GameMath.oaatHash(posX, 0, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
			vars.finalZ += (float)(GameMath.oaatHash(posX, 1, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
		}
		if (block.ShapeUsesColormap || block.LoadColorMapAnyway || block.Frostable)
		{
			int num2 = posX + GameMath.MurmurHash3Mod(posX, 0, posZ, 5) - 2;
			int num3 = posZ + GameMath.MurmurHash3Mod(posX, 1, posZ, 5) - 2;
			int num4 = posX / regionSize;
			int num5 = posZ / regionSize;
			int num6 = currentClimateRegionMap[GameMath.Clamp(num3 - num5 * regionSize, 0, regionSize - 1) * regionSize + GameMath.Clamp(num2 - num4 * regionSize, 0, regionSize - 1)];
			TCTCache tCTCache = vars;
			ColorMap seasonColorMapResolved = block.SeasonColorMapResolved;
			int seasonMapIndex = ((seasonColorMapResolved != null) ? (seasonColorMapResolved.RectIndex + 1) : 0);
			ColorMap climateColorMapResolved = block.ClimateColorMapResolved;
			tCTCache.ColorMapData = new ColorMapData(seasonMapIndex, (climateColorMapResolved != null) ? (climateColorMapResolved.RectIndex + 1) : 0, Climate.GetAdjustedTemperature((num6 >> 16) & 0xFF, vars.posY - seaLevel), Climate.GetRainFall((num6 >> 8) & 0xFF, vars.posY), block.Frostable);
		}
		else
		{
			vars.ColorMapData = defaultColorMapData;
		}
		if (block.DrawType == EnumDrawType.Liquid)
		{
			if (vars.posY == seaLevel - 1)
			{
				Block block2 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[0]];
				Block block3 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[2]];
				Block block4 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[3]];
				Block block5 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[1]];
				Block block6 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]];
				Block block7 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]];
				Block block8 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]];
				Block block9 = currentChunkFluidBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]];
				if (block6.Id == 0 && vars.lx == 0 && vars.lz == 0 && currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]].Id == 0)
				{
					block6 = block;
				}
				if (block7.Id == 0 && vars.lx == 0 && vars.lz == 31 && currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]].Id == 0)
				{
					block7 = block;
				}
				if (block8.Id == 0 && vars.lx == 31 && vars.lz == 0 && currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]].Id == 0)
				{
					block8 = block;
				}
				if (block9.Id == 0 && vars.lx == 31 && vars.lz == 31 && currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]].Id == 0)
				{
					block9 = block;
				}
				vars.OceanityFlagTL = ((block2 == block && block6 == block && block4 == block) ? ((byte)GameMath.BiLerp(currentOceanityMapTL, currentOceanityMapTR, currentOceanityMapBL, currentOceanityMapBR, (float)vars.lx / 32f, (float)vars.lz / 32f) << 2) : 0);
				vars.OceanityFlagTR = ((block2 == block && block8 == block && block5 == block) ? ((byte)GameMath.BiLerp(currentOceanityMapTL, currentOceanityMapTR, currentOceanityMapBL, currentOceanityMapBR, (float)(vars.lx + 1) / 32f, (float)vars.lz / 32f) << 2) : 0);
				vars.OceanityFlagBL = ((block3 == block && block7 == block && block4 == block) ? ((byte)GameMath.BiLerp(currentOceanityMapTL, currentOceanityMapTR, currentOceanityMapBL, currentOceanityMapBR, (float)vars.lx / 32f, (float)(vars.lz + 1) / 32f) << 2) : 0);
				vars.OceanityFlagBR = ((block3 == block && block9 == block && block5 == block) ? ((byte)GameMath.BiLerp(currentOceanityMapTL, currentOceanityMapTR, currentOceanityMapBL, currentOceanityMapBR, (float)(vars.lx + 1) / 32f, (float)(vars.lz + 1) / 32f) << 2) : 0);
			}
			else
			{
				vars.OceanityFlagTL = 0;
				vars.OceanityFlagTR = 0;
				vars.OceanityFlagBL = 0;
				vars.OceanityFlagBR = 0;
			}
		}
		vars.textureVOffset = ((block.alternatingVOffset && (((block.alternatingVOffsetFaces & 0xA) > 0 && posX % 2 == 1) || ((block.alternatingVOffsetFaces & 0x30) > 0 && vars.posY % 2 == 1) || ((block.alternatingVOffsetFaces & 5) > 0 && posZ % 2 == 1))) ? 1f : 0f);
		blockTesselators[drawType].Tesselate(vars);
	}

	private void BuildExtendedChunkData(ClientChunk curChunk, int chunkX, int chunkY, int chunkZ, bool atMapEdge, bool skipChunkCenter)
	{
		int num = 34;
		int count = game.Blocks.Count;
		game.WorldMap.GetNeighbouringChunks(chunksNearby, chunkX, chunkY, chunkZ);
		for (int num2 = 26; num2 >= 0; num2--)
		{
			chunksNearby[num2].Unpack();
			chunkdatasNearby[num2] = (ClientChunkData)chunksNearby[num2].Data;
			chunkdatasNearby[num2].blocksLayer?.ClearPaletteOutsideMaxValue(count);
		}
		chunkdatasNearby[13].BuildFastBlockAccessArray(blocksFast);
		int num3 = num - 1;
		ClientChunkData clientChunkData = (ClientChunkData)curChunk.Data;
		int num4 = 0;
		int num5 = 1190;
		int num6;
		if (skipChunkCenter)
		{
			for (int i = 0; i < 32; i++)
			{
				for (int j = 0; j < 32; j++)
				{
					num6 = (i * 34 + j) * 34 + num5;
					if ((i + 2) % 32 <= 3 || (j + 2) % 32 <= 3)
					{
						clientChunkData.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, num6, num4, num4 + 32, blocksFast, lightConverter);
						num4 += 32;
						continue;
					}
					clientChunkData.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, num6, num4, num4 + 2, blocksFast, lightConverter);
					num6 += 30;
					num4 += 30;
					clientChunkData.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, num6, num4, num4 + 2, blocksFast, lightConverter);
					num4 += 2;
				}
			}
		}
		else
		{
			for (int k = 0; k < 32; k++)
			{
				for (int l = 0; l < 32; l++)
				{
					num6 = (k * 34 + l) * 34 + num5;
					clientChunkData.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, num6, num4, num4 + 32, blocksFast, lightConverter);
					num4 += 32;
				}
			}
		}
		num6 = -1;
		for (int m = 0; m < num; m++)
		{
			bool flag = m == 0 || m == num3;
			for (int n = 0; n < num; n++)
			{
				bool num7 = n == 0 || n == num3;
				int num8 = ((!flag) ? 1 : ((m != 0) ? 2 : 0));
				int num9 = ((!num7) ? 1 : ((n != 0) ? 2 : 0));
				int num10 = (m - 1) & 0x1F;
				int num11 = (n - 1) & 0x1F;
				num4 = (num10 * 32 + num11) * 32;
				int num12 = num8 * 3 + num9;
				clientChunkData = chunkdatasNearby[num12];
				int one = clientChunkData.GetOne(out var lightOut, out var lightSatOut, out var fluidBlockId, num4 + 31);
				currentChunkBlocksExt[++num6] = blocksFast[one];
				currentChunkFluidBlocksExt[num6] = blocksFast[fluidBlockId];
				currentChunkRgbsExt[num6] = lightConverter.ToRgba(lightOut, lightSatOut);
				num12 += 9;
				if (num12 == 13)
				{
					num6 += 32;
				}
				else
				{
					clientChunkData = chunkdatasNearby[num12];
					clientChunkData.GetRange(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, num6, num4, num4 + 32, blocksFast, lightConverter);
					num6 += 32;
				}
				num12 += 9;
				clientChunkData = chunkdatasNearby[num12];
				one = clientChunkData.GetOne(out lightOut, out lightSatOut, out fluidBlockId, num4);
				currentChunkBlocksExt[++num6] = blocksFast[one];
				currentChunkFluidBlocksExt[num6] = blocksFast[fluidBlockId];
				currentChunkRgbsExt[num6] = lightConverter.ToRgba(lightOut, lightSatOut);
			}
		}
		for (int num13 = 0; num13 < currentChunkBlocksExt.Length; num13++)
		{
			if (currentChunkBlocksExt[num13] == null)
			{
				currentChunkBlocksExt[num13] = blocksFast[0];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshData GetMeshPoolForPass(int textureid, EnumChunkRenderPass renderPass, int lodLevel)
	{
		int num = 0;
		do
		{
			if (TextureIdToReturnNum[num] == textureid)
			{
				return currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass][num];
			}
		}
		while (++num < quantityAtlasses);
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshData[] GetPoolForPass(EnumChunkRenderPass renderPass, int lodLevel)
	{
		return currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass];
	}
}
