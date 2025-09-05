using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockUpdate
{
	public bool ExchangeOnly;

	public BlockPos Pos;

	public int OldBlockId;

	public int OldFluidBlockId;

	public int NewSolidBlockId = -1;

	public int NewFluidBlockId = -1;

	public ItemStack ByStack;

	public byte[] OldBlockEntityData;

	public byte[] NewBlockEntityData;

	public List<DecorUpdate> Decors;

	public List<DecorUpdate> OldDecors;
}
