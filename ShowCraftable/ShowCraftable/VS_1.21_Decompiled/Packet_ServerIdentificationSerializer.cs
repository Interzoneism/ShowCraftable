using System;

public class Packet_ServerIdentificationSerializer
{
	private const int field = 8;

	public static Packet_ServerIdentification DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerIdentification packet_ServerIdentification = new Packet_ServerIdentification();
		DeserializeLengthDelimited(stream, packet_ServerIdentification);
		return packet_ServerIdentification;
	}

	public static Packet_ServerIdentification DeserializeBuffer(byte[] buffer, int length, Packet_ServerIdentification instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerIdentification Deserialize(CitoMemoryStream stream, Packet_ServerIdentification instance)
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
				instance.NetworkVersion = ProtocolParser.ReadString(stream);
				break;
			case 138:
				instance.GameVersion = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.ServerName = ProtocolParser.ReadString(stream);
				break;
			case 56:
				instance.MapSizeX = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.MapSizeY = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.MapSizeZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.RegionMapSizeX = ProtocolParser.ReadUInt32(stream);
				break;
			case 176:
				instance.RegionMapSizeY = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.RegionMapSizeZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.DisableShadows = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.PlayerAreaSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.Seed = ProtocolParser.ReadUInt32(stream);
				break;
			case 130:
				instance.PlayStyle = ProtocolParser.ReadString(stream);
				break;
			case 144:
				instance.RequireRemapping = ProtocolParser.ReadUInt32(stream);
				break;
			case 154:
				instance.ModsAdd(Packet_ModIdSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 162:
				instance.WorldConfiguration = ProtocolParser.ReadBytes(stream);
				break;
			case 194:
				instance.SavegameIdentifier = ProtocolParser.ReadString(stream);
				break;
			case 202:
				instance.PlayListCode = ProtocolParser.ReadString(stream);
				break;
			case 210:
				instance.ServerModIdBlackListAdd(ProtocolParser.ReadString(stream));
				break;
			case 218:
				instance.ServerModIdWhiteListAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_ServerIdentification DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerIdentification instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerIdentification result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerIdentification instance)
	{
		if (instance.NetworkVersion != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.NetworkVersion);
		}
		if (instance.GameVersion != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteString(stream, instance.GameVersion);
		}
		if (instance.ServerName != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.ServerName);
		}
		if (instance.MapSizeX != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeX);
		}
		if (instance.MapSizeY != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeY);
		}
		if (instance.MapSizeZ != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeZ);
		}
		if (instance.RegionMapSizeX != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeX);
		}
		if (instance.RegionMapSizeY != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeY);
		}
		if (instance.RegionMapSizeZ != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeZ);
		}
		if (instance.DisableShadows != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.DisableShadows);
		}
		if (instance.PlayerAreaSize != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.PlayerAreaSize);
		}
		if (instance.Seed != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Seed);
		}
		if (instance.PlayStyle != null)
		{
			stream.WriteKey(16, 2);
			ProtocolParser.WriteString(stream, instance.PlayStyle);
		}
		if (instance.RequireRemapping != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequireRemapping);
		}
		if (instance.Mods != null)
		{
			Packet_ModId[] mods = instance.Mods;
			int modsCount = instance.ModsCount;
			for (int i = 0; i < mods.Length && i < modsCount; i++)
			{
				stream.WriteKey(19, 2);
				Packet_ModIdSerializer.SerializeWithSize(stream, mods[i]);
			}
		}
		if (instance.WorldConfiguration != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteBytes(stream, instance.WorldConfiguration);
		}
		if (instance.SavegameIdentifier != null)
		{
			stream.WriteKey(24, 2);
			ProtocolParser.WriteString(stream, instance.SavegameIdentifier);
		}
		if (instance.PlayListCode != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteString(stream, instance.PlayListCode);
		}
		if (instance.ServerModIdBlackList != null)
		{
			string[] serverModIdBlackList = instance.ServerModIdBlackList;
			int serverModIdBlackListCount = instance.ServerModIdBlackListCount;
			for (int j = 0; j < serverModIdBlackList.Length && j < serverModIdBlackListCount; j++)
			{
				stream.WriteKey(26, 2);
				ProtocolParser.WriteString(stream, serverModIdBlackList[j]);
			}
		}
		if (instance.ServerModIdWhiteList != null)
		{
			string[] serverModIdWhiteList = instance.ServerModIdWhiteList;
			int serverModIdWhiteListCount = instance.ServerModIdWhiteListCount;
			for (int k = 0; k < serverModIdWhiteList.Length && k < serverModIdWhiteListCount; k++)
			{
				stream.WriteKey(27, 2);
				ProtocolParser.WriteString(stream, serverModIdWhiteList[k]);
			}
		}
	}

	public static int GetSize(Packet_ServerIdentification instance)
	{
		int num = 0;
		if (instance.NetworkVersion != null)
		{
			num += ProtocolParser.GetSize(instance.NetworkVersion) + 1;
		}
		if (instance.GameVersion != null)
		{
			num += ProtocolParser.GetSize(instance.GameVersion) + 2;
		}
		if (instance.ServerName != null)
		{
			num += ProtocolParser.GetSize(instance.ServerName) + 1;
		}
		if (instance.MapSizeX != 0)
		{
			num += ProtocolParser.GetSize(instance.MapSizeX) + 1;
		}
		if (instance.MapSizeY != 0)
		{
			num += ProtocolParser.GetSize(instance.MapSizeY) + 1;
		}
		if (instance.MapSizeZ != 0)
		{
			num += ProtocolParser.GetSize(instance.MapSizeZ) + 1;
		}
		if (instance.RegionMapSizeX != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionMapSizeX) + 2;
		}
		if (instance.RegionMapSizeY != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionMapSizeY) + 2;
		}
		if (instance.RegionMapSizeZ != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionMapSizeZ) + 2;
		}
		if (instance.DisableShadows != 0)
		{
			num += ProtocolParser.GetSize(instance.DisableShadows) + 1;
		}
		if (instance.PlayerAreaSize != 0)
		{
			num += ProtocolParser.GetSize(instance.PlayerAreaSize) + 1;
		}
		if (instance.Seed != 0)
		{
			num += ProtocolParser.GetSize(instance.Seed) + 1;
		}
		if (instance.PlayStyle != null)
		{
			num += ProtocolParser.GetSize(instance.PlayStyle) + 2;
		}
		if (instance.RequireRemapping != 0)
		{
			num += ProtocolParser.GetSize(instance.RequireRemapping) + 2;
		}
		if (instance.Mods != null)
		{
			for (int i = 0; i < instance.ModsCount; i++)
			{
				int size = Packet_ModIdSerializer.GetSize(instance.Mods[i]);
				num += size + ProtocolParser.GetSize(size) + 2;
			}
		}
		if (instance.WorldConfiguration != null)
		{
			num += ProtocolParser.GetSize(instance.WorldConfiguration) + 2;
		}
		if (instance.SavegameIdentifier != null)
		{
			num += ProtocolParser.GetSize(instance.SavegameIdentifier) + 2;
		}
		if (instance.PlayListCode != null)
		{
			num += ProtocolParser.GetSize(instance.PlayListCode) + 2;
		}
		if (instance.ServerModIdBlackList != null)
		{
			for (int j = 0; j < instance.ServerModIdBlackListCount; j++)
			{
				string s = instance.ServerModIdBlackList[j];
				num += ProtocolParser.GetSize(s) + 2;
			}
		}
		if (instance.ServerModIdWhiteList != null)
		{
			for (int k = 0; k < instance.ServerModIdWhiteListCount; k++)
			{
				string s2 = instance.ServerModIdWhiteList[k];
				num += ProtocolParser.GetSize(s2) + 2;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerIdentification instance)
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

	public static byte[] SerializeToBytes(Packet_ServerIdentification instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerIdentification instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
