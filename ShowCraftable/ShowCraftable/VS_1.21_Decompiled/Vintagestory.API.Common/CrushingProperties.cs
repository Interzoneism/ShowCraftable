using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class CrushingProperties
{
	[DocumentAsJson]
	public JsonItemStack CrushedStack;

	[DocumentAsJson]
	public int HardnessTier = 1;

	[DocumentAsJson]
	public NatFloat Quantity = NatFloat.One;

	public CrushingProperties Clone()
	{
		return new CrushingProperties
		{
			CrushedStack = CrushedStack.Clone(),
			HardnessTier = HardnessTier,
			Quantity = Quantity.Clone()
		};
	}
}
