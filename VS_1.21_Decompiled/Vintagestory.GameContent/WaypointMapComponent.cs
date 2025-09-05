using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WaypointMapComponent : MapComponent
{
	private Vec2f viewPos = new Vec2f();

	private Vec4f color = new Vec4f();

	private Waypoint waypoint;

	private int waypointIndex;

	private Matrixf mvMat = new Matrixf();

	private WaypointMapLayer wpLayer;

	private bool mouseOver;

	public static float IconScale = 0.85f;

	private GuiDialogEditWayPoint editWpDlg;

	public WaypointMapComponent(int waypointIndex, Waypoint waypoint, WaypointMapLayer wpLayer, ICoreClientAPI capi)
		: base(capi)
	{
		this.waypointIndex = waypointIndex;
		this.waypoint = waypoint;
		this.wpLayer = wpLayer;
		ColorUtil.ToRGBAVec4f(waypoint.Color, ref color);
	}

	public override void Render(GuiElementMap map, float dt)
	{
		map.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
		if (waypoint.Pinned)
		{
			map.Api.Render.PushScissor(null);
			map.ClampButPreserveAngle(ref viewPos, 2);
		}
		else if (viewPos.X < -10f || viewPos.Y < -10f || (double)viewPos.X > map.Bounds.OuterWidth + 10.0 || (double)viewPos.Y > map.Bounds.OuterHeight + 10.0)
		{
			return;
		}
		float x = (float)(map.Bounds.renderX + (double)viewPos.X);
		float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
		ICoreClientAPI api = map.Api;
		IShaderProgram engineShader = api.Render.GetEngineShader(EnumShaderProgram.Gui);
		engineShader.Uniform("rgbaIn", color);
		engineShader.Uniform("extraGlow", 0);
		engineShader.Uniform("applyColor", 0);
		engineShader.Uniform("noTexture", 0f);
		float num = (float)(mouseOver ? 6 : 0) - 1.5f * Math.Max(1f, 1f / map.ZoomLevel);
		if (!wpLayer.texturesByIcon.TryGetValue(waypoint.Icon, out var value))
		{
			wpLayer.texturesByIcon.TryGetValue("circle", out value);
		}
		if (value != null)
		{
			engineShader.BindTexture2D("tex2d", value.TextureId, 0);
			engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)value.Width + num, (float)value.Height + num, 0f)
				.Scale(0.5f * IconScale, 0.5f * IconScale, 0f);
			Matrixf matrixf = mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
			engineShader.Uniform("rgbaIn", new Vec4f(0f, 0f, 0f, 0.6f));
			engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
			api.Render.RenderMesh(wpLayer.quadModel);
			engineShader.Uniform("rgbaIn", color);
			engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
			api.Render.RenderMesh(wpLayer.quadModel);
		}
		if (waypoint.Pinned)
		{
			map.Api.Render.PopScissor();
		}
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec2f vec2f = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref vec2f);
		double num = (double)vec2f.X + mapElem.Bounds.renderX;
		double num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
		if (waypoint.Pinned)
		{
			mapElem.ClampButPreserveAngle(ref vec2f, 2);
			num = (double)vec2f.X + mapElem.Bounds.renderX;
			num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
			num = (float)GameMath.Clamp(num, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0);
			num2 = (float)GameMath.Clamp(num2, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0);
		}
		double value = (double)args.X - num;
		double value2 = (double)args.Y - num2;
		float num3 = RuntimeEnv.GUIScale * 8f;
		if (mouseOver = Math.Abs(value) < (double)num3 && Math.Abs(value2) < (double)num3)
		{
			string value3 = Lang.Get("Waypoint {0}", waypointIndex) + "\n" + waypoint.Title;
			hoverText.AppendLine(value3);
		}
	}

	public override void OnMouseUpOnElement(MouseEvent args, GuiElementMap mapElem)
	{
		if (args.Button != EnumMouseButton.Right)
		{
			return;
		}
		Vec2f vec2f = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref vec2f);
		double num = (double)vec2f.X + mapElem.Bounds.renderX;
		double num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
		if (waypoint.Pinned)
		{
			mapElem.ClampButPreserveAngle(ref vec2f, 2);
			num = (double)vec2f.X + mapElem.Bounds.renderX;
			num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
			num = (float)GameMath.Clamp(num, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0);
			num2 = (float)GameMath.Clamp(num2, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0);
		}
		double value = (double)args.X - num;
		double value2 = (double)args.Y - num2;
		float num3 = RuntimeEnv.GUIScale * 8f;
		if (!(Math.Abs(value) < (double)num3) || !(Math.Abs(value2) < (double)num3))
		{
			return;
		}
		if (editWpDlg != null)
		{
			editWpDlg.TryClose();
			editWpDlg.Dispose();
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && capi.World.Player.Entity.Controls.ShiftKey)
		{
			BlockPos asBlockPos = waypoint.Position.AsBlockPos;
			capi.SendChatMessage($"/tp ={asBlockPos.X} {asBlockPos.Y} ={asBlockPos.Z}");
			mapElem.prevPlayerPos.Set(asBlockPos);
			mapElem.CenterMapTo(asBlockPos);
		}
		else
		{
			GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
			editWpDlg = new GuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer, waypoint, waypointIndex);
			editWpDlg.TryOpen();
			editWpDlg.OnClosed += delegate
			{
				capi.Gui.RequestFocus(mapdlg);
			};
		}
		args.Handled = true;
	}
}
