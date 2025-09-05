using System.Collections.Generic;

namespace Vintagestory.API.Common;

public abstract class Mod
{
	public EnumModSourceType SourceType { get; internal set; }

	public string SourcePath { get; internal set; }

	public string FileName { get; internal set; }

	public ModInfo Info { get; internal set; }

	public ModWorldConfiguration WorldConfig { get; internal set; }

	public BitmapExternal Icon { get; internal set; }

	public ILogger Logger { get; internal set; }

	public IReadOnlyCollection<ModSystem> Systems { get; internal set; } = new List<ModSystem>(0).AsReadOnly();

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Info?.ModID))
		{
			return "'" + FileName + "'";
		}
		return $"'{FileName}' ({Info.ModID})";
	}
}
