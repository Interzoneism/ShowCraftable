using System;

public class Packet_ClientBlockPlaceOrBreakSerializer
{
	private const int field = 8;

	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientBlockPlaceOrBreak packet_ClientBlockPlaceOrBreak = new Packet_ClientBlockPlaceOrBreak();
		DeserializeLengthDelimited(stream, packet_ClientBlockPlaceOrBreak);
		return packet_ClientBlockPlaceOrBreak;
	}

	public static Packet_ClientBlockPlaceOrBreak DeserializeBuffer(byte[] buffer, int length, Packet_ClientBlockPlaceOrBreak instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientBlockPlaceOrBreak Deserialize(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
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
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Mode = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.BlockType = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.HitX = ProtocolParser.ReadUInt64(stream);
				break;
			case 72:
				instance.HitY = ProtocolParser.ReadUInt64(stream);
				break;
			case 80:
				instance.HitZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 88:
				instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.DidOffset = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientBlockPlaceOrBreak result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.BlockType != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
		if (instance.DidOffset != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.DidOffset);
		}
	}

	public static int GetSize(Packet_ClientBlockPlaceOrBreak instance)
	{
		int num = 0;
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
		if (instance.Mode != 0)
		{
			num += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		if (instance.BlockType != 0)
		{
			num += ProtocolParser.GetSize(instance.BlockType) + 1;
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
		if (instance.DidOffset != 0)
		{
			num += ProtocolParser.GetSize(instance.DidOffset) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
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

	public static byte[] SerializeToBytes(Packet_ClientBlockPlaceOrBreak instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
