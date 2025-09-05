using System;

public class Packet_SpawnParticlesSerializer
{
	private const int field = 8;

	public static Packet_SpawnParticles DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_SpawnParticles packet_SpawnParticles = new Packet_SpawnParticles();
		DeserializeLengthDelimited(stream, packet_SpawnParticles);
		return packet_SpawnParticles;
	}

	public static Packet_SpawnParticles DeserializeBuffer(byte[] buffer, int length, Packet_SpawnParticles instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_SpawnParticles Deserialize(CitoMemoryStream stream, Packet_SpawnParticles instance)
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
				instance.ParticlePropertyProviderClassName = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_SpawnParticles DeserializeLengthDelimited(CitoMemoryStream stream, Packet_SpawnParticles instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_SpawnParticles result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_SpawnParticles instance)
	{
		if (instance.ParticlePropertyProviderClassName != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.ParticlePropertyProviderClassName);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_SpawnParticles instance)
	{
		int num = 0;
		if (instance.ParticlePropertyProviderClassName != null)
		{
			num += ProtocolParser.GetSize(instance.ParticlePropertyProviderClassName) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_SpawnParticles instance)
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

	public static byte[] SerializeToBytes(Packet_SpawnParticles instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_SpawnParticles instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
