using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.MathTools;

public class MultiCollisionTester
{
	public CachedCuboidList CollisionBoxList = new CachedCuboidList();

	public Cuboidd[] entityBox = ArrayUtil.CreateFilled(10, (int i) => new Cuboidd());

	protected int count;

	private Cuboidf[] collBox = new Cuboidf[10];

	public BlockPos tmpPos = new BlockPos();

	public Vec3d tmpPosDelta = new Vec3d();

	public BlockPos minPos = new BlockPos();

	public BlockPos maxPos = new BlockPos();

	public Vec3d pos = new Vec3d();

	private readonly Cuboidd tmpBox = new Cuboidd();

	private readonly BlockPos blockPos = new BlockPos();

	private readonly Vec3d blockPosVec = new Vec3d();

	public void ApplyTerrainCollision(Cuboidf[] collisionBoxes, int collBoxCount, Entity entity, EntityPos entityPos, float dtFactor, ref Vec3d newPosition, float stepHeight = 1f, float yExtra = 1f)
	{
		count = collBoxCount;
		IWorldAccessor world = entity.World;
		pos.X = entityPos.X;
		pos.Y = entityPos.Y;
		pos.Z = entityPos.Z;
		EnumPushDirection direction = EnumPushDirection.None;
		for (int i = 0; i < collBoxCount; i++)
		{
			entityBox[i].SetAndTranslate(collisionBoxes[i], pos.X, pos.Y, pos.Z);
		}
		double num = entityPos.Motion.X * (double)dtFactor;
		double num2 = entityPos.Motion.Y * (double)dtFactor;
		double num3 = entityPos.Motion.Z * (double)dtFactor;
		GenerateCollisionBoxList(world.BlockAccessor, num, num2, num3, stepHeight, yExtra);
		bool collidedVertically = false;
		int num4 = CollisionBoxList.Count;
		for (int j = 0; j < num4; j++)
		{
			for (int k = 0; k < collBoxCount; k++)
			{
				num2 = CollisionBoxList.cuboids[j].pushOutY(entityBox[k], num2, ref direction);
				if (direction != EnumPushDirection.None)
				{
					CollisionBoxList.blocks[j].OnEntityCollide(world, entity, CollisionBoxList.positions[j], (direction == EnumPushDirection.Negative) ? BlockFacing.UP : BlockFacing.DOWN, tmpPosDelta.Set(num, num2, num3), !entity.CollidedVertically);
					collidedVertically = true;
				}
			}
		}
		for (int l = 0; l < collBoxCount; l++)
		{
			entityBox[l].Translate(0.0, num2, 0.0);
		}
		entity.CollidedVertically = collidedVertically;
		bool flag = false;
		for (int m = 0; m < collBoxCount; m++)
		{
			entityBox[m].Translate(num, 0.0, num3);
		}
		foreach (Cuboidd collisionBox in CollisionBoxList)
		{
			bool flag2 = false;
			for (int n = 0; n < collBoxCount; n++)
			{
				if (collisionBox.Intersects(entityBox[n]))
				{
					flag = true;
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				break;
			}
		}
		for (int num5 = 0; num5 < collBoxCount; num5++)
		{
			entityBox[num5].Translate(0.0 - num, 0.0, 0.0 - num3);
		}
		collidedVertically = false;
		if (flag)
		{
			for (int num6 = 0; num6 < num4; num6++)
			{
				bool flag3 = false;
				for (int num7 = 0; num7 < collBoxCount; num7++)
				{
					num = CollisionBoxList.cuboids[num6].pushOutX(entityBox[num7], num, ref direction);
					if (direction != EnumPushDirection.None)
					{
						CollisionBoxList.blocks[num6].OnEntityCollide(world, entity, CollisionBoxList.positions[num6], (direction == EnumPushDirection.Negative) ? BlockFacing.EAST : BlockFacing.WEST, tmpPosDelta.Set(num, num2, num3), !entity.CollidedHorizontally);
					}
					flag3 = flag3 || direction != EnumPushDirection.None;
				}
				collidedVertically = flag3;
			}
			for (int num8 = 0; num8 < collBoxCount; num8++)
			{
				entityBox[num8].Translate(num, 0.0, 0.0);
			}
			for (int num9 = 0; num9 < num4; num9++)
			{
				bool flag4 = false;
				for (int num10 = 0; num10 < collBoxCount; num10++)
				{
					num3 = CollisionBoxList.cuboids[num9].pushOutZ(entityBox[num10], num3, ref direction);
					if (direction != EnumPushDirection.None)
					{
						CollisionBoxList.blocks[num9].OnEntityCollide(world, entity, CollisionBoxList.positions[num9], (direction == EnumPushDirection.Negative) ? BlockFacing.SOUTH : BlockFacing.NORTH, tmpPosDelta.Set(num, num2, num3), !entity.CollidedHorizontally);
					}
					flag4 = flag4 || direction != EnumPushDirection.None;
				}
				collidedVertically = flag4;
			}
		}
		entity.CollidedHorizontally = collidedVertically;
		newPosition.Set(pos.X + num, pos.Y + num2, pos.Z + num3);
	}

	protected virtual void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra)
	{
		double num = double.MaxValue;
		double num2 = double.MaxValue;
		double num3 = double.MaxValue;
		double num4 = double.MinValue;
		double num5 = double.MinValue;
		double num6 = double.MinValue;
		for (int i = 0; i < count; i++)
		{
			Cuboidd cuboidd = entityBox[i];
			num = Math.Min(num, cuboidd.X1);
			num2 = Math.Min(num2, cuboidd.Y1);
			num3 = Math.Min(num3, cuboidd.Z1);
			num4 = Math.Max(num4, cuboidd.X2);
			num5 = Math.Max(num5, cuboidd.Y2);
			num6 = Math.Max(num6, cuboidd.Z2);
		}
		minPos.Set((int)(num + Math.Min(0.0, motionX)), (int)(num2 + Math.Min(0.0, motionY) - (double)yExtra), (int)(num3 + Math.Min(0.0, motionZ)));
		double num7 = Math.Max(num2 + (double)stepHeight, num5);
		maxPos.Set((int)(num4 + Math.Max(0.0, motionX)), (int)(num7 + Math.Max(0.0, motionY)), (int)(num6 + Math.Max(0.0, motionZ)));
		CollisionBoxList.Clear();
		blockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, tmpPos.Set(x, y, z));
			if (collisionBoxes != null)
			{
				CollisionBoxList.Add(collisionBoxes, x, y, z, block);
			}
		}, centerOrder: true);
	}

	public Cuboidd GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf[] ecollisionBoxes, int collBoxCount, Vec3d pos, bool alsoCheckTouch = true)
	{
		for (int i = 0; i < collBoxCount; i++)
		{
			Cuboidf obj = ecollisionBoxes[i];
			BlockPos blockPos = new BlockPos();
			Vec3d vec3d = new Vec3d();
			Cuboidd cuboidd = obj.ToDouble().Translate(pos);
			cuboidd.Y1 = Math.Round(cuboidd.Y1, 5);
			int num = (int)((double)obj.X1 + pos.X);
			int num2 = (int)((double)obj.Y1 + pos.Y - 1.0);
			int num3 = (int)((double)obj.Z1 + pos.Z);
			int num4 = (int)Math.Ceiling((double)obj.X2 + pos.X);
			int num5 = (int)Math.Ceiling((double)obj.Y2 + pos.Y);
			int num6 = (int)Math.Ceiling((double)obj.Z2 + pos.Z);
			for (int j = num2; j <= num5; j++)
			{
				for (int k = num; k <= num4; k++)
				{
					for (int l = num3; l <= num6; l++)
					{
						Block mostSolidBlock = blockAccessor.GetMostSolidBlock(k, j, l);
						blockPos.Set(k, j, l);
						vec3d.Set(k, j, l);
						Cuboidf[] collisionBoxes = mostSolidBlock.GetCollisionBoxes(blockAccessor, blockPos);
						if (collisionBoxes == null)
						{
							continue;
						}
						foreach (Cuboidf cuboidf in collisionBoxes)
						{
							if (cuboidf != null && (alsoCheckTouch ? cuboidd.IntersectsOrTouches(cuboidf, vec3d) : cuboidd.Intersects(cuboidf, vec3d)))
							{
								return cuboidf.ToDouble().Translate(blockPos);
							}
						}
					}
				}
			}
		}
		return null;
	}
}
