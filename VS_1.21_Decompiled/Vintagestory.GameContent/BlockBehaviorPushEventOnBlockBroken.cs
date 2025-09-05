using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorPushEventOnBlockBroken : BlockBehavior
{
	[DocumentAsJson("Required", "", false)]
	private string eventName;

	public BlockBehaviorPushEventOnBlockBroken(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		eventName = properties["eventName"]?.AsString();
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (byPlayer != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetInt("x", pos.X);
			treeAttribute.SetInt("y", pos.Y);
			treeAttribute.SetInt("z", pos.Z);
			world.Api.Event.PushEvent(eventName, treeAttribute);
		}
	}
}
