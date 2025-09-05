using System;

public class Packet_UdpPacketSerializer
{
	private const int field = 8;

	public static Packet_UdpPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UdpPacket packet_UdpPacket = new Packet_UdpPacket();
		DeserializeLengthDelimited(stream, packet_UdpPacket);
		return packet_UdpPacket;
	}

	public static Packet_UdpPacket DeserializeBuffer(byte[] buffer, int length, Packet_UdpPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UdpPacket Deserialize(CitoMemoryStream stream, Packet_UdpPacket instance)
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
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 26:
				if (instance.BulkPositions == null)
				{
					instance.BulkPositions = Packet_BulkEntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityPositionSerializer.DeserializeLengthDelimited(stream, instance.BulkPositions);
				}
				break;
			case 34:
				if (instance.ChannelPacket == null)
				{
					instance.ChannelPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.ChannelPacket);
				}
				break;
			case 42:
				if (instance.ConnectionPacket == null)
				{
					instance.ConnectionPacket = Packet_ConnectionPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ConnectionPacketSerializer.DeserializeLengthDelimited(stream, instance.ConnectionPacket);
				}
				break;
			case 48:
				instance.Length = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_UdpPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UdpPacket instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_UdpPacket result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UdpPacket instance)
	{
		stream.WriteByte(8);
		ProtocolParser.WriteUInt32(stream, instance.Id);
		if (instance.EntityPosition != null)
		{
			stream.WriteByte(18);
			Packet_EntityPosition entityPosition = instance.EntityPosition;
			Packet_EntityPositionSerializer.GetSize(entityPosition);
			Packet_EntityPositionSerializer.SerializeWithSize(stream, entityPosition);
		}
		if (instance.BulkPositions != null)
		{
			stream.WriteByte(26);
			Packet_BulkEntityPosition bulkPositions = instance.BulkPositions;
			Packet_BulkEntityPositionSerializer.GetSize(bulkPositions);
			Packet_BulkEntityPositionSerializer.SerializeWithSize(stream, bulkPositions);
		}
		if (instance.ChannelPacket != null)
		{
			stream.WriteByte(34);
			Packet_CustomPacket channelPacket = instance.ChannelPacket;
			Packet_CustomPacketSerializer.GetSize(channelPacket);
			Packet_CustomPacketSerializer.SerializeWithSize(stream, channelPacket);
		}
		if (instance.ConnectionPacket != null)
		{
			stream.WriteByte(42);
			Packet_ConnectionPacket connectionPacket = instance.ConnectionPacket;
			Packet_ConnectionPacketSerializer.GetSize(connectionPacket);
			Packet_ConnectionPacketSerializer.SerializeWithSize(stream, connectionPacket);
		}
		if (instance.Length != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
	}

	public static int GetSize(Packet_UdpPacket instance)
	{
		int num = 0;
		num += ProtocolParser.GetSize(instance.Id) + 1;
		if (instance.EntityPosition != null)
		{
			int size = Packet_EntityPositionSerializer.GetSize(instance.EntityPosition);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.BulkPositions != null)
		{
			int size2 = Packet_BulkEntityPositionSerializer.GetSize(instance.BulkPositions);
			num += size2 + ProtocolParser.GetSize(size2) + 1;
		}
		if (instance.ChannelPacket != null)
		{
			int size3 = Packet_CustomPacketSerializer.GetSize(instance.ChannelPacket);
			num += size3 + ProtocolParser.GetSize(size3) + 1;
		}
		if (instance.ConnectionPacket != null)
		{
			int size4 = Packet_ConnectionPacketSerializer.GetSize(instance.ConnectionPacket);
			num += size4 + ProtocolParser.GetSize(size4) + 1;
		}
		if (instance.Length != 0)
		{
			num += ProtocolParser.GetSize(instance.Length) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UdpPacket instance)
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

	public static byte[] SerializeToBytes(Packet_UdpPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UdpPacket instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
