using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleGrasshopper : EntityParticleInsect
{
	public override string Type => "grassHopper";

	public EntityParticleGrasshopper(ICoreClientAPI capi, double x, double y, double z)
		: base(capi, x, y, z)
	{
		Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)x, (int)y, (int)z);
		if (blockRaw.BlockMaterial == EnumBlockMaterial.Plant)
		{
			int color = blockRaw.GetColor(capi, new BlockPos((int)x, (int)y, (int)z));
			ColorRed = (byte)((color >> 16) & 0xFF);
			ColorGreen = (byte)((color >> 8) & 0xFF);
			ColorBlue = (byte)(color & 0xFF);
		}
		else
		{
			ColorRed = 31;
			ColorGreen = 178;
			ColorBlue = 144;
		}
		sound = new AssetLocation("sounds/creature/grasshopper");
	}

	protected override bool shouldPlaySound()
	{
		if (EntityParticleInsect.rand.NextDouble() < 0.01 && capi.World.BlockAccessor.GetLightLevel(Position.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight) > 7)
		{
			float seasonRel = capi.World.Calendar.GetSeasonRel(Position.AsBlockPos);
			if (((double)seasonRel > 0.48 && (double)seasonRel < 0.63) || EntityParticleInsect.rand.NextDouble() < 0.33)
			{
				return true;
			}
		}
		return false;
	}
}
