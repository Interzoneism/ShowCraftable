using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorGroundStorable : CollectibleBehavior
{
	public GroundStorageProperties StorageProps { get; protected set; }

	public CollectibleBehaviorGroundStorable(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		StorageProps = properties.AsObject<GroundStorageProperties>(null, collObj.Code.Domain);
		if (StorageProps.SprintKey)
		{
			StorageProps.CtrlKey = true;
		}
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		Interact(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCodes = ((!StorageProps.CtrlKey) ? new string[1] { "shift" } : new string[2] { "ctrl", "shift" }),
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		};
	}

	public static void Interact(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		IWorldAccessor worldAccessor = byEntity?.World;
		if (blockSel == null || worldAccessor == null || !byEntity.Controls.ShiftKey)
		{
			return;
		}
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = worldAccessor.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player == null)
		{
			return;
		}
		if (!worldAccessor.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			itemslot.MarkDirty();
			worldAccessor.BlockAccessor.MarkBlockDirty(blockSel.Position.UpCopy());
		}
		else
		{
			if (!(worldAccessor.GetBlock(new AssetLocation("groundstorage")) is BlockGroundStorage blockGroundStorage))
			{
				return;
			}
			BlockEntity blockEntity = worldAccessor.BlockAccessor.GetBlockEntity(blockSel.Position);
			BlockEntity blockEntity2 = worldAccessor.BlockAccessor.GetBlockEntity(blockSel.Position.UpCopy());
			if (blockEntity is BlockEntityGroundStorage || blockEntity2 is BlockEntityGroundStorage)
			{
				if (((blockEntity as BlockEntityGroundStorage) ?? (blockEntity2 as BlockEntityGroundStorage)).OnPlayerInteractStart(player, blockSel))
				{
					handHandling = EnumHandHandling.PreventDefault;
				}
			}
			else if (blockSel.Face == BlockFacing.UP && worldAccessor.BlockAccessor.GetBlock(blockSel.Position).CanAttachBlockAt(worldAccessor.BlockAccessor, blockGroundStorage, blockSel.Position, BlockFacing.UP))
			{
				BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);
				if (worldAccessor.BlockAccessor.GetBlock(pos).Replaceable >= 6000 && blockGroundStorage.CreateStorage(byEntity.World, blockSel, player))
				{
					handHandling = EnumHandHandling.PreventDefault;
					handling = EnumHandling.PreventSubsequent;
				}
			}
		}
	}
}
