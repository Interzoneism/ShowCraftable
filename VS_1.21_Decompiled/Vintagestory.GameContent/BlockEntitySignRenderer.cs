using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySignRenderer : IRenderer, IDisposable
{
	protected int TextWidth = 200;

	protected int TextHeight = 100;

	protected float QuadWidth = 0.875f;

	protected float QuadHeight = 13f / 32f;

	protected CairoFont font;

	protected BlockPos pos;

	protected ICoreClientAPI api;

	protected LoadedTexture loadedTexture;

	protected MeshRef quadModelRef;

	public Matrixf ModelMat = new Matrixf();

	public float rotY;

	protected float translateX;

	protected float translateY;

	protected float translateZ;

	protected float offsetX;

	protected float offsetY;

	protected float offsetZ;

	public float fontSize = 20f;

	private EnumVerticalAlign verticalAlign;

	internal bool translateable;

	public double RenderOrder => 1.1;

	public int RenderRange => 24;

	public BlockEntitySignRenderer(BlockPos pos, ICoreClientAPI api, TextAreaConfig config)
	{
		this.api = api;
		this.pos = pos;
		if (config == null)
		{
			config = new TextAreaConfig();
		}
		fontSize = config.FontSize;
		QuadWidth = config.textVoxelWidth / 16f;
		QuadHeight = config.textVoxelHeight / 16f;
		verticalAlign = config.VerticalAlign;
		TextWidth = config.MaxWidth;
		font = new CairoFont(fontSize, config.FontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
		if (config.BoldFont)
		{
			font.WithWeight((FontWeight)1);
		}
		font.LineHeightMultiplier = 0.8999999761581421;
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "sign");
		MeshData quad = QuadMeshUtil.GetQuad();
		quad.Uv = new float[8] { 1f, 1f, 0f, 1f, 0f, 0f, 1f, 0f };
		quad.Rgba = new byte[16];
		quad.Rgba.Fill(byte.MaxValue);
		quadModelRef = api.Render.UploadMesh(quad);
		Block block = api.World.BlockAccessor.GetBlock(pos);
		BlockFacing blockFacing = BlockFacing.FromCode(block.LastCodePart());
		if (blockFacing != null)
		{
			float num = ((block.Variant["attachment"] == "wall") ? 0.22f : 0f);
			translateY = 0.5625f;
			switch (blockFacing.Index)
			{
			case 0:
				translateX = 0.5f;
				translateZ = 0.29000002f - num;
				rotY = 180f;
				break;
			case 1:
				translateX = 0.71f + num;
				translateZ = 0.5f;
				rotY = 90f;
				break;
			case 2:
				translateX = 0.5f;
				translateZ = 0.71f + num;
				break;
			case 3:
				translateX = 0.29000002f - num;
				translateZ = 0.5f;
				rotY = 270f;
				break;
			}
			offsetX += config.textVoxelOffsetX / 16f;
			offsetY += config.textVoxelOffsetY / 16f;
			offsetZ += config.textVoxelOffsetZ / 16f;
		}
	}

	public void SetFreestanding(float angleRad)
	{
		rotY = 180f + angleRad * (180f / (float)Math.PI);
		translateX = 0.5f;
		translateZ = 0.5f;
		offsetZ = -0.094375f;
	}

	public virtual void SetNewText(string text, int color)
	{
		if (translateable)
		{
			string text2 = Lang.Get(text);
			if (Lang.UsesNonLatinCharacters(Lang.CurrentLocale))
			{
				string l = Lang.GetL(Lang.DefaultLocale, text);
				if (text2 != l)
				{
					font.Fontname = GuiStyle.StandardFontName;
				}
			}
			text = text2;
		}
		font.WithColor(ColorUtil.ToRGBADoubles(color));
		loadedTexture?.Dispose();
		loadedTexture = null;
		if (text.Length > 0)
		{
			font.UnscaledFontsize = fontSize / RuntimeEnv.GUIScale;
			double num = ((verticalAlign == EnumVerticalAlign.Middle) ? ((double)TextHeight - api.Gui.Text.GetMultilineTextHeight(font, text, TextWidth)) : 0.0);
			TextBackground background = new TextBackground
			{
				VerPadding = (int)num / 2
			};
			loadedTexture = api.Gui.TextTexture.GenTextTexture(text, font, TextWidth, TextHeight, background, EnumTextOrientation.Center);
		}
	}

	public virtual void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (loadedTexture != null && api.Render.DefaultFrustumCuller.SphereInFrustum((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5, 1.0))
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
			IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			standardShaderProgram.Tex2D = loadedTexture.TextureId;
			standardShaderProgram.NormalShaded = 0;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(translateX, translateY, translateZ)
				.RotateY(rotY * ((float)Math.PI / 180f))
				.Translate(offsetX, offsetY, offsetZ)
				.Scale(0.5f * QuadWidth, 0.5f * QuadHeight, 0.5f * QuadWidth)
				.Values;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			standardShaderProgram.NormalShaded = 0;
			standardShaderProgram.ExtraGodray = 0f;
			standardShaderProgram.SsaoAttn = 0f;
			standardShaderProgram.AlphaTest = 0.05f;
			standardShaderProgram.OverlayOpacity = 0f;
			render.RenderMesh(quadModelRef);
			standardShaderProgram.Stop();
			render.GlToggleBlend(blend: true);
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		loadedTexture?.Dispose();
		quadModelRef?.Dispose();
	}
}
