using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

internal class ItemLiquidPortion : Item
{
	public override void OnGroundIdle(EntityItem entityItem)
	{
		entityItem.Die(EnumDespawnReason.Removed);
		if (entityItem.World.Side == EnumAppSide.Server)
		{
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(entityItem.Itemstack);
			float num = (float)entityItem.Itemstack.StackSize / (containableProps?.ItemsPerLitre ?? 1f);
			entityItem.World.SpawnCubeParticles(entityItem.SidedPos.XYZ, entityItem.Itemstack, 0.75f, (int)(num * 2f), 0.45f);
			entityItem.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), (float)entityItem.SidedPos.X, (float)entityItem.SidedPos.InternalY, (float)entityItem.SidedPos.Z);
		}
		base.OnGroundIdle(entityItem);
	}
}
