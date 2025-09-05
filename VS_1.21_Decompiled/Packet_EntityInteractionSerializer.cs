using System;

public class Packet_EntityInteractionSerializer
{
	private const int field = 8;

	public static Packet_EntityInteraction DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityInteraction packet_EntityInteraction = new Packet_EntityInteraction();
		DeserializeLengthDelimited(stream, packet_EntityInteraction);
		return packet_EntityInteraction;
	}

	public static Packet_EntityInteraction DeserializeBuffer(byte[] buffer, int length, Packet_EntityInteraction instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityInteraction Deserialize(CitoMemoryStream stream, Packet_EntityInteraction instance)
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
				instance.MouseButton = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.HitX = ProtocolParser.ReadUInt64(stream);
				break;
			case 40:
				instance.HitY = ProtocolParser.ReadUInt64(stream);
				break;
			case 48:
				instance.HitZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 56:
				instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_EntityInteraction DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityInteraction instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityInteraction result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityInteraction instance)
	{
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
	}

	public static int GetSize(Packet_EntityInteraction instance)
	{
		int num = 0;
		if (instance.MouseButton != 0)
		{
			num += ProtocolParser.GetSize(instance.MouseButton) + 1;
		}
		if (instance.EntityId != 0L)
		{
			num += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.OnBlockFace != 0)
		{
			num += ProtocolParser.GetSize(instance.OnBlockFace) + 1;
		}
		if (instance.HitX != 0L)
		{
			num += ProtocolParser.GetSize(instance.HitX) + 1;
		}
		if (instance.HitY != 0L)
		{
			num += ProtocolParser.GetSize(instance.HitY) + 1;
		}
		if (instance.HitZ != 0L)
		{
			num += ProtocolParser.GetSize(instance.HitZ) + 1;
		}
		if (instance.SelectionBoxIndex != 0)
		{
			num += ProtocolParser.GetSize(instance.SelectionBoxIndex) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityInteraction instance)
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

	public static byte[] SerializeToBytes(Packet_EntityInteraction instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityInteraction instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
