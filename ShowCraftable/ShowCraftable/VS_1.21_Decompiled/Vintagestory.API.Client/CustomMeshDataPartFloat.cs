namespace Vintagestory.API.Client;

public class CustomMeshDataPartFloat : CustomMeshDataPart<float>
{
	public CustomMeshDataPartFloat()
	{
	}

	public CustomMeshDataPartFloat(int arraySize)
		: base(arraySize)
	{
	}

	public CustomMeshDataPartFloat Clone()
	{
		CustomMeshDataPartFloat customMeshDataPartFloat = new CustomMeshDataPartFloat();
		customMeshDataPartFloat.SetFrom(this);
		return customMeshDataPartFloat;
	}

	public CustomMeshDataPartFloat EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartFloat()) as CustomMeshDataPartFloat;
	}
}
