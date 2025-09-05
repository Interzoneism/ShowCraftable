using System;

public class Packet_ClientSerializer
{
	private const int field = 8;

	public static Packet_Client DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Client packet_Client = new Packet_Client();
		DeserializeLengthDelimited(stream, packet_Client);
		return packet_Client;
	}

	public static Packet_Client DeserializeBuffer(byte[] buffer, int length, Packet_Client instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Client Deserialize(CitoMemoryStream stream, Packet_Client instance)
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
			case 266:
				if (instance.LoginTokenQuery == null)
				{
					instance.LoginTokenQuery = Packet_LoginTokenQuerySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LoginTokenQuerySerializer.DeserializeLengthDelimited(stream, instance.LoginTokenQuery);
				}
				break;
			case 8:
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				if (instance.Identification == null)
				{
					instance.Identification = Packet_ClientIdentificationSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientIdentificationSerializer.DeserializeLengthDelimited(stream, instance.Identification);
				}
				break;
			case 26:
				if (instance.BlockPlaceOrBreak == null)
				{
					instance.BlockPlaceOrBreak = Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimited(stream, instance.BlockPlaceOrBreak);
				}
				break;
			case 34:
				if (instance.Chatline == null)
				{
					instance.Chatline = Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance.Chatline);
				}
				break;
			case 42:
				if (instance.RequestJoin == null)
				{
					instance.RequestJoin = Packet_ClientRequestJoinSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientRequestJoinSerializer.DeserializeLengthDelimited(stream, instance.RequestJoin);
				}
				break;
			case 50:
				if (instance.PingReply == null)
				{
					instance.PingReply = Packet_ClientPingReplySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientPingReplySerializer.DeserializeLengthDelimited(stream, instance.PingReply);
				}
				break;
			case 58:
				if (instance.SpecialKey_ == null)
				{
					instance.SpecialKey_ = Packet_ClientSpecialKeySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientSpecialKeySerializer.DeserializeLengthDelimited(stream, instance.SpecialKey_);
				}
				break;
			case 66:
				if (instance.SelectedHotbarSlot == null)
				{
					instance.SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance.SelectedHotbarSlot);
				}
				break;
			case 74:
				if (instance.Leave == null)
				{
					instance.Leave = Packet_ClientLeaveSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientLeaveSerializer.DeserializeLengthDelimited(stream, instance.Leave);
				}
				break;
			case 82:
				if (instance.Query == null)
				{
					instance.Query = Packet_ClientServerQuerySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientServerQuerySerializer.DeserializeLengthDelimited(stream, instance.Query);
				}
				break;
			case 114:
				if (instance.MoveItemstack == null)
				{
					instance.MoveItemstack = Packet_MoveItemstackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MoveItemstackSerializer.DeserializeLengthDelimited(stream, instance.MoveItemstack);
				}
				break;
			case 122:
				if (instance.Flipitemstacks == null)
				{
					instance.Flipitemstacks = Packet_FlipItemstacksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_FlipItemstacksSerializer.DeserializeLengthDelimited(stream, instance.Flipitemstacks);
				}
				break;
			case 130:
				if (instance.EntityInteraction == null)
				{
					instance.EntityInteraction = Packet_EntityInteractionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityInteractionSerializer.DeserializeLengthDelimited(stream, instance.EntityInteraction);
				}
				break;
			case 146:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 154:
				if (instance.ActivateInventorySlot == null)
				{
					instance.ActivateInventorySlot = Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimited(stream, instance.ActivateInventorySlot);
				}
				break;
			case 162:
				if (instance.CreateItemstack == null)
				{
					instance.CreateItemstack = Packet_CreateItemstackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CreateItemstackSerializer.DeserializeLengthDelimited(stream, instance.CreateItemstack);
				}
				break;
			case 170:
				if (instance.RequestModeChange == null)
				{
					instance.RequestModeChange = Packet_PlayerModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance.RequestModeChange);
				}
				break;
			case 178:
				if (instance.MoveKeyChange == null)
				{
					instance.MoveKeyChange = Packet_MoveKeyChangeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MoveKeyChangeSerializer.DeserializeLengthDelimited(stream, instance.MoveKeyChange);
				}
				break;
			case 186:
				if (instance.BlockEntityPacket == null)
				{
					instance.BlockEntityPacket = Packet_BlockEntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntityPacketSerializer.DeserializeLengthDelimited(stream, instance.BlockEntityPacket);
				}
				break;
			case 250:
				if (instance.EntityPacket == null)
				{
					instance.EntityPacket = Packet_EntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance.EntityPacket);
				}
				break;
			case 194:
				if (instance.CustomPacket == null)
				{
					instance.CustomPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.CustomPacket);
				}
				break;
			case 202:
				if (instance.HandInteraction == null)
				{
					instance.HandInteraction = Packet_ClientHandInteractionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientHandInteractionSerializer.DeserializeLengthDelimited(stream, instance.HandInteraction);
				}
				break;
			case 210:
				if (instance.ToolMode == null)
				{
					instance.ToolMode = Packet_ToolModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ToolModeSerializer.DeserializeLengthDelimited(stream, instance.ToolMode);
				}
				break;
			case 218:
				if (instance.BlockDamage == null)
				{
					instance.BlockDamage = Packet_BlockDamageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance.BlockDamage);
				}
				break;
			case 226:
				if (instance.ClientPlaying == null)
				{
					instance.ClientPlaying = Packet_ClientPlayingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientPlayingSerializer.DeserializeLengthDelimited(stream, instance.ClientPlaying);
				}
				break;
			case 242:
				if (instance.InvOpenedClosed == null)
				{
					instance.InvOpenedClosed = Packet_InvOpenCloseSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InvOpenCloseSerializer.DeserializeLengthDelimited(stream, instance.InvOpenedClosed);
				}
				break;
			case 258:
				if (instance.RuntimeSetting == null)
				{
					instance.RuntimeSetting = Packet_RuntimeSettingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RuntimeSettingSerializer.DeserializeLengthDelimited(stream, instance.RuntimeSetting);
				}
				break;
			case 274:
				if (instance.UdpPacket == null)
				{
					instance.UdpPacket = Packet_UdpPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance.UdpPacket);
				}
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

	public static Packet_Client DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Client instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Client result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Client instance)
	{
		if (instance.LoginTokenQuery != null)
		{
			stream.WriteKey(33, 2);
			Packet_LoginTokenQuery loginTokenQuery = instance.LoginTokenQuery;
			Packet_LoginTokenQuerySerializer.GetSize(loginTokenQuery);
			Packet_LoginTokenQuerySerializer.SerializeWithSize(stream, loginTokenQuery);
		}
		if (instance.Id != 1)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Id);
		}
		if (instance.Identification != null)
		{
			stream.WriteByte(18);
			Packet_ClientIdentification identification = instance.Identification;
			Packet_ClientIdentificationSerializer.GetSize(identification);
			Packet_ClientIdentificationSerializer.SerializeWithSize(stream, identification);
		}
		if (instance.BlockPlaceOrBreak != null)
		{
			stream.WriteByte(26);
			Packet_ClientBlockPlaceOrBreak blockPlaceOrBreak = instance.BlockPlaceOrBreak;
			Packet_ClientBlockPlaceOrBreakSerializer.GetSize(blockPlaceOrBreak);
			Packet_ClientBlockPlaceOrBreakSerializer.SerializeWithSize(stream, blockPlaceOrBreak);
		}
		if (instance.Chatline != null)
		{
			stream.WriteByte(34);
			Packet_ChatLine chatline = instance.Chatline;
			Packet_ChatLineSerializer.GetSize(chatline);
			Packet_ChatLineSerializer.SerializeWithSize(stream, chatline);
		}
		if (instance.RequestJoin != null)
		{
			stream.WriteByte(42);
			Packet_ClientRequestJoin requestJoin = instance.RequestJoin;
			Packet_ClientRequestJoinSerializer.GetSize(requestJoin);
			Packet_ClientRequestJoinSerializer.SerializeWithSize(stream, requestJoin);
		}
		if (instance.PingReply != null)
		{
			stream.WriteByte(50);
			Packet_ClientPingReply pingReply = instance.PingReply;
			Packet_ClientPingReplySerializer.GetSize(pingReply);
			Packet_ClientPingReplySerializer.SerializeWithSize(stream, pingReply);
		}
		if (instance.SpecialKey_ != null)
		{
			stream.WriteByte(58);
			Packet_ClientSpecialKey specialKey_ = instance.SpecialKey_;
			Packet_ClientSpecialKeySerializer.GetSize(specialKey_);
			Packet_ClientSpecialKeySerializer.SerializeWithSize(stream, specialKey_);
		}
		if (instance.SelectedHotbarSlot != null)
		{
			stream.WriteByte(66);
			Packet_SelectedHotbarSlot selectedHotbarSlot = instance.SelectedHotbarSlot;
			Packet_SelectedHotbarSlotSerializer.GetSize(selectedHotbarSlot);
			Packet_SelectedHotbarSlotSerializer.SerializeWithSize(stream, selectedHotbarSlot);
		}
		if (instance.Leave != null)
		{
			stream.WriteByte(74);
			Packet_ClientLeave leave = instance.Leave;
			Packet_ClientLeaveSerializer.GetSize(leave);
			Packet_ClientLeaveSerializer.SerializeWithSize(stream, leave);
		}
		if (instance.Query != null)
		{
			stream.WriteByte(82);
			Packet_ClientServerQuery query = instance.Query;
			Packet_ClientServerQuerySerializer.GetSize(query);
			Packet_ClientServerQuerySerializer.SerializeWithSize(stream, query);
		}
		if (instance.MoveItemstack != null)
		{
			stream.WriteByte(114);
			Packet_MoveItemstack moveItemstack = instance.MoveItemstack;
			Packet_MoveItemstackSerializer.GetSize(moveItemstack);
			Packet_MoveItemstackSerializer.SerializeWithSize(stream, moveItemstack);
		}
		if (instance.Flipitemstacks != null)
		{
			stream.WriteByte(122);
			Packet_FlipItemstacks flipitemstacks = instance.Flipitemstacks;
			Packet_FlipItemstacksSerializer.GetSize(flipitemstacks);
			Packet_FlipItemstacksSerializer.SerializeWithSize(stream, flipitemstacks);
		}
		if (instance.EntityInteraction != null)
		{
			stream.WriteKey(16, 2);
			Packet_EntityInteraction entityInteraction = instance.EntityInteraction;
			Packet_EntityInteractionSerializer.GetSize(entityInteraction);
			Packet_EntityInteractionSerializer.SerializeWithSize(stream, entityInteraction);
		}
		if (instance.EntityPosition != null)
		{
			stream.WriteKey(18, 2);
			Packet_EntityPosition entityPosition = instance.EntityPosition;
			Packet_EntityPositionSerializer.GetSize(entityPosition);
			Packet_EntityPositionSerializer.SerializeWithSize(stream, entityPosition);
		}
		if (instance.ActivateInventorySlot != null)
		{
			stream.WriteKey(19, 2);
			Packet_ActivateInventorySlot activateInventorySlot = instance.ActivateInventorySlot;
			Packet_ActivateInventorySlotSerializer.GetSize(activateInventorySlot);
			Packet_ActivateInventorySlotSerializer.SerializeWithSize(stream, activateInventorySlot);
		}
		if (instance.CreateItemstack != null)
		{
			stream.WriteKey(20, 2);
			Packet_CreateItemstack createItemstack = instance.CreateItemstack;
			Packet_CreateItemstackSerializer.GetSize(createItemstack);
			Packet_CreateItemstackSerializer.SerializeWithSize(stream, createItemstack);
		}
		if (instance.RequestModeChange != null)
		{
			stream.WriteKey(21, 2);
			Packet_PlayerMode requestModeChange = instance.RequestModeChange;
			Packet_PlayerModeSerializer.GetSize(requestModeChange);
			Packet_PlayerModeSerializer.SerializeWithSize(stream, requestModeChange);
		}
		if (instance.MoveKeyChange != null)
		{
			stream.WriteKey(22, 2);
			Packet_MoveKeyChange moveKeyChange = instance.MoveKeyChange;
			Packet_MoveKeyChangeSerializer.GetSize(moveKeyChange);
			Packet_MoveKeyChangeSerializer.SerializeWithSize(stream, moveKeyChange);
		}
		if (instance.BlockEntityPacket != null)
		{
			stream.WriteKey(23, 2);
			Packet_BlockEntityPacket blockEntityPacket = instance.BlockEntityPacket;
			Packet_BlockEntityPacketSerializer.GetSize(blockEntityPacket);
			Packet_BlockEntityPacketSerializer.SerializeWithSize(stream, blockEntityPacket);
		}
		if (instance.EntityPacket != null)
		{
			stream.WriteKey(31, 2);
			Packet_EntityPacket entityPacket = instance.EntityPacket;
			Packet_EntityPacketSerializer.GetSize(entityPacket);
			Packet_EntityPacketSerializer.SerializeWithSize(stream, entityPacket);
		}
		if (instance.CustomPacket != null)
		{
			stream.WriteKey(24, 2);
			Packet_CustomPacket customPacket = instance.CustomPacket;
			Packet_CustomPacketSerializer.GetSize(customPacket);
			Packet_CustomPacketSerializer.SerializeWithSize(stream, customPacket);
		}
		if (instance.HandInteraction != null)
		{
			stream.WriteKey(25, 2);
			Packet_ClientHandInteraction handInteraction = instance.HandInteraction;
			Packet_ClientHandInteractionSerializer.GetSize(handInteraction);
			Packet_ClientHandInteractionSerializer.SerializeWithSize(stream, handInteraction);
		}
		if (instance.ToolMode != null)
		{
			stream.WriteKey(26, 2);
			Packet_ToolMode toolMode = instance.ToolMode;
			Packet_ToolModeSerializer.GetSize(toolMode);
			Packet_ToolModeSerializer.SerializeWithSize(stream, toolMode);
		}
		if (instance.BlockDamage != null)
		{
			stream.WriteKey(27, 2);
			Packet_BlockDamage blockDamage = instance.BlockDamage;
			Packet_BlockDamageSerializer.GetSize(blockDamage);
			Packet_BlockDamageSerializer.SerializeWithSize(stream, blockDamage);
		}
		if (instance.ClientPlaying != null)
		{
			stream.WriteKey(28, 2);
			Packet_ClientPlaying clientPlaying = instance.ClientPlaying;
			Packet_ClientPlayingSerializer.GetSize(clientPlaying);
			Packet_ClientPlayingSerializer.SerializeWithSize(stream, clientPlaying);
		}
		if (instance.InvOpenedClosed != null)
		{
			stream.WriteKey(30, 2);
			Packet_InvOpenClose invOpenedClosed = instance.InvOpenedClosed;
			Packet_InvOpenCloseSerializer.GetSize(invOpenedClosed);
			Packet_InvOpenCloseSerializer.SerializeWithSize(stream, invOpenedClosed);
		}
		if (instance.RuntimeSetting != null)
		{
			stream.WriteKey(32, 2);
			Packet_RuntimeSetting runtimeSetting = instance.RuntimeSetting;
			Packet_RuntimeSettingSerializer.GetSize(runtimeSetting);
			Packet_RuntimeSettingSerializer.SerializeWithSize(stream, runtimeSetting);
		}
		if (instance.UdpPacket != null)
		{
			stream.WriteKey(34, 2);
			Packet_UdpPacket udpPacket = instance.UdpPacket;
			Packet_UdpPacketSerializer.GetSize(udpPacket);
			Packet_UdpPacketSerializer.SerializeWithSize(stream, udpPacket);
		}
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Client instance)
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

	public static byte[] SerializeToBytes(Packet_Client instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Client instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
