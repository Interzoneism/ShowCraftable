using System.Collections.Generic;

namespace Vintagestory.GameContent;

public class EPCounter
{
	private Dictionary<string, int> epcount = new Dictionary<string, int>();

	public Dictionary<string, int> Dict => epcount;

	public int this[string key]
	{
		get
		{
			if (epcount.TryGetValue(key, out var value))
			{
				return value;
			}
			return 0;
		}
	}

	public void Inc(string key)
	{
		if (epcount.TryGetValue(key, out var value))
		{
			epcount[key] = value + 1;
		}
		else
		{
			epcount[key] = 1;
		}
	}

	public void Dec(string key)
	{
		if (epcount.TryGetValue(key, out var value))
		{
			epcount[key] = value - 1;
		}
		else
		{
			epcount[key] = -1;
		}
	}

	public void Clear()
	{
		epcount.Clear();
	}
}
