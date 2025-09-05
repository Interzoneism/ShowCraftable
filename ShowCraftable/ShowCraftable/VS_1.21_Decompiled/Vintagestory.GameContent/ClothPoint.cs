using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ClothPoint
{
	public static bool PushingPhysics;

	[ProtoMember(1)]
	public int PointIndex;

	[ProtoMember(2)]
	public float Mass;

	[ProtoMember(3)]
	public float InvMass;

	[ProtoMember(4)]
	public Vec3d Pos;

	[ProtoMember(5)]
	public Vec3f Velocity = new Vec3f();

	[ProtoMember(6)]
	public Vec3f Tension = new Vec3f();

	[ProtoMember(7)]
	private float GravityStrength = 1f;

	[ProtoMember(8)]
	private bool pinned;

	[ProtoMember(9)]
	public long pinnedToEntityId;

	[ProtoMember(10)]
	private BlockPos pinnedToBlockPos;

	[ProtoMember(11)]
	public Vec3f pinnedToOffset;

	[ProtoMember(12)]
	private float pinnedToOffsetStartYaw;

	[ProtoMember(13)]
	private string pinnedToPlayerUid;

	public EnumCollideFlags CollideFlags;

	public float YCollideRestMul;

	private Vec4f tmpvec = new Vec4f();

	private ClothSystem cs;

	private Entity pinnedTo;

	private Matrixf pinOffsetTransform;

	public Vec3d TensionDirection = new Vec3d();

	public double extension;

	private float dampFactor = 0.9f;

	private float accum1s;

	public bool Dirty { get; internal set; }

	public Entity PinnedToEntity => pinnedTo;

	public BlockPos PinnedToBlockPos => pinnedToBlockPos;

	public bool Pinned => pinned;

	public ClothPoint(ClothSystem cs)
	{
		this.cs = cs;
		Pos = new Vec3d();
		init();
	}

	protected ClothPoint()
	{
	}

	public ClothPoint(ClothSystem cs, int pointIndex, double x, double y, double z)
	{
		this.cs = cs;
		PointIndex = pointIndex;
		Pos = new Vec3d(x, y, z);
		init();
	}

	public void setMass(float mass)
	{
		Mass = mass;
		InvMass = 1f / mass;
	}

	private void init()
	{
		setMass(1f);
	}

	public void PinTo(Entity toEntity, Vec3f pinOffset)
	{
		pinned = true;
		pinnedTo = toEntity;
		pinnedToEntityId = toEntity.EntityId;
		pinnedToOffset = pinOffset;
		pinnedToOffsetStartYaw = toEntity.SidedPos.Yaw;
		pinOffsetTransform = Matrixf.Create();
		pinnedToBlockPos = null;
		if (toEntity is EntityPlayer entityPlayer)
		{
			pinnedToPlayerUid = entityPlayer.PlayerUID;
		}
		MarkDirty();
	}

	public void PinTo(BlockPos blockPos, Vec3f offset)
	{
		pinnedToBlockPos = blockPos;
		pinnedToOffset = offset;
		pinnedToPlayerUid = null;
		pinned = true;
		Pos.Set(pinnedToBlockPos).Add(pinnedToOffset);
		pinnedTo = null;
		pinnedToEntityId = 0L;
		MarkDirty();
	}

	public void UnPin()
	{
		pinned = false;
		pinnedTo = null;
		pinnedToPlayerUid = null;
		pinnedToEntityId = 0L;
		MarkDirty();
	}

	public void MarkDirty()
	{
		Dirty = true;
	}

	public void update(float dt, IWorldAccessor world)
	{
		if (pinnedTo == null && pinnedToPlayerUid != null)
		{
			EntityPlayer entityPlayer = world.PlayerByUid(pinnedToPlayerUid)?.Entity;
			if (entityPlayer?.World != null)
			{
				PinTo(entityPlayer, pinnedToOffset);
			}
		}
		if (pinned)
		{
			if (pinnedTo != null)
			{
				Entity entity = pinnedTo;
				entity = (entity as EntityAgent)?.MountedOn?.Entity ?? entity;
				if (entity.ShouldDespawn)
				{
					EntityDespawnData despawnReason = entity.DespawnReason;
					if (despawnReason == null || despawnReason.Reason != EnumDespawnReason.Unload)
					{
						UnPin();
						return;
					}
				}
				float weight = entity.Properties.Weight;
				float num = GameMath.Clamp(50f / weight, 0.1f, 2f);
				EntityAgent obj = entity as EntityAgent;
				int num2;
				if (obj == null || !obj.Controls.Sneak)
				{
					if (entity is EntityPlayer)
					{
						IAnimationManager animManager = entity.AnimManager;
						num2 = (((animManager != null && animManager.IsAnimationActive("sit")) || (entity.AnimManager?.IsAnimationActive("sleep") ?? false)) ? 1 : 0);
					}
					else
					{
						num2 = 0;
					}
				}
				else
				{
					num2 = 1;
				}
				bool flag = (byte)num2 != 0;
				float num3 = weight / 10f * (float)((!flag) ? 1 : 200);
				EntityPlayer entityPlayer2 = pinnedTo as EntityPlayer;
				EntityAgent entityAgent = pinnedTo as EntityAgent;
				AttachmentPointAndPose attachmentPointAndPose = entityPlayer2?.AnimManager?.Animator?.GetAttachmentPointPose("RightHand");
				if (attachmentPointAndPose == null)
				{
					attachmentPointAndPose = pinnedTo?.AnimManager?.Animator?.GetAttachmentPointPose("rope");
				}
				Vec4f vec4f;
				if (attachmentPointAndPose != null)
				{
					Matrixf matrixf = new Matrixf();
					if (entityPlayer2 != null)
					{
						matrixf.RotateY(entityAgent.BodyYaw + (float)Math.PI / 2f);
					}
					else
					{
						matrixf.RotateY(pinnedTo.SidedPos.Yaw + (float)Math.PI / 2f);
					}
					matrixf.Translate(-0.5, 0.0, -0.5);
					attachmentPointAndPose.MulUncentered(matrixf);
					vec4f = matrixf.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
				}
				else
				{
					pinOffsetTransform.Identity();
					pinOffsetTransform.RotateY(pinnedTo.SidedPos.Yaw - pinnedToOffsetStartYaw);
					tmpvec.Set(pinnedToOffset.X, pinnedToOffset.Y, pinnedToOffset.Z, 1f);
					vec4f = pinOffsetTransform.TransformVector(tmpvec);
				}
				EntityPos sidedPos = pinnedTo.SidedPos;
				Pos.Set(sidedPos.X + (double)vec4f.X, sidedPos.Y + (double)vec4f.Y, sidedPos.Z + (double)vec4f.Z);
				if (true && extension > 0.0)
				{
					float num4 = num * dt * 0.006f;
					Vec3d vec3d = TensionDirection.Clone();
					vec3d.Normalize();
					double num5 = num3 * 1.65f;
					double num6 = num3;
					Vec3d vec3d2 = new Vec3d(vec3d.X, 0.0, vec3d.Z);
					double num7 = vec3d2.Length();
					if (num7 > 0.001)
					{
						vec3d2 /= num7;
					}
					else
					{
						vec3d2.Set(0.0, 0.0, 0.0);
					}
					_ = vec3d.Y;
					double num8 = Math.Sqrt(num5 * num5 + num6 * num6);
					double num9 = num3 * 1.65f;
					double num10 = num5 - num9;
					double num11 = num9 + num10;
					double num12 = Math.Sqrt(num11 * num11 + num6 * num6);
					double num13 = 0.0 - num6;
					bool onGround = entity.OnGround;
					bool flag2 = extension > 0.05;
					if (flag2 && onGround)
					{
						Vec3d a = new Vec3d(GameMath.Clamp(Math.Abs(TensionDirection.X) + num12 - num8, 0.0, 400.0) * (double)Math.Sign(TensionDirection.X), GameMath.Clamp(Math.Abs(TensionDirection.Y) + num12 - num8 + num13, 0.0, 400.0) * (double)Math.Sign(TensionDirection.Y), GameMath.Clamp(Math.Abs(TensionDirection.Z) + num12 - num8, 0.0, 400.0) * (double)Math.Sign(TensionDirection.Z)) * num4;
						entity.SidedPos.Motion.Add(a);
					}
					else if (flag2)
					{
						Vec3d a2 = new Vec3d(GameMath.Clamp(Math.Abs(TensionDirection.X * 0.1) + (num12 - num8) * 0.5, 0.0, 400.0) * (double)Math.Sign(TensionDirection.X), GameMath.Clamp(Math.Abs(TensionDirection.Y * 0.3) + num12 - num8 + num13, 0.0, 400.0) * (double)Math.Sign(TensionDirection.Y), GameMath.Clamp(Math.Abs(TensionDirection.Z * 0.1) + (num12 - num8) * 0.5, 0.0, 400.0) * (double)Math.Sign(TensionDirection.Z)) * num4;
						entity.SidedPos.Motion.Add(a2);
					}
				}
				Velocity.Set(0f, 0f, 0f);
				return;
			}
			Velocity.Set(0f, 0f, 0f);
			if (!(pinnedToBlockPos != null))
			{
				return;
			}
			accum1s += dt;
			if (accum1s >= 1f)
			{
				accum1s = 0f;
				if (!cs.api.World.BlockAccessor.GetBlock(PinnedToBlockPos).HasBehavior<BlockBehaviorRopeTieable>())
				{
					UnPin();
				}
			}
		}
		else
		{
			Vec3f vec3f = Tension.Clone();
			vec3f.Y -= GravityStrength * 10f;
			Vec3f vec3f2 = vec3f * InvMass;
			if (CollideFlags == (EnumCollideFlags)0)
			{
				vec3f2.X += (float)cs.windSpeed.X * InvMass;
			}
			Vec3f vec3f3 = Velocity + vec3f2 * dt;
			vec3f3 *= dampFactor;
			float num14 = 0.1f;
			cs.pp.HandleBoyancy(Pos, vec3f3, cs.boyant, GravityStrength, dt, num14);
			CollideFlags = cs.pp.UpdateMotion(Pos, vec3f3, num14);
			dt *= 0.99f;
			Pos.Add(vec3f3.X * dt, vec3f3.Y * dt, vec3f3.Z * dt);
			Velocity.Set(vec3f3);
			Tension.Set(0f, 0f, 0f);
		}
	}

	public void restoreReferences(ClothSystem cs, IWorldAccessor world)
	{
		this.cs = cs;
		if (pinnedToEntityId != 0L)
		{
			pinnedTo = world.GetEntityById(pinnedToEntityId);
			if (pinnedTo != null)
			{
				PinTo(pinnedTo, pinnedToOffset);
			}
		}
		if (pinnedToBlockPos != null)
		{
			PinTo(pinnedToBlockPos, pinnedToOffset);
		}
	}

	public void restoreReferences(Entity entity)
	{
		if (pinnedToEntityId == entity.EntityId)
		{
			PinTo(entity, pinnedToOffset);
		}
	}

	public void updateFromPoint(ClothPoint point, IWorldAccessor world)
	{
		PointIndex = point.PointIndex;
		Mass = point.Mass;
		InvMass = point.InvMass;
		Pos.Set(point.Pos);
		Velocity.Set(point.Velocity);
		Tension.Set(point.Tension);
		GravityStrength = point.GravityStrength;
		pinned = point.pinned;
		pinnedToPlayerUid = point.pinnedToPlayerUid;
		pinnedToOffsetStartYaw = point.pinnedToOffsetStartYaw;
		pinnedToEntityId = point.pinnedToEntityId;
		pinnedToBlockPos = pinnedToBlockPos.SetOrCreate(point.PinnedToBlockPos);
		pinnedToOffset = pinnedToOffset.SetOrCreate(point.pinnedToOffset);
		CollideFlags = point.CollideFlags;
		YCollideRestMul = point.YCollideRestMul;
		if (pinnedToEntityId != 0L)
		{
			pinnedTo = world.GetEntityById(pinnedToEntityId);
			if (pinnedTo != null)
			{
				PinTo(pinnedTo, pinnedToOffset);
			}
			else
			{
				UnPin();
			}
		}
		else if (pinnedToBlockPos != null)
		{
			PinTo(pinnedToBlockPos, pinnedToOffset);
		}
	}
}
