using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class AnimationFrame
{
	public int FrameNumber;

	public List<ElementPose> RootElementTransforms = new List<ElementPose>();

	[Obsolete("Does nothing in 1.20.11 - actually it had no useful effect even before 1.20")]
	public void SetTransform(int jointId, float[] modelTransform)
	{
	}

	[Obsolete("Does nothing in 1.20.11 - actually it had no useful effect even before 1.20")]
	public void FinalizeMatrices(Dictionary<int, AnimationJoint> jointsById)
	{
	}
}
