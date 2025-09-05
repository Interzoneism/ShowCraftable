using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorRightClickPickup : BlockBehavior
{
	[DocumentAsJson("Optional", "False", false)]
	private bool dropsPickupMode;

	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation pickupSound;

	public BlockBehaviorRightClickPickup(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		dropsPickupMode = properties["dropsPickupMode"].AsBool();
		string text = properties["sound"].AsString();
		if (text == null)
		{
			text = block.Attributes?["placeSound"].AsString();
		}
		pickupSound = ((text == null) ? null : AssetLocation.Create(text, block.Code.Domain));
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		ItemStack[] array = new ItemStack[1] { block.OnPickBlock(world, blockSel.Position) };
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool flag = activeHotbarSlot.Empty || (array.Length >= 1 && activeHotbarSlot.Itemstack.Equals(world, array[0], GlobalConstants.IgnoredStackAttributes));
		if (dropsPickupMode)
		{
			float num = 1f;
			JsonObject attributes = block.Attributes;
			if (attributes != null && attributes.IsTrue("forageStatAffected"))
			{
				num *= byPlayer.Entity.Stats.GetBlended("forageDropRate");
			}
			array = block.GetDrops(world, blockSel.Position, byPlayer, num);
			BlockDropItemStack[] dropsForHandbook = block.GetDropsForHandbook(new ItemStack(block), byPlayer);
			if (!flag)
			{
				BlockDropItemStack[] array2 = dropsForHandbook;
				foreach (BlockDropItemStack blockDropItemStack in array2)
				{
					flag |= activeHotbarSlot.Itemstack.Equals(world, blockDropItemStack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes);
				}
			}
		}
		if (!flag || !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (world.Side == EnumAppSide.Server && BlockBehaviorReinforcable.AllowRightClickPickup(world, blockSel.Position, byPlayer))
			{
				bool flag2 = true;
				ItemStack[] array3 = array;
				foreach (ItemStack itemStack in array3)
				{
					ItemStack itemStack2 = itemStack.Clone();
					if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
					{
						world.SpawnItemEntity(itemStack, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
					}
					world.Logger.Audit("{0} Took {1}x{2} from Ground at {3}.", byPlayer.PlayerName, itemStack2.StackSize, itemStack.Collectible.Code, blockSel.Position);
					TreeAttribute treeAttribute = new TreeAttribute();
					treeAttribute["itemstack"] = new ItemstackAttribute(itemStack2.Clone());
					treeAttribute["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
					world.Api.Event.PushEvent("onitemcollected", treeAttribute);
					if (flag2)
					{
						flag2 = false;
						world.BlockAccessor.SetBlock(0, blockSel.Position);
						world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
					}
					world.PlaySoundAt(pickupSound ?? block.GetSounds(world.BlockAccessor, blockSel).Place, byPlayer);
				}
			}
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		return base.OnPickBlock(world, pos, ref handling);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-behavior-rightclickpickup",
				MouseButton = EnumMouseButton.Right,
				RequireFreeHand = true
			}
		};
	}
}
