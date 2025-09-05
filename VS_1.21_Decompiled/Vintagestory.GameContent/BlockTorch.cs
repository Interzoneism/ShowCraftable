using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTorch : BlockGroundAndSideAttachable, IIgnitable
{
	private bool isExtinct;

	private bool isLit;

	private Dictionary<string, Cuboidi> attachmentAreas;

	private WorldInteraction[] interactions;

	public bool IsExtinct => isExtinct;

	public Block ExtinctVariant { get; private set; }

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		if (forEntity.AnimManager.IsAnimationActive("startfire"))
		{
			return null;
		}
		return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		HeldPriorityInteract = true;
		Dictionary<string, RotatableCube> dictionary = Attributes?["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		if (dictionary != null)
		{
			attachmentAreas = new Dictionary<string, Cuboidi>();
			foreach (KeyValuePair<string, RotatableCube> item in dictionary)
			{
				item.Value.Origin.Set(8.0, 8.0, 8.0);
				attachmentAreas[item.Key] = item.Value.RotatedCopy().ConvertToCuboidi();
			}
		}
		if (Variant.ContainsKey("state"))
		{
			AssetLocation blockCode = CodeWithVariant("state", "extinct");
			ExtinctVariant = api.World.GetBlock(blockCode);
			isExtinct = Variant["state"] == "extinct" || Variant["state"] == "burnedout";
			isLit = Variant["state"] == "lit";
		}
		if (!IsExtinct)
		{
			return;
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "torchInteractions" + FirstCodePart(), delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-ignite",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => wi.Itemstacks
				}
			};
		});
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		if (api.World.Side == EnumAppSide.Server && byEntity.Swimming && !IsExtinct && ExtinctVariant != null)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), byEntity.Pos.X + 0.5, byEntity.Pos.InternalY + 0.75, byEntity.Pos.Z + 0.5, null, randomizePitch: false, 16f);
			int stackSize = slot.Itemstack.StackSize;
			slot.Itemstack = new ItemStack(ExtinctVariant);
			slot.Itemstack.StackSize = stackSize;
			slot.MarkDirty();
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		if (!IsExtinct && entityItem.Swimming && ExtinctVariant != null)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), entityItem.Pos.X + 0.5, entityItem.Pos.InternalY + 0.75, entityItem.Pos.Z + 0.5, null, randomizePitch: false, 16f);
			int stackSize = entityItem.Itemstack.StackSize;
			entityItem.Itemstack = new ItemStack(ExtinctVariant);
			entityItem.Itemstack.StackSize = stackSize;
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is IIgnitable ignitable)
		{
			if (byEntity is EntityPlayer entityPlayer && !byEntity.World.Claims.TryAccess(entityPlayer.Player, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				return;
			}
			if (isExtinct)
			{
				if (ignitable.OnTryIgniteStack(byEntity, blockSel.Position, slot, 0f) == EnumIgniteState.Ignitable)
				{
					byEntity.World.PlaySoundAt(new AssetLocation("sounds/torch-ignite"), byEntity, (byEntity as EntityPlayer)?.Player, randomizePitch: false, 16f);
					handling = EnumHandHandling.PreventDefault;
				}
			}
			else
			{
				handling = EnumHandHandling.Handled;
			}
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (isExtinct && blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is IIgnitable ignitable)
		{
			if (byEntity is EntityPlayer entityPlayer && !byEntity.World.Claims.TryAccess(entityPlayer.Player, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				return false;
			}
			switch (ignitable.OnTryIgniteStack(byEntity, blockSel.Position, slot, secondsUsed))
			{
			case EnumIgniteState.Ignitable:
				if (byEntity.World is IClientWorldAccessor && secondsUsed > 0.25f && (int)(30f * secondsUsed) % 2 == 1)
				{
					Random rand = byEntity.World.Rand;
					Vec3d basePos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition).Add(rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125);
					Block block = byEntity.World.GetBlock(new AssetLocation("fire"));
					AdvancedParticleProperties advancedParticleProperties = block.ParticleProperties[block.ParticleProperties.Length - 1].Clone();
					advancedParticleProperties.basePos = basePos;
					advancedParticleProperties.Quantity.avg = 0.5f;
					byEntity.World.SpawnParticles(advancedParticleProperties);
					advancedParticleProperties.Quantity.avg = 0f;
				}
				return true;
			case EnumIgniteState.IgniteNow:
			{
				if (byEntity.World.Side == EnumAppSide.Client)
				{
					return false;
				}
				ItemStack itemstack = new ItemStack(byEntity.World.GetBlock(CodeWithVariant("state", "lit")));
				if (slot.StackSize == 1)
				{
					slot.Itemstack = itemstack;
				}
				else
				{
					slot.TakeOut(1);
					if (!byEntity.TryGiveItemStack(itemstack))
					{
						byEntity.World.SpawnItemEntity(itemstack, byEntity.Pos.XYZ);
					}
				}
				slot.MarkDirty();
				return false;
			}
			}
		}
		return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (Variant["state"] == "burnedout")
		{
			return Array.Empty<ItemStack>();
		}
		Block block = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", "up"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (Variant["state"] == "burnedout")
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (IsExtinct)
		{
			if (!(secondsIgniting > 1f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		Block block = api.World.GetBlock(CodeWithVariant("state", "lit"));
		if (block != null)
		{
			api.World.BlockAccessor.SetBlock(block.Id, pos);
		}
	}

	public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
	{
		base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
		float stormStrength = api.ModLoader.GetModSystem<SystemTemporalStability>().StormStrength;
		if (!IsExtinct && attackedEntity != null && byEntity.World.Side == EnumAppSide.Server && api.World.Rand.NextDouble() < 0.1 + (double)stormStrength)
		{
			attackedEntity.Ignite();
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (Variant["state"] == "burnedout")
		{
			return Array.Empty<WorldInteraction>();
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(interactions);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (Attributes != null && isLit)
		{
			dsc.AppendLine();
			dsc.AppendLine(Lang.Get("Burns for {0} hours when placed.", Attributes["transientProps"]["inGameHours"].AsFloat()));
		}
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
		if (isLit)
		{
			ReplaceWithBurnedOut(world.BlockAccessor, blockPos);
		}
	}

	protected void ReplaceWithBurnedOut(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlock(pos, 2);
		if (block.IsLiquid() && block.LiquidLevel > 3)
		{
			Block block2 = api.World.GetBlock(CodeWithVariant("state", "burnedout"));
			if (block2 != null)
			{
				api.World.BlockAccessor.SetBlock(block2.Id, pos);
			}
		}
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		if (!base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes))
		{
			return false;
		}
		if (isLit)
		{
			ReplaceWithBurnedOut(blockAccessor, pos);
		}
		return true;
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if (!IsExtinct)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}
}
