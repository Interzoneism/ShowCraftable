using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityMapComponent : MapComponent
{
	public Entity entity;

	internal MeshRef quadModel;

	public LoadedTexture Texture;

	private Vec2f viewPos = new Vec2f();

	private Matrixf mvMat = new Matrixf();

	private int color;

	public EntityMapComponent(ICoreClientAPI capi, LoadedTexture texture, Entity entity, string color = null)
		: base(capi)
	{
		quadModel = capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
		Texture = texture;
		this.entity = entity;
		this.color = ((color != null) ? (ColorUtil.Hex2Int(color) | -16777216) : 0);
	}

	public override void Render(GuiElementMap map, float dt)
	{
		IPlayer player = (entity as EntityPlayer)?.Player;
		if (player != null && player.WorldData?.CurrentGameMode == EnumGameMode.Spectator && capi.World.Player != player)
		{
			return;
		}
		EntityPlayer obj = entity as EntityPlayer;
		if (obj == null || !obj.Controls.Sneak || player == capi.World.Player)
		{
			map.TranslateWorldPosToViewPos(entity.Pos.XYZ, ref viewPos);
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
				.RotateZ(0f - entity.Pos.Yaw + (float)Math.PI);
			engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
			api.Render.RenderMesh(quadModel);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		quadModel.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec2f vec2f = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(entity.Pos.XYZ, ref vec2f);
		double num = (double)args.X - mapElem.Bounds.renderX;
		double num2 = (double)args.Y - mapElem.Bounds.renderY;
		double num3 = GuiElement.scaled(5.0);
		if (Math.Abs((double)vec2f.X - num) < num3 && Math.Abs((double)vec2f.Y - num2) < num3)
		{
			if (entity is EntityPlayer entityPlayer)
			{
				hoverText.AppendLine("Player " + capi.World.PlayerByUid(entityPlayer.PlayerUID)?.PlayerName);
			}
			else
			{
				hoverText.AppendLine(entity.GetName());
			}
		}
	}
}
