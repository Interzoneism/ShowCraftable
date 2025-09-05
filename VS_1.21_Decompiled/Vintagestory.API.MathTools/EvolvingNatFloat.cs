using System;
using System.IO;
using Newtonsoft.Json;

namespace Vintagestory.API.MathTools;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class EvolvingNatFloat
{
	private static TransformFunction[] transfuncs;

	[JsonProperty]
	private EnumTransformFunction transform;

	[JsonProperty]
	private float factor;

	[JsonProperty]
	private float? maxvalue;

	public float Factor => factor;

	public float? MaxValue => maxvalue;

	public EnumTransformFunction Transform => transform;

	static EvolvingNatFloat()
	{
		transfuncs = new TransformFunction[20];
		transfuncs[0] = (float firstval, float factor, float seq) => firstval;
		transfuncs[1] = (float firstval, float factor, float seq) => firstval + factor * seq;
		transfuncs[6] = (float firstval, float factor, float seq) => firstval + 1f / (1f + factor * seq);
		transfuncs[2] = (float firstval, float factor, float seq) => (!(factor > 0f)) ? Math.Max(0f, firstval + factor * seq) : Math.Min(0f, firstval + factor * seq);
		transfuncs[3] = (float firstval, float factor, float seq) => firstval - firstval / Math.Abs(firstval) * factor * seq;
		transfuncs[4] = (float firstval, float factor, float seq) => firstval + firstval / Math.Abs(firstval) * factor * seq;
		transfuncs[5] = (float firstval, float factor, float seq) => firstval + (float)Math.Sign(factor) * (factor * seq) * (factor * seq);
		transfuncs[7] = (float firstval, float factor, float seq) => firstval + (float)Math.Sqrt(factor * seq);
		transfuncs[8] = (float firstval, float factor, float seq) => firstval + GameMath.FastSin(factor * seq);
		transfuncs[9] = (float firstval, float factor, float seq) => firstval * GameMath.Min(5f * Math.Abs(GameMath.FastSin(factor * seq)), 1f);
		transfuncs[10] = (float firstval, float factor, float seq) => firstval + GameMath.FastCos(factor * seq);
		transfuncs[11] = (float firstval, float factor, float seq) => firstval + GameMath.SmoothStep(factor * seq);
	}

	public EvolvingNatFloat()
	{
	}

	public EvolvingNatFloat(EnumTransformFunction transform, float factor)
	{
		this.transform = transform;
		this.factor = factor;
	}

	public static EvolvingNatFloat createIdentical(float factor)
	{
		return new EvolvingNatFloat(EnumTransformFunction.IDENTICAL, factor);
	}

	public static EvolvingNatFloat create(EnumTransformFunction function, float factor)
	{
		return new EvolvingNatFloat(function, factor);
	}

	private EvolvingNatFloat setMax(float? value)
	{
		maxvalue = value;
		return this;
	}

	public float nextFloat(float firstvalue, float sequence)
	{
		float num = transfuncs[(int)transform](firstvalue, factor, sequence);
		if (maxvalue.HasValue)
		{
			return Math.Min(maxvalue.Value, num);
		}
		return num;
	}

	public EvolvingNatFloat Clone()
	{
		return (EvolvingNatFloat)MemberwiseClone();
	}

	public void FromBytes(BinaryReader reader)
	{
		transform = (EnumTransformFunction)reader.ReadByte();
		factor = reader.ReadSingle();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write((byte)transform);
		writer.Write(factor);
	}

	public static EvolvingNatFloat CreateFromBytes(BinaryReader reader)
	{
		EvolvingNatFloat evolvingNatFloat = new EvolvingNatFloat();
		evolvingNatFloat.FromBytes(reader);
		return evolvingNatFloat;
	}
}
