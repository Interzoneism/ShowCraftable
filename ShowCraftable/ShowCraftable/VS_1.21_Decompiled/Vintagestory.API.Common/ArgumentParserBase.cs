using System;
using System.Collections.Generic;
using System.Text;

namespace Vintagestory.API.Common;

public abstract class ArgumentParserBase : ICommandArgumentParser
{
	protected string lastErrorMessage;

	protected bool isMandatoryArg;

	protected int argCount = 1;

	protected string argName;

	public string LastErrorMessage => lastErrorMessage;

	public string ArgumentName => argName;

	public bool IsMandatoryArg => isMandatoryArg;

	public bool IsMissing { get; set; }

	public int ArgCount => argCount;

	protected ArgumentParserBase(string argName, bool isMandatoryArg)
	{
		this.argName = argName;
		this.isMandatoryArg = isMandatoryArg;
	}

	public virtual string[] GetValidRange(CmdArgs args)
	{
		return null;
	}

	public abstract object GetValue();

	public abstract void SetValue(object data);

	public virtual string GetSyntax()
	{
		if (!isMandatoryArg)
		{
			return "<i>[" + argName + "]</i>";
		}
		return "<i>&lt;" + argName + "&gt;</i>";
	}

	public virtual string GetSyntaxUnformatted()
	{
		if (!isMandatoryArg)
		{
			return "[" + argName + "]";
		}
		return "&lt;" + argName + "&gt;";
	}

	public virtual string GetSyntaxExplanation(string indent)
	{
		return null;
	}

	public virtual string GetLastError()
	{
		return lastErrorMessage;
	}

	public abstract EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null);

	protected Dictionary<string, string> parseSubArgs(string strargs)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (strargs.Length == 0)
		{
			return dictionary;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (char c in strargs)
		{
			switch (c)
			{
			case '[':
				flag = true;
				continue;
			default:
				if (flag)
				{
					stringBuilder.Append(c);
				}
				continue;
			case ']':
				break;
			}
			break;
		}
		string[] array = stringBuilder.ToString().Split(',');
		foreach (string text in array)
		{
			if (text.Length != 0)
			{
				string[] array2 = text.Split('=');
				if (array2.Length >= 2)
				{
					dictionary[array2[0].ToLowerInvariant().Trim()] = array2[1].Trim();
				}
			}
		}
		return dictionary;
	}

	public virtual void PreProcess(TextCommandCallingArgs args)
	{
		IsMissing = args.RawArgs.Length == 0;
	}
}
