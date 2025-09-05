using System;

public class Packet_EntityPositionSerializer
{
	private const int field = 8;

	public static Packet_EntityPosition DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityPosition packet_EntityPosition = new Packet_EntityPosition();
		DeserializeLengthDelimited(stream, packet_EntityPosition);
		return packet_EntityPosition;
	}

	public static Packet_EntityPosition DeserializeBuffer(byte[] buffer, int length, Packet_EntityPosition instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityPosition Deserialize(CitoMemoryStream stream, Packet_EntityPosition instance)
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
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 16:
				instance.X = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.Y = ProtocolParser.ReadUInt64(stream);
				break;
			case 32:
				instance.Z = ProtocolParser.ReadUInt64(stream);
				break;
			case 40:
				instance.Yaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Pitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.Roll = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.HeadYaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.HeadPitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.BodyYaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.Controls = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.Tick = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.PositionVersion = ProtocolParser.ReadUInt32(stream);
				break;
			case 112:
				instance.MotionX = ProtocolParser.ReadUInt64(stream);
				break;
			case 120:
				instance.MotionY = ProtocolParser.ReadUInt64(stream);
				break;
			case 128:
				instance.MotionZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 136:
				instance.Teleport = ProtocolParser.ReadBool(stream);
				break;
			case 144:
				instance.TagsBitmask1 = ProtocolParser.ReadUInt64(stream);
				break;
			case 152:
				instance.TagsBitmask2 = ProtocolParser.ReadUInt64(stream);
				break;
			case 160:
				instance.MountControls = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_EntityPosition DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityPosition instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityPosition result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityPosition instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.X != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.X);
		}
		if (instance.Y != 0L)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt64(stream, instance.Y);
		}
		if (instance.Z != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.Z);
		}
		if (instance.Yaw != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Yaw);
		}
		if (instance.Pitch != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Roll != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Roll);
		}
		if (instance.HeadYaw != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.HeadYaw);
		}
		if (instance.HeadPitch != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.HeadPitch);
		}
		if (instance.BodyYaw != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.BodyYaw);
		}
		if (instance.Controls != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Controls);
		}
		if (instance.Tick != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Tick);
		}
		if (instance.PositionVersion != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.PositionVersion);
		}
		if (instance.MotionX != 0L)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt64(stream, instance.MotionX);
		}
		if (instance.MotionY != 0L)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt64(stream, instance.MotionY);
		}
		if (instance.MotionZ != 0L)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt64(stream, instance.MotionZ);
		}
		if (instance.Teleport)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteBool(stream, instance.Teleport);
		}
		if (instance.TagsBitmask1 != 0L)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt64(stream, instance.TagsBitmask1);
		}
		if (instance.TagsBitmask2 != 0L)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt64(stream, instance.TagsBitmask2);
		}
		if (instance.MountControls != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.MountControls);
		}
	}

	public static int GetSize(Packet_EntityPosition instance)
	{
		int num = 0;
		if (instance.EntityId != 0L)
		{
			num += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.X != 0L)
		{
			num += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0L)
		{
			num += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0L)
		{
			num += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Yaw != 0)
		{
			num += ProtocolParser.GetSize(instance.Yaw) + 1;
		}
		if (instance.Pitch != 0)
		{
			num += ProtocolParser.GetSize(instance.Pitch) + 1;
		}
		if (instance.Roll != 0)
		{
			num += ProtocolParser.GetSize(instance.Roll) + 1;
		}
		if (instance.HeadYaw != 0)
		{
			num += ProtocolParser.GetSize(instance.HeadYaw) + 1;
		}
		if (instance.HeadPitch != 0)
		{
			num += ProtocolParser.GetSize(instance.HeadPitch) + 1;
		}
		if (instance.BodyYaw != 0)
		{
			num += ProtocolParser.GetSize(instance.BodyYaw) + 1;
		}
		if (instance.Controls != 0)
		{
			num += ProtocolParser.GetSize(instance.Controls) + 1;
		}
		if (instance.Tick != 0)
		{
			num += ProtocolParser.GetSize(instance.Tick) + 1;
		}
		if (instance.PositionVersion != 0)
		{
			num += ProtocolParser.GetSize(instance.PositionVersion) + 1;
		}
		if (instance.MotionX != 0L)
		{
			num += ProtocolParser.GetSize(instance.MotionX) + 1;
		}
		if (instance.MotionY != 0L)
		{
			num += ProtocolParser.GetSize(instance.MotionY) + 1;
		}
		if (instance.MotionZ != 0L)
		{
			num += ProtocolParser.GetSize(instance.MotionZ) + 2;
		}
		if (instance.Teleport)
		{
			num += 3;
		}
		if (instance.TagsBitmask1 != 0L)
		{
			num += ProtocolParser.GetSize(instance.TagsBitmask1) + 2;
		}
		if (instance.TagsBitmask2 != 0L)
		{
			num += ProtocolParser.GetSize(instance.TagsBitmask2) + 2;
		}
		if (instance.MountControls != 0)
		{
			num += ProtocolParser.GetSize(instance.MountControls) + 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityPosition instance)
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

	public static byte[] SerializeToBytes(Packet_EntityPosition instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityPosition instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
