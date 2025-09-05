using System;

public class Packet_HeldSoundSetSerializer
{
	private const int field = 8;

	public static Packet_HeldSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_HeldSoundSet packet_HeldSoundSet = new Packet_HeldSoundSet();
		DeserializeLengthDelimited(stream, packet_HeldSoundSet);
		return packet_HeldSoundSet;
	}

	public static Packet_HeldSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_HeldSoundSet instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_HeldSoundSet Deserialize(CitoMemoryStream stream, Packet_HeldSoundSet instance)
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
				instance.Idle = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Equip = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Unequip = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.Attack = ProtocolParser.ReadString(stream);
				break;
			case 42:
				instance.InvPickup = ProtocolParser.ReadString(stream);
				break;
			case 50:
				instance.InvPlace = ProtocolParser.ReadString(stream);
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

	public static Packet_HeldSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_HeldSoundSet instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_HeldSoundSet result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_HeldSoundSet instance)
	{
		if (instance.Idle != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Idle);
		}
		if (instance.Equip != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Equip);
		}
		if (instance.Unequip != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Unequip);
		}
		if (instance.Attack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Attack);
		}
		if (instance.InvPickup != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.InvPickup);
		}
		if (instance.InvPlace != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.InvPlace);
		}
	}

	public static int GetSize(Packet_HeldSoundSet instance)
	{
		int num = 0;
		if (instance.Idle != null)
		{
			num += ProtocolParser.GetSize(instance.Idle) + 1;
		}
		if (instance.Equip != null)
		{
			num += ProtocolParser.GetSize(instance.Equip) + 1;
		}
		if (instance.Unequip != null)
		{
			num += ProtocolParser.GetSize(instance.Unequip) + 1;
		}
		if (instance.Attack != null)
		{
			num += ProtocolParser.GetSize(instance.Attack) + 1;
		}
		if (instance.InvPickup != null)
		{
			num += ProtocolParser.GetSize(instance.InvPickup) + 1;
		}
		if (instance.InvPlace != null)
		{
			num += ProtocolParser.GetSize(instance.InvPlace) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_HeldSoundSet instance)
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

	public static byte[] SerializeToBytes(Packet_HeldSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_HeldSoundSet instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
