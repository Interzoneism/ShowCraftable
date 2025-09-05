using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common.Entities;

[DocumentAsJson]
public class BaseSpawnConditions : ClimateSpawnCondition
{
	[DocumentAsJson]
	public string Group;

	[DocumentAsJson]
	public int MinLightLevel;

	[DocumentAsJson]
	public int MaxLightLevel = 32;

	[DocumentAsJson]
	public EnumLightLevelType LightLevelType = EnumLightLevelType.MaxLight;

	[DocumentAsJson]
	public NatFloat HerdSize = NatFloat.createUniform(1f, 0f);

	[DocumentAsJson]
	public AssetLocation[] Companions = Array.Empty<AssetLocation>();

	[DocumentAsJson]
	public AssetLocation[] InsideBlockCodes = new AssetLocation[1]
	{
		new AssetLocation("air")
	};

	[DocumentAsJson]
	public bool RequireSolidGround = true;

	[DocumentAsJson]
	public bool TryOnlySurface;

	[DocumentAsJson]
	public EnumGetClimateMode ClimateValueMode;

	protected HashSet<Block> InsideBlockCodesResolved;

	protected string[] InsideBlockCodesBeginsWith;

	protected string[] InsideBlockCodesExact;

	protected string InsideBlockFirstLetters = "";

	[DocumentAsJson]
	[Obsolete("Use HerdSize instead")]
	public NatFloat GroupSize
	{
		get
		{
			return HerdSize;
		}
		set
		{
			HerdSize = value;
		}
	}

	public bool CanSpawnInside(Block testBlock)
	{
		string path = testBlock.Code.Path;
		if (path.Length < 1)
		{
			return false;
		}
		if (InsideBlockFirstLetters.IndexOf(path[0]) < 0)
		{
			return false;
		}
		if (PathMatchesInsideBlockCodes(path))
		{
			return InsideBlockCodesResolved.Contains(testBlock);
		}
		return false;
	}

	private bool PathMatchesInsideBlockCodes(string testPath)
	{
		for (int i = 0; i < InsideBlockCodesExact.Length; i++)
		{
			if (testPath == InsideBlockCodesExact[i])
			{
				return true;
			}
		}
		for (int j = 0; j < InsideBlockCodesBeginsWith.Length; j++)
		{
			if (testPath.StartsWithOrdinal(InsideBlockCodesBeginsWith[j]))
			{
				return true;
			}
		}
		return false;
	}

	public void Initialise(IServerWorldAccessor server, string entityName, Dictionary<AssetLocation, Block[]> searchCache)
	{
		if (InsideBlockCodes == null || InsideBlockCodes.Length == 0)
		{
			return;
		}
		bool flag = false;
		AssetLocation[] insideBlockCodes = InsideBlockCodes;
		foreach (AssetLocation assetLocation in insideBlockCodes)
		{
			if (!searchCache.TryGetValue(assetLocation, out var value))
			{
				value = (searchCache[assetLocation] = server.SearchBlocks(assetLocation));
			}
			Block[] array2 = value;
			foreach (Block item in array2)
			{
				if (InsideBlockCodesResolved == null)
				{
					InsideBlockCodesResolved = new HashSet<Block>();
				}
				InsideBlockCodesResolved.Add(item);
			}
			flag |= value.Length != 0;
		}
		if (!flag)
		{
			server.Logger.Warning("Entity with code {0} has defined InsideBlockCodes for its spawn conditions, but none of these blocks exists, entity is unlikely to spawn.", entityName);
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		AssetLocation[] insideBlockCodes2 = InsideBlockCodes;
		for (int k = 0; k < insideBlockCodes2.Length; k++)
		{
			string path = insideBlockCodes2[k].Path;
			if (path.EndsWith('*'))
			{
				list2.Add(path.Substring(0, path.Length - 1));
			}
			else
			{
				list.Add(path);
			}
		}
		InsideBlockCodesBeginsWith = list2.ToArray();
		InsideBlockCodesExact = new string[list.Count];
		int num = 0;
		foreach (string item2 in list)
		{
			if (item2.Length != 0)
			{
				InsideBlockCodesExact[num++] = item2;
				char c = item2[0];
				if (InsideBlockFirstLetters.IndexOf(c) < 0)
				{
					InsideBlockFirstLetters += c;
				}
			}
		}
		string[] insideBlockCodesBeginsWith = InsideBlockCodesBeginsWith;
		foreach (string text in insideBlockCodesBeginsWith)
		{
			if (text.Length != 0)
			{
				char c2 = text[0];
				if (InsideBlockFirstLetters.IndexOf(c2) < 0)
				{
					InsideBlockFirstLetters += c2;
				}
			}
		}
	}
}
