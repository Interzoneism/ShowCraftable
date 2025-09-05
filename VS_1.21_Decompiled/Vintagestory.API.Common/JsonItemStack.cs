using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[DocumentAsJson]
[ProtoContract]
public class JsonItemStack : IRecipeOutput
{
	[ProtoMember(1)]
	[DocumentAsJson]
	public EnumItemClass Type;

	[ProtoMember(2)]
	[DocumentAsJson]
	public AssetLocation Code;

	[ProtoMember(3)]
	[DocumentAsJson]
	public int StackSize = 1;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	[ProtoMember(4)]
	public JsonObject Attributes;

	public ItemStack ResolvedItemstack;

	[DocumentAsJson]
	public int Quantity
	{
		get
		{
			return StackSize;
		}
		set
		{
			StackSize = value;
		}
	}

	public static JsonItemStack FromString(string jsonItemstack)
	{
		return JsonObject.FromJson(jsonItemstack).AsObject<JsonItemStack>();
	}

	public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging, AssetLocation assetLoc, bool printWarningOnError = true)
	{
		if (Type == EnumItemClass.Block)
		{
			Block block = resolver.GetBlock(Code);
			if (block == null || block.IsMissing)
			{
				if (printWarningOnError)
				{
					resolver.Logger.Warning("Failed resolving a blocks blockdrop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging + assetLoc);
				}
				return false;
			}
			ResolvedItemstack = new ItemStack(block, StackSize);
		}
		else
		{
			Item item = resolver.GetItem(Code);
			if (item == null || item.IsMissing)
			{
				if (printWarningOnError)
				{
					resolver.Logger.Warning("Failed resolving a blocks itemdrop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging + assetLoc);
				}
				return false;
			}
			ResolvedItemstack = new ItemStack(item, StackSize);
		}
		if (Attributes != null)
		{
			IAttribute attribute = Attributes.ToAttribute();
			if (attribute is ITreeAttribute)
			{
				ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
			}
		}
		return true;
	}

	public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging, bool printWarningOnError = true)
	{
		if (Type == EnumItemClass.Block)
		{
			Block block = resolver.GetBlock(Code);
			if (block == null || block.IsMissing)
			{
				if (printWarningOnError)
				{
					resolver.Logger.Warning("Failed resolving a blocks blockdrop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging);
				}
				return false;
			}
			ResolvedItemstack = new ItemStack(block, StackSize);
		}
		else
		{
			Item item = resolver.GetItem(Code);
			if (item == null || item.IsMissing)
			{
				if (printWarningOnError)
				{
					resolver.Logger.Warning("Failed resolving a blocks itemdrop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging);
				}
				return false;
			}
			ResolvedItemstack = new ItemStack(item, StackSize);
		}
		if (Attributes != null)
		{
			IAttribute attribute = Attributes.ToAttribute();
			if (attribute is ITreeAttribute)
			{
				ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
			}
		}
		return true;
	}

	public bool Matches(IWorldAccessor worldForResolve, ItemStack inputStack)
	{
		return ResolvedItemstack.Equals(worldForResolve, inputStack, GlobalConstants.IgnoredStackAttributes);
	}

	public JsonItemStack Clone()
	{
		JsonItemStack jsonItemStack = new JsonItemStack
		{
			Code = Code.Clone(),
			ResolvedItemstack = ResolvedItemstack?.Clone(),
			StackSize = StackSize,
			Type = Type
		};
		if (Attributes != null)
		{
			jsonItemStack.Attributes = Attributes.Clone();
		}
		return jsonItemStack;
	}

	public virtual void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		Type = (EnumItemClass)reader.ReadInt16();
		Code = new AssetLocation(reader.ReadString());
		StackSize = reader.ReadInt32();
		if (reader.ReadBoolean())
		{
			ResolvedItemstack = new ItemStack(reader);
		}
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write((short)Type);
		writer.Write(Code.ToShortString());
		writer.Write(StackSize);
		writer.Write(ResolvedItemstack != null);
		if (ResolvedItemstack != null)
		{
			ResolvedItemstack.ToBytes(writer);
		}
	}

	public void FillPlaceHolder(string key, string value)
	{
		Code = Code.CopyWithPath(Code.Path.Replace("{" + key + "}", value));
		Attributes?.FillPlaceHolder(key, value);
	}
}
