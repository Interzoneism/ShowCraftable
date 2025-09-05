using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlayerRevivable : EntityBehavior
{
	private EntityPlayer entityPlayer;

	public EntityBehaviorPlayerRevivable(Entity entity)
		: base(entity)
	{
		entityPlayer = entity as EntityPlayer;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (entity.Alive)
		{
			return;
		}
		double num = entityPlayer.RevivableIngameHoursLeft();
		if (num < 1.0)
		{
			if (num * 60.0 >= 0.0)
			{
				infotext.AppendLine(Lang.Get("Mortally wounded, alive for {0} ingame minutes.", (int)(num * 60.0)));
			}
		}
		else
		{
			infotext.AppendLine(Lang.Get("Mortally wounded, alive for {0} more hours", (int)num));
		}
	}

	public void AttemptRevive()
	{
		if (!(entityPlayer.RevivableIngameHoursLeft() <= 0.0))
		{
			entity.Revive();
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		WorldInteraction[] array = base.GetInteractionHelp(world, es, player, ref handled);
		if (!entity.Alive && entityPlayer.RevivableIngameHoursLeft() > 0.0)
		{
			if (array == null)
			{
				array = Array.Empty<WorldInteraction>();
			}
			array = array.Append(GetReviveInteractionHelp(world.Api));
		}
		return array;
	}

	public static WorldInteraction GetReviveInteractionHelp(ICoreAPI api)
	{
		ItemStack[] orCreate = ObjectCacheUtil.GetOrCreate(api, "poulticeStacks", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item item in api.World.Items)
			{
				if (item.HasBehavior<BehaviorHealingItem>() || item is ItemPoultice)
				{
					list.Add(new ItemStack(item));
				}
			}
			return list.ToArray();
		});
		return new WorldInteraction
		{
			ActionLangCode = "reviveplayer",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = "ctrl",
			Itemstacks = orCreate,
			RequireFreeHand = true
		};
	}

	public override string PropertyName()
	{
		return "playerrevivable";
	}
}
