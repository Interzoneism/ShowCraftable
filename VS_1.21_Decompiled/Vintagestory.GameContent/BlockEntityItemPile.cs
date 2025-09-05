using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public abstract class BlockEntityItemPile : BlockEntity, IBlockEntityItemPile
{
	public InventoryGeneric inventory;

	public object inventoryLock = new object();

	public bool RandomizeSoundPitch;

	public abstract AssetLocation SoundLocation { get; }

	public abstract string BlockCode { get; }

	public abstract int MaxStackSize { get; }

	public virtual int DefaultTakeQuantity => 1;

	public virtual int BulkTakeQuantity => 4;

	public int OwnStackSize => inventory[0]?.StackSize ?? 0;

	public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

	public BlockEntityItemPile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.LateInitialize(BlockCode + "-" + Pos.ToString(), api);
		inventory.ResolveBlocksOrItems();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (Api.World is IServerWorldAccessor)
		{
			ItemSlot itemSlot = inventory[0];
			while (itemSlot.StackSize > 0)
			{
				ItemStack itemstack = itemSlot.TakeOut(GameMath.Clamp(itemSlot.StackSize, 1, Math.Max(1, itemSlot.Itemstack.Collectible.MaxStackSize / 4)));
				Api.World.SpawnItemEntity(itemstack, Pos);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
		if (Api != null)
		{
			inventory.Api = Api;
			inventory.ResolveBlocksOrItems();
		}
		if (Api is ICoreClientAPI)
		{
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ITreeAttribute treeAttribute = new TreeAttribute();
		inventory.ToTreeAttributes(treeAttribute);
		tree["inventory"] = treeAttribute;
	}

	public virtual bool OnPlayerInteract(IPlayer byPlayer)
	{
		BlockPos blockPos = Pos.UpCopy();
		BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(blockPos);
		if (blockEntity is BlockEntityItemPile)
		{
			return ((BlockEntityItemPile)blockEntity).OnPlayerInteract(byPlayer);
		}
		bool shiftKey = byPlayer.Entity.Controls.ShiftKey;
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool flag = activeHotbarSlot.Itemstack != null && activeHotbarSlot.Itemstack.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes);
		if (shiftKey && !flag)
		{
			return false;
		}
		if (shiftKey && flag && OwnStackSize >= MaxStackSize)
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos);
			if (Api.World.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block))
			{
				if (Api.World is IServerWorldAccessor)
				{
					Api.World.BlockAccessor.SetBlock((ushort)block.Id, blockPos);
					if (Api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityItemPile blockEntityItemPile)
					{
						blockEntityItemPile.TryPutItem(byPlayer);
					}
				}
				return true;
			}
			return false;
		}
		lock (inventoryLock)
		{
			if (shiftKey)
			{
				return TryPutItem(byPlayer);
			}
			return TryTakeItem(byPlayer);
		}
	}

	public virtual bool TryPutItem(IPlayer player)
	{
		if (OwnStackSize >= MaxStackSize)
		{
			return false;
		}
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null)
		{
			return false;
		}
		ItemSlot itemSlot = inventory[0];
		if (itemSlot.Itemstack == null)
		{
			itemSlot.Itemstack = activeHotbarSlot.Itemstack.Clone();
			itemSlot.Itemstack.StackSize = 0;
			Api.World.PlaySoundAt(SoundLocation, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
		}
		if (itemSlot.Itemstack.Equals(Api.World, activeHotbarSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			bool ctrlKey = player.Entity.Controls.CtrlKey;
			int num = GameMath.Min(activeHotbarSlot.StackSize, ctrlKey ? BulkTakeQuantity : DefaultTakeQuantity, MaxStackSize - OwnStackSize);
			int stackSize = itemSlot.Itemstack.StackSize;
			itemSlot.Itemstack.StackSize += num;
			if (stackSize + num > 0)
			{
				float temperature = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack);
				float temperature2 = activeHotbarSlot.Itemstack.Collectible.GetTemperature(Api.World, activeHotbarSlot.Itemstack);
				itemSlot.Itemstack.Collectible.SetTemperature(Api.World, itemSlot.Itemstack, (temperature * (float)stackSize + temperature2 * (float)num) / (float)(stackSize + num), delayCooldown: false);
			}
			Api.World.Logger.Audit("{0} Put {1}x{2} into {3} at {4}.", player.PlayerName, num, activeHotbarSlot.Itemstack.Collectible.Code, base.Block.Code, Pos);
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				activeHotbarSlot.TakeOut(num);
				activeHotbarSlot.OnItemSlotModified(null);
			}
			Api.World.PlaySoundAt(SoundLocation, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
			MarkDirty();
			Cuboidf[] collisionBoxes = Api.World.BlockAccessor.GetBlock(Pos).GetCollisionBoxes(Api.World.BlockAccessor, Pos);
			if (collisionBoxes != null && collisionBoxes.Length != 0 && CollisionTester.AabbIntersect(collisionBoxes[0], Pos.X, Pos.Y, Pos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
			{
				player.Entity.SidedPos.Y += (double)collisionBoxes[0].Y2 - (player.Entity.SidedPos.Y - (double)(int)player.Entity.SidedPos.Y);
			}
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		return false;
	}

	public bool TryTakeItem(IPlayer player)
	{
		int num = GameMath.Min(player.Entity.Controls.CtrlKey ? BulkTakeQuantity : DefaultTakeQuantity, OwnStackSize);
		if (inventory[0]?.Itemstack != null)
		{
			ItemStack itemStack = inventory[0].TakeOut(num);
			player.InventoryManager.TryGiveItemstack(itemStack);
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from {3} at {4}.", player.PlayerName, num, itemStack.Collectible.Code, base.Block.Code, Pos);
		}
		if (OwnStackSize == 0)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		Api.World.PlaySoundAt(SoundLocation, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
		MarkDirty();
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack itemstack = inventory[0].Itemstack;
		if (itemstack != null)
		{
			dsc.AppendLine(itemstack.StackSize + "x " + itemstack.GetName());
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack obj = inventory?[0]?.Itemstack;
		if (obj != null && !obj.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			inventory[0].Itemstack = null;
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		(inventory?[0]?.Itemstack)?.Collectible.OnStoreCollectibleMappings(Api.World, inventory[0], blockIdMapping, itemIdMapping);
	}
}
