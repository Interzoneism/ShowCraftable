using System.Text;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorReinforcable : BlockBehavior
{
	public BlockBehaviorReinforcable(Block block)
		: base(block)
	{
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		if (byPlayer == null)
		{
			return;
		}
		ModSystemBlockReinforcement modSystem = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		BlockReinforcement reinforcment = modSystem.GetReinforcment(pos);
		if (reinforcment != null && reinforcment.Strength > 0)
		{
			handling = EnumHandling.PreventDefault;
			world.PlaySoundAt(new AssetLocation("sounds/tool/breakreinforced"), pos, 0.0, byPlayer);
			if (!byPlayer.HasPrivilege("denybreakreinforced"))
			{
				modSystem.ConsumeStrength(pos, 1);
				world.BlockAccessor.MarkBlockDirty(pos);
			}
		}
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
	{
		ModSystemBlockReinforcement modSystem = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		BlockReinforcement reinforcment = modSystem.GetReinforcment(pos);
		if (reinforcment != null && reinforcment.Strength > 0)
		{
			modSystem.ConsumeStrength(pos, 2);
			world.BlockAccessor.MarkBlockDirty(pos);
			handling = EnumHandling.PreventDefault;
		}
		else
		{
			base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
		}
	}

	public override float GetMiningSpeedModifier(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		BlockReinforcement reinforcment = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().GetReinforcment(pos);
		if (reinforcment != null && reinforcment.Strength > 0 && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return 0.6f;
		}
		return 1f;
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side == EnumAppSide.Server)
		{
			world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().ClearReinforcement(pos);
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		ModSystemBlockReinforcement modSystem = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		if (modSystem != null)
		{
			BlockReinforcement reinforcment = modSystem.GetReinforcment(pos);
			if (reinforcment == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (reinforcment.GroupUid != 0)
			{
				stringBuilder.AppendLine(Lang.Get(reinforcment.Locked ? "Has been locked and reinforced by group {0}." : "Has been reinforced by group {0}.", reinforcment.LastGroupname));
			}
			else
			{
				stringBuilder.AppendLine(Lang.Get(reinforcment.Locked ? "Has been locked and reinforced by {0}." : "Has been reinforced by {0}.", reinforcment.LastPlayername));
			}
			stringBuilder.AppendLine(Lang.Get("Strength: {0}", reinforcment.Strength));
			return stringBuilder.ToString();
		}
		return null;
	}

	public static bool AllowRightClickPickup(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		BlockReinforcement reinforcment = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().GetReinforcment(pos);
		if (reinforcment != null && reinforcment.Strength > 0)
		{
			return false;
		}
		return true;
	}
}
