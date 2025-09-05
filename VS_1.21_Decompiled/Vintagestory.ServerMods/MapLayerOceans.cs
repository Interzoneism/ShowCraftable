using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

internal class MapLayerOceans : MapLayerBase
{
	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	private float wobbleIntensity;

	private NoiseOcean noiseOcean;

	public float landFormHorizontalScale = 1f;

	private List<XZ> requireLandAt;

	private int spawnOffsX;

	private int spawnOffsZ;

	private float scale;

	private readonly bool requiresSpawnOffset;

	public MapLayerOceans(long seed, float scale, float landCoverRate, List<XZ> requireLandAt, bool requiresSpawnOffset)
		: base(seed)
	{
		noiseOcean = new NoiseOcean(seed, scale, landCoverRate);
		this.requireLandAt = requireLandAt;
		this.scale = scale;
		int quantityOctaves = 3;
		float num = 0.9f;
		wobbleIntensity = (float)TerraGenConfig.oceanMapScale * scale / (float)TerraGenConfig.oceanMapScale;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / scale, num, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / scale, num, seed + 1231296);
		this.requiresSpawnOffset = requiresSpawnOffset;
		XZ xZ = requireLandAt[0];
		XZ noiseOffsetAt = GetNoiseOffsetAt(xZ.X, xZ.Z);
		spawnOffsX = -noiseOffsetAt.X;
		spawnOffsZ = -noiseOffsetAt.Z;
	}

	public XZ GetNoiseOffsetAt(int xCoord, int zCoord)
	{
		int x = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord, zCoord) * 1.2000000476837158);
		int z = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord, zCoord) * 1.2000000476837158);
		return new XZ(x, z);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		if (requiresSpawnOffset)
		{
			xCoord += spawnOffsX;
			zCoord += spawnOffsZ;
		}
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				int num = xCoord + i;
				int num2 = zCoord + j;
				int num3 = (int)((double)wobbleIntensity * noisegenX.Noise(num, num2));
				int num4 = (int)((double)wobbleIntensity * noisegenY.Noise(num, num2));
				int num5 = num + num3;
				int num6 = num2 + num4;
				int num7 = noiseOcean.GetOceanIndexAt(num5, num6);
				if (num7 == 255)
				{
					if (requiresSpawnOffset)
					{
						float num8 = scale / 2f;
						for (int k = 0; k < requireLandAt.Count; k++)
						{
							XZ xZ = requireLandAt[k];
							if ((float)Math.Abs(xZ.X - num5) <= num8 && (float)Math.Abs(xZ.Z - num6) <= num8)
							{
								num7 = 0;
								break;
							}
						}
					}
					else
					{
						for (int l = 0; l < requireLandAt.Count; l++)
						{
							XZ xZ2 = requireLandAt[l];
							if (xZ2.X == num && xZ2.Z == num2)
							{
								num7 = 0;
								break;
							}
						}
					}
				}
				array[j * sizeX + i] = num7;
			}
		}
		return array;
	}
}
