using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class Camera
{
	public float ZNear = 0.1f;

	public float ZFar = 3000f;

	public float Fov;

	public Vec3d CameraEyePos = new Vec3d();

	public double[] CameraMatrix;

	public double[] CameraMatrixOrigin;

	public float[] CameraMatrixOriginf = Mat4f.Create();

	public float Tppcameradistance;

	public int TppCameraDistanceMin;

	public int TppCameraDistanceMax;

	internal EnumCameraMode CameraMode;

	private double[] upVec3;

	private Vec3d camEyePosIn = new Vec3d();

	private Vec3d originPos = new Vec3d();

	public double PlayerHeight;

	public Vec3d forwardVec = new Vec3d();

	private Vec3d camTargetTmp = new Vec3d();

	private Vec3d camEyePosOutTmp = new Vec3d();

	public ModelTransform CameraOffset = new ModelTransform();

	private bool cameraStuck;

	private Vec3d to = new Vec3d();

	private Vec3d eyePosAbs = new Vec3d();

	public CachedCuboidList CollisionBoxList = new CachedCuboidList();

	public float MotionCap = 2f;

	private BlockPos minPos = new BlockPos();

	private BlockPos maxPos = new BlockPos();

	private Cuboidd cameraCollBox = new Cuboidd();

	private Cuboidd blockCollBox = new Cuboidd();

	private BlockPos tmpPos = new BlockPos();

	public Vec3d CamSourcePosition
	{
		get
		{
			return camEyePosIn;
		}
		set
		{
			camEyePosIn = value;
		}
	}

	public Vec3d OriginPosition
	{
		get
		{
			return originPos;
		}
		set
		{
			originPos = value;
		}
	}

	public double Yaw { get; set; }

	public double Pitch { get; set; }

	public double Roll { get; set; }

	public Camera()
	{
		CameraMode = EnumCameraMode.FirstPerson;
		CameraOffset.EnsureDefaultValues();
		Tppcameradistance = 3f;
		TppCameraDistanceMin = 1;
		TppCameraDistanceMax = 10;
		CameraMatrix = Mat4d.Create();
		upVec3 = Vec3Utilsd.FromValues(0.0, 1.0, 0.0);
	}

	internal virtual void SetMode(EnumCameraMode type)
	{
		CameraMode = type;
	}

	public void Update(float deltaTime, AABBIntersectionTest intersectionTester)
	{
		CameraMatrix = GetCameraMatrix(camEyePosIn, camEyePosIn, Yaw, Pitch, intersectionTester);
		CameraEyePos.Set(camEyePosOutTmp);
		CameraMatrixOrigin = GetCameraMatrix(originPos, camEyePosIn, Yaw, Pitch, intersectionTester);
		Mat4d.Rotate(CameraMatrixOrigin, CameraMatrixOrigin, Roll, new double[3] { 1.0, 0.0, 0.0 });
		for (int i = 0; i < 16; i++)
		{
			CameraMatrixOriginf[i] = (float)CameraMatrixOrigin[i];
		}
	}

	public double[] GetCameraMatrix(Vec3d camEyePosIn, Vec3d worldPos, double yaw, double pitch, AABBIntersectionTest intersectionTester)
	{
		VectorTool.ToVectorInFixedSystem(CameraOffset.Translation.X, CameraOffset.Translation.Y, CameraOffset.Translation.Z + 1f, (double)CameraOffset.Rotation.X + pitch, (double)CameraOffset.Rotation.Y - yaw + 3.1415927410125732, forwardVec);
		IClientWorldAccessor clientWorldAccessor = intersectionTester.bsTester as IClientWorldAccessor;
		EntityPlayer entity = clientWorldAccessor.Player.Entity;
		(clientWorldAccessor.Player as ClientPlayer).OverrideCameraMode = null;
		EnumCameraMode cameraMode = CameraMode;
		if ((uint)(cameraMode - 1) <= 1u)
		{
			float num = ((CameraMode == EnumCameraMode.FirstPerson) ? 0f : Tppcameradistance);
			camTargetTmp.X = worldPos.X + entity.LocalEyePos.X;
			camTargetTmp.Y = worldPos.Y + entity.LocalEyePos.Y;
			camTargetTmp.Z = worldPos.Z + entity.LocalEyePos.Z;
			camEyePosOutTmp.X = camTargetTmp.X + forwardVec.X * (double)(0f - num);
			camEyePosOutTmp.Y = camTargetTmp.Y + forwardVec.Y * (double)(0f - num);
			camEyePosOutTmp.Z = camTargetTmp.Z + forwardVec.Z * (double)(0f - num);
			FloatRef floatRef = FloatRef.Create(num);
			if (num > 0f && !LimitThirdPersonCameraToWalls(intersectionTester, yaw, camEyePosOutTmp, camTargetTmp, floatRef))
			{
				(clientWorldAccessor.Player as ClientPlayer).OverrideCameraMode = EnumCameraMode.FirstPerson;
				return lookatFp(entity, camEyePosIn);
			}
			if ((double)floatRef.value > 0.5)
			{
				camTargetTmp.X = camEyePosIn.X + entity.LocalEyePos.X;
				camTargetTmp.Y = camEyePosIn.Y + entity.LocalEyePos.Y;
				camTargetTmp.Z = camEyePosIn.Z + entity.LocalEyePos.Z;
				camEyePosOutTmp.X = camTargetTmp.X + forwardVec.X * (double)(0f - floatRef.value);
				camEyePosOutTmp.Y = camTargetTmp.Y + forwardVec.Y * (double)(0f - floatRef.value);
				camEyePosOutTmp.Z = camTargetTmp.Z + forwardVec.Z * (double)(0f - floatRef.value);
				return lookAt(camEyePosOutTmp, camTargetTmp);
			}
			camEyePosOutTmp.X = camEyePosIn.X + entity.LocalEyePos.X + forwardVec.X * 0.2;
			camEyePosOutTmp.Y = camEyePosIn.Y + entity.LocalEyePos.Y + forwardVec.Y * 0.2;
			camEyePosOutTmp.Z = camEyePosIn.Z + entity.LocalEyePos.Z + forwardVec.Z * 0.2;
			camTargetTmp.X = camEyePosOutTmp.X + forwardVec.X;
			camTargetTmp.Y = camEyePosOutTmp.Y + forwardVec.Y;
			camTargetTmp.Z = camEyePosOutTmp.Z + forwardVec.Z;
			return lookAt(camEyePosOutTmp, camTargetTmp);
		}
		_ = clientWorldAccessor.Api;
		camTargetTmp.X = camEyePosIn.X + entity.LocalEyePos.X;
		camTargetTmp.Y = camEyePosIn.Y + entity.LocalEyePos.Y;
		camTargetTmp.Z = camEyePosIn.Z + entity.LocalEyePos.Z;
		if (camEyePosIn == OriginPosition || !clientWorldAccessor.Player.ImmersiveFpMode)
		{
			return lookatFp(entity, camEyePosIn);
		}
		float num2 = 0.5f;
		RenderAPIGame renderapi = (clientWorldAccessor as ClientMain).api.renderapi;
		if (clientWorldAccessor.Player.WorldData.NoClip || cameraStuck)
		{
			eyePosAbs.Set(camTargetTmp);
			renderapi.CameraStuck = clientWorldAccessor.CollisionTester.IsColliding(clientWorldAccessor.BlockAccessor, new Cuboidf(num2), eyePosAbs, alsoCheckTouch: false);
			return lookatFp(entity, camEyePosIn);
		}
		if (camTargetTmp.DistanceTo(eyePosAbs) > 1f)
		{
			eyePosAbs.Set(camTargetTmp);
		}
		else
		{
			Vec3d vec3d = camTargetTmp - eyePosAbs;
			EnumCollideFlags enumCollideFlags = UpdateCameraMotion(clientWorldAccessor, eyePosAbs, vec3d.Mul(1.01), num2);
			eyePosAbs.Add(vec3d.Mul(0.99));
			entity.LocalEyePos.Set(eyePosAbs.X - camEyePosIn.X, eyePosAbs.Y - camEyePosIn.Y, eyePosAbs.Z - camEyePosIn.Z);
			renderapi.CameraStuck = enumCollideFlags != (EnumCollideFlags)0;
			if (enumCollideFlags != 0)
			{
				if ((double)clientWorldAccessor.Player.CameraPitch > 3.769911289215088)
				{
					entity.LocalEyePos.Y += ((double)clientWorldAccessor.Player.CameraPitch - 3.769911289215088) / 8.0;
				}
				cameraStuck = clientWorldAccessor.CollisionTester.IsColliding(clientWorldAccessor.BlockAccessor, new Cuboidf(num2 * 0.99f), eyePosAbs, alsoCheckTouch: false);
			}
		}
		camEyePosOutTmp.X = eyePosAbs.X;
		camEyePosOutTmp.Y = eyePosAbs.Y;
		camEyePosOutTmp.Z = eyePosAbs.Z;
		to.Set(camEyePosOutTmp.X + forwardVec.X, camEyePosOutTmp.Y + forwardVec.Y, camEyePosOutTmp.Z + forwardVec.Z);
		return lookAt(camTargetTmp, to);
	}

	private double[] lookatFp(EntityPlayer plr, Vec3d camEyePosIn)
	{
		camEyePosOutTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
		camEyePosOutTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
		camEyePosOutTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
		camTargetTmp.X = camEyePosOutTmp.X + forwardVec.X;
		camTargetTmp.Y = camEyePosOutTmp.Y + forwardVec.Y;
		camTargetTmp.Z = camEyePosOutTmp.Z + forwardVec.Z;
		return lookAt(camEyePosOutTmp, camTargetTmp);
	}

	private double[] lookAt(Vec3d from, Vec3d to)
	{
		double[] array = new double[16];
		Mat4d.LookAt(array, from.ToDoubleArray(), to.ToDoubleArray(), upVec3);
		return array;
	}

	public bool LimitThirdPersonCameraToWalls(AABBIntersectionTest intersectionTester, double yaw, Vec3d eye, Vec3d target, FloatRef curtppcameradistance)
	{
		float intersectionDistance = GetIntersectionDistance(intersectionTester, eye, target);
		float intersectionDistance2 = GetIntersectionDistance(intersectionTester, eye.AheadCopy(0.15000000596046448, 0.0, yaw), target.AheadCopy(0.15000000596046448, 0.0, yaw));
		float intersectionDistance3 = GetIntersectionDistance(intersectionTester, eye.AheadCopy(-0.15000000596046448, 0.0, yaw), target.AheadCopy(-0.15000000596046448, 0.0, yaw));
		float num = GameMath.Min(intersectionDistance, intersectionDistance2, intersectionDistance3);
		if ((double)num < 0.35)
		{
			return false;
		}
		curtppcameradistance.value = Math.Min(curtppcameradistance.value, num);
		double num2 = eye.X - target.X;
		double num3 = eye.Y - target.Y;
		double num4 = eye.Z - target.Z;
		float num5 = (float)Math.Sqrt(num2 * num2 + num3 * num3 + num4 * num4);
		num2 /= (double)num5;
		num3 /= (double)num5;
		num4 /= (double)num5;
		num2 *= (double)(Tppcameradistance + 1f);
		num3 *= (double)(Tppcameradistance + 1f);
		num4 *= (double)(Tppcameradistance + 1f);
		float num6 = (float)Math.Sqrt(num2 * num2 + num3 * num3 + num4 * num4);
		num2 /= (double)num6;
		num3 /= (double)num6;
		num4 /= (double)num6;
		eye.X = target.X + num2 * (double)curtppcameradistance.value;
		eye.Y = target.Y + num3 * (double)curtppcameradistance.value;
		eye.Z = target.Z + num4 * (double)curtppcameradistance.value;
		return true;
	}

	private float GetIntersectionDistance(AABBIntersectionTest intersectionTester, Vec3d eye, Vec3d target)
	{
		Line3D line3D = new Line3D();
		double num = eye.X - target.X;
		double num2 = eye.Y - target.Y;
		double num3 = eye.Z - target.Z;
		float num4 = (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
		num /= (double)num4;
		num2 /= (double)num4;
		num3 /= (double)num4;
		num *= (double)(Tppcameradistance + 1f);
		num2 *= (double)(Tppcameradistance + 1f);
		num3 *= (double)(Tppcameradistance + 1f);
		line3D.Start = target.ToDoubleArray();
		line3D.End = new double[3];
		line3D.End[0] = target.X + num;
		line3D.End[1] = target.Y + num2;
		line3D.End[2] = target.Z + num3;
		intersectionTester.LoadRayAndPos(line3D);
		BlockSelection selectedBlock = intersectionTester.GetSelectedBlock(TppCameraDistanceMax, (BlockPos pos, Block block) => block.CollisionBoxes != null && block.CollisionBoxes.Length != 0 && block.RenderPass != EnumChunkRenderPass.Transparent && block.RenderPass != EnumChunkRenderPass.Meta);
		if (selectedBlock != null)
		{
			float x = (float)((double)selectedBlock.Position.X + selectedBlock.HitPosition.X - target.X);
			float y = (float)((double)selectedBlock.Position.InternalY + selectedBlock.HitPosition.Y - target.Y);
			float z = (float)((double)selectedBlock.Position.Z + selectedBlock.HitPosition.Z - target.Z);
			float num5 = Length(x, y, z);
			return GameMath.Max(0.3f, num5 - 1f);
		}
		return 999f;
	}

	public float Length(float x, float y, float z)
	{
		return GameMath.Sqrt(x * x + y * y + z * z);
	}

	public EnumCollideFlags UpdateCameraMotion(IWorldAccessor world, Vec3d pos, Vec3d motion, float size)
	{
		cameraCollBox.Set(pos.X - (double)(size / 2f), pos.Y - (double)(size / 2f), pos.Z - (double)(size / 2f), pos.X + (double)(size / 2f), pos.Y + (double)(size / 2f), pos.Z + (double)(size / 2f));
		motion.X = GameMath.Clamp(motion.X, 0f - MotionCap, MotionCap);
		motion.Y = GameMath.Clamp(motion.Y, 0f - MotionCap, MotionCap);
		motion.Z = GameMath.Clamp(motion.Z, 0f - MotionCap, MotionCap);
		EnumCollideFlags enumCollideFlags = (EnumCollideFlags)0;
		minPos.SetAndCorrectDimension((int)(cameraCollBox.X1 + Math.Min(0.0, motion.X)), (int)(cameraCollBox.Y1 + Math.Min(0.0, motion.Y) - 1.0), (int)(cameraCollBox.Z1 + Math.Min(0.0, motion.Z)));
		maxPos.SetAndCorrectDimension((int)(cameraCollBox.X2 + Math.Max(0.0, motion.X)), (int)(cameraCollBox.Y2 + Math.Max(0.0, motion.Y)), (int)(cameraCollBox.Z2 + Math.Max(0.0, motion.Z)));
		tmpPos.dimension = minPos.dimension;
		cameraCollBox.Y1 %= 32768.0;
		cameraCollBox.Y2 %= 32768.0;
		CollisionBoxList.Clear();
		world.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block cblock, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = cblock.GetCollisionBoxes(world.BlockAccessor, tmpPos.Set(x, y, z));
			if (collisionBoxes != null)
			{
				CollisionBoxList.Add(collisionBoxes, x, y, z, cblock);
			}
		});
		EnumPushDirection direction = EnumPushDirection.None;
		for (int num = 0; num < CollisionBoxList.Count; num++)
		{
			blockCollBox = CollisionBoxList.cuboids[num];
			motion.Y = (float)blockCollBox.pushOutY(cameraCollBox, motion.Y, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideY;
			}
		}
		cameraCollBox.Translate(0.0, motion.Y, 0.0);
		for (int num2 = 0; num2 < CollisionBoxList.Count; num2++)
		{
			blockCollBox = CollisionBoxList.cuboids[num2];
			motion.X = (float)blockCollBox.pushOutX(cameraCollBox, motion.X, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideX;
			}
		}
		cameraCollBox.Translate(motion.X, 0.0, 0.0);
		for (int num3 = 0; num3 < CollisionBoxList.Count; num3++)
		{
			blockCollBox = CollisionBoxList.cuboids[num3];
			motion.Z = (float)blockCollBox.pushOutZ(cameraCollBox, motion.Z, ref direction);
			if (direction != EnumPushDirection.None)
			{
				enumCollideFlags |= EnumCollideFlags.CollideZ;
			}
		}
		return enumCollideFlags;
	}
}
