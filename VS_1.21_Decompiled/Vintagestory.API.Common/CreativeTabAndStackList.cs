using System.IO;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class CreativeTabAndStackList
{
	[DocumentAsJson]
	public string[] Tabs;

	[DocumentAsJson]
	public JsonItemStack[] Stacks;

	public void FromBytes(BinaryReader reader, IClassRegistryAPI registry)
	{
		Tabs = new string[reader.ReadInt32()];
		for (int i = 0; i < Tabs.Length; i++)
		{
			Tabs[i] = reader.ReadString();
		}
		Stacks = new JsonItemStack[reader.ReadInt32()];
		for (int j = 0; j < Stacks.Length; j++)
		{
			Stacks[j] = new JsonItemStack();
			Stacks[j].FromBytes(reader, registry);
		}
	}

	public void ToBytes(BinaryWriter writer, IClassRegistryAPI registry)
	{
		writer.Write(Tabs.Length);
		for (int i = 0; i < Tabs.Length; i++)
		{
			writer.Write(Tabs[i]);
		}
		writer.Write(Stacks.Length);
		for (int j = 0; j < Stacks.Length; j++)
		{
			Stacks[j].ToBytes(writer);
		}
	}
}
