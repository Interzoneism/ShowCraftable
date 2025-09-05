using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class HudElementInteractionHelp : HudElement
{
	private Block currentBlock;

	private int currentBlockSelectionIndex;

	private Entity currentEntity;

	private Vec3d currentPos;

	private DrawWorldInteractionUtil wiUtil;

	private int entityInViewCount;

	private int entitySelectionBoxIndex = -1;

	private bool wasAlive;

	private ICustomInteractionHelpPositioning cp;

	private bool customCurrentPosSet;

	public override string ToggleKeyCombinationCode => "blockinteractionhelp";

	public override double DrawOrder => 0.05;

	public HudElementInteractionHelp(ICoreClientAPI capi)
		: base(capi)
	{
		wiUtil = new DrawWorldInteractionUtil(capi, Composers, "-placedBlock");
		(capi.World as ClientMain).eventManager?.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, PlayerPosDiv8Changed);
		capi.Event.RegisterGameTickListener(Every15ms, 15);
		capi.Event.BlockChanged += OnBlockChanged;
		ComposeBlockWorldInteractionHelp();
		ClientSettings.Inst.AddWatcher("showBlockInteractionHelp", delegate(bool on)
		{
			if (on)
			{
				TryOpen();
			}
			else
			{
				TryClose();
			}
		});
		if (ClientSettings.ShowBlockInteractionHelp)
		{
			TryOpen();
		}
	}

	private void ComposeBlockWorldInteractionHelp()
	{
		if (IsOpened())
		{
			WorldInteraction[] worldInteractions = getWorldInteractions();
			wiUtil.ComposeBlockWorldInteractionHelp(worldInteractions);
		}
	}

	private WorldInteraction[] getWorldInteractions()
	{
		if (currentBlock != null)
		{
			EntityPos pos = capi.World.Player.Entity.Pos;
			BlockSelection currentBlockSelection = capi.World.Player.CurrentBlockSelection;
			if (currentBlockSelection == null || pos.XYZ.AsBlockPos.DistanceTo(currentBlockSelection.Position) > 8f)
			{
				return null;
			}
			return currentBlock.GetPlacedBlockInteractionHelp(capi.World, currentBlockSelection, capi.World.Player);
		}
		if (currentEntity != null)
		{
			EntityPos pos2 = capi.World.Player.Entity.Pos;
			EntitySelection currentEntitySelection = capi.World.Player.CurrentEntitySelection;
			if (currentEntitySelection == null || pos2.XYZ.AsBlockPos.DistanceTo(currentEntitySelection.Position.AsBlockPos) > 8f)
			{
				return null;
			}
			return currentEntitySelection.Entity.GetInteractionHelp(capi.World, currentEntitySelection, capi.World.Player);
		}
		return null;
	}

	private void Every15ms(float dt)
	{
		if (!IsOpened())
		{
			return;
		}
		if (capi.World.Player.CurrentEntitySelection == null)
		{
			currentEntity = null;
			if (capi.World.Player.CurrentBlockSelection == null)
			{
				currentBlock = null;
			}
			else
			{
				BlockInView();
			}
		}
		else
		{
			EntityInView();
		}
	}

	private void BlockInView()
	{
		BlockSelection currentBlockSelection = capi.World.Player.CurrentBlockSelection;
		Block block;
		if (currentBlockSelection.DidOffset)
		{
			BlockFacing opposite = currentBlockSelection.Face.Opposite;
			block = capi.World.BlockAccessor.GetBlockOnSide(currentBlockSelection.Position, opposite);
		}
		else
		{
			block = capi.World.BlockAccessor.GetBlock(currentBlockSelection.Position);
		}
		if (block.BlockId == 0)
		{
			currentBlock = null;
		}
		else if (block != currentBlock || (int)currentPos.X != currentBlockSelection.Position.X || (int)currentPos.Y != currentBlockSelection.Position.Y || (int)currentPos.Z != currentBlockSelection.Position.Z || currentBlockSelection.SelectionBoxIndex != currentBlockSelectionIndex)
		{
			currentBlockSelectionIndex = currentBlockSelection.SelectionBoxIndex;
			currentBlock = block;
			currentEntity = null;
			currentPos = currentBlockSelection.Position.ToVec3d().Add(0.5, block.InteractionHelpYOffset, 0.5);
			if (currentBlock.RandomDrawOffset != 0)
			{
				currentPos.X += (float)(GameMath.oaatHash(currentBlockSelection.Position.X, 0, currentBlockSelection.Position.Z) % 12) / (24f + 12f * (float)currentBlock.RandomDrawOffset);
				currentPos.Z += (float)(GameMath.oaatHash(currentBlockSelection.Position.X, 1, currentBlockSelection.Position.Z) % 12) / (24f + 12f * (float)currentBlock.RandomDrawOffset);
			}
			ComposeBlockWorldInteractionHelp();
		}
	}

	private void EntityInView()
	{
		Entity entity = capi.World.Player.CurrentEntitySelection.Entity;
		int selectionBoxIndex = capi.World.Player.CurrentEntitySelection.SelectionBoxIndex;
		if (entitySelectionBoxIndex != selectionBoxIndex || entity != currentEntity || wasAlive != entity?.Alive || entityInViewCount++ > 20)
		{
			entityInViewCount = 0;
			wasAlive = entity.Alive;
			currentEntity = entity;
			currentBlock = null;
			entitySelectionBoxIndex = selectionBoxIndex;
			cp = entity.GetInterface<ICustomInteractionHelpPositioning>();
			ComposeBlockWorldInteractionHelp();
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		ElementBounds elementBounds = wiUtil.Composer?.Bounds;
		if (currentEntity != null)
		{
			_ = customCurrentPosSet;
			if (cp != null)
			{
				currentPos = cp.GetInteractionHelpPosition();
				customCurrentPosSet = currentPos != null;
			}
			if (cp == null || currentPos == null)
			{
				double x = currentEntity.SelectionBox.X2 - currentEntity.OriginSelectionBox.X2;
				double z = currentEntity.SelectionBox.Z2 - currentEntity.OriginSelectionBox.Z2;
				currentPos = currentEntity.ServerPos.XYZ.Add(x, currentEntity.SelectionBox.Y2, z);
			}
		}
		if (elementBounds != null)
		{
			Vec3d vec3d = MatrixToolsd.Project(currentPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (vec3d.Z < 0.0)
			{
				return;
			}
			elementBounds.Alignment = EnumDialogArea.None;
			elementBounds.fixedOffsetX = 0.0;
			elementBounds.fixedOffsetY = 0.0;
			elementBounds.absFixedX = vec3d.X - wiUtil.ActualWidth / 2.0;
			elementBounds.absFixedY = (double)capi.Render.FrameHeight - vec3d.Y - elementBounds.OuterHeight * 0.8;
			elementBounds.absMarginX = 0.0;
			elementBounds.absMarginY = 0.0;
		}
		if ((capi.World as ClientMain).MouseGrabbed)
		{
			if (cp == null || cp.TransparentCenter)
			{
				capi.Render.CurrentActiveShader.Uniform("transparentCenter", 1);
			}
			base.OnRenderGUI(deltaTime);
			capi.Render.CurrentActiveShader.Uniform("transparentCenter", 0);
		}
	}

	private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		ComposeBlockWorldInteractionHelp();
	}

	public override bool ShouldReceiveRenderEvents()
	{
		if (currentBlock == null)
		{
			return currentEntity != null;
		}
		return true;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override bool ShouldReceiveMouseEvents()
	{
		return false;
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		IPlayer player = capi.World.Player;
		if (player?.CurrentBlockSelection != null && pos.Equals(player.CurrentBlockSelection.Position))
		{
			ComposeBlockWorldInteractionHelp();
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowBlockInteractionHelp = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowBlockInteractionHelp = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		wiUtil?.Dispose();
	}
}
