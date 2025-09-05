using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Systems;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorLockable : BlockBehavior
{
	public BlockBehaviorLockable(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().IsLockedForInteract(blockSel.Position, byPlayer))
		{
			if (world.Side == EnumAppSide.Client)
			{
				(world.Api as ICoreClientAPI).TriggerIngameError(this, "locked", Lang.Get("ingameerror-locked"));
			}
			handling = EnumHandling.PreventSubsequent;
			return false;
		}
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		BEBehaviorDoor bEBehaviorDoor = blockEntity?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor != null && bEBehaviorDoor.StoryLockedCode != null)
		{
			if (!(world.Api is ICoreClientAPI coreClientAPI))
			{
				handling = EnumHandling.PreventSubsequent;
				return false;
			}
			handling = EnumHandling.Handled;
			if (coreClientAPI.ModLoader.GetModSystem<StoryLockableDoor>().StoryLockedLocationCodes.TryGetValue(bEBehaviorDoor.StoryLockedCode, out HashSet<string> value) && value.Contains(byPlayer.PlayerUID))
			{
				return true;
			}
			coreClientAPI.TriggerIngameError(this, "locked", Lang.Get("ingameerror-locked"));
			handling = EnumHandling.PreventSubsequent;
			return false;
		}
		return (blockEntity?.GetBehavior<BEBehaviorDoorBarLock>())?.OnBlockInteractStart(world, byPlayer, blockSel, ref handling) ?? base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
	}
}
