using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSupportBeam : Block
{
	private ModSystemSupportBeamPlacer bp;

	public bool PartialEnds;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		bp = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>();
		PartialSelection = true;
		PartialEnds = Attributes?["partialEnds"].AsBool() ?? false;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorSupportBeam bEBehaviorSupportBeam = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (bEBehaviorSupportBeam != null)
		{
			return bEBehaviorSupportBeam.GetSelectionBoxes();
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorSupportBeam bEBehaviorSupportBeam = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (bEBehaviorSupportBeam != null)
		{
			return bEBehaviorSupportBeam.GetCollisionBoxes();
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			handling = EnumHandHandling.PreventDefault;
			bp.OnInteract(this, slot, byEntity, blockSel, PartialEnds);
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (bp.CancelPlace(this, byEntity))
		{
			handling = EnumHandHandling.PreventDefault;
		}
		else
		{
			base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		}
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BEBehaviorSupportBeam bEBehaviorSupportBeam = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (bEBehaviorSupportBeam != null)
		{
			int valueOrDefault = ((api as ICoreClientAPI)?.World.Player?.CurrentBlockSelection?.SelectionBoxIndex).GetValueOrDefault();
			if (valueOrDefault < bEBehaviorSupportBeam.Beams.Length)
			{
				blockModelData = bEBehaviorSupportBeam.genMesh(valueOrDefault, null, null);
				decalModelData = bEBehaviorSupportBeam.genMesh(valueOrDefault, decalTexSource, "decal");
			}
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		int? num = byPlayer?.CurrentBlockSelection?.SelectionBoxIndex;
		BEBehaviorSupportBeam bEBehaviorSupportBeam = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (num.HasValue && bEBehaviorSupportBeam != null && bEBehaviorSupportBeam.Beams.Length > 1)
		{
			bEBehaviorSupportBeam.BreakBeam(num.Value, byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative);
		}
		else
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		return false;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[3]
		{
			new WorldInteraction
			{
				ActionLangCode = "Set Beam Start/End Point (Snap to 4x4 grid)",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "Set Beam Start/End Point (Snap to 16x16 grid)",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "ctrl"
			},
			new WorldInteraction
			{
				ActionLangCode = "Cancel placement",
				MouseButton = EnumMouseButton.Left
			}
		};
	}

	public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		return false;
	}
}
