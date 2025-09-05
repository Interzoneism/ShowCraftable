using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityConditional : BlockEntityGuiConfigurableCommands, IWrenchOrientable
{
	private BlockFacing facing = BlockFacing.EAST;

	private int prevState;

	private bool Latching => Silent;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		facing = BlockFacing.FromCode(base.Block.Code.EndVariant()) ?? BlockFacing.EAST;
	}

	public override void Execute(Caller caller, string commands)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			int num = EvaluateConditionAsTrue();
			if (num != 0 && (!Latching || num != prevState))
			{
				prevState = num;
				BlockPos blockPos = ((num == 2) ? Pos.AddCopy(facing) : Pos.AddCopy(facing.GetCCW()));
				Api.World.BlockAccessor.GetBlock(blockPos).Activate(Api.World, getCaller(), new BlockSelection
				{
					Position = blockPos
				});
			}
		}
	}

	private int EvaluateConditionAsTrue()
	{
		string text = Commands.Trim();
		TextCommandCallingArgs args = new TextCommandCallingArgs
		{
			Caller = getCaller(),
			RawArgs = new CmdArgs(text)
		};
		if (text.StartsWith("isBlock"))
		{
			ICommandArgumentParser commandArgumentParser = new IsBlockArgParser("cond", Api, isMandatoryArg: true);
			if (commandArgumentParser.TryProcess(args) != EnumParseResult.Good)
			{
				return 0;
			}
			if (!(bool)commandArgumentParser.GetValue())
			{
				return 1;
			}
			return 2;
		}
		ICommandArgumentParser commandArgumentParser2 = new EntitiesArgParser("cond", Api, isMandatoryArg: true);
		if (commandArgumentParser2.TryProcess(args) != EnumParseResult.Good)
		{
			return 0;
		}
		if ((commandArgumentParser2.GetValue() as Entity[]).Length == 0)
		{
			return 1;
		}
		return 2;
	}

	private Caller getCaller()
	{
		Caller caller = new Caller();
		caller.Type = EnumCallerType.Console;
		caller.CallerRole = "admin";
		caller.CallerPrivileges = new string[1] { "*" };
		caller.FromChatGroupId = GlobalConstants.ConsoleGroup;
		caller.Pos = new Vec3d(0.5, 0.5, 0.5).Add(Pos);
		return caller;
	}

	public override bool OnInteract(Caller caller)
	{
		if (caller.Player != null && BlockEntityGuiConfigurableCommands.CanEditCommandblocks(caller.Player))
		{
			if (Api.Side == EnumAppSide.Client && caller.Player.Entity.Controls.ShiftKey)
			{
				if (clientDialog != null)
				{
					clientDialog.TryClose();
					clientDialog.Dispose();
					clientDialog = null;
					return true;
				}
				clientDialog = new GuiDialogBlockEntityConditional(Pos, Commands, Latching, Api as ICoreClientAPI, "Conditional editor");
				clientDialog.TryOpen();
				clientDialog.OnClosed += delegate
				{
					clientDialog?.Dispose();
					clientDialog = null;
				};
				return true;
			}
		}
		else
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "noprivilege", "Can only be modified in creative mode and with controlserver privlege");
		}
		return base.OnInteract(caller);
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		facing = ((dir > 0) ? facing.GetCCW() : facing.GetCW());
		Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(base.Block.CodeWithVariant("side", facing.Code)).Id, Pos);
		MarkDirty(redrawOnClient: true);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		prevState = tree.GetInt("prev");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("prev", prevState);
	}
}
