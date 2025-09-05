public class Packet_Tags
{
	public string[] EntityTags;

	public int EntityTagsCount;

	public int EntityTagsLength;

	public string[] BlockTags;

	public int BlockTagsCount;

	public int BlockTagsLength;

	public string[] ItemTags;

	public int ItemTagsCount;

	public int ItemTagsLength;

	public const int EntityTagsFieldID = 1;

	public const int BlockTagsFieldID = 2;

	public const int ItemTagsFieldID = 3;

	public int size;

	public string[] GetEntityTags()
	{
		return EntityTags;
	}

	public void SetEntityTags(string[] value, int count, int length)
	{
		EntityTags = value;
		EntityTagsCount = count;
		EntityTagsLength = length;
	}

	public void SetEntityTags(string[] value)
	{
		EntityTags = value;
		EntityTagsCount = value.Length;
		EntityTagsLength = value.Length;
	}

	public int GetEntityTagsCount()
	{
		return EntityTagsCount;
	}

	public void EntityTagsAdd(string value)
	{
		if (EntityTagsCount >= EntityTagsLength)
		{
			if ((EntityTagsLength *= 2) == 0)
			{
				EntityTagsLength = 1;
			}
			string[] array = new string[EntityTagsLength];
			for (int i = 0; i < EntityTagsCount; i++)
			{
				array[i] = EntityTags[i];
			}
			EntityTags = array;
		}
		EntityTags[EntityTagsCount++] = value;
	}

	public string[] GetBlockTags()
	{
		return BlockTags;
	}

	public void SetBlockTags(string[] value, int count, int length)
	{
		BlockTags = value;
		BlockTagsCount = count;
		BlockTagsLength = length;
	}

	public void SetBlockTags(string[] value)
	{
		BlockTags = value;
		BlockTagsCount = value.Length;
		BlockTagsLength = value.Length;
	}

	public int GetBlockTagsCount()
	{
		return BlockTagsCount;
	}

	public void BlockTagsAdd(string value)
	{
		if (BlockTagsCount >= BlockTagsLength)
		{
			if ((BlockTagsLength *= 2) == 0)
			{
				BlockTagsLength = 1;
			}
			string[] array = new string[BlockTagsLength];
			for (int i = 0; i < BlockTagsCount; i++)
			{
				array[i] = BlockTags[i];
			}
			BlockTags = array;
		}
		BlockTags[BlockTagsCount++] = value;
	}

	public string[] GetItemTags()
	{
		return ItemTags;
	}

	public void SetItemTags(string[] value, int count, int length)
	{
		ItemTags = value;
		ItemTagsCount = count;
		ItemTagsLength = length;
	}

	public void SetItemTags(string[] value)
	{
		ItemTags = value;
		ItemTagsCount = value.Length;
		ItemTagsLength = value.Length;
	}

	public int GetItemTagsCount()
	{
		return ItemTagsCount;
	}

	public void ItemTagsAdd(string value)
	{
		if (ItemTagsCount >= ItemTagsLength)
		{
			if ((ItemTagsLength *= 2) == 0)
			{
				ItemTagsLength = 1;
			}
			string[] array = new string[ItemTagsLength];
			for (int i = 0; i < ItemTagsCount; i++)
			{
				array[i] = ItemTags[i];
			}
			ItemTags = array;
		}
		ItemTags[ItemTagsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
