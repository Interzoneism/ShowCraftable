using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IAssetManager
{
	Dictionary<AssetLocation, IAsset> AllAssets { get; }

	List<IAssetOrigin> Origins { get; }

	bool Exists(AssetLocation location);

	void Add(AssetLocation path, IAsset asset);

	IAsset Get(AssetLocation Location);

	IAsset TryGet(AssetLocation Location, bool loadAsset = true);

	List<IAsset> GetMany(string pathBegins, string domain = null, bool loadAsset = true);

	List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true);

	Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string pathBegins, string domain = null);

	List<AssetLocation> GetLocations(string pathBegins, string domain = null);

	T Get<T>(AssetLocation location);

	int Reload(AssetLocation baseLocation);

	int Reload(AssetCategory category);

	[Obsolete("Use AddModOrigin")]
	void AddPathOrigin(string domain, string fullPath);

	void AddModOrigin(string domain, string fullPath);

	void AddModOrigin(string domain, string fullPath, string pathForReservedCharsCheck);
}
