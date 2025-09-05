using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

internal class MapLayerGeoProvince : MapLayerBase
{
	private NoiseGeoProvince noiseGeoProvince;

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	private float wobbleIntensity;

	public MapLayerGeoProvince(long seed, ICoreServerAPI api)
		: base(seed)
	{
		noiseGeoProvince = new NoiseGeoProvince(seed, api);
		int quantityOctaves = 4;
		float num = 1.5f * (float)TerraGenConfig.geoProvMapScale;
		float num2 = 0.9f;
		wobbleIntensity = (float)TerraGenConfig.geoProvMapScale * 1.5f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 0.4f / num, num2, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 0.4f / num, num2, seed + 1231296);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				int num = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord + i, zCoord + j));
				int num2 = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord + i, zCoord + j));
				int xpos = (xCoord + i + num) / TerraGenConfig.geoProvMapScale;
				int zpos = (zCoord + j + num2) / TerraGenConfig.geoProvMapScale;
				array[j * sizeX + i] = noiseGeoProvince.GetProvinceIndexAt(xpos, zpos);
			}
		}
		return array;
	}
}
