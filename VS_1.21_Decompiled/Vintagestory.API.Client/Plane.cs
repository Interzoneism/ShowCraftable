using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public struct Plane
{
	public double normalX;

	public double normalY;

	public double normalZ;

	public double D;

	private const float SQRT3 = 1.7320508f;

	public Plane(double x, double y, double z, double d)
	{
		double num = Math.Sqrt(x * x + y * y + z * z);
		normalX = x / num;
		normalY = y / num;
		normalZ = z / num;
		D = d / num;
	}

	public double distanceOfPoint(double x, double y, double z)
	{
		return normalX * x + normalY * y + normalZ * z + D;
	}

	public bool AABBisOutside(Sphere sphere)
	{
		int num = ((normalX > 0.0) ? 1 : (-1));
		double num2 = (double)sphere.x + (double)((float)num * sphere.radius / 1.7320508f);
		num = ((normalY > 0.0) ? 1 : (-1));
		double num3 = (double)sphere.y + (double)((float)num * sphere.radiusY / 1.7320508f);
		num = ((normalZ > 0.0) ? 1 : (-1));
		double num4 = (double)sphere.z + (double)((float)num * sphere.radiusZ / 1.7320508f);
		return num2 * normalX + num3 * normalY + num4 * normalZ + D < 0.0;
	}
}
