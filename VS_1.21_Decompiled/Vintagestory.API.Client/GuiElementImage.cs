using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class GuiElementImage : GuiElementTextBase
{
	private readonly AssetLocation imageAsset;

	public GuiElementImage(ICoreClientAPI capi, ElementBounds bounds, AssetLocation imageAsset)
		: base(capi, "", null, bounds)
	{
		this.imageAsset = imageAsset;
	}

	public override void ComposeElements(Context context, ImageSurface surface)
	{
		context.Save();
		ImageSurface imageSurfaceFromAsset = GuiElement.getImageSurfaceFromAsset(api, imageAsset);
		SurfacePattern pattern = GuiElement.getPattern(api, imageAsset);
		pattern.Filter = (Filter)2;
		context.SetSource((Pattern)(object)pattern);
		context.Rectangle(Bounds.drawX, Bounds.drawY, Bounds.OuterWidth, Bounds.OuterHeight);
		context.SetSourceSurface((Surface)(object)imageSurfaceFromAsset, (int)Bounds.drawX, (int)Bounds.drawY);
		context.FillPreserve();
		context.Restore();
		((Pattern)pattern).Dispose();
		((Surface)imageSurfaceFromAsset).Dispose();
	}
}
