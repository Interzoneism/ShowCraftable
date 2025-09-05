using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server;

public class ServerPackets
{
	public static Packet_Server IngameError(string code, string text, params object[] langargs)
	{
		Packet_Server packet_Server = new Packet_Server();
		packet_Server.Id = 68;
		packet_Server.IngameError = new Packet_IngameError
		{
			Message = text,
			Code = code
		};
		if (langargs != null)
		{
			packet_Server.IngameError.SetLangParams(langargs.Select((object e) => (e != null) ? e.ToString() : "").ToArray());
		}
		return packet_Server;
	}

	public static Packet_Server IngameDiscovery(string code, string text, params object[] langargs)
	{
		Packet_Server packet_Server = new Packet_Server();
		packet_Server.Id = 69;
		packet_Server.IngameDiscovery = new Packet_IngameDiscovery
		{
			Message = text,
			Code = code
		};
		if (langargs != null)
		{
			packet_Server.IngameDiscovery.SetLangParams(langargs.Select((object e) => (e != null) ? e.ToString() : "").ToArray());
		}
		return packet_Server;
	}

	public static Packet_Server ChatLine(int groupid, string text, EnumChatType chatType, string data)
	{
		return new Packet_Server
		{
			Id = 8,
			Chatline = new Packet_ChatLine
			{
				Message = text,
				Groupid = groupid,
				ChatType = (int)chatType,
				Data = data
			}
		};
	}

	public static Packet_Server LevelInitialize(int maxViewDistance)
	{
		return new Packet_Server
		{
			Id = 4,
			LevelInitialize = new Packet_ServerLevelInitialize
			{
				ServerChunkSize = MagicNum.ServerChunkSize,
				ServerMapChunkSize = MagicNum.ServerChunkSize,
				ServerMapRegionSize = MagicNum.MapRegionSize,
				MaxViewDistance = maxViewDistance
			}
		};
	}

	public static Packet_Server LevelFinalize()
	{
		return new Packet_Server
		{
			Id = 6,
			LevelFinalize = new Packet_ServerLevelFinalize()
		};
	}

