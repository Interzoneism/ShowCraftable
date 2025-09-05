using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityShapeRenderer : EntityRenderer, ITexPositionSource
{
	protected long listenerId;

	protected LoadedTexture debugTagTexture;

	protected MultiTextureMeshRef meshRefOpaque;

	protected Vec4f color = new Vec4f(1f, 1f, 1f, 1f);

	protected long lastDebugInfoChangeMs;

	protected bool isSpectator;

	protected IClientPlayer player;

	public float bodyYawLerped;

	public Vec3f OriginPos = new Vec3f();

	public float[] ModelMat = Mat4f.Create();

	protected float[] tmpMvMat = Mat4f.Create();

	protected Matrixf ItemModelMat = new Matrixf();

	public bool DoRenderHeldItem;

	public int AddRenderFlags;

	public double WindWaveIntensity = 1.0;

	public bool glitchFlicker;

	public bool frostable;

	public float frostAlpha;

	public float targetFrostAlpha;

	public OnGetFrostAlpha getFrostAlpha;

	public float frostAlphaAccum;

	protected List<MessageTexture> messageTextures;

	protected EntityAgent eagent;

	public CompositeShape OverrideCompositeShape;

	public Shape OverrideEntityShape;

	public string[] OverrideSelectiveElements;

	public bool glitchAffected;

	protected IInventory gearInv;

	protected ITexPositionSource defaultTexSource;

	protected Vec4f lightrgbs;

	protected float intoxIntensity;

	public TextureAtlasPosition skinTexPos;

	protected bool loaded;

	private float accum;

	protected float[] pMatrixHandFov;

	protected float[] pMatrixNormalFov;

	public double stepPitch;

	private double prevY;

	private double prevYAccum;

	public float xangle;

	public float yangle;

	public float zangle;

	private IMountable ims;

	private float stepingAccum;

	private float fallingAccum;

	public float targetSwivelRad;

	public float nowSwivelRad;

	protected double prevAngleSwing;

	protected double prevPosXSwing;

	protected double prevPosZSwing;

	private float swivelaccum;

	public long LastJumpMs;

	public bool shouldSwivelFromMotion = true;

	public float maxSwivelAngle = 22.91831f;

	public virtual bool DisplayChatMessages { get; set; }

	public Size2i AtlasSize => capi.EntityTextureAtlas.Size;

	public virtual TextureAtlasPosition this[string textureCode] => defaultTexSource[textureCode] ?? skinTexPos;

	public EntityShapeRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		EntityShapeRenderer entityShapeRenderer = this;
		eagent = entity as EntityAgent;
		DoRenderHeldItem = true;
		glitchAffected = true;
		glitchFlicker = entity.Properties.Attributes?["glitchFlicker"].AsBool() ?? false;
		frostable = entity.Properties.Attributes?["frostable"].AsBool(defaultValue: true) ?? true;
		shouldSwivelFromMotion = entity.Properties.Attributes?["shouldSwivelFromMotion"].AsBool(defaultValue: true) ?? true;
		maxSwivelAngle = entity.Properties.Attributes?["maxSwivelAngle"].AsFloat(22.91831f) ?? 22.91831f;
		frostAlphaAccum = (float)api.World.Rand.NextDouble();
		listenerId = api.Event.RegisterGameTickListener(UpdateDebugInfo, 250);
		OnDebugInfoChanged();
		if (DisplayChatMessages)
		{
			messageTextures = new List<MessageTexture>();
			api.Event.ChatMessage += OnChatMessage;
		}
		api.Event.ReloadShapes += entity.MarkShapeModified;
		getFrostAlpha = delegate
		{
			BlockPos asBlockPos = entity.Pos.AsBlockPos;
			ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(asBlockPos);
			if (climateAt == null)
			{
				return entityShapeRenderer.targetFrostAlpha;
			}
			float num = 1f - GameMath.Clamp((float)(api.World.BlockAccessor.GetDistanceToRainFall(asBlockPos, 5) - 2) / 3f, 0f, 1f);
			float num2 = GameMath.Clamp((Math.Max(0f, 0f - climateAt.Temperature) - 2f) / 5f, 0f, 1f) * num;
			if (num2 > 0f)
			{
				float num3 = Math.Max(api.World.BlockAccessor.GetClimateAt(asBlockPos, EnumGetClimateMode.ForSuppliedDateValues, api.World.Calendar.TotalDays - (double)(4f / api.World.Calendar.HoursPerDay)).Rainfall, climateAt.Rainfall);
				num2 *= num3;
			}
			return Math.Max(0f, num2);
		};
	}

	public override void OnEntityLoaded()
	{
		loaded = true;
		prevY = entity.Pos.Y;
		prevPosXSwing = entity.Pos.X;
		prevPosZSwing = entity.Pos.Z;
	}

	protected void OnChatMessage(int groupId, string message, EnumChatType chattype, string data)
	{
		if (data == null || !data.Contains("from:") || !(entity.Pos.SquareDistanceTo(capi.World.Player.Entity.Pos.XYZ) < 400.0) || message.Length <= 0)
		{
			return;
		}
		string[] array = data.Split(new char[1] { ',' }, 2);
		if (array.Length < 2)
		{
			return;
		}
		string[] array2 = array[0].Split(new char[1] { ':' }, 2);
		string[] array3 = array[1].Split(new char[1] { ':' }, 2);
		if (!(array2[0] != "from"))
		{
			int.TryParse(array2[1], out var result);
			if (entity.EntityId == result)
			{
				message = array3[1];
				message = message.Replace("&lt;", "<").Replace("&gt;", ">");
				LoadedTexture tex = capi.Gui.TextTexture.GenTextTexture(message, new CairoFont(25.0, GuiStyle.StandardFontName, ColorUtil.WhiteArgbDouble), 350, new TextBackground
				{
					FillColor = GuiStyle.DialogLightBgColor,
					Padding = 3,
					Radius = GuiStyle.ElementBGRadius
				}, EnumTextOrientation.Center);
				messageTextures.Insert(0, new MessageTexture
				{
					tex = tex,
					message = message,
					receivedTime = capi.World.ElapsedMilliseconds
				});
			}
		}
	}

	public virtual void TesselateShape()
	{
		if (loaded)
		{
			ims = entity.GetInterface<IMountable>();
			TesselateShape(onMeshReady);
		}
	}

	protected virtual void onMeshReady(MeshData meshData)
	{
		if (meshRefOpaque != null)
		{
			meshRefOpaque.Dispose();
			meshRefOpaque = null;
		}
		if (!capi.IsShuttingDown && meshData.VerticesCount > 0)
		{
			meshRefOpaque = capi.Render.UploadMultiTextureMesh(meshData);
		}
	}

	public virtual void TesselateShape(Action<MeshData> onMeshDataReady, string[] overrideSelectiveElements = null)
	{
		if (!loaded)
		{
			return;
		}
		CompositeShape compositeShape = ((OverrideCompositeShape != null) ? OverrideCompositeShape : entity.Properties.Client.Shape);
		Shape entityShape = ((OverrideEntityShape != null) ? OverrideEntityShape : entity.Properties.Client.LoadedShapeForEntity);
		if (entityShape == null)
		{
			return;
		}
		entity.OnTesselation(ref entityShape, compositeShape.Base.ToString());
		defaultTexSource = GetTextureSource();
		string[] ovse = overrideSelectiveElements ?? OverrideSelectiveElements;
		TyronThreadPool.QueueTask(delegate
		{
			MeshData meshdata;
			if (entity.Properties.Client.Shape.VoxelizeTexture)
			{
				int num = entity.WatchedAttributes.GetInt("textureIndex");
				TextureAtlasPosition atlasPos = defaultTexSource["all"];
				CompositeTexture firstTexture = entity.Properties.Client.FirstTexture;
				CompositeTexture[] alternates = firstTexture.Alternates;
				CompositeTexture texture = ((num == 0) ? firstTexture : alternates[num % alternates.Length]);
				meshdata = capi.Tesselator.VoxelizeTexture(texture, capi.EntityTextureAtlas.Size, atlasPos);
				for (int i = 0; i < meshdata.xyz.Length; i += 3)
				{
					meshdata.xyz[i] -= 0.125f;
					meshdata.xyz[i + 1] -= 0.5f;
					meshdata.xyz[i + 2] += 0.0625f;
				}
			}
			else
			{
				try
				{
					TesselationMetaData meta = new TesselationMetaData
					{
						QuantityElements = compositeShape.QuantityElements,
						SelectiveElements = (ovse ?? compositeShape.SelectiveElements),
						IgnoreElements = compositeShape.IgnoreElements,
						TexSource = this,
						WithJointIds = true,
						WithDamageEffect = true,
						TypeForLogging = "entity",
						Rotation = new Vec3f(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ)
					};
					capi.Tesselator.TesselateShape(meta, entityShape, out meshdata);
					meshdata.Translate(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
				}
				catch (Exception e)
				{
					capi.World.Logger.Fatal("Failed tesselating entity {0} with id {1}. Entity will probably be invisible!.", entity.Code, entity.EntityId);
					capi.World.Logger.Fatal(e);
					return;
				}
			}
			capi.Event.EnqueueMainThreadTask(delegate
			{
				onMeshDataReady(meshdata);
				entity.OnTesselated();
			}, "uploadentitymesh");
			capi.TesselatorManager.ThreadDispose();
		});
	}

	protected virtual ITexPositionSource GetTextureSource()
	{
		return entity.GetTextureSource();
	}

	protected void UpdateDebugInfo(float dt)
	{
		OnDebugInfoChanged();
		entity.DebugAttributes.MarkClean();
	}

	protected void OnDebugInfoChanged()
	{
		bool flag = capi.Settings.Bool["showEntityDebugInfo"];
		if (flag && !entity.DebugAttributes.AllDirty && !entity.DebugAttributes.PartialDirty && debugTagTexture != null)
		{
			return;
		}
		if (debugTagTexture != null)
		{
			if (flag && capi.World.Player.Entity.Pos.SquareDistanceTo(entity.Pos) > 225f && debugTagTexture.Width > 10)
			{
				return;
			}
			debugTagTexture.Dispose();
			debugTagTexture = null;
		}
		if (!flag)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, IAttribute> debugAttribute in entity.DebugAttributes)
		{
			stringBuilder.AppendLine(debugAttribute.Key + ": " + debugAttribute.Value.ToString());
		}
		debugTagTexture = capi.Gui.TextTexture.GenUnscaledTextTexture(stringBuilder.ToString(), new CairoFont(20.0, GuiStyle.StandardFontName, ColorUtil.WhiteArgbDouble), new TextBackground
		{
			FillColor = GuiStyle.DialogDefaultBgColor,
			Padding = 3,
			Radius = GuiStyle.ElementBGRadius
		});
		lastDebugInfoChangeMs = entity.World.ElapsedMilliseconds;
	}

	public override void BeforeRender(float dt)
	{
		if (!entity.ShapeFresh)
		{
			TesselateShape();
			capi.World.FrameProfiler.Mark("esr-tesseleateshape");
		}
		lightrgbs = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.InternalY, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
		if (entity.SelectionBox.Y2 > 1f)
		{
			Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.InternalY + 1, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
			if (lightRGBs.W > lightrgbs.W)
			{
				lightrgbs = lightRGBs;
			}
		}
		if (meshRefOpaque == null)
		{
			return;
		}
		if (player == null && entity is EntityPlayer)
		{
			player = capi.World.PlayerByUid((entity as EntityPlayer).PlayerUID) as IClientPlayer;
		}
		if (capi.IsGamePaused)
		{
			return;
		}
		frostAlphaAccum += dt;
		if (frostAlphaAccum > 5f)
		{
			frostAlphaAccum = 0f;
			targetFrostAlpha = getFrostAlpha();
		}
		isSpectator = player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator;
		if (isSpectator)
		{
			return;
		}
		if (DisplayChatMessages && messageTextures.Count > 0)
		{
			MessageTexture messageTexture = messageTextures.Last();
			if (capi.World.ElapsedMilliseconds > messageTexture.receivedTime + 3500 + 100 * (messageTexture.message.Length - 10))
			{
				messageTextures.RemoveAt(messageTextures.Count - 1);
				messageTexture.tex.Dispose();
			}
		}
		determineSidewaysSwivel(dt);
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (!isSpectator)
		{
			loadModelMatrix(entity, dt, isShadowPass);
			Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
			OriginPos.Set((float)(entity.Pos.X - cameraPos.X), (float)(entity.Pos.InternalY - cameraPos.Y), (float)(entity.Pos.Z - cameraPos.Z));
			if (isShadowPass)
			{
				DoRender3DAfterOIT(dt, isShadowPass: true);
			}
			if (DoRenderHeldItem && !entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("lie") && !isSpectator)
			{
				RenderHeldItem(dt, isShadowPass, right: false);
				RenderHeldItem(dt, isShadowPass, right: true);
			}
		}
	}

	protected virtual IShaderProgram getReadyShader()
	{
		IStandardShaderProgram standardShader = capi.Render.StandardShader;
		standardShader.Use();
		return standardShader;
	}

	protected virtual void RenderHeldItem(float dt, bool isShadowPass, bool right)
	{
		ItemSlot itemSlot = ((!right) ? eagent?.LeftHandItemSlot : eagent?.RightHandItemSlot);
		ItemStack itemStack = itemSlot?.Itemstack;
		if (itemStack != null && !(itemSlot is ItemSlotSkill))
		{
			AttachmentPointAndPose attachmentPointAndPose = entity.AnimManager?.Animator?.GetAttachmentPointPose(right ? "RightHand" : "LeftHand");
			if (attachmentPointAndPose != null)
			{
				ItemRenderInfo itemStackRenderInfo = capi.Render.GetItemStackRenderInfo(itemSlot, right ? EnumItemRenderTarget.HandTp : EnumItemRenderTarget.HandTpOff, dt);
				RenderItem(dt, isShadowPass, itemStack, attachmentPointAndPose, itemStackRenderInfo);
			}
		}
	}

	protected virtual void RenderItem(float dt, bool isShadowPass, ItemStack stack, AttachmentPointAndPose apap, ItemRenderInfo renderInfo)
	{
		IRenderAPI render = capi.Render;
		AttachmentPoint attachPoint = apap.AttachPoint;
		IShaderProgram shaderProgram = null;
		if (renderInfo?.Transform == null || renderInfo.ModelRef == null)
		{
			return;
		}
		ModelTransform modelTransform = renderInfo.Transform.EnsureDefaultValues();
		FastVec3f origin = modelTransform.Origin;
		FastVec3f translation = modelTransform.Translation;
		FastVec3f rotation = modelTransform.Rotation;
		FastVec3f scaleXYZ = modelTransform.ScaleXYZ;
		ItemModelMat.Set(ModelMat).Mul(apap.AnimModelMatrix).Translate(origin.X, origin.Y, origin.Z)
			.Scale(scaleXYZ.X, scaleXYZ.Y, scaleXYZ.Z)
			.Translate(attachPoint.PosX / 16.0 + (double)translation.X, attachPoint.PosY / 16.0 + (double)translation.Y, attachPoint.PosZ / 16.0 + (double)translation.Z)
			.RotateX((float)(attachPoint.RotationX + (double)rotation.X) * ((float)Math.PI / 180f))
			.RotateY((float)(attachPoint.RotationY + (double)rotation.Y) * ((float)Math.PI / 180f))
			.RotateZ((float)(attachPoint.RotationZ + (double)rotation.Z) * ((float)Math.PI / 180f))
			.Translate(0f - origin.X, 0f - origin.Y, 0f - origin.Z);
		string textureSampleName = "tex";
		if (isShadowPass)
		{
			textureSampleName = "tex2d";
			render.CurrentActiveShader.BindTexture2D("tex2d", renderInfo.TextureId, 0);
			float[] array = Mat4f.Mul(ItemModelMat.Values, capi.Render.CurrentModelviewMatrix, ItemModelMat.Values);
			Mat4f.Mul(array, capi.Render.CurrentProjectionMatrix, array);
			capi.Render.CurrentActiveShader.UniformMatrix("mvpMatrix", array);
			capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f());
		}
		else
		{
			shaderProgram = getReadyShader();
			shaderProgram.Uniform("dontWarpVertices", 0);
			shaderProgram.Uniform("addRenderFlags", 0);
			shaderProgram.Uniform("normalShaded", 1);
			shaderProgram.Uniform("tempGlowMode", stack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0);
			shaderProgram.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
			shaderProgram.Uniform("alphaTest", renderInfo.AlphaTest);
			shaderProgram.Uniform("damageEffect", renderInfo.DamageEffect);
			shaderProgram.Uniform("overlayOpacity", renderInfo.OverlayOpacity);
			if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
			{
				shaderProgram.BindTexture2D("tex2dOverlay", renderInfo.OverlayTexture.TextureId, 1);
				shaderProgram.Uniform("overlayTextureSize", new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height));
				shaderProgram.Uniform("baseTextureSize", new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height));
				TextureAtlasPosition textureAtlasPosition = render.GetTextureAtlasPosition(stack);
				shaderProgram.Uniform("baseUvOrigin", new Vec2f(textureAtlasPosition.x1, textureAtlasPosition.y1));
			}
			int num = (int)stack.Collectible.GetTemperature(capi.World, stack);
			float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int num2 = GameMath.Clamp((num - 500) / 3, 0, 255);
			BakedCompositeTexture bakedCompositeTexture = (stack.Item?.FirstTexture ?? stack.Block?.FirstTextureInventory)?.Baked;
			Vec4f value = ((bakedCompositeTexture == null) ? new Vec4f(1f, 1f, 1f, 1f) : ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor(bakedCompositeTexture.TextureSubId)));
			shaderProgram.Uniform("averageColor", value);
			shaderProgram.Uniform("extraGlow", num2);
			shaderProgram.Uniform("rgbaAmbientIn", render.AmbientColor);
			shaderProgram.Uniform("rgbaLightIn", lightrgbs);
			shaderProgram.Uniform("rgbaGlowIn", new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f));
			shaderProgram.Uniform("rgbaFogIn", render.FogColor);
			shaderProgram.Uniform("fogMinIn", render.FogMin);
			shaderProgram.Uniform("fogDensityIn", render.FogDensity);
			shaderProgram.Uniform("normalShaded", renderInfo.NormalShaded ? 1 : 0);
			shaderProgram.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);
			shaderProgram.UniformMatrix("viewMatrix", render.CameraMatrixOriginf);
			shaderProgram.UniformMatrix("modelMatrix", ItemModelMat.Values);
		}
		if (!renderInfo.CullFaces)
		{
			render.GlDisableCullFace();
		}
		render.RenderMultiTextureMesh(renderInfo.ModelRef, textureSampleName);
		if (!isShadowPass)
		{
			shaderProgram.Uniform("tempGlowMode", 0);
		}
		if (!renderInfo.CullFaces)
		{
			render.GlEnableCullFace();
		}
		if (isShadowPass)
		{
			return;
		}
		shaderProgram.Uniform("damageEffect", 0f);
		shaderProgram.Stop();
		float num3 = Math.Max(0f, 1f - (float)capi.World.BlockAccessor.GetDistanceToRainFall(entity.Pos.AsBlockPos) / 5f);
		AdvancedParticleProperties[] array2 = stack.Collectible?.ParticleProperties;
		if (stack.Collectible == null || capi.IsGamePaused)
		{
			return;
		}
		Vec4f vec4f = ItemModelMat.TransformVector(new Vec4f(stack.Collectible.TopMiddlePos.X, stack.Collectible.TopMiddlePos.Y, stack.Collectible.TopMiddlePos.Z, 1f));
		if (pMatrixHandFov != null)
		{
			Vec4f vec = new Matrixf().Set(pMatrixHandFov).Mul(render.CameraMatrixOriginf).TransformVector(vec4f);
			vec4f = new Matrixf(render.CameraMatrixOriginf).Invert().Mul(new Matrixf(pMatrixNormalFov).Invert()).TransformVector(vec);
		}
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		accum += dt;
		if (array2 != null && array2.Length != 0 && accum > 0.05f)
		{
			accum %= 0.025f;
			foreach (AdvancedParticleProperties advancedParticleProperties in array2)
			{
				advancedParticleProperties.WindAffectednesAtPos = num3;
				advancedParticleProperties.WindAffectednes = num3;
				advancedParticleProperties.basePos.X = (double)vec4f.X + entityPlayer.CameraPos.X;
				advancedParticleProperties.basePos.Y = (double)vec4f.Y + entityPlayer.CameraPos.Y;
				advancedParticleProperties.basePos.Z = (double)vec4f.Z + entityPlayer.CameraPos.Z;
				eagent.World.SpawnParticles(advancedParticleProperties);
			}
		}
	}

	public override void RenderToGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
		loadModelMatrixForGui(entity, posX, posY, posZ, yawDelta, size);
		if (meshRefOpaque != null)
		{
			capi.Render.CurrentActiveShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			capi.Render.CurrentActiveShader.UniformMatrix("modelViewMatrix", GetModelMatrixForGui(dt, posX, posY, posZ, yawDelta, size));
			capi.Render.RenderMultiTextureMesh(meshRefOpaque, "tex2d");
		}
		if (!entity.ShapeFresh)
		{
			TesselateShape();
		}
	}

	protected virtual float[] GetModelMatrixForGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
		Mat4f.Mul(ModelMat, capi.Render.CurrentModelviewMatrix, ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, new float[3] { 0.5f, 0f, 0.5f });
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { 1f, 1f, -1f });
		Mat4f.Translate(ModelMat, ModelMat, new float[3] { -0.5f, 0f, -0.5f });
		return ModelMat;
	}

	public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
		if (!isSpectator && meshRefOpaque != null)
		{
			IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
			if (isShadowPass)
			{
				Mat4f.Mul(tmpMvMat, capi.Render.CurrentModelviewMatrix, ModelMat);
				currentActiveShader.UniformMatrix("modelViewMatrix", tmpMvMat);
			}
			else
			{
				frostAlpha += (targetFrostAlpha - frostAlpha) * dt / 6f;
				float value = (float)Math.Round(GameMath.Clamp(frostAlpha, 0f, 1f), 4);
				currentActiveShader.Uniform("rgbaLightIn", lightrgbs);
				currentActiveShader.Uniform("extraGlow", entity.Properties.Client.GlowLevel);
				currentActiveShader.UniformMatrix("modelMatrix", ModelMat);
				currentActiveShader.UniformMatrix("viewMatrix", capi.Render.CurrentModelviewMatrix);
				currentActiveShader.Uniform("addRenderFlags", AddRenderFlags);
				currentActiveShader.Uniform("windWaveIntensity", (float)WindWaveIntensity);
				currentActiveShader.Uniform("entityId", (int)entity.EntityId);
				currentActiveShader.Uniform("glitchFlicker", glitchFlicker ? 1 : 0);
				currentActiveShader.Uniform("frostAlpha", value);
				currentActiveShader.Uniform("waterWaveCounter", capi.Render.ShaderUniforms.WaterWaveCounter);
				color.R = (float)((entity.RenderColor >> 16) & 0xFF) / 255f;
				color.G = (float)((entity.RenderColor >> 8) & 0xFF) / 255f;
				color.B = (float)(entity.RenderColor & 0xFF) / 255f;
				color.A = (float)((entity.RenderColor >> 24) & 0xFF) / 255f;
				currentActiveShader.Uniform("renderColor", color);
				double val = entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
				double val2 = capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
				double num = Math.Min(val, val2);
				float value2 = (float)(glitchAffected ? Math.Max(0.0, 1.0 - 2.5 * num) : 0.0);
				currentActiveShader.Uniform("glitchEffectStrength", value2);
			}
			currentActiveShader.UBOs["Animation"].Update((object)entity.AnimManager.Animator.Matrices, 0, entity.AnimManager.Animator.MaxJointId * 16 * 4);
			if (meshRefOpaque != null)
			{
				capi.Render.RenderMultiTextureMesh(meshRefOpaque, "entityTex");
			}
		}
	}

	public override void DoRender2D(float dt)
	{
		if (isSpectator || (debugTagTexture == null && messageTextures == null))
		{
			return;
		}
		EntityPlayer obj = entity as EntityPlayer;
		if (obj != null && obj.ServerControls.Sneak && debugTagTexture == null)
		{
			return;
		}
		IRenderAPI render = capi.Render;
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Vec3d vec3d = MatrixToolsd.Project(getAboveHeadPosition(entityPlayer), render.PerspectiveProjectionMat, render.PerspectiveViewMat, render.FrameWidth, render.FrameHeight);
		if (vec3d.Z < 0.0)
		{
			return;
		}
		float val = 4f / Math.Max(1f, (float)vec3d.Z);
		float num = Math.Min(1f, val);
		if (num > 0.75f)
		{
			num = 0.75f + (num - 0.75f) / 2f;
		}
		float num2 = 0f;
		entityPlayer.Pos.SquareDistanceTo(entity.Pos);
		if (debugTagTexture != null)
		{
			float posX = (float)vec3d.X - num * (float)debugTagTexture.Width / 2f;
			float num3 = (float)render.FrameHeight - (float)vec3d.Y - (num2 + (float)debugTagTexture.Height) * Math.Max(0f, num);
			render.Render2DTexture(debugTagTexture.TextureId, posX, num3 - num2, num * (float)debugTagTexture.Width, num * (float)debugTagTexture.Height, 20f);
		}
		if (messageTextures == null)
		{
			return;
		}
		num2 += 0f;
		foreach (MessageTexture messageTexture in messageTextures)
		{
			num2 += (float)messageTexture.tex.Height * num + 4f;
			float posX2 = (float)vec3d.X - num * (float)messageTexture.tex.Width / 2f;
			float num4 = (float)vec3d.Y + num2;
			render.Render2DTexture(messageTexture.tex.TextureId, posX2, (float)render.FrameHeight - num4, num * (float)messageTexture.tex.Width, num * (float)messageTexture.tex.Height, 20f);
		}
	}

	public virtual Vec3d getAboveHeadPosition(EntityPlayer entityPlayer)
	{
		IMountableSeat mountableSeat = (entity as EntityAgent)?.MountedOn;
		IMountableSeat mountedOn = entityPlayer.MountedOn;
		Vec3d vec3d;
		if (mountableSeat?.MountSupplier != null && mountableSeat.MountSupplier == mountedOn?.MountSupplier)
		{
			Vec3d a = mountableSeat.SeatPosition.XYZ - mountedOn.SeatPosition.XYZ;
			vec3d = new Vec3d(entityPlayer.CameraPos.X + entityPlayer.LocalEyePos.X, entityPlayer.CameraPos.Y + 0.4 + entityPlayer.LocalEyePos.Y, entityPlayer.CameraPos.Z + entityPlayer.LocalEyePos.Z);
			vec3d.Add(a);
		}
		else
		{
			vec3d = new Vec3d(entity.Pos.X, entity.Pos.InternalY + (double)entity.SelectionBox.Y2 + 0.2, entity.Pos.Z);
		}
		double x = entity.SelectionBox.X2 - entity.OriginSelectionBox.X2;
		double z = entity.SelectionBox.Z2 - entity.OriginSelectionBox.Z2;
		vec3d.Add(x, 0.0, z);
		return vec3d;
	}

	public void loadModelMatrix(Entity entity, float dt, bool isShadowPass)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		IMountableSeat seatOfMountedEntity;
		if (ims != null && (seatOfMountedEntity = ims.GetSeatOfMountedEntity(entityPlayer)) != null)
		{
			Vec3d vec3d = seatOfMountedEntity.SeatPosition.XYZ - seatOfMountedEntity.MountSupplier.Position.XYZ;
			ModelMat = Mat4f.Translate(ModelMat, ModelMat, 0f - (float)vec3d.X, 0f - (float)vec3d.Y, 0f - (float)vec3d.Z);
		}
		else
		{
			seatOfMountedEntity = eagent?.MountedOn;
			if (seatOfMountedEntity != null)
			{
				if (entityPlayer.MountedOn != null && entityPlayer.MountedOn.Entity == eagent.MountedOn.Entity)
				{
					EntityPos seatPosition = entityPlayer.MountedOn.SeatPosition;
					Mat4f.Translate(ModelMat, ModelMat, (float)(seatOfMountedEntity.SeatPosition.X - seatPosition.X), (float)(seatOfMountedEntity.SeatPosition.InternalY - seatPosition.Y), (float)(seatOfMountedEntity.SeatPosition.Z - seatPosition.Z));
				}
				else
				{
					Mat4f.Translate(ModelMat, ModelMat, (float)(seatOfMountedEntity.SeatPosition.X - entityPlayer.CameraPos.X), (float)(seatOfMountedEntity.SeatPosition.InternalY - entityPlayer.CameraPos.Y), (float)(seatOfMountedEntity.SeatPosition.Z - entityPlayer.CameraPos.Z));
				}
			}
			else
			{
				Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));
			}
		}
		float num = entity.Properties.Client.Shape?.rotateX ?? 0f;
		float num2 = entity.Properties.Client.Shape?.rotateY ?? 0f;
		float num3 = entity.Properties.Client.Shape?.rotateZ ?? 0f;
		Mat4f.Translate(ModelMat, ModelMat, 0f, entity.SelectionBox.Y2 / 2f, 0f);
		if (!isShadowPass)
		{
			updateStepPitch(dt);
		}
		double[] array = Quaterniond.Create();
		float num4 = ((entity is EntityPlayer) ? 0f : entity.Pos.Pitch);
		float num5 = entity.Pos.Yaw + (num2 + 90f) * ((float)Math.PI / 180f);
		BlockFacing climbingOnFace = entity.ClimbingOnFace;
		int num6;
		if (entity.Properties.RotateModelOnClimb)
		{
			BlockFacing climbingOnFace2 = entity.ClimbingOnFace;
			num6 = ((climbingOnFace2 != null && climbingOnFace2.Axis == EnumAxis.X) ? 1 : 0);
		}
		else
		{
			num6 = 0;
		}
		bool flag = (byte)num6 != 0;
		float num7 = -1f;
		Quaterniond.RotateX(array, array, num4 + num * ((float)Math.PI / 180f) + (flag ? (num5 * num7) : 0f));
		Quaterniond.RotateY(array, array, flag ? 0f : num5);
		Quaterniond.RotateZ(array, array, (double)entity.Pos.Roll + stepPitch + (double)(num3 * ((float)Math.PI / 180f)) + (double)(flag ? ((float)Math.PI / 2f * (float)((climbingOnFace != BlockFacing.WEST) ? 1 : (-1))) : 0f));
		Quaterniond.RotateX(array, array, xangle);
		Quaterniond.RotateY(array, array, yangle);
		Quaterniond.RotateZ(array, array, zangle);
		float[] array2 = new float[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (float)array[i];
		}
		Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), array2));
		if (shouldSwivelFromMotion)
		{
			Mat4f.RotateX(ModelMat, ModelMat, nowSwivelRad);
		}
		float size = entity.Properties.Client.Size;
		Mat4f.Translate(ModelMat, ModelMat, 0f, (0f - entity.SelectionBox.Y2) / 2f, 0f);
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { size, size, size });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	protected void loadModelMatrixForGui(Entity entity, double posX, double posY, double posZ, double yawDelta, float size)
	{
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)posX, (float)posY, (float)posZ);
		Mat4f.Translate(ModelMat, ModelMat, size, 2f * size, 0f);
		float num = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateX : 0f);
		float num2 = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateY : 0f);
		float num3 = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateZ : 0f);
		Mat4f.RotateX(ModelMat, ModelMat, (float)Math.PI + num * ((float)Math.PI / 180f));
		Mat4f.RotateY(ModelMat, ModelMat, (float)yawDelta + num2 * ((float)Math.PI / 180f));
		Mat4f.RotateZ(ModelMat, ModelMat, num3 * ((float)Math.PI / 180f));
		float num4 = entity.Properties.Client.Size * size;
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { num4, num4, num4 });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	private void updateStepPitch(float dt)
	{
		if (!entity.CanStepPitch)
		{
			stepPitch = 0.0;
			return;
		}
		double num = 0.0;
		if (LastJumpMs > 0)
		{
			num = 0.0;
			if (capi.InWorldEllapsedMilliseconds - LastJumpMs > 500 && entity.OnGround)
			{
				LastJumpMs = -1L;
			}
		}
		else
		{
			prevYAccum += dt;
			if (prevYAccum > 0.20000000298023224)
			{
				prevYAccum = 0.0;
				prevY = entity.Pos.Y;
			}
			EntityAgent entityAgent = eagent;
			if (entityAgent != null && !entityAgent.Alive)
			{
				stepPitch = Math.Max(0.0, stepPitch - (double)(2f * dt));
			}
			if (eagent == null || entity.Properties.CanClimbAnywhere || !eagent.Alive || entity.Attributes.GetInt("dmgkb") != 0 || !entity.Properties.Client.PitchStep)
			{
				return;
			}
			if (entity.Properties.Habitat == EnumHabitat.Air || eagent.Controls.IsClimbing)
			{
				stepPitch = GameMath.Clamp(entity.Pos.Y - prevY + 0.1, 0.0, 0.3) - GameMath.Clamp(prevY - entity.Pos.Y - 0.1, 0.0, 0.3);
				return;
			}
			double num2 = entity.Pos.Y - prevY;
			bool flag = num2 > 0.02 && !entity.FeetInLiquid && !entity.Swimming && !entity.OnGround;
			bool num3 = num2 < 0.0 && !entity.OnGround && !entity.FeetInLiquid && !entity.Swimming;
			stepingAccum = Math.Max(0f, stepingAccum - dt);
			fallingAccum = Math.Max(0f, fallingAccum - dt);
			if (flag)
			{
				stepingAccum = 0.2f;
			}
			if (num3)
			{
				fallingAccum = 0.2f;
			}
			if (stepingAccum > 0f)
			{
				num = -0.5;
			}
			else if (fallingAccum > 0f)
			{
				num = 0.5;
			}
		}
		stepPitch += (num - stepPitch) * (double)dt * 5.0;
	}

	protected virtual void determineSidewaysSwivel(float dt)
	{
		if (!shouldSwivelFromMotion)
		{
			if (eagent != null)
			{
				eagent.sidewaysSwivelAngle = 0f;
			}
			return;
		}
		if (!entity.CanSwivel)
		{
			nowSwivelRad = 0f;
			targetSwivelRad = 0f;
			if (eagent != null)
			{
				eagent.sidewaysSwivelAngle = 0f;
			}
			return;
		}
		swivelaccum += dt;
		if ((double)swivelaccum > 0.1 && entity.CanSwivelNow)
		{
			double num = entity.Pos.X - prevPosXSwing;
			double num2 = entity.Pos.Z - prevPosZSwing;
			double num3 = Math.Atan2(num2, num);
			double num4 = Math.Sqrt(num * num + num2 * num2);
			swivelaccum = 0f;
			float num5 = GameMath.AngleRadDistance((float)num3, (float)prevAngleSwing);
			if (Math.Abs(num5) < (float)Math.PI / 2f)
			{
				float num6 = (float)Math.PI / 180f * maxSwivelAngle;
				targetSwivelRad = GameMath.Clamp((float)num4 * num5 * 3f, 0f - num6, num6);
			}
			else
			{
				targetSwivelRad = 0f;
			}
			prevAngleSwing = num3;
			prevPosXSwing = entity.Pos.X;
			prevPosZSwing = entity.Pos.Z;
		}
		float num7 = GameMath.AngleRadDistance(nowSwivelRad, targetSwivelRad);
		nowSwivelRad += GameMath.Clamp(num7 * dt * 2f, -0.15f, 0.15f);
		if (eagent != null)
		{
			eagent.sidewaysSwivelAngle = nowSwivelRad;
		}
	}

	public override void Dispose()
	{
		capi.World.UnregisterGameTickListener(listenerId);
		listenerId = 0L;
		if (meshRefOpaque != null)
		{
			meshRefOpaque.Dispose();
			meshRefOpaque = null;
		}
		if (debugTagTexture != null)
		{
			debugTagTexture.Dispose();
			debugTagTexture = null;
		}
		capi.Event.ReloadShapes -= entity.MarkShapeModified;
		if (DisplayChatMessages)
		{
			capi.Event.ChatMessage -= OnChatMessage;
		}
	}
}
