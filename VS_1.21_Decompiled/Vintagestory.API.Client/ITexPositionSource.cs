using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface ITexPositionSource
{
	TextureAtlasPosition this[string textureCode] { get; }

	Size2i AtlasSize { get; }
}
