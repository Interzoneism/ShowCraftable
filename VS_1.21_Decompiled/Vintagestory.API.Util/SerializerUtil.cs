using System.IO;
using ProtoBuf;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Util;

public static class SerializerUtil
{
	public delegate void ByteWriteDelegatae(BinaryWriter writer);

	public delegate void ByteReadDelegatae(BinaryReader reader);

	public static readonly byte[] SerializedOne;

	public static readonly byte[] SerializedZero;

	static SerializerUtil()
	{
		SerializedOne = Serialize(1);
		SerializedZero = Serialize(0);
	}

	public static byte[] Serialize<T>(T data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		Serializer.Serialize<T>((Stream)memoryStream, data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize<T>(T data, FastMemoryStream ms)
	{
		ms.Reset();
		Serializer.Serialize<T>((Stream)ms, data);
		return ms.ToArray();
	}

	public static T Deserialize<T>(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		return Serializer.Deserialize<T>((Stream)memoryStream);
	}

	public static T DeserializeInto<T>(T instance, byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		return Serializer.Merge<T>((Stream)memoryStream, instance);
	}

	public static T Deserialize<T>(byte[] data, T defaultValue)
	{
		if (data == null)
		{
			return defaultValue;
		}
		using MemoryStream memoryStream = new MemoryStream(data);
		return Serializer.Deserialize<T>((Stream)memoryStream);
	}

	public static byte[] ToBytes(ByteWriteDelegatae toWrite)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter writer = new BinaryWriter(memoryStream))
		{
			toWrite(writer);
		}
		return memoryStream.ToArray();
	}

	public static void FromBytes(byte[] data, ByteReadDelegatae toRead)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader reader = new BinaryReader(input);
		toRead(reader);
	}
}
