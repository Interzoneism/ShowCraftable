using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AnimationKeyFrame
{
	[JsonProperty]
	public int Frame;

	[JsonProperty]
	public Dictionary<string, AnimationKeyFrameElement> Elements;

	private IDictionary<ShapeElement, AnimationKeyFrameElement> ElementsByShapeElement;

	[Obsolete("Use the overload taking a Dictionary argument instead for higher performance on large sets")]
	public void Resolve(ShapeElement[] allElements)
	{
		if (Elements == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AnimationKeyFrameElement> element in Elements)
		{
			element.Value.Frame = Frame;
		}
		foreach (ShapeElement shapeElement in allElements)
		{
			if (shapeElement != null && Elements.TryGetValue(shapeElement.Name, out var value))
			{
				ElementsByShapeElement[shapeElement] = value;
			}
		}
	}

	public void Resolve(Dictionary<string, ShapeElement> allElements)
	{
		if (Elements == null)
		{
			return;
		}
		ElementsByShapeElement = new FastSmallDictionary<ShapeElement, AnimationKeyFrameElement>(Elements.Count);
		foreach (KeyValuePair<string, AnimationKeyFrameElement> element in Elements)
		{
			AnimationKeyFrameElement value = element.Value;
			value.Frame = Frame;
			allElements.TryGetValue(element.Key, out var value2);
			if (value2 != null)
			{
				ElementsByShapeElement[value2] = value;
			}
		}
	}

	internal AnimationKeyFrameElement GetKeyFrameElement(ShapeElement forElem)
	{
		if (forElem == null)
		{
			return null;
		}
		ElementsByShapeElement.TryGetValue(forElem, out var value);
		return value;
	}

	public AnimationKeyFrame Clone()
	{
		return new AnimationKeyFrame
		{
			Elements = ((Elements == null) ? null : new Dictionary<string, AnimationKeyFrameElement>(Elements)),
			Frame = Frame
		};
	}
}
