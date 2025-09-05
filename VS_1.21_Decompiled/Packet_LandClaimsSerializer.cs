using System;

public class Packet_LandClaimsSerializer
{
	private const int field = 8;

	public static Packet_LandClaims DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LandClaims packet_LandClaims = new Packet_LandClaims();
		DeserializeLengthDelimited(stream, packet_LandClaims);
		return packet_LandClaims;
	}

	public static Packet_LandClaims DeserializeBuffer(byte[] buffer, int length, Packet_LandClaims instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LandClaims Deserialize(CitoMemoryStream stream, Packet_LandClaims instance)
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
				instance.AllclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.AddclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_LandClaims DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LandClaims instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_LandClaims result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_LandClaims instance)
	{
		if (instance.Allclaims != null)
		{
			Packet_LandClaim[] allclaims = instance.Allclaims;
			int allclaimsCount = instance.AllclaimsCount;
			for (int i = 0; i < allclaims.Length && i < allclaimsCount; i++)
			{
				stream.WriteByte(10);
				Packet_LandClaimSerializer.SerializeWithSize(stream, allclaims[i]);
			}
		}
		if (instance.Addclaims != null)
		{
			Packet_LandClaim[] addclaims = instance.Addclaims;
			int addclaimsCount = instance.AddclaimsCount;
			for (int j = 0; j < addclaims.Length && j < addclaimsCount; j++)
			{
				stream.WriteByte(18);
				Packet_LandClaimSerializer.SerializeWithSize(stream, addclaims[j]);
			}
		}
	}

	public static int GetSize(Packet_LandClaims instance)
	{
		int num = 0;
		if (instance.Allclaims != null)
		{
			for (int i = 0; i < instance.AllclaimsCount; i++)
			{
				int size = Packet_LandClaimSerializer.GetSize(instance.Allclaims[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.Addclaims != null)
		{
			for (int j = 0; j < instance.AddclaimsCount; j++)
			{
				int size2 = Packet_LandClaimSerializer.GetSize(instance.Addclaims[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LandClaims instance)
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

	public static byte[] SerializeToBytes(Packet_LandClaims instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LandClaims instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
