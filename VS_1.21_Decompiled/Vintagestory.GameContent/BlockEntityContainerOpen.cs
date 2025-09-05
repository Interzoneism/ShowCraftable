using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityContainerOpen
{
	public string BlockEntity;

	public string DialogTitle;

	public byte Columns;

	public TreeAttribute Tree;

	public static byte[] ToBytes(string entityName, string dialogTitle, byte columns, InventoryBase inventory)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(entityName);
		binaryWriter.Write(dialogTitle);
		binaryWriter.Write(columns);
		TreeAttribute treeAttribute = new TreeAttribute();
		inventory.ToTreeAttributes(treeAttribute);
		treeAttribute.ToBytes(binaryWriter);
		return memoryStream.ToArray();
	}

	public static BlockEntityContainerOpen FromBytes(byte[] data)
	{
		BlockEntityContainerOpen blockEntityContainerOpen = new BlockEntityContainerOpen();
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		blockEntityContainerOpen.BlockEntity = binaryReader.ReadString();
		blockEntityContainerOpen.DialogTitle = binaryReader.ReadString();
		blockEntityContainerOpen.Columns = binaryReader.ReadByte();
		blockEntityContainerOpen.Tree = new TreeAttribute();
		blockEntityContainerOpen.Tree.FromBytes(binaryReader);
		return blockEntityContainerOpen;
	}
}
