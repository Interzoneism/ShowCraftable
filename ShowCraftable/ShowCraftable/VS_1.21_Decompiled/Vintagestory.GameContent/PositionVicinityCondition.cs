using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class PositionVicinityCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private float range;

	[JsonProperty]
	private float yrange = -1f;

	public Vec3d Target = new Vec3d();

	protected EntityActivitySystem vas;

	private Vec3d tmpPos = new Vec3d();

	[JsonProperty]
	public bool Invert { get; set; }

	[JsonProperty]
	public double targetX
	{
		get
		{
			return Target.X;
		}
		set
		{
			Target.X = value;
		}
	}

	[JsonProperty]
	public double targetY
	{
		get
		{
			return Target.Y;
		}
		set
		{
			Target.Y = value;
		}
	}

	[JsonProperty]
	public double targetZ
	{
		get
		{
			return Target.Z;
		}
		set
		{
			Target.Z = value;
		}
	}

	public string Type => "positionvicinity";

	public PositionVicinityCondition()
	{
	}

	public PositionVicinityCondition(EntityActivitySystem vas, Vec3d pos, float range, float yrange, bool invert = false)
	{
		this.vas = vas;
		Target = pos;
		this.range = range;
		this.yrange = yrange;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		tmpPos.Set(Target).Add(vas.ActivityOffset);
		if (yrange >= 0f)
		{
			if (e.ServerPos.HorDistanceTo(tmpPos) < (double)range)
			{
				return Math.Abs(e.ServerPos.Y - tmpPos.Y) < (double)yrange;
			}
			return false;
		}
		return e.ServerPos.DistanceTo(tmpPos) < (double)range;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("x/y/z Pos", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(), null, CairoFont.WhiteDetailText(), "x").AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), elementBounds = elementBounds.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), elementBounds2 = elementBounds2.FlatCopy().FixedUnder(elementBounds), EnumButtonStyle.Small)
			.AddStaticText("Range", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "range")
			.AddStaticText("Vertical Range (-1 to ignore)", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "vrange");
		singleComposer.GetNumberInput("range").SetValue(range);
		singleComposer.GetNumberInput("vrange").SetValue(yrange);
		GuiComposer composer = singleComposer;
		composer.GetTextInput("x").SetValue((Target?.X).ToString() ?? "");
		composer.GetTextInput("y").SetValue((Target?.Y).ToString() ?? "");
		composer.GetTextInput("z").SetValue((Target?.Z).ToString() ?? "");
	}

	private bool onClickTpTo(ICoreClientAPI capi)
	{
		double num = Target.X;
		double num2 = Target.Y;
		double num3 = Target.Z;
		if (vas != null)
		{
			num += (double)vas.ActivityOffset.X;
			num2 += (double)vas.ActivityOffset.Y;
			num3 += (double)vas.ActivityOffset.Z;
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

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		Target = new Vec3d(s.GetTextInput("x").GetText().ToDouble(), s.GetTextInput("y").GetText().ToDouble(), s.GetTextInput("z").GetText().ToDouble());
		range = s.GetNumberInput("range").GetValue();
		yrange = s.GetNumberInput("vrange").GetValue();
	}

	public IActionCondition Clone()
	{
		return new PositionVicinityCondition(vas, Target, range, yrange, Invert);
	}

	public override string ToString()
	{
		return (Invert ? "When not near pos " : "When near pos ") + Target;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
