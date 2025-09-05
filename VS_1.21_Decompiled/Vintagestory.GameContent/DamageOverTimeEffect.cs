using System;
using System.IO;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class DamageOverTimeEffect
{
	public EnumDamageSource DamageSource;

	public EnumDamageType DamageType;

	public int DamageTier;

	public float Damage;

	public int EffectType;

	public TimeSpan TickDuration;

	public TimeSpan PreviousTickTime;

	public int TicksLeft;

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write((byte)DamageSource);
		writer.Write((byte)DamageType);
		writer.Write(DamageTier);
		writer.Write(Damage);
		writer.Write(TickDuration.Ticks);
		writer.Write(PreviousTickTime.Ticks);
		writer.Write(TicksLeft);
		writer.Write(EffectType);
	}

	public static DamageOverTimeEffect FromBytes(BinaryReader reader)
	{
		return new DamageOverTimeEffect
		{
			DamageSource = (EnumDamageSource)reader.ReadByte(),
			DamageType = (EnumDamageType)reader.ReadByte(),
			DamageTier = reader.ReadInt32(),
			Damage = reader.ReadSingle(),
			TickDuration = TimeSpan.FromTicks(reader.ReadInt64()),
			PreviousTickTime = TimeSpan.FromTicks(reader.ReadInt64()),
			TicksLeft = reader.ReadInt32(),
			EffectType = reader.ReadInt32()
		};
	}
}
