using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChunkCuller
{
	private ClientMain game;

	private const int chunksize = 32;

	public Ray ray = new Ray();

	private Vec3d planePosition = new Vec3d();

	private Vec3i[] cubicShellPositions;

	private Vec3i centerpos = new Vec3i();

	private bool isAboveHeightLimit;

	public Vec3i curpos = new Vec3i();

	private Vec3i toPos = new Vec3i();

	private int qCount;

	private bool nowOff;

	private const long ExtraDimensionsStart = 4503599627370496L;

	public ChunkCuller(ClientMain game)
	{
		this.game = game;
		ClientSettings.Inst.AddWatcher<int>("viewDistance", genShellVectors);
		genShellVectors(ClientSettings.ViewDistance);
	}

	private void genShellVectors(int viewDistance)
	{
		Vec2i[] octagonPoints = ShapeUtil.GetOctagonPoints(0, 0, viewDistance / 32 + 1);
		int chunkMapSizeY = game.WorldMap.ChunkMapSizeY;
		HashSet<Vec3i> hashSet = new HashSet<Vec3i>();
		foreach (Vec2i vec2i in octagonPoints)
		{
			for (int j = -chunkMapSizeY; j <= chunkMapSizeY; j++)
			{
				hashSet.Add(new Vec3i(vec2i.X, j, vec2i.Y));
			}
		}
		for (int k = 0; k < viewDistance / 32 + 1; k++)
		{
			octagonPoints = ShapeUtil.GetOctagonPoints(0, 0, k);
			foreach (Vec2i vec2i2 in octagonPoints)
			{
				hashSet.Add(new Vec3i(vec2i2.X, -chunkMapSizeY, vec2i2.Y));
				hashSet.Add(new Vec3i(vec2i2.X, chunkMapSizeY, vec2i2.Y));
			}
		}
		cubicShellPositions = hashSet.ToArray();
	}

	public void CullInvisibleChunks()
	{
		if (!ClientSettings.Occlusionculling || game.WorldMap.chunks.Count < 100)
		{
			if (nowOff)
			{
				return;
			}
			ClientChunk.bufIndex = 1;
			lock (game.WorldMap.chunksLock)
			{
				foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
				{
					if (chunk.Key / 4503599627370496L != 1)
					{
						chunk.Value.SetVisible(visible: true);
					}
				}
			}
			ClientChunk.bufIndex = 0;
			lock (game.WorldMap.chunksLock)
			{
				foreach (KeyValuePair<long, ClientChunk> chunk2 in game.WorldMap.chunks)
				{
					if (chunk2.Key / 4503599627370496L != 1)
					{
						chunk2.Value.SetVisible(visible: true);
					}
				}
			}
			nowOff = true;
			return;
		}
		nowOff = false;
		Vec3d cameraPos = game.player.Entity.CameraPos;
		if (centerpos.Equals((int)cameraPos.X / 32, (int)cameraPos.Y / 32, (int)cameraPos.Z / 32) && Math.Abs(game.chunkPositionsForRegenTrav.Count - qCount) < 10)
		{
			return;
		}
		qCount = game.chunkPositionsForRegenTrav.Count;
		centerpos.Set((int)(cameraPos.X / 32.0), (int)(cameraPos.Y / 32.0), (int)(cameraPos.Z / 32.0));
		isAboveHeightLimit = centerpos.Y >= game.WorldMap.ChunkMapSizeY;
		lock (game.WorldMap.chunksLock)
		{
			foreach (KeyValuePair<long, ClientChunk> chunk3 in game.WorldMap.chunks)
			{
				chunk3.Value.SetVisible(visible: false);
			}
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						long key = game.WorldMap.ChunkIndex3D(i + centerpos.X, j + centerpos.Y, k + centerpos.Z);
						if (game.WorldMap.chunks.TryGetValue(key, out var value))
						{
							value.SetVisible(visible: true);
						}
					}
				}
			}
		}
		for (int l = 0; l < cubicShellPositions.Length; l++)
		{
			Vec3i toPosRel = cubicShellPositions[l];
			traverseRayAndMarkVisible(centerpos, toPosRel, 0.25);
			traverseRayAndMarkVisible(centerpos, toPosRel, 0.75);
			traverseRayAndMarkVisible(centerpos, toPosRel, 0.75, 0.0);
		}
		game.chunkRenderer.SwapVisibleBuffers();
	}

	private void traverseRayAndMarkVisible(Vec3i fromPos, Vec3i toPosRel, double yoffset = 0.5, double xoffset = 0.5)
	{
		ray.origin.Set((double)fromPos.X + xoffset, (double)fromPos.Y + yoffset, (double)fromPos.Z + 0.5);
		ray.dir.Set((double)toPosRel.X + xoffset, (double)toPosRel.Y + yoffset, (double)toPosRel.Z + 0.5);
		toPos.Set(fromPos.X + toPosRel.X, fromPos.Y + toPosRel.Y, fromPos.Z + toPosRel.Z);
		curpos.Set(fromPos);
		BlockFacing blockFacing = null;
		int num = fromPos.ManhattenDistanceTo(toPos);
		int num2;
		while ((num2 = curpos.ManhattenDistanceTo(fromPos)) <= num + 2)
		{
			BlockFacing exitingFace = getExitingFace(curpos);
			if (exitingFace == null)
			{
				break;
			}
			long key = ((long)curpos.Y * (long)game.WorldMap.index3dMulZ + curpos.Z) * game.WorldMap.index3dMulX + curpos.X;
			game.WorldMap.chunks.TryGetValue(key, out var value);
			if (value != null)
			{
				value.SetVisible(visible: true);
				if (num2 > 1 && !value.IsTraversable(blockFacing, exitingFace))
				{
					break;
				}
			}
			curpos.Offset(exitingFace);
			blockFacing = exitingFace.Opposite;
			if (!game.WorldMap.IsValidChunkPosFast(curpos.X, curpos.Y, curpos.Z) && (!isAboveHeightLimit || curpos.Y <= 0))
			{
				break;
			}
		}
	}

	private BlockFacing getExitingFace(Vec3i pos)
	{
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			Vec3i normali = blockFacing.Normali;
			double num = (double)normali.X * ray.dir.X + (double)normali.Y * ray.dir.Y + (double)normali.Z * ray.dir.Z;
			if (!(num <= 1E-05))
			{
				planePosition.Set(pos).Add(blockFacing.PlaneCenter);
				double num2 = planePosition.X - ray.origin.X;
				double num3 = planePosition.Y - ray.origin.Y;
				double num4 = planePosition.Z - ray.origin.Z;
				double num5 = (num2 * (double)normali.X + num3 * (double)normali.Y + num4 * (double)normali.Z) / num;
				if (num5 >= 0.0 && Math.Abs(ray.origin.X + ray.dir.X * num5 - planePosition.X) <= 0.5 && Math.Abs(ray.origin.Y + ray.dir.Y * num5 - planePosition.Y) <= 0.5 && Math.Abs(ray.origin.Z + ray.dir.Z * num5 - planePosition.Z) <= 0.5)
				{
					return blockFacing;
				}
			}
		}
		return null;
	}
}
