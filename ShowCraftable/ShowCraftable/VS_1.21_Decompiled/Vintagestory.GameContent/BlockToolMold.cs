using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockToolMold : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		string text = (text = Attributes["createdByText"]?.AsString());
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["drop"].Exists && text != null)
		{
			JsonItemStack jsonItemStack = Attributes["drop"].AsObject<JsonItemStack>();
			if (jsonItemStack != null)
			{
				MetalProperty metalProperty = coreClientAPI.Assets.TryGet("worldproperties/block/metal.json").ToObject<MetalProperty>();
				for (int i = 0; i < metalProperty.Variants.Length; i++)
				{
					string path = metalProperty.Variants[i].Code.Path;
					JsonItemStack jsonItemStack2 = jsonItemStack.Clone();
					jsonItemStack2.Code.Path = jsonItemStack2.Code.Path.Replace("{metal}", path);
					CollectibleObject collectibleObject = ((jsonItemStack2.Type != EnumItemClass.Block) ? ((CollectibleObject)coreClientAPI.World.GetItem(jsonItemStack2.Code)) : ((CollectibleObject)coreClientAPI.World.GetBlock(jsonItemStack2.Code)));
					if (collectibleObject == null)
					{
						continue;
					}
					JsonObject attributes2 = collectibleObject.Attributes;
					if (attributes2 == null || !attributes2["handbook"].Exists)
					{
						if (collectibleObject.Attributes == null)
						{
							collectibleObject.Attributes = new JsonObject(JToken.Parse("{ handbook: {} }"));
						}
						else
						{
							collectibleObject.Attributes.Token[(object)"handbook"] = JToken.Parse("{ }");
						}
					}
					collectibleObject.Attributes["handbook"].Token[(object)"createdBy"] = JToken.FromObject((object)text);
				}
			}
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, Variant["tooltype"] + "moldBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (collectible is BlockSmeltedContainer)
				{
					list.Add(new ItemStack(collectible));
				}
			}
			foreach (CollectibleObject collectible2 in api.World.Collectibles)
			{
				EnumTool? tool = collectible2.Tool;
				if (tool.HasValue && tool == EnumTool.Chisel)
				{
					list2.Add(new ItemStack(collectible2));
				}
			}
			return new WorldInteraction[5]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { IsFull: false, Shattered: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-takeworkitem",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { IsFull: not false, IsHardened: not false, Shattered: false } blockEntityToolMold && !blockEntityToolMold.BreaksWhenFilled
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-breakmoldforitem",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { IsFull: not false, IsHardened: not false, Shattered: false } blockEntityToolMold && blockEntityToolMold.BreaksWhenFilled
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-chiselmoldforbits",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { FillLevel: >0, IsHardened: not false, Shattered: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { MetalContent: null } blockEntityToolMold && !blockEntityToolMold.Shattered
				}
			};
		});
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (world.Rand.NextDouble() < 0.05)
		{
			BlockEntityToolMold blockEntity = GetBlockEntity<BlockEntityToolMold>(pos);
			if (blockEntity != null && blockEntity.Temperature > 300f)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Block,
					SourceBlock = this,
					Type = EnumDamageType.Fire,
					SourcePos = pos.ToVec3d()
				}, 0.5f);
			}
		}
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityToolMold blockEntity = GetBlockEntity<BlockEntityToolMold>(pos);
			if (blockEntity != null && blockEntity.Temperature > 300f)
			{
				return 10000f;
			}
		}
		return 0f;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		Cuboidf[] colSelRotatedBoxes = getColSelRotatedBoxes(SelectionBoxes, (world.GetBlockEntity(pos) as BlockEntityToolMold)?.MeshAngle ?? 0f);
		if (RandomDrawOffset != 0 && colSelRotatedBoxes != null && colSelRotatedBoxes.Length >= 1)
		{
			float x = (float)(GameMath.oaatHash(pos.X, 0, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
			float z = (float)(GameMath.oaatHash(pos.X, 1, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
			return new Cuboidf[1] { colSelRotatedBoxes[0].OffsetCopy(x, 0f, z) };
		}
		if (colSelRotatedBoxes == null || colSelRotatedBoxes.Length != 1)
		{
			return colSelRotatedBoxes;
		}
		IWorldChunk chunkAtBlockPos = world.GetChunkAtBlockPos(pos);
		if (chunkAtBlockPos == null)
		{
			return colSelRotatedBoxes;
		}
		return chunkAtBlockPos.AdjustSelectionBoxForDecor(world, pos, colSelRotatedBoxes);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return getColSelRotatedBoxes(CollisionBoxes, (blockAccessor.GetBlockEntity(pos) as BlockEntityToolMold)?.MeshAngle ?? 0f);
	}

	private Cuboidf[] getColSelRotatedBoxes(Cuboidf[] boxes, float meshAngle)
	{
		if (meshAngle == 0f)
		{
			return boxes;
		}
		Cuboidf[] array = new Cuboidf[boxes.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = boxes[i].RotatedCopy(0f, meshAngle * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
		}
		return array;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
		else if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite)) is BlockEntityToolMold blockEntityToolMold)
		{
			IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
			if (player != null && blockEntityToolMold.OnPlayerInteract(player, blockSel.Face, blockSel.HitPosition))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel?.Position) is BlockEntityToolMold blockEntityToolMold)
		{
			return blockEntityToolMold.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
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

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityToolMold blockEntityToolMold)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float meshAngle = (float)(int)Math.Round((float)Math.Atan2(y, x) / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
			blockEntityToolMold.MeshAngle = meshAngle;
			blockEntityToolMold.MarkDirty();
		}
		return num;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityToolMold { FillLevel: >0 } blockEntityToolMold && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			IPlayerInventoryManager playerInventoryManager = byPlayer?.InventoryManager;
			if (playerInventoryManager != null)
			{
				EnumTool? offhandTool = playerInventoryManager.OffhandTool;
				if (offhandTool.HasValue && offhandTool == EnumTool.Hammer)
				{
					offhandTool = playerInventoryManager.ActiveTool;
					if (offhandTool.HasValue && offhandTool == EnumTool.Chisel)
					{
						ItemStack chiseledStack = blockEntityToolMold.GetChiseledStack();
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
							blockEntityToolMold.MetalContent = null;
							blockEntityToolMold.FillLevel = 0;
							DamageItem(world, byPlayer.Entity, playerInventoryManager.ActiveHotbarSlot);
							DamageItem(world, byPlayer.Entity, byPlayer.Entity?.LeftHandItemSlot);
							return;
						}
					}
				}
			}
			if (blockEntityToolMold.BreaksWhenFilled)
			{
				world.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), pos, -0.4);
				SpawnBlockBrokenParticles(pos);
			}
		}
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		List<ItemStack> list = new List<ItemStack>();
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityToolMold blockEntityToolMold)
		{
			ItemStack[] stateAwareMold = blockEntityToolMold.GetStateAwareMold();
			if (stateAwareMold != null)
			{
				list.AddRange(stateAwareMold);
			}
			ItemStack[] stateAwareMoldedStacks = blockEntityToolMold.GetStateAwareMoldedStacks();
			if (stateAwareMoldedStacks != null)
			{
				list.AddRange(stateAwareMoldedStacks);
			}
		}
		else
		{
			list.Add(new ItemStack(this));
		}
		return list.ToArray();
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityToolMold { Shattered: not false })
		{
			return Lang.Get("ceramicblock-blockname-shattered", base.GetPlacedBlockName(world, pos));
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
