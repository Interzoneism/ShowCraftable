using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskIdleConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public bool StopWhenTargetDetected;

	[JsonProperty]
	private string[]? allowedBlockBelowTags = Array.Empty<string>();

	[JsonProperty]
	private string[]? skipBlockBelowTags = Array.Empty<string>();

	[JsonProperty]
	public AssetLocation? AllowedBlockBelowCode;

	[JsonProperty]
	public bool CheckForSolidUpSide = true;

	[JsonProperty]
	public int MinBlockInsideReplaceable = 6000;

	[JsonProperty]
	public float ChanceToCheckTarget = 0.3f;

	public BlockTagRule AllowedBlockBelowTags;

	public BlockTagRule SkipBlockBelowTags;

	public bool IgnoreBlockCodeAndTags
	{
		get
		{
			if (AllowedBlockBelowTags == BlockTagRule.Empty && SkipBlockBelowTags == BlockTagRule.Empty)
			{
				return AllowedBlockBelowCode == null;
			}
			return false;
		}
	}

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (allowedBlockBelowTags != null)
		{
			AllowedBlockBelowTags = new BlockTagRule(entity.Api, allowedBlockBelowTags);
			allowedBlockBelowTags = null;
		}
		if (skipBlockBelowTags != null)
		{
			SkipBlockBelowTags = new BlockTagRule(entity.Api, skipBlockBelowTags);
			skipBlockBelowTags = null;
		}
	}
}
