using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Vintagestory.Client.NoObf;

public class ShapeTesselatorManager : AsyncHelper.Multithreaded, ITesselatorManager
{
	public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

	public OrderedDictionary<AssetLocation, IAsset> objs;

	public OrderedDictionary<AssetLocation, GltfType> gltfs;

	public MeshData[] blockModelDatas;

	public MeshData[][] altblockModelDatasLod0;

	public MeshData[][] altblockModelDatasLod1;

	public MeshData[][] altblockModelDatasLod2;

	public MultiTextureMeshRef[] blockModelRefsInventory;

	public MultiTextureMeshRef[] itemModelRefsInventory;

	public MultiTextureMeshRef[][] altItemModelRefsInventory;

	public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public MultiTextureMeshRef unknownItemModelRef;

	public MeshData unknownBlockModelData = CubeMeshUtil.GetCubeOnlyScaleXyz(0.5f, 0.5f, new Vec3f(0.5f, 0.5f, 0.5f));

	public MultiTextureMeshRef unknownBlockModelRef;

	private ClientMain game;

	internal volatile int finishedAsyncBlockTesselation;

	[ThreadStatic]
	private static ShapeTesselator TLTesselator;

	private UnloadableShape basicCubeShape;

	private bool itemloadingdone;

	private Dictionary<AssetLocation, UnloadableShape> shapes2;

	private Dictionary<AssetLocation, UnloadableShape> shapes3;

	private Dictionary<AssetLocation, UnloadableShape> shapes4;

	private Dictionary<AssetLocation, UnloadableShape> itemshapes;

	public ShapeTesselator Tesselator => TLTesselator ?? (TLTesselator = new ShapeTesselator(game, shapes, objs, gltfs));

	public MeshData GetDefaultBlockMesh(Block block)
	{
		if (blockModelDatas[block.BlockId] == null)
		{
			TesselateBlock(block, lazyLoad: false);
		}
		return blockModelDatas[block.BlockId];
	}

	internal ITesselatorAPI GetNewTesselator()
	{
		return new ShapeTesselator(game, shapes, objs, gltfs);
	}

	public ShapeTesselatorManager(ClientMain game)
	{
		this.game = game;
		ClientEventManager eventManager = game.eventManager;
		if (eventManager != null)
		{
			eventManager.OnReloadShapes += TesselateBlocksAndItems;
		}
	}

	public ShapeTesselatorManager(ServerMain server)
	{
	}

	private void TesselateBlocksAndItems()
	{
		PrepareToLoadShapes();
		LoadItemShapesAsync(game.Items);
		LoadBlockShapes(game.Blocks);
		TLTesselator = new ShapeTesselator(game, shapes, objs, gltfs);
		TesselateBlocks_Pre();
		TyronThreadPool.QueueTask(delegate
		{
			TesselateBlocks_Async(game.Blocks);
		});
		for (int num = 0; num < game.Blocks.Count; num = TesselateBlocksForInventory(game.Blocks, num, game.Blocks.Count))
		{
		}
		for (int num = 0; num < game.Items.Count; num = TesselateItems(game.Items, num, game.Items.Count))
		{
		}
		while (finishedAsyncBlockTesselation != 2 && !game.disposed)
		{
			Thread.Sleep(30);
		}
		LoadDone();
	}

	public void LoadDone()
	{
		if (ClientSettings.OptimizeRamMode != 2)
		{
			return;
		}
		foreach (KeyValuePair<AssetLocation, UnloadableShape> shape in shapes)
		{
			if (shape.Value != basicCubeShape)
			{
				shape.Value.Unload();
			}
		}
	}

	public void PrepareToLoadShapes()
	{
		ResetThreading();
		shapes = new OrderedDictionary<AssetLocation, UnloadableShape>();
		shapes2 = new Dictionary<AssetLocation, UnloadableShape>();
		shapes3 = new Dictionary<AssetLocation, UnloadableShape>();
		shapes4 = new Dictionary<AssetLocation, UnloadableShape>();
		itemshapes = new Dictionary<AssetLocation, UnloadableShape>();
		objs = new OrderedDictionary<AssetLocation, IAsset>();
		gltfs = new OrderedDictionary<AssetLocation, GltfType>();
		shapes[new AssetLocation("block/basic/cube")] = BasicCube(game.api);
	}

