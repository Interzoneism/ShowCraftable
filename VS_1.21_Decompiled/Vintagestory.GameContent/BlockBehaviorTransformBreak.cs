using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("TransformIntoBlock", "The asset location of the block to replace this one with.", "Vintagestory.API.Common.AssetLocation", "Required", "", false)]
public class BlockBehaviorTransformBreak : BlockBehavior
{
	private Block transformIntoBlock;

	private JsonObject properties;

	[DocumentAsJson("Optional", "False", false)]
	private bool withDrops;

	public BlockBehaviorTransformBreak(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (!properties["transformIntoBlock"].Exists)
		{
			api.Logger.Error("Block {0}, required property transformIntoBlock does not exist", block.Code);
			return;
		}
		AssetLocation assetLocation = AssetLocation.Create(properties["transformIntoBlock"].AsString(), block.Code.Domain);
		transformIntoBlock = api.World.GetBlock(assetLocation);
		if (transformIntoBlock == null)
		{
			api.Logger.Error("Block {0}, transformIntoBlock code '{1}' - no such block exists. Block will not transform upon breakage.", block.Code, assetLocation);
		}
		else
		{
			withDrops = properties["withDrops"].AsBool();
		}
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		this.properties = properties;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		if (transformIntoBlock != null)
		{
			handling = EnumHandling.PreventDefault;
			world.BlockAccessor.SetBlock(transformIntoBlock.Id, pos);
			if (withDrops)
			{
				spawnDrops(world, pos, byPlayer);
			}
			block.SpawnBlockBrokenParticles(pos, byPlayer);
		}
	}

	private void spawnDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		if (world.Side != EnumAppSide.Server || (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative))
		{
			return;
		}
		ItemStack[] drops = block.GetDrops(world, pos, byPlayer);
		if (drops != null)
		{
			for (int i = 0; i < drops.Length; i++)
			{
				if (block.SplitDropStacks)
				{
					for (int j = 0; j < drops[i].StackSize; j++)
					{
						ItemStack itemStack = drops[i].Clone();
						itemStack.StackSize = 1;
						world.SpawnItemEntity(itemStack, pos);
					}
				}
				else
				{
					world.SpawnItemEntity(drops[i].Clone(), pos);
				}
			}
		}
		world.PlaySoundAt(block.Sounds?.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
	}
}
