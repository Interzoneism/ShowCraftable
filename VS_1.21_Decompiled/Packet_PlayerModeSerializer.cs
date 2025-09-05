using System;

public class Packet_PlayerModeSerializer
{
	private const int field = 8;

	public static Packet_PlayerMode DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerMode packet_PlayerMode = new Packet_PlayerMode();
		DeserializeLengthDelimited(stream, packet_PlayerMode);
		return packet_PlayerMode;
	}

	public static Packet_PlayerMode DeserializeBuffer(byte[] buffer, int length, Packet_PlayerMode instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerMode Deserialize(CitoMemoryStream stream, Packet_PlayerMode instance)
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
				instance.PlayerUID = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.GameMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.FreeMove = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.NoClip = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.ViewDistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.PickingRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.RenderMetaBlocks = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerMode DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerMode instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_PlayerMode result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerMode instance)
	{
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.ViewDistance != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.ViewDistance);
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.RenderMetaBlocks != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.RenderMetaBlocks);
		}
	}

	public static int GetSize(Packet_PlayerMode instance)
	{
		int num = 0;
		if (instance.PlayerUID != null)
		{
			num += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.GameMode != 0)
		{
			num += ProtocolParser.GetSize(instance.GameMode) + 1;
		}
		if (instance.MoveSpeed != 0)
		{
			num += ProtocolParser.GetSize(instance.MoveSpeed) + 1;
		}
		if (instance.FreeMove != 0)
		{
			num += ProtocolParser.GetSize(instance.FreeMove) + 1;
		}
		if (instance.NoClip != 0)
		{
			num += ProtocolParser.GetSize(instance.NoClip) + 1;
		}
		if (instance.ViewDistance != 0)
		{
			num += ProtocolParser.GetSize(instance.ViewDistance) + 1;
		}
		if (instance.PickingRange != 0)
		{
			num += ProtocolParser.GetSize(instance.PickingRange) + 1;
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			num += ProtocolParser.GetSize(instance.FreeMovePlaneLock) + 1;
		}
		if (instance.ImmersiveFpMode != 0)
		{
			num += ProtocolParser.GetSize(instance.ImmersiveFpMode) + 1;
		}
		if (instance.RenderMetaBlocks != 0)
		{
			num += ProtocolParser.GetSize(instance.RenderMetaBlocks) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerMode instance)
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

	public static byte[] SerializeToBytes(Packet_PlayerMode instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerMode instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
