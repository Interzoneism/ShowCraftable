using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWithGrassOverlay : Block
{
	private CompositeTexture grassTex;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client && (Textures == null || !Textures.TryGetValue("specialSecondTexture", out grassTex)))
		{
			grassTex = Textures?.First().Value;
		}
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		string text = LastCodePart();
		if (text == "none")
		{
			return base.GetColorWithoutTint(capi, pos);
		}
		int? num = grassTex?.Baked.TextureSubId;
		if (!num.HasValue)
		{
			return -1;
		}
		int randomColor = capi.BlockTextureAtlas.GetRandomColor(num.Value);
		if (text == "normal")
		{
			return randomColor;
		}
		return ColorUtil.ColorOverlay(capi.BlockTextureAtlas.GetRandomColor(Textures["up"].Baked.TextureSubId), randomColor, (text == "verysparse") ? 0.5f : 0.75f);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		string text = LastCodePart();
		if (text == "none")
		{
			return base.GetColorWithoutTint(capi, pos);
		}
		int? num = grassTex?.Baked.TextureSubId;
		if (!num.HasValue)
		{
			return -1;
		}
		int num2 = capi.BlockTextureAtlas.GetAverageColor(num.Value);
		if (ClimateColorMapResolved != null)
		{
			num2 = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num2, pos.X, pos.Y, pos.Z, flipRb: false);
		}
		if (text == "normal")
		{
			return num2;
		}
		return ColorUtil.ColorOverlay(capi.BlockTextureAtlas.GetAverageColor(Textures["up"].Baked.TextureSubId), num2, (text == "verysparse") ? 0.5f : 0.75f);
	}
}
