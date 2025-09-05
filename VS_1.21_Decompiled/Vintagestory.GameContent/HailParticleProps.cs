using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class HailParticleProps : WeatherParticleProps
{
	public override Vec3d Pos
	{
		get
		{
			double x = MinPos.X + SimpleParticleProperties.rand.NextDouble() * SimpleParticleProperties.rand.NextDouble() * 80.0 * (double)(1 - 2 * SimpleParticleProperties.rand.Next(2));
			double z = MinPos.Z + SimpleParticleProperties.rand.NextDouble() * SimpleParticleProperties.rand.NextDouble() * 80.0 * (double)(1 - 2 * SimpleParticleProperties.rand.Next(2));
			tmpPos.Set(x, MinPos.Y + AddPos.Y * SimpleParticleProperties.rand.NextDouble(), z);
			int num = (int)(tmpPos.X - (double)centerPos.X);
			int num2 = (int)(tmpPos.Z - (double)centerPos.Z);
			int num3 = GameMath.Clamp(num / 4 + 8, 0, 15);
			int num4 = GameMath.Clamp(num2 / 4 + 8, 0, 15);
			tmpPos.Y = Math.Max(tmpPos.Y, lowResRainHeightMap[num3, num4] + 3);
			return tmpPos;
		}
	}
}
