using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

[DocumentAsJson]
public class RotatableCube : Cuboidf
{
	[DocumentAsJson]
	public float RotateX;

	[DocumentAsJson]
	public float RotateY;

	[DocumentAsJson]
	public float RotateZ;

	[DocumentAsJson]
	public Vec3d Origin = new Vec3d(0.5, 0.5, 0.5);

	public RotatableCube()
	{
	}

	public Cuboidi ToHitboxCuboidi(float rotateY, Vec3d origin = null)
	{
		return RotatedCopy(0f, rotateY, 0f, origin ?? new Vec3d(8.0, 8.0, 8.0)).ConvertToCuboidi();
	}

	public RotatableCube(float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ)
		: base(MinX, MinY, MinZ, MaxX, MaxY, MaxZ)
	{
	}

	public Cuboidf RotatedCopy()
	{
		return RotatedCopy(RotateX, RotateY, RotateZ, Origin);
	}

	public new RotatableCube Clone()
	{
		return (RotatableCube)MemberwiseClone();
	}
}
