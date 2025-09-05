using System;

namespace Vintagestory.API.Common;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ModInfoAttribute : Attribute
{
	public string Name { get; }

	public string IconPath { get; set; }

	public string ModID { get; }

	public string Version { get; set; }

	public bool CoreMod { get; set; }

	public string NetworkVersion { get; set; }

	public string Description { get; set; }

	public string Website { get; set; }

	public string[] Authors { get; set; }

	public string[] Contributors { get; set; }

	public string Side { get; set; } = EnumAppSide.Universal.ToString();

	public bool RequiredOnClient { get; set; } = true;

	public bool RequiredOnServer { get; set; } = true;

	public string WorldConfig { get; set; }

	public ModInfoAttribute(string name, string modID)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (modID == null)
		{
			throw new ArgumentNullException("modID");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("name can't be empty", "name");
		}
		if (!ModInfo.IsValidModID(modID))
		{
			throw new ArgumentException("'" + modID + "' is not a valid mod ID. Please use only lowercase letters and numbers.", "modID");
		}
		Name = name;
		ModID = modID;
	}

	public ModInfoAttribute(string name)
		: this(name, ModInfo.ToModID(name))
	{
	}
}
