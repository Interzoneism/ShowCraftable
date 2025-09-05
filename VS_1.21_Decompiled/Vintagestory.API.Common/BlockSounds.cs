using System.Collections.Generic;
using System.Runtime.Serialization;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class BlockSounds
{
	[DocumentAsJson]
	public AssetLocation Ambient;

	[DocumentAsJson]
	public EnumSoundType AmbientSoundType = EnumSoundType.Ambient;

	[DocumentAsJson]
	public float AmbientMaxDistanceMerge = 3f;

	[DocumentAsJson]
	public float AmbientBlockCount = 10f;

	[DocumentAsJson]
	public virtual AssetLocation Walk { get; set; }

	[DocumentAsJson]
	public virtual AssetLocation Inside { get; set; }

	[DocumentAsJson]
	public virtual AssetLocation Break { get; set; }

	[DocumentAsJson]
	public virtual AssetLocation Place { get; set; }

	[DocumentAsJson]
	public virtual AssetLocation Hit { get; set; }

	[DocumentAsJson]
	public virtual Dictionary<EnumTool, BlockSounds> ByTool { get; set; }

	public BlockSounds Clone()
	{
		BlockSounds blockSounds = new BlockSounds
		{
			Walk = Walk?.PermanentClone(),
			Inside = Inside?.PermanentClone(),
			Break = Break?.PermanentClone(),
			Place = Place?.PermanentClone(),
			Hit = Hit?.PermanentClone(),
			Ambient = Ambient?.PermanentClone(),
			AmbientBlockCount = AmbientBlockCount,
			AmbientSoundType = AmbientSoundType,
			AmbientMaxDistanceMerge = AmbientMaxDistanceMerge
		};
		if (ByTool != null)
		{
			blockSounds.ByTool = new Dictionary<EnumTool, BlockSounds>(ByTool.Count);
			foreach (KeyValuePair<EnumTool, BlockSounds> item in ByTool)
			{
				blockSounds.ByTool[item.Key] = item.Value.Clone();
			}
		}
		return blockSounds;
	}

	public AssetLocation GetBreakSound(IPlayer byPlayer)
	{
		EnumTool? enumTool = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool;
		if (enumTool.HasValue)
		{
			return GetBreakSound(enumTool.Value);
		}
		return Break;
	}

	public AssetLocation GetHitSound(IPlayer byPlayer)
	{
		EnumTool? enumTool = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool;
		if (enumTool.HasValue)
		{
			return GetHitSound(enumTool.Value);
		}
		return Hit;
	}

	public AssetLocation GetBreakSound(EnumTool tool)
	{
		if (ByTool != null)
		{
			ByTool.TryGetValue(tool, out var value);
			if (value?.Break != null)
			{
				return value.Break;
			}
		}
		return Break;
	}

	public AssetLocation GetHitSound(EnumTool tool)
	{
		if (ByTool != null)
		{
			ByTool.TryGetValue(tool, out var value);
			if (value?.Hit != null)
			{
				return value.Hit;
			}
		}
		return Hit;
	}

	[OnDeserialized]
	public void OnDeserializedMethod(StreamingContext context)
	{
		Walk?.WithPathPrefixOnce("sounds/");
		Inside?.WithPathPrefixOnce("sounds/");
		Break?.WithPathPrefixOnce("sounds/");
		Place?.WithPathPrefixOnce("sounds/");
		Hit?.WithPathPrefixOnce("sounds/");
		Ambient?.WithPathPrefixOnce("sounds/");
	}
}
