using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityItemRenderer : EntityRenderer
{
	public static bool RunWittySkipRenderAlgorithm;

	public static BlockPos LastPos = new BlockPos();

	public static int LastCollectibleId;

	public static int RenderCount;

	public static int RenderModulo;

	private EntityItem entityitem;

	private long touchGroundMS;

	public float[] ModelMat = Mat4f.Create();

	private float scaleRand;

	private float yRotRand;

	private Vec3d lerpedPos = new Vec3d();

	private ItemSlot inslot;

	private float accum;

	private Vec4f particleOutTransform = new Vec4f();

	private Vec4f glowRgb = new Vec4f();

	private bool rotateWhenFalling;

	private float xangle;

	private float yangle;

	private float zangle;

	public EntityItemRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		entityitem = (EntityItem)entity;
		inslot = entityitem.Slot;
		rotateWhenFalling = inslot.Itemstack?.Collectible?.Attributes?["rotateWhenFalling"].AsBool(defaultValue: true) ?? true;
		scaleRand = (float)api.World.Rand.NextDouble() / 20f - 0.025f;
		touchGroundMS = entityitem.itemSpawnedMilliseconds - api.World.Rand.Next(5000);
		yRotRand = (float)api.World.Rand.NextDouble() * ((float)Math.PI * 2f);
		lerpedPos = entity.Pos.XYZ;
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (isShadowPass && !entity.IsRendered)
		{
			return;
		}
		if (RunWittySkipRenderAlgorithm)
		{
			int num = (int)entity.Pos.X;
			int num2 = (int)entity.Pos.Y;
			int num3 = (int)entity.Pos.Z;
			int num4 = ((entityitem.Itemstack.Class != EnumItemClass.Block) ? 1 : (-1)) * entityitem.Itemstack.Id;
			if (LastPos.X == num && LastPos.Y == num2 && LastPos.Z == num3 && LastCollectibleId == num4)
			{
				if (entity.EntityId % RenderModulo != 0L)
				{
					return;
				}
			}
			else
			{
				LastPos.Set(num, num2, num3);
			}
			LastCollectibleId = num4;
		}
		IRenderAPI render = capi.Render;
		lerpedPos.X += (entity.Pos.X - lerpedPos.X) * 22.0 * (double)dt;
		lerpedPos.Y += (entity.Pos.InternalY - lerpedPos.Y) * 22.0 * (double)dt;
		lerpedPos.Z += (entity.Pos.Z - lerpedPos.Z) * 22.0 * (double)dt;
		ItemRenderInfo itemStackRenderInfo = render.GetItemStackRenderInfo(inslot, EnumItemRenderTarget.Ground, dt);
		if (itemStackRenderInfo.ModelRef == null || itemStackRenderInfo.Transform == null)
		{
			return;
		}
		IStandardShaderProgram standardShaderProgram = null;
		LoadModelMatrix(itemStackRenderInfo, isShadowPass, dt);
		string textureSampleName = "tex";
		if (isShadowPass)
		{
			textureSampleName = "tex2d";
			float[] array = Mat4f.Mul(ModelMat, capi.Render.CurrentModelviewMatrix, ModelMat);
			Mat4f.Mul(array, capi.Render.CurrentProjectionMatrix, array);
			capi.Render.CurrentActiveShader.UniformMatrix("mvpMatrix", array);
			capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f());
		}
		else
		{
			standardShaderProgram = render.StandardShader;
			standardShaderProgram.Use();
			standardShaderProgram.RgbaTint = (entity.Swimming ? new Vec4f(0.5f, 0.5f, 0.5f, 1f) : ColorUtil.WhiteArgbVec);
			standardShaderProgram.DontWarpVertices = 0;
			standardShaderProgram.NormalShaded = 1;
			standardShaderProgram.AlphaTest = itemStackRenderInfo.AlphaTest;
			standardShaderProgram.DamageEffect = itemStackRenderInfo.DamageEffect;
			if (entity.Swimming)
			{
				standardShaderProgram.AddRenderFlags = (int)(((entityitem.Itemstack.Collectible.MaterialDensity <= 1000) ? 1u : 0u) << 12);
				standardShaderProgram.WaterWaveCounter = capi.Render.ShaderUniforms.WaterWaveCounter;
			}
			else
			{
				standardShaderProgram.AddRenderFlags = 0;
			}
			standardShaderProgram.OverlayOpacity = itemStackRenderInfo.OverlayOpacity;
			if (itemStackRenderInfo.OverlayTexture != null && itemStackRenderInfo.OverlayOpacity > 0f)
			{
				standardShaderProgram.Tex2dOverlay2D = itemStackRenderInfo.OverlayTexture.TextureId;
				standardShaderProgram.OverlayTextureSize = new Vec2f(itemStackRenderInfo.OverlayTexture.Width, itemStackRenderInfo.OverlayTexture.Height);
				standardShaderProgram.BaseTextureSize = new Vec2f(itemStackRenderInfo.TextureSize.Width, itemStackRenderInfo.TextureSize.Height);
				TextureAtlasPosition textureAtlasPosition = render.GetTextureAtlasPosition(entityitem.Itemstack);
				standardShaderProgram.BaseUvOrigin = new Vec2f(textureAtlasPosition.x1, textureAtlasPosition.y1);
			}
			BlockPos asBlockPos = entityitem.Pos.AsBlockPos;
			Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs(asBlockPos.X, asBlockPos.InternalY, asBlockPos.Z);
			int num5 = (int)entityitem.Itemstack.Collectible.GetTemperature(capi.World, entityitem.Itemstack);
			float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num5);
			int num6 = GameMath.Clamp((num5 - 550) / 2, 0, 255);
			glowRgb.R = incandescenceColorAsColor4f[0];
			glowRgb.G = incandescenceColorAsColor4f[1];
			glowRgb.B = incandescenceColorAsColor4f[2];
			glowRgb.A = (float)num6 / 255f;
			standardShaderProgram.ExtraGlow = num6;
			standardShaderProgram.RgbaAmbientIn = render.AmbientColor;
			standardShaderProgram.RgbaLightIn = lightRGBs;
			standardShaderProgram.RgbaGlowIn = glowRgb;
			standardShaderProgram.RgbaFogIn = render.FogColor;
			standardShaderProgram.FogMinIn = render.FogMin;
			standardShaderProgram.FogDensityIn = render.FogDensity;
			standardShaderProgram.ExtraGodray = 0f;
			standardShaderProgram.NormalShaded = (itemStackRenderInfo.NormalShaded ? 1 : 0);
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ModelMatrix = ModelMat;
			ItemStack itemstack = entityitem.Itemstack;
			AdvancedParticleProperties[] array2 = itemstack.Block?.ParticleProperties;
			if (itemstack.Block != null && !capi.IsGamePaused)
			{
				Mat4f.MulWithVec4(ModelMat, new Vec4f(itemstack.Block.TopMiddlePos.X, itemstack.Block.TopMiddlePos.Y - 0.4f, itemstack.Block.TopMiddlePos.Z - 0.5f, 0f), particleOutTransform);
				accum += dt;
				if (array2 != null && array2.Length != 0 && accum > 0.025f)
				{
					accum %= 0.025f;
					foreach (AdvancedParticleProperties advancedParticleProperties in array2)
					{
						advancedParticleProperties.basePos.X = (double)particleOutTransform.X + entity.Pos.X;
						advancedParticleProperties.basePos.Y = (double)particleOutTransform.Y + entity.Pos.InternalY;
						advancedParticleProperties.basePos.Z = (double)particleOutTransform.Z + entity.Pos.Z;
						entityitem.World.SpawnParticles(advancedParticleProperties);
					}
				}
			}
		}
		if (!itemStackRenderInfo.CullFaces)
		{
			render.GlDisableCullFace();
		}
		render.RenderMultiTextureMesh(itemStackRenderInfo.ModelRef, textureSampleName);
		if (!itemStackRenderInfo.CullFaces)
		{
			render.GlEnableCullFace();
		}
		if (!isShadowPass)
		{
			standardShaderProgram.AddRenderFlags = 0;
			standardShaderProgram.DamageEffect = 0f;
			standardShaderProgram.Stop();
		}
	}

	private void LoadModelMatrix(ItemRenderInfo renderInfo, bool isShadowPass, float dt)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)(lerpedPos.X - entityPlayer.CameraPos.X), (float)(lerpedPos.Y - entityPlayer.CameraPos.Y), (float)(lerpedPos.Z - entityPlayer.CameraPos.Z));
		float num = 0.2f * renderInfo.Transform.ScaleXYZ.X;
		float num2 = 0.2f * renderInfo.Transform.ScaleXYZ.Y;
		float num3 = 0.2f * renderInfo.Transform.ScaleXYZ.Z;
		float num4 = 0f;
		float num5 = 0f;
		if (!isShadowPass)
		{
			long elapsedMilliseconds = capi.World.ElapsedMilliseconds;
			bool flag = !entity.Collided && !entity.Swimming && !capi.IsGamePaused;
			if (!flag)
			{
				touchGroundMS = elapsedMilliseconds;
			}
			if (entity.Collided)
			{
				xangle *= 0.55f;
				yangle *= 0.55f;
				zangle *= 0.55f;
			}
			else if (rotateWhenFalling)
			{
				float num6 = Math.Min(1L, (elapsedMilliseconds - touchGroundMS) / 200);
				float num7 = (flag ? (1000f * dt / 7f * num6) : 0f);
				yangle += num7;
				xangle += num7;
				zangle += num7;
			}
			if (entity.Swimming)
			{
				float num8 = 1f;
				if (entityitem.Itemstack.Collectible.MaterialDensity > 1000)
				{
					num4 = GameMath.Sin((float)((double)elapsedMilliseconds / 1000.0)) / 50f;
					num5 = (0f - GameMath.Sin((float)((double)elapsedMilliseconds / 3000.0))) / 50f;
					num8 = 0.1f;
				}
				xangle = GameMath.Sin((float)((double)elapsedMilliseconds / 1000.0)) * 8f * num8;
				yangle = GameMath.Cos((float)((double)elapsedMilliseconds / 2000.0)) * 3f * num8;
				zangle = (0f - GameMath.Sin((float)((double)elapsedMilliseconds / 3000.0))) * 8f * num8;
			}
		}
		ModelTransform transform = renderInfo.Transform;
		FastVec3f translation = transform.Translation;
		FastVec3f rotation = transform.Rotation;
		Mat4f.Translate(ModelMat, ModelMat, num4 + translation.X, translation.Y, num5 + translation.Z);
		Mat4f.Scale(ModelMat, ModelMat, new float[3]
		{
			num + scaleRand,
			num2 + scaleRand,
			num3 + scaleRand
		});
		Mat4f.RotateY(ModelMat, ModelMat, (float)Math.PI / 180f * (rotation.Y + yangle) + (transform.Rotate ? yRotRand : 0f));
		Mat4f.RotateZ(ModelMat, ModelMat, (float)Math.PI / 180f * (rotation.Z + zangle));
		Mat4f.RotateX(ModelMat, ModelMat, (float)Math.PI / 180f * (rotation.X + xangle));
		Mat4f.Translate(ModelMat, ModelMat, 0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
	}

	public override void Dispose()
	{
	}
}
