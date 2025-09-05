using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class DamageSource
{
	public EnumDamageSource Source;

	public EnumDamageType Type;

	public Vec3d HitPosition;

	public Entity SourceEntity;

	public Entity CauseEntity;

	public Block SourceBlock;

	public Vec3d SourcePos;

	public int DamageTier;

	public float KnockbackStrength = 1f;

	public TimeSpan Duration = TimeSpan.Zero;

	public int TicksPerDuration = 1;

	public int DamageOverTimeType;

	public float YDirKnockbackDiv = 1f;

	public bool IgnoreInvFrames;

	public EnumDamageOverTimeEffectType DamageOverTimeTypeEnum
	{
		get
		{
			return (EnumDamageOverTimeEffectType)DamageOverTimeType;
		}
		set
		{
			DamageOverTimeType = (int)value;
		}
	}

	public Vec3d GetSourcePosition()
	{
		if (SourceEntity != null)
		{
			return SourceEntity.SidedPos.XYZ;
		}
		return SourcePos;
	}

	public Entity GetCauseEntity()
	{
		return CauseEntity ?? SourceEntity;
	}

	public bool GetAttackAngle(Vec3d attackedPos, out double attackYaw, out double attackPitch)
	{
		double num;
		double num2;
		double num3;
		if (HitPosition != null)
		{
			num = HitPosition.X;
			num2 = HitPosition.Y;
			num3 = HitPosition.Z;
		}
		else if (SourceEntity != null)
		{
			num = SourceEntity.Pos.X - attackedPos.X;
			num2 = SourceEntity.Pos.Y - attackedPos.Y;
			num3 = SourceEntity.Pos.Z - attackedPos.Z;
		}
		else
		{
			if (!(SourcePos != null))
			{
				attackYaw = 0.0;
				attackPitch = 0.0;
				return false;
			}
			num = SourcePos.X - attackedPos.X;
			num2 = SourcePos.Y - attackedPos.Y;
			num3 = SourcePos.Z - attackedPos.Z;
		}
		attackYaw = Math.Atan2(num, num3);
		double y = num2;
		float num4 = (float)Math.Sqrt(num * num + num3 * num3);
		attackPitch = (float)Math.Atan2(y, num4);
		return true;
	}
}
