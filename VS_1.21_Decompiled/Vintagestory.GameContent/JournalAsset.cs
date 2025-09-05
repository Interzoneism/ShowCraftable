using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class JournalAsset
{
	public string Code;

	public string Title;

	public string[] Pieces;

	public string Category;
}
