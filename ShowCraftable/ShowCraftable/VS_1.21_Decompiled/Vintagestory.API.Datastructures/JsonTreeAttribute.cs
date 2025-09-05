using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Datastructures;

public class JsonTreeAttribute
{
	public string value;

	public string[] values;

	public Dictionary<string, JsonTreeAttribute> elems = new Dictionary<string, JsonTreeAttribute>();

	public EnumAttributeType type;

	public IAttribute ToAttribute(IWorldAccessor resolver)
	{
		if (type == EnumAttributeType.Unknown)
		{
			if (elems != null)
			{
				type = EnumAttributeType.Tree;
			}
			else if (values != null)
			{
				type = EnumAttributeType.StringArray;
			}
			else
			{
				type = EnumAttributeType.String;
			}
		}
		switch (type)
		{
		case EnumAttributeType.Bool:
			return new BoolAttribute(value == "true");
		case EnumAttributeType.Int:
		{
			int.TryParse(value, out var result4);
			return new IntAttribute(result4);
		}
		case EnumAttributeType.Double:
		{
			double.TryParse(value, out var result3);
			return new DoubleAttribute(result3);
		}
		case EnumAttributeType.Float:
		{
			float.TryParse(value, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result2);
			return new FloatAttribute(result2);
		}
		case EnumAttributeType.String:
			return new StringAttribute(value);
		case EnumAttributeType.StringArray:
			return new StringArrayAttribute(values);
		case EnumAttributeType.Tree:
		{
			ITreeAttribute treeAttribute = new TreeAttribute();
			if (elems == null)
			{
				return treeAttribute;
			}
			{
				foreach (KeyValuePair<string, JsonTreeAttribute> elem in elems)
				{
					IAttribute attribute2 = elem.Value.ToAttribute(resolver);
					if (attribute2 != null)
					{
						treeAttribute[elem.Key] = attribute2;
					}
				}
				return treeAttribute;
			}
		}
		case EnumAttributeType.Itemstack:
		{
			if (elems == null)
			{
				return null;
			}
			JsonTreeAttribute jsonTreeAttribute;
			bool num = elems.TryGetValue("class", out jsonTreeAttribute) && jsonTreeAttribute.type == EnumAttributeType.String;
			JsonTreeAttribute jsonTreeAttribute2;
			bool flag = elems.TryGetValue("code", out jsonTreeAttribute2) && jsonTreeAttribute2.type == EnumAttributeType.String;
			JsonTreeAttribute jsonTreeAttribute3;
			bool flag2 = elems.TryGetValue("quantity", out jsonTreeAttribute3) && jsonTreeAttribute3.type == EnumAttributeType.Int;
			if (!num || !flag || !flag2)
			{
				return null;
			}
			EnumItemClass enumItemClass;
			try
			{
				enumItemClass = (EnumItemClass)Enum.Parse(typeof(EnumItemClass), elems["class"].value);
			}
			catch (Exception)
			{
				return null;
			}
			int result = 0;
			if (!int.TryParse(elems["quantity"].value, out result))
			{
				return null;
			}
			ItemStack itemStack;
			if (enumItemClass == EnumItemClass.Block)
			{
				Block block = resolver.GetBlock(new AssetLocation(elems["code"].value));
				if (block == null)
				{
					return null;
				}
				itemStack = new ItemStack(block, result);
			}
			else
			{
				Item item = resolver.GetItem(new AssetLocation(elems["code"].value));
				if (item == null)
				{
					return null;
				}
				itemStack = new ItemStack(item, result);
			}
			if (elems.TryGetValue("attributes", out var jsonTreeAttribute4))
			{
				IAttribute attribute = jsonTreeAttribute4.ToAttribute(resolver);
				if (attribute is ITreeAttribute)
				{
					itemStack.Attributes = (ITreeAttribute)attribute;
				}
			}
			return new ItemstackAttribute(itemStack);
		}
		default:
			return null;
		}
	}

	public JsonTreeAttribute Clone()
	{
		JsonTreeAttribute jsonTreeAttribute = new JsonTreeAttribute
		{
			type = type,
			value = value
		};
		if (elems != null)
		{
			jsonTreeAttribute.elems = new Dictionary<string, JsonTreeAttribute>(elems);
		}
		return jsonTreeAttribute;
	}
}
