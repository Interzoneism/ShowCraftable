using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class MacroBase : IMacroBase
{
	[JsonProperty]
	public int Index { get; set; }

	[JsonProperty]
	public string Code { get; set; }

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public string[] Commands { get; set; }

	[JsonProperty]
	public KeyCombination KeyCombination { get; set; }

	public LoadedTexture iconTexture { get; set; }

	public virtual void GenTexture(ICoreClientAPI capi, int size)
	{
	}

	public MacroBase()
	{
		Code = "";
		Name = "";
		Commands = Array.Empty<string>();
		KeyCombination = null;
	}
}
