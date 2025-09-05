using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityPlayerShapeRenderer : EntityShapeRenderer
{
	private MultiTextureMeshRef firstPersonMeshRef;

	private MultiTextureMeshRef thirdPersonMeshRef;

	private bool watcherRegistered;

	private EntityPlayer entityPlayer;

	private ModSystemFpHands modSys;

	private RenderMode renderMode;

	private float smoothedBodyYaw;

	private bool previfpMode;

	private static ModelTransform DefaultTongTransform = new ModelTransform
	{
		Translation = new FastVec3f(-0.68f, -0.52f, -0.6f),
		Rotation = new FastVec3f(-26f, -13f, -88f),
		Origin = new FastVec3f(0.5f, 0f, 0.5f),
		Scale = 0.7f
	};

	public float? HeldItemPitchFollowOverride { get; set; }

	protected bool IsSelf => entity.EntityId == capi.World.Player.Entity.EntityId;

	public override bool DisplayChatMessages => true;

	public virtual float HandRenderFov => (float)capi.Settings.Int["fpHandsFoV"] * ((float)Math.PI / 180f);

	public EntityPlayerShapeRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		entityPlayer = entity as EntityPlayer;
		modSys = api.ModLoader.GetModSystem<ModSystemFpHands>();
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
	}

	public override void TesselateShape()
	{
		if (entityPlayer.GetBehavior<EntityBehaviorPlayerInventory>().Inventory == null)
		{
			return;
		}
		defaultTexSource = GetTextureSource();
		Tesselate();
		if (watcherRegistered)
		{
			return;
		}
		previfpMode = capi.Settings.Bool["immersiveFpMode"];
		if (IsSelf)
		{
			capi.Settings.Bool.AddWatcher("immersiveFpMode", delegate(bool on)
			{
				entity.MarkShapeModified();
				(entityPlayer.AnimManager as PlayerAnimationManager).OnIfpModeChanged(previfpMode, on);
			});
		}
		watcherRegistered = true;
	}

	protected override void onMeshReady(MeshData meshData)
	{
		base.onMeshReady(meshData);
		if (!IsSelf)
		{
			thirdPersonMeshRef = meshRefOpaque;
		}
	}

	public void Tesselate()
	{
		if (!IsSelf)
		{
			base.TesselateShape();
		}
		else
		{
			if (!loaded)
			{
				return;
			}
			TesselateShape(delegate(MeshData meshData)
			{
				disposeMeshes();
				if (!capi.IsShuttingDown && meshData.VerticesCount > 0)
				{
					MeshData meshData2 = meshData.EmptyClone();
					thirdPersonMeshRef = capi.Render.UploadMultiTextureMesh(meshData);
					determineRenderMode();
					if (renderMode == RenderMode.ImmersiveFirstPerson)
					{
						HashSet<int> skipJointIds = new HashSet<int>();
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("Neck"), skipJointIds);
						meshData2.AddMeshData(meshData, (int i) => !skipJointIds.Contains(meshData.CustomInts.Values[i * 4]));
					}
					else
					{
						HashSet<int> includeJointIds = new HashSet<int>();
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("UpperArmL"), includeJointIds);
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("UpperArmR"), includeJointIds);
						meshData2.AddMeshData(meshData, (int i) => includeJointIds.Contains(meshData.CustomInts.Values[i * 4]));
					}
					firstPersonMeshRef = capi.Render.UploadMultiTextureMesh(meshData2);
				}
			});
		}
	}

	private void loadJointIdsRecursive(ElementPose elementPose, HashSet<int> outList)
	{
		outList.Add(elementPose.ForElement.JointId);
		foreach (ElementPose childElementPose in elementPose.ChildElementPoses)
		{
			loadJointIdsRecursive(childElementPose, outList);
		}
	}

	private void disposeMeshes()
	{
		if (firstPersonMeshRef != null)
		{
			firstPersonMeshRef.Dispose();
			firstPersonMeshRef = null;
		}
		if (thirdPersonMeshRef != null)
		{
			thirdPersonMeshRef.Dispose();
			thirdPersonMeshRef = null;
		}
		meshRefOpaque = null;
	}

	public override void BeforeRender(float dt)
	{
		RenderMode renderMode = this.renderMode;
		determineRenderMode();
		if ((renderMode == RenderMode.FirstPerson && this.renderMode == RenderMode.ImmersiveFirstPerson) || (renderMode == RenderMode.ImmersiveFirstPerson && this.renderMode == RenderMode.FirstPerson))
		{
			entity.MarkShapeModified();
			(entityPlayer.AnimManager as PlayerAnimationManager).OnIfpModeChanged(previfpMode, this.renderMode == RenderMode.ImmersiveFirstPerson);
		}
		base.BeforeRender(dt);
	}

	private void determineRenderMode()
	{
		if (IsSelf)
		{
			IClientPlayer clientPlayer = player;
			if (clientPlayer != null && clientPlayer.CameraMode == EnumCameraMode.FirstPerson)
			{
				if (capi.Settings.Bool["immersiveFpMode"] && !capi.Render.CameraStuck)
				{
					ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("tiredness");
					if (treeAttribute == null || treeAttribute.GetInt("isSleeping") != 1)
					{
						renderMode = RenderMode.ImmersiveFirstPerson;
						return;
					}
				}
				renderMode = RenderMode.FirstPerson;
				return;
			}
		}
		renderMode = RenderMode.ThirdPerson;
	}

	public override void RenderToGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
		if (IsSelf)
		{
			meshRefOpaque = thirdPersonMeshRef;
		}
		base.RenderToGui(dt, posX, posY, posZ, yawDelta, size);
	}

	public override void DoRender2D(float dt)
	{
		if (IsSelf)
		{
			IClientPlayer clientPlayer = player;
			if (clientPlayer != null && clientPlayer.CameraMode == EnumCameraMode.FirstPerson)
			{
				return;
			}
		}
		base.DoRender2D(dt);
	}

	public override Vec3d getAboveHeadPosition(EntityPlayer entityPlayer)
	{
		if (IsSelf)
		{
			return new Vec3d(entityPlayer.CameraPos.X + entityPlayer.LocalEyePos.X, entityPlayer.CameraPos.Y + 0.4 + entityPlayer.LocalEyePos.Y, entityPlayer.CameraPos.Z + entityPlayer.LocalEyePos.Z);
		}
		return base.getAboveHeadPosition(entityPlayer);
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (IsSelf)
		{
			entityPlayer.selfNowShadowPass = isShadowPass;
		}
		bool flag = renderMode == RenderMode.FirstPerson && !isShadowPass;
		loadModelMatrixForPlayer(entity, IsSelf, dt, isShadowPass);
		if (IsSelf && (renderMode == RenderMode.ImmersiveFirstPerson || isShadowPass))
		{
			OriginPos.Set(0f, 0f, 0f);
		}
		if (flag && capi.HideGuis)
		{
			return;
		}
		if (flag)
		{
			pMatrixNormalFov = (float[])capi.Render.CurrentProjectionMatrix.Clone();
			capi.Render.Set3DProjection(capi.Render.ShaderUniforms.ZFar, HandRenderFov);
			pMatrixHandFov = (float[])capi.Render.CurrentProjectionMatrix.Clone();
		}
		else
		{
			pMatrixHandFov = null;
			pMatrixNormalFov = null;
		}
		if (isShadowPass)
		{
			DoRender3DAfterOIT(dt, isShadowPass: true);
		}
		if (DoRenderHeldItem && !entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("lie") && !isSpectator)
		{
			RenderHeldItem(dt, isShadowPass, right: false);
			RenderHeldItem(dt, isShadowPass, right: true);
		}
		if (flag)
		{
			if (!capi.Settings.Bool["hideFpHands"] && !entityPlayer.GetBehavior<EntityBehaviorTiredness>().IsSleeping)
			{
				IShaderProgram fpModeHandShader = modSys.fpModeHandShader;
				meshRefOpaque = firstPersonMeshRef;
				fpModeHandShader.Use();
				fpModeHandShader.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
				fpModeHandShader.Uniform("rgbaFogIn", capi.Render.FogColor);
				fpModeHandShader.Uniform("fogMinIn", capi.Render.FogMin);
				fpModeHandShader.Uniform("fogDensityIn", capi.Render.FogDensity);
				fpModeHandShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				fpModeHandShader.Uniform("alphaTest", 0.05f);
				fpModeHandShader.Uniform("lightPosition", capi.Render.ShaderUniforms.LightPosition3D);
				fpModeHandShader.Uniform("depthOffset", -0.3f - GameMath.Max(0f, (float)capi.Settings.Int["fieldOfView"] / 90f - 1f) / 2f);
				capi.Render.GlPushMatrix();
				capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
				base.DoRender3DOpaqueBatched(dt, isShadowPass: false);
				capi.Render.GlPopMatrix();
				fpModeHandShader.Stop();
			}
			capi.Render.Reset3DProjection();
		}
	}

	protected override IShaderProgram getReadyShader()
	{
		if (!entityPlayer.selfNowShadowPass && renderMode == RenderMode.FirstPerson)
		{
			IShaderProgram fpModeItemShader = modSys.fpModeItemShader;
			fpModeItemShader.Use();
			fpModeItemShader.Uniform("depthOffset", -0.3f - GameMath.Max(0f, (float)capi.Settings.Int["fieldOfView"] / 90f - 1f) / 2f);
			fpModeItemShader.Uniform("ssaoAttn", 1f);
			return fpModeItemShader;
		}
		return base.getReadyShader();
	}

	protected override void RenderHeldItem(float dt, bool isShadowPass, bool right)
	{
		if (IsSelf)
		{
			entityPlayer.selfNowShadowPass = isShadowPass;
		}
		if (right)
		{
			ItemSlot itemSlot = eagent?.RightHandItemSlot;
			if (itemSlot is ItemSlotSkill)
			{
				return;
			}
			ItemStack itemStack = itemSlot?.Itemstack;
			ItemStack itemStack2 = eagent?.LeftHandItemSlot?.Itemstack;
			if (itemStack != null && itemStack.Collectible.GetTemperature(entity.World, itemStack) > 200f && itemStack2 != null && itemStack2.ItemAttributes?.IsTrue("heatResistant") == true)
			{
				AttachmentPointAndPose apap = entity.AnimManager?.Animator?.GetAttachmentPointPose("LeftHand");
				ItemRenderInfo itemStackRenderInfo = capi.Render.GetItemStackRenderInfo(itemSlot, EnumItemRenderTarget.HandTpOff, dt);
				itemStackRenderInfo.Transform = itemStack.ItemAttributes?["onTongTransform"].AsObject(DefaultTongTransform) ?? DefaultTongTransform;
				RenderItem(dt, isShadowPass, itemStack, apap, itemStackRenderInfo);
				return;
			}
		}
		bool flag = renderMode == RenderMode.FirstPerson;
		if ((flag && !capi.Settings.Bool["hideFpHands"]) || !flag)
		{
			base.RenderHeldItem(dt, isShadowPass, right);
		}
	}

	public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
		if (renderMode != RenderMode.FirstPerson || isShadowPass)
		{
			if (isShadowPass)
			{
				meshRefOpaque = thirdPersonMeshRef;
			}
			else
			{
				meshRefOpaque = ((renderMode == RenderMode.ImmersiveFirstPerson) ? firstPersonMeshRef : thirdPersonMeshRef);
			}
			base.DoRender3DOpaqueBatched(dt, isShadowPass);
		}
	}

	public void loadModelMatrixForPlayer(Entity entity, bool isSelf, float dt, bool isShadowPass)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		if (isSelf)
		{
			Matrixf matrixf = entityPlayer.MountedOn?.RenderTransform;
			if (matrixf != null)
			{
				ModelMat = Mat4f.Mul(ModelMat, ModelMat, matrixf.Values);
			}
		}
		else
		{
			Vec3f otherPlayerRenderOffset = GetOtherPlayerRenderOffset();
			Mat4f.Translate(ModelMat, ModelMat, otherPlayerRenderOffset.X, otherPlayerRenderOffset.Y, otherPlayerRenderOffset.Z);
			Matrixf matrixf2 = this.entityPlayer.MountedOn?.RenderTransform;
			if (matrixf2 != null)
			{
				ModelMat = Mat4f.Mul(ModelMat, ModelMat, matrixf2.Values);
			}
		}
		float num = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateX : 0f);
		float num2 = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateY : 0f);
		float num3 = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateZ : 0f);
		float num4 = Math.Min(0.05f, dt);
		if (!isSelf && this.entityPlayer.MountedOn?.Entity != null)
		{
			smoothedBodyYaw = (bodyYawLerped = this.entityPlayer.MountedOn.Entity.Pos.Yaw);
		}
		else
		{
			IClientPlayer clientPlayer = player;
			if (clientPlayer == null || clientPlayer.CameraMode != EnumCameraMode.FirstPerson)
			{
				float val = GameMath.AngleRadDistance(bodyYawLerped, eagent.BodyYaw);
				bodyYawLerped += GameMath.Clamp(val, (0f - num4) * 8f, num4 * 8f);
				float num5 = bodyYawLerped;
				smoothedBodyYaw = num5;
			}
			else
			{
				float num5 = ((renderMode != RenderMode.ThirdPerson) ? eagent.BodyYaw : eagent.Pos.Yaw);
				if (!isShadowPass)
				{
					smoothCameraTurning(num5, num4);
				}
			}
		}
		float num6 = ((this.entityPlayer == null) ? 0f : this.entityPlayer.WalkPitch);
		float valueOrDefault = (this.entityPlayer.MountedOn?.SeatPosition?.Pitch).GetValueOrDefault();
		Mat4f.RotateX(ModelMat, ModelMat, entity.Pos.Roll + num * ((float)Math.PI / 180f));
		Mat4f.RotateY(ModelMat, ModelMat, smoothedBodyYaw + (90f + num2) * ((float)Math.PI / 180f));
		if ((!isSelf || !eagent.Swimming || renderMode != RenderMode.FirstPerson) && (((entityPlayer == null || !entityPlayer.Controls.Gliding) && entityPlayer.MountedOn == null) || renderMode != RenderMode.FirstPerson))
		{
			Mat4f.RotateZ(ModelMat, ModelMat, num6 + num3 * ((float)Math.PI / 180f));
			if (valueOrDefault != 0f)
			{
				Mat4f.Translate(ModelMat, ModelMat, 0f, -0.5f, 0f);
				Mat4f.RotateZ(ModelMat, ModelMat, valueOrDefault);
				Mat4f.Translate(ModelMat, ModelMat, 0f, 0.5f, 0f);
			}
		}
		Mat4f.RotateX(ModelMat, ModelMat, nowSwivelRad);
		if (entityPlayer != null && renderMode == RenderMode.FirstPerson && !isShadowPass)
		{
			float num7 = eagent.RightHandItemSlot?.Itemstack?.ItemAttributes?["heldItemPitchFollow"].AsFloat(0.75f) ?? 0.75f;
			float num8 = eagent.MountedOn?.FpHandPitchFollow ?? 1f;
			float num9 = ((entityPlayer != null && entityPlayer.Controls.IsFlying) ? 1f : (HeldItemPitchFollowOverride ?? (num7 * num8)));
			Mat4f.Translate(ModelMat, ModelMat, 0f, (float)entity.LocalEyePos.Y, 0f);
			Mat4f.RotateZ(ModelMat, ModelMat, (entity.Pos.Pitch - (float)Math.PI) * num9);
			Mat4f.Translate(ModelMat, ModelMat, 0f, 0f - (float)entity.LocalEyePos.Y, 0f);
		}
		if (renderMode == RenderMode.FirstPerson && !isShadowPass)
		{
			Mat4f.Translate(ModelMat, ModelMat, 0f, capi.Settings.Float["fpHandsYOffset"], 0f);
		}
		float num10 = entity.WatchedAttributes.GetFloat("intoxication");
		intoxIntensity += (num10 - intoxIntensity) * dt / 3f;
		capi.Render.PerceptionEffects.ApplyToTpPlayer(entity as EntityPlayer, ModelMat, intoxIntensity);
		float size = entity.Properties.Client.Size;
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { size, size, size });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	private void smoothCameraTurning(float bodyYaw, float mdt)
	{
		float value = GameMath.AngleRadDistance(smoothedBodyYaw, bodyYaw);
		smoothedBodyYaw += Math.Max(0f, Math.Abs(value) - 0.6f) * (float)Math.Sign(value);
		value = GameMath.AngleRadDistance(smoothedBodyYaw, eagent.BodyYaw);
		smoothedBodyYaw += value * mdt * 25f;
	}

	protected Vec3f GetOtherPlayerRenderOffset()
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		IMountable mountable = entityPlayer.MountedOn?.MountSupplier;
		IMountable mountable2 = (entity as EntityAgent).MountedOn?.MountSupplier;
		if (mountable != null && mountable == mountable2)
		{
			EntityPos seatPosition = entityPlayer.MountedOn.SeatPosition;
			EntityPos seatPosition2 = (entity as EntityAgent).MountedOn.SeatPosition;
			return new Vec3f((float)(0.0 - seatPosition.X + seatPosition2.X), (float)(0.0 - seatPosition.Y + seatPosition2.Y), (float)(0.0 - seatPosition.Z + seatPosition2.Z));
		}
		return new Vec3f((float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));
	}

	protected override void determineSidewaysSwivel(float dt)
	{
		if (entityPlayer.MountedOn != null)
		{
			entityPlayer.sidewaysSwivelAngle = (nowSwivelRad = 0f);
			return;
		}
		double num = Math.Atan2(entity.Pos.Motion.Z, entity.Pos.Motion.X);
		double num2 = entity.Pos.Motion.LengthSq();
		if (num2 > 0.001 && entity.OnGround)
		{
			float val = GameMath.AngleRadDistance((float)prevAngleSwing, (float)num);
			float num3 = nowSwivelRad;
			float num4 = GameMath.Clamp(val, -0.05f, 0.05f) * dt * 40f * (float)Math.Min(0.02500000037252903, num2) * 80f;
			EntityAgent entityAgent = eagent;
			nowSwivelRad = num3 - num4 * (float)((entityAgent == null || !entityAgent.Controls.Backward) ? 1 : (-1));
			nowSwivelRad = GameMath.Clamp(nowSwivelRad, -0.3f, 0.3f);
		}
		nowSwivelRad *= Math.Min(0.99f, 1f - 0.1f * dt * 60f);
		prevAngleSwing = num;
		entityPlayer.sidewaysSwivelAngle = nowSwivelRad;
	}

	public override void Dispose()
	{
		base.Dispose();
		firstPersonMeshRef?.Dispose();
		thirdPersonMeshRef?.Dispose();
	}
}
