using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class JsonExport : ModSystem
{
	private ICoreServerAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.ChatCommands.GetOrCreate("dev").BeginSubCommand("jsonexport").WithDescription("Export items and blocks as json files")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(CmdExport)
			.EndSubCommand();
		this.api = api;
	}

	private TextCommandResult CmdExport(TextCommandCallingArgs textCommandCallingArgs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		int num = 0;
		for (int i = 0; i < api.World.Blocks.Count; i++)
		{
			Block block = api.World.Blocks[i];
			if (block != null && !(block.Code == null))
			{
				if (num > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append("{");
				stringBuilder.Append($"\"name\": \"{new ItemStack(block).GetName()}\", ");
				stringBuilder.Append($"\"code\": \"{block.Code}\", ");
				stringBuilder.Append($"\"material\": \"{block.BlockMaterial}\", ");
				stringBuilder.Append($"\"shape\": \"{block.Shape.Base.Path}\", ");
				stringBuilder.Append($"\"tool\": \"{block.Tool}\"");
				stringBuilder.Append("}");
				num++;
			}
		}
		stringBuilder.Append("]");
		File.WriteAllText("blocks.json", stringBuilder.ToString());
		stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		num = 0;
		for (int j = 0; j < api.World.Items.Count; j++)
		{
			Item item = api.World.Items[j];
			if (item != null && !(item.Code == null))
			{
				if (num > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append("{");
				stringBuilder.Append($"\"name\": \"{new ItemStack(item).GetName()}\", ");
				stringBuilder.Append($"\"code\": \"{item.Code}\", ");
				stringBuilder.Append($"\"shape\": \"{item.Shape?.Base?.Path}\", ");
				stringBuilder.Append($"\"tool\": \"{item.Tool}\"");
				stringBuilder.Append("}");
				num++;
			}
		}
		stringBuilder.Append("]");
		File.WriteAllText("items.json", stringBuilder.ToString());
		return TextCommandResult.Success("All Blocks and Items written to block.json and item.json in " + AppDomain.CurrentDomain.BaseDirectory);
	}
}
