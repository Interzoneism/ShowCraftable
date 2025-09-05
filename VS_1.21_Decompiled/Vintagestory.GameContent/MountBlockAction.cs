using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class MountBlockAction : EntityActionBase
{
	[JsonProperty]
	private AssetLocation targetBlockCode;

	[JsonProperty]
	private float searchRange;

	[JsonProperty]
	public BlockPos targetPosition;

	public override string Type => "mountblock";

	public MountBlockAction()
	{
	}

	public MountBlockAction(EntityActivitySystem vas, AssetLocation targetBlockCode, float searchRange, BlockPos pos)
	{
		base.vas = vas;
		this.targetBlockCode = targetBlockCode;
		this.searchRange = searchRange;
		targetPosition = pos;
	}

	public override bool IsFinished()
	{
		return vas.Entity.MountedOn != null;
	}

	public override void Start(EntityActivity act)
	{
		if (vas.Entity.MountedOn != null)
		{
			return;
		}
		bool mountablefound = false;
		searchMountable(vas.Entity.ServerPos.XYZ, delegate(IMountableSeat seat, BlockPos pos)
		{
			mountablefound = true;
			if (vas.Entity.TryMount(seat))
			{
				vas.Entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.StopTasks();
				vas.Entity.ServerControls.StopAllMovement();
				return true;
			}
			return false;
		});
		if (vas.Debug && !mountablefound)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0} MountBlockAction, no nearby block of code {1} found.", vas.Entity.EntityId, targetBlockCode);
		}
		ExecutionHasFailed = vas.Entity.MountedOn == null;
	}

	private void searchMountable(Vec3d fromPos, ActionBoolReturn<IMountableSeat, BlockPos> onblock)
	{
		if (targetPosition != null)
		{
			BlockPos blockPos = targetPosition.Copy();
			if (vas != null)
			{
				blockPos.Add(vas.ActivityOffset);
			}
			IMountableSeat mountableSeat = vas.Entity.World.BlockAccessor.GetBlock(blockPos).GetInterface<IMountableSeat>(vas.Entity.World, blockPos);
			if (mountableSeat != null)
			{
				onblock(mountableSeat, blockPos);
			}
			return;
		}
		BlockPos asBlockPos = fromPos.Clone().Sub(searchRange, 1.0, searchRange).AsBlockPos;
		BlockPos asBlockPos2 = fromPos.Clone().Add(searchRange, 1.0, searchRange).AsBlockPos;
		vas.Entity.World.BlockAccessor.SearchBlocks(asBlockPos, asBlockPos2, delegate(Block block, BlockPos pos)
		{
			if (block.WildCardMatch(targetBlockCode))
			{
				IMountableSeat mountableSeat2 = block.GetInterface<IMountableSeat>(vas.Entity.World, pos);
				if (mountableSeat2 != null && onblock(mountableSeat2, pos))
				{
					return false;
				}
			}
			return true;
		});
	}

	public override void Cancel()
	{
		vas.Entity.TryUnmount();
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		singleComposer.AddStaticText("Search Range", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "searchRange").AddStaticText("OR exact x/y/z Pos", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0))
			.AddTextInput(elementBounds2 = elementBounds2.FlatCopy().FixedUnder(elementBounds, -3.0), null, CairoFont.WhiteDetailText(), "x")
			.AddTextInput(elementBounds2 = elementBounds2.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(elementBounds2 = elementBounds2.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), elementBounds2 = elementBounds2.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), elementBounds = elementBounds.FlatCopy().WithFixedPosition(0.0, 0.0).FixedUnder(elementBounds2, 2.0), EnumButtonStyle.Small)
			.AddStaticText("Block Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "targetBlockCode");
		GuiComposer composer = singleComposer;
		composer.GetTextInput("searchRange").SetValue(searchRange);
		composer.GetTextInput("targetBlockCode").SetValue(targetBlockCode?.ToShortString() ?? "");
		composer.GetTextInput("x").SetValue((targetPosition?.X).ToString() ?? "");
		composer.GetTextInput("y").SetValue((targetPosition?.Y).ToString() ?? "");
		composer.GetTextInput("z").SetValue((targetPosition?.Z).ToString() ?? "");
	}

	private bool onClickTpTo(ICoreClientAPI capi)
	{
		int num = targetPosition.X;
		int num2 = targetPosition.Y;
		int num3 = targetPosition.Z;
		if (vas != null)
		{
			num += vas.ActivityOffset.X;
			num2 += vas.ActivityOffset.Y;
			num3 += vas.ActivityOffset.Z;
		}
		capi.SendChatMessage($"/tp ={num} ={num2} ={num3}");
		return false;
	}

	private bool onClickPlayerPos(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Vec3d xYZ = capi.World.Player.Entity.Pos.XYZ;
		singleComposer.GetTextInput("x").SetValue(Math.Round(xYZ.X, 1).ToString() ?? "");
		singleComposer.GetTextInput("y").SetValue(Math.Round(xYZ.Y, 1).ToString() ?? "");
		singleComposer.GetTextInput("z").SetValue(Math.Round(xYZ.Z, 1).ToString() ?? "");
		return true;
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		if (singleComposer.GetTextInput("x").GetText().Length > 0)
		{
			targetPosition = new BlockPos((int)singleComposer.GetTextInput("x").GetText().ToDouble(), (int)singleComposer.GetTextInput("y").GetText().ToDouble(), (int)singleComposer.GetTextInput("z").GetText().ToDouble());
		}
		else
		{
			targetPosition = null;
		}
		searchRange = singleComposer.GetTextInput("searchRange").GetText().ToFloat();
		targetBlockCode = new AssetLocation(singleComposer.GetTextInput("targetBlockCode").GetText());
		return true;
	}

	public override IEntityAction Clone()
	{
		return new MountBlockAction(vas, targetBlockCode, searchRange, targetPosition);
	}

	public override string ToString()
	{
		if (targetPosition != null)
		{
			BlockPos blockPos = targetPosition.Copy();
			if (vas != null)
			{
				blockPos.Add(vas.ActivityOffset);
			}
			return "Mount block at " + blockPos;
		}
		return string.Concat("Mount block ", targetBlockCode, " within ", searchRange.ToString(), " blocks");
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		searchMountable(visualizer.CurrentPos, delegate(IMountableSeat seat, BlockPos pos)
		{
			visualizer.LineTo(visualizer.CurrentPos, pos.ToVec3d().Add(0.5, 0.5, 0.5), ColorUtil.ColorFromRgba(0, 255, 255, 255));
			return false;
		});
	}
}
