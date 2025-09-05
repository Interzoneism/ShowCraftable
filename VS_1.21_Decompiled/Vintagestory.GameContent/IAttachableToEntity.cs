using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public interface IAttachableToEntity
{
	int RequiresBehindSlots { get; set; }

	bool IsAttachable(Entity toEntity, ItemStack itemStack);

	void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict);

	string GetCategoryCode(ItemStack stack);

	CompositeShape GetAttachedShape(ItemStack stack, string slotCode);

	string[] GetDisableElements(ItemStack stack);

	string[] GetKeepElements(ItemStack stack);

	string GetTexturePrefixCode(ItemStack stack);

	static IAttachableToEntity FromCollectible(CollectibleObject cobj)
	{
		IAttachableToEntity collectibleInterface = cobj.GetCollectibleInterface<IAttachableToEntity>();
		if (collectibleInterface != null)
		{
			return collectibleInterface;
		}
		return FromAttributes(cobj);
	}

	static IAttachableToEntity FromAttributes(CollectibleObject cobj)
	{
		AttributeAttachableToEntity attributeAttachableToEntity = cobj.Attributes?["attachableToEntity"].AsObject<AttributeAttachableToEntity>(null, cobj.Code.Domain);
		if (attributeAttachableToEntity == null)
		{
			JsonObject attributes = cobj.Attributes;
			if (attributes != null && attributes["wearableAttachment"].Exists)
			{
				return new AttributeAttachableToEntity
				{
					CategoryCode = (cobj.Attributes["clothescategory"].AsString() ?? cobj.Attributes?["attachableToEntity"]["categoryCode"].AsString()),
					KeepElements = cobj.Attributes["keepElements"].AsArray<string>(),
					DisableElements = cobj.Attributes["disableElements"].AsArray<string>()
				};
			}
		}
		return attributeAttachableToEntity;
	}

	static void CollectTexturesFromCollectible(ItemStack stack, string texturePrefixCode, Shape gearShape, Dictionary<string, CompositeTexture> intoDict)
	{
		if (gearShape.Textures == null)
		{
			gearShape.Textures = new Dictionary<string, AssetLocation>();
		}
		IDictionary<string, CompositeTexture> dictionary;
		if (stack.Class != EnumItemClass.Block)
		{
			IDictionary<string, CompositeTexture> textures = stack.Item.Textures;
			dictionary = textures;
		}
		else
		{
			dictionary = stack.Block.Textures;
		}
		IDictionary<string, CompositeTexture> dictionary2 = dictionary;
		if (dictionary2 == null)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> item in dictionary2)
		{
			gearShape.Textures[item.Key] = item.Value.Base;
		}
	}
}
