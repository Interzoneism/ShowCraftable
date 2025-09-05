using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSpawner : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntitySpawner blockEntitySpawner && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			blockEntitySpawner.OnInteract(byPlayer);
			return true;
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BESpawnerData bESpawnerData = (world.BlockAccessor.GetBlockEntity(pos) as BlockEntitySpawner)?.Data;
		if (bESpawnerData != null)
		{
			itemStack.Attributes.SetBytes("spawnerData", SerializerUtil.Serialize(bESpawnerData));
		}
		return itemStack;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		byte[] bytes = inSlot.Itemstack.Attributes.GetBytes("spawnerData");
		if (bytes != null)
		{
			try
			{
				BESpawnerData bESpawnerData = SerializerUtil.Deserialize<BESpawnerData>(bytes);
				if (bESpawnerData.EntityCodes == null)
				{
					dsc.AppendLine("Spawns: Nothing");
				}
				else
				{
					string text = "";
					string[] entityCodes = bESpawnerData.EntityCodes;
					foreach (string text2 in entityCodes)
					{
						if (text.Length > 0)
						{
							text += ", ";
						}
						text += Lang.Get("item-creature-" + text2);
					}
					dsc.AppendLine("Spawns: " + text);
				}
				dsc.AppendLine("Area: " + bESpawnerData.SpawnArea);
				dsc.AppendLine("Interval: " + bESpawnerData.InGameHourInterval);
				dsc.AppendLine("Max count: " + bESpawnerData.MaxCount);
			}
			catch
			{
			}
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}
}
