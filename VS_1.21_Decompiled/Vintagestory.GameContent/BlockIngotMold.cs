using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockIngotMold : Block
{
	private WorldInteraction[] interactionsLeft;

	private WorldInteraction[] interactionsRight;

	private readonly Cuboidf[] oneMoldBoxes = new Cuboidf[1]
	{
		new Cuboidf(0f, 0f, 0f, 1f, 0.1875f, 1f)
	};

	private readonly Cuboidf[] twoMoldBoxesNS = new Cuboidf[2]
	{
		new Cuboidf(0f, 0f, 0f, 0.5f, 0.1875f, 1f),
		new Cuboidf(0.5f, 0f, 0f, 1f, 0.1875f, 1f)
	};

	private readonly Cuboidf[] twoMoldBoxesEW = new Cuboidf[2]
	{
		new Cuboidf(0f, 0f, 0f, 1f, 0.1875f, 0.5f),
		new Cuboidf(0f, 0f, 0.5f, 1f, 0.1875f, 1f)
	};

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client || LastCodePart() == "raw")
		{
			return;
		}
		interactionsLeft = ObjectCacheUtil.GetOrCreate(api, "ingotmoldBlockInteractionsLeft", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (collectible is BlockSmeltedContainer)
				{
					list.Add(new ItemStack(collectible));
				}
				EnumTool? tool = collectible.Tool;
				if (tool.HasValue && tool == EnumTool.Chisel)
				{
					list2.Add(new ItemStack(collectible));
				}
			}
			return new WorldInteraction[5]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { IsFullLeft: false, ShatteredLeft: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-takeingot",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { IsFullLeft: not false, IsHardenedLeft: not false } blockEntityIngotMold && !blockEntityIngotMold.ShatteredLeft
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-chiselmoldforbits",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { FillLevelLeft: >0, IsHardenedLeft: not false, ShatteredLeft: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { ContentsLeft: null } blockEntityIngotMold && !blockEntityIngotMold.ShatteredLeft
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-placemold",
					HotKeyCode = "shift",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(this)
					},
					MouseButton = EnumMouseButton.Right,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: <2 })) ? null : wi.Itemstacks
				}
			};
		});
		interactionsRight = ObjectCacheUtil.GetOrCreate(api, "ingotmoldBlockInteractionsRight", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (CollectibleObject collectible2 in api.World.Collectibles)
			{
				if (collectible2 is BlockSmeltedContainer)
				{
					list.Add(new ItemStack(collectible2));
				}
				EnumTool? tool = collectible2.Tool;
				if (tool.HasValue && tool == EnumTool.Chisel)
				{
					list2.Add(new ItemStack(collectible2));
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, IsFullRight: false, ShatteredRight: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-takeingot",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, IsFullRight: not false, IsHardenedRight: not false } blockEntityIngotMold && !blockEntityIngotMold.ShatteredRight
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-chiselmoldforbits",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, FillLevelRight: >0, IsHardenedRight: not false, ShatteredRight: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, ContentsRight: null } blockEntityIngotMold && !blockEntityIngotMold.ShatteredRight
				}
			};
		});
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold { QuantityMolds: not 1 } blockEntityIngotMold))
		{
			return oneMoldBoxes;
		}
		int index = BlockFacing.HorizontalFromAngle(blockEntityIngotMold.MeshAngle).Index;
		if (index == 0 || index == 2)
		{
			return twoMoldBoxesEW;
		}
		return twoMoldBoxesNS;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetSelectionBoxes(blockAccessor, pos);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
		else if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite)) is BlockEntityIngotMold blockEntityIngotMold)
		{
			IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
			if (player != null && blockEntityIngotMold.OnPlayerInteract(player, blockSel.Face, blockSel.HitPosition))
			{
				handling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel?.Position) is BlockEntityIngotMold blockEntityIngotMold)
		{
			return blockEntityIngotMold.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (world.Rand.NextDouble() > 0.05)
		{
			base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
			return;
		}
		BlockEntityIngotMold blockEntity = GetBlockEntity<BlockEntityIngotMold>(pos);
		if ((blockEntity != null && blockEntity.TemperatureLeft > 300f) || (blockEntity != null && blockEntity.TemperatureRight > 300f))
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				Type = EnumDamageType.Fire,
				SourcePos = pos.ToVec3d()
			}, 0.5f);
		}
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityIngotMold blockEntity = GetBlockEntity<BlockEntityIngotMold>(pos);
			if ((blockEntity != null && blockEntity.TemperatureLeft > 300f) || (blockEntity != null && blockEntity.TemperatureRight > 300f))
			{
				return 10000f;
			}
		}
		return 0f;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "onlywhensneaking";
			return false;
		}
		if (!world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP))
		{
			failureCode = "requiresolidground";
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold blockEntityIngotMold && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			BlockSelection blockSelection = byPlayer?.CurrentBlockSelection;
			if (blockSelection != null)
			{
				blockEntityIngotMold.SetSelectedSide(blockSelection.HitPosition);
				IPlayerInventoryManager playerInventoryManager = byPlayer?.InventoryManager;
				if (playerInventoryManager != null)
				{
					EnumTool? offhandTool = playerInventoryManager.OffhandTool;
					if (offhandTool.HasValue && offhandTool == EnumTool.Hammer)
					{
						offhandTool = playerInventoryManager.ActiveTool;
						if (offhandTool.HasValue && offhandTool == EnumTool.Chisel)
						{
							ItemStack chiseledStack = blockEntityIngotMold.GetChiseledStack(blockEntityIngotMold.SelectedContents, blockEntityIngotMold.SelectedFillLevel, blockEntityIngotMold.SelectedShattered, blockEntityIngotMold.SelectedIsHardened);
							if (chiseledStack != null)
							{
								if (SplitDropStacks)
								{
									for (int i = 0; i < chiseledStack.StackSize; i++)
									{
										ItemStack itemStack = chiseledStack.Clone();
										itemStack.StackSize = 1;
										world.SpawnItemEntity(itemStack, pos);
									}
								}
								else
								{
									world.SpawnItemEntity(chiseledStack, pos);
								}
								world.PlaySoundAt(new AssetLocation("sounds/block/ingot"), pos, 0.0, byPlayer);
								blockEntityIngotMold.SelectedContents = null;
								blockEntityIngotMold.SelectedFillLevel = 0;
								DamageItem(world, byPlayer.Entity, playerInventoryManager.ActiveHotbarSlot);
								DamageItem(world, byPlayer.Entity, byPlayer.Entity?.LeftHandItemSlot);
								return;
							}
						}
					}
				}
			}
		}
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return Drops;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold blockEntityIngotMold))
		{
			return new ItemStack[1]
			{
				new ItemStack(this)
			};
		}
		ItemStack[] stateAwareMolds = blockEntityIngotMold.GetStateAwareMolds();
		ItemStack[] stateAwareMoldedStacks = blockEntityIngotMold.GetStateAwareMoldedStacks();
		int num = 0;
		ItemStack[] array = new ItemStack[stateAwareMolds.Length + stateAwareMoldedStacks.Length];
		ReadOnlySpan<ItemStack> readOnlySpan = new ReadOnlySpan<ItemStack>(stateAwareMolds);
		readOnlySpan.CopyTo(new Span<ItemStack>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<ItemStack> readOnlySpan2 = new ReadOnlySpan<ItemStack>(stateAwareMoldedStacks);
		readOnlySpan2.CopyTo(new Span<ItemStack>(array).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		return array;
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold blockEntityIngotMold)
		{
			if (blockEntityIngotMold.SelectedMold == null)
			{
				return base.GetPlacedBlockName(world, pos);
			}
			if (!blockEntityIngotMold.SelectedShattered)
			{
				return blockEntityIngotMold.SelectedMold.GetName();
			}
			return Lang.Get("ceramicblock-blockname-shattered", blockEntityIngotMold.SelectedMold.GetName());
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityIngotMold blockEntityIngotMold)
		{
			blockEntityIngotMold.SetSelectedSide(selection.HitPosition);
			return (blockEntityIngotMold.IsRightSideSelected ? interactionsRight : interactionsLeft).Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return ((selection.SelectionBoxIndex == 0) ? interactionsLeft : interactionsRight).Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityIngotMold blockEntityIngotMold)
		{
			blockEntityIngotMold.MoldLeft = byItemStack?.Clone() ?? byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Clone() ?? new ItemStack(this);
			if (blockEntityIngotMold.MoldLeft != null)
			{
				blockEntityIngotMold.MoldLeft.StackSize = 1;
			}
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float meshAngle = (float)(int)Math.Round((float)Math.Atan2(y, x) / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
			blockEntityIngotMold.MeshAngle = meshAngle;
			blockEntityIngotMold.MarkDirty();
		}
		return num;
	}
}
