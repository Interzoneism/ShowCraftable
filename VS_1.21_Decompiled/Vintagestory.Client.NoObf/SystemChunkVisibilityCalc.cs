using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

public class SystemChunkVisibilityCalc : ClientSystem
{
	private bool doOcclCulling;

	private uint[] visitedBlock;

	private uint iteration;

	private readonly QueueOfInt bfsQueue = new QueueOfInt();

	private const int chunksize = 32;

	private Block[] blocksFast;

	private int[] Blocks;

	public override string Name => "chunkculler";

	public SystemChunkVisibilityCalc(ClientMain game)
		: base(game)
	{
		SystemChunkVisibilityCalc systemChunkVisibilityCalc = this;
		game.eventManager.OnUpdateLighting += OnUpdateLighting;
		game.eventManager.OnChunkLoaded += OnChunkLoaded;
		game.eventManager.AddGameTickListener(onEvery500ms, 500);
		doOcclCulling = ClientSettings.Occlusionculling;
		ClientSettings.Inst.AddWatcher("occlusionculling", delegate(bool nowon)
		{
			bool num = systemChunkVisibilityCalc.doOcclCulling;
			systemChunkVisibilityCalc.doOcclCulling = nowon;
			if (!num && nowon)
			{
				lock (game.chunkPositionsLock)
				{
					foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
					{
						chunk.Value.traversabilityFresh = false;
						ChunkPos item = game.WorldMap.ChunkPosFromChunkIndex3D(chunk.Key);
						if (item.Dimension == 0)
						{
							game.chunkPositionsForRegenTrav.Add(item);
						}
					}
				}
			}
		});
	}

