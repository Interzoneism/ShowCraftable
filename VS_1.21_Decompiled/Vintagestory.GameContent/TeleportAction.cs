using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TeleportAction : EntityActionBase
{
	[JsonProperty]
	public double TargetX { get; set; }

	[JsonProperty]
	public double TargetY { get; set; }

	[JsonProperty]
	public double TargetZ { get; set; }

	[JsonProperty]
	public double Yaw { get; set; }

	public override string Type => "teleport";

	public TeleportAction()
	{
	}

	public TeleportAction(EntityActivitySystem vas, double targetX, double targetY, double targetZ, double yaw)
	{
		base.vas = vas;
		TargetX = targetX;
		TargetY = targetY;
		TargetZ = targetZ;
		Yaw = yaw;
	}

	public TeleportAction(EntityActivitySystem vas)
	{
		base.vas = vas;
	}

	public override void Start(EntityActivity act)
	{
		vas.Entity.TeleportToDouble(TargetX + (double)vas.ActivityOffset.X, TargetY + (double)vas.ActivityOffset.Y, TargetZ + (double)vas.ActivityOffset.Z);
		vas.Entity.Controls.StopAllMovement();
		vas.wppathTraverser.Stop();
		vas.Entity.ServerPos.Yaw = (float)Yaw;
		vas.Entity.Pos.Yaw = (float)Yaw;
		vas.Entity.BodyYaw = (float)Yaw;
		vas.Entity.BodyYawServer = (float)Yaw;
		vas.ClearNextActionDelay();
	}

	public override string ToString()
	{
		double num = TargetX;
		double num2 = TargetY;
		double num3 = TargetZ;
		if (vas != null)
		{
			num += (double)vas.ActivityOffset.X;
			num2 += (double)vas.ActivityOffset.Y;
			num3 += (double)vas.ActivityOffset.Z;
		}
		return $"Teleport to {num}/{num2}/{num3}";
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("x/y/z Pos", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(), null, CairoFont.WhiteDetailText(), "x").AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(elementBounds = elementBounds.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), elementBounds = elementBounds.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), elementBounds2 = elementBounds2.FlatCopy().FixedUnder(elementBounds), EnumButtonStyle.Small)
			.AddStaticText("Yaw (in radians)", CairoFont.WhiteDetailText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 15.0).WithFixedWidth(120.0))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 5.0), null, CairoFont.WhiteDetailText(), "yaw");
		GuiComposer composer = singleComposer;
		composer.GetTextInput("x").SetValue(TargetX.ToString() ?? "");
		composer.GetTextInput("y").SetValue(TargetY.ToString() ?? "");
		composer.GetTextInput("z").SetValue(TargetZ.ToString() ?? "");
		composer.GetTextInput("yaw").SetValue(Yaw.ToString() ?? "");
	}

	private bool onClickTpTo(ICoreClientAPI capi)
	{
		double num = TargetX;
		double num2 = TargetY;
		double num3 = TargetZ;
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
		singleComposer.GetTextInput("yaw").SetValue(Math.Round(capi.World.Player.Entity.ServerPos.Yaw - (float)Math.PI / 2f, 1).ToString() ?? "");
		return true;
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		TargetX = s.GetTextInput("x").GetText().ToDouble();
		TargetY = s.GetTextInput("y").GetText().ToDouble();
		TargetZ = s.GetTextInput("z").GetText().ToDouble();
		Yaw = s.GetTextInput("yaw").GetText().ToDouble();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new TeleportAction(vas, TargetX, TargetY, TargetZ, Yaw);
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		Vec3d vec3d = new Vec3d(TargetX, TargetY, TargetZ);
		if (vas != null)
		{
			vec3d.Add(vas.ActivityOffset);
		}
		visualizer.GoTo(vec3d, ColorUtil.ColorFromRgba(255, 255, 0, 255));
	}
}
