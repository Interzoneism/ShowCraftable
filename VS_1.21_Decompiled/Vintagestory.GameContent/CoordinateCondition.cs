using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class CoordinateCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	public int Axis;

	[JsonProperty]
	public double Value;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "coordinate";

	public CoordinateCondition()
	{
	}

	public CoordinateCondition(EntityActivitySystem vas, int axis, double value, bool invert = false)
	{
		this.vas = vas;
		Axis = axis;
		Value = value;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		EntityPos serverPos = e.ServerPos;
		int num = 0;
		if (vas != null)
		{
			num = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[Axis];
		}
		return Axis switch
		{
			0 => serverPos.X < Value + (double)num, 
			1 => serverPos.Y < Value + (double)num, 
			2 => serverPos.Z < Value + (double)num, 
			_ => false, 
		};
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("When", CairoFont.WhiteDetailText(), elementBounds).AddDropDown(new string[3] { "x", "y", "z" }, new string[3] { "X", "Y", "Z" }, Axis, null, elementBounds = elementBounds.BelowCopy().WithFixedWidth(100.0), "axis").AddSmallButton("Tp to", () => btnTp(singleComposer, capi), elementBounds.CopyOffsetedSibling(110.0).WithFixedWidth(50.0), EnumButtonStyle.Small)
			.AddStaticText("Is smaller than", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedWidth(200.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy().WithFixedWidth(200.0), null, CairoFont.WhiteDetailText(), "value")
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), elementBounds = elementBounds.BelowCopy(), EnumButtonStyle.Small);
		singleComposer.GetNumberInput("value").SetValue(Value);
	}

	private bool btnTp(GuiComposer s, ICoreClientAPI capi)
	{
		int num = s.GetDropDown("axis").SelectedIndices[0];
		double x = capi.World.Player.Entity.Pos.X;
		double num2 = capi.World.Player.Entity.Pos.Y;
		double num3 = capi.World.Player.Entity.Pos.Z;
		double num4 = (x = s.GetNumberInput("value").GetValue());
		int num5 = 0;
		if (vas != null)
		{
			num5 = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[num];
		}
		switch (num)
		{
		case 0:
			x = num4 + (double)num5;
			break;
		case 1:
			num2 = num4 + (double)num5;
			break;
		case 2:
			num3 = num4 + (double)num5;
			break;
		}
		capi.SendChatMessage($"/tp ={x} ={num2} ={num3}");
		return false;
	}

	private bool onClickPlayerPos(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		int num = singleComposer.GetDropDown("axis").SelectedIndices[0];
		double value = 0.0;
		switch (num)
		{
		case 0:
			value = capi.World.Player.Entity.Pos.X;
			break;
		case 1:
			value = capi.World.Player.Entity.Pos.Y;
			break;
		case 2:
			value = capi.World.Player.Entity.Pos.Z;
			break;
		}
		singleComposer.GetTextInput("value").SetValue(Math.Round(value, 1).ToString() ?? "");
		return true;
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		Value = s.GetNumberInput("value").GetValue();
		Axis = s.GetDropDown("axis").SelectedIndices[0];
	}

	public IActionCondition Clone()
	{
		return new CoordinateCondition(vas, Axis, Value, Invert);
	}

	public override string ToString()
	{
		string arg = (new string[3] { "X", "Y", "Z" })[Axis];
		int num = 0;
		if (vas != null)
		{
			num = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[Axis];
		}
		return string.Format("When {0} {1} {2}", arg, Invert ? "&gt;=" : "&lt;", Value + (double)num);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
