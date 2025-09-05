using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class HumanoidOutfits : ModSystem
{
	private Dictionary<string, HumanoidWearableProperties> propsByConfigFilename = new Dictionary<string, HumanoidWearableProperties>();

	private ICoreAPI api;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public HumanoidWearableProperties loadProps(string configFilename)
	{
		HumanoidWearableProperties humanoidWearableProperties = api.Assets.TryGet(new AssetLocation("config/" + configFilename + ".json"))?.ToObject<HumanoidWearableProperties>();
		if (humanoidWearableProperties == null)
		{
			throw new FileNotFoundException("config/" + configFilename + ".json is missing.");
		}
		for (int i = 0; i < humanoidWearableProperties.BySlot.Length; i++)
		{
			string[] array = humanoidWearableProperties.BySlot[i].Variants;
			for (int j = 0; j < array.Length; j++)
			{
				if (!humanoidWearableProperties.Variants.TryGetValue(array[j], out var value))
				{
					api.World.Logger.Error("Typo in " + configFilename + ".json Shape reference {0} defined for slot {1}, but not in list of shapes. Will remove.", array[j], humanoidWearableProperties.BySlot[i].Code);
					array = array.Remove(array[j]);
					j--;
				}
				else
				{
					humanoidWearableProperties.BySlot[i].WeightSum += value.Weight;
				}
			}
		}
		return propsByConfigFilename[configFilename] = humanoidWearableProperties;
	}

	public Dictionary<string, string> GetRandomOutfit(string configFilename, Dictionary<string, WeightedCode[]> partialRandomOutfits = null)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var value))
		{
			value = loadProps(configFilename);
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		for (int i = 0; i < value.BySlot.Length; i++)
		{
			SlotAlloc slotAlloc = value.BySlot[i];
			if (partialRandomOutfits != null && partialRandomOutfits.TryGetValue(slotAlloc.Code, out var value2))
			{
				float num = 0f;
				for (int j = 0; j < value2.Length; j++)
				{
					num += value2[j].Weight;
				}
				float num2 = (float)api.World.Rand.NextDouble() * num;
				foreach (WeightedCode weightedCode in value2)
				{
					num2 -= weightedCode.Weight;
					if (num2 <= 0f)
					{
						dictionary[slotAlloc.Code] = weightedCode.Code;
						break;
					}
				}
				continue;
			}
			float num3 = (float)api.World.Rand.NextDouble() * slotAlloc.WeightSum;
			for (int l = 0; l < slotAlloc.Variants.Length; l++)
			{
				TexturedWeightedCompositeShape texturedWeightedCompositeShape = value.Variants[slotAlloc.Variants[l]];
				num3 -= texturedWeightedCompositeShape.Weight;
				if (num3 <= 0f)
				{
					dictionary[slotAlloc.Code] = slotAlloc.Variants[l];
					break;
				}
			}
		}
		return dictionary;
	}

	public HumanoidWearableProperties GetConfig(string configFilename)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var value))
		{
			return loadProps(configFilename);
		}
		return value;
	}

	public TexturedWeightedCompositeShape[] Outfit2Shapes(string configFilename, string[] outfit)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var value))
		{
			value = loadProps(configFilename);
		}
		TexturedWeightedCompositeShape[] array = new TexturedWeightedCompositeShape[outfit.Length];
		for (int i = 0; i < outfit.Length; i++)
		{
			if (!value.Variants.TryGetValue(outfit[i], out array[i]))
			{
				api.Logger.Warning("Outfit code {1} for config file {0} cannot be resolved into a variant - wrong code or missing entry?", configFilename, outfit[i]);
			}
		}
		return array;
	}

	public void Reload()
	{
		propsByConfigFilename.Clear();
	}
}
