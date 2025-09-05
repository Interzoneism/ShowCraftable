using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemSkinnableAdditions : ModSystem
{
	protected Dictionary<string, SkinnablePart> skinPartsByCode = new Dictionary<string, SkinnablePart>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		base.AssetsLoaded(api);
		new List<SkinnablePart>();
		foreach (IAsset item in api.Assets.GetMany("config/seraphskinnableparts.json"))
		{
			SkinnablePart[] array = item.ToObject<SkinnablePart[]>();
			foreach (SkinnablePart skinnablePart in array)
			{
				skinPartsByCode.TryGetValue(skinnablePart.Code, out var value);
				if (value != null)
				{
					value.Variants = value.Variants.Append(skinnablePart.Variants);
				}
				else
				{
					skinPartsByCode[skinnablePart.Code] = skinnablePart;
				}
			}
		}
	}

	public SkinnablePart[] AppendAdditions(SkinnablePart[] toParts)
	{
		foreach (SkinnablePart skinnablePart in toParts)
		{
			skinPartsByCode.TryGetValue(skinnablePart.Code, out var value);
			if (value != null)
			{
				skinnablePart.Variants = skinnablePart.Variants.Append(value.Variants);
			}
		}
		return toParts;
	}
}
