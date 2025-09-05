using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class LookatBlockAction : EntityActionBase
{
	[JsonProperty]
	private AssetLocation targetBlockCode;

	[JsonProperty]
	private float searchRange;

	public override string Type => "lookatblock";

	public LookatBlockAction()
	{
	}

	public LookatBlockAction(EntityActivitySystem vas, AssetLocation targetBlockCode, float searchRange)
	{
		base.vas = vas;
		this.targetBlockCode = targetBlockCode;
		this.searchRange = searchRange;
	}

	public override void Start(EntityActivity act)
	{
		BlockPos target = getTarget(vas.Entity.Api, vas.Entity.ServerPos.XYZ);
		ExecutionHasFailed = target == null;
		if (target != null)
		{
			Vec3f vec3f = new Vec3f();
			vec3f.Set((float)((double)target.X + 0.5 - vas.Entity.ServerPos.X), (float)((double)target.Y + 0.5 - vas.Entity.ServerPos.Y), (float)((double)target.Z + 0.5 - vas.Entity.ServerPos.Z));
			vas.Entity.ServerPos.Yaw = (float)Math.Atan2(vec3f.X, vec3f.Z);
		}
	}

	private BlockPos getTarget(ICoreAPI api, Vec3d fromPos)
	{
		float num = GameMath.Clamp(searchRange, -10f, 10f);
		BlockPos asBlockPos = fromPos.Clone().Add(0f - num, -1.0, 0f - num).AsBlockPos;
		BlockPos asBlockPos2 = fromPos.Clone().Add(num, 1.0, num).AsBlockPos;
		BlockPos targetPos = null;
		api.World.BlockAccessor.WalkBlocks(asBlockPos, asBlockPos2, delegate(Block block, int x, int y, int z)
		{
			if (!(targetBlockCode == null) && block.WildCardMatch(targetBlockCode))
			{
				targetPos = new BlockPos(x, y, z);
			}
		}, centerOrder: true);
		return targetPos;
	}

	public override string ToString()
	{
		return string.Concat("Look at nearest block ", targetBlockCode, " within ", searchRange.ToString(), " blocks");
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Search Range (capped to 10 blocks)", CairoFont.WhiteDetailText(), elementBounds).AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "searchRange").AddStaticText("Block Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "targetBlockCode");
		singleComposer.GetNumberInput("searchRange").SetValue(searchRange);
		singleComposer.GetTextInput("targetBlockCode").SetValue(targetBlockCode?.ToShortString());
	}

	public override IEntityAction Clone()
	{
		return new LookatBlockAction(vas, targetBlockCode, searchRange);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		searchRange = singleComposer.GetTextInput("searchRange").GetText().ToFloat();
		targetBlockCode = new AssetLocation(singleComposer.GetTextInput("targetBlockCode").GetText());
		return true;
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		BlockPos target = getTarget(visualizer.Api, visualizer.CurrentPos);
		if (target != null)
		{
			visualizer.LineTo(visualizer.CurrentPos, target.ToVec3d().Add(0.5, 0.5, 0.5), ColorUtil.ColorFromRgba(0, 255, 0, 255));
		}
	}
}