	private void onEvery500ms(float dt)
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["traversethread"] = "traverseQ: " + game.chunkPositionsForRegenTrav.Count;
		}
	}

	public override void OnBlockTexturesLoaded()
	{
		base.OnBlockTexturesLoaded();
		visitedBlock = new uint[32768];
		blocksFast = (game.Blocks as BlockList).BlocksFast;
		Blocks = new int[32768];
	}

	private void OnChunkLoaded(Vec3i chunkpos)
	{
		if (!doOcclCulling)
		{
			return;
		}
		lock (game.chunkPositionsLock)
		{
			game.chunkPositionsForRegenTrav.Add(new ChunkPos(chunkpos));
		}
	}

	private void OnUpdateLighting(int oldBlockId, int newBlockId, BlockPos pos, Dictionary<BlockPos, BlockUpdate> blockUpdatesBulk)
	{
		if (!doOcclCulling)
		{
			return;
		}
		lock (game.chunkPositionsLock)
		{
			if (blockUpdatesBulk != null)
			{
				foreach (KeyValuePair<BlockPos, BlockUpdate> item3 in blockUpdatesBulk)
				{
					if (item3.Value.NewSolidBlockId >= 0 && RequiresRecalc(item3.Value.OldBlockId, item3.Value.NewSolidBlockId))
					{
						ChunkPos item = ChunkPos.FromPosition(item3.Key.X, item3.Key.Y, item3.Key.Z, 0);
						if (!game.chunkPositionsForRegenTrav.Contains(item))
						{
							game.chunkPositionsForRegenTrav.Add(item);
							ClientChunk clientChunk = game.WorldMap.GetClientChunk(item.X, item.Y, item.Z);
							if (clientChunk != null)
							{
								clientChunk.traversabilityFresh = false;
							}
						}
					}
				}
				return;
			}
			if (!RequiresRecalc(oldBlockId, newBlockId))
			{
				return;
			}
			ChunkPos item2 = ChunkPos.FromPosition(pos.X, pos.Y, pos.Z);
			if (!game.chunkPositionsForRegenTrav.Contains(item2))
			{
				game.chunkPositionsForRegenTrav.Add(item2);
				ClientChunk clientChunk2 = game.WorldMap.GetClientChunk(item2.X, item2.Y, item2.Z);
				if (clientChunk2 != null)
				{
					clientChunk2.traversabilityFresh = false;
				}
			}
		}
	}

	private bool RequiresRecalc(int oldblockid, int newblockid)
	{
		Block block = game.Blocks[oldblockid];
		Block block2 = game.Blocks[newblockid];
		if (block.SideOpaque[0] == block2.SideOpaque[0] && block.SideOpaque[1] == block2.SideOpaque[1] && block.SideOpaque[2] == block2.SideOpaque[2] && block.SideOpaque[3] == block2.SideOpaque[3] && block.SideOpaque[4] == block2.SideOpaque[4])
		{
			return block.SideOpaque[5] != block2.SideOpaque[5];
		}
		return true;
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 10;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (game.chunkPositionsForRegenTrav.Count != 0)
		{
			ChunkPos chunkpos;
			lock (game.chunkPositionsLock)
			{
				chunkpos = game.chunkPositionsForRegenTrav.PopOne();
			}
			RegenTraversabilityGraph(chunkpos);
		}
	}

	private void RegenTraversabilityGraph(ChunkPos chunkpos)
	{
		ClientChunk clientChunk = game.WorldMap.GetClientChunk(chunkpos.X, chunkpos.Y, chunkpos.Z);
		if (clientChunk == null || !clientChunk.ChunkHasData())
		{
			return;
		}
		if (clientChunk.Empty)
		{
			setFullyTraversable(clientChunk);
			return;
		}
		clientChunk.ClearTraversable();
		bfsQueue.Clear();
		uint num = ++iteration;
		clientChunk.TemporaryUnpack(Blocks);
		Vec3i vec3i = new Vec3i();
		int num2 = blocksFast.Length;
		for (int i = 0; i < Blocks.Length; i++)
		{
			if (visitedBlock[i] == num)
			{
				continue;
			}
			int num3 = 0;
			bfsQueue.Enqueue(i);
			while (bfsQueue.Count > 0)
			{
				int num4 = bfsQueue.Dequeue();
				int num5 = Blocks[num4];
				if (num5 >= num2)
				{
					continue;
				}
				Block block = blocksFast[num5];
				if (AllSidesOpaque(block))
				{
					continue;
				}
				vec3i.Set(num4 % 32, num4 / 32 / 32, num4 / 32 % 32);
				for (int j = 0; j < 6; j++)
				{
					if (block.SideOpaque[j])
					{
						continue;
					}
					Vec3i vec3i2 = BlockFacing.ALLNORMALI[j];
					int num6 = vec3i.X + vec3i2.X;
					int num7 = vec3i.Y + vec3i2.Y;
					int num8 = vec3i.Z + vec3i2.Z;
					if (DidWeExitChunk(num6, num7, num8))
					{
						num3 |= 1 << j;
						continue;
					}
					int num9 = (num7 * 32 + num8) * 32 + num6;
					if (visitedBlock[num9] != num)
					{
						visitedBlock[num9] = num;
						bfsQueue.Enqueue(num9);
					}
				}
			}
			connectFacesAndSetTraversable(num3, clientChunk);
		}
		clientChunk.traversabilityFresh = true;
	}

	private void connectFacesAndSetTraversable(int exitedFaces, ClientChunk chunk)
	{
		for (int i = 0; i < 6; i++)
		{
			if ((exitedFaces & (1 << i)) == 0)
			{
				continue;
			}
			for (int j = i + 1; j < 6; j++)
			{
				if ((exitedFaces & (1 << j)) != 0)
				{
					chunk.SetTraversable(i, j);
				}
			}
		}
	}

	private void setFullyTraversable(ClientChunk chunk)
	{
		for (int i = 0; i < 6; i++)
		{
			for (int j = i + 1; j < 6; j++)
			{
				chunk.SetTraversable(i, j);
			}
		}
	}

	public bool AllSidesOpaque(Block block)
	{
		if (block.SideOpaque[0] && block.SideOpaque[1] && block.SideOpaque[2] && block.SideOpaque[3] && block.SideOpaque[4])
		{
			return block.SideOpaque[5];
		}
		return false;
	}

	public bool DidWeExitChunk(int posX, int posY, int posZ)
	{
		if (posX >= 0 && posX < 32 && posY >= 0 && posY < 32 && posZ >= 0)
		{
			return posZ >= 32;
		}
		return true;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
