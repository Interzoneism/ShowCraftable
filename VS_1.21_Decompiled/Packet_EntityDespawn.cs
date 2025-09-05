public class Packet_EntityDespawn
{
	public long[] EntityId;

	public int EntityIdCount;

	public int EntityIdLength;

	public int[] DespawnReason;

	public int DespawnReasonCount;

	public int DespawnReasonLength;

	public int[] DeathDamageSource;

	public int DeathDamageSourceCount;

	public int DeathDamageSourceLength;

	public long[] ByEntityId;

	public int ByEntityIdCount;

	public int ByEntityIdLength;

	public const int EntityIdFieldID = 1;

	public const int DespawnReasonFieldID = 2;

	public const int DeathDamageSourceFieldID = 3;

	public const int ByEntityIdFieldID = 4;

	public int size;

	public long[] GetEntityId()
	{
		return EntityId;
	}

	public void SetEntityId(long[] value, int count, int length)
	{
		EntityId = value;
		EntityIdCount = count;
		EntityIdLength = length;
	}

	public void SetEntityId(long[] value)
	{
		EntityId = value;
		EntityIdCount = value.Length;
		EntityIdLength = value.Length;
	}

	public int GetEntityIdCount()
	{
		return EntityIdCount;
	}

	public void EntityIdAdd(long value)
	{
		if (EntityIdCount >= EntityIdLength)
		{
			if ((EntityIdLength *= 2) == 0)
			{
				EntityIdLength = 1;
			}
			long[] array = new long[EntityIdLength];
			for (int i = 0; i < EntityIdCount; i++)
			{
				array[i] = EntityId[i];
			}
			EntityId = array;
		}
		EntityId[EntityIdCount++] = value;
	}

	public int[] GetDespawnReason()
	{
		return DespawnReason;
	}

	public void SetDespawnReason(int[] value, int count, int length)
	{
		DespawnReason = value;
		DespawnReasonCount = count;
		DespawnReasonLength = length;
	}

	public void SetDespawnReason(int[] value)
	{
		DespawnReason = value;
		DespawnReasonCount = value.Length;
		DespawnReasonLength = value.Length;
	}

	public int GetDespawnReasonCount()
	{
		return DespawnReasonCount;
	}

	public void DespawnReasonAdd(int value)
	{
		if (DespawnReasonCount >= DespawnReasonLength)
		{
			if ((DespawnReasonLength *= 2) == 0)
			{
				DespawnReasonLength = 1;
			}
			int[] array = new int[DespawnReasonLength];
			for (int i = 0; i < DespawnReasonCount; i++)
			{
				array[i] = DespawnReason[i];
			}
			DespawnReason = array;
		}
		DespawnReason[DespawnReasonCount++] = value;
	}

	public int[] GetDeathDamageSource()
	{
		return DeathDamageSource;
	}

	public void SetDeathDamageSource(int[] value, int count, int length)
	{
		DeathDamageSource = value;
		DeathDamageSourceCount = count;
		DeathDamageSourceLength = length;
	}

	public void SetDeathDamageSource(int[] value)
	{
		DeathDamageSource = value;
		DeathDamageSourceCount = value.Length;
		DeathDamageSourceLength = value.Length;
	}

	public int GetDeathDamageSourceCount()
	{
		return DeathDamageSourceCount;
	}

	public void DeathDamageSourceAdd(int value)
	{
		if (DeathDamageSourceCount >= DeathDamageSourceLength)
		{
			if ((DeathDamageSourceLength *= 2) == 0)
			{
				DeathDamageSourceLength = 1;
			}
			int[] array = new int[DeathDamageSourceLength];
			for (int i = 0; i < DeathDamageSourceCount; i++)
			{
				array[i] = DeathDamageSource[i];
			}
			DeathDamageSource = array;
		}
		DeathDamageSource[DeathDamageSourceCount++] = value;
	}

	public long[] GetByEntityId()
	{
		return ByEntityId;
	}

	public void SetByEntityId(long[] value, int count, int length)
	{
		ByEntityId = value;
		ByEntityIdCount = count;
		ByEntityIdLength = length;
	}

	public void SetByEntityId(long[] value)
	{
		ByEntityId = value;
		ByEntityIdCount = value.Length;
		ByEntityIdLength = value.Length;
	}

	public int GetByEntityIdCount()
	{
		return ByEntityIdCount;
	}

	public void ByEntityIdAdd(long value)
	{
		if (ByEntityIdCount >= ByEntityIdLength)
		{
			if ((ByEntityIdLength *= 2) == 0)
			{
				ByEntityIdLength = 1;
			}
			long[] array = new long[ByEntityIdLength];
			for (int i = 0; i < ByEntityIdCount; i++)
			{
				array[i] = ByEntityId[i];
			}
			ByEntityId = array;
		}
		ByEntityId[ByEntityIdCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
