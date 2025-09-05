using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class AuctionlistPacket
{
	public bool IsFullUpdate;

	public Auction[] NewAuctions;

	public long[] RemovedAuctions;

	public float TraderDebt;
}
