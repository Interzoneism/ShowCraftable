using System.Text;

namespace Vintagestory.API.Util;

public static class StringBuilderExtensions
{
	public static void AppendLineOnce(this StringBuilder sb)
	{
		if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
		{
			sb.AppendLine();
		}
	}

	public static void AppendHex(this StringBuilder sb, byte b)
	{
		sb.Append((char)(48 + b / 16 + b / 160 * 7));
		b %= 16;
		sb.Append((char)(48 + b + b / 10 * 7));
	}

	public static void AppendHex(this StringBuilder sb, byte[] bb)
	{
		foreach (byte b in bb)
		{
			sb.AppendHex(b);
		}
	}
}
