using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySignPostRenderer : IRenderer, IDisposable
{
	protected static int TextWidth = 200;

	protected static int TextHeight = 25;

	protected static float QuadWidth = 0.7f;

	protected static float QuadHeight = 0.1f;

	protected CairoFont font;

	protected BlockPos pos;

	protected ICoreClientAPI api;

	protected LoadedTexture loadedTexture;

	protected MeshRef quadModelRef;

	public Matrixf ModelMat = new Matrixf();

	protected float rotY;

	protected float translateX;

	protected float translateY = 0.5625f;

	protected float translateZ;

	private string[] textByCardinal;

	private double fontSize;

	public double RenderOrder => 0.5;

	public int RenderRange => 48;

	public BlockEntitySignPostRenderer(BlockPos pos, ICoreClientAPI api, CairoFont font)
	{
		this.api = api;
		this.pos = pos;
		this.font = font;
		fontSize = font.UnscaledFontsize;
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "signpost");
	}

	private void genMesh()
	{
		MeshData meshData = new MeshData(4, 6);
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			if (textByCardinal[i].Length != 0)
			{
				num++;
			}
		}
		if (num == 0)
		{
			quadModelRef?.Dispose();
			quadModelRef = null;
			return;
		}
		int num2 = 0;
		for (int j = 0; j < 8; j++)
		{
			if (textByCardinal[j].Length != 0)
			{
				Cardinal cardinal = Cardinal.ALL[j];
				MeshData quad = QuadMeshUtil.GetQuad();
				float num3 = (float)num2 / (float)num;
				float num4 = (float)(num2 + 1) / (float)num;
				num2++;
				quad.Uv = new float[8] { 1f, num4, 0f, num4, 0f, num3, 1f, num3 };
				quad.Rgba = new byte[16];
				quad.Rgba.Fill(byte.MaxValue);
				Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
				switch (cardinal.Index)
				{
				case 0:
					rotY = 90f;
					break;
				case 1:
					rotY = 45f;
					break;
				case 2:
					rotY = 0f;
					break;
				case 3:
					rotY = 315f;
					break;
				case 4:
					rotY = 270f;
					break;
				case 5:
					rotY = 225f;
					break;
				case 6:
					rotY = 180f;
					break;
				case 7:
					rotY = 135f;
					break;
				}
				quad.Translate(1.6f, 0f, 0.375f);
				MeshData meshData2 = quad.Clone();
				meshData2.Scale(origin, 0.5f * QuadWidth, 0.4f * QuadHeight, 0.5f * QuadWidth);
				meshData2.Rotate(origin, 0f, rotY * ((float)Math.PI / 180f), 0f);
				meshData2.Translate(0f, 1.39f, 0f);
				meshData.AddMeshData(meshData2);
				MeshData meshData3 = quad;
				meshData3.Uv = new float[8] { 0f, num4, 1f, num4, 1f, num3, 0f, num3 };
				meshData3.Translate(0f, 0f, 0.26f);
				meshData3.Scale(origin, 0.5f * QuadWidth, 0.4f * QuadHeight, 0.5f * QuadWidth);
				meshData3.Rotate(origin, 0f, rotY * ((float)Math.PI / 180f), 0f);
				meshData3.Translate(0f, 1.39f, 0f);
				meshData.AddMeshData(meshData3);
			}
		}
		quadModelRef?.Dispose();
		quadModelRef = api.Render.UploadMesh(meshData);
	}

	public virtual void SetNewText(string[] textByCardinal, int color)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		this.textByCardinal = textByCardinal;
		font.WithColor(ColorUtil.ToRGBADoubles(color));
		font.UnscaledFontsize = fontSize / (double)RuntimeEnv.GUIScale;
		int num = 0;
		for (int i = 0; i < textByCardinal.Length; i++)
		{
			if (textByCardinal[i].Length > 0)
			{
				num++;
			}
		}
		if (num == 0)
		{
			loadedTexture?.Dispose();
			loadedTexture = null;
			return;
		}
		ImageSurface val = new ImageSurface((Format)0, TextWidth, TextHeight * num);
		Context val2 = new Context((Surface)(object)val);
		font.SetupContext(val2);
		int num2 = 0;
		for (int j = 0; j < textByCardinal.Length; j++)
		{
			if (textByCardinal[j].Length > 0)
			{
				TextExtents textExtents = font.GetTextExtents(textByCardinal[j]);
				double width = ((TextExtents)(ref textExtents)).Width;
				double num3 = ((double)TextWidth - width) / 2.0;
				double num4 = num2 * TextHeight;
				FontExtents fontExtents = val2.FontExtents;
				val2.MoveTo(num3, num4 + ((FontExtents)(ref fontExtents)).Ascent);
				val2.ShowText(textByCardinal[j]);
				num2++;
			}
		}
		if (loadedTexture == null)
		{
			loadedTexture = new LoadedTexture(api);
		}
		api.Gui.LoadOrUpdateCairoTexture(val, linearMag: true, ref loadedTexture);
		((Surface)val).Dispose();
		val2.Dispose();
		genMesh();
	}

	public virtual void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (loadedTexture != null)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
			IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			standardShaderProgram.Tex2D = loadedTexture.TextureId;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Values;
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
