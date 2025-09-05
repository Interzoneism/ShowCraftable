using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class OreMapComponent : MapComponent
{
	private Vec2f viewPos = new Vec2f();

	private Vec4f color = new Vec4f();

	private PropickReading reading;

	private int waypointIndex;

	private Matrixf mvMat = new Matrixf();

	private OreMapLayer oreLayer;

	private bool mouseOver;

	public static float IconScale = 0.85f;

	public string filterByOreCode;

	public OreMapComponent(int waypointIndex, PropickReading reading, OreMapLayer wpLayer, ICoreClientAPI capi, string filterByOreCode)
		: base(capi)
	{
		this.waypointIndex = waypointIndex;
		this.reading = reading;
		oreLayer = wpLayer;
		int num = GuiStyle.DamageColorGradient[(int)Math.Min(99.0, reading.HighestReading * 150.0)];
		if (filterByOreCode != null)
		{
			num = GuiStyle.DamageColorGradient[(int)Math.Min(99.0, reading.OreReadings[filterByOreCode].TotalFactor * 150.0)];
		}
		color = new Vec4f();
		ColorUtil.ToRGBAVec4f(num, ref color);
		color.W = 1f;
	}

	public override void Render(GuiElementMap map, float dt)
	{
		map.TranslateWorldPosToViewPos(reading.Position, ref viewPos);
		if (!(viewPos.X < -10f) && !(viewPos.Y < -10f) && !((double)viewPos.X > map.Bounds.OuterWidth + 10.0) && !((double)viewPos.Y > map.Bounds.OuterHeight + 10.0))
		{
			float x = (float)(map.Bounds.renderX + (double)viewPos.X);
			float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
			ICoreClientAPI api = map.Api;
			IShaderProgram engineShader = api.Render.GetEngineShader(EnumShaderProgram.Gui);
			engineShader.Uniform("rgbaIn", color);
			engineShader.Uniform("extraGlow", 0);
			engineShader.Uniform("applyColor", 0);
			engineShader.Uniform("noTexture", 0f);
			LoadedTexture oremapTexture = oreLayer.oremapTexture;
			float num = (float)(mouseOver ? 6 : 0) - 1.5f * Math.Max(1f, 1f / map.ZoomLevel);
			if (oremapTexture != null)
			{
				engineShader.BindTexture2D("tex2d", oremapTexture.TextureId, 0);
				engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
				mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)oremapTexture.Width + num, (float)oremapTexture.Height + num, 0f)
					.Scale(0.5f * IconScale, 0.5f * IconScale, 0f);
				Matrixf matrixf = mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
				engineShader.Uniform("rgbaIn", new Vec4f(0f, 0f, 0f, 0.7f));
				engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
				api.Render.RenderMesh(oreLayer.quadModel);
				engineShader.Uniform("rgbaIn", color);
				engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
				api.Render.RenderMesh(oreLayer.quadModel);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec2f vec2f = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(reading.Position, ref vec2f);
		double num = (double)vec2f.X + mapElem.Bounds.renderX;
		double num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
		double value = (double)args.X - num;
		double value2 = (double)args.Y - num2;
		float num3 = RuntimeEnv.GUIScale * 8f;
		if (mouseOver = Math.Abs(value) < (double)num3 && Math.Abs(value2) < (double)num3)
		{
			Dictionary<string, string> pageCodes = capi.ModLoader.GetModSystem<ModSystemOreMap>().prospectingMetaData.PageCodes;
			string value3 = reading.ToHumanReadable(capi.Settings.String["language"], pageCodes);
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
		mapElem.TranslateWorldPosToViewPos(reading.Position, ref vec2f);
		double num = (double)vec2f.X + mapElem.Bounds.renderX;
		double num2 = (double)vec2f.Y + mapElem.Bounds.renderY;
		double value = (double)args.X - num;
		double value2 = (double)args.Y - num2;
		float num3 = RuntimeEnv.GUIScale * 8f;
		if (Math.Abs(value) < (double)num3 && Math.Abs(value2) < (double)num3)
		{
			GuiDialogConfirm guiDialogConfirm = new GuiDialogConfirm(capi, Lang.Get("prospecting-reading-confirmdelete"), onConfirmDone);
			guiDialogConfirm.TryOpen();
			GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
			guiDialogConfirm.OnClosed += delegate
			{
				capi.Gui.RequestFocus(mapdlg);
			};
			args.Handled = true;
		}
	}

	private void onConfirmDone(bool confirm)
	{
		if (confirm)
		{
			oreLayer.Delete(capi.World.Player, waypointIndex);
		}
	}
}
