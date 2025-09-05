using System;

public class Packet_PlayerGroupSerializer
{
	private const int field = 8;

	public static Packet_PlayerGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroup packet_PlayerGroup = new Packet_PlayerGroup();
		DeserializeLengthDelimited(stream, packet_PlayerGroup);
		return packet_PlayerGroup;
	}

	public static Packet_PlayerGroup DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroup instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroup Deserialize(CitoMemoryStream stream, Packet_PlayerGroup instance)
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
			case 8:
				instance.Uid = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.Owneruid = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.ChathistoryAdd(Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.Createdbyprivatemessage = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Membership = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroup instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_PlayerGroup result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroup instance)
	{
		if (instance.Uid != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Uid);
		}
		if (instance.Owneruid != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Owneruid);
		}
		if (instance.Name != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.Chathistory != null)
		{
			Packet_ChatLine[] chathistory = instance.Chathistory;
			int chathistoryCount = instance.ChathistoryCount;
			for (int i = 0; i < chathistory.Length && i < chathistoryCount; i++)
			{
				stream.WriteByte(34);
				Packet_ChatLineSerializer.SerializeWithSize(stream, chathistory[i]);
			}
		}
		if (instance.Createdbyprivatemessage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Createdbyprivatemessage);
		}
		if (instance.Membership != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Membership);
		}
	}

	public static int GetSize(Packet_PlayerGroup instance)
	{
		int num = 0;
		if (instance.Uid != 0)
		{
			num += ProtocolParser.GetSize(instance.Uid) + 1;
		}
		if (instance.Owneruid != null)
		{
			num += ProtocolParser.GetSize(instance.Owneruid) + 1;
		}
		if (instance.Name != null)
		{
			num += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.Chathistory != null)
		{
			for (int i = 0; i < instance.ChathistoryCount; i++)
			{
				int size = Packet_ChatLineSerializer.GetSize(instance.Chathistory[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.Createdbyprivatemessage != 0)
		{
			num += ProtocolParser.GetSize(instance.Createdbyprivatemessage) + 1;
		}
		if (instance.Membership != 0)
		{
			num += ProtocolParser.GetSize(instance.Membership) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerGroup instance)
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

	public static byte[] SerializeToBytes(Packet_PlayerGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroup instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
