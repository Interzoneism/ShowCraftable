using Newtonsoft.Json;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class AnimationTrigger
{
	[JsonProperty]
	public EnumEntityActivity[] OnControls;

	[JsonProperty]
	public bool MatchExact;

	[JsonProperty]
	public bool DefaultAnim;

	public AnimationTrigger Clone()
	{
		return new AnimationTrigger
		{
			OnControls = (EnumEntityActivity[])OnControls?.Clone(),
			MatchExact = MatchExact,
			DefaultAnim = DefaultAnim
		};
	}
}
