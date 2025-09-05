using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class AnimationManager : IAnimationManager, IDisposable
{
	protected ICoreAPI api;

	protected ICoreClientAPI capi;

	[ThreadStatic]
	private static FastMemoryStream reusableStream;

	public Dictionary<string, AnimationMetaData> ActiveAnimationsByAnimCode = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);

	public List<AnimFrameCallback> Triggers;

	protected Entity entity;

	public bool AnimationsDirty { get; set; }

	public IAnimator Animator { get; set; }

	public EntityHeadController HeadController { get; set; }

	Dictionary<string, AnimationMetaData> IAnimationManager.ActiveAnimationsByAnimCode => ActiveAnimationsByAnimCode;

	public bool AdjustCollisionBoxToAnimation { get; set; }

	public event StartAnimationDelegate OnStartAnimation;

	public event StartAnimationDelegate OnAnimationReceived;

	public event Action<string> OnAnimationStopped;

	public virtual void Init(ICoreAPI api, Entity entity)
	{
		this.api = api;
		this.entity = entity;
		capi = api as ICoreClientAPI;
	}

	public IAnimator LoadAnimator(ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		Init(entity.Api, entity);
		if (entityShape == null)
		{
			return null;
		}
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes != null && attributes["requireJointsForElements"].Exists)
		{
			requireJointsForElements = requireJointsForElements.Append(entity.Properties.Attributes["requireJointsForElements"].AsArray<string>());
		}
		entityShape.InitForAnimations(api.Logger, entity.Properties.Client.ShapeForEntity.Base.ToString(), requireJointsForElements);
		IAnimator animator = (Animator = ((api.Side == EnumAppSide.Client) ? ClientAnimator.CreateForEntity(entity, entityShape.Animations, entityShape.Elements, entityShape.JointsById) : ServerAnimator.CreateForEntity(entity, entityShape.Animations, entityShape.Elements, entityShape.JointsById, requirePosesOnServer)));
		CopyOverAnimStates(copyOverAnims, animator);
		return animator;
	}

	public void CopyOverAnimStates(RunningAnimation[] copyOverAnims, IAnimator animator)
	{
		if (copyOverAnims == null || animator == null)
		{
			return;
		}
		foreach (RunningAnimation runningAnimation in copyOverAnims)
		{
			if (runningAnimation != null && runningAnimation.Active)
			{
				ActiveAnimationsByAnimCode.TryGetValue(runningAnimation.Animation.Code, out var value);
				if (value != null)
				{
					value.StartFrameOnce = runningAnimation.CurrentFrame;
				}
			}
		}
	}

	public virtual bool IsAnimationActive(params string[] anims)
	{
		foreach (string key in anims)
		{
			if (ActiveAnimationsByAnimCode.ContainsKey(key))
			{
				return true;
			}
		}
		return false;
	}

	public virtual RunningAnimation GetAnimationState(string anim)
	{
		return Animator.GetAnimationState(anim);
	}

	public virtual void ResetAnimation(string animCode)
	{
		if (animCode != null)
		{
			RunningAnimation runningAnimation = Animator?.GetAnimationState(animCode);
			if (runningAnimation != null)
			{
				runningAnimation.CurrentFrame = 0f;
				runningAnimation.Iterations = 0;
			}
		}
	}

	public virtual bool TryStartAnimation(AnimationMetaData animdata)
	{
		if (((AnimatorBase)Animator).GetAnimationState(animdata.Animation) == null)
		{
			return false;
		}
		return StartAnimation(animdata);
	}

	public virtual bool StartAnimation(AnimationMetaData animdata)
	{
		if (this.OnStartAnimation != null)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag = false;
			bool result = false;
			Delegate[] invocationList = this.OnStartAnimation.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				result = ((StartAnimationDelegate)invocationList[i])(ref animdata, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					return result;
				}
				flag = handling == EnumHandling.PreventDefault;
			}
			if (flag)
			{
				return result;
			}
		}
		if (ActiveAnimationsByAnimCode.TryGetValue(animdata.Animation, out var value) && value == animdata)
		{
			return false;
		}
		if (animdata.Code == null)
		{
			throw new Exception("anim meta data code cannot be null!");
		}
		AnimationsDirty = true;
		ActiveAnimationsByAnimCode[animdata.Animation] = animdata;
		entity?.UpdateDebugAttributes();
		return true;
	}

	public virtual bool StartAnimation(string configCode)
	{
		if (configCode == null)
		{
			return false;
		}
		if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode, out var value))
		{
			StartAnimation(value);
			return true;
		}
		return false;
	}

	public virtual void StopAnimation(string code)
	{
		if (code == null || entity == null)
		{
			return;
		}
		if (entity.World.Side == EnumAppSide.Server)
		{
			AnimationsDirty = true;
		}
		if (!ActiveAnimationsByAnimCode.Remove(code) && ActiveAnimationsByAnimCode.Count > 0)
		{
			foreach (KeyValuePair<string, AnimationMetaData> item in ActiveAnimationsByAnimCode)
			{
				if (item.Value.Code == code)
				{
					ActiveAnimationsByAnimCode.Remove(item.Key);
					break;
				}
			}
		}
		if (entity.World.EntityDebugMode)
		{
			entity.UpdateDebugAttributes();
		}
	}

	public virtual void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		HashSet<string> hashSet = new HashSet<string>();
		string text = "";
		for (int i = 0; i < activeAnimationsCount; i++)
		{
			uint key = (uint)activeAnimations[i];
			if (entity.Properties.Client.AnimationsByCrc32.TryGetValue(key, out var value))
			{
				hashSet.Add(value.Animation);
				if (!ActiveAnimationsByAnimCode.ContainsKey(value.Code))
				{
					value.AnimationSpeed = activeAnimationSpeeds[i];
					onReceivedServerAnimation(value);
				}
			}
			else
			{
				if (!entity.Properties.Client.LoadedShapeForEntity.AnimationsByCrc32.TryGetValue(key, out var value2))
				{
					continue;
				}
				hashSet.Add(value2.Code);
				if (!ActiveAnimationsByAnimCode.ContainsKey(value2.Code))
				{
					string text2 = ((value2.Code == null) ? value2.Name.ToLowerInvariant() : value2.Code);
					text = text + ", " + text2;
					entity.Properties.Client.AnimationsByMetaCode.TryGetValue(text2, out var value3);
					if (value3 == null)
					{
						value3 = new AnimationMetaData
						{
							Code = text2,
							Animation = text2,
							CodeCrc32 = value2.CodeCrc32
						};
					}
					value3.AnimationSpeed = activeAnimationSpeeds[i];
					onReceivedServerAnimation(value3);
				}
			}
		}
		if (entity.EntityId == (entity.World as IClientWorldAccessor).Player.Entity.EntityId)
		{
			return;
		}
		string[] array = ActiveAnimationsByAnimCode.Keys.ToArray();
		foreach (string text3 in array)
		{
			AnimationMetaData animationMetaData = ActiveAnimationsByAnimCode[text3];
			if (!hashSet.Contains(text3) && !animationMetaData.ClientSide && (!entity.Properties.Client.AnimationsByMetaCode.TryGetValue(text3, out var value4) || value4.TriggeredBy == null || !value4.WasStartedFromTrigger))
			{
				ActiveAnimationsByAnimCode.Remove(text3);
			}
		}
	}

	protected virtual void onReceivedServerAnimation(AnimationMetaData animmetadata)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		this.OnAnimationReceived?.Invoke(ref animmetadata, ref handling);
		if (handling == EnumHandling.PassThrough)
		{
			ActiveAnimationsByAnimCode[animmetadata.Animation] = animmetadata;
		}
	}

	public virtual void ToAttributes(ITreeAttribute tree, bool forClient)
	{
		if (Animator != null)
		{
			ITreeAttribute animtree = new TreeAttribute();
			tree["activeAnims"] = animtree;
			SerializeActiveAnimations(forClient, delegate(string code, FastMemoryStream ms)
			{
				animtree[code] = new ByteArrayAttribute(ms);
			});
		}
	}

	public virtual void ToAttributeBytes(BinaryWriter stream, bool forClient)
	{
		if (Animator != null)
		{
			StreamedTreeAttribute streamedAnimTree = new StreamedTreeAttribute(stream);
			streamedAnimTree.WithKey("activeAnims");
			SerializeActiveAnimations(forClient, delegate(string code, FastMemoryStream ms)
			{
				streamedAnimTree[code] = new StreamedByteArrayAttribute(ms);
			});
			streamedAnimTree.EndKey();
		}
	}

	protected virtual void SerializeActiveAnimations(bool forClient, Action<string, FastMemoryStream> output)
	{
		if (ActiveAnimationsByAnimCode.Count == 0)
		{
			return;
		}
		using FastMemoryStream fastMemoryStream = reusableStream ?? (reusableStream = new FastMemoryStream());
		using BinaryWriter writer = new BinaryWriter(fastMemoryStream);
		foreach (KeyValuePair<string, AnimationMetaData> item in ActiveAnimationsByAnimCode)
		{
			if (item.Value.Code == null)
			{
				item.Value.Code = item.Key;
			}
			if (forClient || !(item.Value.Code != "die"))
			{
				RunningAnimation animationState = Animator.GetAnimationState(item.Value.Animation);
				if (animationState != null)
				{
					item.Value.StartFrameOnce = animationState.CurrentFrame;
				}
				fastMemoryStream.Reset();
				item.Value.ToBytes(writer);
				item.Value.StartFrameOnce = 0f;
				output(item.Key, fastMemoryStream);
			}
		}
	}

	public virtual void FromAttributes(ITreeAttribute tree, string version)
	{
		if (!(tree["activeAnims"] is ITreeAttribute treeAttribute))
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			using MemoryStream input = new MemoryStream((item.Value as ByteArrayAttribute).value);
			using BinaryReader reader = new BinaryReader(input);
			ActiveAnimationsByAnimCode[item.Key] = AnimationMetaData.FromBytes(reader, version);
		}
	}

	public virtual void OnServerTick(float dt)
	{
		if (Animator != null)
		{
			Animator.CalculateMatrices = !entity.Alive || entity.requirePosesOnServer;
			Animator.OnFrame(ActiveAnimationsByAnimCode, dt);
			AdjustCollisionBoxToAnimation = ActiveAnimationsByAnimCode.Any((KeyValuePair<string, AnimationMetaData> anim) => anim.Value.AdjustCollisionBox);
		}
		runTriggers();
	}

	public virtual void OnClientFrame(float dt)
	{
		if (capi.IsGamePaused || Animator == null)
		{
			return;
		}
		if (HeadController != null)
		{
			HeadController.OnFrame(dt);
		}
		if (entity.IsRendered || entity.IsShadowRendered || !entity.Alive)
		{
			Animator.OnFrame(ActiveAnimationsByAnimCode, dt);
			AdjustCollisionBoxToAnimation = ActiveAnimationsByAnimCode.Any((KeyValuePair<string, AnimationMetaData> anim) => anim.Value.AdjustCollisionBox);
			runTriggers();
		}
	}

	public virtual void RegisterFrameCallback(AnimFrameCallback trigger)
	{
		if (Triggers == null)
		{
			Triggers = new List<AnimFrameCallback>();
		}
		Triggers.Add(trigger);
	}

	private void runTriggers()
	{
		List<AnimFrameCallback> triggers = Triggers;
		if (triggers == null)
		{
			return;
		}
		for (int i = 0; i < triggers.Count; i++)
		{
			AnimFrameCallback animFrameCallback = triggers[i];
			if (ActiveAnimationsByAnimCode.ContainsKey(animFrameCallback.Animation))
			{
				RunningAnimation animationState = Animator.GetAnimationState(animFrameCallback.Animation);
				if (animationState != null && animationState.CurrentFrame >= animFrameCallback.Frame)
				{
					triggers.RemoveAt(i);
					animFrameCallback.Callback();
					i--;
				}
			}
		}
	}

	public void Dispose()
	{
	}

	public virtual void TriggerAnimationStopped(string code)
	{
		this.OnAnimationStopped?.Invoke(code);
	}

	public void ShouldPlaySound(AnimationSound sound)
	{
		entity.World.PlaySoundAt(sound.Location, entity, null, sound.RandomizePitch, sound.Range);
	}
}
