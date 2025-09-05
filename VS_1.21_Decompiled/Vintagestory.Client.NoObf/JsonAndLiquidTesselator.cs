using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class JsonAndLiquidTesselator : IBlockTesselator
{
	private IBlockTesselator liquid;

	private IBlockTesselator json;

	public JsonAndLiquidTesselator(ChunkTesselator tct)
	{
		liquid = new LiquidTesselator(tct);
		json = new JsonTesselator();
	}

	public void Tesselate(TCTCache vars)
	{
		float finalX = vars.finalX;
		float finalZ = vars.finalZ;
		vars.finalX = vars.lx;
		vars.finalZ = vars.lz;
		vars.RenderPass = EnumChunkRenderPass.Liquid;
		byte climateMapIndex = (byte)(vars.tct.game.ColorMaps.IndexOfKey("climateWaterTint") + 1);
		ColorMapData colorMapData = vars.ColorMapData;
		int vertexFlags = vars.VertexFlags;
		vars.ColorMapData = new ColorMapData((byte)0, climateMapIndex, colorMapData.Temperature, colorMapData.Rainfall, frostable: false);
		vars.VertexFlags = 0;
		vars.ColorMapData = colorMapData;
		vars.VertexFlags = vertexFlags;
		vars.RenderPass = EnumChunkRenderPass.OpaqueNoCull;
		vars.finalX = finalX;
		vars.finalZ = finalZ;
		vars.drawFaceFlags = 255;
		json.Tesselate(vars);
	}
}
