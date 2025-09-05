using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class RemapAnimationManager : AnimationManager
{
	public Dictionary<string, string> Remaps = new Dictionary<string, string>();

	public string IdleAnimation = "crawlidle";

	public RemapAnimationManager()
	{
	}

	public RemapAnimationManager(Dictionary<string, string> remaps)
	{
		Remaps = remaps;
	}

	public override bool StartAnimation(string configCode)
	{
		if (Remaps.ContainsKey(configCode.ToLowerInvariant()))
		{
			configCode = Remaps[configCode];
		}
		StopIdle();
		return base.StartAnimation(configCode);
	}

	public override bool StartAnimation(AnimationMetaData animdata)
	{
		if (Remaps.ContainsKey(animdata.Animation))
		{
			animdata = animdata.Clone();
			animdata.Animation = Remaps[animdata.Animation];
			animdata.CodeCrc32 = AnimationMetaData.GetCrc32(animdata.Animation);
		}
		StopIdle();
		return base.StartAnimation(animdata);
	}

	public override void StopAnimation(string code)
	{
		base.StopAnimation(code);
		if (Remaps.ContainsKey(code))
		{
			base.StopAnimation(Remaps[code]);
		}
	}

	public override void TriggerAnimationStopped(string code)
	{
		base.TriggerAnimationStopped(code);
		if (entity.Alive && ActiveAnimationsByAnimCode.Count == 0)
		{
			StartAnimation(new AnimationMetaData
			{
				Code = "idle",
				Animation = "idle",
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			});
		}
	}

	private void StopIdle()
	{
		StopAnimation(IdleAnimation);
	}
}
