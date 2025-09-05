using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class JournalOld
{
	public List<JournalEntryOld> Entries = new List<JournalEntryOld>();
}
