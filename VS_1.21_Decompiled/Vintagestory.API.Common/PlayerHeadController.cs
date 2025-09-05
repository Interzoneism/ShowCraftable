using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class PlayerHeadController : EntityHeadController
{
	protected IPlayer player;

	private EntityPlayer entityPlayer;

	protected bool turnOpposite;

	protected bool rotateTpYawNow;

	public PlayerHeadController(IAnimationManager animator, EntityPlayer entity, Shape entityShape)
		: base(animator, entity, entityShape)
	{
		entityPlayer = entity;
	}

	public override void OnFrame(float dt)
	{
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		ICoreClientAPI coreClientAPI = entity.Api as ICoreClientAPI;
		if (coreClientAPI.World.Player.Entity.EntityId != entity.EntityId)
		{
			base.OnFrame(dt);
			if (entity.BodyYawServer == 0f)
			{
				entity.BodyYaw = entity.Pos.Yaw;
			}
			return;
		}
		float num = GameMath.AngleRadDistance(entity.BodyYaw, entity.Pos.Yaw);
		if (Math.Abs(num) > 1.8849558f)
		{
			turnOpposite = true;
		}
		if (turnOpposite)
		{
			if (Math.Abs(num) < (float)Math.PI * 9f / 20f)
			{
				turnOpposite = false;
			}
			else
			{
				num = 0f;
			}
		}
		EnumCameraMode cameraMode = (player as IClientPlayer).CameraMode;
		bool flag = coreClientAPI.Settings.Bool["overheadLookAt"] && cameraMode == EnumCameraMode.Overhead;
		if (!flag && coreClientAPI.Input.MouseGrabbed)
		{
			entity.Pos.HeadYaw += (num - entity.Pos.HeadYaw) * dt * 6f;
			entity.Pos.HeadYaw = GameMath.Clamp(entity.Pos.HeadYaw, -0.75f, 0.75f);
			entity.Pos.HeadPitch = GameMath.Clamp((entity.Pos.Pitch - (float)Math.PI) * 0.75f, -1.2f, 1.2f);
		}
		EnumMountAngleMode enumMountAngleMode = EnumMountAngleMode.Unaffected;
		IMountableSeat mountedOn = player.Entity.MountedOn;
		if (player.Entity.MountedOn != null)
		{
			enumMountAngleMode = mountedOn.AngleMode;
		}
		if (player?.Entity == null || enumMountAngleMode == EnumMountAngleMode.Fixate || enumMountAngleMode == EnumMountAngleMode.FixateYaw || cameraMode == EnumCameraMode.Overhead)
		{
			if (coreClientAPI.Input.MouseGrabbed)
			{
				entity.BodyYaw = entity.Pos.Yaw;
				if (flag)
				{
					float num2 = 0f - GameMath.AngleRadDistance((entity.Api as ICoreClientAPI).Input.MouseYaw, entity.Pos.Yaw);
					float num3 = (float)Math.PI + num2;
					float num4 = GameMath.Clamp(0f - entity.Pos.Pitch - (float)Math.PI + (float)Math.PI * 2f, -1f, 0.8f);
					if (num3 > (float)Math.PI)
					{
						num3 -= (float)Math.PI * 2f;
					}
					if (num3 < -1f || num3 > 1f)
					{
						num3 = 0f;
						entity.Pos.HeadPitch += (GameMath.Clamp((entity.Pos.Pitch - (float)Math.PI) * 0.75f, -1.2f, 1.2f) - entity.Pos.HeadPitch) * dt * 6f;
					}
					else
					{
						entity.Pos.HeadPitch += (num4 - entity.Pos.HeadPitch) * dt * 6f;
					}
					entity.Pos.HeadYaw += (num3 - entity.Pos.HeadYaw) * dt * 6f;
				}
			}
		}
		else
		{
			IPlayer obj = player;
			if (obj != null && obj.Entity.Alive)
			{
				float num5 = GameMath.AngleRadDistance(entity.BodyYaw, entity.Pos.Yaw);
				bool flag2 = player.Entity.Controls.TriesToMove || player.Entity.ServerControls.TriesToMove;
				bool flag3 = false;
				float num6 = 1.2f - (flag2 ? 1.19f : 0f) + (float)(flag3 ? 3 : 0);
				if (entity.Controls.Gliding)
				{
					num6 = 0f;
				}
				if (player.PlayerUID == coreClientAPI.World.Player.PlayerUID && !coreClientAPI.Settings.Bool["immersiveFpMode"] && cameraMode != EnumCameraMode.FirstPerson)
				{
					if (Math.Abs(num5) > num6 || rotateTpYawNow)
					{
						float num7 = 0.05f + Math.Abs(num5) * 3.5f;
						entity.BodyYaw += GameMath.Clamp(num5, (0f - dt) * num7, dt * num7);
						rotateTpYawNow = Math.Abs(num5) > 0.01f;
					}
				}
				else
				{
					entity.BodyYaw = entity.Pos.Yaw;
				}
			}
		}
		base.OnFrame(dt);
	}
}
