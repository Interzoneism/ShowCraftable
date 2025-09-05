using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface IAnimationManager : IDisposable
{
	IAnimator Animator { get; set; }

	EntityHeadController HeadController { get; set; }

	bool AnimationsDirty { get; set; }

	Dictionary<string, AnimationMetaData> ActiveAnimationsByAnimCode { get; }

	bool AdjustCollisionBoxToAnimation { get; }

	event StartAnimationDelegate OnStartAnimation;

	event StartAnimationDelegate OnAnimationReceived;

	event Action<string> OnAnimationStopped;

	void Init(ICoreAPI api, Entity entity);

	bool IsAnimationActive(params string[] anims);

	RunningAnimation GetAnimationState(string anim);

	bool TryStartAnimation(AnimationMetaData animdata);

	bool StartAnimation(AnimationMetaData animdata);

	bool StartAnimation(string configCode);

	void StopAnimation(string code);

	void FromAttributes(ITreeAttribute tree, string version);

	void ToAttributes(ITreeAttribute tree, bool forClient);

	void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds);

	void TriggerAnimationStopped(string code);

	void ShouldPlaySound(AnimationSound sound);

	void OnServerTick(float dt);

	void OnClientFrame(float dt);

	void ResetAnimation(string beginholdAnim);

	void RegisterFrameCallback(AnimFrameCallback trigger);

	IAnimator LoadAnimator(ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements);

	void CopyOverAnimStates(RunningAnimation[] copyOverAnims, IAnimator animator);
}
