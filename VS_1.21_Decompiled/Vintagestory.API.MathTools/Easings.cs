using System;

namespace Vintagestory.API.MathTools;

public static class Easings
{
	public static double EaseOutBack(double x)
	{
		double num = 1.7015800476074219;
		double num2 = num + 1.0;
		return 1.0 + num2 * Math.Pow(x - 1.0, 3.0) + num * Math.Pow(x - 1.0, 2.0);
	}
}