	internal void LoadItemShapesAsync(IList<Item> items)
	{
		itemloadingdone = false;
		TyronThreadPool.QueueTask(delegate
		{
			LoadItemShapes(items);
		});
	}

	internal Dictionary<AssetLocation, UnloadableShape> LoadItemShapes(IList<Item> items)
	{
		try
		{
			HashSet<AssetLocationAndSource> hashSet = new HashSet<AssetLocationAndSource>();
			for (int i = 0; i < items.Count; i++)
			{
				if (game.disposed)
				{
					return itemshapes;
				}
				Item item = items[i];
				if (item == null || item.Shape == null)
				{
					continue;
				}
				CompositeShape shape = item.Shape;
				if (!shape.VoxelizeTexture)
				{
					hashSet.Add(new AssetLocationAndSource(shape.Base, "Shape for item ", item.Code));
					shape.LoadAlternates(game.api.Assets, game.Logger);
				}
				if (shape.BakedAlternates != null)
				{
					for (int j = 0; j < shape.BakedAlternates.Length; j++)
					{
						if (game.disposed)
						{
							return itemshapes;
						}
						if (!shape.BakedAlternates[j].VoxelizeTexture)
						{
							hashSet.Add(new AssetLocationAndSource(shape.BakedAlternates[j].Base, "Alternate shape for item ", item.Code, j));
						}
					}
				}
				if (shape.Overlays == null)
				{
					continue;
				}
				for (int k = 0; k < shape.Overlays.Length; k++)
				{
					if (game.disposed)
					{
						return itemshapes;
					}
					if (!shape.Overlays[k].VoxelizeTexture)
					{
						hashSet.Add(new AssetLocationAndSource(shape.Overlays[k].Base, "Overlay shape for item ", item.Code, k));
					}
				}
			}
			game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through items...");
			LoadShapes(hashSet, itemshapes, "for items");
		}
		finally
		{
			itemloadingdone = true;
		}
		return itemshapes;
	}

