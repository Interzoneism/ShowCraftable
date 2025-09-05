using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class GotoAction : EntityActionBase
{
	[JsonProperty]
	public float AnimSpeed = 1f;

	[JsonProperty]
	public float WalkSpeed = 0.02f;

	[JsonProperty]
	public string AnimCode = "walk";

	[JsonProperty]
	public bool Astar = true;

	[JsonProperty]
	public float Radius;

	public Vec3d Target = new Vec3d();

	private bool done;

	private Vec3d hereTarget;

	private int astarTries;

	private EnumAICreatureType ct;

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

	public override string Type => "goto";

	public GotoAction()
	{
	}

	public GotoAction(EntityActivitySystem vas, Vec3d target, bool astar, string animCode = "walk", float walkSpeed = 0.02f, float animSpeed = 1f, float radius = 0f)
	{
		base.vas = vas;
		Astar = astar;
		Target = target;
		AnimSpeed = animSpeed;
		AnimCode = animCode;
		WalkSpeed = walkSpeed;
		Radius = radius;
	}

	public GotoAction(EntityActivitySystem vas)
	{
		base.vas = vas;
	}

	public override void Pause(EnumInteruptionType interuptionType)
	{
		stop();
	}

	public override void Resume()
	{
		navTo(hereTarget);
	}

	public override void Start(EntityActivity act)
	{
		done = false;
		ExecutionHasFailed = false;
		hereTarget = Target.Clone().Add(vas.ActivityOffset);
		if (Radius > 0f)
		{
			float num = (float)vas.Entity.World.Rand.NextDouble() * ((float)Math.PI * 2f);
			hereTarget.X += Math.Sin(num) * (double)Radius;
			hereTarget.Z += Math.Cos(num) * (double)Radius;
		}
		astarTries = 4;
		navTo(hereTarget);
	}

	private void navTo(Vec3d hereTarget)
	{
		ct = EnumAICreatureType.Default;
		if (Enum.TryParse<EnumAICreatureType>(vas.Entity.Properties.Server.Attributes.GetString("aiCreatureType", "Humanoid"), out var result))
		{
			ct = result;
		}
		if (Astar)
		{
			vas.wppathTraverser.OnFoundPath = onFoundPath;
			vas.wppathTraverser.NavigateTo_Async(hereTarget, WalkSpeed, 0.15f, OnDone, OnStuck, OnNoPath, 10000, 0, ct);
		}
		else
		{
			vas.linepathTraverser.NavigateTo(hereTarget, WalkSpeed, OnDone, OnStuck, null, 0, ct);
			setAnimation();
		}
	}

	private void setAnimation()
	{
		if (AnimSpeed != 0.02f)
		{
			vas.Entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = AnimCode,
				Code = AnimCode,
				AnimationSpeed = AnimSpeed,
				BlendMode = EnumAnimationBlendMode.Average
			}.Init());
		}
		else if (!vas.Entity.AnimManager.StartAnimation(AnimCode))
		{
			vas.Entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = AnimCode,
				Code = AnimCode,
				AnimationSpeed = AnimSpeed,
				BlendMode = EnumAnimationBlendMode.Average
			}.Init());
		}
		vas.Entity.Controls.Sprint = AnimCode == "run" || AnimCode == "sprint";
	}

	private void onFoundPath()
	{
		setAnimation();
	}

	private void OnNoPath()
	{
		if (Astar && astarTries > 0)
		{
			astarTries--;
			vas.wppathTraverser.NavigateTo_Async(hereTarget, WalkSpeed, 0.15f, OnDone, OnStuck, OnNoPath, 10000, 0, ct);
			return;
		}
		EntityPos serverPos = vas.Entity.ServerPos;
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0} action goto from {1}/{2}/{3} to {4}/{5}/{6} failed, found no A* path to target.", vas.Entity.EntityId, serverPos.X, serverPos.Y, serverPos.Z, targetX, targetY, targetZ);
		}
		ExecutionHasFailed = true;
		Finish();
	}

	private void OnStuck()
	{
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0} GotoAction, OnStuck() called", vas.Entity.EntityId);
		}
		ExecutionHasFailed = true;
		Finish();
	}

	public override void Cancel()
	{
		Finish();
	}

	public override void Finish()
	{
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0} GotoAction, Stop() called", vas.Entity.EntityId);
		}
		stop();
	}

	private void stop()
	{
		vas.linepathTraverser.Stop();
		vas.wppathTraverser.Stop();
		vas.Entity.AnimManager.StopAnimation(AnimCode);
		vas.Entity.Controls.StopAllMovement();
	}

	private void OnDone()
	{
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0} GotoAction, OnDone() called", vas.Entity.EntityId);
		}
		vas.Entity.AnimManager.StopAnimation(AnimCode);
		vas.Entity.Controls.StopAllMovement();
		done = true;
	}

	public override bool IsFinished()
	{
		if (!done)
		{
			return ExecutionHasFailed;
		}
		return true;
	}

	public override string ToString()
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
		if (Radius > 0f)
		{
			return string.Format("{0}Goto {1}/{2}/{3} (walkSpeed {4}, animspeed {5}), radius {6}", Astar ? "A* " : "", num, num2, num3, WalkSpeed, AnimSpeed, Radius);
		}
		return string.Format("{0}Goto {1}/{2}/{3} (walkSpeed {4}, animspeed {5})", Astar ? "A* " : "", num, num2, num3, WalkSpeed, AnimSpeed);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("x/y/z Pos", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(), null, CairoFont.WhiteDetailText(), "x").AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), elementBounds = elementBounds.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), elementBounds2 = elementBounds2.FlatCopy().FixedUnder(elementBounds), EnumButtonStyle.Small)
			.AddStaticText("Goto animation code", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(), null, CairoFont.WhiteDetailText(), "animCode")
			.AddStaticText("Animation Speed", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds2 = elementBounds2.BelowCopy().WithFixedHeight(25.0), null, CairoFont.WhiteDetailText(), "animSpeed")
			.AddStaticText("Walk Speed", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds2 = elementBounds2.BelowCopy().WithFixedHeight(25.0), null, CairoFont.WhiteDetailText(), "walkSpeed")
			.AddSwitch(null, elementBounds2 = elementBounds2.BelowCopy(0.0, 15.0).WithFixedWidth(25.0), "astar", 25.0)
			.AddStaticText("A* Pathfinding", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.RightCopy(10.0, 5.0).WithFixedWidth(100.0))
			.AddStaticText("Random target offset radius", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(-35.0, 10.0).WithFixedWidth(250.0))
			.AddNumberInput(elementBounds2 = elementBounds2.BelowCopy().WithFixedSize(100.0, 25.0), null, CairoFont.WhiteDetailText(), "radius");
		GuiComposer composer = singleComposer;
		composer.GetTextInput("x").SetValue((Target?.X).ToString() ?? "");
		composer.GetTextInput("y").SetValue((Target?.Y).ToString() ?? "");
		composer.GetTextInput("z").SetValue((Target?.Z).ToString() ?? "");
		composer.GetSwitch("astar").On = Astar;
		composer.GetTextInput("animCode").SetValue(AnimCode);
		composer.GetNumberInput("animSpeed").SetValue(AnimSpeed.ToString() ?? "");
		composer.GetNumberInput("walkSpeed").SetValue(WalkSpeed.ToString() ?? "");
		composer.GetNumberInput("radius").SetValue(Radius.ToString() ?? "");
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

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		Target = new Vec3d(s.GetTextInput("x").GetText().ToDouble(), s.GetTextInput("y").GetText().ToDouble(), s.GetTextInput("z").GetText().ToDouble());
		Astar = s.GetSwitch("astar").On;
		AnimCode = s.GetTextInput("animCode").GetText();
		AnimSpeed = s.GetNumberInput("animSpeed").GetText().ToFloat();
		WalkSpeed = s.GetNumberInput("walkSpeed").GetText().ToFloat();
		Radius = s.GetNumberInput("radius").GetText().ToFloat();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new GotoAction(vas, Target, Astar, AnimCode, WalkSpeed, AnimSpeed, Radius);
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		Vec3d vec3d = Target.Clone();
		if (vas != null)
		{
			vec3d.Add(vas.ActivityOffset);
		}
		visualizer.GoTo(vec3d);
	}
}
