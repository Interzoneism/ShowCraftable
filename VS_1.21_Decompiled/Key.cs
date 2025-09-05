public struct Key
{
	public const int Size1 = 1;

	public const int Size2 = 2;

	public int key;

	public readonly int Field => key >> 3;

	public readonly int WireType => key % 8;

	public Key(byte field, byte wiretype)
	{
		key = (field << 3) + wiretype;
	}

	public static implicit operator int(Key a)
	{
		return a.key;
	}

	public static Key Create(int firstByte, int secondByte)
	{
		return new Key
		{
			key = ((secondByte << 7) | (firstByte & 0x7F))
		};
	}

	public static Key Create(int n)
	{
		return new Key
		{
			key = n
		};
	}
}
