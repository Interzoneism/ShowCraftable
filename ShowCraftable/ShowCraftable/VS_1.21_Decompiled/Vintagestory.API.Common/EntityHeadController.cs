using System;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public class EntityHeadController
{
	public ElementPose HeadPose;

	public ElementPose NeckPose;

	public ElementPose UpperTorsoPose;

	public ElementPose LowerTorsoPose;

	public ElementPose UpperFootLPose;

	public ElementPose UpperFootRPose;

	protected EntityAgent entity;

	protected IAnimationManager animManager;

	public float yawOffset;

	public float pitchOffset;

	public EntityHeadController(IAnimationManager animator, EntityAgent entity, Shape entityShape)
	{
		this.entity = entity;
		animManager = animator;
		HeadPose = animator.Animator.GetPosebyName("Head");
		NeckPose = animator.Animator.GetPosebyName("Neck");
		UpperTorsoPose = animator.Animator.GetPosebyName("UpperTorso");
		LowerTorsoPose = animator.Animator.GetPosebyName("LowerTorso");
		UpperFootRPose = animator.Animator.GetPosebyName("UpperFootR");
		UpperFootLPose = animator.Animator.GetPosebyName("UpperFootL");
	}

	public virtual void OnFrame(float dt)
	{
		HeadPose.degOffY = 0f;
		HeadPose.degOffZ = 0f;
		NeckPose.degOffZ = 0f;
		UpperTorsoPose.degOffY = 0f;
		UpperTorsoPose.degOffZ = 0f;
		LowerTorsoPose.degOffZ = 0f;
		UpperFootRPose.degOffZ = 0f;
		UpperFootLPose.degOffZ = 0f;
		if (entity.Pos.HeadYaw != 0f || entity.Pos.HeadPitch != 0f)
		{
			float num = (entity.Pos.HeadYaw + yawOffset) * (180f / (float)Math.PI);
			float num2 = (entity.Pos.HeadPitch + pitchOffset) * (180f / (float)Math.PI);
			HeadPose.degOffY = num * 0.45f;
			HeadPose.degOffZ = num2 * 0.35f;
			NeckPose.degOffY = num * 0.35f;
			NeckPose.degOffZ = num2 * 0.4f;
			ICoreClientAPI obj = entity.World.Api as ICoreClientAPI;
			IPlayer player = (entity as EntityPlayer)?.Player;
			IPlayer obj2 = ((obj?.World.Player.PlayerUID == player?.PlayerUID) ? player : null);
			if (obj2 != null && obj2.ImmersiveFpMode)
			{
				UpperTorsoPose.degOffZ = num2 * 0.3f;
				UpperTorsoPose.degOffY = num * 0.2f;
				float num3 = num2 * 0.1f;
				LowerTorsoPose.degOffZ = num3;
				UpperFootRPose.degOffZ = 0f - num3;
				UpperFootLPose.degOffZ = 0f - num3;
			}
		}
	}
}
