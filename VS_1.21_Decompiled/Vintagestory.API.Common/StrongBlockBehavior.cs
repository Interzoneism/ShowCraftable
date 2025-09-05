using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public abstract class StrongBlockBehavior : BlockBehavior
{
	protected StrongBlockBehavior(Block block)
		: base(block)
	{
	}

	public virtual void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
	}

	public virtual Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return null;
	}

	public virtual Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return null;
	}

	public virtual Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return null;
	}

	public virtual Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return null;
	}

	public virtual void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
	}

	public virtual bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool DoParticalSelection(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return false;
	}
}
