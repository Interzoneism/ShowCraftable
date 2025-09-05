using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class Item : CollectibleObject
{
	public ItemTagArray Tags = ItemTagArray.Empty;

	public int ItemId;

	public CompositeShape Shape;

	public Dictionary<string, CompositeTexture> Textures = new Dictionary<string, CompositeTexture>();

	public override int Id => ItemId;

	public override EnumItemClass ItemClass => EnumItemClass.Item;

	public CompositeTexture FirstTexture
	{
		get
		{
			if (Textures != null && Textures.Count != 0)
			{
				return Textures.First().Value;
			}
			return null;
		}
	}

	public Item()
	{
	}

	public Item(int itemId)
	{
		ItemId = itemId;
		MaxStackSize = 1;
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		BakedCompositeTexture bakedCompositeTexture = Textures?.First().Value?.Baked;
		if (bakedCompositeTexture != null)
		{
			return capi.ItemTextureAtlas.GetRandomColor(bakedCompositeTexture.TextureSubId);
		}
		return 0;
	}

	public Item Clone()
	{
		Item item = (Item)MemberwiseClone();
		item.Code = Code.Clone();
		if (MiningSpeed != null)
		{
			item.MiningSpeed = new Dictionary<EnumBlockMaterial, float>(MiningSpeed);
		}
		item.Textures = new Dictionary<string, CompositeTexture>();
		if (Textures != null)
		{
			foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
			{
				item.Textures[texture.Key] = texture.Value.Clone();
			}
		}
		if (Shape != null)
		{
			item.Shape = Shape.Clone();
		}
		if (Attributes != null)
		{
			item.Attributes = Attributes.Clone();
		}
		if (CombustibleProps != null)
		{
			item.CombustibleProps = CombustibleProps.Clone();
		}
		if (NutritionProps != null)
		{
			item.NutritionProps = NutritionProps.Clone();
		}
		if (GrindingProps != null)
		{
			item.GrindingProps = GrindingProps.Clone();
		}
		return item;
	}

	internal void CheckTextures(ILogger logger)
	{
		List<string> list = null;
		int num = 0;
		foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
		{
			if (texture.Value.Base == null)
			{
				logger.Error("The texture definition {0} for #{2} in item with code {1} is invalid. The base property is null. Will skip.", num, Code, texture.Key);
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(texture.Key);
			}
			num++;
		}
		if (list == null)
		{
			return;
		}
		foreach (string item in list)
		{
			Textures.Remove(item);
		}
	}

	public virtual void FreeRAMServer()
	{
		Textures = null;
		GuiTransform = null;
		FpHandTransform = null;
		TpHandTransform = null;
		TpOffHandTransform = null;
		GroundTransform = null;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (world.Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			IEnumerable<string> source = GetTags(inSlot.Itemstack).ToArray().Select(coreClientAPI.TagRegistry.ItemTagIdToTag).Order();
			if (source.Any())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(35, 1, dsc);
				handler.AppendLiteral("<font color=\"#bbbbbb\">Tags: ");
				handler.AppendFormatted(source.Aggregate((string first, string second) => first + ", " + second));
				handler.AppendLiteral("</font>");
				dsc.AppendLine(ref handler);
			}
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public virtual ItemTagArray GetTags(ItemStack stack)
	{
		return Tags;
	}
}
