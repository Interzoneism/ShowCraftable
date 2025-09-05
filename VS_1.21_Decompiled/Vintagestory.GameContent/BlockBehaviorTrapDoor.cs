using System;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("TriggerSound", "The sound to play when the trapdoor is opened or closed.", "System.String", "Optional", "sounds/block/door", true)]
public class BlockBehaviorTrapDoor : StrongBlockBehavior, IClaimTraverseable
{
	[DocumentAsJson("Optional", "sounds/block/door", true)]
	public AssetLocation OpenSound;

	[DocumentAsJson("Optional", "sounds/block/door", true)]
	public AssetLocation CloseSound;

	[DocumentAsJson("Optional", "True", true)]
	public bool handopenable;

	[DocumentAsJson("Optional", "True", true)]
	public bool airtight;

	private ICoreAPI api;

	public MeshData animatableOrigMesh;

	public Shape animatableShape;

	public string animatableDictKey;

	public BlockBehaviorTrapDoor(Block block)
		: base(block)
	{
		airtight = block.Attributes["airtight"].AsBool(defaultValue: true);
		handopenable = block.Attributes["handopenable"].AsBool(defaultValue: true);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
		OpenSound = (CloseSound = AssetLocation.Create(block.Attributes["triggerSound"].AsString("sounds/block/door")));
		if (block.Attributes["openSound"].Exists)
		{
			OpenSound = AssetLocation.Create(block.Attributes["openSound"].AsString("sounds/block/door"));
		}
		if (block.Attributes["closeSound"].Exists)
		{
			CloseSound = AssetLocation.Create(block.Attributes["closeSound"].AsString("sounds/block/door"));
		}
		base.OnLoaded(api);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
		BEBehaviorTrapDoor bEBehaviorTrapDoor = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorTrapDoor>();
		bool flag = !bEBehaviorTrapDoor.Opened;
		if (activationArgs != null)
		{
			flag = activationArgs.GetBool("opened", flag);
		}
		if (bEBehaviorTrapDoor.Opened != flag)
		{
			bEBehaviorTrapDoor.ToggleDoorState(null, flag);
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorTrapDoor bEBehaviorTrapDoor = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>();
		if (bEBehaviorTrapDoor != null)
		{
			decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bEBehaviorTrapDoor.RotRad, 0f);
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockPos position = blockSel.Position;
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (blockAccessor.GetBlock(position, 1).Id == 0 && block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return placeDoor(world, byPlayer, itemstack, blockSel, position, blockAccessor);
		}
		return false;
	}

	public bool placeDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
	{
		ba.SetBlock(block.BlockId, pos);
		(ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>()).OnBlockPlaced(itemstack, byPlayer, blockSel);
		return true;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing, ref EnumHandling handled)
	{
		return base.GetParticleBreakBox(blockAccess, pos, facing, ref handled);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
	{
		if ((world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>()).Opened)
		{
			decalModelData = decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2f, 0f, 0f);
			decalModelData = decalModelData.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1f, -1f, 1f);
		}
		base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData, ref handled);
	}

	public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
	{
		doorNameWithMaterial(sb);
	}

	public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
	{
	}

	private void doorNameWithMaterial(StringBuilder sb)
	{
		if (block.Variant.ContainsKey("wood"))
		{
			string text = sb.ToString();
			sb.Clear();
			sb.Append(Lang.Get("doorname-with-material", text, Lang.Get("material-" + block.Variant["wood"])));
		}
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorTrapDoor bEBehavior = block.GetBEBehavior<BEBehaviorTrapDoor>(pos);
		if (bEBehavior == null)
		{
			return 0f;
		}
		if (!bEBehavior.IsSideSolid(face))
		{
			return 0f;
		}
		if (!airtight)
		{
			return 0f;
		}
		return 1f;
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorTrapDoor bEBehavior = block.GetBEBehavior<BEBehaviorTrapDoor>(pos);
		if (bEBehavior == null)
		{
			return 0;
		}
		if (type == EnumRetentionType.Sound)
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!bEBehavior.IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!bEBehavior.IsSideSolid(facing) && !bEBehavior.IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	private int getInsulation(BlockPos pos)
	{
		EnumBlockMaterial blockMaterial = block.GetBlockMaterial(api.World.BlockAccessor, pos);
		if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
		{
			return -1;
		}
		return 1;
	}
}
