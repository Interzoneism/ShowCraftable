namespace Vintagestory.API.Client;

public class CustomMeshDataPartInt : CustomMeshDataPart<int>
{
	public DataConversion Conversion = DataConversion.Integer;

	public CustomMeshDataPartInt()
	{
	}

	public CustomMeshDataPartInt(int size)
		: base(size)
	{
	}

	public CustomMeshDataPartInt Clone()
	{
		CustomMeshDataPartInt customMeshDataPartInt = new CustomMeshDataPartInt();
		customMeshDataPartInt.SetFrom(this);
		return customMeshDataPartInt;
	}

	public CustomMeshDataPartInt EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartInt()) as CustomMeshDataPartInt;
	}
}
