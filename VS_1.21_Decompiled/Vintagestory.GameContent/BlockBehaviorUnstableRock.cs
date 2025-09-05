using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("UnstableRockStabilization", "The vertical stabilization that this block gives to nearby unstable rock blocks.", "System.Int32", "Optional", "0", true)]
[AddDocumentationProperty("MaxCollapseDistance", "Obsolete. No longer used.", "System.Single", "Obsolete", "1", false)]
public class BlockBehaviorUnstableRock : BlockBehavior, IConditionalChiselable
{
	[DocumentAsJson("Optional", "effect/rockslide", false)]
	protected AssetLocation fallSound = new AssetLocation("effect/rockslide");

	[DocumentAsJson("Optional", "1", false)]
	protected float dustIntensity = 1f;

	[DocumentAsJson("Optional", "1", false)]
	protected float impactDamageMul = 1f;

	[DocumentAsJson("Optional", "None", false)]
	protected AssetLocation collapsedBlockLoc;

	protected Block collapsedBlock;

	[DocumentAsJson("Optional", "0.25", false)]
	protected float collapseChance = 0.25f;

	protected float maxSupportSearchDistanceSq = 36f;

	[DocumentAsJson("Optional", "2", false)]
	protected float maxSupportDistance = 2f;

	private ICoreAPI api;

	public bool AllowFallingBlocks;

	public bool CaveIns;

	private bool Enabled
	{
		get
		{
			if (CaveIns)
			{
				return AllowFallingBlocks;
			}
			return false;
		}
	}

	public BlockBehaviorUnstableRock(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		dustIntensity = properties["dustIntensity"].AsFloat(1f);
		collapseChance = properties["collapseChance"].AsFloat(0.25f);
		maxSupportDistance = properties["maxSupportDistance"].AsFloat(2f);
		string text = properties["fallSound"].AsString();
		if (text != null)
		{
			fallSound = AssetLocation.Create(text, block.Code.Domain);
		}
		impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
		string text2 = properties["collapsedBlock"].AsString();
		if (text2 != null)
		{
			collapsedBlockLoc = AssetLocation.Create(text2, block.Code.Domain);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		this.api = api;
		collapsedBlock = ((collapsedBlockLoc == null) ? block : (api.World.GetBlock(collapsedBlockLoc) ?? block));
		AllowFallingBlocks = api.World.Config.GetBool("allowFallingBlocks");
		CaveIns = api.World.Config.GetString("caveIns") == "on";
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		base.OnBlockBroken(world, pos, byPlayer, ref handling);
		checkCollapsibleNeighbours(world, pos);
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
	{
		base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
		checkCollapsibleNeighbours(world, pos);
	}

	protected void checkCollapsibleNeighbours(IWorldAccessor world, BlockPos pos)
	{
		if (Enabled)
		{
			BlockFacing[] array = (BlockFacing[])BlockFacing.ALLFACES.Clone();
			GameMath.Shuffle(world.Rand, array);
			for (int i = 0; i < array.Length && i < 3 && !CheckCollapsible(world, pos.AddCopy(array[i])); i++)
			{
			}
		}
	}

	public bool CheckCollapsible(IWorldAccessor world, BlockPos pos)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return false;
		}
		if (!Enabled)
		{
			return false;
		}
		if (!world.BlockAccessor.GetBlock(pos, 1).HasBehavior<BlockBehaviorUnstableRock>())
		{
			return false;
		}
		CollapsibleSearchResult collapsibleSearchResult = searchCollapsible(pos, ignoreBeams: false);
		if (collapsibleSearchResult.Unconnected)
		{
			collapse(world, collapsibleSearchResult.SupportPositions, pos);
		}
		else
		{
			if (world.Rand.NextDouble() + 0.001 > (double)collapsibleSearchResult.Instability)
			{
				return false;
			}
			if (world.Rand.NextDouble() > (double)collapseChance)
			{
				return false;
			}
			collapse(world, collapsibleSearchResult.SupportPositions, pos);
		}
		return true;
	}

	private void collapse(IWorldAccessor world, List<Vec4i> supportPositions, BlockPos startPos)
	{
		List<BlockPos> nearestUnstableBlocks = getNearestUnstableBlocks(world, supportPositions, startPos);
		if (nearestUnstableBlocks.Any())
		{
			IOrderedEnumerable<BlockPos> orderedEnumerable = nearestUnstableBlocks.OrderBy((BlockPos pos) => pos.Y);
			int y = orderedEnumerable.First().Y;
			collapseLayer(world, orderedEnumerable, y);
		}
	}

