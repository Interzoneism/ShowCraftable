using System;

namespace Vintagestory.Common;

public class BoxedPacket : BoxedArray
{
	public int Length;

	public int LengthSent;

	internal int Serialize(IPacket p)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream(this);
		p.SerializeTo(citoMemoryStream);
		return Length = citoMemoryStream.Position();
	}

	public override void Dispose()
	{
		buffer = null;
		Length = 0;
		LengthSent = 0;
	}

	internal byte[] Clone(int destOffset)
	{
		int length = Length;
		byte[] array = new byte[length + destOffset];
		if (length > 256)
		{
			Array.Copy(buffer, 0, array, destOffset, length);
		}
		else
		{
			int num = length - length % 4;
			int i;
			for (i = 0; i < num; i += 4)
			{
				array[destOffset] = buffer[i];
				array[destOffset + 1] = buffer[i + 1];
				array[destOffset + 2] = buffer[i + 2];
				array[destOffset + 3] = buffer[i + 3];
				destOffset += 4;
			}
			for (; i < length; i++)
			{
				array[destOffset++] = buffer[i];
			}
		}
		return array;
	}
}
