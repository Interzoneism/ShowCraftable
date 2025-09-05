using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public static class StackConverter
{
	public static ItemStack FromPacket(Packet_ItemStack fromPacket, IWorldAccessor resolver)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		if (fromPacket.Attributes != null)
		{
			BinaryReader stream = new BinaryReader(new MemoryStream(fromPacket.Attributes));
			treeAttribute.FromBytes(stream);
		}
		return new ItemStack(fromPacket.ItemId, (EnumItemClass)fromPacket.ItemClass, fromPacket.StackSize, treeAttribute, resolver);
	}

	public static Packet_ItemStack ToPacket(ItemStack stack)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter stream = new BinaryWriter(memoryStream);
		stack.Attributes.ToBytes(stream);
		return new Packet_ItemStack
		{
			ItemClass = (int)stack.Class,
			ItemId = stack.Id,
			StackSize = stack.StackSize,
			Attributes = memoryStream.ToArray()
		};
	}
}
