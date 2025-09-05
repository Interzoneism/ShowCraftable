using System;

public class Packet_EntityDespawnSerializer
{
	private const int field = 8;

	public static Packet_EntityDespawn DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityDespawn packet_EntityDespawn = new Packet_EntityDespawn();
		DeserializeLengthDelimited(stream, packet_EntityDespawn);
		return packet_EntityDespawn;
	}

	public static Packet_EntityDespawn DeserializeBuffer(byte[] buffer, int length, Packet_EntityDespawn instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityDespawn Deserialize(CitoMemoryStream stream, Packet_EntityDespawn instance)
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
				instance.EntityIdAdd(ProtocolParser.ReadUInt64(stream));
				break;
			case 16:
				instance.DespawnReasonAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.DeathDamageSourceAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.ByEntityIdAdd(ProtocolParser.ReadUInt64(stream));
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

	public static Packet_EntityDespawn DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityDespawn instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityDespawn result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityDespawn instance)
	{
		if (instance.EntityId != null)
		{
			long[] entityId = instance.EntityId;
			int entityIdCount = instance.EntityIdCount;
			for (int i = 0; i < entityId.Length && i < entityIdCount; i++)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt64(stream, entityId[i]);
			}
		}
		if (instance.DespawnReason != null)
		{
			int[] despawnReason = instance.DespawnReason;
			int despawnReasonCount = instance.DespawnReasonCount;
			for (int j = 0; j < despawnReason.Length && j < despawnReasonCount; j++)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, despawnReason[j]);
			}
		}
		if (instance.DeathDamageSource != null)
		{
			int[] deathDamageSource = instance.DeathDamageSource;
			int deathDamageSourceCount = instance.DeathDamageSourceCount;
			for (int k = 0; k < deathDamageSource.Length && k < deathDamageSourceCount; k++)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, deathDamageSource[k]);
			}
		}
		if (instance.ByEntityId != null)
		{
			long[] byEntityId = instance.ByEntityId;
			int byEntityIdCount = instance.ByEntityIdCount;
			for (int l = 0; l < byEntityId.Length && l < byEntityIdCount; l++)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt64(stream, byEntityId[l]);
			}
		}
	}

	public static int GetSize(Packet_EntityDespawn instance)
	{
		int num = 0;
		if (instance.EntityId != null)
		{
			for (int i = 0; i < instance.EntityIdCount; i++)
			{
				long v = instance.EntityId[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.DespawnReason != null)
		{
			for (int j = 0; j < instance.DespawnReasonCount; j++)
			{
				int v2 = instance.DespawnReason[j];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.DeathDamageSource != null)
		{
			for (int k = 0; k < instance.DeathDamageSourceCount; k++)
			{
				int v3 = instance.DeathDamageSource[k];
				num += ProtocolParser.GetSize(v3) + 1;
			}
		}
		if (instance.ByEntityId != null)
		{
			for (int l = 0; l < instance.ByEntityIdCount; l++)
			{
				long v4 = instance.ByEntityId[l];
				num += ProtocolParser.GetSize(v4) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityDespawn instance)
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

	public static byte[] SerializeToBytes(Packet_EntityDespawn instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityDespawn instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
