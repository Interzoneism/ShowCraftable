using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Common.Network.Packets;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class AnimationPacket
{
	public long entityId;

	public int[] activeAnimations;

	public int activeAnimationsCount;

	public int activeAnimationsLength;

	public int[] activeAnimationSpeeds;

	public int activeAnimationSpeedsCount;

	public int activeAnimationSpeedsLength;

	public AnimationPacket()
	{
	}

	public AnimationPacket(Entity entity)
	{
		entityId = entity.EntityId;
		if (entity.AnimManager == null)
		{
			return;
		}
		Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode = entity.AnimManager.ActiveAnimationsByAnimCode;
		if (activeAnimationsByAnimCode.Count <= 0)
		{
			return;
		}
		int[] array = new int[activeAnimationsByAnimCode.Count];
		int[] array2 = new int[activeAnimationsByAnimCode.Count];
		int num = 0;
		foreach (KeyValuePair<string, AnimationMetaData> item in activeAnimationsByAnimCode)
		{
			AnimationTrigger triggeredBy = item.Value.TriggeredBy;
			if (triggeredBy == null || !triggeredBy.DefaultAnim)
			{
				array2[num] = CollectibleNet.SerializeFloatPrecise(item.Value.AnimationSpeed);
				array[num++] = (int)item.Value.CodeCrc32;
			}
		}
		activeAnimations = array;
		activeAnimationsCount = num;
		activeAnimationsLength = array.Length;
		activeAnimationSpeeds = array2;
		activeAnimationSpeedsCount = num;
		activeAnimationSpeedsLength = array2.Length;
	}
}
