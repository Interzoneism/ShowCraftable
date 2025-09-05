using System;

namespace Vintagestory.API.Common;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ModDependencyAttribute : Attribute
{
	public string ModID { get; }

	public string Version { get; }

	public ModDependencyAttribute(string modID, string version = "")
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
}
