using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class MusicTrackPart
{
	[JsonProperty]
	public float MinSuitability = 0.1f;

	[JsonProperty]
	public float MaxSuitability = 1f;

	[JsonProperty]
	public float MinVolumne = 0.35f;

	[JsonProperty]
	public float MaxVolumne = 1f;

	[JsonProperty]
	public float[] PosY;

	[JsonProperty]
	public float[] Sunlight;

	[JsonProperty]
	public AssetLocation[] Files;

	public ILoadedSound Sound;

	public long StartedMs;

	public bool Loading;

	internal AssetLocation NowPlayingFile;

	public bool IsPlaying
	{
		get
		{
			if (Sound != null)
			{
				return Sound.IsPlaying;
			}
			return false;
		}
	}

	public bool Applicable(IWorldAccessor world, TrackedPlayerProperties props)
	{
		return CurrentSuitability(world, props) > MinSuitability;
	}

	public float CurrentVolume(IWorldAccessor world, TrackedPlayerProperties props)
	{
		float num = CurrentSuitability(world, props);
		if (num == 1f)
		{
			return 1f;
		}
		float num2 = (MaxVolumne - MinVolumne) / (MaxSuitability - MinSuitability);
		float num3 = MinVolumne - num2 * MinSuitability;
		if (num < MinSuitability)
		{
			return 0f;
		}
		return GameMath.Min(num2 * num + num3, MaxVolumne);
	}

	public float CurrentSuitability(IWorldAccessor world, TrackedPlayerProperties props)
	{
		int num = 0;
		float num2 = 0f;
		if (PosY != null)
		{
			num2 += GameMath.TriangleStep(props.posY, PosY[0], PosY[1]);
			num++;
		}
		if (Sunlight != null)
		{
			num2 += GameMath.TriangleStep(props.sunSlight, Sunlight[0], Sunlight[1]);
			num++;
		}
		if (num == 0)
		{
			return 1f;
		}
		return num2 / (float)num;
	}

	public virtual void ExpandFiles(IAssetManager assetManager)
	{
		List<AssetLocation> list = new List<AssetLocation>();
		for (int i = 0; i < Files.Length; i++)
		{
			AssetLocation assetLocation = Files[i];
			if (assetLocation.Path.EndsWith('*'))
			{
				foreach (AssetLocation location in assetManager.GetLocations("music/" + assetLocation.Path.Substring(0, assetLocation.Path.Length - 1), assetLocation.Domain))
				{
					list.Add(location);
				}
			}
			else
			{
				list.Add(assetLocation);
			}
		}
		Files = list.ToArray();
	}
}
