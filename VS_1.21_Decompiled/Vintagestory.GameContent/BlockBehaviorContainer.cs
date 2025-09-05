using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorContainer : BlockBehavior
{
	public BlockBehaviorContainer(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventSubsequent;
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (blockEntity is BlockEntityOpenableContainer)
		{
			return ((BlockEntityOpenableContainer)blockEntity).OnPlayerRightClick(byPlayer, blockSel);
		}
		return false;
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (!(blockEntity is BlockEntityOpenableContainer))
		{
			return;
		}
		BlockEntityOpenableContainer blockEntityOpenableContainer = (BlockEntityOpenableContainer)blockEntity;
		IPlayer[] allOnlinePlayers = world.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			if (allOnlinePlayers[i].InventoryManager.HasInventory(blockEntityOpenableContainer.Inventory))
			{
				allOnlinePlayers[i].InventoryManager.CloseInventoryAndSync(blockEntityOpenableContainer.Inventory);
			}
		}
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
		base.Activate(world, caller, blockSel, activationArgs, ref handled);
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		int millisecondDelay = (int)(activationArgs.TryGetLong("close") ?? 2000);
		BlockEntityOpenableContainer container = blockEntity as BlockEntityOpenableContainer;
		if (container != null)
		{
			byte[] data = SerializerUtil.Serialize(new OpenContainerLidPacket(caller.Entity.EntityId, opened: true));
			((ICoreServerAPI)world.Api).Network.BroadcastBlockEntityPacket(blockSel.Position, 5001, data);
			(world.Api as ICoreServerAPI).World.PlaySoundAt(container.OpenSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			world.Api.Event.RegisterCallback(delegate
			{
				byte[] data2 = SerializerUtil.Serialize(new OpenContainerLidPacket(caller.Entity.EntityId, opened: false));
				((ICoreServerAPI)world.Api).Network.BroadcastBlockEntityPacket(blockSel.Position, 5001, data2);
				(world.Api as ICoreServerAPI).World.PlaySoundAt(container.CloseSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			}, millisecondDelay);
		}
	}
}
