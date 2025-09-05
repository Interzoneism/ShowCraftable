using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSkep : Block
{
	private float beemobSpawnChance = 0.4f;

	public bool IsEmpty()
	{
		return Variant["type"] == "empty";
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack? byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
		if (byItemStack != null && byItemStack.Attributes.GetBool("harvestable"))
		{
			GetBlockEntity<BlockEntityBeehive>(blockPos).Harvestable = true;
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		CollectibleObject collectibleObject = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible;
		if ((collectibleObject is ItemClosedBeenade || collectibleObject is ItemOpenedBeenade) ? true : false)
		{
			return false;
		}
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && collectibleObject?.FirstCodePart() == "honeycomb")
		{
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBeehive { Harvestable: false } blockEntityBeehive)
			{
				blockEntityBeehive.Harvestable = true;
				blockEntityBeehive.MarkDirty(redrawOnClient: true);
			}
			return true;
		}
		if (byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "east")))))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(new AssetLocation("sounds/block/planks"), blockSel.Position, -0.5, byPlayer, randomizePitch: false);
			return true;
		}
		return false;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		beemobSpawnChance = Attributes?["beemobSpawnChance"].AsFloat(0.4f) ?? 0.4f;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		if (world.Side == EnumAppSide.Server && !IsEmpty() && world.Rand.NextDouble() < (double)beemobSpawnChance)
		{
			Entity entity = world.ClassRegistry.CreateEntity(world.GetEntityType("beemob"));
			if (entity != null)
			{
				entity.ServerPos.X = (float)pos.X + 0.5f;
				entity.ServerPos.Y = (float)pos.Y + 0.5f;
				entity.ServerPos.Z = (float)pos.Z + 0.5f;
				entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "brokenbeehive");
				world.SpawnEntity(entity);
			}
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		ItemStack[] array = (handbookStack.Attributes.GetBool("harvestable") ? getHarvestableDrops(api.World, forPlayer.Entity.Pos.XYZ.AsBlockPos, forPlayer) : GetDrops(api.World, forPlayer.Entity.Pos.XYZ.AsBlockPos, forPlayer));
		if (array == null)
		{
			return Array.Empty<BlockDropItemStack>();
		}
		return array.Select((ItemStack stack) => new BlockDropItemStack(stack)).ToArray();
	}

	public override ItemStack[]? GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!IsEmpty())
		{
			BlockEntityBeehive blockEntity = GetBlockEntity<BlockEntityBeehive>(pos);
			if (blockEntity != null && blockEntity.Harvestable)
			{
				return getHarvestableDrops(world, pos, byPlayer, dropQuantityMultiplier);
			}
		}
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "east")))
		};
	}

	private ItemStack[]? getHarvestableDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (Drops == null)
		{
			return null;
		}
		List<ItemStack> list = new List<ItemStack>();
		for (int i = 0; i < Drops.Length; i++)
		{
			if (Drops[i].Tool.HasValue && (byPlayer == null || Drops[i].Tool != byPlayer.InventoryManager.ActiveTool))
			{
				continue;
			}
			ItemStack nextItemStack = Drops[i].GetNextItemStack(dropQuantityMultiplier);
			if (nextItemStack != null)
			{
				list.Add(nextItemStack);
				if (Drops[i].LastDrop)
				{
					break;
				}
			}
		}
		return list.ToArray();
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack.Attributes.GetBool("harvestable"))
		{
			return Lang.GetMatching(Code.Domain + ":block-" + CodeWithVariant("type", "harvestable").Path);
		}
		return base.GetHeldItemName(itemStack);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BlockEntityBeehive blockEntity = GetBlockEntity<BlockEntityBeehive>(pos);
		if (blockEntity != null && blockEntity.Harvestable)
		{
			itemStack.Attributes.SetBool("harvestable", value: true);
		}
		return itemStack;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		WorldInteraction[] array = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = ((Variant["type"] == "populated") ? "blockhelp-skep-putinbagslot" : "blockhelp-skep-pickup"),
				MouseButton = EnumMouseButton.Right
			}
		};
		BlockEntityBeehive blockEntity = GetBlockEntity<BlockEntityBeehive>(selection.Position);
		if (blockEntity != null && blockEntity.Harvestable)
		{
			array.Append(new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-skep-harvest",
					MouseButton = EnumMouseButton.Left
				}
			});
		}
		return array.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		string text = Variant["side"];
		string key = "beehive-" + Variant["material"] + "-harvestablemesh-" + text;
		MeshData modeldata;
		if (!api.ObjectCache.ContainsKey(key))
		{
			Block block = capi.World.GetBlock(CodeWithVariant("type", "populated"));
			capi.Tesselator.TesselateShape(block, Vintagestory.API.Common.Shape.TryGet(api, "shapes/block/beehive/skep-harvestable.json"), out modeldata, new Vec3f(0f, BlockFacing.FromCode(text).HorizontalAngleIndex * 90 - 90, 0f));
			api.ObjectCache[key] = modeldata;
		}
		else
		{
			modeldata = (MeshData)api.ObjectCache[key];
		}
		if (itemstack.Attributes.GetBool("harvestable"))
		{
			renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(modeldata);
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}
}
