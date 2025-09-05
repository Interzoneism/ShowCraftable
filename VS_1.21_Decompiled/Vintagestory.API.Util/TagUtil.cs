using System.Collections.Generic;

namespace Vintagestory.API.Util;

public static class TagUtil
{
	public static bool Intersects(ushort[] first, ushort[] second)
	{
		int num = 0;
		int num2 = 0;
		while (num < first.Length && num2 < second.Length)
		{
			if (first[num] == second[num2])
			{
				return true;
			}
			if (first[num] < second[num2])
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		return false;
	}

	public static bool ContainsAll(ushort[] requirement, ushort[] sample)
	{
		int num = 0;
		int num2 = 0;
		while (num < requirement.Length && num2 < sample.Length)
		{
			if (requirement[num] == sample[num2])
			{
				num++;
			}
			else if (requirement[num] < sample[num2])
			{
				return false;
			}
			num2++;
		}
		return num == requirement.Length;
	}

	public static bool IntersectsAll(IEnumerable<ushort[]> requirementGroups, ushort[] sample)
	{
		foreach (ushort[] requirementGroup in requirementGroups)
		{
			if (!Intersects(requirementGroup, sample))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ContainsAllFromAtLeastOne(IEnumerable<ushort[]> requirementGroups, ushort[] sample)
	{
		foreach (ushort[] requirementGroup in requirementGroups)
		{
			if (ContainsAll(requirementGroup, sample))
			{
				return true;
			}
		}
		return false;
	}
}
