using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorExchangeOnInteract : BlockBehavior
{
	[DocumentAsJson("Required", "", false)]
	private AssetLocation[] blockCodes;

	[DocumentAsJson("Required", "", false)]
	private string sound;

	[DocumentAsJson("Required", "", false)]
	private string actionlangcode;

	public BlockBehaviorExchangeOnInteract(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		string[] array = properties["exchangeStates"].AsArray<string>();
		blockCodes = new AssetLocation[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			blockCodes[i] = AssetLocation.Create(array[i], block.Code.Domain);
		}
		sound = properties["sound"].AsString();
		actionlangcode = properties["actionLangCode"].AsString();
		base.Initialize(properties);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		handling = EnumHandling.PreventDefault;
		return DoExchange(world, byPlayer, blockSel.Position);
	}

	private bool DoExchange(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		int num = -1;
		for (int i = 0; i < blockCodes.Length; i++)
		{
			if (base.block.WildCardMatch(blockCodes[i]))
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			return false;
		}
		AssetLocation blockCode = base.block.Code.WildCardReplace(blockCodes[num], blockCodes[(num + 1) % blockCodes.Length]);
		Block block = world.GetBlock(blockCode);
		if (block == null)
		{
			return false;
		}
		world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
		if (sound != null)
		{
			world.PlaySoundAt(new AssetLocation("sounds/" + sound), pos, 0.0, byPlayer);
		}
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
		if (activationArgs == null || !activationArgs.HasAttribute("opened") || activationArgs.GetBool("opened") != block.Code.Path.Contains("opened"))
		{
			DoExchange(world, caller.Player, blockSel.Position);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = actionlangcode,
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
