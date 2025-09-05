namespace Vintagestory.API.Client;

public class TextureAtlasPosition
{
	public const int RndColorsLength = 30;

	public int atlasTextureId;

	public byte atlasNumber;

	public short reloadIteration;

	public int AvgColor;

	public int[] RndColors;

	public float x1;

	public float y1;

	public float x2;

	public float y2;

	public TextureAtlasPosition Clone()
	{
		return new TextureAtlasPosition
		{
			atlasTextureId = atlasTextureId,
			atlasNumber = atlasNumber,
			reloadIteration = reloadIteration,
			AvgColor = AvgColor,
			RndColors = RndColors,
			x1 = x1,
			y1 = y1,
			x2 = x2,
			y2 = y2
		};
	}
}
