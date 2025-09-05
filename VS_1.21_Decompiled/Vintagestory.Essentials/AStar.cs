using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class AStar
{
	protected ICoreServerAPI api;

	protected ICachingBlockAccessor blockAccess;

	public int NodesChecked;

	public double centerOffsetX = 0.5;

	public double centerOffsetZ = 0.5;

	public EnumAICreatureType creatureType;

	protected CollisionTester collTester;

	public PathNodeSet openSet = new PathNodeSet();

	public HashSet<PathNode> closedSet = new HashSet<PathNode>();

	protected readonly Vec3d tmpVec = new Vec3d();

	protected readonly BlockPos tmpPos = new BlockPos();

	protected Cuboidd tmpCub = new Cuboidd();

	public AStar(ICoreServerAPI api)
	{
		this.api = api;
		collTester = new CollisionTester();
		blockAccess = api.World.GetCachingBlockAccessor(synchronize: true, relight: true);
	}

	public virtual List<Vec3d> FindPathAsWaypoints(BlockPos start, BlockPos end, int maxFallHeight, float stepHeight, Cuboidf entityCollBox, int searchDepth = 9999, int mhdistanceTolerance = 0, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		List<PathNode> list = FindPath(start, end, maxFallHeight, stepHeight, entityCollBox, searchDepth, mhdistanceTolerance, creatureType);
		if (list != null)
		{
			return ToWaypoints(list);
		}
		return null;
	}

	public virtual List<PathNode> FindPath(BlockPos start, BlockPos end, int maxFallHeight, float stepHeight, Cuboidf entityCollBox, int searchDepth = 9999, int mhdistanceTolerance = 0, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		if (entityCollBox.XSize > 100f || entityCollBox.YSize > 100f || entityCollBox.ZSize > 100f)
		{
			api.Logger.Warning("AStar:FindPath() was called with a entity box larger than 100 ({0}). Algo not designed for such sizes, likely coding error. Will ignore.", entityCollBox);
			return null;
		}
		this.creatureType = creatureType;
		blockAccess.Begin();
		centerOffsetX = 0.3 + api.World.Rand.NextDouble() * 0.4;
		centerOffsetZ = 0.3 + api.World.Rand.NextDouble() * 0.4;
		NodesChecked = 0;
		PathNode pathNode = new PathNode(start);
		PathNode pathNode2 = new PathNode(end);
		openSet.Clear();
		closedSet.Clear();
		openSet.Add(pathNode);
		while (openSet.Count > 0)
		{
			if (NodesChecked++ > searchDepth)
			{
				return null;
			}
			PathNode pathNode3 = openSet.RemoveNearest();
			closedSet.Add(pathNode3);
			if (pathNode3 == pathNode2 || (mhdistanceTolerance > 0 && Math.Abs(pathNode3.X - pathNode2.X) <= mhdistanceTolerance && Math.Abs(pathNode3.Z - pathNode2.Z) <= mhdistanceTolerance && Math.Abs(pathNode3.Y - pathNode2.Y) <= mhdistanceTolerance))
			{
				return retracePath(pathNode, pathNode3);
			}
			for (int i = 0; i < Cardinal.ALL.Length; i++)
			{
				Cardinal cardinal = Cardinal.ALL[i];
				PathNode pathNode4 = new PathNode(pathNode3, cardinal);
				float extraCost = 0f;
				PathNode pathNode5 = openSet.TryFindValue(pathNode4);
				if ((object)pathNode5 != null)
				{
					float num = pathNode3.gCost + pathNode3.distanceTo(pathNode4);
					if (pathNode5.gCost > num + 0.0001f && traversable(pathNode4, stepHeight, maxFallHeight, entityCollBox, cardinal, ref extraCost) && pathNode5.gCost > num + extraCost + 0.0001f)
					{
						UpdateNode(pathNode3, pathNode5, extraCost);
					}
				}
				else if (!closedSet.Contains(pathNode4) && traversable(pathNode4, stepHeight, maxFallHeight, entityCollBox, cardinal, ref extraCost))
				{
					UpdateNode(pathNode3, pathNode4, extraCost);
					pathNode4.hCost = pathNode4.distanceTo(pathNode2);
					openSet.Add(pathNode4);
				}
			}
		}
		return null;
	}

	protected void UpdateNode(PathNode nearestNode, PathNode neighbourNode, float extraCost)
	{
		neighbourNode.gCost = nearestNode.gCost + nearestNode.distanceTo(neighbourNode) + extraCost;
		neighbourNode.Parent = nearestNode;
		neighbourNode.pathLength = nearestNode.pathLength + 1;
	}

	[Obsolete("Deprecated, please use UpdateNode() instead")]
	protected void addIfNearer(PathNode nearestNode, PathNode neighbourNode, PathNode targetNode, HashSet<PathNode> openSet, float extraCost)
	{
		UpdateNode(nearestNode, neighbourNode, extraCost);
	}

	protected bool traversable(PathNode node, float stepHeight, int maxFallHeight, Cuboidf entityCollBox, Cardinal fromDir, ref float extraCost)
	{
		tmpVec.Set((double)node.X + centerOffsetX, node.Y, (double)node.Z + centerOffsetZ);
		tmpPos.dimension = node.dimension;
		Block block;
		if (!collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
		{
			int num = 0;
			while (true)
			{
				tmpPos.Set(node.X, node.Y - 1, node.Z);
				block = blockAccess.GetBlock(tmpPos, 1);
				if (!block.CanStep)
				{
					return false;
				}
				Block block2 = blockAccess.GetBlock(tmpPos, 2);
				if (block2.IsLiquid())
				{
					float traversalCost = block2.GetTraversalCost(tmpPos, creatureType);
					if (traversalCost > 10000f)
					{
						return false;
					}
					extraCost += traversalCost;
					break;
				}
				if (block2.BlockMaterial == EnumBlockMaterial.Ice)
				{
					block = block2;
				}
				Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccess, tmpPos);
				if (collisionBoxes != null && collisionBoxes.Length != 0)
				{
					float traversalCost2 = block.GetTraversalCost(tmpPos, creatureType);
					if (traversalCost2 > 10000f)
					{
						return false;
					}
					extraCost += traversalCost2;
					break;
				}
				tmpVec.Y -= 1.0;
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
				num++;
				node.Y--;
				maxFallHeight--;
				if (maxFallHeight < 0)
				{
					return false;
				}
			}
			if (fromDir.IsDiagnoal)
			{
				tmpVec.Add((float)(-fromDir.Normali.X) / 2f, 0.0, (float)(-fromDir.Normali.Z) / 2f);
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
			}
			tmpPos.Set(node.X, node.Y, node.Z);
			float traversalCost3 = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
			if (traversalCost3 > 10000f)
			{
				return false;
			}
			extraCost += traversalCost3;
			if (fromDir.IsDiagnoal && creatureType == EnumAICreatureType.Humanoid)
			{
				tmpPos.Set(node.X - fromDir.Normali.X, node.Y, node.Z);
				traversalCost3 = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
				extraCost += traversalCost3 - 1f;
				if (traversalCost3 > 10000f)
				{
					return false;
				}
				tmpPos.Set(node.X, node.Y, node.Z - fromDir.Normali.Z);
				traversalCost3 = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
				extraCost += traversalCost3 - 1f;
				if (traversalCost3 > 10000f)
				{
					return false;
				}
			}
			return true;
		}
		tmpPos.Set(node.X, node.Y, node.Z);
		block = blockAccess.GetBlock(tmpPos, 4);
		if (!block.CanStep)
		{
			return false;
		}
		float traversalCost4 = block.GetTraversalCost(tmpPos, creatureType);
		if (traversalCost4 > 10000f)
		{
			return false;
		}
		if (block.Id != 0)
		{
			extraCost += traversalCost4;
		}
		Block block3 = blockAccess.GetBlock(tmpPos, 2);
		traversalCost4 = block3.GetTraversalCost(tmpPos, creatureType);
		if (traversalCost4 > 10000f)
		{
			return false;
		}
		if (block3.Id != 0)
		{
			extraCost += traversalCost4;
		}
		float num2 = -1f;
		Cuboidf[] collisionBoxes2 = block.GetCollisionBoxes(blockAccess, tmpPos);
		if (collisionBoxes2 != null && collisionBoxes2.Length != 0)
		{
			num2 += collisionBoxes2.Max((Cuboidf cuboid) => cuboid.Y2);
		}
		tmpVec.Set((double)node.X + centerOffsetX, (float)node.Y + stepHeight + num2, (double)node.Z + centerOffsetZ);
		if (!collTester.GetCollidingCollisionBox(blockAccess, entityCollBox, tmpVec, ref tmpCub, alsoCheckTouch: false, node.dimension))
		{
			if (!fromDir.IsDiagnoal)
			{
				node.Y += (int)(1f + num2);
				return true;
			}
			if (collisionBoxes2 != null && collisionBoxes2.Length != 0)
			{
				tmpVec.Add((float)(-fromDir.Normali.X) / 2f, 0.0, (float)(-fromDir.Normali.Z) / 2f);
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
				node.Y += (int)(1f + num2);
				return true;
			}
		}
		return false;
	}

	protected List<PathNode> retracePath(PathNode startNode, PathNode endNode)
	{
		int pathLength = endNode.pathLength;
		List<PathNode> list = new List<PathNode>(pathLength);
		for (int i = 0; i < pathLength; i++)
		{
			list.Add(null);
		}
		PathNode pathNode = endNode;
		for (int num = pathLength - 1; num >= 0; num--)
		{
			list[num] = pathNode;
			pathNode = pathNode.Parent;
		}
		return list;
	}

	public List<Vec3d> ToWaypoints(List<PathNode> path)
	{
		List<Vec3d> list = new List<Vec3d>(path.Count + 1);
		for (int i = 1; i < path.Count; i++)
		{
			list.Add(path[i].ToWaypoint().Add(centerOffsetX, 0.0, centerOffsetZ));
		}
		return list;
	}

	public void Dispose()
	{
		blockAccess?.Dispose();
	}
}
