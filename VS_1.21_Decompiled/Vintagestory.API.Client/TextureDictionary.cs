using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Client;

public class TextureDictionary : FastSmallDictionary<string, CompositeTexture>
{
	internal bool alreadyBaked;

	public TextureDictionary()
		: base(2)
	{
	}

	public TextureDictionary(int initialCapacity)
		: base(initialCapacity)
	{
	}

	public virtual void BakeAndCollect(IAssetManager manager, ITextureLocationDictionary mainTextureDict, AssetLocation sourceCode, string sourceMessage)
	{
		if (alreadyBaked)
		{
			return;
		}
		foreach (CompositeTexture value in base.Values)
		{
			value.Bake(manager);
			BakedCompositeTexture[] bakedVariants = value.Baked.BakedVariants;
			if (bakedVariants != null)
			{
				for (int i = 0; i < bakedVariants.Length; i++)
				{
					if (!mainTextureDict.ContainsKey(bakedVariants[i].BakedName))
					{
						mainTextureDict.SetTextureLocation(new AssetLocationAndSource(bakedVariants[i].BakedName, sourceMessage, sourceCode, i + 1));
					}
				}
				continue;
			}
			bakedVariants = value.Baked.BakedTiles;
			if (bakedVariants != null)
			{
				for (int j = 0; j < bakedVariants.Length; j++)
				{
					if (!mainTextureDict.ContainsKey(bakedVariants[j].BakedName))
					{
						mainTextureDict.SetTextureLocation(new AssetLocationAndSource(bakedVariants[j].BakedName, sourceMessage, sourceCode, j + 1));
					}
				}
			}
			else if (!mainTextureDict.ContainsKey(value.Baked.BakedName))
			{
				mainTextureDict.SetTextureLocation(new AssetLocationAndSource(value.Baked.BakedName, sourceMessage, sourceCode));
			}
		}
		alreadyBaked = true;
	}
}
