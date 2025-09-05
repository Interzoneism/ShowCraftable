using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemMouseInWorldInteractions : ClientSystem
{
	internal long lastbuildMilliseconds;

	internal long lastbreakMilliseconds;

	internal long lastbreakNotifyMilliseconds;

	private bool isSurvivalBreaking;

	private int survivalBreakingCounter;

	private BlockDamage curBlockDmg;

	public bool prevMouseLeft;

	public bool prevMouseRight;

	private float accum;

	private float stepPacketAccum;

	public override string Name => "miw";

	public SystemMouseInWorldInteractions(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(OnEverySecond, 1000);
		game.RegisterGameTickListener(OnGameTick, 20);
		game.eventManager.RegisterRenderer(OnRenderOpaque, EnumRenderStage.Opaque, Name + "-op", 0.9);
		game.eventManager.RegisterRenderer(OnRenderOit, EnumRenderStage.OIT, Name + "-oit", 0.9);
		game.eventManager.RegisterRenderer(OnRenderOrtho, EnumRenderStage.Ortho, Name + "-2d", 0.9);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, Name + "-done", 0.9);
	}

	private void OnEverySecond(float dt)
	{
		List<BlockPos> list = new List<BlockPos>();
		foreach (KeyValuePair<BlockPos, BlockDamage> damagedBlock in game.damagedBlocks)
		{
			BlockDamage value = damagedBlock.Value;
			if (game.ElapsedMilliseconds - value.LastBreakEllapsedMs > 1000)
			{
				value.LastBreakEllapsedMs = game.ElapsedMilliseconds;
				value.RemainingResistance += 0.1f * value.Block.GetResistance(game.BlockAccessor, value.Position);
				game.eventManager?.TriggerBlockUnbreaking(value);
				if (value.RemainingResistance >= value.Block.GetResistance(game.BlockAccessor, value.Position))
				{
					list.Add(damagedBlock.Key);
				}
			}
		}
		foreach (BlockPos item in list)
		{
			game.damagedBlocks.Remove(item);
		}
		ScreenManager.FrameProfiler.Mark("miw-1s");
	}

	public void OnFinalizeFrame(float dt)
	{
		if (game.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			ScreenManager.FrameProfiler.Mark("finaframe-beg");
			if (game.MouseGrabbed || game.mouseWorldInteractAnyway || game.player.worlddata.AreaSelectionMode)
			{
				UpdatePicking(dt);
			}
			prevMouseLeft = game.InWorldMouseState.Left;
			prevMouseRight = game.InWorldMouseState.Right;
			ScreenManager.FrameProfiler.Mark("finaframe-miw");
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (game.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			if (game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
			{
				lastbuildMilliseconds = 0L;
			}
			StopBlockBreakSurvival();
		}
	}

	private void OnGameTick(float dt)
	{
		ItemStack itemStack = game.player.inventoryMgr?.ActiveHotbarSlot?.Itemstack;
		if (game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
		{
			itemStack?.Collectible.OnHeldIdle(game.player.inventoryMgr.ActiveHotbarSlot, game.EntityPlayer);
		}
		if (!game.EntityPlayer.LeftHandItemSlot.Empty)
		{
			game.EntityPlayer.LeftHandItemSlot.Itemstack.Collectible.OnHeldIdle(game.EntityPlayer.LeftHandItemSlot, game.EntityPlayer);
		}
		if (game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator || game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
		{
			return;
		}
		if (!game.MouseGrabbed && !game.mouseWorldInteractAnyway)
		{
			if (game.EntityPlayer.Controls.HandUsingBlockSel != null)
			{
				ClientPlayer player = game.player;
				EntityPlayer entityPlayer = game.EntityPlayer;
				Block block = game.BlockAccessor.GetBlock(entityPlayer.Controls.HandUsingBlockSel.Position);
				EnumHandInteract handUse = entityPlayer.Controls.HandUse;
				if (block != null)
				{
					float secondsUsed = (float)(game.ElapsedMilliseconds - entityPlayer.Controls.UsingBeginMS) / 1000f;
					entityPlayer.Controls.HandUse = ((!block.OnBlockInteractCancel(secondsUsed, game, player, entityPlayer.Controls.HandUsingBlockSel, EnumItemUseCancelReason.ReleasedMouse)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
					game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, handUse, EnumHandInteractNw.CancelBlockUse, firstEvent: false);
				}
			}
		}
		else
		{
			accum += dt;
			if ((double)accum > 0.25)
			{
				accum = 0f;
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, game.EntityPlayer.Controls.HandUse, EnumHandInteractNw.StepHeldItemUse, firstEvent: false);
			}
			HandleHandInteraction(dt);
			ScreenManager.FrameProfiler.Mark("miw-handlehandinteraction");
		}
	}

	private void OnRenderOrtho(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOrtho(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	private void OnRenderOit(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOit(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	private void OnRenderOpaque(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOpaque(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	internal void UpdatePicking(float dt)
	{
		UpdateCurrentSelection();
		if (game.MouseGrabbed || game.mouseWorldInteractAnyway)
		{
			if (game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				if ((game.InWorldMouseState.Left ? 1 : 0) + (game.InWorldMouseState.Middle ? 1 : 0) + (game.InWorldMouseState.Right ? 1 : 0) > 1)
				{
					ResetMouseInteractions();
				}
				else if (game.BlockSelection == null)
				{
					HandleMouseInteractionsNoBlockSelected(dt);
				}
				else
				{
					HandleMouseInteractionsBlockSelected(dt);
				}
			}
		}
		else
		{
			ResetMouseInteractions();
		}
	}

	private void HandleHandInteraction(float dt)
	{
		ClientPlayer player = game.player;
		EntityPlayer entityPlayer = game.EntityPlayer;
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		float num = (float)(game.ElapsedMilliseconds - entityPlayer.Controls.UsingBeginMS) / 1000f;
		bool flag = false;
		if (entityPlayer.Controls.HandUse == EnumHandInteract.BlockInteract)
		{
			Block block = game.BlockAccessor.GetBlock(entityPlayer.Controls.HandUsingBlockSel.Position);
			if (game.BlockSelection?.Position == null || !game.BlockSelection.Position.Equals(entityPlayer.Controls.HandUsingBlockSel.Position))
			{
				entityPlayer.Controls.HandUse = ((!block.OnBlockInteractCancel(num, game, player, entityPlayer.Controls.HandUsingBlockSel, EnumItemUseCancelReason.MovedAway)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
				game.SendHandInteraction(2, entityPlayer.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.CancelBlockUse, firstEvent: false, EnumItemUseCancelReason.MovedAway);
				return;
			}
			EnumHandInteract handUse = entityPlayer.Controls.HandUse;
			if (!game.InWorldMouseState.Right)
			{
				entityPlayer.Controls.HandUse = ((!block.OnBlockInteractCancel(num, game, player, game.BlockSelection, EnumItemUseCancelReason.ReleasedMouse)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
			}
			if (entityPlayer.Controls.HandUse != EnumHandInteract.None)
			{
				entityPlayer.Controls.HandUse = (block.OnBlockInteractStep(num, game, player, game.BlockSelection) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
				entityPlayer.Controls.UsingCount++;
				stepPacketAccum += dt;
				if ((double)stepPacketAccum > 0.15)
				{
					game.SendHandInteraction(2, entityPlayer.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StepBlockUse, firstEvent: false);
					stepPacketAccum = 0f;
				}
			}
			if (entityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				block.OnBlockInteractStop(num, game, player, game.BlockSelection);
				flag = true;
			}
			if (entityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, handUse, flag ? EnumHandInteractNw.StopBlockUse : EnumHandInteractNw.CancelBlockUse, firstEvent: false);
			}
		}
		else if (activeHotbarSlot?.Itemstack == null)
		{
			entityPlayer.Controls.HandUse = EnumHandInteract.None;
		}
		else
		{
			EnumHandInteract handUse2 = entityPlayer.Controls.HandUse;
			if ((!game.InWorldMouseState.Right && handUse2 == EnumHandInteract.HeldItemInteract) || (!game.InWorldMouseState.Left && handUse2 == EnumHandInteract.HeldItemAttack))
			{
				entityPlayer.Controls.HandUse = activeHotbarSlot.Itemstack.Collectible.OnHeldUseCancel(num, activeHotbarSlot, game.EntityPlayer, game.BlockSelection, game.EntitySelection, EnumItemUseCancelReason.ReleasedMouse);
			}
			if (entityPlayer.Controls.HandUse != EnumHandInteract.None)
			{
				entityPlayer.Controls.HandUse = activeHotbarSlot.Itemstack.Collectible.OnHeldUseStep(num, activeHotbarSlot, game.EntityPlayer, game.BlockSelection, game.EntitySelection);
				entityPlayer.Controls.UsingCount++;
			}
			if (entityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				activeHotbarSlot.Itemstack?.Collectible.OnHeldUseStop(num, activeHotbarSlot, game.EntityPlayer, game.BlockSelection, game.EntitySelection, handUse2);
				flag = true;
			}
			if (activeHotbarSlot.StackSize <= 0)
			{
				activeHotbarSlot.Itemstack = null;
				activeHotbarSlot.MarkDirty();
			}
			if (entityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, handUse2, (!flag) ? EnumHandInteractNw.CancelHeldItemUse : EnumHandInteractNw.StopHeldItemUse, firstEvent: false);
			}
		}
	}

	private void UpdateCurrentSelection()
	{
		if (game.EntityPlayer == null)
		{
			return;
		}
		bool renderMeta = ClientSettings.RenderMetaBlocks;
		BlockFilter bfilter = (BlockPos pos, Block block) => block == null || renderMeta || block.RenderPass != EnumChunkRenderPass.Meta || (block.GetInterface<IMetaBlock>(game.api.World, pos)?.IsSelectable(pos) ?? false);
		EntityFilter efilter = (Entity e) => e.IsInteractable && e.EntityId != game.EntityPlayer.EntityId;
		bool liquidSelectable = game.LiquidSelectable;
		if (!game.InWorldMouseState.Left && game.InWorldMouseState.Right && game.player?.inventoryMgr?.ActiveHotbarSlot?.Itemstack?.Collectible != null && game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.LiquidSelectable)
		{
			game.forceLiquidSelectable = true;
		}
		game.EntityPlayer.PreviousBlockSelection = game.EntityPlayer.BlockSelection?.Position.Copy();
		if (!game.MouseGrabbed)
		{
			Ray pickingRayByMouseCoordinates = game.pickingRayUtil.GetPickingRayByMouseCoordinates(game);
			if (pickingRayByMouseCoordinates == null)
			{
				game.forceLiquidSelectable = liquidSelectable;
				return;
			}
			game.RayTraceForSelection(pickingRayByMouseCoordinates, ref game.EntityPlayer.BlockSelection, ref game.EntityPlayer.EntitySelection, bfilter, efilter);
		}
		else
		{
			game.RayTraceForSelection(game.player, ref game.EntityPlayer.BlockSelection, ref game.EntityPlayer.EntitySelection, bfilter, efilter);
		}
		game.forceLiquidSelectable = liquidSelectable;
		if (game.EntityPlayer.BlockSelection != null)
		{
			bool firstTick = game.EntityPlayer.PreviousBlockSelection == null || game.EntityPlayer.BlockSelection.Position != game.EntityPlayer.PreviousBlockSelection;
			game.EntityPlayer.BlockSelection.Block.OnBeingLookedAt(game.player, game.EntityPlayer.BlockSelection, firstTick);
		}
	}

	private void ResetMouseInteractions()
	{
		isSurvivalBreaking = false;
		survivalBreakingCounter = 0;
	}

	private void HandleMouseInteractionsNoBlockSelected(float dt)
	{
		StopBlockBreakSurvival();
		if (!((float)(game.InWorldEllapsedMs - lastbuildMilliseconds) / 1000f >= BuildRepeatDelay(game)))
		{
			return;
		}
		if (game.InWorldMouseState.Left || game.InWorldMouseState.Right || game.InWorldMouseState.Middle)
		{
			lastbuildMilliseconds = game.InWorldEllapsedMs;
		}
		else
		{
			lastbuildMilliseconds = 0L;
		}
		if (game.InWorldMouseState.Left)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, on: true, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				return;
			}
			EnumHandHandling handling2 = EnumHandHandling.NotHandled;
			TryBeginAttackWithActiveSlotItem(null, game.EntitySelection, ref handling2);
			if (handling2 != EnumHandHandling.PreventDefaultAnimation && handling2 != EnumHandHandling.PreventDefault)
			{
				StartAttackAnimation();
			}
			if (game.EntitySelection != null && handling2 != EnumHandHandling.PreventDefaultAction && handling2 != EnumHandHandling.PreventDefault)
			{
				game.TryAttackEntity(game.EntitySelection);
			}
		}
		if (game.InWorldMouseState.Right)
		{
			EnumHandling handling3 = EnumHandling.PassThrough;
			game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, on: true, ref handling3);
			if (handling3 == EnumHandling.PassThrough && !TryBeginUseActiveSlotItem(null, game.EntitySelection) && game.EntitySelection != null)
			{
				EntitySelection entitySelection = game.EntitySelection;
				game.EntitySelection.Entity.OnInteract(game.EntityPlayer, game.player.inventoryMgr.ActiveHotbarSlot, entitySelection.HitPosition, EnumInteractMode.Interact);
				game.SendPacketClient(ClientPackets.EntityInteraction(1, entitySelection.Entity.EntityId, entitySelection.Face, entitySelection.HitPosition, entitySelection.SelectionBoxIndex));
			}
		}
	}

	private void HandleMouseInteractionsBlockSelected(float dt)
	{
		BlockSelection blockSelection = game.BlockSelection;
		Block block = blockSelection.Block ?? game.WorldMap.RelaxedBlockAccess.GetBlock(blockSelection.Position);
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if ((float)(game.InWorldEllapsedMs - lastbuildMilliseconds) / 1000f >= BuildRepeatDelay(game))
		{
			if (game.InWorldMouseState.Left || game.InWorldMouseState.Right || game.InWorldMouseState.Middle)
			{
				lastbuildMilliseconds = game.InWorldEllapsedMs;
			}
			else
			{
				lastbuildMilliseconds = 0L;
				ResetMouseInteractions();
			}
			if (game.InWorldMouseState.Left)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, on: true, ref handling);
				if (handling != EnumHandling.PassThrough)
				{
					return;
				}
				EnumHandHandling handling2 = EnumHandHandling.NotHandled;
				TryBeginUseActiveSlotItem(blockSelection, null, EnumHandInteract.HeldItemAttack, ref handling2);
				if (handling2 != EnumHandHandling.PreventDefaultAnimation && handling2 != EnumHandHandling.PreventDefault)
				{
					StartAttackAnimation();
				}
				if (handling2 == EnumHandHandling.PreventDefaultAction || handling2 == EnumHandHandling.PreventDefault)
				{
					isSurvivalBreaking = false;
					survivalBreakingCounter = 0;
				}
				else if (game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
				{
					game.damagedBlocks.TryGetValue(blockSelection.Position, out var value);
					if (value == null)
					{
						value = new BlockDamage
						{
							Block = block,
							Facing = blockSelection.Face,
							Position = blockSelection.Position,
							ByPlayer = game.player
						};
					}
					game.damagedBlocks.Remove(blockSelection.Position);
					game.eventManager?.TriggerBlockBroken(value);
					game.OnPlayerTryDestroyBlock(blockSelection);
					UpdateCurrentSelection();
					game.PlaySound(block.GetSounds(game.BlockAccessor, blockSelection)?.GetBreakSound(game.player), randomizePitch: true);
				}
				else
				{
					InitBlockBreakSurvival(blockSelection, dt);
				}
			}
			if (game.InWorldMouseState.Right)
			{
				EnumHandling handling3 = EnumHandling.PassThrough;
				game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, on: true, ref handling3);
				if (handling3 != EnumHandling.PassThrough)
				{
					return;
				}
				bool flag = activeHotbarSlot.Itemstack != null;
				bool flag2 = flag && activeHotbarSlot.Itemstack.Class == EnumItemClass.Block && (game.player.worlddata.CurrentGameMode == EnumGameMode.Survival || game.player.worlddata.CurrentGameMode == EnumGameMode.Creative);
				bool flag3 = game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator;
				if ((flag3 && !game.Player.Entity.Controls.ShiftKey && TryBeginUseBlock(block, blockSelection)) || (flag && (!game.Player.Entity.Controls.ShiftKey || activeHotbarSlot.Itemstack.Collectible.HeldPriorityInteract) && TryBeginUseActiveSlotItem(blockSelection, null)))
				{
					return;
				}
				string failureCode = null;
				if ((flag3 && game.Player.Entity.Controls.ShiftKey && block.PlacedPriorityInteract && TryBeginUseBlock(block, blockSelection)) || (flag2 && OnBlockBuild(blockSelection, block, ref failureCode)) || (flag && game.Player.Entity.Controls.ShiftKey && TryBeginUseActiveSlotItem(blockSelection, null)) || (flag3 && game.Player.Entity.Controls.ShiftKey && TryBeginUseBlock(block, blockSelection)))
				{
					return;
				}
				if (failureCode != null && failureCode != "__ignore__")
				{
					game.eventManager?.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
				}
			}
			if (game.PickBlock)
			{
				OnBlockPick(blockSelection.Position, block);
			}
		}
		long elapsedMilliseconds = game.ElapsedMilliseconds;
		if (isSurvivalBreaking && game.InWorldMouseState.Left && game.player.worlddata.CurrentGameMode == EnumGameMode.Survival && elapsedMilliseconds - lastbreakMilliseconds >= 40)
		{
			ContinueBreakSurvival(blockSelection, block, dt);
			lastbreakMilliseconds = elapsedMilliseconds;
			if (elapsedMilliseconds - lastbreakNotifyMilliseconds > 80)
			{
				lastbreakNotifyMilliseconds = elapsedMilliseconds;
			}
		}
	}

	private void StartAttackAnimation()
	{
		game.HandSetAttackDestroy = true;
	}

	private void OnBlockPick(BlockPos pos, Block block)
	{
		ClientPlayerInventoryManager inventoryMgr = game.player.inventoryMgr;
		IInventory hotbarInventory = inventoryMgr.GetHotbarInventory();
		if (hotbarInventory == null)
		{
			return;
		}
		ItemStack blockStack = block.OnPickBlock(game, pos);
		int num = -1;
		for (int i = 0; i < hotbarInventory.Count; i++)
		{
			if ((hotbarInventory[i].StorageType & (EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Offhand)) == 0)
			{
				IItemStack itemstack = hotbarInventory[i].Itemstack;
				if (num == -1 && hotbarInventory[i].Empty && hotbarInventory[i].CanTakeFrom(new DummySlot(blockStack)))
				{
					num = i;
				}
				if (itemstack != null && itemstack.Equals(game, blockStack, GlobalConstants.IgnoredStackAttributes))
				{
					inventoryMgr.ActiveHotbarSlotNumber = i;
					return;
				}
			}
		}
		bool flag = game.player.worlddata.CurrentGameMode == EnumGameMode.Creative;
		ItemSlot flipSlot = null;
		if (flag)
		{
			flipSlot = new DummySlot(blockStack);
		}
		else
		{
			game.player.Entity.WalkInventory(delegate(ItemSlot slot)
			{
				if (!(slot.Inventory is InventoryPlayerBackPacks))
				{
					return true;
				}
				ItemStack itemstack2 = slot.Itemstack;
				if (itemstack2 != null && itemstack2.Equals(game, blockStack, GlobalConstants.IgnoredStackAttributes))
				{
					flipSlot = slot;
				}
				return flipSlot == null;
			});
			if (flipSlot == null)
			{
				return;
			}
		}
		ItemSlot itemSlot = inventoryMgr.ActiveHotbarSlot;
		if ((itemSlot.Itemstack != null || !itemSlot.CanTakeFrom(flipSlot)) && num != -1)
		{
			itemSlot = hotbarInventory[num];
			inventoryMgr.ActiveHotbarSlotNumber = num;
		}
		if (itemSlot.CanHold(flipSlot))
		{
			if (flag)
			{
				itemSlot.Itemstack = blockStack;
				itemSlot.MarkDirty();
				game.SendPacketClient(new Packet_Client
				{
					Id = 10,
					CreateItemstack = new Packet_CreateItemstack
					{
						Itemstack = StackConverter.ToPacket(blockStack),
						TargetInventoryId = itemSlot.Inventory.InventoryID,
						TargetSlot = inventoryMgr.ActiveHotbarSlotNumber,
						TargetLastChanged = ((InventoryBase)hotbarInventory).lastChangedSinceServerStart
					}
				});
			}
			else
			{
				game.SendPacketClient(hotbarInventory.TryFlipItems(inventoryMgr.ActiveHotbarSlotNumber, flipSlot) as Packet_Client);
			}
		}
	}

	private bool OnBlockBuild(BlockSelection blockSelection, Block onBlock, ref string failureCode)
	{
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		Block block = game.Blocks[activeHotbarSlot.Itemstack.Id];
		BlockPos blockPos = blockSelection.Position;
		if (onBlock == null || !onBlock.IsReplacableBy(block))
		{
			blockPos = blockPos.Offset(blockSelection.Face);
			blockSelection.DidOffset = true;
		}
		if (game.OnPlayerTryPlace(blockSelection, ref failureCode))
		{
			game.PlaySound(block.GetSounds(game.BlockAccessor, blockSelection)?.Place, randomizePitch: true);
			game.HandSetAttackBuild = true;
			return true;
		}
		if (blockSelection.DidOffset)
		{
			blockPos.Offset(blockSelection.Face.Opposite);
			blockSelection.DidOffset = false;
		}
		return false;
	}

	private void loadOrCreateBlockDamage(BlockSelection blockSelection, Block block)
	{
		BlockDamage blockDamage = curBlockDmg;
		EnumTool? tool = game.player.inventoryMgr?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool;
		curBlockDmg = game.loadOrCreateBlockDamage(blockSelection, block, tool, game.player);
		if (blockDamage != null && !blockDamage.Position.Equals(blockSelection.Position))
		{
			curBlockDmg.LastBreakEllapsedMs = game.ElapsedMilliseconds;
		}
	}

	private void InitBlockBreakSurvival(BlockSelection blockSelection, float dt)
	{
		Block block = blockSelection.Block ?? game.BlockAccessor.GetBlock(blockSelection.Position);
		loadOrCreateBlockDamage(blockSelection, block);
		curBlockDmg.LastBreakEllapsedMs = game.ElapsedMilliseconds;
		curBlockDmg.BeginBreakEllapsedMs = game.ElapsedMilliseconds;
		isSurvivalBreaking = true;
	}

	private void StopBlockBreakSurvival()
	{
		curBlockDmg = null;
		isSurvivalBreaking = false;
		survivalBreakingCounter = 0;
	}

	private void ContinueBreakSurvival(BlockSelection blockSelection, Block block, float dt)
	{
		loadOrCreateBlockDamage(blockSelection, block);
		long elapsedMilliseconds = game.ElapsedMilliseconds;
		int num = (int)(elapsedMilliseconds - curBlockDmg.LastBreakEllapsedMs);
		long num2 = curBlockDmg.BeginBreakEllapsedMs + 225;
		if (elapsedMilliseconds >= num2 && curBlockDmg.LastBreakEllapsedMs < num2 && game.BlockAccessor.GetChunkAtBlockPos(blockSelection.Position) is WorldChunk worldChunk && game.tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
		{
			BlockPos position = blockSelection.Position;
			int num3 = 32;
			worldChunk.BreakDecor(game, position, blockSelection.Face);
			game.WorldMap.MarkChunkDirty(position.X / num3, position.Y / num3, position.Z / num3, priority: true);
			game.SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 2, 0));
		}
		curBlockDmg.RemainingResistance = block.OnGettingBroken(game.player, blockSelection, game.player.inventoryMgr.ActiveHotbarSlot, curBlockDmg.RemainingResistance, (float)num / 1000f, survivalBreakingCounter);
		survivalBreakingCounter++;
		curBlockDmg.Facing = blockSelection.Face;
		if (curBlockDmg.Position != blockSelection.Position || curBlockDmg.Block != block)
		{
			curBlockDmg.RemainingResistance = block.GetResistance(game.BlockAccessor, blockSelection.Position);
			curBlockDmg.Block = block;
			curBlockDmg.Position = blockSelection.Position;
		}
		if (curBlockDmg.RemainingResistance <= 0f)
		{
			game.eventManager?.TriggerBlockBroken(curBlockDmg);
			game.OnPlayerTryDestroyBlock(blockSelection);
			game.damagedBlocks.Remove(blockSelection.Position);
			UpdateCurrentSelection();
		}
		else
		{
			game.eventManager?.TriggerBlockBreaking(curBlockDmg);
		}
		curBlockDmg.LastBreakEllapsedMs = elapsedMilliseconds;
	}

	internal float BuildRepeatDelay(ClientMain game)
	{
		return 0.25f;
	}

	private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel)
	{
		EnumHandHandling handling = EnumHandHandling.NotHandled;
		bool num = TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemInteract, ref handling);
		if (num && (handling == EnumHandHandling.PreventDefaultAction || handling == EnumHandHandling.Handled))
		{
			game.HandSetAttackBuild = true;
		}
		return num;
	}

	private bool TryBeginAttackWithActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		return TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemAttack, ref handling);
	}

	private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, ref EnumHandHandling handling)
	{
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (((game.InWorldMouseState.Right && useType == EnumHandInteract.HeldItemInteract) || (game.InWorldMouseState.Left && useType == EnumHandInteract.HeldItemAttack)) && activeHotbarSlot != null && activeHotbarSlot.Itemstack != null)
		{
			EntityControls controls = game.EntityPlayer.Controls;
			bool firstEvent = (useType == EnumHandInteract.HeldItemInteract && !prevMouseRight) || (useType == EnumHandInteract.HeldItemAttack && !prevMouseLeft);
			activeHotbarSlot.Itemstack.Collectible.OnHeldUseStart(activeHotbarSlot, game.EntityPlayer, blockSel, entitySel, useType, firstEvent, ref handling);
			if (handling == EnumHandHandling.NotHandled)
			{
				controls.HandUse = EnumHandInteract.None;
			}
			else
			{
				controls.HandUse = useType;
			}
			if (handling != EnumHandHandling.NotHandled)
			{
				controls.UsingCount = 0;
				controls.UsingBeginMS = game.ElapsedMilliseconds;
				if (controls.LeftUsingHeldItemTransformBefore != null)
				{
					controls.LeftUsingHeldItemTransformBefore.Clear();
				}
				if (activeHotbarSlot.StackSize <= 0)
				{
					activeHotbarSlot.Itemstack = null;
					activeHotbarSlot.MarkDirty();
				}
				game.SendHandInteraction(2, blockSel, entitySel, useType, EnumHandInteractNw.StartHeldItemUse, firstEvent);
				return true;
			}
		}
		return false;
	}

	private bool TryBeginUseBlock(Block selectedBlock, BlockSelection blockSelection)
	{
		if (!game.tryAccess(blockSelection, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		if (selectedBlock.OnBlockInteractStart(game, game.player, blockSelection))
		{
			EntityControls controls = game.EntityPlayer.Controls;
			controls.HandUse = EnumHandInteract.BlockInteract;
			game.api.Network.SendPlayerPositionPacket();
			controls.UsingCount = 0;
			controls.UsingBeginMS = game.ElapsedMilliseconds;
			controls.HandUsingBlockSel = blockSelection.Clone();
			if (controls.LeftUsingHeldItemTransformBefore != null)
			{
				controls.LeftUsingHeldItemTransformBefore.Clear();
			}
			game.SendHandInteraction(2, blockSelection, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StartBlockUse, firstEvent: false);
			return true;
		}
		return false;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
