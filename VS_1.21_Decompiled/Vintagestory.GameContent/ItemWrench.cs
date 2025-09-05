using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWrench : Item
{
	private SkillItem rotateSk;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		rotateSk = new SkillItem
		{
			Code = new AssetLocation("rotate"),
			Name = "Rotate (Default)"
		};
		if (api is ICoreClientAPI coreClientAPI)
		{
			rotateSk.WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/rotate.svg"), 48, 48, 5, -1));
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		rotateSk?.Dispose();
	}

	private SkillItem[] GetExtraWrenchModes(IPlayer byPlayer, BlockSelection blockSelection)
	{
		if (blockSelection != null)
		{
			return api.World.BlockAccessor.GetBlock(blockSelection.Position).GetInterface<IExtraWrenchModes>(api.World, blockSelection.Position)?.GetExtraWrenchModes(byPlayer, blockSelection);
		}
		return null;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		if (GetExtraWrenchModes(byPlayer, blockSelection) != null)
		{
			Block block = api.World.BlockAccessor.GetBlock(blockSelection.Position);
			return slot.Itemstack.Attributes.GetInt("toolMode-" + block.Id);
		}
		return base.GetToolMode(slot, byPlayer, blockSelection);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		SkillItem[] extraWrenchModes = GetExtraWrenchModes(forPlayer, blockSel);
		if (extraWrenchModes != null && extraWrenchModes.Length != 0)
		{
			return new SkillItem[1] { rotateSk }.Append(extraWrenchModes);
		}
		return base.GetToolModes(slot, forPlayer, blockSel);
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		SkillItem[] extraWrenchModes = GetExtraWrenchModes(byPlayer, blockSelection);
		if (extraWrenchModes != null && extraWrenchModes.Length != 0)
		{
			Block block = api.World.BlockAccessor.GetBlock(blockSelection.Position);
			slot.Itemstack.Attributes.SetInt("toolMode-" + block.Id, toolMode);
		}
		else
		{
			base.SetToolMode(slot, byPlayer, blockSelection, toolMode);
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			api.World.BlockAccessor.MarkBlockEntityDirty(blockSel.Position.AddCopy(blockSel.Face));
			api.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
			return;
		}
		if (handleModedInteract(slot, blockSel, player, 1))
		{
			handling = EnumHandHandling.PreventDefault;
			return;
		}
		if (rotate(byEntity, blockSel, 1) && player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			DamageItem(api.World, byEntity, slot);
		}
		handling = EnumHandHandling.PreventDefault;
	}

	private bool handleModedInteract(ItemSlot slot, BlockSelection blockSel, IPlayer player, int interactmode)
	{
		if (GetExtraWrenchModes(player, blockSel) != null)
		{
			int toolMode = GetToolMode(slot, player, blockSel);
			if (toolMode > 0)
			{
				IExtraWrenchModes extraWrenchModes = api.World.BlockAccessor.GetBlock(blockSel.Position).GetInterface<IExtraWrenchModes>(api.World, blockSel.Position);
				if (extraWrenchModes != null)
				{
					extraWrenchModes.OnWrenchInteract(player, blockSel, toolMode - 1, interactmode);
					return true;
				}
			}
		}
		return false;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault || blockSel == null)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			api.World.BlockAccessor.MarkBlockEntityDirty(blockSel.Position.AddCopy(blockSel.Face));
			api.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
			return;
		}
		Dictionary<int, Block> subDecors = api.World.BlockAccessor.GetSubDecors(blockSel.Position);
		if (subDecors != null)
		{
			int num = blockSel.ToDecorIndex() / 6;
			foreach (KeyValuePair<int, Block> item in subDecors)
			{
				DecorBits decorBits = new DecorBits(item.Key);
				if (decorBits.Face == blockSel.Face.Index)
				{
					int subPosition = decorBits.SubPosition;
					if (subPosition == 0 || subPosition == num)
					{
						int rotation = (decorBits.Rotation + 1) % 8;
						api.World.BlockAccessor.SetDecor(api.World.BlockAccessor.GetBlock(0), blockSel.Position, decorBits);
						decorBits.Rotation = rotation;
						api.World.BlockAccessor.SetDecor(item.Value, blockSel.Position, decorBits);
						handling = EnumHandHandling.PreventDefault;
						return;
					}
				}
			}
		}
		if (handleModedInteract(slot, blockSel, player, 0))
		{
			handling = EnumHandHandling.PreventDefault;
			return;
		}
		if (rotate(byEntity, blockSel, -1) && player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			DamageItem(api.World, byEntity, slot);
		}
		handling = EnumHandHandling.PreventDefault;
	}

	private bool rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (player == null)
		{
			return false;
		}
		Block block = api.World.BlockAccessor.GetBlock(blockSel.Position);
		IWrenchOrientable wrenchOrientable = block.GetInterface<IWrenchOrientable>(api.World, blockSel.Position);
		if (wrenchOrientable != null)
		{
			Rotate(blockSel, dir, player, block, wrenchOrientable);
			return true;
		}
		BlockBehaviorWrenchOrientable behavior = block.GetBehavior<BlockBehaviorWrenchOrientable>();
		if (behavior == null)
		{
			return false;
		}
		using SortedSet<AssetLocation>.Enumerator enumerator = BlockBehaviorWrenchOrientable.VariantsByType[behavior.BaseCode].GetEnumerator();
		while (enumerator.MoveNext() && (!(enumerator.Current != null) || !enumerator.Current.Equals(behavior.block.Code)))
		{
		}
		AssetLocation blockCode = (enumerator.MoveNext() ? enumerator.Current : BlockBehaviorWrenchOrientable.VariantsByType[behavior.BaseCode].First());
		Block block2 = api.World.GetBlock(blockCode);
		api.World.BlockAccessor.ExchangeBlock(block2.Id, blockSel.Position);
		api.World.PlaySoundAt(block2.Sounds.Place, blockSel.Position, 0.0, player);
		(api.World as IClientWorldAccessor)?.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	private void Rotate(BlockSelection blockSel, int dir, IPlayer byPlayer, Block block, IWrenchOrientable iwre)
	{
		api.World.PlaySoundAt(block.Sounds.Place, blockSel.Position, 0.0, byPlayer);
		(api.World as IClientWorldAccessor)?.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		iwre.Rotate(byPlayer.Entity, blockSel, dir);
	}
}
