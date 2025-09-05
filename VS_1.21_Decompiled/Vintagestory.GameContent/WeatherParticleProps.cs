using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherParticleProps : SimpleParticleProperties
{
	public int[,] lowResRainHeightMap;

	public BlockPos centerPos;

	public override Vec3d Pos
	{
		get
		{
			tmpPos.Set(MinPos.X + AddPos.X * SimpleParticleProperties.rand.NextDouble(), MinPos.Y + AddPos.Y * SimpleParticleProperties.rand.NextDouble(), MinPos.Z + AddPos.Z * SimpleParticleProperties.rand.NextDouble());
			int num = (int)(tmpPos.X - (double)centerPos.X);
			int num2 = (int)(tmpPos.Z - (double)centerPos.Z);
			int num3 = GameMath.Clamp(num / 4 + 8, 0, 15);
			int num4 = GameMath.Clamp(num2 / 4 + 8, 0, 15);
			tmpPos.Y = Math.Max(tmpPos.Y, lowResRainHeightMap[num3, num4] + 3);
			return tmpPos;
		}
	}
}
