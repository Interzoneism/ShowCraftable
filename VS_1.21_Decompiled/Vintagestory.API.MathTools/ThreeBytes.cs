namespace Vintagestory.API.MathTools;

public struct ThreeBytes
{
	private int val;

	public byte this[int i]
	{
		get
		{
			return i switch
			{
				1 => (byte)(val >> 8), 
				0 => (byte)val, 
				_ => (byte)(val >> 16), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				val &= 16776960 + value;
				break;
			case 1:
				val &= 16711935 + (value << 8);
				break;
			default:
				val &= 65535 + (value << 16);
				break;
			}
		}
	}

	public ThreeBytes(int a)
	{
		val = a;
	}

	public ThreeBytes(byte[] a)
	{
		val = a[0] + (a[1] << 8) + (a[2] << 16);
	}

	public static implicit operator byte[](ThreeBytes a)
	{
		return new byte[3]
		{
			(byte)a.val,
			(byte)(a.val >> 8),
			(byte)(a.val >> 16)
		};
	}

	public static implicit operator ThreeBytes(byte[] a)
	{
		return new ThreeBytes(a);
	}

	public ThreeBytes Clone()
	{
		return new ThreeBytes(this);
	}
}
