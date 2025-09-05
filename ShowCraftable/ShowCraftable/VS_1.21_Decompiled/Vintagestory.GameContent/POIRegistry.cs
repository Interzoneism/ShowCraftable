using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class POIRegistry : ModSystem
{
	private Dictionary<Vec2i, List<IPointOfInterest>> PoisByChunkColumn = new Dictionary<Vec2i, List<IPointOfInterest>>();

	private Vec2i tmp = new Vec2i();

	private const int chunksize = 32;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
	}

	public void WalkPois(Vec3d centerPos, float radius, PoiMatcher callback = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int num2 = (int)(centerPos.Z - (double)radius) / 32;
		int num3 = (int)(centerPos.X + (double)radius) / 32;
		int num4 = (int)(centerPos.Z + (double)radius) / 32;
		float num5 = radius * radius;
		for (int i = num; i < num3; i++)
		{
			for (int j = num2; j < num4; j++)
			{
				tmp.Set(i, j);
				PoisByChunkColumn.TryGetValue(tmp, out var value);
				if (value == null)
				{
					continue;
				}
				for (int k = 0; k < value.Count; k++)
				{
					if (!(value[k].Position.SquareDistanceTo(centerPos) > num5))
					{
						callback(value[k]);
					}
				}
			}
		}
	}

	public IPointOfInterest GetNearestPoi(Vec3d centerPos, float radius, PoiMatcher matcher = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int num2 = (int)(centerPos.Z - (double)radius) / 32;
		int num3 = (int)(centerPos.X + (double)radius) / 32;
		int num4 = (int)(centerPos.Z + (double)radius) / 32;
		float num5 = radius * radius;
		float num6 = 9999999f;
		IPointOfInterest result = null;
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				tmp.Set(i, j);
				PoisByChunkColumn.TryGetValue(tmp, out var value);
				if (value == null)
				{
					continue;
				}
				for (int k = 0; k < value.Count; k++)
				{
					float num7 = value[k].Position.SquareDistanceTo(centerPos);
					if (!(num7 > num5) && num7 < num6 && matcher(value[k]))
					{
						result = value[k];
						num6 = num7;
					}
				}
			}
		}
		return result;
	}

	public IPointOfInterest GetWeightedNearestPoi(Vec3d centerPos, float radius, PoiMatcher matcher = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int num2 = (int)(centerPos.Z - (double)radius) / 32;
		int num3 = (int)(centerPos.X + (double)radius) / 32;
		int num4 = (int)(centerPos.Z + (double)radius) / 32;
		float num5 = radius * radius;
		float num6 = 9999999f;
		IPointOfInterest result = null;
		for (int i = num; i <= num3; i++)
		{
			double num7 = 0.0;
			if ((double)(i * 32) > centerPos.X)
			{
				num7 = (double)(i * 32) - centerPos.X;
			}
			else if ((double)((i + 1) * 32) < centerPos.X)
			{
				num7 = centerPos.X - (double)((i + 1) * 32);
			}
			for (int j = num2; j <= num4; j++)
			{
				double num8 = 0.0;
				if ((double)(j * 32) > centerPos.Z)
				{
					num8 = (double)(j * 32) - centerPos.Z;
				}
				else if ((double)((j + 1) * 32) < centerPos.Z)
				{
					num8 = centerPos.Z - (double)((j + 1) * 32);
				}
				if (num7 * num7 + num8 * num8 > (double)num6)
				{
					continue;
				}
				tmp.Set(i, j);
				PoisByChunkColumn.TryGetValue(tmp, out var value);
				if (value == null)
				{
					continue;
				}
				for (int k = 0; k < value.Count; k++)
				{
					Vec3d position = value[k].Position;
					float num9 = ((value[k] is IAnimalNest animalNest) ? animalNest.DistanceWeighting : 1f);
					float num10 = position.SquareDistanceTo(centerPos) * num9;
					if (!(num10 > num5) && num10 < num6 && matcher(value[k]))
					{
						result = value[k];
						num6 = num10;
					}
				}
			}
		}
		return result;
	}

	public void AddPOI(IPointOfInterest poi)
	{
		tmp.Set((int)poi.Position.X / 32, (int)poi.Position.Z / 32);
		PoisByChunkColumn.TryGetValue(tmp, out var value);
		if (value == null)
		{
			value = (PoisByChunkColumn[tmp] = new List<IPointOfInterest>());
		}
		if (!value.Contains(poi))
		{
			value.Add(poi);
		}
	}

	public void RemovePOI(IPointOfInterest poi)
	{
		tmp.Set((int)poi.Position.X / 32, (int)poi.Position.Z / 32);
		PoisByChunkColumn.TryGetValue(tmp, out var value);
		value?.Remove(poi);
	}
}
