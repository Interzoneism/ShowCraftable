using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IItemTextureAtlasAPI : ITextureAtlasAPI
{
	TextureAtlasPosition GetPosition(Item item, string textureName = null, bool returnNullWhenMissing = false);
}
