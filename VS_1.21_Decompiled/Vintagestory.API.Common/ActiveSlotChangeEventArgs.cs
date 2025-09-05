namespace Vintagestory.API.Common;

public class ActiveSlotChangeEventArgs
{
	public int FromSlot { get; }

	public int ToSlot { get; }

	public ActiveSlotChangeEventArgs(int from, int to)
	{
		FromSlot = from;
		ToSlot = to;
	}
}
