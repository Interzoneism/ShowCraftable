using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class JournalEntryOld
{
	public int EntryId;

	public string LoreCode;

	public string Title;

	public bool Editable;

	public List<JournalChapterOld> Chapters = new List<JournalChapterOld>();
}
