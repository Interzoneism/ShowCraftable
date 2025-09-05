using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.MathTools;

public class CollisionTester
{
	public CachedCuboidListFaster CollisionBoxList = new CachedCuboidListFaster();

	public Cuboidd entityBox = new Cuboidd();

	public BlockPos tmpPos = new BlockPos();

	public Vec3d tmpPosDelta = new Vec3d();

	public BlockPos minPos = new BlockPos();

	public BlockPos maxPos = new BlockPos();

	public Vec3d pos = new Vec3d();

	private readonly Cuboidd tmpBox = new Cuboidd();

	private readonly BlockPos blockPos = new BlockPos();

	private readonly Vec3d blockPosVec = new Vec3d();

	private readonly BlockPos collBlockPos = new BlockPos();

	public void ApplyTerrainCollision(Entity entity, EntityPos entityPos, float dtFactor, ref Vec3d newPosition, float stepHeight = 1f, float yExtra = 1f)
	{
		minPos.dimension = entityPos.Dimension;
		IWorldAccessor world = entity.World;
		Vec3d vec3d = pos;
		Cuboidd cuboidd = entityBox;
		vec3d.X = entityPos.X;
		vec3d.Y = entityPos.Y;
		vec3d.Z = entityPos.Z;
		EnumPushDirection direction = EnumPushDirection.None;
		cuboidd.SetAndTranslate(entity.CollisionBox, vec3d.X, vec3d.Y, vec3d.Z);
		double num = entityPos.Motion.X * (double)dtFactor;
		double num2 = entityPos.Motion.Y * (double)dtFactor;
		double num3 = entityPos.Motion.Z * (double)dtFactor;
		double num4 = 0.0001;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		if (num > num4)
		{
			num5 = num4;
		}
		if (num < 0.0 - num4)
		{
			num5 = 0.0 - num4;
		}
		if (num2 > num4)
		{
			num6 = num4;
		}
		if (num2 < 0.0 - num4)
		{
			num6 = 0.0 - num4;
		}
		if (num3 > num4)
		{
			num7 = num4;
		}
		if (num3 < 0.0 - num4)
		{
			num7 = 0.0 - num4;
		}
		num += num5;
		num2 += num6;
		num3 += num7;
		GenerateCollisionBoxList(world.BlockAccessor, num, num2, num3, stepHeight, yExtra, entityPos.Dimension);
		bool collidedVertically = false;
		int count = CollisionBoxList.Count;
		Cuboidd[] cuboids = CollisionBoxList.cuboids;
		collBlockPos.dimension = entityPos.Dimension;
		for (int i = 0; i < cuboids.Length && i < count; i++)
		{
			num2 = cuboids[i].pushOutY(cuboidd, num2, ref direction);
			if (direction != EnumPushDirection.None)
			{
				collidedVertically = true;
				collBlockPos.Set(CollisionBoxList.positions[i]);
				CollisionBoxList.blocks[i].OnEntityCollide(world, entity, collBlockPos, (direction == EnumPushDirection.Negative) ? BlockFacing.UP : BlockFacing.DOWN, tmpPosDelta.Set(num, num2, num3), !entity.CollidedVertically);
			}
		}
		cuboidd.Translate(0.0, num2, 0.0);
		entity.CollidedVertically = collidedVertically;
		bool flag = false;
		cuboidd.Translate(num, 0.0, num3);
		foreach (Cuboidd collisionBox in CollisionBoxList)
		{
			if (collisionBox.Intersects(cuboidd))
			{
				flag = true;
				break;
			}
		}
		cuboidd.Translate(0.0 - num, 0.0, 0.0 - num3);
		collidedVertically = false;
		if (flag)
		{
			for (int j = 0; j < cuboids.Length && j < count; j++)
			{
				num = cuboids[j].pushOutX(cuboidd, num, ref direction);
				if (direction != EnumPushDirection.None)
				{
					collidedVertically = true;
					collBlockPos.Set(CollisionBoxList.positions[j]);
					CollisionBoxList.blocks[j].OnEntityCollide(world, entity, collBlockPos, (direction == EnumPushDirection.Negative) ? BlockFacing.EAST : BlockFacing.WEST, tmpPosDelta.Set(num, num2, num3), !entity.CollidedHorizontally);
				}
			}
			cuboidd.Translate(num, 0.0, 0.0);
			for (int k = 0; k < cuboids.Length && k < count; k++)
			{
				num3 = cuboids[k].pushOutZ(cuboidd, num3, ref direction);
				if (direction != EnumPushDirection.None)
				{
					collidedVertically = true;
					collBlockPos.Set(CollisionBoxList.positions[k]);
					CollisionBoxList.blocks[k].OnEntityCollide(world, entity, collBlockPos, (direction == EnumPushDirection.Negative) ? BlockFacing.SOUTH : BlockFacing.NORTH, tmpPosDelta.Set(num, num2, num3), !entity.CollidedHorizontally);
				}
			}
		}
		entity.CollidedHorizontally = collidedVertically;
		if (num2 > 0.0 && entity.CollidedVertically)
		{
			num2 -= entity.LadderFixDelta;
		}
		num -= num5;
		num2 -= num6;
		num3 -= num7;
		newPosition.Set(vec3d.X + num, vec3d.Y + num2, vec3d.Z + num3);
	}

