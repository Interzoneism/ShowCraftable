using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class ClientAnimator : AnimatorBase
{
	public Dictionary<int, AnimationJoint> jointsById;

	protected HashSet<int> jointsDone = new HashSet<int>();

	public static int MaxConcurrentAnimations = 16;

	private int maxDepth;

	private List<ElementPose>[][] frameByDepthByAnimation;

	private List<ElementPose>[][] nextFrameTransformsByAnimation;

	private ShapeElementWeights[][][] weightsByAnimationAndElement;

	private float[] localTransformMatrix = Mat4f.Create();

	private float[] tmpMatrix = Mat4f.Create();

	private Action<AnimationSound> onShouldPlaySoundListener;

	private int[] prevFrame = new int[MaxConcurrentAnimations];

	private int[] nextFrame = new int[MaxConcurrentAnimations];

	private static bool EleWeightDebug = false;

	private Dictionary<string, string> eleWeights = new Dictionary<string, string>();

	public override int MaxJointId => jointsById.Count + 1;

	public static ClientAnimator CreateForEntity(Entity entity, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
		}
		return new ClientAnimator(() => 1.0, rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
	}

	public static ClientAnimator CreateForEntity(Entity entity, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
		}
		return new ClientAnimator(() => 1.0, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		initFields();
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		this.jointsById = jointsById;
		RootPoses = rootPoses;
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		LoadAttachmentPoints(RootPoses);
		initFields();
		initMatrices(MaxJointId);
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		this.jointsById = jointsById;
		RootPoses = new List<ElementPose>();
		LoadPosesAndAttachmentPoints(rootElements, RootPoses);
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		initFields();
		initMatrices(MaxJointId);
	}

	protected virtual void initFields()
	{
		maxDepth = 2 + ((RootPoses != null) ? getMaxDepth(RootPoses, 1) : 0);
		frameByDepthByAnimation = new List<ElementPose>[maxDepth][];
		nextFrameTransformsByAnimation = new List<ElementPose>[maxDepth][];
		weightsByAnimationAndElement = new ShapeElementWeights[maxDepth][][];
		for (int i = 0; i < maxDepth; i++)
		{
			frameByDepthByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
			nextFrameTransformsByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
			weightsByAnimationAndElement[i] = new ShapeElementWeights[MaxConcurrentAnimations][];
		}
	}

	protected virtual void initMatrices(int maxJointId)
	{
		float[] array = AnimatorBase.identMat;
		float[] array2 = new float[16 * maxJointId];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = array[i % 16];
		}
		TransformationMatricesDefaultPose = array2;
		TransformationMatrices = new float[array2.Length];
	}

	public override void ReloadAttachmentPoints()
	{
		LoadAttachmentPoints(RootPoses);
	}

	protected virtual void LoadAttachmentPoints(List<ElementPose> cachedPoses)
	{
		for (int i = 0; i < cachedPoses.Count; i++)
		{
			ElementPose elementPose = cachedPoses[i];
			AttachmentPoint[] attachmentPoints = elementPose.ForElement.AttachmentPoints;
			if (attachmentPoints != null)
			{
				foreach (AttachmentPoint attachmentPoint in attachmentPoints)
				{
					AttachmentPointByCode[attachmentPoint.Code] = new AttachmentPointAndPose
					{
						AttachPoint = attachmentPoint,
						CachedPose = elementPose
					};
				}
			}
			if (elementPose.ChildElementPoses != null)
			{
				LoadAttachmentPoints(elementPose.ChildElementPoses);
			}
		}
	}

	protected virtual void LoadPosesAndAttachmentPoints(ShapeElement[] elements, List<ElementPose> intoPoses)
	{
		foreach (ShapeElement shapeElement in elements)
		{
			ElementPose elementPose;
			intoPoses.Add(elementPose = new ElementPose());
			elementPose.AnimModelMatrix = Mat4f.Create();
			elementPose.ForElement = shapeElement;
			if (shapeElement.AttachmentPoints != null)
			{
				for (int j = 0; j < shapeElement.AttachmentPoints.Length; j++)
				{
					AttachmentPoint attachmentPoint = shapeElement.AttachmentPoints[j];
					AttachmentPointByCode[attachmentPoint.Code] = new AttachmentPointAndPose
					{
						AttachPoint = attachmentPoint,
						CachedPose = elementPose
					};
				}
			}
			if (shapeElement.Children != null)
			{
				elementPose.ChildElementPoses = new List<ElementPose>(shapeElement.Children.Length);
				LoadPosesAndAttachmentPoints(shapeElement.Children, elementPose.ChildElementPoses);
			}
		}
	}

	private int getMaxDepth(List<ElementPose> poses, int depth)
	{
		for (int i = 0; i < poses.Count; i++)
		{
			ElementPose elementPose = poses[i];
			if (elementPose.ChildElementPoses != null)
			{
				depth = getMaxDepth(elementPose.ChildElementPoses, depth);
			}
		}
		return depth + 1;
	}

	public override ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		return getPosebyName(RootPoses, name);
	}

	private ElementPose getPosebyName(List<ElementPose> poses, string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		for (int i = 0; i < poses.Count; i++)
		{
			ElementPose elementPose = poses[i];
			if (elementPose.ForElement.Name.Equals(name, stringComparison))
			{
				return elementPose;
			}
			if (elementPose.ChildElementPoses != null)
			{
				ElementPose posebyName = getPosebyName(elementPose.ChildElementPoses, name);
				if (posebyName != null)
				{
					return posebyName;
				}
			}
		}
		return null;
	}

	protected override void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
	{
		base.AnimNowActive(anim, animData);
		if (anim.Animation.PrevNextKeyFrameByFrame == null)
		{
			anim.Animation.GenerateAllFrames(RootElements, jointsById);
		}
		anim.LoadWeights(RootElements);
	}

	public override void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
		for (int i = 0; i < activeAnimCount; i++)
		{
			RunningAnimation runningAnimation = CurAnims[i];
			if (runningAnimation.Animation.PrevNextKeyFrameByFrame == null && runningAnimation.Animation.KeyFrames.Length != 0)
			{
				runningAnimation.Animation.GenerateAllFrames(RootElements, jointsById);
			}
			if (runningAnimation.meta.AnimationSound != null && onShouldPlaySoundListener != null && runningAnimation.CurrentFrame >= (float)runningAnimation.meta.AnimationSound.Frame && runningAnimation.SoundPlayedAtIteration != runningAnimation.Iterations && runningAnimation.Active)
			{
				onShouldPlaySoundListener(runningAnimation.meta.AnimationSound);
				runningAnimation.SoundPlayedAtIteration = runningAnimation.Iterations;
			}
		}
		base.OnFrame(activeAnimationsByAnimCode, dt);
	}

	protected override void calculateMatrices(float dt)
	{
		if (!base.CalculateMatrices)
		{
			return;
		}
		jointsDone.Clear();
		int num = 0;
		for (int i = 0; i < activeAnimCount; i++)
		{
			RunningAnimation runningAnimation = CurAnims[i];
			weightsByAnimationAndElement[0][i] = runningAnimation.ElementWeights;
			num = Math.Max(num, runningAnimation.Animation.Version);
			AnimationFrame[] array = runningAnimation.Animation.PrevNextKeyFrameByFrame[(int)runningAnimation.CurrentFrame % runningAnimation.Animation.QuantityFrames];
			frameByDepthByAnimation[0][i] = array[0].RootElementTransforms;
			prevFrame[i] = array[0].FrameNumber;
			if (runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && (int)runningAnimation.CurrentFrame + 1 == runningAnimation.Animation.QuantityFrames)
			{
				nextFrameTransformsByAnimation[0][i] = array[0].RootElementTransforms;
				nextFrame[i] = array[0].FrameNumber;
			}
			else
			{
				nextFrameTransformsByAnimation[0][i] = array[1].RootElementTransforms;
				nextFrame[i] = array[1].FrameNumber;
			}
		}
		calculateMatrices(num, dt, RootPoses, weightsByAnimationAndElement[0], Mat4f.Create(), frameByDepthByAnimation[0], nextFrameTransformsByAnimation[0], 0);
		float[] transformationMatrices = TransformationMatrices;
		if (transformationMatrices != null)
		{
			for (int j = 0; j < transformationMatrices.Length; j += 16)
			{
				if (!jointsById.ContainsKey(j / 16))
				{
					for (int k = 0; k < 16; k++)
					{
						transformationMatrices[j + k] = AnimatorBase.identMat[k];
					}
				}
			}
		}
		foreach (KeyValuePair<string, AttachmentPointAndPose> item in AttachmentPointByCode)
		{
			float[] animModelMatrix = item.Value.CachedPose.AnimModelMatrix;
			float[] animModelMatrix2 = item.Value.AnimModelMatrix;
			for (int l = 0; l < 16; l++)
			{
				animModelMatrix2[l] = animModelMatrix[l];
			}
		}
	}

	private void calculateMatrices(int animVersion, float dt, List<ElementPose> outFrame, ShapeElementWeights[][] weightsByAnimationAndElement, float[] modelMatrix, List<ElementPose>[] nowKeyFrameByAnimation, List<ElementPose>[] nextInKeyFrameByAnimation, int depth)
	{
		depth++;
		List<ElementPose>[] array = frameByDepthByAnimation[depth];
		List<ElementPose>[] array2 = nextFrameTransformsByAnimation[depth];
		ShapeElementWeights[][] array3 = this.weightsByAnimationAndElement[depth];
		for (int i = 0; i < outFrame.Count; i++)
		{
			ElementPose elementPose = outFrame[i];
			ShapeElement forElement = elementPose.ForElement;
			elementPose.SetMat(modelMatrix);
			Mat4f.Identity(localTransformMatrix);
			elementPose.Clear();
			float num = 0f;
			for (int j = 0; j < activeAnimCount; j++)
			{
				RunningAnimation runningAnimation = CurAnims[j];
				ShapeElementWeights shapeElementWeights = weightsByAnimationAndElement[j][i];
				if (shapeElementWeights.BlendMode != EnumAnimationBlendMode.Add)
				{
					num += shapeElementWeights.Weight * runningAnimation.EasingFactor;
				}
			}
			for (int k = 0; k < activeAnimCount; k++)
			{
				RunningAnimation runningAnimation2 = CurAnims[k];
				ShapeElementWeights shapeElementWeights2 = weightsByAnimationAndElement[k][i];
				runningAnimation2.CalcBlendedWeight(num / shapeElementWeights2.Weight, shapeElementWeights2.BlendMode);
				ElementPose elementPose2 = nowKeyFrameByAnimation[k][i];
				ElementPose elementPose3 = nextInKeyFrameByAnimation[k][i];
				int num2 = prevFrame[k];
				int num3 = nextFrame[k];
				float num4 = ((num3 > num2) ? (num3 - num2) : (runningAnimation2.Animation.QuantityFrames - num2 + num3));
				float l = ((runningAnimation2.CurrentFrame >= (float)num2) ? (runningAnimation2.CurrentFrame - (float)num2) : ((float)(runningAnimation2.Animation.QuantityFrames - num2) + runningAnimation2.CurrentFrame)) / num4;
				elementPose.Add(elementPose2, elementPose3, l, runningAnimation2.BlendedWeight);
				array[k] = elementPose2.ChildElementPoses;
				array3[k] = shapeElementWeights2.ChildElements;
				array2[k] = elementPose3.ChildElementPoses;
			}
			forElement.GetLocalTransformMatrix(animVersion, localTransformMatrix, elementPose);
			Mat4f.Mul(elementPose.AnimModelMatrix, elementPose.AnimModelMatrix, localTransformMatrix);
			if (TransformationMatrices != null && forElement.JointId > 0 && !jointsDone.Contains(forElement.JointId))
			{
				Mat4f.Mul(tmpMatrix, elementPose.AnimModelMatrix, forElement.inverseModelTransform);
				int num5 = 16 * forElement.JointId;
				float[] transformationMatrices = TransformationMatrices;
				float[] array4 = tmpMatrix;
				if (num5 + 16 > transformationMatrices.Length)
				{
					float[] transformationMatricesDefaultPose = TransformationMatricesDefaultPose;
					initMatrices(forElement.JointId + 1);
					Array.Copy(transformationMatrices, TransformationMatrices, transformationMatrices.Length);
					Array.Copy(transformationMatricesDefaultPose, TransformationMatricesDefaultPose, transformationMatricesDefaultPose.Length);
					transformationMatrices = TransformationMatrices;
				}
				for (int m = 0; m < 16; m++)
				{
					transformationMatrices[num5 + m] = array4[m];
				}
				jointsDone.Add(forElement.JointId);
			}
			if (elementPose.ChildElementPoses != null)
			{
				calculateMatrices(animVersion, dt, elementPose.ChildElementPoses, array3, elementPose.AnimModelMatrix, array, array2, depth);
			}
		}
	}

	public override string DumpCurrentState()
	{
		EleWeightDebug = true;
		eleWeights.Clear();
		calculateMatrices(1f / 60f);
		EleWeightDebug = false;
		return base.DumpCurrentState() + "\nElement weights:\n" + string.Join("\n", eleWeights.Select((KeyValuePair<string, string> x) => x.Key + ": " + x.Value));
	}
}
