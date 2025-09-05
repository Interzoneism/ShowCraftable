using System;
using System.Security.Cryptography;

namespace Vintagestory.API.MathTools;

public class Crc32Algorithm : HashAlgorithm
{
	private uint _currentCrc;

	private static readonly SafeProxy _proxy = new SafeProxy();

	public Crc32Algorithm()
	{
		HashSizeValue = 32;
	}

	public static uint Append(uint initial, byte[] input, int offset, int length)
	{
		if (input == null)
		{
			throw new ArgumentNullException();
		}
		if (offset < 0 || length < 0 || offset + length > input.Length)
		{
			throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
		}
		return AppendInternal(initial, input, offset, length);
	}

	public static uint Append(uint initial, byte[] input)
	{
		if (input == null)
		{
			throw new ArgumentNullException();
		}
		return AppendInternal(initial, input, 0, input.Length);
	}

	public static uint Compute(byte[] input, int offset, int length)
	{
		return Append(0u, input, offset, length);
	}

	public static uint Compute(byte[] input)
	{
		return Append(0u, input);
	}

	public override void Initialize()
	{
		_currentCrc = 0u;
	}

	protected override void HashCore(byte[] input, int offset, int length)
	{
		_currentCrc = AppendInternal(_currentCrc, input, offset, length);
	}

	protected override byte[] HashFinal()
	{
		return new byte[4]
		{
			(byte)(_currentCrc >> 24),
			(byte)(_currentCrc >> 16),
			(byte)(_currentCrc >> 8),
			(byte)_currentCrc
		};
	}

	private static uint AppendInternal(uint initial, byte[] input, int offset, int length)
	{
		if (length > 0)
		{
			return _proxy.Append(initial, input, offset, length);
		}
		return initial;
	}
}
