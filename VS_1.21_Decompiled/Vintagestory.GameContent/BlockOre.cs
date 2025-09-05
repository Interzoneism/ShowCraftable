using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockOre : Block
{
	public string Grade
	{
		get
		{
			string text = Variant["grade"];
			switch (text)
			{
			case "poor":
			case "medium":
			case "rich":
			case "bountiful":
				return text;
			default:
				return null;
			}
		}
	}

	public string MotherRock => Variant["rock"];

	public string OreName => Variant["type"];

	public string InfoText
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Grade != null)
			{
				stringBuilder.AppendLine(Lang.Get("ore-grade-" + Grade));
			}
			stringBuilder.AppendLine(Lang.Get("ore-in-rock", Lang.Get("ore-" + OreName), Lang.Get("rock-" + MotherRock)));
			return stringBuilder.ToString();
		}
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		dropQuantityMultiplier *= byPlayer?.Entity.Stats.GetBlended("oreDropRate") ?? 1f;
		EnumHandling handling = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnBlockBroken(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if (drops != null)
			{
				for (int j = 0; j < drops.Length; j++)
				{
					world.SpawnItemEntity(drops[j], pos);
				}
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		SpawnBlockBrokenParticles(pos);
		world.BlockAccessor.SetBlock(0, pos);
		if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			CollectibleObject collectibleObject = byPlayer?.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			if (OreName == "flint" && (collectibleObject == null || collectibleObject.ToolTier == 0))
			{
				world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("rock-" + MotherRock)).BlockId, pos);
			}
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(InfoText);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		return InfoText + "\n" + ((OreName == "flint") ? (Lang.Get("Break with bare hands to extract flint") + "\n") : "") + base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return;
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
		double num = ExplosionDropChance(world, pos, blastType);
		if (world.Rand.NextDouble() < num)
		{
			ItemStack[] drops = GetDrops(world, pos, null);
			if (drops == null)
			{
				return;
			}
			for (int j = 0; j < drops.Length; j++)
			{
				if (!SplitDropStacks)
				{
					continue;
				}
				for (int k = 0; k < drops[j].StackSize; k++)
				{
					ItemStack itemStack = drops[j].Clone();
					if (!itemStack.Collectible.Code.Path.Contains("crystal"))
					{
						itemStack.StackSize = 1;
						world.SpawnItemEntity(itemStack, pos);
					}
				}
			}
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
		}
	}
}
