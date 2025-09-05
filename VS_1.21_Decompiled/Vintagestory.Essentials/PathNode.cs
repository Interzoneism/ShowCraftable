using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Essentials;

public class PathNode : BlockPos, IEquatable<PathNode>
{
	public float gCost;

	public float hCost;

	public PathNode Parent;

	public int pathLength;

	public EnumTraverseAction Action;

	public float fCost => gCost + hCost;

	public int HeapIndex { get; set; }

	public PathNode(PathNode nearestNode, Cardinal card)
		: base(nearestNode.X + card.Normali.X, nearestNode.Y + card.Normali.Y, nearestNode.Z + card.Normali.Z)
	{
		dimension = nearestNode.dimension;
	}

	public PathNode(BlockPos pos)
		: base(pos.X, pos.Y, pos.Z)
	{
		dimension = pos.dimension;
	}

	public bool Equals(PathNode other)
	{
		if (other.X == X && other.Y == Y)
		{
			return other.Z == Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PathNode)
		{
			return Equals(obj as PathNode);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool operator ==(PathNode left, PathNode right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(PathNode left, PathNode right)
	{
		return !(left == right);
	}

	public float distanceTo(PathNode node)
	{
		int num = Math.Abs(node.X - X);
		int num2 = Math.Abs(node.Z - Z);
		if (num <= num2)
		{
			return (float)(num2 - num) + 1.4142137f * (float)num;
		}
		return (float)(num - num2) + 1.4142137f * (float)num2;
	}

	public Vec3d ToWaypoint()
	{
		return new Vec3d(X, base.InternalY, Z);
	}

	public int CompareTo(PathNode other)
	{
		int num = fCost.CompareTo(other.fCost);
		if (num == 0)
		{
			num = hCost.CompareTo(other.hCost);
		}
		return -num;
	}
}
