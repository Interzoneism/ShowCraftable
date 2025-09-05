using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockGroundStorage : Block, ICombustible, IIgnitable
{
	private ItemStack[] groundStorablesQuadrants;

	private ItemStack[] groundStorablesHalves;

	public static bool IsUsingContainedBlock;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ItemStack[][] orCreate = ObjectCacheUtil.GetOrCreate(api, "groundStorablesQuadrands", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				CollectibleBehaviorGroundStorable behavior = collectible.GetBehavior<CollectibleBehaviorGroundStorable>();
				if (behavior != null && behavior.StorageProps.Layout == EnumGroundStorageLayout.Quadrants)
				{
					list.Add(new ItemStack(collectible));
				}
				if (behavior != null && behavior.StorageProps.Layout == EnumGroundStorageLayout.Halves)
				{
					list2.Add(new ItemStack(collectible));
				}
			}
			return new ItemStack[2][]
			{
				list.ToArray(),
				list2.ToArray()
			};
		});
		groundStorablesQuadrants = orCreate[0];
		groundStorablesHalves = orCreate[1];
		if (api.Side == EnumAppSide.Client)
		{
			(api as ICoreClientAPI).Event.MouseUp += Event_MouseUp;
		}
	}

	private void Event_MouseUp(MouseEvent e)
	{
		IsUsingContainedBlock = false;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.GetCollisionBoxes();
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.GetCollisionBoxes();
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityGroundStorage blockEntity = blockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
		if (blockEntity != null)
		{
			return blockEntity.GetSelectionBoxes();
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return blockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos)?.CanAttachBlockAt(blockFace, attachmentArea) ?? base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (api.Side == EnumAppSide.Client && IsUsingContainedBlock)
		{
			return false;
		}
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.OnPlayerInteractStart(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.OnPlayerInteractStep(secondsUsed, byPlayer, blockSel);
		}
		return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			blockEntityGroundStorage.OnPlayerInteractStop(secondsUsed, byPlayer, blockSel);
		}
		else
		{
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}
	}

	public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return base.GetBlockMaterial(blockAccessor, pos, stack);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (ItemSlot item in blockEntityGroundStorage.Inventory)
			{
				if (!item.Empty)
				{
					list.Add(item.Itemstack);
				}
			}
			return list.ToArray();
		}
		return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public float FillLevel(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return (int)Math.Ceiling((float)blockEntityGroundStorage.TotalStackSize / (float)blockEntityGroundStorage.Capacity);
		}
		return 1f;
	}

	public bool CreateStorage(IWorldAccessor world, BlockSelection blockSel, IPlayer player)
	{
		if (!world.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			player.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return false;
		}
		BlockPos blockPos = blockSel.Position;
		if (blockSel.Face != null)
		{
			blockPos = blockPos.AddCopy(blockSel.Face);
		}
		BlockPos pos = blockPos.DownCopy();
		if (!world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, BlockFacing.UP))
		{
			return false;
		}
		GroundStorageProperties groundStorageProperties = player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
		if (groundStorageProperties != null && groundStorageProperties.CtrlKey && !player.Entity.Controls.CtrlKey)
		{
			return false;
		}
		BlockPos blockPos2 = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		double y = player.Entity.Pos.X - ((double)blockPos2.X + blockSel.HitPosition.X);
		double x = (double)(float)player.Entity.Pos.Z - ((double)blockPos2.Z + blockSel.HitPosition.Z);
		float num = (float)Math.Atan2(y, x);
		float num2 = (float)Math.PI / 2f;
		float meshAngle = (float)(int)Math.Round(num / num2) * num2;
		BlockFacing blockFacing = null;
		if (groundStorageProperties.Layout == EnumGroundStorageLayout.WallHalves)
		{
			blockFacing = Block.SuggestedHVOrientation(player, blockSel)[0];
			BlockPos pos2 = blockPos.AddCopy(blockFacing).Up(groundStorageProperties.WallOffY - 1);
			if (!world.BlockAccessor.GetBlock(pos2).CanAttachBlockAt(world.BlockAccessor, this, pos2, blockFacing.Opposite))
			{
				blockFacing = null;
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing blockFacing2 in hORIZONTALS)
				{
					pos2 = blockPos.AddCopy(blockFacing2).Up(groundStorageProperties.WallOffY - 1);
					if (world.BlockAccessor.GetBlock(pos2).CanAttachBlockAt(world.BlockAccessor, this, pos2, blockFacing2.Opposite))
					{
						blockFacing = blockFacing2;
						break;
					}
				}
			}
			if (blockFacing == null)
			{
				if (groundStorageProperties.WallOffY > 1)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "requireswall", Lang.Get("placefailure-requirestallwall", groundStorageProperties.WallOffY));
				}
				else
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "requireswall", Lang.Get("placefailure-requireswall"));
				}
				return false;
			}
			meshAngle = (float)Math.Atan2(blockFacing.Normali.X, blockFacing.Normali.Z);
		}
		world.BlockAccessor.SetBlock(BlockId, blockPos);
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			blockEntityGroundStorage.MeshAngle = meshAngle;
			blockEntityGroundStorage.AttachFace = blockFacing;
			blockEntityGroundStorage.clientsideFirstPlacement = world.Side == EnumAppSide.Client;
			blockEntityGroundStorage.OnPlayerInteractStart(player, blockSel);
		}
		if (CollisionTester.AabbIntersect(GetCollisionBoxes(world.BlockAccessor, blockPos)[0], blockPos.X, blockPos.Y, blockPos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
		{
			player.Entity.SidedPos.Y += GetCollisionBoxes(world.BlockAccessor, blockPos)[0].Y2;
		}
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BlockEntityGroundStorage blockEntityGroundStorage = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityGroundStorage;
		if (blockEntityGroundStorage?.StorageProps != null && blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.WallHalves)
		{
			BlockFacing attachFace = blockEntityGroundStorage.AttachFace;
			BlockPos pos2 = pos.AddCopy(attachFace.Normali.X, blockEntityGroundStorage.StorageProps.WallOffY - 1, attachFace.Normali.Z);
			if (!world.BlockAccessor.GetBlock(pos2).CanAttachBlockAt(world.BlockAccessor, this, pos2, attachFace.Opposite))
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
			if (!world.BlockAccessor.GetBlock(pos.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP))
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
			return;
		}
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage { IsBurning: not false } blockEntityGroundStorage2)
		{
			if (!world.BlockAccessor.GetBlock(pos.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP))
			{
				world.BlockAccessor.BreakBlock(pos, null);
				return;
			}
			Block block = world.BlockAccessor.GetBlock(neibpos);
			Block block2 = world.BlockAccessor.GetBlock(neibpos, 2);
			JsonObject attributes = block.Attributes;
			if (attributes == null || !attributes.IsTrue("smothersFire"))
			{
				JsonObject attributes2 = block2.Attributes;
				if (attributes2 == null || !attributes2.IsTrue("smothersFire"))
				{
					goto IL_01a1;
				}
			}
			blockEntityGroundStorage2?.Extinguish();
		}
		goto IL_01a1;
		IL_01a1:
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			ItemSlot itemSlot = blockEntityGroundStorage.Inventory.ToArray().Shuffle(capi.World.Rand).FirstOrDefault((ItemSlot s) => !s.Empty);
			if (itemSlot != null)
			{
				return itemSlot.Itemstack.Collectible.GetRandomColor(capi, itemSlot.Itemstack);
			}
		}
		return base.GetColorWithoutTint(capi, pos);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			ItemSlot itemSlot = blockEntityGroundStorage.Inventory.ToArray().Shuffle(capi.World.Rand).FirstOrDefault((ItemSlot s) => !s.Empty);
			return itemSlot?.Itemstack.Collectible.GetRandomColor(capi, itemSlot.Itemstack) ?? 0;
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		return base.GetRandomColor(capi, stack);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.GetBlockName();
		}
		return OnPickBlock(world, pos)?.GetName();
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			return blockEntityGroundStorage.Inventory.FirstNonEmptySlot?.Itemstack.Clone();
		}
		return null;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BlockEntityGroundStorage blockEntityGroundStorage = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityGroundStorage;
		if (blockEntityGroundStorage?.StorageProps != null)
		{
			WorldInteraction[] value = (blockEntityGroundStorage.Inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty && slot.Itemstack.Collectible is BlockLiquidContainerBase)?.Itemstack.Collectible as BlockLiquidContainerBase)?.interactions ?? Array.Empty<WorldInteraction>();
			int bulkTransferQuantity = blockEntityGroundStorage.StorageProps.BulkTransferQuantity;
			if (blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.Stacking && !blockEntityGroundStorage.Inventory.Empty)
			{
				ItemStack[] itemstacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true).ToArray();
				CollectibleObject collectibleObject = blockEntityGroundStorage.Inventory[0].Itemstack?.Collectible;
				if (collectibleObject == null)
				{
					return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(value);
				}
				return new WorldInteraction[5]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-firepit-ignite",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = itemstacks,
						GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityGroundStorage { IsBurning: false } blockEntityGroundStorage2 && blockEntityGroundStorage2 != null && blockEntityGroundStorage2.CanIgnite) ? wi.Itemstacks : null
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-addone",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = new ItemStack[1]
						{
							new ItemStack(collectibleObject)
						}
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-removeone",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = null
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-addbulk",
						MouseButton = EnumMouseButton.Right,
						HotKeyCodes = new string[2] { "ctrl", "shift" },
						Itemstacks = new ItemStack[1]
						{
							new ItemStack(collectibleObject, bulkTransferQuantity)
						}
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-removebulk",
						HotKeyCode = "ctrl",
						MouseButton = EnumMouseButton.Right
					}
				}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)).Append(value);
			}
			if (blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.SingleCenter)
			{
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-behavior-rightclickpickup",
						MouseButton = EnumMouseButton.Right
					}
				}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)).Append(value);
			}
			if (blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.Halves || blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.Quadrants)
			{
				return new WorldInteraction[2]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-add",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = ((blockEntityGroundStorage.StorageProps.Layout == EnumGroundStorageLayout.Halves) ? groundStorablesHalves : groundStorablesQuadrants)
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-groundstorage-remove",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = null
					}
				}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)).Append(value);
			}
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public float GetBurnDuration(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			ItemStack itemStack = blockEntityGroundStorage.Inventory.FirstNonEmptySlot?.Itemstack;
			if (itemStack?.Collectible?.CombustibleProps == null)
			{
				return 0f;
			}
			float burnDuration = itemStack.Collectible.CombustibleProps.BurnDuration;
			if (burnDuration == 0f)
			{
				return 0f;
			}
			return GameMath.Clamp(burnDuration * (float)Math.Log(itemStack.StackSize), 1f, 120f);
		}
		return 0f;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		Dictionary<string, MultiTextureMeshRef> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "groundStorageUMC");
		if (dictionary == null)
		{
			return;
		}
		foreach (MultiTextureMeshRef value in dictionary.Values)
		{
			if (value != null && !value.Disposed)
			{
				value.Dispose();
			}
		}
		ObjectCacheUtil.Delete(api, "groundStorageUMC");
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage { CanIgnite: not false }))
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (secondsIgniting > 0.25f && (int)(30f * secondsIgniting) % 9 == 1)
		{
			Random rand = byEntity.World.Rand;
			Vec3d basePos = new Vec3d((double)((float)pos.X + 0.25f) + 0.5 * rand.NextDouble(), (float)pos.InternalY + 0.875f, (double)((float)pos.Z + 0.25f) + 0.5 * rand.NextDouble());
			Block block = byEntity.World.GetBlock(new AssetLocation("fire"));
			AdvancedParticleProperties advancedParticleProperties = block.ParticleProperties[block.ParticleProperties.Length - 1];
			advancedParticleProperties.basePos = basePos;
			advancedParticleProperties.Quantity.avg = 1f;
			IPlayer dualCallByPlayer = null;
			if (byEntity is EntityPlayer)
			{
				dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.SpawnParticles(advancedParticleProperties, dualCallByPlayer);
			advancedParticleProperties.Quantity.avg = 0f;
		}
		if (secondsIgniting >= 1.5f)
		{
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.Ignitable;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		if (!(secondsIgniting < 1.45f))
		{
			handling = EnumHandling.PreventDefault;
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null)
			{
				(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGroundStorage)?.TryIgnite();
			}
		}
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		if (!base.ShouldReceiveClientParticleTicks(world, player, pos, out isWindAffected))
		{
			return world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos)?.Inventory.Any((ItemSlot slot) => slot.Itemstack?.Collectible?.GetCollectibleInterface<IGroundStoredParticleEmitter>() != null) ?? false;
		}
		return true;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityGroundStorage { StorageProps: not null } blockEntityGroundStorage && !blockEntityGroundStorage.Inventory.Empty)
		{
			Vec3f[] array = new Vec3f[blockEntityGroundStorage.DisplayedItems];
			blockEntityGroundStorage.GetLayoutOffset(array);
			foreach (ItemSlot item in blockEntityGroundStorage.Inventory)
			{
				IGroundStoredParticleEmitter groundStoredParticleEmitter = item?.Itemstack?.Collectible.GetCollectibleInterface<IGroundStoredParticleEmitter>();
				if (groundStoredParticleEmitter == null)
				{
					continue;
				}
				int slotId = blockEntityGroundStorage.Inventory.GetSlotId(item);
				if (slotId >= 0 && slotId < array.Length)
				{
					Vec3f xYZ = new Matrixf().RotateY(blockEntityGroundStorage.MeshAngle).TransformVector(new Vec4f(array[slotId].X, array[slotId].Y, array[slotId].Z, 1f)).XYZ;
					if (groundStoredParticleEmitter.ShouldSpawnGSParticles(blockEntityGroundStorage.Api.World, item.Itemstack))
					{
						groundStoredParticleEmitter.DoSpawnGSParticles(manager, pos, xYZ);
					}
				}
			}
		}
		base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
	}
}
