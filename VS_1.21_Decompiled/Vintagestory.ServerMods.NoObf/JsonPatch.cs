using System;
using Newtonsoft.Json;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class JsonPatch
{
	[DocumentAsJson]
	public EnumJsonPatchOp Op;

	[DocumentAsJson]
	public AssetLocation File;

	[DocumentAsJson]
	public string FromPath;

	[DocumentAsJson]
	public string Path;

	[DocumentAsJson]
	public PatchModDependence[] DependsOn;

	[DocumentAsJson]
	public bool Enabled = true;

	[DocumentAsJson]
	public EnumAppSide? Side = EnumAppSide.Universal;

	[DocumentAsJson]
	public PatchCondition Condition;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Value;

	[Obsolete("Use Side instead")]
	[DocumentAsJson]
	public EnumAppSide? SideType
	{
		get
		{
			return Side;
		}
		set
		{
			Side = value;
		}
	}
}
