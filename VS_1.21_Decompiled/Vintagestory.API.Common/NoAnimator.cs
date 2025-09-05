using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class NoAnimator : IAnimator
{
	public float[] Matrices => null;

	public int ActiveAnimationCount => 0;

	public bool CalculateMatrices { get; set; }

	public RunningAnimation[] Animations => Array.Empty<RunningAnimation>();

	public int MaxJointId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string DumpCurrentState()
	{
		throw new NotImplementedException();
	}

	public RunningAnimation GetAnimationState(string code)
	{
		return null;
	}

	public AttachmentPointAndPose GetAttachmentPointPose(string code)
	{
		return null;
	}

	public ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		return null;
	}

	public void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
	}

	public void ReloadAttachmentPoints()
	{
	}
}
