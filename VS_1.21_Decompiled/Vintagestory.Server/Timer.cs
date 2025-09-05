using System;

namespace Vintagestory.Server;

public class Timer
{
	public delegate void Tick();

	private double interval = 1.0;

	private double maxDeltaTime = double.PositiveInfinity;

	private double starttime;

	private double oldtime;

	public double Accumulator;

	public double Interval
	{
		get
		{
			return interval;
		}
		set
		{
			interval = value;
		}
	}

	public double MaxDeltaTime
	{
		get
		{
			return maxDeltaTime;
		}
		set
		{
			maxDeltaTime = value;
		}
	}

	public Timer()
	{
		Reset();
	}

	public void Reset()
	{
		starttime = Gettime();
	}

	public void Update(Tick tick)
	{
		double num = Gettime() - starttime;
		double num2 = num - oldtime;
		Accumulator += num2;
		double num3 = Interval;
		if (MaxDeltaTime != double.PositiveInfinity && Accumulator > MaxDeltaTime)
		{
			Accumulator = MaxDeltaTime;
		}
		while (Accumulator >= num3)
		{
			tick();
			Accumulator -= num3;
		}
		oldtime = num;
	}

	private static double Gettime()
	{
		return (double)DateTime.UtcNow.Ticks / 10000000.0;
	}
}
