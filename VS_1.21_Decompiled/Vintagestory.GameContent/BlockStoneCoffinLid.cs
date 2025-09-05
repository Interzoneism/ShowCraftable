using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStoneCoffinLid : Block
{
	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]] is BlockStoneCoffinSection blockStoneCoffinSection)
		{
			int temperature = blockStoneCoffinSection.GetTemperature(api.World, pos.DownCopy());
			int num = GameMath.Clamp((temperature - 550) / 2, 0, 255);
			for (int i = 0; i < sourceMesh.FlagsCount; i++)
			{
				sourceMesh.Flags[i] &= -256;
				sourceMesh.Flags[i] |= num;
			}
			int[] incandescenceColor = ColorUtil.getIncandescenceColor(temperature);
			float num2 = GameMath.Clamp((float)incandescenceColor[3] / 255f, 0f, 1f);
			for (int j = 0; j < lightRgbsByCorner.Length; j++)
			{
				int num3 = lightRgbsByCorner[j];
				int v = num3 & 0xFF;
				int v2 = (num3 >> 8) & 0xFF;
				int v3 = (num3 >> 16) & 0xFF;
				int v4 = (num3 >> 24) & 0xFF;
				lightRgbsByCorner[j] = (GameMath.Mix(v4, 0, Math.Min(1f, 1.5f * num2)) << 24) | (GameMath.Mix(v3, incandescenceColor[2], num2) << 16) | (GameMath.Mix(v2, incandescenceColor[1], num2) << 8) | GameMath.Mix(v, incandescenceColor[0], num2);
			}
		}
	}
}
