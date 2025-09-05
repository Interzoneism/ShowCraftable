using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockSelection
{
	public BlockPos Position;

	public BlockFacing Face;

	public Vec3d HitPosition;

	public int SelectionBoxIndex;

	public bool DidOffset;

	public Block Block;

	public Vec3d FullPosition => new Vec3d((double)Position.X + HitPosition.X, (double)Position.InternalY + HitPosition.Y, (double)Position.Z + HitPosition.Z);

	public BlockSelection()
	{
	}

	public BlockSelection(BlockPos pos, BlockFacing face, Block block)
	{
		Position = pos;
		Face = face;
		HitPosition = new Vec3d(1.0, 1.0, 1.0).Offset(face.Normald).Scale(0.5);
		Block = block;
	}

	public BlockSelection SetPos(int x, int y, int z)
	{
		Position.Set(x, y, z);
		return this;
	}

	public BlockSelection AddPosCopy(int x, int y, int z)
	{
		BlockSelection blockSelection = Clone();
		blockSelection.Position.Add(x, y, z);
		return blockSelection;
	}

	public BlockSelection AddPosCopy(Vec3i vec)
	{
		BlockSelection blockSelection = Clone();
		blockSelection.Position.Add(vec);
		return blockSelection;
	}

	public BlockSelection Clone()
	{
		return new BlockSelection
		{
			Face = Face,
			HitPosition = HitPosition?.Clone(),
			SelectionBoxIndex = SelectionBoxIndex,
			Position = Position?.Copy(),
			DidOffset = DidOffset
		};
	}

	public int ToDecorIndex()
	{
		int vx = (int)(HitPosition.X * 16.0);
		int vy = 15 - (int)(HitPosition.Y * 16.0);
		int vz = (int)(HitPosition.Z * 16.0);
		return new DecorBits(Face, vx, vy, vz);
	}

	[Obsolete("Use (int)new DecorBits(face, x, y, z) instead, which has the same functionality")]
	public static int GetDecorIndex(BlockFacing face, int x, int y, int z)
	{
		return new DecorBits(face, x, y, z);
	}

	[Obsolete("Use (int)new DecorBits(face) instead, which has the same functionality")]
	public static int GetDecorIndex(BlockFacing face)
	{
		return new DecorBits(face);
	}
}
