using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorHideWaterSurface : EntityBehavior, IRenderer, IDisposable, ITexPositionSource
{
	private MultiTextureMeshRef meshref;

	private ICoreClientAPI capi;

	private string hideWaterElement;

	private Size2i dummysize = new Size2i(2048, 2048);

	private TextureAtlasPosition dummyPos = new TextureAtlasPosition
	{
		x1 = 0f,
		y1 = 0f,
		x2 = 1f,
		y2 = 1f
	};

	protected float[] tmpMvMat = Mat4f.Create();

	public double RenderOrder => 0.36;

	public int RenderRange => 99;

	public Size2i AtlasSize => dummysize;

	public TextureAtlasPosition this[string textureCode] => dummyPos;

	public EntityBehaviorHideWaterSurface(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		capi = entity.World.Api as ICoreClientAPI;
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "re-ebhhws");
		hideWaterElement = attributes["hideWaterElement"].AsString();
	}

	public override void OnTesselated()
	{
		CompositeShape shape = entity.Properties.Client.Shape;
		Shape loadedShapeForEntity = entity.Properties.Client.LoadedShapeForEntity;
		try
		{
			TesselationMetaData tesselationMetaData = new TesselationMetaData();
			tesselationMetaData.QuantityElements = shape.QuantityElements;
			tesselationMetaData.SelectiveElements = new string[1] { hideWaterElement };
			tesselationMetaData.TexSource = this;
			tesselationMetaData.WithJointIds = true;
			tesselationMetaData.WithDamageEffect = true;
			tesselationMetaData.TypeForLogging = "entity";
			tesselationMetaData.Rotation = new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ);
			TesselationMetaData meta = tesselationMetaData;
			capi.Tesselator.TesselateShape(meta, loadedShapeForEntity, out var modeldata);
			modeldata.Translate(shape.offsetX, shape.offsetY, shape.offsetZ);
			meshref?.Dispose();
			meshref = capi.Render.UploadMultiTextureMesh(modeldata);
		}
		catch (Exception e)
		{
			capi.World.Logger.Fatal("Failed tesselating entity {0} with id {1}. Entity will probably be invisible!.", entity.Code, entity.EntityId);
			capi.World.Logger.Fatal(e);
		}
	}

	public void Dispose()
	{
		meshref?.Dispose();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		meshref?.Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (meshref != null && entity.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
		{
			capi.Render.GLDepthMask(on: true);
			IShaderProgram program = capi.Shader.GetProgram(capi.Render.UseSSBOs ? 42 : 32);
			program.Use();
			float[] modelMat = entityShapeRenderer.ModelMat;
			Mat4f.Mul(tmpMvMat, capi.Render.CurrentProjectionMatrix, capi.Render.CameraMatrixOriginf);
			Mat4f.Mul(tmpMvMat, tmpMvMat, modelMat);
			program.BindTexture2D("tex2d", 0, 0);
			program.UniformMatrix("mvpMatrix", tmpMvMat);
			program.Uniform("origin", new Vec3f(0f, 0f, 0f));
			capi.Render.RenderMultiTextureMesh(meshref, "tex2d");
			program.Stop();
			capi.Render.GLDepthMask(on: false);
			capi.Render.GLEnableDepthTest();
		}
	}

	public override string PropertyName()
	{
		return "hidewatersurface";
	}
}
