using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockBaseDoor : Block, IClaimTraverseable
{
	protected string type;

	protected bool open;

	public abstract string GetKnobOrientation();

	public abstract BlockFacing GetDirection();

	protected abstract BlockPos TryGetConnectedDoorPos(BlockPos pos);

	protected abstract void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position);

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		type = string.Intern(Code.Path.Substring(0, Code.Path.IndexOf('-')));
		open = Variant["state"] == "opened";
	}

	public bool IsSameDoor(Block block)
	{
		if (block is BlockBaseDoor blockBaseDoor)
		{
			return blockBaseDoor.type == type;
		}
		return false;
	}

	public virtual bool IsOpened()
	{
		return open;
	}

	public bool DoesBehaviorAllow(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return false;
			}
		}
		if (flag)
		{
			return false;
		}
		if (this is BlockDoor)
		{
			blockSel = blockSel.Clone();
			blockSel.Position = ((this as BlockDoor).IsUpperHalf() ? blockSel.Position.DownCopy() : blockSel.Position.UpCopy());
			blockBehaviors = BlockBehaviors;
			foreach (BlockBehavior obj2 in blockBehaviors)
			{
				EnumHandling handling2 = EnumHandling.PassThrough;
				obj2.OnBlockInteractStart(world, byPlayer, blockSel, ref handling2);
				if (handling2 != EnumHandling.PassThrough)
				{
					flag = true;
				}
				if (handling2 == EnumHandling.PreventSubsequent)
				{
					return false;
				}
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!DoesBehaviorAllow(world, byPlayer, blockSel))
		{
			return true;
		}
		BlockPos position = blockSel.Position;
		Open(world, byPlayer, position);
		world.PlaySoundAt(AssetLocation.Create(Attributes["triggerSound"].AsString("sounds/block/door"), Code.Domain), position, 0.0, byPlayer);
		if (!(FirstCodePart() == "roughhewnfencegate"))
		{
			TryOpenConnectedDoor(world, byPlayer, position);
		}
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	protected void TryOpenConnectedDoor(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		BlockPos blockPos = TryGetConnectedDoorPos(pos);
		if (blockPos != null && world.BlockAccessor.GetBlock(blockPos) is BlockBaseDoor blockBaseDoor && IsSameDoor(blockBaseDoor) && pos == blockBaseDoor.TryGetConnectedDoorPos(blockPos))
		{
			blockBaseDoor.Open(world, byPlayer, blockPos);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-door-openclose",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
