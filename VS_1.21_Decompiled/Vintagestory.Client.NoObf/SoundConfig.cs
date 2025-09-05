using System.Collections.Generic;
using System.Runtime.Serialization;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class SoundConfig
{
	public Dictionary<AssetLocation, AssetLocation[]> Soundsets = new Dictionary<AssetLocation, AssetLocation[]>();

	public BlockSounds defaultBlockSounds = new BlockSounds();

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Dictionary<AssetLocation, AssetLocation[]> dictionary = new Dictionary<AssetLocation, AssetLocation[]>();
		foreach (KeyValuePair<AssetLocation, AssetLocation[]> soundset in Soundsets)
		{
			soundset.Key.WithPathPrefix("sounds/");
			for (int i = 0; i < soundset.Value.Length; i++)
			{
				soundset.Value[i].WithPathPrefix("sounds/");
			}
			dictionary[soundset.Key] = soundset.Value;
		}
		Soundsets = dictionary;
	}
}