	protected virtual void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra, int dimension)
	{
		bool num = minPos.SetAndEquals((int)(entityBox.X1 + Math.Min(0.0, motionX)), (int)(entityBox.Y1 + Math.Min(0.0, motionY) - (double)yExtra), (int)(entityBox.Z1 + Math.Min(0.0, motionZ)));
		double num2 = Math.Max(entityBox.Y1 + (double)stepHeight, entityBox.Y2);
		bool flag = maxPos.SetAndEquals((int)(entityBox.X2 + Math.Max(0.0, motionX)), (int)(num2 + Math.Max(0.0, motionY)), (int)(entityBox.Z2 + Math.Max(0.0, motionZ)));
		if (num && flag)
		{
			return;
		}
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

	public bool IsColliding(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		return GetCollidingBlock(blockAccessor, entityBoxRel, pos, alsoCheckTouch) != null;
	}

	public Block GetCollidingBlock(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		Cuboidd cuboidd = tmpBox.SetAndTranslate(entityBoxRel, pos);
		int num = (int)cuboidd.X1;
		int num2 = (int)cuboidd.Y1 - 1;
		int num3 = (int)cuboidd.Z1;
		int num4 = (int)cuboidd.X2;
		int num5 = (int)cuboidd.Y2;
		int num6 = (int)cuboidd.Z2;
		cuboidd.Y1 = Math.Round(cuboidd.Y1, 5);
		BlockPos blockPos = this.blockPos;
		Vec3d vec3d = blockPosVec;
		for (int i = num2; i <= num5; i++)
		{
			blockPos.SetAndCorrectDimension(num, i, num3);
			vec3d.Set(num, i, num3);
			for (int j = num; j <= num4; j++)
			{
				blockPos.X = j;
				vec3d.X = j;
				for (int k = num3; k <= num6; k++)
				{
					blockPos.Z = k;
					Block block = blockAccessor.GetBlock(blockPos, 4);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null || collisionBoxes.Length == 0)
					{
						continue;
					}
					vec3d.Z = k;
					foreach (Cuboidf cuboidf in collisionBoxes)
					{
						if (cuboidf != null && (alsoCheckTouch ? cuboidd.IntersectsOrTouches(cuboidf, vec3d) : cuboidd.Intersects(cuboidf, vec3d)))
						{
							return block;
						}
					}
				}
			}
		}
		return null;
	}

	public Cuboidd GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		BlockPos blockPos = new BlockPos();
		Vec3d vec3d = new Vec3d();
		Cuboidd cuboidd = entityBoxRel.ToDouble().Translate(pos);
		cuboidd.Y1 = Math.Round(cuboidd.Y1, 5);
		int num = (int)((double)entityBoxRel.X1 + pos.X);
		int num2 = (int)((double)entityBoxRel.Y1 + pos.Y - 1.0);
		int num3 = (int)((double)entityBoxRel.Z1 + pos.Z);
		int num4 = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int num5 = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int num6 = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int i = num2; i <= num5; i++)
		{
			blockPos.Set(num, i, num3);
			vec3d.Set(num, i, num3);
			for (int j = num; j <= num4; j++)
			{
				blockPos.X = j;
				vec3d.X = j;
				for (int k = num3; k <= num6; k++)
				{
					blockPos.Z = k;
					Cuboidf[] collisionBoxes = blockAccessor.GetMostSolidBlock(j, i, k).GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					vec3d.Z = k;
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
		return null;
	}

	public bool GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, ref Cuboidd intoCuboid, bool alsoCheckTouch = true, int dimension = 0)
	{
		BlockPos blockPos = new BlockPos(dimension);
		Vec3d vec3d = new Vec3d();
		Cuboidd cuboidd = entityBoxRel.ToDouble().Translate(pos);
		cuboidd.Y1 = Math.Round(cuboidd.Y1, 5);
		int num = (int)((double)entityBoxRel.X1 + pos.X);
		int num2 = (int)((double)entityBoxRel.Y1 + pos.Y - 1.0);
		int num3 = (int)((double)entityBoxRel.Z1 + pos.Z);
		int num4 = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int num5 = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int num6 = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int i = num2; i <= num5; i++)
		{
			for (int j = num; j <= num4; j++)
			{
				blockPos.Set(j, i, num3);
				vec3d.Set(j, i, num3);
				for (int k = num3; k <= num6; k++)
				{
					blockPos.Z = k;
					Cuboidf[] collisionBoxes = blockAccessor.GetBlock(blockPos, 4).GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					vec3d.Z = k;
					foreach (Cuboidf cuboidf in collisionBoxes)
					{
						if (cuboidf != null && (alsoCheckTouch ? cuboidd.IntersectsOrTouches(cuboidf, vec3d) : cuboidd.Intersects(cuboidf, vec3d)))
						{
							intoCuboid.Set(cuboidf).Translate(blockPos);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static bool AabbIntersect(Cuboidf aabb, double x, double y, double z, Cuboidf aabb2, Vec3d pos)
	{
		if (aabb2 == null)
		{
			return true;
		}
		if (x + (double)aabb.X1 < (double)aabb2.X2 + pos.X && x + (double)aabb.X2 > (double)aabb2.X1 + pos.X && z + (double)aabb.Z1 < (double)aabb2.Z2 + pos.Z && z + (double)aabb.Z2 > (double)aabb2.Z1 + pos.Z && y + (double)aabb.Y1 < (double)aabb2.Y2 + pos.Y)
		{
			return y + (double)aabb.Y2 > (double)aabb2.Y1 + pos.Y;
		}
		return false;
	}

	public static EnumIntersect AabbIntersect(Cuboidd aabb, Cuboidd aabb2, Vec3d motion)
	{
		if (aabb.Intersects(aabb2))
		{
			return EnumIntersect.Stuck;
		}
		if (aabb.X1 < aabb2.X2 + motion.X && aabb.X2 > aabb2.X1 + motion.X && aabb.Z1 < aabb2.Z2 && aabb.Z2 > aabb2.Z1 && aabb.Y1 < aabb2.Y2 && aabb.Y2 > aabb2.Y1)
		{
			return EnumIntersect.IntersectX;
		}
		if (aabb.X1 < aabb2.X2 && aabb.X2 > aabb2.X1 && aabb.Z1 < aabb2.Z2 && aabb.Z2 > aabb2.Z1 && aabb.Y1 < aabb2.Y2 + motion.Y && aabb.Y2 > aabb2.Y1 + motion.Y)
		{
			return EnumIntersect.IntersectY;
		}
		if (aabb.X1 < aabb2.X2 && aabb.X2 > aabb2.X1 && aabb.Z1 < aabb2.Z2 + motion.Z && aabb.Z2 > aabb2.Z1 + motion.Z && aabb.Y1 < aabb2.Y2 && aabb.Y2 > aabb2.Y1)
		{
			return EnumIntersect.IntersectZ;
		}
		return EnumIntersect.NoIntersect;
	}
}
