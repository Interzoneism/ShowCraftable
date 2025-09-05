using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class LookatEntityAction : EntityActionBase
{
	[JsonProperty]
	private AssetLocation targetEntityCode;

	[JsonProperty]
	private float searchRange;

	public override string Type => "lookatentity";

	public LookatEntityAction()
	{
	}

	public LookatEntityAction(EntityActivitySystem vas, AssetLocation targetEntityCode, float searchRange)
	{
		base.vas = vas;
		this.targetEntityCode = targetEntityCode;
		this.searchRange = searchRange;
	}

	public override void Start(EntityActivity act)
	{
		Entity target = getTarget(vas.Entity.Api, vas.Entity.ServerPos.XYZ);
		ExecutionHasFailed = target == null;
		if (target != null)
		{
			Vec3f vec3f = new Vec3f();
			vec3f.Set((float)(target.ServerPos.X - vas.Entity.ServerPos.X), (float)(target.ServerPos.Y - vas.Entity.ServerPos.Y), (float)(target.ServerPos.Z - vas.Entity.ServerPos.Z));
			vas.Entity.ServerPos.Yaw = (float)Math.Atan2(vec3f.X, vec3f.Z);
		}
	}

	private Entity getTarget(ICoreAPI api, Vec3d fromPos)
	{
		return api.ModLoader.GetModSystem<EntityPartitioning>().GetNearestEntity(fromPos, searchRange, (Entity e) => e.WildCardMatch(targetEntityCode), EnumEntitySearchType.Creatures);
	}

	public override string ToString()
	{
		return "Look at nearest entity " + targetEntityCode.ToShortString() + " within " + searchRange + " blocks";
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Search Range", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "searchRange").AddStaticText("Entity Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "targetEntityCode");
		singleComposer.GetTextInput("searchRange").SetValue(searchRange);
		singleComposer.GetTextInput("targetEntityCode").SetValue(targetEntityCode);
	}

	public override IEntityAction Clone()
	{
		return new LookatEntityAction(vas, targetEntityCode, searchRange);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		searchRange = singleComposer.GetTextInput("searchRange").GetText().ToFloat();
		targetEntityCode = new AssetLocation(singleComposer.GetTextInput("targetEntityCode").GetText());
		return true;
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		Entity target = getTarget(visualizer.Api, visualizer.CurrentPos);
		if (target != null)
		{
			visualizer.LineTo(visualizer.CurrentPos, target.Pos.XYZ.Add(0.0, 0.5, 0.0), ColorUtil.ColorFromRgba(0, 0, 255, 255));
		}
	}
}
