namespace Vintagestory.Client;

public class QueueByte
{
	private static int bufferPortionSize = 5242880;

	private byte[] items;

	public int start;

	public int count;

	public int max;

	public QueueByte()
	{
		max = bufferPortionSize;
		items = new byte[max];
	}

	public int GetCount()
	{
		return count;
	}

	public void Enqueue(byte value)
	{
		if (count + 1 >= max)
		{
			byte[] array = new byte[max + bufferPortionSize];
			int num = GetCount();
			for (int i = 0; i < num; i++)
			{
				array[i] = items[(start + i) % max];
			}
			items = array;
			start = 0;
			count = num;
			max += bufferPortionSize;
		}
		int num2 = start + count;
		num2 %= max;
		count++;
		items[num2] = value;
	}

	public byte Dequeue()
	{
		byte result = items[start];
		start++;
		start %= max;
		count--;
		return result;
	}

	public void DequeueRange(byte[] data, int length)
	{
		for (int i = 0; i < length; i++)
		{
			data[i] = Dequeue();
		}
	}

	internal void PeekRange(byte[] data, int length)
	{
		for (int i = 0; i < length; i++)
		{
			data[i] = items[(start + i) % max];
		}
	}
}
