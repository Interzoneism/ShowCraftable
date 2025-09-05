using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IColorPresets
{
	void Initialize(IAsset asset);

	void OnUpdateSetting();

	int GetColor(string key);
}
