using System;

public class Packet_PlayerDataSerializer
{
	private const int field = 8;

	public static Packet_PlayerData DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerData packet_PlayerData = new Packet_PlayerData();
		DeserializeLengthDelimited(stream, packet_PlayerData);
		return packet_PlayerData;
	}

	public static Packet_PlayerData DeserializeBuffer(byte[] buffer, int length, Packet_PlayerData instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerData Deserialize(CitoMemoryStream stream, Packet_PlayerData instance)
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
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.GameMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.FreeMove = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.NoClip = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.InventoryContentsAdd(Packet_InventoryContentsSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.PlayerUID = ProtocolParser.ReadString(stream);
				break;
			case 72:
				instance.PickingRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.AreaSelectionMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 98:
				instance.PrivilegesAdd(ProtocolParser.ReadString(stream));
				break;
			case 106:
				instance.PlayerName = ProtocolParser.ReadString(stream);
				break;
			case 114:
				instance.Entitlements = ProtocolParser.ReadString(stream);
				break;
			case 120:
				instance.HotbarSlotId = ProtocolParser.ReadUInt32(stream);
				break;
			case 128:
				instance.Deaths = ProtocolParser.ReadUInt32(stream);
				break;
			case 136:
				instance.Spawnx = ProtocolParser.ReadUInt32(stream);
				break;
			case 144:
				instance.Spawny = ProtocolParser.ReadUInt32(stream);
				break;
			case 152:
				instance.Spawnz = ProtocolParser.ReadUInt32(stream);
				break;
			case 162:
				instance.RoleCode = ProtocolParser.ReadString(stream);
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

	public static Packet_PlayerData DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerData instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_PlayerData result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerData instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.InventoryContents != null)
		{
			Packet_InventoryContents[] inventoryContents = instance.InventoryContents;
			int inventoryContentsCount = instance.InventoryContentsCount;
			for (int i = 0; i < inventoryContents.Length && i < inventoryContentsCount; i++)
			{
				stream.WriteByte(58);
				Packet_InventoryContentsSerializer.SerializeWithSize(stream, inventoryContents[i]);
			}
		}
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.AreaSelectionMode != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AreaSelectionMode);
		}
		if (instance.Privileges != null)
		{
			string[] privileges = instance.Privileges;
			int privilegesCount = instance.PrivilegesCount;
			for (int j = 0; j < privileges.Length && j < privilegesCount; j++)
			{
				stream.WriteByte(98);
				ProtocolParser.WriteString(stream, privileges[j]);
			}
		}
		if (instance.PlayerName != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteString(stream, instance.PlayerName);
		}
		if (instance.Entitlements != null)
		{
			stream.WriteByte(114);
			ProtocolParser.WriteString(stream, instance.Entitlements);
		}
		if (instance.HotbarSlotId != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.HotbarSlotId);
		}
		if (instance.Deaths != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.Deaths);
		}
		if (instance.Spawnx != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnx);
		}
		if (instance.Spawny != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawny);
		}
		if (instance.Spawnz != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnz);
		}
		if (instance.RoleCode != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteString(stream, instance.RoleCode);
		}
	}

	public static int GetSize(Packet_PlayerData instance)
	{
		int num = 0;
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.EntityId != 0L)
		{
			num += ProtocolParser.GetSize(instance.EntityId) + 1;
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
		if (instance.InventoryContents != null)
		{
			for (int i = 0; i < instance.InventoryContentsCount; i++)
			{
				int size = Packet_InventoryContentsSerializer.GetSize(instance.InventoryContents[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.PlayerUID != null)
		{
			num += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.PickingRange != 0)
		{
			num += ProtocolParser.GetSize(instance.PickingRange) + 1;
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			num += ProtocolParser.GetSize(instance.FreeMovePlaneLock) + 1;
		}
		if (instance.AreaSelectionMode != 0)
		{
			num += ProtocolParser.GetSize(instance.AreaSelectionMode) + 1;
		}
		if (instance.Privileges != null)
		{
			for (int j = 0; j < instance.PrivilegesCount; j++)
			{
				string s = instance.Privileges[j];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.PlayerName != null)
		{
			num += ProtocolParser.GetSize(instance.PlayerName) + 1;
		}
		if (instance.Entitlements != null)
		{
			num += ProtocolParser.GetSize(instance.Entitlements) + 1;
		}
		if (instance.HotbarSlotId != 0)
		{
			num += ProtocolParser.GetSize(instance.HotbarSlotId) + 1;
		}
		if (instance.Deaths != 0)
		{
			num += ProtocolParser.GetSize(instance.Deaths) + 2;
		}
		if (instance.Spawnx != 0)
		{
			num += ProtocolParser.GetSize(instance.Spawnx) + 2;
		}
		if (instance.Spawny != 0)
		{
			num += ProtocolParser.GetSize(instance.Spawny) + 2;
		}
		if (instance.Spawnz != 0)
		{
			num += ProtocolParser.GetSize(instance.Spawnz) + 2;
		}
		if (instance.RoleCode != null)
		{
			num += ProtocolParser.GetSize(instance.RoleCode) + 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerData instance)
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

	public static byte[] SerializeToBytes(Packet_PlayerData instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerData instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
