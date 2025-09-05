namespace Vintagestory.API.Common;

[DocumentAsJson]
public class BakingProperties
{
	[DocumentAsJson]
	public float? Temp;

	[DocumentAsJson]
	public float LevelFrom;

	[DocumentAsJson]
	public float LevelTo;

	[DocumentAsJson]
	public float StartScaleY;

	[DocumentAsJson]
	public float EndScaleY;

	[DocumentAsJson]
	public string ResultCode;

	[DocumentAsJson]
	public string InitialCode;

	[DocumentAsJson]
	public bool LargeItem;

	public static BakingProperties ReadFrom(ItemStack stack)
	{
		if (stack == null)
		{
			return null;
		}
		BakingProperties bakingProperties = stack.Collectible?.Attributes?["bakingProperties"]?.AsObject<BakingProperties>();
		if (bakingProperties == null)
		{
			return null;
		}
		if (!bakingProperties.Temp.HasValue || bakingProperties.Temp == 0f)
		{
			CombustibleProperties combustibleProps = stack.Collectible.CombustibleProps;
			if (combustibleProps != null)
			{
				bakingProperties.Temp = combustibleProps.MeltingPoint - 40;
			}
		}
		return bakingProperties;
	}
}
