using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

internal class GuiElementImageBackground : GuiElement
{
	private AssetLocation textureLoc;

	private float brightness;

	private float alpha;

	private float scale;

	public GuiElementImageBackground(ICoreClientAPI capi, ElementBounds bounds, AssetLocation textureLoc, float brightness = 1f, float alpha = 1f, float scale = 1f)
		: base(capi, bounds)
	{
		this.alpha = alpha;
		this.scale = scale;
		this.textureLoc = textureLoc;
		this.brightness = brightness;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		SurfacePattern pattern = GuiElement.getPattern(api, textureLoc, doCache: true, (int)(alpha * 255f), scale);
		ctx.SetSource((Pattern)(object)pattern);
		ElementRoundRectangle(ctx, Bounds);
		ctx.Fill();
		if (brightness < 1f)
		{
			ElementRoundRectangle(ctx, Bounds);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, (double)(1f - brightness));
			ctx.Fill();
		}
	}
}
