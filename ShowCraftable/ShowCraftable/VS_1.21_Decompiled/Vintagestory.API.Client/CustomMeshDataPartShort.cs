namespace Vintagestory.API.Client;

public class CustomMeshDataPartShort : CustomMeshDataPart<short>
{
	public DataConversion Conversion = DataConversion.NormalizedFloat;

	public CustomMeshDataPartShort()
	{
	}

	public CustomMeshDataPartShort(int size)
		: base(size)
	{
	}

	public CustomMeshDataPartShort Clone()
	{
		CustomMeshDataPartShort customMeshDataPartShort = new CustomMeshDataPartShort();
		customMeshDataPartShort.SetFrom(this);
		return customMeshDataPartShort;
	}

	public CustomMeshDataPartShort EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartShort()) as CustomMeshDataPartShort;
	}

	public void AddPackedUV(float u1, float v1)
	{
		Add((short)(u1 * 32768f + 0.5f));
		Add((short)(v1 * 32768f + 0.5f));
	}
}
