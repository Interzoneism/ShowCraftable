using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityDrifter : EntityHumanoid
{
	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		AnimationTrigger animationTrigger = new AnimationTrigger();
		animationTrigger.OnControls = new EnumEntityActivity[1] { EnumEntityActivity.Dead };
		AnimationTrigger triggeredBy = animationTrigger;
		int num = properties.Attributes["oddsToAlter"].AsInt(5);
		string dieAnimationCode = properties.Attributes[" "].AsString("die");
		string alternativeDieAnimationCode = properties.Attributes["alternativeDieAnimationCode"].AsString("crawldie");
		if (EntityId % num == 0L)
		{
			float[] array = properties.Attributes["alternativeCollisionBox"].AsArray(new float[2] { 0.9f, 0.6f });
			Dictionary<string, string> remaps = new Dictionary<string, string>
			{
				{ "idle", "crawlidle" },
				{ "standwalk", "crawlwalk" },
				{ "standlowwalk", "crawlwalk" },
				{ "standrun", "crawlrun" },
				{ "standidle", "crawlidle" },
				{ "standdespair", "crawlemote" },
				{ "standcry", "crawlemote" },
				{ "standhurt", "crawlhurt" },
				{ "standdie", "crawldie" }
			};
			if (properties.Attributes.KeyExists("animationsRemapping"))
			{
				remaps = properties.Attributes["animationsRemapping"].AsObject<Dictionary<string, string>>();
			}
			AnimationMetaData animationMetaData = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == dieAnimationCode);
			AnimationMetaData animationMetaData2 = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == alternativeDieAnimationCode);
			if (animationMetaData != null)
			{
				animationMetaData.TriggeredBy = null;
			}
			if (animationMetaData2 != null)
			{
				animationMetaData2.TriggeredBy = triggeredBy;
			}
			properties.CollisionBoxSize = new Vec2f(array[0], array[1]);
			properties.SelectionBoxSize = new Vec2f(array[0], array[1]);
			string idleAnimationCode = properties.Attributes["idleAnimationCode"].AsString("idle");
			string alternativeIdleAnimationCode = properties.Attributes["alternativeIdleAnimationCode"].AsString("crawlidle");
			AnimationMetaData animationMetaData3 = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == idleAnimationCode);
			AnimationMetaData animationMetaData4 = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == alternativeIdleAnimationCode);
			if (animationMetaData3 != null)
			{
				animationMetaData3.TriggeredBy = null;
			}
			if (animationMetaData4 != null)
			{
				animationMetaData4.TriggeredBy = new AnimationTrigger
				{
					DefaultAnim = true
				};
			}
			bool canClimb = properties.Attributes["alternativeCanClimb"].AsBool();
			properties.CanClimb = canClimb;
			AnimManager = new RemapAnimationManager(remaps)
			{
				IdleAnimation = alternativeIdleAnimationCode
			};
		}
		else
		{
			AnimationMetaData animationMetaData5 = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == dieAnimationCode);
			AnimationMetaData animationMetaData6 = properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == alternativeDieAnimationCode);
			if (animationMetaData5 != null)
			{
				animationMetaData5.TriggeredBy = triggeredBy;
			}
			if (animationMetaData6 != null)
			{
				animationMetaData6.TriggeredBy = null;
			}
		}
		base.Initialize(properties, api, InChunkIndex3d);
	}
}
