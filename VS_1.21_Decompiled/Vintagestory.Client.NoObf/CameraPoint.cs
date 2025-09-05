using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf;

public class CameraPoint
{
	internal double x;

	internal double y;

	internal double z;

	internal float pitch;

	internal float yaw;

	internal float roll;

	internal double distance;

	public static CameraPoint FromEntityPos(EntityPos pos)
	{
		return new CameraPoint
		{
			x = pos.X,
			y = pos.Y,
			z = pos.Z,
			pitch = pos.Pitch,
			yaw = pos.Yaw,
			roll = pos.Roll
		};
	}

	internal CameraPoint Clone()
	{
		return new CameraPoint
		{
			x = x,
			y = y,
			z = z,
			pitch = pitch,
			yaw = yaw,
			roll = roll
		};
	}

	internal CameraPoint ExtrapolateFrom(CameraPoint p, int direction)
	{
		double num = p.x - x;
		double num2 = p.y - y;
		double num3 = p.z - z;
		float num4 = p.pitch - pitch;
		float num5 = p.yaw - yaw;
		float num6 = p.roll - roll;
		return new CameraPoint
		{
			x = x - num * (double)direction,
			y = y - num2 * (double)direction,
			z = z - num3 * (double)direction,
			pitch = pitch - num4 * (float)direction,
			yaw = yaw - num5 * (float)direction,
			roll = roll - num6 * (float)direction
		};
	}

	internal bool PositionEquals(CameraPoint point)
	{
		if (point.x == x && point.y == y)
		{
			return point.z == z;
		}
		return false;
	}
}