	public static byte[] Serialize(Packet_Server packet, IntRef retLength)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerSerializer.Serialize(citoMemoryStream, packet);
		byte[] result = citoMemoryStream.ToArray();
		retLength.value = citoMemoryStream.Position();
		return result;
	}

	internal static Packet_Server Ping()
	{
		return new Packet_Server
		{
			Id = 2,
			Ping = new Packet_ServerPing()
		};
	}

	internal static Packet_Server DisconnectPlayer(string disconnectReason)
	{
		Packet_Server packet_Server = new Packet_Server();
		packet_Server.Id = 9;
		packet_Server.DisconnectPlayer = new Packet_ServerDisconnectPlayer();
		packet_Server.DisconnectPlayer.DisconnectReason = disconnectReason;
		return packet_Server;
	}

	internal static Packet_Server AnswerQuery(Packet_ServerQueryAnswer answer)
	{
		return new Packet_Server
		{
			Id = 28,
			QueryAnswer = answer
		};
	}

	public static Packet_EntityAttributes GetEntityPacket(FastMemoryStream ms, Entity entity)
	{
		BinaryWriter writer = new BinaryWriter(ms);
		Packet_EntityAttributes obj = new Packet_EntityAttributes
		{
			EntityId = entity.EntityId
		};
		entity.ToBytes(writer, forClient: true);
		obj.Data = ms.ToArray();
		return obj;
	}

	public static EntityTagPacket GetEntityTagPacket(Entity entity)
	{
		return new EntityTagPacket
		{
			TagsBitmask1 = (long)entity.Tags.BitMask1,
			TagsBitmask2 = (long)entity.Tags.BitMask2
		};
	}

	public static Packet_EntityAttributes GetEntityDebugAttributePacket(FastMemoryStream ms, Entity entity)
	{
		BinaryWriter stream = new BinaryWriter(ms);
		Packet_EntityAttributes obj = new Packet_EntityAttributes
		{
			EntityId = entity.EntityId
		};
		entity.DebugAttributes.ToBytes(stream);
		obj.Data = ms.ToArray();
		return obj;
	}

	public static Packet_EntityAttributeUpdate GetEntityPartialAttributePacket(FastMemoryStream ms, Entity entity)
	{
		Packet_EntityAttributeUpdate packet_EntityAttributeUpdate = new Packet_EntityAttributeUpdate();
		entity.WatchedAttributes.GetDirtyPathData(ms, out var paths, out var dirtydata);
		Packet_PartialAttribute[] array = new Packet_PartialAttribute[paths.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Packet_PartialAttribute
			{
				Path = paths[i],
				Data = dirtydata[i]
			};
		}
		packet_EntityAttributeUpdate.SetAttributes(array);
		packet_EntityAttributeUpdate.EntityId = entity.EntityId;
		return packet_EntityAttributeUpdate;
	}

	public static Packet_Server GetBulkEntityAttributesPacket(List<Packet_EntityAttributes> fullPackets, List<Packet_EntityAttributeUpdate> partialPackets)
	{
		Packet_BulkEntityAttributes packet_BulkEntityAttributes = new Packet_BulkEntityAttributes();
		packet_BulkEntityAttributes.SetFullUpdates(fullPackets.ToArray());
		packet_BulkEntityAttributes.SetPartialUpdates(partialPackets.ToArray());
		return new Packet_Server
		{
			Id = 60,
			BulkEntityAttributes = packet_BulkEntityAttributes
		};
	}

	public static Packet_Server GetBulkEntityDebugAttributesPacket(List<Packet_EntityAttributes> fullPackets)
	{
		Packet_BulkEntityDebugAttributes packet_BulkEntityDebugAttributes = new Packet_BulkEntityDebugAttributes();
		packet_BulkEntityDebugAttributes.SetFullUpdates(fullPackets.ToArray());
		return new Packet_Server
		{
			Id = 62,
			BulkEntityDebugAttributes = packet_BulkEntityDebugAttributes
		};
	}

	public static Packet_Server GetEntityAttributesPacket(Entity entity)
	{
		Packet_EntityAttributes packet_EntityAttributes = new Packet_EntityAttributes();
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);
		entity.ToBytes(writer, forClient: true);
		packet_EntityAttributes.SetData(memoryStream.ToArray());
		packet_EntityAttributes.EntityId = entity.EntityId;
		return new Packet_Server
		{
			Id = 37,
			EntityAttributes = packet_EntityAttributes
		};
	}

	public static Packet_Server GetEntityAttributesUpdatePacket(Entity entity)
	{
		Packet_EntityAttributeUpdate packet_EntityAttributeUpdate = new Packet_EntityAttributeUpdate();
		entity.WatchedAttributes.GetDirtyPathData(out var paths, out var dirtydata);
		Packet_PartialAttribute[] array = new Packet_PartialAttribute[paths.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Packet_PartialAttribute();
			array[i].Path = paths[i];
			array[i].SetData(dirtydata[i]);
		}
		packet_EntityAttributeUpdate.SetAttributes(array);
		packet_EntityAttributeUpdate.EntityId = entity.EntityId;
		return new Packet_Server
		{
			Id = 38,
			EntityAttributeUpdate = packet_EntityAttributeUpdate
		};
	}

	public static Packet_Server GetFullEntityPacket(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		return new Packet_Server
		{
			Id = 33,
			Entity = GetEntityPacket(entity, ms, writer)
		};
	}

	public static Packet_Server GetEntityDespawnPacket(List<EntityDespawn> despawns)
	{
		long[] entityId = despawns.Select((EntityDespawn item) => item.EntityId).ToArray();
		int[] despawnReason = despawns.Select((EntityDespawn item) => (int)((item.DespawnData != null) ? item.DespawnData.Reason : EnumDespawnReason.Death)).ToArray();
		int[] deathDamageSource = despawns.Select((EntityDespawn item) => (int)((item.DespawnData != null) ? ((item.DespawnData.DamageSourceForDeath != null) ? item.DespawnData.DamageSourceForDeath.Source : EnumDamageSource.Unknown) : EnumDamageSource.Block)).ToArray();
		Packet_EntityDespawn packet_EntityDespawn = new Packet_EntityDespawn();
		packet_EntityDespawn.SetEntityId(entityId);
		packet_EntityDespawn.SetDeathDamageSource(deathDamageSource);
		packet_EntityDespawn.SetDespawnReason(despawnReason);
		return new Packet_Server
		{
			Id = 36,
			EntityDespawn = packet_EntityDespawn
		};
	}

	public static Packet_Server GetEntitySpawnPacket(List<Entity> spawns)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return GetEntitySpawnPacket(spawns, ms);
	}

	public static Packet_Server GetEntitySpawnPacket(List<Entity> spawns, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter writer = new BinaryWriter(ms);
		Packet_Entity[] array = new Packet_Entity[spawns.Count];
		int num = 0;
		for (int i = 0; i < array.Length && i + num < spawns.Count; i++)
		{
			Entity entity = spawns[i + num];
			while (!entity.Alive && ++num + i < spawns.Count)
			{
				entity = spawns[i + num];
			}
			if (i + num >= spawns.Count)
			{
				break;
			}
			int num2 = i;
			Entity entity2 = entity;
			array[num2] = (Packet_Entity)(entity2.packet ?? (entity2.packet = GetEntityPacket(entity, ms, writer)));
		}
		return new Packet_Server
		{
			Id = 34,
			EntitySpawn = new Packet_EntitySpawn
			{
				Entity = array,
				EntityCount = array.Length - num,
				EntityLength = array.Length - num
			}
		};
	}

	public static Packet_Entity GetEntityPacket(Entity entity)
	{
		using FastMemoryStream fastMemoryStream = new FastMemoryStream();
		BinaryWriter writer = new BinaryWriter(fastMemoryStream);
		return new Packet_Entity
		{
			EntityId = entity.EntityId,
			Data = getEntityDataForClient(entity, fastMemoryStream, writer),
			EntityType = entity.Code.ToShortString(),
			SimulationRange = entity.SimulationRange
		};
	}

	public static Packet_Entity GetEntityPacket(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		return new Packet_Entity
		{
			EntityId = entity.EntityId,
			Data = getEntityDataForClient(entity, ms, writer),
			EntityType = entity.Code.ToShortString(),
			SimulationRange = entity.SimulationRange
		};
	}

	public static Packet_EntityPosition getEntityPositionPacket(EntityPos pos, Entity entity, int tick)
	{
		Packet_EntityPosition packet_EntityPosition = new Packet_EntityPosition
		{
			EntityId = entity.EntityId,
			X = CollectibleNet.SerializeDoublePrecise(pos.X),
			Y = CollectibleNet.SerializeDoublePrecise(pos.Y),
			Z = CollectibleNet.SerializeDoublePrecise(pos.Z),
			Yaw = CollectibleNet.SerializeFloatPrecise(pos.Yaw),
			Pitch = CollectibleNet.SerializeFloatPrecise(pos.Pitch),
			Roll = CollectibleNet.SerializeFloatPrecise(pos.Roll),
			MotionX = CollectibleNet.SerializeDoublePrecise(pos.Motion.X),
			MotionY = CollectibleNet.SerializeDoublePrecise(pos.Motion.Y),
			MotionZ = CollectibleNet.SerializeDoublePrecise(pos.Motion.Z),
			Teleport = entity.IsTeleport,
			HeadYaw = CollectibleNet.SerializeFloatPrecise(pos.HeadYaw),
			HeadPitch = CollectibleNet.SerializeFloatPrecise(pos.HeadPitch),
			PositionVersion = entity.WatchedAttributes.GetInt("positionVersionNumber"),
			Tick = tick,
			TagsBitmask1 = (long)entity.Tags.BitMask1,
			TagsBitmask2 = (long)entity.Tags.BitMask2
		};
		if (entity is EntityAgent entityAgent)
		{
			packet_EntityPosition.BodyYaw = CollectibleNet.SerializeFloatPrecise(entityAgent.BodyYaw);
			packet_EntityPosition.Controls = entityAgent.Controls.ToInt();
		}
		EntityControls entityControls = ((entity.SidedProperties == null) ? null : entity.GetInterface<IMountable>()?.ControllingControls);
		if (entityControls != null)
		{
			packet_EntityPosition.MountControls = entityControls.ToInt();
		}
		return packet_EntityPosition;
	}

	public static byte[] getEntityDataForClient(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		ms.Reset();
		entity.ToBytes(writer, forClient: true);
		return ms.ToArray();
	}

	internal static Packet_BlockEntity getBlockEntityPacket(BlockEntity blockEntity, string classname, FastMemoryStream ms, BinaryWriter writer)
	{
		return new Packet_BlockEntity
		{
			Classname = classname,
			Data = getBlockEntityData(blockEntity, ms, writer),
			PosX = blockEntity.Pos.X,
			PosY = blockEntity.Pos.InternalY,
			PosZ = blockEntity.Pos.Z
		};
	}

	private static byte[] getBlockEntityData(BlockEntity blockEntity, FastMemoryStream ms, BinaryWriter writer)
	{
		ms.Reset();
		TreeAttribute treeAttribute = new TreeAttribute();
		blockEntity.ToTreeAttributes(treeAttribute);
		treeAttribute.ToBytes(writer);
		return ms.ToArray();
	}
}
