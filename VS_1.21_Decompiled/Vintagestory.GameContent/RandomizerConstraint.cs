using System;
using System.Linq;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class RandomizerConstraint
{
	public string[] Allow;

	public string[] Disallow;

	public string SelectRandom(Random rand, SkinnablePartVariant[] variants)
	{
		if (Allow != null)
		{
			return Allow[rand.Next(Allow.Length)];
		}
		if (Disallow != null)
		{
			SkinnablePartVariant[] array = variants.Where((SkinnablePartVariant ele) => !Disallow.Contains(ele.Code)).ToArray();
			return array[rand.Next(array.Length)].Code;
		}
		return variants[rand.Next(variants.Length)].Code;
	}
}
