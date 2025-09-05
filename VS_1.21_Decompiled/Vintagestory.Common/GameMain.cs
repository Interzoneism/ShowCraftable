using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public abstract class GameMain : IWorldIntersectionSupplier
{
	public AABBIntersectionTest interesectionTester;

	public List<CollectibleObject> Collectibles = new List<CollectibleObject>();

	public IList<Item> Items = new List<Item>();

	public IList<Block> Blocks;

	public TagRegistry TagRegistry = new TagRegistry();

	public List<GridRecipe> GridRecipes = new List<GridRecipe>();

	public OrderedDictionary<string, ColorMap> ColorMaps = new OrderedDictionary<string, ColorMap>();

	public Dictionary<string, RecipeRegistryBase> recipeRegistries = new Dictionary<string, RecipeRegistryBase>();

	public Dictionary<AssetLocation, Item> ItemsByCode = new Dictionary<AssetLocation, Item>();

	public Dictionary<AssetLocation, Block> BlocksByCode = new Dictionary<AssetLocation, Block>();

	private EntitySelection entitySelTmp = new EntitySelection();

	private static readonly Vec3d MidVec3d = new Vec3d(0.5, 0.5, 0.5);

	public Block WaterBlock { get; set; }

	public abstract ClassRegistry ClassRegistryInt { get; set; }

	public abstract IWorldAccessor World { get; }

	protected abstract WorldMap worldmap { get; }

	public AABBIntersectionTest InteresectionTester => interesectionTester;

	public virtual Vec3i MapSize => null;

	public abstract IBlockAccessor blockAccessor { get; }

	public GameMain()
	{
		Blocks = new BlockList(this);
		ClassRegistryInt = new ClassRegistry();
		GridRecipes = RegisterRecipeRegistry<RecipeRegistryGeneric<GridRecipe>>("gridrecipes").Recipes;
		interesectionTester = new AABBIntersectionTest(this);
	}

	public float RandomPitch()
	{
		return (float)World.Rand.NextDouble() * 0.5f + 0.75f;
	}

	public RecipeRegistryBase GetRecipeRegistry(string code)
	{
		recipeRegistries.TryGetValue(code, out var value);
		return value;
	}

	public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
	{
		ClassRegistryInt.RegisterRecipeRegistry<T>(recipeRegistryCode);
		T val = ClassRegistryInt.CreateRecipeRegistry<T>(recipeRegistryCode);
		recipeRegistries[recipeRegistryCode] = val;
		return val;
	}

	public void LoadCollectibles(IList<Item> items, IList<Block> blocks)
	{
		Collectibles = new List<CollectibleObject>();
		foreach (Item item in items)
		{
			if (!(item?.Code == null) && !item.IsMissing)
			{
				Collectibles.Add(item);
			}
		}
		foreach (Block block in blocks)
		{
			if (!block.IsMissing)
			{
				Collectibles.Add(block);
			}
		}
	}

	public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		Ray ray = Ray.FromPositions(fromPos, toPos);
		if (ray != null)
		{
			RayTraceForSelection(supplier, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}
	}

	public void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		Ray ray = Ray.FromPositions(fromPos, toPos);
		if (ray != null)
		{
			RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}
	}

	public void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		Ray ray = Ray.FromAngles(fromPos, pitch, yaw, range);
		if (ray != null)
		{
			RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}
	}

	public void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
	}

	public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		interesectionTester.LoadRayAndPos(ray);
		interesectionTester.bsTester = supplier;
		float num = (float)ray.Length;
		blockSelection = interesectionTester.GetSelectedBlock(num, bfilter);
		Entity[] entitiesAround = supplier.GetEntitiesAround(ray.origin, num, num, (Entity entity3) => efilter == null || efilter(entity3));
		Entity entity = null;
		double num2 = double.MaxValue;
		foreach (Entity entity2 in entitiesAround)
		{
			int selectionBoxIndex = 0;
			if (entity2.IntersectsRay(ray, interesectionTester, out var intersectionDistance, ref selectionBoxIndex) && intersectionDistance < num2)
			{
				entity = entity2;
				num2 = intersectionDistance;
				entitySelTmp.SelectionBoxIndex = selectionBoxIndex;
				entitySelTmp.Entity = entity2;
				entitySelTmp.Face = interesectionTester.hitOnBlockFace;
				entitySelTmp.HitPosition = interesectionTester.hitPosition.SubCopy(entity2.SidedPos.X, entity2.SidedPos.Y, entity2.SidedPos.Z);
				entitySelTmp.Position = entity2.SidedPos.XYZ;
			}
		}
		entitySelection = null;
		if (entity == null)
		{
			return;
		}
		if (blockSelection != null)
		{
			BlockPos position = blockSelection.Position;
			Vec3d pos = new Vec3d(position.X, position.InternalY, position.Z).Add(blockSelection.HitPosition);
			Vec3d pos2 = new Vec3d(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z).Add(entitySelTmp.HitPosition);
			float num4 = ray.origin.SquareDistanceTo(pos2);
			float num5 = ray.origin.SquareDistanceTo(pos);
			if (num4 < num5)
			{
				blockSelection = null;
				entitySelection = entitySelTmp.Clone();
			}
		}
		else
		{
			entitySelection = entitySelTmp.Clone();
		}
	}

	public void RayTraceForSelection(IPlayer player, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		Vec3d fromPos = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
		RayTraceForSelection(fromPos, player.Entity.SidedPos.Pitch, player.Entity.SidedPos.Yaw, player.WorldData.PickingRange, ref blockSelection, ref entitySelection, bfilter, efilter);
	}

	public Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null)
	{
		if (collisionBoxes == null)
		{
			return Array.Empty<Entity>();
		}
		return GetEntitiesAround(MidVec3d.AddCopy(basePos), 5f, 5f, delegate(Entity e)
		{
			if (!matches(e))
			{
				return false;
			}
			for (int i = 0; i < collisionBoxes.Length; i++)
			{
				if (CollisionTester.AabbIntersect(collisionBoxes[i], basePos.X, basePos.Y, basePos.Z, e.SelectionBox, e.Pos.XYZ))
				{
					return true;
				}
			}
			return false;
		});
	}

	public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		int num = (int)((position.X - (double)horRange) / 32.0);
		int num2 = (int)((position.X + (double)horRange) / 32.0);
		int num3 = (int)((position.Y - (double)vertRange) / 32.0);
		int num4 = (int)((position.Y + (double)vertRange) / 32.0);
		int num5 = (int)((position.Z - (double)horRange) / 32.0);
		int num6 = (int)((position.Z + (double)horRange) / 32.0);
		List<Entity> list = new List<Entity>();
		float horRangeSq = horRange * horRange;
		if (matches == null)
		{
			matches = (Entity e) => true;
		}
		for (int num7 = num; num7 <= num2; num7++)
		{
			for (int num8 = num3; num8 <= num4; num8++)
			{
				for (int num9 = num5; num9 <= num6; num9++)
				{
					IWorldChunk chunk = World.BlockAccessor.GetChunk(num7, num8, num9);
					if (chunk == null || chunk.Entities == null)
					{
						continue;
					}
					for (int num10 = 0; num10 < chunk.Entities.Length; num10++)
					{
						Entity entity = chunk.Entities[num10];
						if (entity == null)
						{
							if (num10 >= chunk.EntitiesCount)
							{
								break;
							}
						}
						else if (entity.State != EnumEntityState.Despawned && matches(entity) && entity.InRangeOf(position, horRangeSq, vertRange))
						{
							list.Add(entity);
						}
					}
				}
			}
		}
		return list.ToArray();
	}

	public Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null)
	{
		int num = Math.Min(startPos.X, endPos.X);
		int num2 = Math.Min(startPos.InternalY, endPos.InternalY);
		int num3 = Math.Min(startPos.Z, endPos.Z);
		int num4 = Math.Max(startPos.X, endPos.X);
		int num5 = Math.Max(startPos.InternalY, endPos.InternalY);
		int num6 = Math.Max(startPos.Z, endPos.Z);
		int num7 = num / 32;
		int num8 = num4 / 32;
		int num9 = num2 / 32;
		int num10 = num5 / 32;
		int num11 = num3 / 32;
		int num12 = num6 / 32;
		List<Entity> list = new List<Entity>();
		if (matches == null)
		{
			matches = (Entity e) => true;
		}
		for (int num13 = num7; num13 <= num8; num13++)
		{
			for (int num14 = num9; num14 <= num10; num14++)
			{
				for (int num15 = num11; num15 <= num12; num15++)
				{
					IWorldChunk chunk = World.BlockAccessor.GetChunk(num13, num14, num15);
					if (chunk == null || chunk.Entities == null)
					{
						continue;
					}
					for (int num16 = 0; num16 < chunk.EntitiesCount; num16++)
					{
						Entity entity = chunk.Entities[num16];
						EntityPos sidedPos = entity.SidedPos;
						if (!(sidedPos.X < (double)num) && !(sidedPos.InternalY < (double)num2) && !(sidedPos.Z < (double)num3) && !(sidedPos.X > (double)num4) && !(sidedPos.InternalY > (double)num5) && !(sidedPos.Z > (double)num6) && entity != null && matches(entity) && entity.State != EnumEntityState.Despawned)
						{
							list.Add(entity);
						}
					}
				}
			}
		}
		return list.ToArray();
	}

	public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos, bool liquidSelectable)
	{
		int num = 0;
		List<Cuboidf> list;
		if (blockAccessor.GetChunkAtBlockPos(pos) is WorldChunk worldChunk)
		{
			list = worldChunk.GetDecorSelectionBoxes(blockAccessor, pos);
			num = list.Count;
		}
		else
		{
			list = null;
		}
		Block block;
		int num2;
		Cuboidf[] array;
		if (liquidSelectable && (block = blockAccessor.GetBlock(pos, 2)).IsLiquid())
		{
			num2 = 1;
			array = new Cuboidf[num2 + num];
			array[0] = new Cuboidf(0f, 0f, 0f, 1f, (float)block.LiquidLevel / 8f, 1f);
		}
		else
		{
			Cuboidf[] selectionBoxes = blockAccessor.GetBlock(pos).GetSelectionBoxes(blockAccessor, pos);
			if (num == 0)
			{
				return selectionBoxes;
			}
			num2 = ((selectionBoxes != null) ? selectionBoxes.Length : 0);
			array = new Cuboidf[num2 + num];
			for (int i = 0; i < num2; i++)
			{
				array[i] = selectionBoxes[i];
			}
		}
		for (int j = 0; j < num; j++)
		{
			array[num2 + j] = list[j];
		}
		return array;
	}

	public abstract Block GetBlock(BlockPos pos);

	public abstract bool IsValidPos(BlockPos pos);

	public Item GetItem(AssetLocation itemCode)
	{
		ItemsByCode.TryGetValue(itemCode, out var value);
		return value;
	}

	public Block GetBlock(AssetLocation blockCode)
	{
		if (blockCode == null)
		{
			return null;
		}
		BlocksByCode.TryGetValue(blockCode, out var value);
		return value;
	}

	public Block[] SearchBlocks(AssetLocation wildcard)
	{
		return (Blocks as BlockList).Search(wildcard);
	}

	public Item[] SearchItems(AssetLocation wildcard)
	{
		List<Item> list = new List<Item>();
		foreach (Item item in Items)
		{
			if (item?.Code != null && !item.IsMissing && item.WildCardMatch(wildcard))
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
	{
		return new BlockAccessorCaching(worldmap, World, synchronize, relight);
	}

	public IBlockAccessor GetLockFreeBlockAccessor()
	{
		return new BlockAccessorReadLockfree(worldmap, World);
	}

	public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
	{
		if (strict)
		{
			return new BlockAccessorStrict(worldmap, World, synchronize, relight, debug);
		}
		return new BlockAccessorRelaxed(worldmap, World, synchronize, relight);
	}

	public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
	{
		return new BlockAccessorRelaxedBulkUpdate(worldmap, World, synchronize, relight, debug);
	}

	public IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false)
	{
		return new BlockAccessorBulkMinimalUpdate(worldmap, World, synchronize, debug);
	}

	public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
	{
		return new BlockAccessorRevertable(worldmap, World, synchronize, relight, debug);
	}

	public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
	{
		return new BlockAccessorPrefetch(worldmap, World, synchronize, relight);
	}

	public IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false)
	{
		return new BlockAccessorMapChunkLoading(worldmap, World, synchronize, debug);
	}

	public virtual Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
	{
		return GetBlockIntersectionBoxes(pos, liquidSelectable: false);
	}
}
