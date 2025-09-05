using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IAssetOrigin
{
	string OriginPath { get; }

	void LoadAsset(IAsset asset);

	bool TryLoadAsset(IAsset asset);

	List<IAsset> GetAssets(AssetCategory category, bool shouldLoad = true);

	List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true);

	bool IsAllowedToAffectGameplay();
}
