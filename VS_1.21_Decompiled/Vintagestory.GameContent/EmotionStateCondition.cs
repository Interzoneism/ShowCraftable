using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class EmotionStateCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string emotionState;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "emotionstate";

	public EmotionStateCondition()
	{
	}

	public EmotionStateCondition(EntityActivitySystem vas, string emotionState, bool invert = false)
	{
		this.emotionState = emotionState;
		this.vas = vas;
		Invert = invert;
	}

	public IActionCondition Clone()
	{
		return new EmotionStateCondition(vas, emotionState, Invert);
	}

	public bool ConditionSatisfied(Entity e)
	{
		if (emotionState.Contains(","))
		{
			string[] array = emotionState.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
				if (e.GetBehavior<EntityBehaviorEmotionStates>().IsInEmotionState(emotionState))
				{
					return true;
				}
			}
			return false;
		}
		return e.GetBehavior<EntityBehaviorEmotionStates>().IsInEmotionState(emotionState);
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Emotion State", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "emotionState");
		singleComposer.GetTextInput("emotionState").SetValue(emotionState);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		emotionState = singleComposer.GetTextInput("emotionState").GetText();
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public override string ToString()
	{
		return (Invert ? "When NOT in emotion state" : "When in emotion state ") + emotionState;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
