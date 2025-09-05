using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCommand : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityGuiConfigurableCommands obj = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGuiConfigurableCommands;
		if (obj != null && !obj.OnInteract(new Caller
		{
			Player = byPlayer
		}))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		return true;
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
	{
		(world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGuiConfigurableCommands)?.OnInteract(caller);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		bool flag = Code.PathStartsWith("conditionalblock");
		WorldInteraction[] array = new WorldInteraction[flag ? 3 : 2];
		array[0] = new WorldInteraction
		{
			ActionLangCode = (flag ? "Execute condition" : "Run commands"),
			MouseButton = EnumMouseButton.Right
		};
		array[1] = new WorldInteraction
		{
			ActionLangCode = "Edit (requires Creative mode)",
			HotKeyCode = "shift",
			MouseButton = EnumMouseButton.Right
		};
		if (flag)
		{
			array[2] = new WorldInteraction
			{
				ActionLangCode = "Rotate (requires Creative mode)",
				Itemstacks = (from item in world.SearchItems(new AssetLocation("wrench-*"))
					select new ItemStack(item)).ToArray(),
				MouseButton = EnumMouseButton.Right
			};
		}
		return array;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGuiConfigurableCommands blockEntityGuiConfigurableCommands)
		{
			itemStack.Attributes.SetString("commands", blockEntityGuiConfigurableCommands.Commands);
			if (blockEntityGuiConfigurableCommands.CallingPrivileges != null)
			{
				itemStack.Attributes["callingPrivileges"] = new StringArrayAttribute(blockEntityGuiConfigurableCommands.CallingPrivileges);
			}
		}
		return itemStack;
	}
}
