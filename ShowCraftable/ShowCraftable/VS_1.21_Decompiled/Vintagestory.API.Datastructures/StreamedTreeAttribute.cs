using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class StreamedTreeAttribute
{
	private readonly BinaryWriter stream;

	public IAttribute this[string key]
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			if (value is StreamedByteArrayAttribute streamedByteArrayAttribute)
			{
				streamedByteArrayAttribute.BeginDirectWrite(stream, key);
				streamedByteArrayAttribute.ToBytes(stream);
			}
		}
	}

	public StreamedTreeAttribute(BinaryWriter writer)
	{
		stream = writer;
	}

	internal void WithKey(string key)
	{
		TreeAttribute.BeginDirectWrite(stream, key);
	}

	internal void EndKey()
	{
		TreeAttribute.TerminateWrite(stream);
	}
}
