using System;
using System.ComponentModel;
using ProtoBuf;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[DocumentAsJson]
[TypeConverter(typeof(StringAssetLocationConverter))]
[ProtoContract]
public class AssetLocation : IEquatable<AssetLocation>, IComparable<AssetLocation>
{
	public const char LocationSeparator = ':';

	[ProtoMember(1)]
	private string domain;

	[ProtoMember(2)]
	private string path;

	public string Domain
	{
		get
		{
			return domain ?? "game";
		}
		set
		{
			domain = ((value == null) ? null : string.Intern(value.ToLowerInvariant()));
		}
	}

	public string Path
	{
		get
		{
			return path;
		}
		set
		{
			path = value;
		}
	}

	public bool IsWildCard
	{
		get
		{
			if (Path.IndexOf('*') < 0)
			{
				return Path[0] == '@';
			}
			return true;
		}
	}

	public bool EndsWithWildCard
	{
		get
		{
			if (path.Length > 1)
			{
				return path[path.Length - 1] == '*';
			}
			return false;
		}
	}

	public bool Valid
	{
		get
		{
			string text = domain;
			int num;
			if ((text == null || text.Length != 0) && path.Length != 0)
			{
				string text2 = domain;
				if ((text2 == null || !text2.Contains('/')) && path[0] != '/' && path[path.Length - 1] != '/')
				{
					num = (path.Contains("//") ? 1 : 0);
					goto IL_0079;
				}
			}
			num = 1;
			goto IL_0079;
			IL_0079:
			return num == 0;
		}
	}

	public AssetCategory Category => AssetCategory.FromCode(FirstPathPart());

	public AssetLocation()
	{
	}

	public AssetLocation(string domainAndPath)
	{
		ResolveToDomainAndPath(domainAndPath, out domain, out path);
	}

	private static void ResolveToDomainAndPath(string domainAndPath, out string domain, out string path)
	{
		domainAndPath = domainAndPath.ToLowerInvariant();
		int num = domainAndPath.IndexOf(':');
		if (num == -1)
		{
			domain = null;
			path = string.Intern(domainAndPath);
		}
		else
		{
			domain = string.Intern(domainAndPath.Substring(0, num));
			path = string.Intern(domainAndPath.Substring(num + 1));
		}
	}

	public AssetLocation(string domain, string path)
	{
		this.domain = ((domain == null) ? null : string.Intern(domain));
		this.path = path;
	}

	public static AssetLocation CreateOrNull(string domainAndPath)
	{
		if (domainAndPath.Length == 0)
		{
			return null;
		}
		return new AssetLocation(domainAndPath);
	}

	public static AssetLocation Create(string domainAndPath, string defaultDomain = "game")
	{
		if (!domainAndPath.Contains(':'))
		{
			return new AssetLocation(defaultDomain, domainAndPath.ToLowerInvariant().DeDuplicate());
		}
		return new AssetLocation(domainAndPath);
	}

	public virtual bool IsChild(AssetLocation Location)
	{
		if (Location.Domain.Equals(Domain))
		{
			return Location.path.StartsWithFast(path);
		}
		return false;
	}

	public virtual bool BeginsWith(string domain, string partialPath)
	{
		if (path.StartsWithFast(partialPath))
		{
			return domain?.Equals(Domain) ?? true;
		}
		return false;
	}

	internal virtual bool BeginsWith(string domain, string partialPath, int offset)
	{
		if (path.StartsWithFast(partialPath, offset))
		{
			return domain?.Equals(Domain) ?? true;
		}
		return false;
	}

	public bool PathStartsWith(string partialPath)
	{
		return path.StartsWithOrdinal(partialPath);
	}

	public string ToShortString()
	{
		if (domain == null || domain.Equals("game"))
		{
			return path;
		}
		return ToString();
	}

	public string ShortDomain()
	{
		if (domain != null && !domain.Equals("game"))
		{
			return domain;
		}
		return "";
	}

	public string FirstPathPart(int posFromLeft = 0)
	{
		return path.Split('/')[posFromLeft];
	}

	public string FirstCodePart()
	{
		int num = path.IndexOf('-');
		if (num >= 0)
		{
			return path.Substring(0, num);
		}
		return path;
	}

	public string SecondCodePart()
	{
		int num = path.IndexOf('-') + 1;
		int num2 = ((num <= 0) ? (-1) : path.IndexOf('-', num));
		if (num2 >= 0)
		{
			return path.Substring(num, num2 - num);
		}
		return path;
	}

