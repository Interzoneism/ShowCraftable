using Vintagestory.API.Client;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

internal class TextureAtlasNode
{
	public TextureAtlasNode left;

	public TextureAtlasNode right;

	public QuadBoundsi bounds;

	public int? textureSubId;

	public TextureAtlasNode(int x1, int y1, int x2, int y2)
	{
		bounds = new QuadBoundsi
		{
			x1 = x1,
			y1 = y1,
			x2 = x2,
			y2 = y2
		};
	}

	public void PopulateAtlasPositions(TextureAtlasPosition[] positions, int atlasTextureId, int atlasNumber, float atlasWidth, float atlasHeight, float subPixelPaddingx, float subPixelPaddingy)
	{
		if (textureSubId.HasValue)
		{
			positions[textureSubId.Value] = new TextureAtlasPosition
			{
				atlasTextureId = atlasTextureId,
				atlasNumber = (byte)atlasNumber,
				x1 = (float)bounds.x1 / atlasWidth + subPixelPaddingx,
				y1 = (float)bounds.y1 / atlasHeight + subPixelPaddingy,
				x2 = (float)bounds.x2 / atlasWidth - subPixelPaddingx,
				y2 = (float)bounds.y2 / atlasHeight - subPixelPaddingy
			};
		}
		if (left != null)
		{
			left.PopulateAtlasPositions(positions, atlasTextureId, atlasNumber, atlasWidth, atlasHeight, subPixelPaddingx, subPixelPaddingy);
		}
		if (right != null)
		{
			right.PopulateAtlasPositions(positions, atlasTextureId, atlasNumber, atlasWidth, atlasHeight, subPixelPaddingx, subPixelPaddingy);
		}
	}

	public TextureAtlasNode GetFreeNode(int textureSubId, int width, int height)
	{
		if (left != null)
		{
			TextureAtlasNode freeNode = left.GetFreeNode(textureSubId, width, height);
			if (freeNode == null)
			{
				freeNode = right.GetFreeNode(textureSubId, width, height);
			}
			return freeNode;
		}
		if (this.textureSubId.HasValue)
		{
			return null;
		}
		int num = bounds.x2 - bounds.x1;
		int num2 = bounds.y2 - bounds.y1;
		if (num < width || num2 < height)
		{
			return null;
		}
		if (num == width && num2 == height)
		{
			return this;
		}
		int num3 = num - width;
		int num4 = num2 - height;
		if (num3 > num4)
		{
			left = new TextureAtlasNode(bounds.x1, bounds.y1, bounds.x1 + width, bounds.y2);
			right = new TextureAtlasNode(bounds.x1 + width, bounds.y1, bounds.x2, bounds.y2);
		}
		else
		{
			left = new TextureAtlasNode(bounds.x1, bounds.y1, bounds.x2, bounds.y1 + height);
			right = new TextureAtlasNode(bounds.x1, bounds.y1 + height, bounds.x2, bounds.y2);
		}
		return left.GetFreeNode(textureSubId, width, height);
	}
}
