using System;

public class Packet_ServerCalendarSerializer
{
	private const int field = 8;

	public static Packet_ServerCalendar DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerCalendar packet_ServerCalendar = new Packet_ServerCalendar();
		DeserializeLengthDelimited(stream, packet_ServerCalendar);
		return packet_ServerCalendar;
	}

	public static Packet_ServerCalendar DeserializeBuffer(byte[] buffer, int length, Packet_ServerCalendar instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerCalendar Deserialize(CitoMemoryStream stream, Packet_ServerCalendar instance)
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
				instance.TotalSeconds = ProtocolParser.ReadUInt64(stream);
				break;
			case 18:
				instance.TimeSpeedModifierNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 24:
				instance.TimeSpeedModifierSpeedsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.MoonOrbitDays = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.HoursPerDay = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Running = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.CalendarSpeedMul = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.DaysPerMonth = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.TotalSecondsStart = ProtocolParser.ReadUInt64(stream);
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

	public static Packet_ServerCalendar DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerCalendar instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerCalendar result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerCalendar instance)
	{
		if (instance.TotalSeconds != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.TotalSeconds);
		}
		if (instance.TimeSpeedModifierNames != null)
		{
			string[] timeSpeedModifierNames = instance.TimeSpeedModifierNames;
			int timeSpeedModifierNamesCount = instance.TimeSpeedModifierNamesCount;
			for (int i = 0; i < timeSpeedModifierNames.Length && i < timeSpeedModifierNamesCount; i++)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, timeSpeedModifierNames[i]);
			}
		}
		if (instance.TimeSpeedModifierSpeeds != null)
		{
			int[] timeSpeedModifierSpeeds = instance.TimeSpeedModifierSpeeds;
			int timeSpeedModifierSpeedsCount = instance.TimeSpeedModifierSpeedsCount;
			for (int j = 0; j < timeSpeedModifierSpeeds.Length && j < timeSpeedModifierSpeedsCount; j++)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, timeSpeedModifierSpeeds[j]);
			}
		}
		if (instance.MoonOrbitDays != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoonOrbitDays);
		}
		if (instance.HoursPerDay != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.HoursPerDay);
		}
		if (instance.Running != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Running);
		}
		if (instance.CalendarSpeedMul != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CalendarSpeedMul);
		}
		if (instance.DaysPerMonth != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.DaysPerMonth);
		}
		if (instance.TotalSecondsStart != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.TotalSecondsStart);
		}
	}

	public static int GetSize(Packet_ServerCalendar instance)
	{
		int num = 0;
		if (instance.TotalSeconds != 0L)
		{
			num += ProtocolParser.GetSize(instance.TotalSeconds) + 1;
		}
		if (instance.TimeSpeedModifierNames != null)
		{
			for (int i = 0; i < instance.TimeSpeedModifierNamesCount; i++)
			{
				string s = instance.TimeSpeedModifierNames[i];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.TimeSpeedModifierSpeeds != null)
		{
			for (int j = 0; j < instance.TimeSpeedModifierSpeedsCount; j++)
			{
				int v = instance.TimeSpeedModifierSpeeds[j];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.MoonOrbitDays != 0)
		{
			num += ProtocolParser.GetSize(instance.MoonOrbitDays) + 1;
		}
		if (instance.HoursPerDay != 0)
		{
			num += ProtocolParser.GetSize(instance.HoursPerDay) + 1;
		}
		if (instance.Running != 0)
		{
			num += ProtocolParser.GetSize(instance.Running) + 1;
		}
		if (instance.CalendarSpeedMul != 0)
		{
			num += ProtocolParser.GetSize(instance.CalendarSpeedMul) + 1;
		}
		if (instance.DaysPerMonth != 0)
		{
			num += ProtocolParser.GetSize(instance.DaysPerMonth) + 1;
		}
		if (instance.TotalSecondsStart != 0L)
		{
			num += ProtocolParser.GetSize(instance.TotalSecondsStart) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerCalendar instance)
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

	public static byte[] SerializeToBytes(Packet_ServerCalendar instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerCalendar instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
