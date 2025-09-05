namespace Vintagestory.API.MathTools;

public interface IRandom
{
	int NextInt(int max);

	int NextInt();

	double NextDouble();

	float NextFloat();
}
