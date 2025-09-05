using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorDoorBarLock : BlockEntityBehavior
{
	public bool IsLocked { get; set; } = true;

	public BEBehaviorDoorBarLock(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (IsLocked)
		{
			BEBehaviorDoor behavior = Blockentity.GetBehavior<BEBehaviorDoor>();
			float easeOutSpeed = base.Block.Attributes?["easingSpeed"].AsFloat(10f) ?? 10f;
			AnimationMetaData meta = new AnimationMetaData
			{
				Animation = "lock",
				Code = "lock",
				EaseInSpeed = 10f,
				EaseOutSpeed = easeOutSpeed,
				AnimationSpeed = 0.6f
			};
			behavior.animUtil.StartAnimation(meta);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		IsLocked = tree.GetBool("isLocked", defaultValue: true);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("isLocked", IsLocked);
	}

	public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		BEBehaviorDoor behavior = Blockentity.GetBehavior<BEBehaviorDoor>();
		float rotateYRad = behavior.RotateYRad;
		double y = (double)blockSel.Position.X + blockSel.HitPosition.X - byPlayer.Entity.Pos.X;
		double x = (double)blockSel.Position.Z + blockSel.HitPosition.Z - byPlayer.Entity.Pos.Z;
		float num = GameMath.Mod((float)Math.Atan2(y, x) - rotateYRad, (float)Math.PI * 2f);
		if (IsLocked && num > (float)Math.PI / 2f && num < 4.712389f)
		{
			Api.Logger.Notification("open");
			behavior.animUtil.StopAnimation("lock");
			IsLocked = false;
			Blockentity.MarkDirty(redrawOnClient: true);
		}
		else if (IsLocked && Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.TriggerIngameError(this, "doorBarLocked", Lang.Get("ingameerror-doorbarlocked"));
		}
		if (IsLocked)
		{
			handling = EnumHandling.PreventSubsequent;
		}
		return !IsLocked;
	}
}
