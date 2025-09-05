using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IBlockTextureAtlasAPI : ITextureAtlasAPI
{
	TextureAtlasPosition GetPosition(Block block, string textureName, bool returnNullWhenMissing = false);
}
