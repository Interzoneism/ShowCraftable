using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorHeatSource : BlockBehavior, IHeatSource
{
	[DocumentAsJson("Recommended", "0", false)]
	private float heatStrength;

	public BlockBehaviorHeatSource(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		heatStrength = properties["heatStrength"].AsFloat();
		base.Initialize(properties);
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		if (block.EntityClass != null && world.BlockAccessor.GetBlockEntity(heatSourcePos) is IHeatSource heatSource)
		{
			return heatSource.GetHeatStrength(world, heatSourcePos, heatReceiverPos);
		}
		return heatStrength;
	}
}
