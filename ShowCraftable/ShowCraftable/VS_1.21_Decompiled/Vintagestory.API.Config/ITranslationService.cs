using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.Config;

public interface ITranslationService
{
	EnumLinebreakBehavior LineBreakBehavior { get; }

	string LanguageCode { get; }

	void Load(bool lazyload = false);

	void PreLoad(string assetsPath, bool lazyLoad = false);

	void PreLoadModWorldConfig(string modPath, string modDomain, bool lazyLoad = false);

	string GetIfExists(string key, params object[] args);

	string Get(string key, params object[] args);

	IDictionary<string, string> GetAllEntries();

	string GetUnformatted(string key);

	string GetMatching(string key, params object[] args);

	string GetMatchingIfExists(string key, params object[] args);

	bool HasTranslation(string key, bool findWildcarded = true);

	bool HasTranslation(string key, bool findWildcarded, bool logErrors);

	void UseAssetManager(IAssetManager assetManager);

	void InitialiseSearch();

	void Invalidate();
}
