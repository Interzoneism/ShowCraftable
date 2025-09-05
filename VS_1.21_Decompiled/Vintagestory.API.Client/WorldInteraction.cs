using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class WorldInteraction
{
	[JsonProperty]
	public EnumMouseButton MouseButton;

	[JsonProperty]
	public string HotKeyCode;

	[JsonProperty]
	public string ActionLangCode;

	[JsonProperty("ItemStacks")]
	public JsonItemStack[] JsonItemStacks;

	public ItemStack[] Itemstacks;

	public bool RequireFreeHand;

	public InteractionStacksDelegate GetMatchingStacks;

	public InteractionMatcherDelegate ShouldApply;

	[JsonProperty]
	public string[] HotKeyCodes { get; set; }
}
