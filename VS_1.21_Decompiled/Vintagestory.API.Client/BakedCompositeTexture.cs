using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class BakedCompositeTexture
{
	public int TextureSubId;

	public AssetLocation BakedName;

	public AssetLocation[] TextureFilenames;

	public BakedCompositeTexture[] BakedVariants;

	public BakedCompositeTexture[] BakedTiles;

	public int TilesWidth;

	public static int GetTiledTexturesSelector(BakedCompositeTexture[] tiles, int tileSide, int posX, int posY, int posZ)
	{
		BakedCompositeTexture obj = tiles[0];
		int tilesWidth = obj.TilesWidth;
		int n = tiles.Length / tilesWidth;
		string path = obj.BakedName.Path;
		int num = path.IndexOf('@');
		int result = 0;
		if (num > 0)
		{
			int num2 = num + 1;
			int.TryParse(path.Substring(num2, path.Length - num2), out result);
			result /= 90;
		}
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		switch (tileSide)
		{
		case 0:
			num3 = posX;
			num4 = posZ;
			num5 = posY;
			break;
		case 1:
			num3 = posZ;
			num4 = -posX;
			num5 = posY;
			break;
		case 2:
			num3 = -posX;
			num4 = posZ;
			num5 = posY;
			break;
		case 3:
			num3 = -posZ;
			num4 = -posX;
			num5 = posY;
			break;
		case 4:
			num3 = posX;
			num4 = posY;
			num5 = posZ;
			break;
		case 5:
			num3 = posX;
			num4 = posY;
			num5 = -posZ;
			break;
		}
		return result switch
		{
			0 => GameMath.Mod(-num3 + num4, tilesWidth) + tilesWidth * GameMath.Mod(-num5, n), 
			1 => GameMath.Mod(num5 + num4, tilesWidth) + tilesWidth * GameMath.Mod(num3, n), 
			2 => GameMath.Mod(num3 + num4, tilesWidth) + tilesWidth * GameMath.Mod(num5, n), 
			3 => GameMath.Mod(-num5 + num4, tilesWidth) + tilesWidth * GameMath.Mod(-num3, n), 
			_ => 0, 
		};
	}
}
