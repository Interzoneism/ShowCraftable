using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public abstract class AnimatorBase : IAnimator
{
	public static readonly float[] identMat = Mat4f.Create();

	public static readonly HashSet<string> logAntiSpam = new HashSet<string>();

	private WalkSpeedSupplierDelegate WalkSpeedSupplier;

	private Action<string> onAnimationStoppedListener;

	protected int activeAnimCount;

	public ShapeElement[] RootElements;

	public List<ElementPose> RootPoses;

	public RunningAnimation[] anims;

	private readonly Dictionary<string, RunningAnimation> animsByCode;

	public float[] TransformationMatrices;

	public float[] TransformationMatricesDefaultPose;

	public Dictionary<string, AttachmentPointAndPose> AttachmentPointByCode = new Dictionary<string, AttachmentPointAndPose>();

	public RunningAnimation[] CurAnims = new RunningAnimation[20];

	private readonly List<RunningAnimation> activeOrRunning = new List<RunningAnimation>(2);

	public Entity entityForLogging;

	private float accum = 0.25f;

	private double walkSpeed;

	public bool CalculateMatrices { get; set; } = true;

	public float[] Matrices
	{
		get
		{
			if (activeAnimCount <= 0)
			{
				return TransformationMatricesDefaultPose;
			}
			return TransformationMatrices;
		}
	}

	public int ActiveAnimationCount => activeAnimCount;

	[Obsolete("Use Animations instead")]
	public RunningAnimation[] RunningAnimations => Animations;

	public RunningAnimation[] Animations => anims;

	public abstract int MaxJointId { get; }

	public RunningAnimation GetAnimationState(string code)
	{
		if (code == null)
		{
			return null;
		}
		animsByCode.TryGetValue(code.ToLowerInvariant(), out var value);
		return value;
	}

	public AnimatorBase(WalkSpeedSupplierDelegate WalkSpeedSupplier, Animation[] Animations, Action<string> onAnimationStoppedListener = null)
	{
		this.WalkSpeedSupplier = WalkSpeedSupplier;
		this.onAnimationStoppedListener = onAnimationStoppedListener;
		RunningAnimation[] array = (anims = ((Animations == null) ? Array.Empty<RunningAnimation>() : new RunningAnimation[Animations.Length]));
		animsByCode = new Dictionary<string, RunningAnimation>(array.Length);
		Dictionary<string, RunningAnimation> dictionary = animsByCode;
		for (int i = 0; i < array.Length; i++)
		{
			Animation animation = Animations[i];
			animation.Code = animation.Code.ToLowerInvariant();
			RunningAnimation value = (array[i] = new RunningAnimation
			{
				Animation = animation
			});
			dictionary[animation.Code] = value;
		}
	}

	public virtual void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
		activeAnimCount = 0;
		if ((accum += dt) > 0.25f)
		{
			walkSpeed = ((WalkSpeedSupplier == null) ? 1.0 : WalkSpeedSupplier());
			accum = 0f;
		}
		string text = null;
		foreach (string key in activeAnimationsByAnimCode.Keys)
		{
			if (!animsByCode.TryGetValue(key.ToLowerInvariant(), out var value))
			{
				text = key;
			}
			else if (!value.Active)
			{
				AnimNowActive(value, activeAnimationsByAnimCode[key]);
			}
		}
		if (text != null)
		{
			activeAnimationsByAnimCode.Remove(text);
			if (entityForLogging != null)
			{
				string text2 = entityForLogging.Code.ToShortString();
				string item = text2 + "|" + text;
				if (logAntiSpam.Add(item))
				{
					entityForLogging.World.Logger.Debug(text2 + " attempted to play an animation code which its shape does not have: \"" + text + "\"");
				}
			}
		}
		List<RunningAnimation> list = activeOrRunning;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			RunningAnimation runningAnimation = list[num];
			if (runningAnimation.Active && !activeAnimationsByAnimCode.ContainsKey(runningAnimation.Animation.Code))
			{
				runningAnimation.Active = false;
				EnumEntityActivityStoppedHandling onActivityStopped = runningAnimation.Animation.OnActivityStopped;
				if (onActivityStopped == EnumEntityActivityStoppedHandling.Rewind)
				{
					runningAnimation.ShouldRewind = true;
				}
				if (onActivityStopped == EnumEntityActivityStoppedHandling.Stop)
				{
					runningAnimation.Stop();
					onAnimationStoppedListener?.Invoke(runningAnimation.Animation.Code);
				}
				if (onActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd)
				{
					runningAnimation.ShouldPlayTillEnd = true;
				}
			}
			if (runningAnimation.Running && !ProgressRunningAnimation(runningAnimation, dt))
			{
				string code = runningAnimation.Animation.Code;
				activeAnimationsByAnimCode.Remove(code);
				onAnimationStoppedListener?.Invoke(code);
			}
			if (!runningAnimation.Active && !runningAnimation.Running)
			{
				list.RemoveAt(num);
			}
		}
		calculateMatrices(dt);
	}

	private bool ProgressRunningAnimation(RunningAnimation anim, float dt)
	{
		EnumEntityAnimationEndHandling onAnimationEnd = anim.Animation.OnAnimationEnd;
		EnumEntityActivityStoppedHandling onActivityStopped = anim.Animation.OnActivityStopped;
		bool flag = false;
		if (anim.Iterations > 0)
		{
			flag = onAnimationEnd == EnumEntityAnimationEndHandling.Stop || (!anim.Active && (onActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd || onActivityStopped == EnumEntityActivityStoppedHandling.EaseOut) && anim.EasingFactor < 0.002f) || (onAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && anim.EasingFactor < 0.002f);
		}
		else if (anim.Iterations < 0)
		{
			flag = !anim.Active && onActivityStopped == EnumEntityActivityStoppedHandling.Rewind && anim.EasingFactor < 0.002f;
		}
		if (flag)
		{
			anim.Stop();
			if (onAnimationEnd == EnumEntityAnimationEndHandling.Stop || onAnimationEnd == EnumEntityAnimationEndHandling.EaseOut)
			{
				return false;
			}
			return true;
		}
		CurAnims[activeAnimCount++] = anim;
		if (anim.Iterations != 0 && ((!anim.Active && onAnimationEnd == EnumEntityAnimationEndHandling.Hold) || onAnimationEnd == EnumEntityAnimationEndHandling.EaseOut))
		{
			anim.EaseOut(dt);
		}
		anim.Progress(dt, (float)walkSpeed);
		return true;
	}

	public virtual string DumpCurrentState()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < anims.Length; i++)
		{
			RunningAnimation runningAnimation = anims[i];
			if (runningAnimation.Active && runningAnimation.Running)
			{
				stringBuilder.Append("Active&Running: " + runningAnimation.Animation.Code);
			}
			else if (runningAnimation.Active)
			{
				stringBuilder.Append("Active: " + runningAnimation.Animation.Code);
			}
			else
			{
				if (!runningAnimation.Running)
				{
					continue;
				}
				stringBuilder.Append("Running: " + runningAnimation.Animation.Code);
			}
			stringBuilder.Append(", easing: " + runningAnimation.EasingFactor);
			stringBuilder.Append(", currentframe: " + runningAnimation.CurrentFrame);
			stringBuilder.Append(", iterations: " + runningAnimation.Iterations);
			stringBuilder.Append(", blendedweight: " + runningAnimation.BlendedWeight);
			stringBuilder.Append(", animmetacode: " + runningAnimation.meta.Code);
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}

	protected virtual void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
	{
		anim.Running = true;
		anim.Active = true;
		if (!activeOrRunning.Contains(anim))
		{
			activeOrRunning.Add(anim);
		}
		anim.meta = animData;
		anim.ShouldRewind = false;
		anim.ShouldPlayTillEnd = false;
		anim.CurrentFrame = animData.StartFrameOnce;
		animData.StartFrameOnce = 0f;
	}

	protected abstract void calculateMatrices(float dt);

	public AttachmentPointAndPose GetAttachmentPointPose(string code)
	{
		AttachmentPointByCode.TryGetValue(code, out var value);
		return value;
	}

	public virtual ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		throw new NotImplementedException();
	}

	public virtual void ReloadAttachmentPoints()
	{
		throw new NotImplementedException();
	}
}