	public string CodePartsAfterSecond()
	{
		int num = path.IndexOf('-') + 1;
		int num2 = ((num <= 0) ? (-1) : path.IndexOf('-', num));
		if (num2 >= 0)
		{
			return path.Substring(num2 + 1);
		}
		return path;
	}

	public AssetLocation WithPathPrefix(string prefix)
	{
		path = prefix + path;
		return this;
	}

	public AssetLocation WithPathPrefixOnce(string prefix)
	{
		if (!path.StartsWithFast(prefix))
		{
			path = prefix + path;
		}
		return this;
	}

	public AssetLocation WithLocationPrefixOnce(AssetLocation prefix)
	{
		Domain = prefix.Domain;
		return WithPathPrefixOnce(prefix.Path);
	}

	public AssetLocation WithPathAppendix(string appendix)
	{
		path += appendix;
		return this;
	}

	public AssetLocation WithoutPathAppendix(string appendix)
	{
		if (path.EndsWithOrdinal(appendix))
		{
			path = path.Substring(0, path.Length - appendix.Length);
		}
		return this;
	}

	public AssetLocation WithPathAppendixOnce(string appendix)
	{
		if (!path.EndsWithOrdinal(appendix))
		{
			path += appendix;
		}
		return this;
	}

	public virtual bool HasDomain()
	{
		return domain != null;
	}

	public virtual string GetName()
	{
		int num = Path.LastIndexOf('/');
		return Path.Substring(num + 1);
	}

	public virtual void RemoveEnding()
	{
		path = path.Substring(0, path.LastIndexOf('.'));
	}

	public string PathOmittingPrefixAndSuffix(string prefix, string suffix)
	{
		int num = (path.EndsWithOrdinal(suffix) ? (path.Length - suffix.Length) : path.Length);
		string text = path;
		int length = prefix.Length;
		return text.Substring(length, num - length);
	}

	public string EndVariant()
	{
		int num = path.LastIndexOf('-');
		if (num < 0)
		{
			return "";
		}
		return path.Substring(num + 1);
	}

	public virtual AssetLocation Clone()
	{
		return new AssetLocation(domain, path);
	}

	public virtual AssetLocation PermanentClone()
	{
		return new AssetLocation(domain.DeDuplicate(), path.DeDuplicate());
	}

	public virtual AssetLocation CloneWithoutPrefixAndEnding(int prefixLength)
	{
		int num = path.LastIndexOf('.');
		string text = ((num >= prefixLength) ? path.Substring(prefixLength, num - prefixLength) : path.Substring(prefixLength));
		return new AssetLocation(domain, text);
	}

	public virtual AssetLocation CopyWithPath(string path)
	{
		return new AssetLocation(domain, path);
	}

	public virtual AssetLocation CopyWithPathPrefixAndAppendix(string prefix, string appendix)
	{
		return new AssetLocation(domain, prefix + path + appendix);
	}

	public virtual AssetLocation CopyWithPathPrefixAndAppendixOnce(string prefix, string appendix)
	{
		if (path.StartsWithFast(prefix))
		{
			return new AssetLocation(domain, path.EndsWithOrdinal(appendix) ? path : (path + appendix));
		}
		return new AssetLocation(domain, path.EndsWithOrdinal(appendix) ? (prefix + path) : (prefix + path + appendix));
	}

	public virtual AssetLocation WithPath(string path)
	{
		Path = path;
		return this;
	}

	public virtual AssetLocation WithFilename(string filename)
	{
		Path = Path.Substring(0, Path.LastIndexOf('/') + 1) + filename;
		return this;
	}

	public static AssetLocation[] toLocations(string[] names)
	{
		AssetLocation[] array = new AssetLocation[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new AssetLocation(names[i]);
		}
		return array;
	}

	public override int GetHashCode()
	{
		return Domain.GetHashCode() ^ path.GetHashCode();
	}

	public bool Equals(AssetLocation other)
	{
		if (other == null)
		{
			return false;
		}
		if (path.EqualsFast(other.path))
		{
			return Domain.Equals(other.Domain);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as AssetLocation);
	}

	public static bool operator ==(AssetLocation left, AssetLocation right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(AssetLocation left, AssetLocation right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return Domain + ":" + Path;
	}

	public int CompareTo(AssetLocation other)
	{
		return ToString().CompareOrdinal(other.ToString());
	}

	public bool WildCardMatch(AssetLocation other, string pathAsRegex)
	{
		if (Domain == other.Domain)
		{
			return WildcardUtil.fastMatch(path, other.path, pathAsRegex);
		}
		return false;
	}

	public static implicit operator string(AssetLocation loc)
	{
		return loc?.ToString();
	}

	public static implicit operator AssetLocation(string code)
	{
		return Create(code);
	}
}
