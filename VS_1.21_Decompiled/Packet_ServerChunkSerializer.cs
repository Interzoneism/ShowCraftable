using System;

public class Packet_ServerChunkSerializer
{
	private const int field = 8;

	public static Packet_ServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerChunk packet_ServerChunk = new Packet_ServerChunk();
		DeserializeLengthDelimited(stream, packet_ServerChunk);
		return packet_ServerChunk;
	}

	public static Packet_ServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerChunk Deserialize(CitoMemoryStream stream, Packet_ServerChunk instance)
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
				instance.Blocks = ProtocolParser.ReadBytes(stream);
				break;
			case 18:
				instance.Light = ProtocolParser.ReadBytes(stream);
				break;
			case 26:
				instance.LightSat = ProtocolParser.ReadBytes(stream);
				break;
			case 122:
				instance.Liquids = ProtocolParser.ReadBytes(stream);
				break;
			case 72:
				instance.LightPositionsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.EntitiesAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.BlockEntitiesAdd(Packet_BlockEntitySerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 82:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
				break;
			case 88:
				instance.Empty = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.DecorsPosAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 104:
				instance.DecorsIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 112:
				instance.Compver = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerChunk instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerChunk result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerChunk instance)
	{
		if (instance.Blocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Blocks);
		}
		if (instance.Light != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Light);
		}
		if (instance.LightSat != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.LightSat);
		}
		if (instance.Liquids != null)
		{
			stream.WriteByte(122);
			ProtocolParser.WriteBytes(stream, instance.Liquids);
		}
		if (instance.LightPositions != null)
		{
			int[] lightPositions = instance.LightPositions;
			int lightPositionsCount = instance.LightPositionsCount;
			for (int i = 0; i < lightPositions.Length && i < lightPositionsCount; i++)
			{
				stream.WriteByte(72);
				ProtocolParser.WriteUInt32(stream, lightPositions[i]);
			}
		}
		if (instance.X != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Entities != null)
		{
			Packet_Entity[] entities = instance.Entities;
			int entitiesCount = instance.EntitiesCount;
			for (int j = 0; j < entities.Length && j < entitiesCount; j++)
			{
				stream.WriteByte(58);
				Packet_EntitySerializer.SerializeWithSize(stream, entities[j]);
			}
		}
		if (instance.BlockEntities != null)
		{
			Packet_BlockEntity[] blockEntities = instance.BlockEntities;
			int blockEntitiesCount = instance.BlockEntitiesCount;
			for (int k = 0; k < blockEntities.Length && k < blockEntitiesCount; k++)
			{
				stream.WriteByte(66);
				Packet_BlockEntitySerializer.SerializeWithSize(stream, blockEntities[k]);
			}
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(82);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Empty != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Empty);
		}
		if (instance.DecorsPos != null)
		{
			int[] decorsPos = instance.DecorsPos;
			int decorsPosCount = instance.DecorsPosCount;
			for (int l = 0; l < decorsPos.Length && l < decorsPosCount; l++)
			{
				stream.WriteByte(96);
				ProtocolParser.WriteUInt32(stream, decorsPos[l]);
			}
		}
		if (instance.DecorsIds != null)
		{
			int[] decorsIds = instance.DecorsIds;
			int decorsIdsCount = instance.DecorsIdsCount;
			for (int m = 0; m < decorsIds.Length && m < decorsIdsCount; m++)
			{
				stream.WriteByte(104);
				ProtocolParser.WriteUInt32(stream, decorsIds[m]);
			}
		}
		if (instance.Compver != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Compver);
		}
	}

	public static int GetSize(Packet_ServerChunk instance)
	{
		int num = 0;
		if (instance.Blocks != null)
		{
			num += ProtocolParser.GetSize(instance.Blocks) + 1;
		}
		if (instance.Light != null)
		{
			num += ProtocolParser.GetSize(instance.Light) + 1;
		}
		if (instance.LightSat != null)
		{
			num += ProtocolParser.GetSize(instance.LightSat) + 1;
		}
		if (instance.Liquids != null)
		{
			num += ProtocolParser.GetSize(instance.Liquids) + 1;
		}
		if (instance.LightPositions != null)
		{
			for (int i = 0; i < instance.LightPositionsCount; i++)
			{
				int v = instance.LightPositions[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.X != 0)
		{
			num += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			num += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			num += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Entities != null)
		{
			for (int j = 0; j < instance.EntitiesCount; j++)
			{
				int size = Packet_EntitySerializer.GetSize(instance.Entities[j]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.BlockEntities != null)
		{
			for (int k = 0; k < instance.BlockEntitiesCount; k++)
			{
				int size2 = Packet_BlockEntitySerializer.GetSize(instance.BlockEntities[k]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.Moddata != null)
		{
			num += ProtocolParser.GetSize(instance.Moddata) + 1;
		}
		if (instance.Empty != 0)
		{
			num += ProtocolParser.GetSize(instance.Empty) + 1;
		}
		if (instance.DecorsPos != null)
		{
			for (int l = 0; l < instance.DecorsPosCount; l++)
			{
				int v2 = instance.DecorsPos[l];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.DecorsIds != null)
		{
			for (int m = 0; m < instance.DecorsIdsCount; m++)
			{
				int v3 = instance.DecorsIds[m];
				num += ProtocolParser.GetSize(v3) + 1;
			}
		}
		if (instance.Compver != 0)
		{
			num += ProtocolParser.GetSize(instance.Compver) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerChunk instance)
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

	public static byte[] SerializeToBytes(Packet_ServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerChunk instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
