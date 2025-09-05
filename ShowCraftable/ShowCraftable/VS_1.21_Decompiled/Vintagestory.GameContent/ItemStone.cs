using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemStone : Item
{
	private float damage;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		damage = Attributes["damage"].AsFloat(1f);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if (slot.Itemstack?.Collectible == this)
		{
			return "knap";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		Block block = ((blockSel == null) ? null : byEntity.World.BlockAccessor.GetBlock(blockSel.Position));
		if (block is BlockDisplayCase || block is BlockSign)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			handling = EnumHandHandling.NotHandled;
			return;
		}
		EnumHandHandling handHandling = EnumHandHandling.NotHandled;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling2 = EnumHandling.PassThrough;
			obj.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling2);
			if (handling2 == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handHandling != EnumHandHandling.NotHandled)
		{
			handling = handHandling;
			return;
		}
		bool flag = itemslot.Itemstack.Collectible.Attributes != null && itemslot.Itemstack.Collectible.Attributes["knappable"].AsBool();
		bool flag2 = false;
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (byEntity.Controls.ShiftKey && blockSel != null)
		{
			Block block2 = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			flag2 = block2.Code.PathStartsWith("loosestones") && block2.FirstCodePart(1).Equals(itemslot.Itemstack.Collectible.FirstCodePart(1));
		}
		if (flag2)
		{
			if (!flag)
			{
				if (byEntity.World.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "toosoft", Lang.Get("This type of stone is too soft to be used for knapping."));
				}
				return;
			}
			if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				itemslot.MarkDirty();
				return;
			}
			IWorldAccessor world = byEntity.World;
			Block block3 = world.GetBlock(new AssetLocation("knappingsurface"));
			if (block3 == null)
			{
				return;
			}
			string failureCode = "";
			BlockPos position = blockSel.Position;
			block3.CanPlaceBlock(world, player, blockSel, ref failureCode);
			if (failureCode == "entityintersecting")
			{
				bool selfBlocked = false;
				string text = ((world.GetIntersectingEntities(position, block3.GetCollisionBoxes(world.BlockAccessor, position), delegate(Entity e)
				{
					selfBlocked = e == byEntity;
					return !(e is EntityItem);
				}).Length == 0) ? Lang.Get("Cannot place a knapping surface here") : (selfBlocked ? Lang.Get("Cannot place a knapping surface here, too close to you") : Lang.Get("Cannot place a knapping surface here, to close to another player or creature.")));
				(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", text);
				return;
			}
			world.BlockAccessor.SetBlock(block3.BlockId, position);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			(api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			if (block3.Sounds != null)
			{
				world.PlaySoundAt(block3.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			}
			if (world.BlockAccessor.GetBlockEntity(position) is BlockEntityKnappingSurface blockEntityKnappingSurface)
			{
				blockEntityKnappingSurface.BaseMaterial = itemslot.Itemstack.Clone();
				blockEntityKnappingSurface.BaseMaterial.StackSize = 1;
				if (byEntity.World is IClientWorldAccessor)
				{
					blockEntityKnappingSurface.OpenDialog(world as IClientWorldAccessor, position, itemslot.Itemstack);
				}
			}
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		else if (blockSel != null && byEntity?.World != null && byEntity.Controls.ShiftKey)
		{
			IWorldAccessor world2 = byEntity.World;
			Block block4 = world2.GetBlock(CodeWithPath("loosestones-" + LastCodePart() + "-free"));
			if (block4 == null)
			{
				block4 = world2.GetBlock(CodeWithPath("loosestones-" + LastCodePart(1) + "-" + LastCodePart() + "-free"));
			}
			if (block4 == null)
			{
				return;
			}
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			blockPos.Y--;
			if (!world2.BlockAccessor.GetMostSolidBlock(blockPos).CanAttachBlockAt(world2.BlockAccessor, block4, blockPos, BlockFacing.UP))
			{
				return;
			}
			blockPos.Y++;
			BlockSelection blockSelection = blockSel.Clone();
			blockSelection.Position = blockPos;
			blockSelection.DidOffset = true;
			string failureCode2 = "";
			if (!block4.TryPlaceBlock(world2, player, itemslot.Itemstack, blockSelection, ref failureCode2))
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + failureCode2));
				}
				return;
			}
			world2.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			if (block4.Sounds != null)
			{
				world2.PlaySoundAt(block4.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			}
			(api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			itemslot.Itemstack.StackSize--;
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		else if (!byEntity.Controls.ShiftKey)
		{
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.StartAnimation("aim");
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool flag = true;
		bool flag2 = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		if (byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = GameMath.Clamp(secondsUsed * 3f, 0f, 1.5f);
			modelTransform.Translation.Set(num / 4f, num / 2f, 0f);
			modelTransform.Rotation.Set(0f, 0f, GameMath.Min(90f, secondsUsed * 360f / 1.5f));
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
		{
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool flag = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (flag || byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return;
		}
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
		if (!(secondsUsed < 0.35f))
		{
			ItemStack projectileStack = slot.TakeOut(1);
			slot.MarkDirty();
			IPlayer dualCallByPlayer = null;
			if (byEntity is EntityPlayer)
			{
				dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
			EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("thrownstone-" + Variant["rock"]));
			Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
			((EntityThrownStone)entity).FiredBy = byEntity;
			((EntityThrownStone)entity).Damage = damage;
			((EntityThrownStone)entity).ProjectileStack = projectileStack;
			EntityProjectile.SpawnThrownEntity(entity, byEntity, 0.75, 0.1, 0.2);
			byEntity.StartAnimation("throw");
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get("{0} blunt damage when thrown", damage));
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface blockEntityKnappingSurface)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null)
			{
				blockEntityKnappingSurface.OnBeginUse(player, blockSel);
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface) || !(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface blockEntityKnappingSurface))
		{
			return;
		}
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player != null)
		{
			GetToolMode(slot, player, blockSel);
			if (byEntity.World is IClientWorldAccessor)
			{
				blockEntityKnappingSurface.OnUseOver(player, blockSel.SelectionBoxIndex, blockSel.Face, mouseMode: true);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[2]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-throw",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
