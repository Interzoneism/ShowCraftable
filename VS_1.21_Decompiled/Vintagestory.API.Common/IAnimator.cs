using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IAnimator
{
	int MaxJointId { get; }

	float[] Matrices { get; }

	int ActiveAnimationCount { get; }

	RunningAnimation[] Animations { get; }

	bool CalculateMatrices { get; set; }

	RunningAnimation GetAnimationState(string code);

	AttachmentPointAndPose GetAttachmentPointPose(string code);

	ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);

	void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt);

	string DumpCurrentState();
}
