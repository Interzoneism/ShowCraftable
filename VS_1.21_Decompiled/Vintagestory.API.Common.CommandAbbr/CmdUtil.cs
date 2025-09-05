using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common.CommandAbbr;

public static class CmdUtil
{
	public delegate TextCommandResult EntityEachDelegate(Entity entity);

	public static TextCommandResult EntityEach(TextCommandCallingArgs args, EntityEachDelegate onEntity, int index = 0)
	{
		Entity[] array = (Entity[])args.Parsers[index].GetValue();
		int num = 0;
		if (array.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No matching player/entity found"), "nonefound");
		}
		TextCommandResult textCommandResult = null;
		Entity[] array2 = array;
		foreach (Entity entity in array2)
		{
			textCommandResult = onEntity(entity);
			if (textCommandResult.Status == EnumCommandStatus.Success)
			{
				num++;
			}
		}
		if (array.Length == 1)
		{
			return textCommandResult;
		}
		return TextCommandResult.Success(Lang.Get("Executed commands on {0}/{1} entities", num, array.Length));
	}
}
