using System.IO;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class TransitionableProperties
{
	[DocumentAsJson]
	public EnumTransitionType Type = EnumTransitionType.None;

	[DocumentAsJson]
	public NatFloat FreshHours = NatFloat.createUniform(36f, 0f);

	[DocumentAsJson]
	public NatFloat TransitionHours = NatFloat.createUniform(12f, 0f);

	[DocumentAsJson]
	public JsonItemStack TransitionedStack;

	[DocumentAsJson]
	public float TransitionRatio = 1f;

	public TransitionableProperties Clone()
	{
		return new TransitionableProperties
		{
			FreshHours = FreshHours.Clone(),
			TransitionHours = TransitionHours.Clone(),
			TransitionRatio = TransitionRatio,
			TransitionedStack = TransitionedStack?.Clone(),
			Type = Type
		};
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write((ushort)Type);
		FreshHours.ToBytes(writer);
		TransitionHours.ToBytes(writer);
		TransitionedStack.ToBytes(writer);
		writer.Write(TransitionRatio);
	}

	public void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		Type = (EnumTransitionType)reader.ReadUInt16();
		FreshHours = NatFloat.createFromBytes(reader);
		TransitionHours = NatFloat.createFromBytes(reader);
		TransitionedStack = new JsonItemStack();
		TransitionedStack.FromBytes(reader, instancer);
		TransitionRatio = reader.ReadSingle();
	}
}
