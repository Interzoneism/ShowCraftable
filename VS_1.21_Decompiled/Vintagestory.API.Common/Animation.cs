using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class Animation
{
	[JsonProperty]
	public int QuantityFrames;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public string Code;

	[JsonProperty]
	public int Version;

	[JsonProperty]
	public bool EaseAnimationSpeed;

	[JsonProperty]
	public AnimationKeyFrame[] KeyFrames;

	[JsonProperty]
	public EnumEntityActivityStoppedHandling OnActivityStopped = EnumEntityActivityStoppedHandling.Rewind;

	[JsonProperty]
	public EnumEntityAnimationEndHandling OnAnimationEnd;

	public uint CodeCrc32;

	public AnimationFrame[][] PrevNextKeyFrameByFrame;

	protected HashSet<int> jointsDone = new HashSet<int>();

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Code == null)
		{
			Code = Name;
		}
		CodeCrc32 = AnimationMetaData.GetCrc32(Code);
	}

	public void GenerateAllFrames(ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, bool recursive = true)
	{
		for (int i = 0; i < rootElements.Length; i++)
		{
			rootElements[i].CacheInverseTransformMatrixRecursive();
		}
		AnimationFrame[] array = new AnimationFrame[KeyFrames.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = new AnimationFrame
			{
				FrameNumber = KeyFrames[j].Frame
			};
		}
		if (KeyFrames.Length == 0)
		{
			throw new Exception("Animation '" + Code + "' has no keyframes, this will cause other errors every time it is ticked");
		}
		if (jointsById.Count >= GlobalConstants.MaxAnimatedElements)
		{
			if (GlobalConstants.MaxAnimatedElements < 46 && jointsById.Count <= 46)
			{
				throw new Exception("Max joint cap of " + GlobalConstants.MaxAnimatedElements + " reached, needs to be at least " + jointsById.Count + ". In clientsettings.json, please try increasing the \"maxAnimatedElements\": setting to 46.  This works for most GPUs.  Otherwise you might need to disable the creature.");
			}
			throw new Exception("A mod's entity has " + jointsById.Count + " animation joints which exceeds the max joint cap of " + GlobalConstants.MaxAnimatedElements + ". Sorry, you'll have to either disable this creature or simplify the model.");
		}
		for (int k = 0; k < array.Length; k++)
		{
			jointsDone.Clear();
			GenerateFrame(k, array, rootElements, jointsById, Mat4f.Create(), array[k].RootElementTransforms, recursive);
		}
		for (int l = 0; l < array.Length; l++)
		{
			array[l].FinalizeMatrices(jointsById);
		}
		PrevNextKeyFrameByFrame = new AnimationFrame[QuantityFrames][];
		for (int m = 0; m < QuantityFrames; m++)
		{
			getLeftRightResolvedFrame(m, array, out var left, out var right);
			PrevNextKeyFrameByFrame[m] = new AnimationFrame[2] { left, right };
		}
	}

	protected void GenerateFrame(int indexNumber, AnimationFrame[] resKeyFrames, ShapeElement[] elements, Dictionary<int, AnimationJoint> jointsById, float[] modelMatrix, List<ElementPose> transforms, bool recursive = true)
	{
		int frameNumber = resKeyFrames[indexNumber].FrameNumber;
		if (frameNumber >= QuantityFrames)
		{
			throw new InvalidOperationException("Invalid animation '" + Code + "'. Has QuantityFrames set to " + QuantityFrames + " but a key frame at frame " + frameNumber + ". QuantityFrames always must be higher than frame number");
		}
		foreach (ShapeElement shapeElement in elements)
		{
			ElementPose transform = new ElementPose();
			transform.ForElement = shapeElement;
			GenerateFrameForElement(frameNumber, shapeElement, ref transform);
			transforms.Add(transform);
			float[] array = Mat4f.CloneIt(modelMatrix);
			Mat4f.Mul(array, array, shapeElement.GetLocalTransformMatrix(Version, null, transform));
			if (shapeElement.JointId > 0 && !jointsDone.Contains(shapeElement.JointId))
			{
				resKeyFrames[indexNumber].SetTransform(shapeElement.JointId, array);
				jointsDone.Add(shapeElement.JointId);
			}
			if (recursive && shapeElement.Children != null)
			{
				GenerateFrame(indexNumber, resKeyFrames, shapeElement.Children, jointsById, array, transform.ChildElementPoses);
			}
		}
	}

	protected void GenerateFrameForElement(int frameNumber, ShapeElement element, ref ElementPose transform)
	{
		for (int i = 0; i < 3; i++)
		{
			getTwoKeyFramesElementForFlag(frameNumber, element, i, out var left, out var right);
			if (left != null)
			{
				float t;
				if (right == null || left == right)
				{
					right = left;
					t = 0f;
				}
				else if (right.Frame < left.Frame)
				{
					int num = right.Frame + (QuantityFrames - left.Frame);
					t = (float)GameMath.Mod(frameNumber - left.Frame, QuantityFrames) / (float)num;
				}
				else
				{
					t = (float)(frameNumber - left.Frame) / (float)(right.Frame - left.Frame);
				}
				lerpKeyFrameElement(left, right, i, t, ref transform);
				transform.RotShortestDistanceX = left.RotShortestDistanceX;
				transform.RotShortestDistanceY = left.RotShortestDistanceY;
				transform.RotShortestDistanceZ = left.RotShortestDistanceZ;
			}
		}
	}

	protected void lerpKeyFrameElement(AnimationKeyFrameElement prev, AnimationKeyFrameElement next, int forFlag, float t, ref ElementPose transform)
	{
		if (prev != null || next != null)
		{
			switch (forFlag)
			{
			case 0:
				transform.translateX = GameMath.Lerp((float)prev.OffsetX.Value / 16f, (float)next.OffsetX.Value / 16f, t);
				transform.translateY = GameMath.Lerp((float)prev.OffsetY.Value / 16f, (float)next.OffsetY.Value / 16f, t);
				transform.translateZ = GameMath.Lerp((float)prev.OffsetZ.Value / 16f, (float)next.OffsetZ.Value / 16f, t);
				break;
			case 1:
				transform.degX = GameMath.Lerp((float)prev.RotationX.Value, (float)next.RotationX.Value, t);
				transform.degY = GameMath.Lerp((float)prev.RotationY.Value, (float)next.RotationY.Value, t);
				transform.degZ = GameMath.Lerp((float)prev.RotationZ.Value, (float)next.RotationZ.Value, t);
				break;
			default:
				transform.scaleX = GameMath.Lerp((float)prev.StretchX.Value, (float)next.StretchX.Value, t);
				transform.scaleY = GameMath.Lerp((float)prev.StretchY.Value, (float)next.StretchY.Value, t);
				transform.scaleZ = GameMath.Lerp((float)prev.StretchZ.Value, (float)next.StretchZ.Value, t);
				break;
			}
		}
	}

	protected void getTwoKeyFramesElementForFlag(int frameNumber, ShapeElement forElement, int forFlag, out AnimationKeyFrameElement left, out AnimationKeyFrameElement right)
	{
		left = null;
		right = null;
		int num = seekRightKeyFrame(frameNumber, forElement, forFlag);
		if (num != -1)
		{
			right = KeyFrames[num].GetKeyFrameElement(forElement);
			int num2 = seekLeftKeyFrame(num, forElement, forFlag);
			if (num2 == -1)
			{
				left = right;
			}
			else
			{
				left = KeyFrames[num2].GetKeyFrameElement(forElement);
			}
		}
	}

	private int seekRightKeyFrame(int aboveFrameNumber, ShapeElement forElement, int forFlag)
	{
		int num = -1;
		for (int i = 0; i < KeyFrames.Length; i++)
		{
			AnimationKeyFrame animationKeyFrame = KeyFrames[i];
			AnimationKeyFrameElement keyFrameElement = animationKeyFrame.GetKeyFrameElement(forElement);
			if (keyFrameElement != null && keyFrameElement.IsSet(forFlag))
			{
				if (num == -1)
				{
					num = i;
				}
				if (animationKeyFrame.Frame > aboveFrameNumber)
				{
					return i;
				}
			}
		}
		return num;
	}

	private int seekLeftKeyFrame(int leftOfKeyFrameIndex, ShapeElement forElement, int forFlag)
	{
		for (int i = 0; i < KeyFrames.Length; i++)
		{
			int num = GameMath.Mod(leftOfKeyFrameIndex - i - 1, KeyFrames.Length);
			AnimationKeyFrameElement keyFrameElement = KeyFrames[num].GetKeyFrameElement(forElement);
			if (keyFrameElement != null && keyFrameElement.IsSet(forFlag))
			{
				return num;
			}
		}
		return -1;
	}

	protected void getLeftRightResolvedFrame(int frameNumber, AnimationFrame[] frames, out AnimationFrame left, out AnimationFrame right)
	{
		left = null;
		right = null;
		int num = frames.Length - 1;
		bool flag = false;
		while (num >= -1)
		{
			AnimationFrame animationFrame = frames[GameMath.Mod(num, frames.Length)];
			num--;
			if (animationFrame.FrameNumber <= frameNumber || flag)
			{
				left = animationFrame;
				break;
			}
			if (num == -1)
			{
				flag = true;
			}
		}
		num += 2;
		AnimationFrame animationFrame2 = frames[GameMath.Mod(num, frames.Length)];
		right = animationFrame2;
	}

	public Animation Clone()
	{
		return new Animation
		{
			Code = Code,
			CodeCrc32 = CodeCrc32,
			EaseAnimationSpeed = EaseAnimationSpeed,
			jointsDone = jointsDone,
			KeyFrames = CloneKeyFrames(),
			Name = Name,
			OnActivityStopped = OnActivityStopped,
			OnAnimationEnd = OnAnimationEnd,
			QuantityFrames = QuantityFrames,
			Version = Version
		};
	}

	private AnimationKeyFrame[] CloneKeyFrames()
	{
		AnimationKeyFrame[] array = new AnimationKeyFrame[KeyFrames.Length];
		for (int i = 0; i < KeyFrames.Length; i++)
		{
			array[i] = KeyFrames[i].Clone();
		}
		return array;
	}
}
