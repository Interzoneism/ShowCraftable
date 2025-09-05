using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class OwnedEntityMapComponent : MapComponent
{
	private EntityOwnership entity;

	internal MeshRef quadModel;

	public LoadedTexture Texture;

	private Vec2f viewPos = new Vec2f();

	private Matrixf mvMat = new Matrixf();

	private int color;

	public OwnedEntityMapComponent(ICoreClientAPI capi, LoadedTexture texture, EntityOwnership entity, string color = null)
		: base(capi)
	{
		quadModel = capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
		Texture = texture;
		this.entity = entity;
		this.color = ((color != null) ? (ColorUtil.Hex2Int(color) | -16777216) : 0);
	}

	public override void Render(GuiElementMap map, float dt)
	{
		bool flag = true;
		EntityPos entityPos = capi.World.GetEntityById(entity.EntityId)?.Pos ?? entity.Pos;
		if (!(entityPos.DistanceTo(capi.World.Player.Entity.Pos.XYZ) < 2.0))
		{
			map.TranslateWorldPosToViewPos(entityPos.XYZ, ref viewPos);
			if (flag)
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
			if (Texture.Disposed)
			{
				throw new Exception("Fatal. Trying to render a disposed texture");
			}
			if (quadModel.Disposed)
			{
				throw new Exception("Fatal. Trying to render a disposed texture");
			}
			capi.Render.GlToggleBlend(blend: true);
			IShaderProgram engineShader = api.Render.GetEngineShader(EnumShaderProgram.Gui);
			if (color == 0)
			{
				engineShader.Uniform("rgbaIn", ColorUtil.WhiteArgbVec);
			}
			else
			{
				Vec4f outVal = new Vec4f();
				ColorUtil.ToRGBAVec4f(color, ref outVal);
				engineShader.Uniform("rgbaIn", outVal);
			}
			engineShader.Uniform("applyColor", 0);
			engineShader.Uniform("extraGlow", 0);
			engineShader.Uniform("noTexture", 0f);
			engineShader.BindTexture2D("tex2d", Texture.TextureId, 0);
			mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale(Texture.Width, Texture.Height, 0f)
				.Scale(0.5f, 0.5f, 0f)
				.RotateZ(0f - entityPos.Yaw + (float)Math.PI);
			engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
			api.Render.RenderMesh(quadModel);
			if (flag)
			{
				map.Api.Render.PopScissor();
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		quadModel.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec3d worldPos = capi.World.GetEntityById(entity.EntityId)?.Pos?.XYZ ?? entity.Pos.XYZ;
		Vec2f vec2f = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(worldPos, ref vec2f);
		double num = (double)args.X - mapElem.Bounds.renderX;
		double num2 = (double)args.Y - mapElem.Bounds.renderY;
		double num3 = GuiElement.scaled(5.0);
		if (Math.Abs((double)vec2f.X - num) < num3 && Math.Abs((double)vec2f.Y - num2) < num3)
		{
			hoverText.AppendLine(entity.Name);
			hoverText.AppendLine(Lang.Get("ownableentity-mapmarker-ownedbyyou"));
		}
	}
}