	private void collapseLayer(IWorldAccessor world, IOrderedEnumerable<BlockPos> yorderedPositions, int y)
	{
		foreach (BlockPos pos in yorderedPositions)
		{
			if (pos.Y < y)
			{
				continue;
			}
			if (pos.Y > y)
			{
				world.Api.Event.RegisterCallback(delegate
				{
					collapseLayer(world, yorderedPositions, pos.Y);
				}, 200);
				return;
			}
			if (world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(pos)) == null)
			{
				BlockBehaviorUnstableRock behavior = world.BlockAccessor.GetBlock(pos, 1).GetBehavior<BlockBehaviorUnstableRock>();
				if (behavior != null)
				{
					EntityBlockFalling entity = new EntityBlockFalling(behavior.collapsedBlock, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
					world.SpawnEntity(entity);
				}
			}
		}
		BlockPos blockPos = yorderedPositions.First();
		for (int num = 0; num < 3; num++)
		{
			checkCollapsibleNeighbours(world, blockPos.AddCopy(world.Rand.Next(17) - 8, 0, world.Rand.Next(17) - 8));
		}
	}

	private CollapsibleSearchResult searchCollapsible(BlockPos startPos, bool ignoreBeams)
	{
		CollapsibleSearchResult nearestVerticalSupports = getNearestVerticalSupports(api.World, startPos);
		nearestVerticalSupports.NearestSupportDistance = 9999f;
		foreach (Vec4i supportPosition in nearestVerticalSupports.SupportPositions)
		{
			nearestVerticalSupports.NearestSupportDistance = Math.Min(nearestVerticalSupports.NearestSupportDistance, GameMath.Sqrt(Math.Max(0f, supportPosition.HorDistanceSqTo(startPos.X, startPos.Z) - (float)(supportPosition.W - 1))));
		}
		if (ignoreBeams)
		{
			nearestVerticalSupports.Instability = Math.Clamp(nearestVerticalSupports.NearestSupportDistance / maxSupportDistance, 0f, 99f);
			return nearestVerticalSupports;
		}
		StartEnd beamstartend;
		double stableMostBeam = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>().GetStableMostBeam(startPos, out beamstartend);
		nearestVerticalSupports.NearestSupportDistance = (float)Math.Min(nearestVerticalSupports.NearestSupportDistance, stableMostBeam);
		nearestVerticalSupports.Instability = Math.Clamp(nearestVerticalSupports.NearestSupportDistance / maxSupportDistance, 0f, 99f);
		return nearestVerticalSupports;
	}

	private float getNearestSupportDistance(List<Vec4i> supportPositions, BlockPos startPos)
	{
		float num = 99f;
		if (supportPositions.Count == 0)
		{
			return num;
		}
		foreach (Vec4i supportPosition in supportPositions)
		{
			num = Math.Min(num, supportPosition.HorDistanceSqTo(startPos.X, startPos.Z) - (float)(supportPosition.W - 1));
		}
		return GameMath.Sqrt(num);
	}

	private List<BlockPos> getNearestUnstableBlocks(IWorldAccessor world, List<Vec4i> supportPositions, BlockPos startPos)
	{
		Queue<BlockPos> queue = new Queue<BlockPos>();
		queue.Enqueue(startPos);
		HashSet<BlockPos> hashSet = new HashSet<BlockPos>();
		List<BlockPos> list = new List<BlockPos>();
		int num = 2 + world.Rand.Next(30) + world.Rand.Next(11) * world.Rand.Next(11);
		int num2 = 1 + world.Rand.Next(3);
		while (queue.Count > 0)
		{
			BlockPos blockPos = queue.Dequeue();
			if (hashSet.Contains(blockPos))
			{
				continue;
			}
			hashSet.Add(blockPos);
			for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
			{
				BlockPos blockPos2 = blockPos.AddCopy(BlockFacing.ALLFACES[i]);
				if (blockPos2.HorDistanceSqTo(startPos.X, startPos.Z) > 144f || blockPos2.Y - startPos.Y >= num2 || !world.BlockAccessor.GetBlock(blockPos2, 1).HasBehavior<BlockBehaviorUnstableRock>() || !(getNearestSupportDistance(supportPositions, blockPos2) > 0f))
				{
					continue;
				}
				list.Add(blockPos2);
				for (int j = 1; j < 4; j++)
				{
					if (world.BlockAccessor.GetBlockBelow(blockPos2, j, 1).HasBehavior<BlockBehaviorUnstableRock>() && getVerticalSupportStrength(world, blockPos2) == 0)
					{
						list.Add(blockPos2.DownCopy(j));
					}
				}
				if (list.Count > num)
				{
					return list;
				}
				queue.Enqueue(blockPos2);
			}
		}
		return list;
	}

