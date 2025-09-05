using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class CmdArgs
{
	private List<string> args = new List<string>();

	public string this[int index]
	{
		get
		{
			return args[index];
		}
		set
		{
			args[index] = value;
		}
	}

	public int Length => args.Count;

	public CmdArgs()
	{
	}

	public CmdArgs(string joinedargs)
	{
		string joinedargs2 = Regex.Replace(joinedargs.Trim(), "\\s+", " ");
		Push(joinedargs2);
	}

	public void Push(string joinedargs)
	{
		string[] collection = Array.Empty<string>();
		if (joinedargs.Length > 0)
		{
			collection = joinedargs.Split(' ');
		}
		args.AddRange(collection);
	}

	public CmdArgs(string[] args)
	{
		this.args = new List<string>(args);
	}

	public string PopAll()
	{
		string result = string.Join(" ", args.ToArray(), 0, args.Count);
		args.Clear();
		return result;
	}

	public char? PeekChar(char? defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		if (args[0].Length == 0)
		{
			return '\0';
		}
		return args[0][0];
	}

	public char? PopChar(char? defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		string text = args[0];
		args[0] = args[0].Substring(1);
		return text[0];
	}

	public string PopWord(string defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		string result = args[0];
		args.RemoveAt(0);
		return result;
	}

	public string PeekWord(string defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		return args[0];
	}

	public string PopUntil(char endChar)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = PopAll();
		for (int i = 0; i < text.Length && text[i] != endChar; i++)
		{
			stringBuilder.Append(text[i]);
		}
		Push(text.Substring(stringBuilder.Length).TrimStart());
		return stringBuilder.ToString();
	}

	public string PopCodeBlock(char blockOpenChar, char blockCloseChar, out string parseErrorMsg)
	{
		parseErrorMsg = null;
		string text = PopAll();
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == blockOpenChar)
			{
				num++;
			}
			if (num == 0)
			{
				parseErrorMsg = Lang.Get("First character is not " + blockOpenChar + ". Please consume all input until the block open char");
				return null;
			}
			if (num > 0)
			{
				stringBuilder.Append(text[i]);
			}
			if (text[i] == blockCloseChar)
			{
				num--;
				if (num <= 0)
				{
					break;
				}
			}
		}
		if (num > 0)
		{
			parseErrorMsg = Lang.Get("Incomplete block. At least one " + blockCloseChar + " is missing");
			return null;
		}
		Push(text.Substring(stringBuilder.Length).TrimStart());
		return stringBuilder.ToString();
	}

	public void PushSingle(string arg)
	{
		args.Insert(0, arg);
	}

	public void AppendSingle(string arg)
	{
		args.Add(arg);
	}

	public T PopEnum<T>(T defaultValue = default(T))
	{
		string text = PopWord();
		if (text == null)
		{
			return defaultValue;
		}
		if (int.TryParse(text, out var result) && Enum.IsDefined(typeof(T), result))
		{
			return (T)Enum.ToObject(typeof(T), result);
		}
		return default(T);
	}

	public int? PopInt(int? defaultValue = null)
	{
		string text = PopWord();
		if (text == null)
		{
			return defaultValue;
		}
		if (TryParseIntFancy(text, out var val))
		{
			return val;
		}
		return defaultValue;
	}

	private bool TryParseIntFancy(string arg, out int val)
	{
		arg = arg.Replace(",", "");
		if (arg.StartsWith("0x") && int.TryParse(arg.Substring(2), NumberStyles.HexNumber, GlobalConstants.DefaultCultureInfo, out val))
		{
			return true;
		}
		if (long.TryParse(arg, out var result))
		{
			val = (int)result;
			return true;
		}
		val = 0;
		return false;
	}

	public long? PopLong(long? defaultValue = null)
	{
		string text = PopWord();
		if (text == null)
		{
			return defaultValue;
		}
		if (long.TryParse(text, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public bool? PopBool(bool? defaultValue = null, string trueAlias = "on")
	{
		string text = PopWord()?.ToLowerInvariant();
		if (text == null)
		{
			return defaultValue;
		}
		int value;
		switch (text)
		{
		default:
			value = ((text == trueAlias) ? 1 : 0);
			break;
		case "1":
		case "yes":
		case "true":
		case "on":
			value = 1;
			break;
		}
		return (byte)value != 0;
	}

	public double? PopDouble(double? defaultValue = null)
	{
		string text = PopWord();
		if (text == null)
		{
			return defaultValue;
		}
		return text.ToDoubleOrNull(defaultValue);
	}

	public float? PopFloat(float? defaultValue = null)
	{
		string text = PopWord();
		if (text == null)
		{
			return defaultValue;
		}
		return text.ToFloatOrNull(defaultValue);
	}

	public Vec3i PopVec3i(Vec3i defaultValue = null)
	{
		int? num = PopInt(defaultValue?.X);
		int? num2 = PopInt(defaultValue?.Y);
		int? num3 = PopInt(defaultValue?.Z);
		if (!num.HasValue || !num2.HasValue || !num3.HasValue)
		{
			return defaultValue;
		}
		return new Vec3i(num.Value, num2.Value, num3.Value);
	}

	public Vec3d PopVec3d(Vec3d defaultValue = null)
	{
		double? num = PopDouble(defaultValue?.X);
		double? num2 = PopDouble(defaultValue?.Y);
		double? num3 = PopDouble(defaultValue?.Z);
		if (!num.HasValue || !num2.HasValue || !num3.HasValue)
		{
			return defaultValue;
		}
		return new Vec3d(num.Value, num2.Value, num3.Value);
	}

	public Vec3d PopFlexiblePos(Vec3d playerPos, Vec3d mapMiddle)
	{
		if (args.Count < 3)
		{
			return null;
		}
		Vec3d vec3d = new Vec3d();
		for (int i = 0; i < 3; i++)
		{
			switch (PeekChar().Value)
			{
			case '~':
			{
				PopChar();
				double? num;
				if (PeekChar() != '\0')
				{
					num = PopDouble();
					if (!num.HasValue)
					{
						return null;
					}
				}
				else
				{
					num = 0.0;
					PopWord();
				}
				vec3d[i] = num.Value + playerPos[i];
				break;
			}
			case '=':
			{
				PopChar();
				double? num = PopDouble();
				if (!num.HasValue)
				{
					return null;
				}
				vec3d[i] = num.Value;
				break;
			}
			default:
			{
				double? num = PopDouble();
				if (!num.HasValue)
				{
					return null;
				}
				vec3d[i] = num.Value + mapMiddle[i];
				break;
			}
			}
		}
		return vec3d;
	}

	public Vec2i PopFlexiblePos2D(Vec3d playerPos, Vec3d mapMiddle)
	{
		if (args.Count < 2)
		{
			return null;
		}
		Vec2i vec2i = new Vec2i();
		for (int i = 0; i < 2; i++)
		{
			double? num;
			switch (PeekChar().Value)
			{
			case '~':
				PopChar();
				if (PeekChar() != '\0')
				{
					num = PopDouble();
					if (!num.HasValue)
					{
						return null;
					}
				}
				else
				{
					num = 0.0;
					PopWord();
				}
				vec2i[i] = (int)(num.Value + playerPos[i * 2]);
				continue;
			case '=':
				PopChar();
				num = PopDouble();
				if (!num.HasValue)
				{
					return null;
				}
				vec2i[i] = (int)num.Value;
				continue;
			case '+':
				PopChar();
				break;
			}
			num = PopDouble();
			if (!num.HasValue)
			{
				return null;
			}
			vec2i[i] = (int)(num.Value + mapMiddle[i * 2]);
		}
		return vec2i;
	}

	public CmdArgs Clone()
	{
		return new CmdArgs(args.ToArray());
	}
}
