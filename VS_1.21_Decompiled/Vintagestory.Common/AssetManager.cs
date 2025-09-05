using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class AssetManager : IAssetManager
{
	private EnumAppSide side;

	public bool allAssetsLoaded;

	public Dictionary<AssetLocation, IAsset> Assets;

	public Dictionary<AssetLocation, IAsset> RuntimeAssets = new Dictionary<AssetLocation, IAsset>();

	private IDictionary<string, List<IAsset>> assetsByCategory;

	public List<IAssetOrigin> Origins;

	public List<IAssetOrigin> CustomAppOrigins = new List<IAssetOrigin>();

	public List<IAssetOrigin> CustomModOrigins = new List<IAssetOrigin>();

	private string assetsPath;

	public Dictionary<AssetLocation, IAsset> AllAssets => Assets;

	List<IAssetOrigin> IAssetManager.Origins => Origins;

	public AssetManager(string assetsPath, EnumAppSide side)
	{
		this.assetsPath = assetsPath;
		this.side = side;
	}

	public void Add(AssetLocation path, IAsset asset)
	{
		Assets[path] = asset;
		assetsByCategory[path.Category.Code].Add(asset);
		if (!RuntimeAssets.ContainsKey(path))
		{
			RuntimeAssets[path] = asset;
		}
	}

	public int InitAndLoadBaseAssets(ILogger Logger)
	{
		return InitAndLoadBaseAssets(Logger, null);
	}

	public int InitAndLoadBaseAssets(ILogger Logger, string pathForReservedCharsCheck)
	{
		allAssetsLoaded = false;
		Origins = new List<IAssetOrigin>();
		Origins.Add(new GameOrigin(assetsPath, pathForReservedCharsCheck));
		Assets = new Dictionary<AssetLocation, IAsset>();
		assetsByCategory = new FastSmallDictionary<string, List<IAsset>>(AssetCategory.categories.Values.Count + 1);
		int num = 0;
		foreach (AssetCategory value in AssetCategory.categories.Values)
		{
			if ((value.SideType & side) <= (EnumAppSide)0)
			{
				continue;
			}
			Dictionary<AssetLocation, IAsset> assetsDontLoad = GetAssetsDontLoad(value, Origins);
			foreach (IAsset value2 in assetsDontLoad.Values)
			{
				Assets[value2.Location] = value2;
			}
			num += assetsDontLoad.Count;
			assetsByCategory[value.Code] = assetsDontLoad.Values.ToList();
			Logger?.Notification("Found {1} base assets in category {0}", value, assetsDontLoad.Count);
		}
		return num;
	}

	public int AddExternalAssets(ILogger Logger, ModLoader modloader = null)
	{
		List<string> list = new List<string>();
		List<IAssetOrigin> list2 = new List<IAssetOrigin>();
		foreach (IAssetOrigin customAppOrigin in CustomAppOrigins)
		{
			Origins.Add(customAppOrigin);
			list2.Add(customAppOrigin);
			list.Add("arg@" + customAppOrigin.OriginPath);
		}
		foreach (IAssetOrigin customModOrigin in CustomModOrigins)
		{
			Origins.Add(customModOrigin);
			list2.Add(customModOrigin);
			list.Add("modorigin@" + customModOrigin.OriginPath);
		}
		if (modloader != null)
		{
			foreach (KeyValuePair<string, IAssetOrigin> contentArchive in modloader.GetContentArchives())
			{
				list2.Add(contentArchive.Value);
				Origins.Add(contentArchive.Value);
				list.Add("mod@" + contentArchive.Key);
			}
			foreach (KeyValuePair<string, IAssetOrigin> themeArchive in modloader.GetThemeArchives())
			{
				list2.Add(themeArchive.Value);
				Origins.Add(themeArchive.Value);
				list.Add("themepack@" + themeArchive.Key);
			}
		}
		if (list.Count > 0)
		{
			Logger.Notification("External Origins in load order: {0}", string.Join(", ", list));
		}
		int num = 0;
		int num2 = 0;
		foreach (AssetCategory value2 in AssetCategory.categories.Values)
		{
			if ((value2.SideType & side) > (EnumAppSide)0)
			{
				Dictionary<AssetLocation, IAsset> assetsDontLoad = GetAssetsDontLoad(value2, list2);
				foreach (IAsset value3 in assetsDontLoad.Values)
				{
					Assets[value3.Location] = value3;
				}
				num2 += assetsDontLoad.Count;
				if (!assetsByCategory.TryGetValue(value2.Code, out var value))
				{
					value = (assetsByCategory[value2.Code] = new List<IAsset>());
				}
				value.AddRange(assetsDontLoad.Values);
				Logger.Notification("Found {1} external assets in category {0}", value2, assetsDontLoad.Count);
			}
			num++;
		}
		allAssetsLoaded = true;
		return num2;
	}

	public void UnloadExternalAssets(ILogger logger)
	{
		allAssetsLoaded = false;
		InitAndLoadBaseAssets(null);
	}

	public void UnloadAssets(AssetCategory category)
	{
		foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
		{
			if (asset.Key.Category == category)
			{
				asset.Value.Data = null;
			}
		}
	}

	public void UnloadAssets()
	{
		foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
		{
			asset.Value.Data = null;
		}
	}

	public void UnloadUnpatchedAssets()
	{
		foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
		{
			if (!asset.Value.IsPatched)
			{
				asset.Value.Data = null;
			}
		}
	}

	public List<AssetLocation> GetLocations(string fullPathBeginsWith, string domain = null)
	{
		List<AssetLocation> list = new List<AssetLocation>();
		foreach (IAsset value in Assets.Values)
		{
			if (value.Location.BeginsWith(domain, fullPathBeginsWith))
			{
				list.Add(value.Location);
			}
		}
		return list;
	}

	public bool Exists(AssetLocation location)
	{
		return Assets.ContainsKey(location);
	}

	public IAsset TryGet(string Path, bool loadAsset = true)
	{
		return TryGet(new AssetLocation(Path), loadAsset);
	}

	public IAsset TryGet(AssetLocation Location, bool loadAsset = true)
	{
		if (!allAssetsLoaded)
		{
			throw new Exception("Coding error: Mods must not get assets before AssetsLoaded stage - do not load assets in a Start() method!");
		}
		return TryGet_BaseAssets(Location, loadAsset);
	}

	public IAsset TryGet_BaseAssets(string Path, bool loadAsset = true)
	{
		return TryGet_BaseAssets(new AssetLocation(Path), loadAsset);
	}

	public IAsset TryGet_BaseAssets(AssetLocation Location, bool loadAsset = true)
	{
		if (!Assets.TryGetValue(Location, out var value))
		{
			return null;
		}
		if (!value.IsLoaded() && loadAsset)
		{
			value.Origin.TryLoadAsset(value);
		}
		return value;
	}

	public IAsset Get(string Path)
	{
		return Get(new AssetLocation(Path));
	}

	public IAsset Get(AssetLocation Location)
	{
		return TryGet_BaseAssets(Location) ?? throw new Exception(string.Concat("Asset ", Location, " could not be found"));
	}

	public T Get<T>(AssetLocation Location)
	{
		return Get(Location).ToObject<T>();
	}

	public List<IAsset> GetMany(AssetCategory category, bool loadAsset = true)
	{
		List<IAsset> list = new List<IAsset>();
		if (assetsByCategory.TryGetValue(category.Code, out var value))
		{
			foreach (IAsset item in value)
			{
				if (item.Location.Category == category)
				{
					if (!item.IsLoaded() && loadAsset)
					{
						item.Origin.LoadAsset(item);
					}
					list.Add(item);
				}
			}
		}
		return list;
	}

	public List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true)
	{
		List<IAsset> list = new List<IAsset>();
		if (assetsByCategory.TryGetValue(categoryCode, out var value))
		{
			int offset = categoryCode.Length + 1;
			foreach (IAsset item in value)
			{
				if (item.Location.BeginsWith(domain, pathBegins, offset))
				{
					if (loadAsset && !item.IsLoaded())
					{
						item.Origin.LoadAsset(item);
					}
					list.Add(item);
				}
			}
		}
		return list;
	}

	public List<IAsset> GetMany(string partialPath, string domain = null, bool loadAsset = true)
	{
		List<IAsset> list = new List<IAsset>();
		foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
		{
			IAsset value = asset.Value;
			if (asset.Key.BeginsWith(domain, partialPath))
			{
				if (loadAsset && !value.IsLoaded())
				{
					value.Origin.LoadAsset(value);
				}
				list.Add(value);
			}
		}
		return list;
	}

	public Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string fullPath, string domain = null)
	{
		//IL_003a: Expected O, but got Unknown
		Dictionary<AssetLocation, T> dictionary = new Dictionary<AssetLocation, T>();
		foreach (Asset item in GetMany(fullPath, domain))
		{
			try
			{
				dictionary.Add(item.Location, item.ToObject<T>());
			}
			catch (JsonReaderException ex)
			{
				JsonReaderException ex2 = ex;
				logger.Error("Syntax error in json file '{0}': {1}", item, ((Exception)(object)ex2).Message);
			}
		}
		return dictionary;
	}

	internal Dictionary<AssetLocation, IAsset> GetAssetsDontLoad(AssetCategory category, List<IAssetOrigin> fromOrigins)
	{
		Dictionary<AssetLocation, IAsset> dictionary = new Dictionary<AssetLocation, IAsset>();
		foreach (IAssetOrigin fromOrigin in fromOrigins)
		{
			if (!fromOrigin.IsAllowedToAffectGameplay() && category.AffectsGameplay)
			{
				continue;
			}
			foreach (IAsset asset in fromOrigin.GetAssets(category, shouldLoad: false))
			{
				dictionary[asset.Location] = asset;
			}
		}
		return dictionary;
	}

	public int Reload(AssetLocation location)
	{
		Assets.RemoveAllByKey((AssetLocation x) => location == null || location.IsChild(x));
		int num = 0;
		List<IAsset> value = null;
		if (location != null)
		{
			int num2 = location.Path.IndexOf('/');
			if (num2 > 0)
			{
				string key = location.Path.Substring(0, num2);
				if (assetsByCategory.TryGetValue(key, out value))
				{
					value.RemoveAll((IAsset a) => location.IsChild(a.Location));
				}
			}
		}
		foreach (IAssetOrigin origin in Origins)
		{
			List<IAsset> assets = origin.GetAssets(location);
			foreach (IAsset item in assets)
			{
				Assets[item.Location] = item;
				num++;
			}
			value?.AddRange(assets);
		}
		return num;
	}

	public int Reload(AssetCategory category)
	{
		Assets.RemoveAllByKey((AssetLocation x) => category == null || x.Category == category);
		int num = 0;
		if (!assetsByCategory.TryGetValue(category.Code, out var value))
		{
			value = (assetsByCategory[category.Code] = new List<IAsset>());
		}
		else
		{
			value.Clear();
		}
		foreach (IAssetOrigin origin in Origins)
		{
			List<IAsset> assets = origin.GetAssets(category);
			foreach (IAsset item in assets)
			{
				Assets[item.Location] = item;
				num++;
			}
			value.AddRange(assets);
		}
		foreach (KeyValuePair<AssetLocation, IAsset> runtimeAsset in RuntimeAssets)
		{
			if (runtimeAsset.Key.Category == category)
			{
				Add(runtimeAsset.Key, runtimeAsset.Value);
			}
		}
		return num;
	}

	public AssetCategory GetCategoryFromFullPath(string fullpath)
	{
		return AssetCategory.FromCode(fullpath.Split('/')[0]);
	}

	public void AddPathOrigin(string domain, string fullPath)
	{
		AddModOrigin(domain, fullPath, null);
	}

	public void AddModOrigin(string domain, string fullPath)
	{
		AddModOrigin(domain, fullPath, null);
	}

	public void AddModOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
	{
		for (int i = 0; i < CustomModOrigins.Count; i++)
		{
			IAssetOrigin assetOrigin = CustomModOrigins[i];
			if ((assetOrigin as PathOrigin)?.OriginPath == fullPath && (assetOrigin as PathOrigin)?.Domain == domain)
			{
				return;
			}
		}
		CustomModOrigins.Add(new PathOrigin(domain, fullPath, pathForReservedCharsCheck));
	}
}
