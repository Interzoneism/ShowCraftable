using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemChiselCommands : ModSystem
{
	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		this.api = api;
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("chisel").WithDescription("chisel")
			.BeginSubCommand("genshape")
			.WithDescription("Export a json model file from a chiseled block")
			.WithArgs(api.ChatCommands.Parsers.WorldPosition("pos"))
			.HandleWith(onGenShape)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult onGenShape(TextCommandCallingArgs args)
	{
		Vec3d vec3d = args[0] as Vec3d;
		BlockEntityMicroBlock blockEntity = api.World.BlockAccessor.GetBlockEntity<BlockEntityMicroBlock>(vec3d.AsBlockPos);
		if (blockEntity == null)
		{
			return TextCommandResult.Error("This block is not a microblock");
		}
		string text = JsonUtil.ToPrettyString(blockEntity.GenShape());
		text = text.Replace("Textures", "textures").Replace("Elements", "elements").Replace("Name", "name")
			.Replace("From", "from")
			.Replace("To", "to")
			.Replace("Texture", "texture")
			.Replace("Faces", "faces")
			.Replace("Uv", "uv")
			.Replace("Enabled", "enabled")
			.Replace("game:", "");
		File.WriteAllText("microblockshapefile.json", text);
		return TextCommandResult.Success("shape file microblockshapefile.json generated");
	}
}
