using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class AuctionActionResponsePacket
{
	public string ErrorCode;

	public EnumAuctionAction Action;

	public long AuctionId;

	public long AtAuctioneerEntityId;

	public bool MoneyReceived;

	public int Price;
}
