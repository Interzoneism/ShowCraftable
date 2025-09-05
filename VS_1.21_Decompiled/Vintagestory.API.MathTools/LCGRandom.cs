namespace Vintagestory.API.MathTools;

public class LCGRandom : IRandom
{
	public long worldSeed;

	public long mapGenSeed;

	public long currentSeed;

	private const float maxIntF = 2.1474836E+09f;

	private const double maxIntD = 4294967295.0;

	private const float semiMaxIntF = 46340f;

	private const int semiMaxInt = 46341;

	public LCGRandom(long worldSeed)
	{
		SetWorldSeed(worldSeed);
	}

	public LCGRandom()
	{
	}

	public void SetWorldSeed(long worldSeed)
	{
		this.worldSeed = worldSeed;
		currentSeed = mapGenSeed * 6364136223846793005L + 1442695040888963407L;
		long num = worldSeed;
		num *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		num++;
		num *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		num += 2;
		num *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		num += 3;
		mapGenSeed = num;
	}

	public void InitPositionSeed(int xPos, int zPos)
	{
		long num = mapGenSeed;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += xPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += zPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += xPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += zPos;
		currentSeed = num;
	}

	public void InitPositionSeed(int xPos, int yPos, int zPos)
	{
		long num = mapGenSeed;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += xPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += yPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += zPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += xPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += yPos;
		num *= num * 6364136223846793005L + 1442695040888963407L;
		num += zPos;
		currentSeed = num;
	}

	public int NextInt(int max)
	{
		long num = currentSeed;
		int num2 = (int)((num >> 24) % max);
		if (num2 < 0)
		{
			num2 += max;
		}
		currentSeed = num * (num * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return num2;
	}

	public int NextInt()
	{
		long num = currentSeed;
		int result = (int)(num >> 24) & 0x7FFFFFFF;
		currentSeed = num * (num * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return result;
	}

	public float NextFloat()
	{
		long num = currentSeed;
		int num2 = (int)(num >> 24) & 0x7FFFFFFF;
		currentSeed = num * (num * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (float)num2 / 2.1474836E+09f;
	}

	public float NextFloatMinusToPlusOne()
	{
		long num = currentSeed;
		int num2 = (int)(num >> 24) & 0x7FFFFFFF;
		currentSeed = num * (num * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (float)(num2 % 46341 - num2 / 46341) / 46340f;
	}

	public double NextDouble()
	{
		long num = currentSeed;
		int num2 = (int)(num >> 24);
		currentSeed = num * (num * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (double)(uint)num2 / 4294967295.0;
	}
}
