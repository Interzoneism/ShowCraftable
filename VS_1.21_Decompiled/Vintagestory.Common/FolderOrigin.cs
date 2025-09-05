using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class FolderOrigin : IAssetOrigin
{
	protected readonly Dictionary<AssetLocation, string> _fileLookup = new Dictionary<AssetLocation, string>();

	public string OriginPath { get; protected set; }

	public FolderOrigin(string fullPath)
		: this(fullPath, null)
	{
	}

	public FolderOrigin(string fullPath, string pathForReservedCharsCheck)
	{
		OriginPath = Path.Combine(fullPath, "assets");
		string text = Path.Combine(fullPath, ".ignore");
		IgnoreFile ignoreFile = (File.Exists(text) ? new IgnoreFile(text, fullPath) : null);
		DirectoryInfo directoryInfo = new DirectoryInfo(OriginPath);
		int length = directoryInfo.FullName.Length;
		if (!Directory.Exists(OriginPath))
		{
			return;
		}
		foreach (FileInfo item in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
		{
			if (item.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) || item.Extension == ".psd" || item.Name[0] == '.' || (ignoreFile != null && !ignoreFile.Available(item.FullName)))
			{
				continue;
			}
			string text2 = item.FullName.Substring(length + 1);
			if (Path.DirectorySeparatorChar == '\\')
			{
				text2 = text2.Replace('\\', '/');
			}
			int num = text2.IndexOf('/');
			if (num >= 0)
			{
				string domain = text2.Substring(0, num);
				text2 = text2.Substring(num + 1);
				if (pathForReservedCharsCheck != null && text2.StartsWith(pathForReservedCharsCheck))
				{
					CheckForReservedCharacters(domain, text2);
				}
				AssetLocation key = new AssetLocation(domain, text2);
				_fileLookup.Add(key, item.FullName);
			}
		}
	}

	public void LoadAsset(IAsset asset)
	{
		if (!_fileLookup.TryGetValue(asset.Location, out var value))
		{
			throw new Exception("Requested asset [" + asset?.ToString() + "] could not be found");
		}
		asset.Data = File.ReadAllBytes(value);
	}

	public bool TryLoadAsset(IAsset asset)
	{
		if (!_fileLookup.TryGetValue(asset.Location, out var value))
		{
			return false;
		}
		asset.Data = File.ReadAllBytes(value);
		return true;
	}

	public List<IAsset> GetAssets(AssetCategory Category, bool shouldLoad = true)
	{
		List<IAsset> list = new List<IAsset>();
		if (!Directory.Exists(OriginPath))
		{
			return list;
		}
		string[] directories = Directory.GetDirectories(OriginPath);
		foreach (string text in directories)
		{
			ScanAssetFolderRecursive(text.Substring(OriginPath.Length + 1).ToLowerInvariant(), text + Path.DirectorySeparatorChar + Category.Code, list, shouldLoad);
		}
		return list;
	}

	public List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true)
	{
		List<IAsset> list = new List<IAsset>();
		ScanAssetFolderRecursive(baseLocation.Domain, OriginPath + Path.DirectorySeparatorChar + baseLocation.Domain + Path.DirectorySeparatorChar + baseLocation.Path.Replace('/', Path.DirectorySeparatorChar), list, shouldLoad);
		return list;
	}

	private void ScanAssetFolderRecursive(string domain, string currentPath, List<IAsset> list, bool shouldLoad)
	{
		if (!Directory.Exists(currentPath))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(currentPath);
		foreach (string currentPath2 in directories)
		{
			ScanAssetFolderRecursive(domain, currentPath2, list, shouldLoad);
		}
		directories = Directory.GetFiles(currentPath);
		foreach (string text in directories)
		{
			FileInfo fileInfo = new FileInfo(text);
			if (!fileInfo.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !fileInfo.Name.EndsWithOrdinal(".psd") && !fileInfo.Name.StartsWith('.'))
			{
				AssetLocation location = new AssetLocation(domain, text.Substring(OriginPath.Length + domain.Length + 2).Replace(Path.DirectorySeparatorChar, '/'));
				list.Add(new Asset(shouldLoad ? File.ReadAllBytes(text) : null, location, this));
			}
		}
	}

	public virtual bool IsAllowedToAffectGameplay()
	{
		return true;
	}

	public static void CheckForReservedCharacters(string domain, string filepath)
	{
		string[] reservedCharacterSequences = GlobalConstants.ReservedCharacterSequences;
		foreach (string text in reservedCharacterSequences)
		{
			if (filepath.Contains(text))
			{
				throw new FormatException("Reserved characters " + text + " not allowed in filename:- " + domain + ":" + filepath);
			}
		}
	}
}
