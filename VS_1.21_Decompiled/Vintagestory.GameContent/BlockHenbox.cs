using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockHenbox : Block
{
	public string NestType;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		NestType = Attributes?["nestType"]?.AsString();
		if (NestType == null)
		{
			api.Logger.Warning(string.Concat("BlockHenbox ", Code, " nestType attribute not set, defaulting to \"ground\""));
		}
		if (NestType == null)
		{
			NestType = "ground";
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHenBox blockEntityHenBox)
		{
			return blockEntityHenBox.OnInteract(world, byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (!(world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityHenBox blockEntityHenBox) || blockEntityHenBox.CountEggs() == 0)
		{
			return Array.Empty<WorldInteraction>();
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-collect-eggs",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
