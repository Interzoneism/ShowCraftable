using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class PlaySoundAction : EntityActionBase
{
	[JsonProperty]
	public float delaySeconds;

	[JsonProperty]
	public string soundLocation;

	[JsonProperty]
	public bool randomizePitch = true;

	[JsonProperty]
	public float range = 32f;

	[JsonProperty]
	public float volume = 1f;

	public override string Type => "playsound";

	public PlaySoundAction(EntityActivitySystem vas, float delaySeconds, string soundLocation, bool randomizePitch, float range, float volume)
	{
		base.vas = vas;
		this.delaySeconds = delaySeconds;
		this.soundLocation = soundLocation;
		this.randomizePitch = randomizePitch;
		this.range = range;
		this.volume = volume;
	}

	public PlaySoundAction()
	{
	}

	public override void Start(EntityActivity act)
	{
		if (delaySeconds > 0f)
		{
			vas.Entity.Api.Event.RegisterCallback(playsound, (int)(delaySeconds * 1000f));
		}
		else
		{
			playsound(0f);
		}
	}

	private void playsound(float dt)
	{
		AssetLocation location = new AssetLocation(soundLocation).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg");
		vas.Entity.World.PlaySoundAt(location, vas.Entity, null, randomizePitch, range, volume);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Sound Location", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "soundlocation").AddStaticText("Delay in Seconds", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "delay")
			.AddStaticText("Range", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "range")
			.AddStaticText("Volume", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "volume")
			.AddSwitch(null, elementBounds.BelowCopy(0.0, 10.0), "randomizepitch", 20.0, 2.0)
			.AddStaticText("Randomize pitch", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(25.0, 12.0));
		singleComposer.GetTextInput("soundlocation").SetValue(soundLocation);
		singleComposer.GetNumberInput("delay").SetValue(delaySeconds);
		singleComposer.GetNumberInput("range").SetValue(range);
		singleComposer.GetNumberInput("volume").SetValue(volume);
		singleComposer.GetSwitch("randomizepitch").On = randomizePitch;
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		soundLocation = singleComposer.GetTextInput("soundlocation").GetText();
		delaySeconds = singleComposer.GetNumberInput("delay").GetValue();
		range = singleComposer.GetNumberInput("range").GetValue();
		volume = singleComposer.GetNumberInput("volume").GetValue();
		randomizePitch = singleComposer.GetSwitch("randomizepitch").On;
		return true;
	}

	public override IEntityAction Clone()
	{
		return new PlaySoundAction(vas, delaySeconds, soundLocation, randomizePitch, range, volume);
	}

	public override string ToString()
	{
		return $"Play sound {soundLocation} after {delaySeconds} seconds";
	}
}
