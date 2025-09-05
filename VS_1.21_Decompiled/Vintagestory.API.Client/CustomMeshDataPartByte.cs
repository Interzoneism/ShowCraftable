namespace Vintagestory.API.Client;

public class CustomMeshDataPartByte : CustomMeshDataPart<byte>
{
	public DataConversion Conversion = DataConversion.NormalizedFloat;

	public CustomMeshDataPartByte()
	{
	}

	public CustomMeshDataPartByte(int size)
		: base(size)
	{
	}

	public unsafe void AddBytes(int fourbytes)
	{
		if (Count + 4 >= base.BufferSize)
		{
			GrowBuffer();
		}
		fixed (byte* values = Values)
		{
			int* ptr = (int*)values;
			ptr[Count / 4] = fourbytes;
		}
		Count += 4;
	}

	public CustomMeshDataPartByte Clone()
	{
		CustomMeshDataPartByte customMeshDataPartByte = new CustomMeshDataPartByte();
		customMeshDataPartByte.SetFrom(this);
		customMeshDataPartByte.Conversion = Conversion;
		return customMeshDataPartByte;
	}

	public CustomMeshDataPartByte EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartByte()) as CustomMeshDataPartByte;
	}
}
