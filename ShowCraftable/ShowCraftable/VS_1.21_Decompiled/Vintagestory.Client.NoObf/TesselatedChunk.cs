using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TesselatedChunk : IComparable<TesselatedChunk>, IMergeable<TesselatedChunk>
{
	internal int positionX;

	internal int positionYAndDimension;

	internal int positionZ;

	internal int priority;

	internal Bools CullVisible;

	internal ClientChunk chunk;

	internal TesselatedChunkPart[] centerParts;

	internal TesselatedChunkPart[] edgeParts;

	internal int VerticesCount;

	internal Sphere boundingSphere;

	public int CompareTo(TesselatedChunk obj)
	{
		return priority - obj.priority;
	}

	public bool MergeIfEqual(TesselatedChunk otc)
	{
		if (positionX == otc.positionX && positionYAndDimension == otc.positionYAndDimension && positionZ == otc.positionZ)
		{
			if (otc.centerParts != null)
			{
				Dispose(centerParts);
				centerParts = otc.centerParts;
			}
			if (otc.edgeParts != null)
			{
				Dispose(edgeParts);
				edgeParts = otc.edgeParts;
			}
			return true;
		}
		return false;
	}

	internal bool AddCenterToPools(ChunkRenderer chunkRenderer, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, ClientChunk hostChunk)
	{
		if (centerParts != null)
		{
			bool hiddenState = hostChunk.GetHiddenState(ref hostChunk.centerModelPoolLocations);
			chunk.RemoveCenterDataPoolLocations(chunkRenderer);
			List<ModelDataPoolLocation> list = new List<ModelDataPoolLocation>(centerParts.Length);
			for (int i = 0; i < centerParts.Length; i++)
			{
				centerParts[i].AddToPools(chunkRenderer, list, chunkOrigin, dimension, boundingSphere, CullVisible);
			}
			hostChunk.SetPoolLocations(ref hostChunk.centerModelPoolLocations, list.ToArray(), hiddenState);
			return true;
		}
		return false;
	}

	internal bool AddEdgeToPools(ChunkRenderer chunkRenderer, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, ClientChunk hostChunk)
	{
		if (edgeParts != null)
		{
			bool hiddenState = hostChunk.GetHiddenState(ref hostChunk.edgeModelPoolLocations);
			chunkRenderer.QuantityRenderingChunks -= chunk.RemoveEdgeDataPoolLocations(chunkRenderer);
			List<ModelDataPoolLocation> list = new List<ModelDataPoolLocation>(edgeParts.Length);
			for (int i = 0; i < edgeParts.Length; i++)
			{
				edgeParts[i].AddToPools(chunkRenderer, list, chunkOrigin, dimension, boundingSphere, CullVisible);
			}
			hostChunk.SetPoolLocations(ref hostChunk.edgeModelPoolLocations, list.ToArray(), hiddenState);
			chunkRenderer.QuantityRenderingChunks++;
			return true;
		}
		return false;
	}

	internal void RecalcPriority(ClientPlayer player)
	{
		int num = positionX - player.Entity.Pos.XInt;
		if (num < 0)
		{
			num = ((num >= -32) ? (num + 16) : (num + 32));
		}
		int num2 = positionZ - player.Entity.Pos.ZInt;
		if (num2 < 0)
		{
			num2 = ((num2 >= -32) ? (num2 + 16) : (num2 + 32));
		}
		float value = GameMath.AngleRadDistance((float)Math.Atan2(-num2, num), player.CameraYaw);
		priority = (int)(1000.0 * Math.Sqrt(Math.Sqrt(num * num + num2 * num2)) * (double)Math.Abs(value)) - 1000 * positionYAndDimension / 32;
	}

	internal void SetBounds(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
	{
		boundingSphere = new Sphere((float)positionX + (xMax + xMin) / 2f, (float)positionYAndDimension + (yMax + yMin) / 2f, (float)positionZ + (zMax + zMin) / 2f, Math.Max(0f, xMax - xMin), Math.Max(0f, yMax - yMin), Math.Max(0f, zMax - zMin));
	}

	internal void UnusedDispose()
	{
		Dispose(centerParts);
		Dispose(edgeParts);
	}

	private void Dispose(TesselatedChunkPart[] parts)
	{
		if (parts != null)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i].Dispose();
			}
		}
	}
}
