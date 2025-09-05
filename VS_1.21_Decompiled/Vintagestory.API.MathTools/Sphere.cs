namespace Vintagestory.API.MathTools;

public struct Sphere
{
	public float x;

	public float y;

	public float z;

	public float radius;

	public float radiusY;

	public float radiusZ;

	public const float sqrt3half = 0.8660254f;

	public Sphere(float x1, float y1, float z1, float dx, float dy, float dz)
	{
		x = x1;
		y = y1;
		z = z1;
		radius = 0.8660254f * dx;
		radiusY = 0.8660254f * dy;
		radiusZ = 0.8660254f * dz;
	}

	public static Sphere BoundingSphereForCube(float x, float y, float z, float size)
	{
		return new Sphere
		{
			x = x + size / 2f,
			y = y + size / 2f,
			z = z + size / 2f,
			radius = 0.8660254f * size,
			radiusY = 0.8660254f * size,
			radiusZ = 0.8660254f * size
		};
	}
}
