using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class WeatherSystemConfig
{
	[JsonProperty]
	public AssetLocation[] SnowLayerBlockCodes;

	[JsonProperty]
	public WeatherPatternConfig RainOverlayPattern;

	public OrderedDictionary<Block, int> SnowLayerBlocks;

	internal void Init(IWorldAccessor world)
	{
		SnowLayerBlocks = new OrderedDictionary<Block, int>();
		int num = 0;
		AssetLocation[] snowLayerBlockCodes = SnowLayerBlockCodes;
		foreach (AssetLocation assetLocation in snowLayerBlockCodes)
		{
			Block block = world.GetBlock(assetLocation);
			if (block == null)
			{
				world.Logger.Error("config/weather.json: No such block found: '{0}', will ignore.", assetLocation);
			}
			else
			{
				SnowLayerBlocks[block] = num++;
			}
		}
	}
}
