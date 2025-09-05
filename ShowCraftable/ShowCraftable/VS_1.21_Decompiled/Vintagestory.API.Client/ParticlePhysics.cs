using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ParticlePhysics
{
	public IBlockAccessor BlockAccess;

	public float PhysicsTickTime = 0.033f;

	public const float AsyncSpawnTime = 0.033f;

	private Cuboidd particleCollBox = new Cuboidd();

	private Cuboidd blockCollBox = new Cuboidd();

	private BlockPos tmpPos = new BlockPos();

	public CachedCuboidList CollisionBoxList = new CachedCuboidList();

	public float MotionCap = 2f;

	private BlockPos minPos = new BlockPos();

	private BlockPos maxPos = new BlockPos();

	public ParticlePhysics(IBlockAccessor blockAccess)
	{
		BlockAccess = blockAccess;
	}

	public Vec3f CollisionStrength(Vec3f velocitybefore, Vec3f velocitynow, float gravityStrength, float deltatime)
	{
		velocitybefore.Y -= gravityStrength * deltatime;
		return new Vec3f(velocitybefore.X - velocitynow.X, velocitybefore.Y - velocitynow.Y, velocitybefore.Z - velocitynow.Z);
	}

	public void HandleBoyancy(Vec3d pos, Vec3f velocity, bool boyant, float gravityStrength, float deltatime, float height)
	{
		int x = (int)pos.X;
		int y = (int)pos.Y;
		int z = (int)pos.Z;
		tmpPos.Set(x, y, z);
		Block block = BlockAccess.GetBlock(tmpPos, 2);
		Block block2 = block;
		if (boyant)
		{
			if (block.IsLiquid())
			{
				tmpPos.Set(x, (int)(pos.Y + 1.0), z);
				block = BlockAccess.GetBlock(tmpPos, 2);
				float num = GameMath.Clamp((float)(int)pos.Y + (float)block2.LiquidLevel / 8f + (block.IsLiquid() ? 1.125f : 0f) - (float)pos.Y + height, 0f, 1f);
				float num2 = GameMath.Clamp(9f * num, -1.25f, 1.25f);
				velocity.Y += gravityStrength * deltatime * num2;
				float num3 = GameMath.Clamp(30f * Math.Abs(velocity.Y) - 0.02f, 1f, 1.25f);
				velocity.Y /= num3;
				velocity.X *= 0.99f;
				velocity.Z *= 0.99f;
				if (block2.PushVector != null && num >= 0f)
				{
					float num4 = deltatime / 0.033f;
					velocity.Add((float)block2.PushVector.X * 15f * num4, (float)block2.PushVector.Y * 15f * num4, (float)block2.PushVector.Z * 15f * num4);
				}
			}
		}
		else if (block.PushVector != null)
		{
			velocity.Add((float)block.PushVector.X * 30f * deltatime, (float)block.PushVector.Y * 30f * deltatime, (float)block.PushVector.Z * 30f * deltatime);
		}
	}

	public EnumCollideFlags UpdateMotion(Vec3d pos, Vec3f motion, float size)
	{
		particleCollBox.Set(pos.X - (double)(size / 2f), pos.Y - 0.0, pos.Z - (double)(size / 2f), pos.X + (double)(size / 2f), pos.Y + (double)(size / 2f), pos.Z + (double)(size / 2f));
		motion.X = GameMath.Clamp(motion.X, 0f - MotionCap, MotionCap);
		motion.Y = GameMath.Clamp(motion.Y, 0f - MotionCap, MotionCap);
		motion.Z = GameMath.Clamp(motion.Z, 0f - MotionCap, MotionCap);
		EnumCollideFlags enumCollideFlags = (EnumCollideFlags)0;
		minPos.SetAndCorrectDimension((int)(particleCollBox.X1 + (double)Math.Min(0f, motion.X)), (int)(particleCollBox.Y1 + (double)Math.Min(0f, motion.Y) - 1.0), (int)(particleCollBox.Z1 + (double)Math.Min(0f, motion.Z)));
		maxPos.SetAndCorrectDimension((int)(particleCollBox.X2 + (double)Math.Max(0f, motion.X)), (int)(particleCollBox.Y2 + (double)Math.Max(0f, motion.Y)), (int)(particleCollBox.Z2 + (double)Math.Max(0f, motion.Z)));
		tmpPos.dimension = minPos.dimension;
		particleCollBox.Y1 %= 32768.0;
		particleCollBox.Y2 %= 32768.0;
		CollisionBoxList.Clear();
		BlockAccess.WalkBlocks(minPos, maxPos, delegate(Block cblock, int x, int y, int z)
		{
			Cuboidf[] particleCollisionBoxes = cblock.GetParticleCollisionBoxes(BlockAccess, tmpPos.Set(x, y, z));
			if (particleCollisionBoxes != null)
			{
				CollisionBoxList.Add(particleCollisionBoxes, x, y, z, cblock);
			}
		});
		EnumPushDirection direction = EnumPushDirection.None;
		for (int num = 0; num < CollisionBoxList.Count; num++)
		{
			blockCollBox = CollisionBoxList.cuboids[num];
			motion.Y = (float)blockCollBox.pushOutY(particleCollBox, motion.Y, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideY;
			}
		}
		particleCollBox.Translate(0.0, motion.Y, 0.0);
		for (int num2 = 0; num2 < CollisionBoxList.Count; num2++)
		{
			blockCollBox = CollisionBoxList.cuboids[num2];
			motion.X = (float)blockCollBox.pushOutX(particleCollBox, motion.X, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideX;
			}
		}
		particleCollBox.Translate(motion.X, 0.0, 0.0);
		for (int num3 = 0; num3 < CollisionBoxList.Count; num3++)
		{
			blockCollBox = CollisionBoxList.cuboids[num3];
			motion.Z = (float)blockCollBox.pushOutZ(particleCollBox, motion.Z, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideZ;
			}
		}
		return enumCollideFlags;
	}
}
