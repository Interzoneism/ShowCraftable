using System;

namespace Vintagestory.API.Common;

public class ModDependency
{
	public string ModID { get; }

	public string Version { get; }

	public ModDependency(string modID, string version = "")
	{
		if (modID == null)
		{
			throw new ArgumentNullException("modID");
		}
		if (!ModInfo.IsValidModID(modID))
		{
			throw new ArgumentException("'" + modID + "' is not a valid mod ID. Please use only lowercase letters and numbers.", "modID");
		}
		ModID = modID;
		Version = version ?? "";
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Version))
		{
			return ModID;
		}
		return ModID + "@" + Version;
	}
}
