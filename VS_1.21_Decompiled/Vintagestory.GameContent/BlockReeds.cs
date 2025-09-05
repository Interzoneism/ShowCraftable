using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockReeds : BlockPlant
{
	private WorldInteraction[] interactions;

	private string climateColorMapInt;

	private string seasonColorMapInt;

	private int maxWaterDepth;

	private string habitatBlockCode;

	public override string ClimateColorMapForMap => climateColorMapInt;

	public override string SeasonColorMapForMap => seasonColorMapInt;

	public override string RemapToLiquidsLayer => habitatBlockCode;

	public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		base.OnCollectTextures(api, textureDict);
		climateColorMapInt = Attributes["climateColorMapForMap"].AsString();
		seasonColorMapInt = Attributes["seasonColorMapForMap"].AsString();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		maxWaterDepth = Attributes["maxWaterDepth"].AsInt(1);
		string text = Variant["habitat"];
		if (text == "water")
		{
			habitatBlockCode = "water-still-7";
		}
		else if (text == "ice")
		{
			habitatBlockCode = "lakeice";
		}
		if (LastCodePart() == "harvested")
		{
			return;
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "reedsBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item item in api.World.Items)
			{
				if (!(item.Code == null) && item.Tool == EnumTool.Knife)
				{
					list.Add(new ItemStack(item));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-reeds-harvest",
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (CanPlantStay(world.BlockAccessor, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(BlockId, blockSel.Position);
			return true;
		}
		failureCode = "requirefertileground";
		return false;
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		float value;
		if (Variant["state"] == "harvested")
		{
			dt /= 2f;
		}
		else if (player.InventoryManager.ActiveTool != EnumTool.Knife)
		{
			dt /= 3f;
		}
		else if (itemslot.Itemstack.Collectible.MiningSpeed.TryGetValue(EnumBlockMaterial.Plant, out value))
		{
			dt *= value;
		}
		float num = ((RequiredMiningTier == 0) ? (remainingResistance - dt) : remainingResistance);
		if (counter % 5 == 0 || num <= 0f)
		{
			double posx = (double)blockSel.Position.X + blockSel.HitPosition.X;
			double posy = (double)blockSel.Position.InternalY + blockSel.HitPosition.Y;
			double posz = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
			player.Entity.World.PlaySoundAt((num > 0f) ? Sounds.GetHitSound(player) : Sounds.GetBreakSound(player), posx, posy, posz, player, randomizePitch: true, 16f);
		}
		return num;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		AssetLocation blockCode = CodeWithVariants(new string[2] { "habitat", "cover" }, new string[2] { "land", "free" });
		return new ItemStack(world.GetBlock(blockCode));
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		Block block = api.World.GetBlock(CodeWithVariant("state", "harvested"));
		return api.World.GetBlock(CodeWithVariant("state", "normal")).Drops.Append(block.Drops);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			BlockDropItemStack[] drops = Drops;
			for (int i = 0; i < drops.Length; i++)
			{
				ItemStack nextItemStack = drops[i].GetNextItemStack();
				if (nextItemStack != null)
				{
					world.SpawnItemEntity(nextItemStack, pos);
				}
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		if (byPlayer != null && Variant["state"] == "normal" && (byPlayer.InventoryManager.ActiveTool == EnumTool.Knife || byPlayer.InventoryManager.ActiveTool == EnumTool.Sickle || byPlayer.InventoryManager.ActiveTool == EnumTool.Scythe))
		{
			world.BlockAccessor.SetBlock(world.GetBlock(CodeWithVariants(new string[2] { "habitat", "state" }, new string[2] { "land", "harvested" })).BlockId, pos);
		}
		else
		{
			SpawnBlockBrokenParticles(pos);
			world.BlockAccessor.SetBlock(0, pos);
		}
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		bool flag = true;
		BlockPos blockPos = pos.Copy();
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				blockPos.Set(pos.X + i, pos.Y, pos.Z + j);
				if (blockAccessor.GetBlock(blockPos, 1) is BlockWaterLilyGiant)
				{
					flag = false;
				}
			}
		}
		if (blockAccessor.GetBlock(pos, 1) is BlockPlant)
		{
			flag = false;
		}
		if (!flag)
		{
			return false;
		}
		int num = 0;
		Block blockBelow = blockAccessor.GetBlockBelow(pos);
		while (blockBelow.LiquidCode == "water")
		{
			if (++num > maxWaterDepth)
			{
				return false;
			}
			blockBelow = blockAccessor.GetBlockBelow(pos, num + 1);
		}
		if (blockBelow.Fertility > 0)
		{
			return TryGen(blockAccessor, pos.DownCopy(num));
		}
		return false;
	}

	private bool TryGen(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlock(CodeWithVariant("habitat", "land"));
		if (block == null)
		{
			return false;
		}
		blockAccessor.SetBlock(block.BlockId, pos);
		return true;
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		return capi.World.ApplyColorMapOnRgba(ClimateColorMapForMap, SeasonColorMapForMap, capi.BlockTextureAtlas.GetRandomColor(Textures.Last().Value.Baked.TextureSubId, rndIndex), pos.X, pos.Y, pos.Z);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
