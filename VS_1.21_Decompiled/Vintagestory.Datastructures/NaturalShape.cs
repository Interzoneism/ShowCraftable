using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Datastructures;

public class NaturalShape
{
	private readonly Dictionary<Vec2i, ShapeCell> outline;

	private readonly HashSet<Vec2i> inside;

	private readonly IRandom rand;

	private readonly NatFloat natFloat;

	private bool hasSquareStart;

	public NaturalShape(IRandom rand)
	{
		this.rand = rand;
		outline = new Dictionary<Vec2i, ShapeCell>();
		inside = new HashSet<Vec2i>();
		natFloat = NatFloat.createGauss(1f, 1f);
		Init();
	}

	private void Init()
	{
		Vec2i vec2i = new Vec2i();
		outline.Add(vec2i, new ShapeCell(vec2i, new bool[4] { true, true, true, true }));
	}

	public void InitSquare(int sizeX, int sizeZ)
	{
		hasSquareStart = true;
		inside.Clear();
		outline.Clear();
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				Vec2i vec2i = new Vec2i(i, j);
				bool[] array = new bool[4];
				if (j == 0)
				{
					array[0] = true;
				}
				else if (j == sizeZ - 1)
				{
					array[2] = true;
				}
				if (i == 0)
				{
					array[3] = true;
				}
				else if (i == sizeX - 1)
				{
					array[1] = true;
				}
				ShapeCell value = new ShapeCell(vec2i, array);
				if (!array.Any((bool s) => s))
				{
					inside.Add(vec2i);
				}
				else
				{
					outline.Add(vec2i, value);
				}
			}
		}
	}

	public bool[] GetOpenSides(Vec2i c)
	{
		bool[] array = new bool[4];
		for (int i = 0; i < 4; i++)
		{
			Vec2i vec2i = c + GetOffsetByIndex(i);
			bool flag = outline.ContainsKey(vec2i);
			bool flag2 = inside.Contains(vec2i);
			array[i] = !flag && !flag2;
		}
		return array;
	}

	public ShapeCell GetBySide(ShapeCell cell, int index)
	{
		Vec2i offsetByIndex = GetOffsetByIndex(index);
		Vec2i vec2i = cell.Position + offsetByIndex;
		bool[] openSides = GetOpenSides(vec2i);
		return new ShapeCell(vec2i, openSides);
	}

	private static Vec2i GetOffsetByIndex(int index)
	{
		Vec2i vec2i = new Vec2i();
		switch (index)
		{
		case 0:
			vec2i.Set(0, -1);
			break;
		case 1:
			vec2i.Set(1, 0);
			break;
		case 2:
			vec2i.Set(0, 1);
			break;
		case 3:
			vec2i.Set(-1, 0);
			break;
		}
		return vec2i;
	}

	public void Grow(int steps)
	{
		for (int i = 0; i < steps; i++)
		{
			if (hasSquareStart)
			{
				natFloat.avg = (float)outline.Count * 0.5f;
				natFloat.var = (float)outline.Count * 0.5f;
			}
			else
			{
				natFloat.avg = (float)outline.Count * 0.85f;
				natFloat.var = (float)outline.Count * 0.15f;
			}
			int index = (int)natFloat.nextFloat(1f, rand);
			KeyValuePair<Vec2i, ShapeCell> keyValuePair = outline.ElementAt(index);
			index = rand.NextInt(4);
			ShapeCell shapeCell = null;
			for (int j = index; j < index + 4; j++)
			{
				if (keyValuePair.Value.OpenSides[j % 4])
				{
					shapeCell = GetBySide(keyValuePair.Value, j % 4);
					if (shapeCell.OpenSides.Any((bool s) => s))
					{
						outline.TryAdd(shapeCell.Position, shapeCell);
					}
					else
					{
						inside.Add(shapeCell.Position);
					}
					break;
				}
			}
			if (shapeCell == null)
			{
				continue;
			}
			for (int num = 0; num < 4; num++)
			{
				Vec2i offsetByIndex = GetOffsetByIndex(num);
				Vec2i vec2i = shapeCell.Position + offsetByIndex;
				if (outline.TryGetValue(vec2i, out var value))
				{
					value.OpenSides = GetOpenSides(vec2i);
					if (!GetOpenSides(vec2i).Any((bool s) => s))
					{
						outline.Remove(vec2i);
						inside.Add(new Vec2i(vec2i.X, vec2i.Y));
					}
				}
			}
		}
	}

	public List<BlockPos> GetPositions(BlockPos start)
	{
		List<BlockPos> list = new List<BlockPos>();
		foreach (var (vec2i2, _) in outline)
		{
			list.Add(new BlockPos(start.X + vec2i2.X, start.Y, start.Z + vec2i2.Y, 0));
		}
		foreach (Vec2i item in inside)
		{
			list.Add(new BlockPos(start.X + item.X, start.Y, start.Z + item.Y, 0));
		}
		return list;
	}

	public List<Vec2i> GetPositions()
	{
		List<Vec2i> list = new List<Vec2i>();
		foreach (var (item, _) in outline)
		{
			list.Add(item);
		}
		foreach (Vec2i item2 in inside)
		{
			list.Add(item2);
		}
		return list;
	}
}