	private CollapsibleSearchResult getNearestVerticalSupports(IWorldAccessor world, BlockPos startpos)
	{
		Queue<BlockPos> queue = new Queue<BlockPos>();
		queue.Enqueue(startpos);
		HashSet<BlockPos> hashSet = new HashSet<BlockPos>();
		CollapsibleSearchResult collapsibleSearchResult = new CollapsibleSearchResult();
		collapsibleSearchResult.SupportPositions = new List<Vec4i>();
		int verticalSupportStrength;
		if ((verticalSupportStrength = getVerticalSupportStrength(world, startpos)) > 0)
		{
			collapsibleSearchResult.SupportPositions.Add(new Vec4i(startpos, verticalSupportStrength));
			return collapsibleSearchResult;
		}
		collapsibleSearchResult.Unconnected = true;
		IBlockAccessor blockAccessor = world.BlockAccessor;
		while (queue.Count > 0)
		{
			BlockPos blockPos = queue.Dequeue();
			if (hashSet.Contains(blockPos))
			{
				continue;
			}
			hashSet.Add(blockPos);
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
				BlockPos blockPos2 = blockPos.AddCopy(blockFacing);
				float num = blockPos2.HorDistanceSqTo(startpos.X, startpos.Z);
				Block block = blockAccessor.GetBlock(blockPos2);
				if (block.SideIsSolid(blockAccessor, blockPos2, i) && block.SideIsSolid(blockAccessor, blockPos2, blockFacing.Opposite.Index))
				{
					if (num > maxSupportSearchDistanceSq)
					{
						collapsibleSearchResult.Unconnected = !block.SideIsSolid(blockAccessor, blockPos2, BlockFacing.DOWN.Index);
					}
					else if ((verticalSupportStrength = getVerticalSupportStrength(world, blockPos2)) > 0)
					{
						collapsibleSearchResult.Unconnected = false;
						collapsibleSearchResult.SupportPositions.Add(new Vec4i(blockPos2, verticalSupportStrength));
					}
					else
					{
						queue.Enqueue(blockPos2);
					}
				}
			}
		}
		return collapsibleSearchResult;
	}

	public static int getVerticalSupportStrength(IWorldAccessor world, BlockPos npos)
	{
		BlockPos blockPos = new BlockPos();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		for (int i = 1; i < 5; i++)
		{
			int y = GameMath.Clamp(npos.Y - i, 0, npos.Y);
			blockPos.Set(npos.X, y, npos.Z);
			Block block = blockAccessor.GetBlock(blockPos);
			int num = block.Attributes?["unstableRockStabilization"].AsInt() ?? 0;
			if (num > 0)
			{
				return num;
			}
			if (!block.SideIsSolid(blockAccessor, blockPos, BlockFacing.UP.Index) || !block.SideIsSolid(blockAccessor, blockPos, BlockFacing.DOWN.Index))
			{
				return 0;
			}
		}
		return 1;
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!Enabled)
		{
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		return Lang.Get("instability-percent", getInstability(pos) * 100.0);
	}

	public double getInstability(BlockPos pos)
	{
		return Math.Clamp(searchCollapsible(pos, ignoreBeams: false).NearestSupportDistance / maxSupportDistance, 0f, 1f);
	}

	public bool CanChisel(IWorldAccessor world, BlockPos pos, IPlayer player, out string errorCode)
	{
		errorCode = null;
		if (!Enabled)
		{
			return true;
		}
		if (getInstability(pos) >= 1.0 && player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			errorCode = "cantchisel-toounstable";
			return false;
		}
		return true;
	}
}
