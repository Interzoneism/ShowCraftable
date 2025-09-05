using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class BlockList : IList<Block>, ICollection<Block>, IEnumerable<Block>, IEnumerable
{
	private Block[] blocks;

	private int count;

	private Dictionary<int, Block> noBlocks = new Dictionary<int, Block>();

	private GameMain game;

	public static ModelTransform guitf = ModelTransform.BlockDefaultGui();

	public static ModelTransform fptf = ModelTransform.BlockDefaultFp();

	public static ModelTransform gndtf = ModelTransform.BlockDefaultGround();

	public static ModelTransform tptf = ModelTransform.BlockDefaultTp();

	public Block[] BlocksFast => blocks;

	public Block this[int index]
	{
		get
		{
			if (index >= count)
			{
				return getOrCreateNoBlock(index);
			}
			Block block = blocks[index];
			if (block == null || block.Id != index)
			{
				return blocks[index] = getNoBlock(index, game.World.Api);
			}
			return block;
		}
		set
		{
			if (index != value.Id)
			{
				throw new InvalidOperationException("Trying to add a block at index != id");
			}
			while (index >= count)
			{
				Add(null);
			}
			blocks[index] = value;
		}
	}

	public int Count => count;

	public bool IsReadOnly => false;

	public BlockList(GameMain game, int initialSize = 10000)
	{
		this.game = game;
		blocks = new Block[initialSize];
	}

	public BlockList(GameMain game, Block[] fromBlocks)
	{
		this.game = game;
		blocks = fromBlocks;
		count = fromBlocks.Length;
		for (int i = 0; i < fromBlocks.Length; i++)
		{
			Block block = fromBlocks[i];
			if (block == null || block.Id != i)
			{
				blocks[i] = getNoBlock(i, game.World.Api);
			}
		}
	}

	public void PreAlloc(int atLeastSize)
	{
		if (atLeastSize > blocks.Length)
		{
			Array.Resize(ref blocks, atLeastSize + 10);
		}
	}

	public void Add(Block block)
	{
		if (blocks.Length <= count)
		{
			Array.Resize(ref blocks, blocks.Length + 250);
		}
		blocks[count++] = block;
	}

	public void Clear()
	{
		count = 0;
	}

	public bool Contains(Block item)
	{
		return blocks.Contains(item);
	}

	public Block[] Search(AssetLocation wildcard)
	{
		if (wildcard.Path.Length == 0)
		{
			return Array.Empty<Block>();
		}
		string text = WildcardUtil.Prepare(wildcard.Path);
		if (text == null)
		{
			for (int i = 0; i < blocks.Length; i++)
			{
				if (i > count)
				{
					return Array.Empty<Block>();
				}
				Block block = blocks[i];
				if (block != null && !block.IsMissing && wildcard.Equals(block.Code) && block.Id == i)
				{
					return new Block[1] { block };
				}
			}
			return Array.Empty<Block>();
		}
		List<Block> list = new List<Block>();
		for (int j = 0; j < blocks.Length; j++)
		{
			if (j > count)
			{
				return list.ToArray();
			}
			Block block2 = blocks[j];
			if (block2?.Code != null && !block2.IsMissing && wildcard.WildCardMatch(block2.Code, text) && block2.Id == j)
			{
				list.Add(block2);
			}
		}
		return list.ToArray();
	}

	public void CopyTo(Block[] array, int arrayIndex)
	{
		for (int i = arrayIndex; i < count; i++)
		{
			array[i] = this[i];
		}
	}

	public IEnumerator<Block> GetEnumerator()
	{
		for (int i = 0; i < blocks.Length && i < count; i++)
		{
			Block block = blocks[i];
			if (!(block?.Code == null))
			{
				yield return block;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		for (int i = 0; i < blocks.Length && i < count; i++)
		{
			Block block = blocks[i];
			if (!(block?.Code == null))
			{
				yield return block;
			}
		}
	}

	public int IndexOf(Block item)
	{
		return blocks.IndexOf(item);
	}

	public void Insert(int index, Block item)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	public bool Remove(Block item)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	public void RemoveAt(int index)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	private Block getOrCreateNoBlock(int id)
	{
		if (!noBlocks.TryGetValue(id, out var value))
		{
			value = (noBlocks[id] = getNoBlock(id, game.World.Api));
		}
		return value;
	}

	public static Block getNoBlock(int id, ICoreAPI Api)
	{
		Block block = new Block();
		block.Code = null;
		block.BlockId = id;
		block.IsMissing = true;
		block.Textures = new FastSmallDictionary<string, CompositeTexture>(0);
		block.GuiTransform = guitf;
		block.FpHandTransform = fptf;
		block.GroundTransform = gndtf;
		block.TpHandTransform = tptf;
		block.DrawType = EnumDrawType.Empty;
		block.MatterState = EnumMatterState.Gas;
		block.Sounds = new BlockSounds();
		block.Replaceable = 999;
		block.CollisionBoxes = null;
		block.SelectionBoxes = null;
		block.RainPermeable = true;
		block.AllSidesOpaque = false;
		block.SideSolid = new SmallBoolArray(0);
		block.VertexFlags = new VertexFlags();
		block.OnLoadedNative(Api);
		return block;
	}
}
