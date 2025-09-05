using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("ForageStatAffected", "Should the harvested stack amount be multiplied by the player's 'forageDropRate' stat?", "System.Boolean", "Optional", "False", true)]
public class BlockBehaviorHarvestable : BlockBehavior
{
	[DocumentAsJson("Recommended", "0", false)]
	private float harvestTime;

	[DocumentAsJson("Optional", "False", false)]
	private bool exchangeBlock;

	[DocumentAsJson("Required", "", false)]
	public BlockDropItemStack[] harvestedStacks;

	[DocumentAsJson("Optional", "sounds/block/leafy-picking", false)]
	public AssetLocation harvestingSound;

	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation harvestedBlockCode;

	private Block harvestedBlock;

	[DocumentAsJson("Optional", "blockhelp-harvetable-harvest", false)]
	private string interactionHelpCode;

	[DocumentAsJson("Required", "", false)]
	public BlockDropItemStack harvestedStack
	{
		get
		{
			return harvestedStacks[0];
		}
		set
		{
			harvestedStacks[0] = value;
		}
	}

	public BlockBehaviorHarvestable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		interactionHelpCode = properties["interactionHelpCode"].AsString("blockhelp-harvetable-harvest");
		harvestTime = properties["harvestTime"].AsFloat();
		harvestedStacks = properties["harvestedStacks"].AsObject<BlockDropItemStack[]>();
		BlockDropItemStack blockDropItemStack = properties["harvestedStack"].AsObject<BlockDropItemStack>();
		if (harvestedStacks == null && blockDropItemStack != null)
		{
			harvestedStacks = new BlockDropItemStack[1];
			harvestedStacks[0] = blockDropItemStack;
		}
		exchangeBlock = properties["exchangeBlock"].AsBool();
		string text = properties["harvestingSound"].AsString("game:sounds/block/leafy-picking");
		if (text != null)
		{
			harvestingSound = AssetLocation.Create(text, block.Code.Domain);
		}
		text = properties["harvestedBlockCode"].AsString();
		if (text != null)
		{
			harvestedBlockCode = AssetLocation.Create(text, block.Code.Domain);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		harvestedStacks.Foreach(delegate(BlockDropItemStack harvestedStack)
		{
			harvestedStack?.Resolve(api.World, "harvestedStack of block ", block.Code);
		});
		harvestedBlock = api.World.GetBlock(harvestedBlockCode);
		if (harvestedBlock == null)
		{
			api.World.Logger.Warning("Unable to resolve harvested block code '{0}' for block {1}. Will ignore.", harvestedBlockCode, block.Code);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		handling = EnumHandling.PreventDefault;
		if (harvestedStacks != null)
		{
			world.PlaySoundAt(harvestingSound, blockSel.Position, 0.0, byPlayer);
			return true;
		}
		return false;
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
	{
		if (blockSel == null)
		{
			return false;
		}
		handled = EnumHandling.PreventDefault;
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
		if (world.Rand.NextDouble() < 0.05)
		{
			world.PlaySoundAt(harvestingSound, blockSel.Position, 0.0, byPlayer);
		}
		if (world.Side == EnumAppSide.Client && world.Rand.NextDouble() < 0.25)
		{
			world.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(blockSel.HitPosition), harvestedStacks[0].ResolvedItemstack, 0.25f, 1, 0.5f, byPlayer, new Vec3f(0f, 1f, 0f));
		}
		if (world.Side != EnumAppSide.Client)
		{
			return secondsUsed < harvestTime;
		}
		return true;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (!(secondsUsed > harvestTime - 0.05f) || harvestedStacks == null || world.Side != EnumAppSide.Server)
		{
			return;
		}
		float dropRate = 1f;
		JsonObject attributes = block.Attributes;
		if (attributes != null && attributes.IsTrue("forageStatAffected"))
		{
			dropRate *= byPlayer.Entity.Stats.GetBlended("forageDropRate");
		}
		harvestedStacks.Foreach(delegate(BlockDropItemStack harvestedStack)
		{
			ItemStack nextItemStack = harvestedStack.GetNextItemStack(dropRate);
			if (nextItemStack != null)
			{
				ItemStack itemStack = nextItemStack.Clone();
				int stackSize = nextItemStack.StackSize;
				if (!byPlayer.InventoryManager.TryGiveItemstack(nextItemStack))
				{
					world.SpawnItemEntity(nextItemStack, blockSel.Position);
				}
				world.Logger.Audit("{0} Took {1}x{2} from {3} at {4}.", byPlayer.PlayerName, stackSize, nextItemStack.Collectible.Code, block.Code, blockSel.Position);
				TreeAttribute data = new TreeAttribute
				{
					["itemstack"] = new ItemstackAttribute(itemStack.Clone()),
					["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId)
				};
				world.Api.Event.PushEvent("onitemcollected", data);
			}
		});
		if (harvestedBlock != null)
		{
			if (!exchangeBlock)
			{
				world.BlockAccessor.SetBlock(harvestedBlock.BlockId, blockSel.Position);
			}
			else
			{
				world.BlockAccessor.ExchangeBlock(harvestedBlock.BlockId, blockSel.Position);
			}
		}
		world.PlaySoundAt(harvestingSound, blockSel.Position, 0.0, byPlayer);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
	{
		if (harvestedStacks != null)
		{
			bool flag = true;
			if (world.Claims != null && world is IClientWorldAccessor clientWorldAccessor)
			{
				IClientPlayer player = clientWorldAccessor.Player;
				if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Survival && world.Claims.TestAccess(clientWorldAccessor.Player, selection.Position, EnumBlockAccessFlags.Use) != EnumWorldAccessResponse.Granted)
				{
					flag = false;
				}
			}
			if (flag)
			{
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = interactionHelpCode,
						MouseButton = EnumMouseButton.Right
					}
				};
			}
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handled);
	}
}
