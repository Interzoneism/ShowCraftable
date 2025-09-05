using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

public class CachingCollisionTester : CollisionTester
{
	public void NewTick(EntityPos entityPos)
	{
		minPos.Set(int.MinValue, int.MinValue, int.MinValue);
		minPos.dimension = entityPos.Dimension;
		tmpPos.dimension = entityPos.Dimension;
	}

	public void AssignToEntity(PhysicsBehaviorBase entityPhysics, int dimension)
	{
		minPos.dimension = dimension;
		tmpPos.dimension = dimension;
	}

	protected override void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra, int dimension)
	{
		Cuboidd cuboidd = entityBox;
		bool num = minPos.SetAndEquals((int)(cuboidd.X1 + Math.Min(0.0, motionX)), (int)(cuboidd.Y1 + Math.Min(0.0, motionY) - (double)yExtra), (int)(cuboidd.Z1 + Math.Min(0.0, motionZ)));
		double num2 = Math.Max(cuboidd.Y1 + (double)stepHeight, cuboidd.Y2);
		bool flag = maxPos.SetAndEquals((int)(cuboidd.X2 + Math.Max(0.0, motionX)), (int)(num2 + Math.Max(0.0, motionY)), (int)(cuboidd.Z2 + Math.Max(0.0, motionZ)));
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

	public void PushOutFromBlocks(IBlockAccessor blockAccessor, Entity entity, Vec3d tmpVec, float clippingLimit)
	{
		if (!IsColliding(blockAccessor, entity.CollisionBox, tmpVec, alsoCheckTouch: false))
		{
			return;
		}
		Vec3d xYZ = entity.SidedPos.XYZ;
		entityBox.SetAndTranslate(entity.CollisionBox, xYZ.X, xYZ.Y, xYZ.Z);
		GenerateCollisionBoxList(blockAccessor, 0.0, 0.0, 0.0, 0.5f, 0f, entity.SidedPos.Dimension);
		int count = CollisionBoxList.Count;
		if (count == 0)
		{
			return;
		}
		Cuboidd[] cuboids = CollisionBoxList.cuboids;
		double num = 0.0;
		double num2 = 0.0;
		EnumPushDirection direction = EnumPushDirection.None;
		Cuboidd cuboidd = entity.CollisionBox.ToDouble();
		cuboidd.Translate(xYZ.X, xYZ.Y, xYZ.Z);
		cuboidd.GrowBy(0f - clippingLimit, 0.0, 0f - clippingLimit);
		for (int i = 0; i < cuboids.Length && i < count; i++)
		{
			num = cuboids[i].pushOutX(cuboidd, clippingLimit, ref direction);
		}
		if (num == (double)clippingLimit)
		{
			for (int j = 0; j < cuboids.Length && j < count; j++)
			{
				num = cuboids[j].pushOutX(cuboidd, 0f - clippingLimit, ref direction);
			}
			num += (double)clippingLimit;
		}
		else
		{
			num -= (double)clippingLimit;
		}
		for (int k = 0; k < cuboids.Length && k < count; k++)
		{
			num2 = cuboids[k].pushOutZ(cuboidd, clippingLimit, ref direction);
		}
		if (num2 == (double)clippingLimit)
		{
			for (int l = 0; l < cuboids.Length && l < count; l++)
			{
				num2 = cuboids[l].pushOutZ(cuboidd, 0f - clippingLimit, ref direction);
			}
			num2 += (double)clippingLimit;
		}
		else
		{
			num2 -= (double)clippingLimit;
		}
		entity.SidedPos.X = xYZ.X + num;
		entity.SidedPos.Z = xYZ.Z + num2;
	}
}
