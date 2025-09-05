using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityShelf : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private static int slotCount = 8;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "shelf";

	public override string AttributeTransformCode => "onshelfTransform";

	public BlockEntityShelf()
	{
		inv = new InventoryGeneric(slotCount, "shelf-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inv.OnAcquireTransitionSpeed += Inv_OnAcquireTransitionSpeed;
	}

	private float Inv_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
	{
		if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
		{
			Room room = container.Room;
			if (room == null || room.ExitCount != 0)
			{
				return 0.5f;
			}
			return 2f;
		}
		if (Api == null)
		{
			return 0f;
		}
		if (transType == EnumTransitionType.Ripen)
		{
			float perishRate = container.GetPerishRate();
			return GameMath.Clamp((1f - perishRate - 0.5f) * 3f, 0f, 1f);
		}
		return 1f;
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return TryTake(byPlayer, blockSel);
		}
		if (TryUse(byPlayer, blockSel))
		{
			return true;
		}
		if (GetShelvableLayout(activeHotbarSlot.Itemstack).HasValue)
		{
			return TryPut(byPlayer, blockSel);
		}
		return false;
	}

	public static EnumShelvableLayout? GetShelvableLayout(ItemStack? stack)
	{
		if (stack == null)
		{
			return null;
		}
		JsonObject jsonObject = stack.Collectible?.Attributes;
		EnumShelvableLayout? enumShelvableLayout = stack.Collectible?.GetCollectibleInterface<IShelvable>()?.GetShelvableType(stack);
		EnumShelvableLayout? enumShelvableLayout2 = enumShelvableLayout;
		if (!enumShelvableLayout2.HasValue)
		{
			enumShelvableLayout = jsonObject?["shelvable"].AsString() switch
			{
				"Quadrants" => EnumShelvableLayout.Quadrants, 
				"Halves" => EnumShelvableLayout.Halves, 
				"SingleCenter" => EnumShelvableLayout.SingleCenter, 
				_ => null, 
			};
		}
		enumShelvableLayout2 = enumShelvableLayout;
		if (!enumShelvableLayout2.HasValue)
		{
			enumShelvableLayout = ((jsonObject != null && jsonObject["shelvable"].AsBool()) ? new EnumShelvableLayout?(EnumShelvableLayout.Quadrants) : ((EnumShelvableLayout?)null));
		}
		return enumShelvableLayout;
	}

	public bool CanUse(ItemStack? stack, BlockSelection blockSel)
	{
		if (stack == null)
		{
			return false;
		}
		CollectibleObject collectible = stack.Collectible;
		bool flag = blockSel.SelectionBoxIndex > 1;
		bool flag2 = blockSel.SelectionBoxIndex % 2 == 0;
		EnumShelvableLayout? shelvableLayout = GetShelvableLayout(inv[flag ? 4 : 0].Itemstack);
		if ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) && !flag2)
		{
			shelvableLayout = GetShelvableLayout(inv[flag ? 6 : 2].Itemstack);
		}
		int num = (flag ? 4 : 0) + ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		int num2 = num;
		bool flag3;
		if (shelvableLayout.HasValue)
		{
			EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
			if ((uint)(valueOrDefault - 1) <= 1u)
			{
				flag3 = true;
				goto IL_00be;
			}
		}
		flag3 = false;
		goto IL_00be;
		IL_00be:
		for (int num3 = num2 + (flag3 ? 1 : 2) - 1; num3 >= num; num3--)
		{
			if (!inv[num3].Empty)
			{
				CollectibleObject collectible2 = inv[num3].Itemstack.Collectible;
				flag3 = collectible != null && collectible.Attributes?["mealContainer"]?.AsBool() == true;
				if (!flag3)
				{
					bool flag4 = ((collectible is IContainedInteractable || collectible is IBlockMealContainer) ? true : false);
					flag3 = flag4;
				}
				if (flag3)
				{
					return collectible2 is BlockCookedContainerBase;
				}
				if (collectible != null && collectible.Attributes?["canSealCrock"]?.AsBool() == true)
				{
					return collectible2 is BlockCrock;
				}
			}
		}
		return false;
	}

	public bool CanPlace(ItemStack? stack, BlockSelection blockSel, out bool canTake)
	{
		bool flag = blockSel.SelectionBoxIndex > 1;
		bool flag2 = blockSel.SelectionBoxIndex % 2 == 0;
		EnumShelvableLayout? shelvableLayout = GetShelvableLayout(inv[flag ? 4 : 0].Itemstack);
		if (shelvableLayout.HasValue)
		{
			EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
			if (valueOrDefault == EnumShelvableLayout.SingleCenter || (valueOrDefault == EnumShelvableLayout.Halves && flag2))
			{
				goto IL_0085;
			}
		}
		shelvableLayout = GetShelvableLayout(inv[flag ? 6 : 2].Itemstack);
		if (shelvableLayout.HasValue && shelvableLayout == EnumShelvableLayout.Halves && !flag2)
		{
			goto IL_0085;
		}
		EnumShelvableLayout? shelvableLayout2 = GetShelvableLayout(stack);
		int num = (flag ? 4 : 0) + ((!shelvableLayout2.HasValue || shelvableLayout2 != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		int num2 = num;
		bool flag3;
		if (shelvableLayout2.HasValue)
		{
			EnumShelvableLayout valueOrDefault2 = shelvableLayout2.GetValueOrDefault();
			if ((uint)(valueOrDefault2 - 1) <= 1u)
			{
				flag3 = true;
				goto IL_00dd;
			}
		}
		flag3 = false;
		goto IL_00dd;
		IL_0085:
		canTake = true;
		return false;
		IL_00dd:
		int num3 = num2 + (flag3 ? 1 : 2);
		canTake = false;
		bool result = false;
		for (int num4 = num3 - 1; num4 >= num; num4--)
		{
			if (inv[num4].Empty)
			{
				result = true;
			}
			else
			{
				canTake = true;
			}
		}
		return result;
	}

	private bool TryUse(IPlayer player, BlockSelection blockSel)
	{
		bool flag = blockSel.SelectionBoxIndex > 1;
		bool flag2 = blockSel.SelectionBoxIndex % 2 == 0;
		EnumShelvableLayout? shelvableLayout = GetShelvableLayout(inv[flag ? 4 : 0].Itemstack);
		if ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) && !flag2)
		{
			shelvableLayout = GetShelvableLayout(inv[flag ? 6 : 2].Itemstack);
		}
		int num = (flag ? 4 : 0) + ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		int num2 = num;
		bool flag3;
		if (shelvableLayout.HasValue)
		{
			EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
			if ((uint)(valueOrDefault - 1) <= 1u)
			{
				flag3 = true;
				goto IL_00b0;
			}
		}
		flag3 = false;
		goto IL_00b0;
		IL_00b0:
		for (int num3 = num2 + (flag3 ? 1 : 2) - 1; num3 >= num; num3--)
		{
			if (!player.Entity.Controls.ShiftKey && inv[num3]?.Itemstack?.Collectible is IContainedInteractable containedInteractable && containedInteractable.OnContainedInteractStart(this, inv[num3], player, blockSel))
			{
				MarkDirty();
				return true;
			}
		}
		return false;
	}

	private bool TryPut(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool flag = blockSel.SelectionBoxIndex > 1;
		bool flag2 = blockSel.SelectionBoxIndex % 2 == 0;
		int num = 0;
		EnumShelvableLayout? shelvableLayout = GetShelvableLayout(activeHotbarSlot.Itemstack);
		int num2 = (flag ? 4 : 0) + ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		int num3 = num2 + ((shelvableLayout.HasValue && shelvableLayout == EnumShelvableLayout.SingleCenter) ? 4 : 2);
		bool flag3;
		if (shelvableLayout.HasValue)
		{
			EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
			if ((uint)(valueOrDefault - 1) <= 1u)
			{
				flag3 = true;
				goto IL_0095;
			}
		}
		flag3 = false;
		goto IL_0095;
		IL_0204:
		int num4;
		num3 = num4 + (flag3 ? 1 : 2);
		for (int i = num2; i < num3; i++)
		{
			if (inv[i].Empty)
			{
				int num5 = activeHotbarSlot.TryPutInto(Api.World, inv[i]);
				MarkDirty();
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				if (num5 > 0)
				{
					Api.World.PlaySoundAt(inv[i].Itemstack?.Block?.Sounds?.Place ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
					Api.World.Logger.Audit("{0} Put 1x{1} into Shelf at {2}.", byPlayer.PlayerName, inv[i].Itemstack?.Collectible.Code, Pos);
					return true;
				}
				return false;
			}
		}
		(Api as ICoreClientAPI)?.TriggerIngameError(this, "shelffull", Lang.Get("shelfhelp-shelffull-error"));
		return false;
		IL_0095:
		if (flag3)
		{
			for (int j = num2; j < num3; j++)
			{
				if (!inv[j].Empty)
				{
					EnumShelvableLayout? shelvableLayout2 = GetShelvableLayout(inv[j].Itemstack);
					num += ((shelvableLayout2.HasValue && shelvableLayout2 == EnumShelvableLayout.SingleCenter) ? 4 : ((!shelvableLayout2.HasValue || shelvableLayout2 != EnumShelvableLayout.Halves) ? 1 : 2));
				}
			}
		}
		if (num > 0 && num < ((shelvableLayout.HasValue && shelvableLayout == EnumShelvableLayout.SingleCenter) ? 4 : 2))
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "needsmorespace", Lang.Get("shelfhelp-needsmorespace-error"));
			return false;
		}
		if (!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter)
		{
			shelvableLayout = GetShelvableLayout(inv[flag ? 4 : 0].Itemstack);
		}
		if ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) && !flag2)
		{
			shelvableLayout = GetShelvableLayout(inv[flag ? 6 : 2].Itemstack);
		}
		num2 = (flag ? 4 : 0) + ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		num4 = num2;
		if (shelvableLayout.HasValue)
		{
			EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
			if ((uint)(valueOrDefault - 1) <= 1u)
			{
				flag3 = true;
				goto IL_0204;
			}
		}
		flag3 = false;
		goto IL_0204;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		bool flag = blockSel.SelectionBoxIndex > 1;
		bool flag2 = blockSel.SelectionBoxIndex % 2 == 0;
		EnumShelvableLayout? shelvableLayout = GetShelvableLayout(inv[flag ? 4 : 0].Itemstack);
		if ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) && !flag2)
		{
			shelvableLayout = GetShelvableLayout(inv[flag ? 6 : 2].Itemstack);
		}
		int num = (flag ? 4 : 0) + ((!shelvableLayout.HasValue || shelvableLayout != EnumShelvableLayout.SingleCenter) ? ((!flag2) ? 2 : 0) : 0);
		for (int num2 = num + ((shelvableLayout.HasValue && shelvableLayout == EnumShelvableLayout.SingleCenter) ? 4 : 2) - 1; num2 >= num; num2--)
		{
			if (!inv[num2].Empty)
			{
				ItemStack itemStack = inv[num2].TakeOut(1);
				if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
				{
					AssetLocation assetLocation = itemStack?.Block?.Sounds?.Place;
					Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				}
				if (itemStack != null && itemStack.StackSize > 0)
				{
					Api.World.SpawnItemEntity(itemStack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Shelf at {2}.", byPlayer.PlayerName, itemStack?.Collectible.Code, Pos);
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				MarkDirty();
				return true;
			}
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[slotCount][];
		for (int num = 0; num < slotCount; num++)
		{
			EnumShelvableLayout? shelvableLayout = GetShelvableLayout(inv[num].Itemstack);
			float num2 = ((num % 4 >= 2) ? 0.75f : 0.25f);
			float y = ((num >= 4) ? 0.625f : 0.125f);
			float num3 = ((num % 2 == 0) ? 0.25f : 0.625f);
			bool flag = ((num == 0 || num == 4) ? true : false);
			if (flag && shelvableLayout.HasValue && shelvableLayout == EnumShelvableLayout.SingleCenter)
			{
				num2 = 0.5f;
			}
			switch (num)
			{
			case 0:
			case 2:
			case 4:
			case 6:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool flag2 = flag;
			bool flag3;
			if (flag2)
			{
				if (shelvableLayout.HasValue)
				{
					EnumShelvableLayout valueOrDefault = shelvableLayout.GetValueOrDefault();
					if ((uint)(valueOrDefault - 1) <= 1u)
					{
						flag3 = true;
						goto IL_00e2;
					}
				}
				flag3 = false;
				goto IL_00e2;
			}
			goto IL_00e6;
			IL_00e2:
			flag2 = flag3;
			goto IL_00e6;
			IL_00e6:
			if (flag2)
			{
				num3 = 0.4f;
			}
			array[num] = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(base.Block.Shape.rotateY).Translate(num2 - 0.5f, y, num3 - 0.5f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		float num = GameMath.Clamp((1f - container.GetPerishRate() - 0.5f) * 3f, 0f, 1f);
		if (num > 0f)
		{
			sb.Append(Lang.Get("Suitable spot for food ripening."));
		}
		sb.AppendLine();
		bool flag = forPlayer.CurrentBlockSelection != null && forPlayer.CurrentBlockSelection.SelectionBoxIndex > 1;
		for (int num2 = 3; num2 >= 0; num2--)
		{
			int num3 = num2 + (flag ? 4 : 0);
			num3 ^= 2;
			if (!inv[num3].Empty)
			{
				ItemStack itemstack = inv[num3].Itemstack;
				if (itemstack?.Collectible.TransitionableProps != null && itemstack.Collectible.TransitionableProps.Length != 0)
				{
					sb.Append(PerishableInfoCompact(Api, inv[num3], num));
				}
				else
				{
					sb.AppendLine(itemstack?.Collectible.GetCollectibleInterface<IContainedCustomName>()?.GetContainedInfo(inv[num3]) ?? itemstack?.GetName() ?? Lang.Get("unknown"));
				}
			}
		}
	}

	public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
	{
		if (contentSlot.Empty)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (withStackName)
		{
			stringBuilder.Append(contentSlot.Itemstack.GetName());
		}
		TransitionState[] array = contentSlot.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
		bool flag = false;
		if (array != null)
		{
			bool flag2 = false;
			foreach (TransitionState transitionState in array)
			{
				TransitionableProperties props = transitionState.Props;
				float transitionRateMul = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, props.Type);
				if (transitionRateMul <= 0f)
				{
					continue;
				}
				float transitionLevel = transitionState.TransitionLevel;
				float num = transitionState.FreshHoursLeft / transitionRateMul;
				switch (props.Type)
				{
				case EnumTransitionType.Perish:
				{
					flag2 = true;
					if (transitionLevel > 0f)
					{
						flag = true;
						stringBuilder.Append(", " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
						break;
					}
					double num3 = Api.World.Calendar.HoursPerDay;
					if ((double)num / num3 >= (double)Api.World.Calendar.DaysPerYear)
					{
						stringBuilder.Append(", " + Lang.Get("fresh for {0} years", Math.Round((double)num / num3 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)num > num3)
					{
						stringBuilder.Append(", " + Lang.Get("fresh for {0} days", Math.Round((double)num / num3, 1)));
					}
					else
					{
						stringBuilder.Append(", " + Lang.Get("fresh for {0} hours", Math.Round(num, 1)));
					}
					break;
				}
				case EnumTransitionType.Ripen:
				{
					if (flag)
					{
						break;
					}
					flag2 = true;
					if (transitionLevel > 0f)
					{
						stringBuilder.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100f), (transitionState.TransitionHours - transitionState.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
						break;
					}
					double num2 = Api.World.Calendar.HoursPerDay;
					if ((double)num / num2 >= (double)Api.World.Calendar.DaysPerYear)
					{
						stringBuilder.Append(", " + Lang.Get("will ripen in {0} years", Math.Round((double)num / num2 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)num > num2)
					{
						stringBuilder.Append(", " + Lang.Get("will ripen in {0} days", Math.Round((double)num / num2, 1)));
					}
					else
					{
						stringBuilder.Append(", " + Lang.Get("will ripen in {0} hours", Math.Round(num, 1)));
					}
					break;
				}
				}
			}
			if (flag2)
			{
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString();
	}
}
