using System;

public class Packet_PlayerGroupsSerializer
{
	private const int field = 8;

	public static Packet_PlayerGroups DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroups packet_PlayerGroups = new Packet_PlayerGroups();
		DeserializeLengthDelimited(stream, packet_PlayerGroups);
		return packet_PlayerGroups;
	}

	public static Packet_PlayerGroups DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroups instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroups Deserialize(CitoMemoryStream stream, Packet_PlayerGroups instance)
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
				instance.GroupsAdd(Packet_PlayerGroupSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_PlayerGroups DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroups instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_PlayerGroups result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroups instance)
	{
		if (instance.Groups != null)
		{
			Packet_PlayerGroup[] groups = instance.Groups;
			int groupsCount = instance.GroupsCount;
			for (int i = 0; i < groups.Length && i < groupsCount; i++)
			{
				stream.WriteByte(10);
				Packet_PlayerGroupSerializer.SerializeWithSize(stream, groups[i]);
			}
		}
	}

	public static int GetSize(Packet_PlayerGroups instance)
	{
		int num = 0;
		if (instance.Groups != null)
		{
			for (int i = 0; i < instance.GroupsCount; i++)
			{
				int size = Packet_PlayerGroupSerializer.GetSize(instance.Groups[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerGroups instance)
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

	public static byte[] SerializeToBytes(Packet_PlayerGroups instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroups instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
