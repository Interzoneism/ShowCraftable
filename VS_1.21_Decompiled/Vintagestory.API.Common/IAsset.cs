using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public interface IAsset
{
	string Name { get; }

	AssetLocation Location { get; }

	IAssetOrigin Origin { get; set; }

	byte[] Data { get; set; }

	bool IsPatched { get; set; }

	T ToObject<T>(JsonSerializerSettings settings = null);

	string ToText();

	BitmapRef ToBitmap(ICoreClientAPI capi);

	bool IsLoaded();
}
