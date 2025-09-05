using System;

public class Packet_ServerAssetsSerializer
{
	private const int field = 8;

	public static Packet_ServerAssets DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerAssets packet_ServerAssets = new Packet_ServerAssets();
		DeserializeLengthDelimited(stream, packet_ServerAssets);
		return packet_ServerAssets;
	}

	public static Packet_ServerAssets DeserializeBuffer(byte[] buffer, int length, Packet_ServerAssets instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerAssets Deserialize(CitoMemoryStream stream, Packet_ServerAssets instance)
	{
		instance.InitializeValues();
		int num;
		while (true)
		{
			num = stream.ReadByte();
			if ((num & 0x80) != 0)
			{
				num = ProtocolParser.ReadKeyAsInt(num, stream);
				if ((num & 0x4000) != 0)
				{
					break;
				}
			}
			switch (num)
			{
			case 0:
				return null;
			case 10:
				instance.BlocksAdd(Packet_BlockTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.ItemsAdd(Packet_ItemTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.EntitiesAdd(Packet_EntityTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 34:
				instance.RecipesAdd(Packet_RecipesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 42:
				if (instance.Tags == null)
				{
					instance.Tags = Packet_TagsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_TagsSerializer.DeserializeLengthDelimited(stream, instance.Tags);
				}
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(num));
				break;
			}
		}
		if (num >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_ServerAssets DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerAssets instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerAssets result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerAssets instance)
	{
		if (instance.Blocks != null)
		{
			Packet_BlockType[] blocks = instance.Blocks;
			int blocksCount = instance.BlocksCount;
			for (int i = 0; i < blocks.Length && i < blocksCount; i++)
			{
				stream.WriteByte(10);
				Packet_BlockTypeSerializer.SerializeWithSize(stream, blocks[i]);
			}
		}
		if (instance.Items != null)
		{
			Packet_ItemType[] items = instance.Items;
			int itemsCount = instance.ItemsCount;
			for (int j = 0; j < items.Length && j < itemsCount; j++)
			{
				stream.WriteByte(18);
				Packet_ItemTypeSerializer.SerializeWithSize(stream, items[j]);
			}
		}
		if (instance.Entities != null)
		{
			Packet_EntityType[] entities = instance.Entities;
			int entitiesCount = instance.EntitiesCount;
			for (int k = 0; k < entities.Length && k < entitiesCount; k++)
			{
				stream.WriteByte(26);
				Packet_EntityTypeSerializer.SerializeWithSize(stream, entities[k]);
			}
		}
		if (instance.Recipes != null)
		{
			Packet_Recipes[] recipes = instance.Recipes;
			int recipesCount = instance.RecipesCount;
			for (int l = 0; l < recipes.Length && l < recipesCount; l++)
			{
				stream.WriteByte(34);
				Packet_RecipesSerializer.SerializeWithSize(stream, recipes[l]);
			}
		}
		if (instance.Tags != null)
		{
			stream.WriteByte(42);
			Packet_TagsSerializer.SerializeWithSize(stream, instance.Tags);
		}
	}

	public static int GetSize(Packet_ServerAssets instance)
	{
		int num = 0;
		if (instance.Blocks != null)
		{
			for (int i = 0; i < instance.BlocksCount; i++)
			{
				int size = Packet_BlockTypeSerializer.GetSize(instance.Blocks[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.Items != null)
		{
			for (int j = 0; j < instance.ItemsCount; j++)
			{
				int size2 = Packet_ItemTypeSerializer.GetSize(instance.Items[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.Entities != null)
		{
			for (int k = 0; k < instance.EntitiesCount; k++)
			{
				int size3 = Packet_EntityTypeSerializer.GetSize(instance.Entities[k]);
				num += size3 + ProtocolParser.GetSize(size3) + 1;
			}
		}
		if (instance.Recipes != null)
		{
			for (int l = 0; l < instance.RecipesCount; l++)
			{
				int size4 = Packet_RecipesSerializer.GetSize(instance.Recipes[l]);
				num += size4 + ProtocolParser.GetSize(size4) + 1;
			}
		}
		if (instance.Tags != null)
		{
			int size5 = Packet_TagsSerializer.GetSize(instance.Tags);
			num += size5 + ProtocolParser.GetSize(size5) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerAssets instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int num = stream.Position();
		Serialize(stream, instance);
		int num2 = stream.Position() - num;
		if (num2 != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size + " != " + num2);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerAssets instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerAssets instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
