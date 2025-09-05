using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface ITextureAtlasAPI
{
	TextureAtlasPosition this[AssetLocation textureLocation] { get; }

	TextureAtlasPosition UnknownTexturePosition { get; }

	Size2i Size { get; }

	float SubPixelPaddingX { get; }

	float SubPixelPaddingY { get; }

	TextureAtlasPosition[] Positions { get; }

	List<LoadedTexture> AtlasTextures { get; }

	bool AllocateTextureSpace(int width, int height, out int textureSubId, out TextureAtlasPosition texPos, AssetLocationAndSource loc = null);

	bool InsertTexture(IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

	bool InsertTexture(byte[] pngBytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

	IBitmap LoadCompositeBitmap(AssetLocationAndSource path);

	bool GetOrInsertTexture(AssetLocationAndSource path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f);

	bool GetOrInsertTexture(AssetLocation path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f);

	[Obsolete("Use GetOrInsertTexture() instead. It's more efficient to load the bmp only if the texture was not found in the cache")]
	bool InsertTextureCached(AssetLocation path, IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

	bool InsertTextureCached(AssetLocation path, byte[] pngBytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

	bool GetOrInsertTexture(CompositeTexture ct, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

	void FreeTextureSpace(int textureSubId);

	int GetRandomColor(int textureSubId);

	void RegenMipMaps(int atlasIndex);

	int GetRandomColor(int textureSubId, int rndIndex);

	int GetRandomColor(TextureAtlasPosition texPos, int rndIndex);

	int[] GetRandomColors(TextureAtlasPosition texPos);

	int GetAverageColor(int textureSubId);

	void RenderTextureIntoAtlas(int intoAtlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float alphaTest = 0.005f);
}
