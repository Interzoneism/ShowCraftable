using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class TextCommandCallingArgs
{
	public string LanguageCode;

	public IChatCommand Command;

	public string SubCmdCode;

	public Caller Caller;

	public CmdArgs RawArgs;

	public List<ICommandArgumentParser> Parsers = new List<ICommandArgumentParser>();

	public int ArgCount
	{
		get
		{
			int num = 0;
			foreach (ICommandArgumentParser parser in Parsers)
			{
				int argCount = parser.ArgCount;
				if (argCount < 0)
				{
					return -1;
				}
				num += argCount;
			}
			return num;
		}
	}

	public object this[int index] => Parsers[index].GetValue();

	public object LastArg => Parsers[Parsers.Count - 1].GetValue();
}
