using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class PathOrigin : IAssetOrigin
{
	protected string fullPath;

	protected string domain;

	public string OriginPath => fullPath;

	public string Domain => domain;

	public PathOrigin(string domain, string fullPath)
		: this(domain, fullPath, null)
	{
	}

	public PathOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
	{
		this.domain = domain.ToLowerInvariant();
		this.fullPath = fullPath;
		if (!this.fullPath.EndsWith(Path.DirectorySeparatorChar))
		{
			this.fullPath += Path.DirectorySeparatorChar;
		}
		if (pathForReservedCharsCheck != null)
		{
			CheckForReservedCharacters(domain, pathForReservedCharsCheck);
		}
	}

	public void LoadAsset(IAsset asset)
	{
		if (asset.Location.Domain != domain)
		{
			throw new Exception("Invalid LoadAsset call or invalid asset instance. Trying to load [" + asset?.ToString() + "] from domain " + domain + " is bound to fail.");
		}
		string path = fullPath + asset.Location.Path.Replace('/', Path.DirectorySeparatorChar);
		if (!File.Exists(path))
		{
			throw new Exception(string.Concat("Requested asset [", asset.Location, "] could not be found"));
		}
		asset.Data = File.ReadAllBytes(path);
	}

	public bool TryLoadAsset(IAsset asset)
	{
		if (asset.Location.Domain != domain)
		{
			return false;
		}
		string path = fullPath + (asset as Asset).FilePath.Replace('/', Path.DirectorySeparatorChar);
		if (!File.Exists(path))
		{
			return false;
		}
		asset.Data = File.ReadAllBytes(path);
		return true;
	}

	public List<IAsset> GetAssets(AssetCategory Category, bool shouldLoad = true)
	{
		List<IAsset> list = new List<IAsset>();
		ScanAssetFolderRecursive(fullPath + Category.Code, list, shouldLoad);
		return list;
	}

	public List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true)
	{
		List<IAsset> list = new List<IAsset>();
		ScanAssetFolderRecursive(fullPath + baseLocation.Path, list, shouldLoad);
		return list;
	}

	private void ScanAssetFolderRecursive(string currentPath, List<IAsset> list, bool shouldLoad)
	{
		if (!Directory.Exists(currentPath))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(currentPath);
		foreach (string currentPath2 in directories)
		{
			ScanAssetFolderRecursive(currentPath2, list, shouldLoad);
		}
		directories = Directory.GetFiles(currentPath);
		foreach (string text in directories)
		{
			FileInfo fileInfo = new FileInfo(text);
			if (!fileInfo.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !fileInfo.Name.EndsWithOrdinal(".psd") && !fileInfo.Name.StartsWith('.'))
			{
				string text2 = text.Substring(fullPath.Length).Replace(Path.DirectorySeparatorChar, '/');
				AssetLocation location = new AssetLocation(domain, text2.ToLowerInvariant());
				Asset asset = new Asset(shouldLoad ? File.ReadAllBytes(text) : null, location, this);
				asset.FilePath = text2;
				list.Add(asset);
			}
		}
	}

	public bool IsAllowedToAffectGameplay()
	{
		return true;
	}

	public string GetDefaultDomain()
	{
		return domain;
	}

	public virtual void CheckForReservedCharacters(string domain, string path)
	{
		path = ((path == null) ? OriginPath : Path.Combine(OriginPath, path));
		if (!Directory.Exists(path))
		{
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		int num = directoryInfo.FullName.Length - 9;
		foreach (FileInfo item in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
		{
			string filepath = item.FullName.Substring(num + 1);
			FolderOrigin.CheckForReservedCharacters(domain, filepath);
		}
	}
}
