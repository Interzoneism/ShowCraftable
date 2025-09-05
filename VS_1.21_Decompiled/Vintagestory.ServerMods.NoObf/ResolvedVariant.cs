using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

public class ResolvedVariant
{
	public OrderedDictionary<string, string> CodeParts = new OrderedDictionary<string, string>();

	public AssetLocation Code;

	public void ResolveCode(AssetLocation baseCode)
	{
		Code = baseCode.Clone();
		foreach (string value in CodeParts.Values)
		{
			if (value.Length > 0)
			{
				AssetLocation code = Code;
				code.Path = code.Path + "-" + value;
			}
		}
	}

	public void AddCodePart(string key, string val)
	{
		CodeParts.Add(string.Intern(key), string.Intern(val));
	}
}
