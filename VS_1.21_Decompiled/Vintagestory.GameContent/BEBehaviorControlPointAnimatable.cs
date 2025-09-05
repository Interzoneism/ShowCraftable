using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorControlPointAnimatable : BEBehaviorAnimatable
{
	protected ModSystemControlPoints modSys;

	protected float moveSpeedMul;

	protected ILoadedSound activeSound;

	protected bool active;

	protected ControlPoint animControlPoint;

	protected virtual Shape AnimationShape => null;

	public BEBehaviorControlPointAnimatable(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		AssetLocation code = AssetLocation.Create(properties["controlpointcode"].ToString(), base.Block.Code.Domain);
		moveSpeedMul = properties["animSpeedMul"].AsFloat(1f);
		string text = properties["activeSound"].AsString();
		if (text != null && api is ICoreClientAPI coreClientAPI)
		{
			AssetLocation location = AssetLocation.Create(text, base.Block.Code.Domain).WithPathPrefixOnce("sounds/");
			activeSound = coreClientAPI.World.LoadSound(new SoundParams
			{
				Location = location,
				DisposeOnFinish = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Ambient,
				Volume = 0.25f,
				Range = 16f,
				RelativePosition = false,
				Position = base.Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f)
			});
		}
		modSys = api.ModLoader.GetModSystem<ModSystemControlPoints>();
		animControlPoint = modSys[code];
		animControlPoint.Activate += BEBehaviorControlPointAnimatable_Activate;
		if (api.Side == EnumAppSide.Client)
		{
			animUtil.InitializeAnimator(base.Block.Code.ToShortString(), AnimationShape, null, new Vec3f(0f, base.Block.Shape.rotateY, 0f));
			BEBehaviorControlPointAnimatable_Activate(animControlPoint);
		}
	}

	protected virtual void BEBehaviorControlPointAnimatable_Activate(ControlPoint cpoint)
	{
		updateAnimationstate();
	}

	protected void updateAnimationstate()
	{
		if (animControlPoint == null)
		{
			return;
		}
		active = false;
		AnimationMetaData animationMetaData = animControlPoint.ControlData as AnimationMetaData;
		if (animationMetaData == null)
		{
			return;
		}
		if (animationMetaData.AnimationSpeed == 0f)
		{
			activeSound?.FadeOutAndStop(2f);
			animUtil.StopAnimation(animationMetaData.Animation);
			animUtil.StopAnimation(animationMetaData.Animation + "-inverse");
			return;
		}
		active = true;
		if (moveSpeedMul != 1f)
		{
			animationMetaData = animationMetaData.Clone();
			if (moveSpeedMul < 0f)
			{
				animationMetaData.Animation += "-inverse";
				animationMetaData.Code += "-inverse";
			}
			animationMetaData.AnimationSpeed *= Math.Abs(moveSpeedMul);
		}
		if (!animUtil.StartAnimation(animationMetaData))
		{
			animUtil.activeAnimationsByAnimCode[animationMetaData.Animation].AnimationSpeed = animationMetaData.AnimationSpeed;
		}
		else
		{
			activeSound?.Start();
			activeSound?.FadeIn(2f, null);
		}
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		activeSound?.Stop();
		activeSound?.Dispose();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
	}
}
