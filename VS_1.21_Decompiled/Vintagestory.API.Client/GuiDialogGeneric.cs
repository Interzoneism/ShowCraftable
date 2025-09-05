using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiDialogGeneric : GuiDialog
{
	public string DialogTitle;

	public override bool UnregisterOnClose => true;

	public virtual ITreeAttribute Attributes { get; protected set; }

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogGeneric(string DialogTitle, ICoreClientAPI capi)
		: base(capi)
	{
		this.DialogTitle = DialogTitle;
	}

	public virtual void Recompose()
	{
		foreach (GuiComposer value in Composers.Values)
		{
			value.ReCompose();
		}
	}

	public virtual void UnfocusElements()
	{
		foreach (GuiComposer value in Composers.Values)
		{
			value.UnfocusOwnElements();
		}
	}

	public virtual void FocusElement(int index)
	{
		base.SingleComposer.FocusElement(index);
	}

	public virtual bool IsInRangeOfBlock(BlockPos blockEntityPos)
	{
		Cuboidf[] selectionBoxes = capi.World.BlockAccessor.GetBlock(blockEntityPos).GetSelectionBoxes(capi.World.BlockAccessor, blockEntityPos);
		double num = 99.0;
		int num2 = 0;
		while (selectionBoxes != null && num2 < selectionBoxes.Length)
		{
			Cuboidf cuboidf = selectionBoxes[num2];
			Vec3d vec = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
			num = Math.Min(num, cuboidf.ToDouble().Translate(blockEntityPos.X, blockEntityPos.InternalY, blockEntityPos.Z).ShortestDistanceFrom(vec));
			num2++;
		}
		return num <= (double)capi.World.Player.WorldData.PickingRange + 0.5;
	}
}
