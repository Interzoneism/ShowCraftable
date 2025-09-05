using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EchoChamberRenderer : EntityRenderer, ITexPositionSource
{
	protected int renderRange = 999;

	protected MeshRef meshRef1;

	protected MeshRef meshRef2;

	protected MeshRef meshRef3;

	protected Vec4f color = new Vec4f(1f, 1f, 1f, 1f);

	public Vec3f OriginPos = new Vec3f();

	public float[] ModelMat = Mat4f.Create();

	protected float[] tmpMvMat = Mat4f.Create();

	protected EntityAgent eagent;

	protected bool shapeFresh;

	private Vec4f lightrgbs;

	private LoadedTexture echoTexture1;

	private LoadedTexture echoTexture2;

	private LoadedTexture echoTexture3;

	private Size2i echoTextureSize = new Size2i(256, 256);

	private TextureAtlasPosition texPos = new TextureAtlasPosition
	{
		x1 = 0f,
		y1 = 0f,
		y2 = 20f,
		x2 = 20f
	};

	protected TextureAtlasPosition skinTexPos;

	private bool loaded;

	public Size2i AtlasSize => echoTextureSize;

	public virtual TextureAtlasPosition this[string textureCode] => texPos;

	public EchoChamberRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		eagent = entity as EntityAgent;
		api.Event.ReloadShapes += MarkShapeModified;
	}

	public virtual void MarkShapeModified()
	{
		shapeFresh = false;
	}

	public override void OnEntityLoaded()
	{
		loaded = true;
		MarkShapeModified();
	}

	public virtual void TesselateShape()
	{
		if (loaded)
		{
			shapeFresh = true;
			meshRef1?.Dispose();
			meshRef2?.Dispose();
			meshRef3?.Dispose();
			meshRef1 = tesselate(new CompositeShape
			{
				Base = new AssetLocation("entity/nonliving/echochamber1")
			}, new AssetLocation("textures/entity/nonliving/echochamber/echochamber1.png"), out echoTexture1);
			meshRef2 = tesselate(new CompositeShape
			{
				Base = new AssetLocation("entity/nonliving/echochamber2")
			}, new AssetLocation("textures/entity/nonliving/echochamber/echochamber2.png"), out echoTexture2);
			meshRef3 = tesselate(new CompositeShape
			{
				Base = new AssetLocation("entity/nonliving/echochamber3")
			}, new AssetLocation("textures/entity/nonliving/echochamber/echochamber3.png"), out echoTexture3);
		}
	}

	private MeshRef tesselate(CompositeShape compositeShape, AssetLocation textureLoc, out LoadedTexture partTexture)
	{
		partTexture = null;
		Shape entityShape = capi.Assets.TryGet(compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
		entity.OnTesselation(ref entityShape, compositeShape.ToString());
		MeshData modeldata;
		try
		{
			TesselationMetaData meta = new TesselationMetaData
			{
				QuantityElements = compositeShape.QuantityElements,
				SelectiveElements = compositeShape.SelectiveElements,
				IgnoreElements = compositeShape.IgnoreElements,
				TexSource = this,
				WithJointIds = false,
				WithDamageEffect = true,
				TypeForLogging = "entity",
				Rotation = new Vec3f(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ)
			};
			capi.Tesselator.TesselateShape(meta, entityShape, out modeldata);
			modeldata.Translate(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
		}
		catch (Exception e)
		{
			capi.World.Logger.Fatal("Failed tesselating entity {0} with id {1}. Entity will probably be invisible!.", entity.Code, entity.EntityId);
			capi.World.Logger.Fatal(e);
			return null;
		}
		partTexture?.Dispose();
		partTexture = new LoadedTexture(capi);
		byte[] array = capi.Assets.TryGet(textureLoc)?.Data;
		if (array == null)
		{
			return null;
		}
		BitmapRef bitmapRef = capi.Render.BitmapCreateFromPng(array);
		capi.Render.LoadTexture(bitmapRef, ref partTexture, linearMag: false, 2, generateMipmaps: true);
		bitmapRef.Dispose();
		return capi.Render.UploadMesh(modeldata);
	}

	public override void BeforeRender(float dt)
	{
		if (!shapeFresh)
		{
			TesselateShape();
		}
		lightrgbs = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.Y, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
		if (entity.SelectionBox.Y2 > 1f)
		{
			Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.Y + 1, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
			if (lightRGBs.W > lightrgbs.W)
			{
				lightrgbs = lightRGBs;
			}
		}
	}

	public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (meshRef1 != null && !isShadowPass)
		{
			loadModelMatrix(entity);
			Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
			OriginPos.Set((float)(entity.Pos.X - cameraPos.X), (float)(entity.Pos.Y - cameraPos.Y), (float)(entity.Pos.Z - cameraPos.Z));
			capi.Render.GlDisableCullFace();
			capi.Render.GlMatrixModeModelView();
			capi.Render.GlPushMatrix();
			capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
			capi.Render.GlToggleBlend(blend: true);
			IShaderProgram program = capi.Shader.GetProgram(26);
			program.Use();
			program.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
			program.Uniform("rgbaFogIn", capi.Render.FogColor);
			program.Uniform("fogMinIn", capi.Render.FogMin);
			program.Uniform("fogDensityIn", capi.Render.FogDensity);
			program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			program.Uniform("alphaTest", 0.05f);
			program.Uniform("lightPosition", capi.Render.ShaderUniforms.LightPosition3D);
			program.Uniform("rgbaLightIn", lightrgbs);
			program.Uniform("extraGlow", entity.Properties.Client.GlowLevel);
			program.UniformMatrix("modelMatrix", ModelMat);
			program.UniformMatrix("viewMatrix", capi.Render.CurrentModelviewMatrix);
			program.Uniform("addRenderFlags", 0);
			program.Uniform("windWaveIntensity", 0f);
			program.Uniform("entityId", (int)entity.EntityId);
			program.Uniform("glitchFlicker", 0);
			program.Uniform("frostAlpha", 0f);
			program.Uniform("waterWaveCounter", 0f);
			program.UBOs["Animation"].Update((object)entity.AnimManager.Animator.Matrices, 0, entity.AnimManager.Animator.MaxJointId * 16 * 4);
			color[0] = (float)((entity.RenderColor >> 16) & 0xFF) / 255f;
			color[1] = (float)((entity.RenderColor >> 8) & 0xFF) / 255f;
			color[2] = (float)(entity.RenderColor & 0xFF) / 255f;
			color[3] = (float)((entity.RenderColor >> 24) & 0xFF) / 255f;
			program.Uniform("renderColor", color);
			capi.Render.GlPopMatrix();
			if (meshRef1 != null)
			{
				program.BindTexture2D("entityTex", echoTexture1.TextureId, 0);
				capi.Render.RenderMesh(meshRef1);
				program.BindTexture2D("entityTex", echoTexture2.TextureId, 0);
				capi.Render.RenderMesh(meshRef2);
				program.BindTexture2D("entityTex", echoTexture3.TextureId, 0);
				capi.Render.RenderMesh(meshRef3);
			}
			program.Stop();
		}
	}

	public void loadModelMatrix(Entity entity)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));
		float size = entity.Properties.Client.Size;
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { size, size, size });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	public override void Dispose()
	{
		meshRef1?.Dispose();
		meshRef1 = null;
		meshRef2?.Dispose();
		meshRef2 = null;
		meshRef3?.Dispose();
		meshRef3 = null;
		capi.Event.ReloadShapes -= MarkShapeModified;
	}
}
