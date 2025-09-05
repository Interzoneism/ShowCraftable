using System;
using System.IO;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class EntityControls
{
	public OnEntityAction OnAction = delegate
	{
	};

	private bool[] flags = new bool[15];

	public bool DetachedMode;

	public bool NoClip;

	public EnumFreeMovAxisLock FlyPlaneLock;

	public Vec3d WalkVector = new Vec3d();

	public Vec3d FlyVector = new Vec3d();

	public bool IsFlying;

	public bool IsClimbing;

	public bool IsAiming;

	public bool IsStepping;

	public EnumHandInteract HandUse;

	public BlockSelection HandUsingBlockSel;

	public int UsingCount;

	public long UsingBeginMS;

	public ModelTransform LeftUsingHeldItemTransformBefore;

	[Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
	public ModelTransform UsingHeldItemTransformBefore;

	[Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
	public ModelTransform UsingHeldItemTransformAfter;

	public float MovespeedMultiplier = 1f;

	public bool Dirty;

	public double GlideSpeed;

	public bool[] Flags => flags;

	public bool TriesToMove
	{
		get
		{
			if (!Forward && !Backward && !Left)
			{
				return Right;
			}
			return true;
		}
	}

	public virtual bool Forward
	{
		get
		{
			return flags[0];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Forward, value);
		}
	}

	public virtual bool Backward
	{
		get
		{
			return flags[1];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Backward, value);
		}
	}

	public virtual bool Left
	{
		get
		{
			return flags[2];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Left, value);
		}
	}

	public virtual bool Right
	{
		get
		{
			return flags[3];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Right, value);
		}
	}

	public virtual bool Jump
	{
		get
		{
			return flags[4];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Jump, value);
		}
	}

	public virtual bool Sneak
	{
		get
		{
			return flags[5];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Sneak, value);
		}
	}

	public virtual bool Gliding
	{
		get
		{
			return flags[7];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Glide, value);
		}
	}

	public virtual bool FloorSitting
	{
		get
		{
			return flags[8];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.FloorSit, value);
		}
	}

	public virtual bool Sprint
	{
		get
		{
			return flags[6];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Sprint, value);
		}
	}

	public virtual bool Up
	{
		get
		{
			return flags[11];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Up, value);
		}
	}

	public virtual bool Down
	{
		get
		{
			return flags[12];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Down, value);
		}
	}

	public virtual bool LeftMouseDown
	{
		get
		{
			return flags[9];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.LeftMouseDown, value);
		}
	}

	public virtual bool RightMouseDown
	{
		get
		{
			return flags[10];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.RightMouseDown, value);
		}
	}

	public virtual bool CtrlKey
	{
		get
		{
			return flags[13];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.CtrlKey, value);
		}
	}

	public virtual bool ShiftKey
	{
		get
		{
			return flags[14];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.ShiftKey, value);
		}
	}

	public virtual bool this[EnumEntityAction action]
	{
		get
		{
			return flags[(int)action];
		}
		set
		{
			flags[(int)action] = value;
		}
	}

	protected virtual void AttemptToggleAction(EnumEntityAction action, bool on)
	{
		if (flags[(int)action] != on)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			OnAction(action, on, ref handled);
			if (handled == EnumHandling.PassThrough)
			{
				flags[(int)action] = on;
				Dirty = true;
			}
		}
	}

	public virtual void CalcMovementVectors(EntityPos pos, float dt)
	{
		double num = dt * GlobalConstants.BaseMoveSpeed * MovespeedMultiplier * GlobalConstants.OverallSpeedMultiplier;
		double num2 = (Forward ? num : 0.0) + (Backward ? (0.0 - num) : 0.0);
		double num3 = (Right ? (0.0 - num) : 0.0) + (Left ? num : 0.0);
		double num4 = Math.Cos(pos.Pitch);
		double num5 = Math.Sin(pos.Pitch);
		double num6 = Math.Cos(0f - pos.Yaw);
		double num7 = Math.Sin(0f - pos.Yaw);
		WalkVector.Set(num3 * num6 - num2 * num7, 0.0, num3 * num7 + num2 * num6);
		if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
		{
			num4 = -1.0;
		}
		FlyVector.Set(num3 * num6 + num2 * num4 * num7, num2 * num5, num3 * num7 - num2 * num4 * num6);
		double val = (((Forward || Backward) && (Right || Left)) ? (1.0 / Math.Sqrt(2.0)) : 1.0);
		WalkVector.Mul(val);
		if (FlyPlaneLock == EnumFreeMovAxisLock.X)
		{
			FlyVector.X = 0.0;
		}
		if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
		{
			FlyVector.Y = 0.0;
		}
		if (FlyPlaneLock == EnumFreeMovAxisLock.Z)
		{
			FlyVector.Z = 0.0;
		}
	}

	public virtual void SetFrom(EntityControls controls)
	{
		for (int i = 0; i < controls.flags.Length; i++)
		{
			flags[i] = controls.flags[i];
		}
		DetachedMode = controls.DetachedMode;
		FlyPlaneLock = controls.FlyPlaneLock;
		IsFlying = controls.IsFlying;
		NoClip = controls.NoClip;
	}

	public virtual void UpdateFromPacket(bool pressed, int action)
	{
		if (flags[action] != pressed)
		{
			AttemptToggleAction((EnumEntityAction)action, pressed);
		}
	}

	public virtual void StopAllMovement()
	{
		for (int i = 0; i < flags.Length; i++)
		{
			flags[i] = false;
		}
	}

	public virtual int ToInt()
	{
		return (int)((Forward ? 1u : 0u) | (uint)(Backward ? 2 : 0) | (uint)(Left ? 4 : 0) | (uint)(Right ? 8 : 0) | (uint)(Jump ? 16 : 0) | (uint)(Sneak ? 32 : 0) | (uint)(Sprint ? 64 : 0) | (uint)(Up ? 128 : 0) | (uint)(Down ? 256 : 0) | (uint)(flags[7] ? 512 : 0) | (uint)(flags[8] ? 1024 : 0) | (uint)(flags[9] ? 2048 : 0) | (uint)(flags[10] ? 4096 : 0) | (uint)(IsClimbing ? 8192 : 0) | (uint)(flags[13] ? 16384 : 0)) | (flags[14] ? 32768 : 0);
	}

	public virtual void FromInt(int flagsInt)
	{
		Forward = (flagsInt & 1) > 0;
		Backward = (flagsInt & 2) > 0;
		Left = (flagsInt & 4) > 0;
		Right = (flagsInt & 8) > 0;
		Jump = (flagsInt & 0x10) > 0;
		Sneak = (flagsInt & 0x20) > 0;
		Sprint = (flagsInt & 0x40) > 0;
		Up = (flagsInt & 0x80) > 0;
		Down = (flagsInt & 0x100) > 0;
		flags[7] = (flagsInt & 0x200) > 0;
		flags[8] = (flagsInt & 0x400) > 0;
		flags[9] = (flagsInt & 0x800) > 0;
		flags[10] = (flagsInt & 0x1000) > 0;
		IsClimbing = (flagsInt & 0x2000) > 0;
		flags[13] = (flagsInt & 0x4000) > 0;
		flags[14] = (flagsInt & 0x8000) > 0;
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write(ToInt());
	}

	public virtual void FromBytes(BinaryReader reader, bool ignoreData)
	{
		int flagsInt = reader.ReadInt32();
		if (!ignoreData)
		{
			FromInt(flagsInt);
		}
	}
}