	internal OrderedDictionary<AssetLocation, UnloadableShape> LoadBlockShapes(IList<Block> blocks)
	{
		game.Platform.Logger.VerboseDebug("[LoadShapes] Searching through blocks...");
		int count = blocks.Count;
		IDisposable[] refs = blockModelRefsInventory;
		DisposeArray(refs);
		blockModelDatas = new MeshData[count + 1];
		altblockModelDatasLod0 = new MeshData[count + 1][];
		altblockModelDatasLod1 = new MeshData[count + 1][];
		altblockModelDatasLod2 = new MeshData[count + 1][];
		blockModelRefsInventory = new MultiTextureMeshRef[count + 1];
		CompositeShape basicCube = new CompositeShape
		{
			Base = new AssetLocation("block/basic/cube")
		};
		int val = Environment.ProcessorCount / 2 - 3;
		val = Math.Min(val, 4);
		val = Math.Max(val, 2);
		TargetSet[] array = new TargetSet[1];
		int count2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			TargetSet set = new TargetSet();
			array[i] = set;
			int start = i * blocks.Count / array.Length;
			int end = (i + 1) * blocks.Count / array.Length;
			if (i < array.Length - 1)
			{
				TyronThreadPool.QueueTask(delegate
				{
					CollectBlockShapes(blocks, start, end, set, basicCube, ref count2);
				}, "collectblockshapes");
			}
			else
			{
				CollectBlockShapes(blocks, start, end, set, basicCube, ref count2);
			}
		}
		HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();
		HashSet<AssetLocationAndSource> hashSet = new HashSet<AssetLocationAndSource>();
		HashSet<AssetLocationAndSource> hashSet2 = new HashSet<AssetLocationAndSource>();
		shapelocations.Add(new AssetLocationAndSource(basicCube.Base));
		foreach (TargetSet targetSet in array)
		{
			while (!targetSet.finished && !game.disposed)
			{
				Thread.Sleep(10);
			}
			foreach (AssetLocationAndSource shapelocation in targetSet.shapelocations)
			{
				shapelocations.Add(shapelocation);
			}
			foreach (AssetLocationAndSource objlocation in targetSet.objlocations)
			{
				hashSet.Add(objlocation);
			}
			foreach (AssetLocationAndSource gltflocation in targetSet.gltflocations)
			{
				hashSet2.Add(gltflocation);
			}
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through " + count2 + " blocks");
		while (WorkerThreadsInProgress() && !game.disposed)
		{
			Thread.Sleep(10);
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] Starting to parse block shapes...");
		if (val >= 2)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes2, "(2nd block loading thread)");
			});
		}
		if (val >= 3)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes3, "(3rd block loading thread)");
			});
		}
		if (val >= 4)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes4, "(4th block loading thread)");
			});
		}
		LoadShapes(hashSet, hashSet2);
		LoadShapes(shapelocations, shapes, "for " + count2 + " blocks" + ((val > 1) ? ", some others done offthread" : ""));
		FinalizeLoading();
		return shapes;
	}

	private void CollectBlockShapes(IList<Block> blocks, int start, int maxCount, TargetSet targetSet, CompositeShape basicCube, ref int totalCount)
	{
		int num = 0;
		try
		{
			for (int i = start; i < maxCount; i++)
			{
				if (game.disposed)
				{
					break;
				}
				Block block = blocks[i];
				if (block.Code == null)
				{
					continue;
				}
				num++;
				if (block.Shape == null || block.Shape.Base.Path.Length == 0)
				{
					block.Shape = basicCube;
				}
				else
				{
					CompositeShape shape = block.Shape;
					shape.LoadAlternates(game.api.Assets, game.Logger);
					targetSet.Add(shape, "Shape for block ", block.Code);
					if (shape.BakedAlternates != null)
					{
						for (int j = 0; j < shape.BakedAlternates.Length; j++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape compositeShape = shape.BakedAlternates[j];
							if (compositeShape != null && !(compositeShape.Base == null))
							{
								targetSet.Add(compositeShape, "Alternate shape for block ", block.Code, j);
							}
						}
					}
					if (block.Shape.Overlays != null)
					{
						for (int k = 0; k < block.Shape.Overlays.Length; k++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape compositeShape2 = block.Shape.Overlays[k];
							if (compositeShape2 != null && !(compositeShape2.Base == null))
							{
								targetSet.Add(compositeShape2, "Overlay shape for block ", block.Code, k);
							}
						}
					}
				}
				if (block.ShapeInventory != null)
				{
					if (game.disposed)
					{
						break;
					}
					targetSet.Add(block.ShapeInventory, "Inventory shape for block ", block.Code);
					if (block.ShapeInventory.Overlays != null)
					{
						for (int l = 0; l < block.ShapeInventory.Overlays.Length; l++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape compositeShape3 = block.ShapeInventory.Overlays[l];
							if (compositeShape3 != null && !(compositeShape3.Base == null))
							{
								targetSet.Add(compositeShape3, "Inventory overlay shape for block ", block.Code, l);
							}
						}
					}
				}
				if (block.Lod0Shape != null)
				{
					if (game.disposed)
					{
						break;
					}
					block.Lod0Shape.LoadAlternates(game.api.Assets, game.Logger);
					targetSet.Add(block.Lod0Shape, "Lod0 shape for block ", block.Code);
					if (block.Lod0Shape.BakedAlternates != null)
					{
						for (int m = 0; m < block.Lod0Shape.BakedAlternates.Length; m++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape compositeShape4 = block.Lod0Shape.BakedAlternates[m];
							if (compositeShape4 != null && !(compositeShape4.Base == null))
							{
								targetSet.Add(compositeShape4, "Alternate lod 0 for block ", block.Code, m);
							}
						}
					}
				}
				if (block.Lod2Shape == null)
				{
					continue;
				}
				if (game.disposed)
				{
					break;
				}
				block.Lod2Shape.LoadAlternates(game.api.Assets, game.Logger);
				targetSet.Add(block.Lod2Shape, "Lod2 shape for block ", block.Code);
				if (block.Lod2Shape.BakedAlternates == null)
				{
					continue;
				}
				for (int n = 0; n < block.Lod2Shape.BakedAlternates.Length; n++)
				{
					if (game.disposed)
					{
						return;
					}
					CompositeShape compositeShape5 = block.Lod2Shape.BakedAlternates[n];
					if (compositeShape5 != null && !(compositeShape5.Base == null))
					{
						targetSet.Add(compositeShape5, "Alternate lod 2 for block ", block.Code, n);
					}
				}
			}
		}
		finally
		{
			targetSet.finished = true;
			Interlocked.Add(ref totalCount, num);
		}
	}

	internal void FinalizeLoading()
	{
		while (!itemloadingdone || (WorkerThreadsInProgress() && !game.disposed))
		{
			Thread.Sleep(10);
		}
		ILogger logger = game.Platform.Logger;
		shapes.AddRange(shapes2, logger);
		shapes2.Clear();
		shapes2 = null;
		shapes.AddRange(shapes3, logger);
		shapes3.Clear();
		shapes3 = null;
		shapes.AddRange(shapes4, logger);
		shapes4.Clear();
		shapes4 = null;
		shapes.AddRange(itemshapes, logger);
		itemshapes = null;
		game.DoneBlockAndItemShapeLoading = true;
		logger.Notification("Collected {0} shapes to tesselate.", shapes.Count);
	}

	internal void LoadShapes(HashSet<AssetLocationAndSource> shapelocations, IDictionary<AssetLocation, UnloadableShape> shapes, string typeForLog)
	{
		int num = 0;
		foreach (AssetLocationAndSource shapelocation in shapelocations)
		{
			if (game.disposed)
			{
				break;
			}
			if (AsyncHelper.CanProceedOnThisThread(ref shapelocation.loadedAlready))
			{
				num++;
				UnloadableShape unloadableShape = new UnloadableShape();
				unloadableShape.Loaded = true;
				if (!unloadableShape.Load(game, shapelocation))
				{
					shapes[shapelocation] = basicCubeShape;
				}
				else
				{
					shapes[shapelocation] = unloadableShape;
				}
			}
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] parsed " + num + " shapes from JSON " + typeForLog);
	}

	internal void LoadShapes(HashSet<AssetLocationAndSource> objlocations, HashSet<AssetLocationAndSource> gltflocations)
	{
		int num = 0;
		foreach (AssetLocationAndSource objlocation in objlocations)
		{
			if (game.disposed)
			{
				return;
			}
			AssetLocation assetLocation = objlocation.CopyWithPathPrefixAndAppendixOnce("shapes/", ".obj");
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet(assetLocation);
			if (game.disposed)
			{
				return;
			}
			if (asset == null)
			{
				game.Platform.Logger.Warning("Did not find required obj {0} anywhere. (defined in {1})", assetLocation, objlocation.Source);
			}
			else
			{
				objs[objlocation] = asset;
				num++;
			}
		}
		foreach (AssetLocationAndSource gltflocation in gltflocations)
		{
			if (game.disposed)
			{
				return;
			}
			AssetLocation assetLocation2 = gltflocation.CopyWithPathPrefixAndAppendixOnce("shapes/", ".gltf");
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet(assetLocation2);
			if (game.disposed)
			{
				return;
			}
			if (asset == null)
			{
				game.Platform.Logger.Warning("Did not find required gltf {0} anywhere. (defined in {1})", assetLocation2, gltflocation.Source);
			}
			else
			{
				gltfs[gltflocation] = asset.ToObject<GltfType>();
				num++;
			}
		}
		if (num > 0)
		{
			game.Platform.Logger.VerboseDebug("[LoadShapes] loaded " + num + " block shapes in obj and gltf formats");
		}
	}

	private UnloadableShape BasicCube(ICoreAPI api)
	{
		if (basicCubeShape == null)
		{
			AssetLocation assetLocation = new AssetLocation("shapes/block/basic/cube.json");
			IAsset asset = api.Assets.TryGet(assetLocation);
			if (asset == null)
			{
				throw new Exception("Shape shapes/block/basic/cube.json not found, it is required to run the game");
			}
			ShapeElement.locationForLogging = assetLocation;
			basicCubeShape = asset.ToObject<UnloadableShape>();
			basicCubeShape.Loaded = true;
		}
		return basicCubeShape;
	}

	public void LoadEntityShapesAsync(IEnumerable<EntityProperties> entities, ICoreAPI api)
	{
		OnWorkerThread(delegate
		{
			LoadEntityShapes(entities, api);
		});
	}

	public void LoadEntityShapes(IEnumerable<EntityProperties> entities, ICoreAPI api)
	{
		Dictionary<AssetLocation, Shape> dictionary = new Dictionary<AssetLocation, Shape>();
		dictionary[new AssetLocation("block/basic/cube")] = BasicCube(api);
		api.Logger.VerboseDebug("Entity shape loading starting ...");
		foreach (EntityProperties entity in entities)
		{
			if (game != null && game.disposed)
			{
				return;
			}
			if (entity != null && entity.Client != null)
			{
				try
				{
					LoadShape(entity, api, dictionary);
				}
				catch (Exception)
				{
					api.Logger.Error("Error while attempting to load shape file for entity: " + entity.Code.ToShortString());
					throw;
				}
			}
		}
		api.Logger.VerboseDebug("Entity shape loading completed");
	}

	private void LoadShape(EntityProperties entity, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
	{
		EntityClientProperties client = entity.Client;
		Shape shape = (client.LoadedShape = LoadEntityShape(client.Shape, entity.Code, api, entityShapes));
		if (api is ICoreServerAPI)
		{
			shape?.FreeRAMServer();
		}
		CompositeShape[] array = client.Shape?.Alternates;
		if (array == null)
		{
			return;
		}
		Shape[] array2 = (client.LoadedAlternateShapes = new Shape[array.Length]);
		for (int i = 0; i < array.Length; i++)
		{
			if (game != null && game.disposed)
			{
				break;
			}
			shape = (array2[i] = LoadEntityShape(array[i], entity.Code, api, entityShapes));
			if (api is ICoreServerAPI)
			{
				shape?.FreeRAMServer();
			}
		}
	}

	private Shape LoadEntityShape(CompositeShape cShape, AssetLocation entityTypeForLogging, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
	{
		if (cShape == null)
		{
			return null;
		}
		if (cShape.Base == null || cShape.Base.Path.Length == 0)
		{
			if (cShape == null || !cShape.VoxelizeTexture)
			{
				api.Logger.Warning("No entity shape supplied for entity {0}, using cube shape", entityTypeForLogging);
			}
			cShape.Base = new AssetLocation("block/basic/cube");
			return basicCubeShape;
		}
		if (entityShapes.TryGetValue(cShape.Base, out var value))
		{
			if (value == null)
			{
				api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at shapes/{1}.json. Entity will be invisible!", entityTypeForLogging, cShape.Base);
			}
			return value;
		}
		AssetLocation assetLocation = cShape.Base.CopyWithPath("shapes/" + cShape.Base.Path + ".json");
		value = Shape.TryGet(api, assetLocation);
		entityShapes[cShape.Base] = value;
		if (value == null)
		{
			api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at {1}. Entity will be invisible!", entityTypeForLogging, assetLocation);
			return null;
		}
		value.ResolveReferences(api.Logger, cShape.Base.ToString());
		if (api.Side == EnumAppSide.Client)
		{
			CacheInvTransforms(value.Elements);
		}
		return value;
	}

	private static void CacheInvTransforms(ShapeElement[] elements)
	{
		if (elements != null)
		{
			foreach (ShapeElement obj in elements)
			{
				obj.CacheInverseTransformMatrix();
				CacheInvTransforms(obj.Children);
			}
		}
	}

	public void TesselateBlocks_Pre()
	{
		if (unknownBlockModelRef == null)
		{
			unknownBlockModelRef = game.api.renderapi.UploadMultiTextureMesh(unknownBlockModelData);
		}
		if (shapes == null)
		{
			throw new Exception("Can't tesselate, shapes not loaded yet!");
		}
		finishedAsyncBlockTesselation = 0;
	}

	public int TesselateBlocksForInventory(IList<Block> blocks, int offset, int maxCount)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		int i = offset;
		int num = 0;
		for (; i < maxCount; i++)
		{
			Block block = blocks[i];
			if (!(block.Code == null) && block.BlockId != 0)
			{
				MeshData data = TesselateBlockForInventory(block);
				blockModelRefsInventory[block.BlockId]?.Dispose();
				blockModelRefsInventory[block.BlockId] = game.api.renderapi.UploadMultiTextureMesh(data);
				if (num++ % 4 == 0 && stopwatch.ElapsedMilliseconds >= 60)
				{
					i++;
					break;
				}
			}
		}
		if (i == blocks.Count)
		{
			BlockTesselationHalfCompleted();
		}
		return i;
	}

	public void TesselateBlocksForInventory_ASync(IList<Block> blocks)
	{
		if (TLTesselator != null)
		{
			throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
		}
		MeshData[] meshes = new MeshData[blocks.Count];
		try
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				if (!(block.Code == null) && block.BlockId != 0)
				{
					meshes[i] = TesselateBlockForInventory(block);
				}
			}
		}
		finally
		{
			game.EnqueueGameLaunchTask(delegate
			{
				FinishInventoryMeshes(meshes, 0);
			}, "blockInventoryTesselation");
			BlockTesselationHalfCompleted();
			ThreadDispose();
		}
	}

	private void FinishInventoryMeshes(MeshData[] meshes, int start)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		int num = 0;
		int i;
		for (i = start; i < meshes.Length; i++)
		{
			MeshData meshData = meshes[i];
			if (meshData == null)
			{
				continue;
			}
			if (meshData == unknownBlockModelData)
			{
				blockModelRefsInventory[i] = unknownBlockModelRef;
			}
			else
			{
				blockModelRefsInventory[i]?.Dispose();
				blockModelRefsInventory[i] = game.api.renderapi.UploadMultiTextureMesh(meshData);
			}
			if (num++ % 4 == 0 && stopwatch.ElapsedMilliseconds >= 60)
			{
				game.EnqueueGameLaunchTask(delegate
				{
					FinishInventoryMeshes(meshes, i + 1);
				}, "blockInventoryTesselation");
				break;
			}
		}
	}

	public void TesselateBlocks_Async(IList<Block> blocks)
	{
		if (TLTesselator != null)
		{
			throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
		}
		try
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				if (block != null && !(block.Code == null) && block.BlockId != 0)
				{
					TesselateBlock(block, lazyLoad: true);
					CreateFastTextureAlternates(block);
				}
			}
		}
		finally
		{
			BlockTesselationHalfCompleted();
			ThreadDispose();
		}
	}

	private void BlockTesselationHalfCompleted()
	{
		if (Interlocked.Increment(ref finishedAsyncBlockTesselation) == 2)
		{
			game.Logger.Notification("Blocks tesselated");
			game.Logger.VerboseDebug("Server assets - done block tesselation");
		}
	}

	public static void CreateFastTextureAlternates(Block block)
	{
		BlockFacing[] aLLFACES;
		if (block.HasAlternates && block.DrawType != EnumDrawType.JSON)
		{
			BakedCompositeTexture[][] array = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
			aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				if (block.Textures.TryGetValue(blockFacing.Code, out var value))
				{
					BakedCompositeTexture[] bakedVariants = value.Baked.BakedVariants;
					if (bakedVariants != null && bakedVariants.Length != 0)
					{
						array[blockFacing.Index] = bakedVariants;
					}
				}
			}
		}
		if (!block.HasTiles || block.DrawType == EnumDrawType.JSON)
		{
			return;
		}
		BakedCompositeTexture[][] array2 = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
		aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing2 in aLLFACES)
		{
			if (block.Textures.TryGetValue(blockFacing2.Code, out var value2))
			{
				BakedCompositeTexture[] bakedTiles = value2.Baked.BakedTiles;
				if (bakedTiles != null && bakedTiles.Length != 0)
				{
					array2[blockFacing2.Index] = bakedTiles;
				}
			}
		}
	}

	public void TesselateItems_Pre(IList<Item> itemtypes)
	{
		if (unknownItemModelRef == null)
		{
			CompositeTexture compositeTexture = new CompositeTexture(new AssetLocation("unknown"));
			compositeTexture.Bake(game.Platform.AssetManager);
			BakedBitmap bakedBitmap = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(compositeTexture.Baked.BakedName));
			unknownItemModelData = ShapeTesselator.VoxelizeTextureStatic(bakedBitmap.TexturePixels, bakedBitmap.Width, bakedBitmap.Height, game.BlockAtlasManager.UnknownTexturePos);
			unknownItemModelRef = game.api.renderapi.UploadMultiTextureMesh(unknownItemModelData);
		}
		if (itemModelRefsInventory == null)
		{
			itemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count];
		}
		if (altItemModelRefsInventory == null)
		{
			altItemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count][];
		}
	}

	public int TesselateItems(IList<Item> itemtypes, int offset, int maxCount)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		int num = 0;
		int i;
		for (i = offset; i < maxCount; i++)
		{
			Item item = itemtypes[i];
			if (item == null)
			{
				continue;
			}
			if (item.Code == null || ((item.FirstTexture == null || item.FirstTexture.Base.Path == "unknown") && item.Shape == null))
			{
				itemModelRefsInventory[item.ItemId] = unknownItemModelRef;
				continue;
			}
			Tesselator.TesselateItem(item, item.Shape, out var modeldata);
			if (itemModelRefsInventory[item.ItemId] != null)
			{
				itemModelRefsInventory[item.ItemId].Dispose();
			}
			itemModelRefsInventory[item.ItemId] = game.api.renderapi.UploadMultiTextureMesh(modeldata);
			if (item.Shape?.BakedAlternates != null)
			{
				if (altItemModelRefsInventory[item.ItemId] == null)
				{
					altItemModelRefsInventory[item.ItemId] = new MultiTextureMeshRef[item.Shape.BakedAlternates.Length];
				}
				for (int j = 0; item.Shape.BakedAlternates.Length > j; j++)
				{
					Tesselator.TesselateItem(item, item.Shape.BakedAlternates[j], out var modeldata2);
					if (altItemModelRefsInventory[item.ItemId][j] != null)
					{
						altItemModelRefsInventory[item.ItemId][j].Dispose();
					}
					altItemModelRefsInventory[item.ItemId][j] = game.api.renderapi.UploadMultiTextureMesh(modeldata2);
				}
			}
			if (num++ % 4 == 0 && stopwatch.ElapsedMilliseconds >= 60)
			{
				i++;
				break;
			}
		}
		return i;
	}

	private void TesselateBlock(Block block, bool lazyLoad)
	{
		if (block.IsMissing)
		{
			blockModelDatas[block.BlockId] = unknownBlockModelData;
			return;
		}
		int num = Tesselator.AltTexturesCount(block);
		int val = ((block.Shape.BakedAlternates != null) ? block.Shape.BakedAlternates.Length : 0);
		int num2 = Tesselator.TileTexturesCount(block);
		block.HasAlternates = Math.Max(num, val) != 0;
		block.HasTiles = num2 > 0;
		if (lazyLoad)
		{
			return;
		}
		TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, block);
		if (block.Lod0Shape != null)
		{
			block.Lod0Mesh = Tesselate(texSource, block, block.Lod0Shape, altblockModelDatasLod0, num, num2);
			setLod0Flag(block.Lod0Mesh);
			MeshData[] array = altblockModelDatasLod0[block.Id];
			int num3 = 0;
			while (array != null && num3 < array.Length)
			{
				setLod0Flag(array[num3]);
				num3++;
			}
		}
		blockModelDatas[block.BlockId] = Tesselate(texSource, block, block.Shape, altblockModelDatasLod1, num, num2);
		if (block.Lod2Shape != null)
		{
			block.Lod2Mesh = Tesselate(texSource, block, block.Lod2Shape, altblockModelDatasLod2, num, num2);
		}
	}

	private MeshData TesselateBlockForInventory(Block block)
	{
		if (block.IsMissing)
		{
			return unknownBlockModelData;
		}
		TextureSource textureSource = new TextureSource(game, game.BlockAtlasManager.Size, block, forInventory: true);
		textureSource.blockShape = block.Shape;
		if (block.ShapeInventory != null)
		{
			textureSource.blockShape = block.ShapeInventory;
		}
		MeshData modeldata;
		try
		{
			if (block.Shape.VoxelizeTexture)
			{
				BakedBitmap bakedBitmap = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(block.FirstTextureInventory.Baked.BakedName, "Block code ", block.Code));
				int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
				TextureAtlasPosition pos = game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
				modeldata = ShapeTesselator.VoxelizeTextureStatic(bakedBitmap.TexturePixels, bakedBitmap.Width, bakedBitmap.Height, pos);
			}
			else
			{
				Tesselator.TesselateBlock(block, textureSource.blockShape, out modeldata, textureSource);
			}
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block {0} with first texture {1}:", block, block.FirstTextureInventory?.Baked?.BakedName);
			game.Platform.Logger.Error(e);
			throw;
		}
		int num = modeldata.GetVerticesCount() / 4;
		for (int i = 0; i < num; i++)
		{
			byte[] climateColorMapIds = modeldata.ClimateColorMapIds;
			int num2 = ((climateColorMapIds != null && climateColorMapIds.Length != 0) ? modeldata.ClimateColorMapIds[i] : 0);
			if (num2 == 0)
			{
				continue;
			}
			JsonObject attributes = block.Attributes;
			if (attributes != null && attributes.IsTrue("ignoreTintInventory"))
			{
				continue;
			}
			string keyAtIndex = game.ColorMaps.GetKeyAtIndex(num2 - 1);
			byte[] array = ColorUtil.ToBGRABytes(game.WorldMap.ApplyColorMapOnRgba(keyAtIndex, null, -1, 180, 138, flipRb: false));
			for (int j = 0; j < 4; j++)
			{
				int num3 = i * 4 + j;
				for (int k = 0; k < 3; k++)
				{
					int num4 = 4 * num3 + k;
					modeldata.Rgba[num4] = (byte)(modeldata.Rgba[num4] * array[k] / 255);
				}
			}
		}
		modeldata.CompactBuffers();
		return modeldata;
	}

	private MeshData Tesselate(TextureSource texSource, Block block, CompositeShape shape, MeshData[][] altblockModelDatas, int altTextureCount, int tilesCount)
	{
		MeshData modeldata;
		try
		{
			Tesselator.TesselateBlock(block, shape, out modeldata, texSource);
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block {0}:", block);
			game.Platform.Logger.Error(e);
			throw;
		}
		modeldata.CompactBuffers();
		int num = ((shape.BakedAlternates != null) ? shape.BakedAlternates.Length : 0);
		int num2 = Math.Max(altTextureCount, num);
		if (num2 != 0)
		{
			MeshData[] array = new MeshData[num2];
			for (int i = 0; i < num2; i++)
			{
				if (altTextureCount > 0)
				{
					texSource.UpdateVariant(block, i % altTextureCount);
				}
				CompositeShape compositeShape = ((num == 0) ? shape : shape.BakedAlternates[i % num]);
				Tesselator.TesselateBlock(block, compositeShape, out var modeldata2, texSource);
				modeldata2.CompactBuffers();
				array[i] = modeldata2;
			}
			altblockModelDatas[block.BlockId] = array;
		}
		else if (tilesCount != 0)
		{
			MeshData[] array2 = new MeshData[tilesCount];
			for (int j = 0; j < tilesCount; j++)
			{
				texSource.UpdateVariant(block, j % tilesCount);
				CompositeShape compositeShape2 = ((num == 0) ? shape : shape.BakedAlternates[j % num]);
				Tesselator.TesselateBlock(block, compositeShape2, out var modeldata3, texSource);
				modeldata3.CompactBuffers();
				array2[j] = modeldata3;
			}
			altblockModelDatas[block.BlockId] = array2;
		}
		return modeldata;
	}

	private static void setLod0Flag(MeshData altModeldata)
	{
		for (int i = 0; i < altModeldata.FlagsCount; i++)
		{
			altModeldata.Flags[i] |= 4096;
		}
	}

	internal void Dispose()
	{
		IDisposable[] refs = blockModelRefsInventory;
		DisposeArray(refs);
		refs = itemModelRefsInventory;
		DisposeArray(refs);
		int num = 0;
		while (altItemModelRefsInventory != null && num < altItemModelRefsInventory.Length)
		{
			refs = altItemModelRefsInventory[num];
			DisposeArray(refs);
			num++;
		}
		unknownItemModelRef?.Dispose();
		unknownBlockModelRef?.Dispose();
		TLTesselator = null;
	}

	private void DisposeArray(IDisposable[] refs)
	{
		if (refs != null)
		{
			for (int i = 0; i < refs.Length; i++)
			{
				refs[i]?.Dispose();
			}
		}
	}

	public MultiTextureMeshRef GetDefaultBlockMeshRef(Block block)
	{
		return blockModelRefsInventory[block.Id];
	}

	public MultiTextureMeshRef GetDefaultItemMeshRef(Item item)
	{
		return itemModelRefsInventory[item.Id];
	}

	public Shape GetCachedShape(AssetLocation location)
	{
		shapes.TryGetValue(location, out var value);
		if (value != null && !value.Loaded)
		{
			value.Load(game, new AssetLocationAndSource(location));
		}
		return value;
	}

	public MeshData CreateMesh(string typeForLogging, CompositeShape cshape, TextureSourceBuilder texgen, ITexPositionSource texSource = null)
	{
		cshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = game.api.Assets.TryGet(cshape.Base)?.ToObject<Shape>();
		if (shape == null)
		{
			return new MeshData(4, 3);
		}
		if (texSource == null)
		{
			texSource = texgen(shape, cshape.Base.ToShortString());
		}
		Tesselator.TesselateShape(typeForLogging, shape, out var modeldata, texSource, (cshape.rotateX == 0f && cshape.rotateY == 0f && cshape.rotateZ == 0f) ? null : new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ), 0, 0, 0);
		return modeldata;
	}

	public void ThreadDispose()
	{
		TLTesselator = null;
	}
}
