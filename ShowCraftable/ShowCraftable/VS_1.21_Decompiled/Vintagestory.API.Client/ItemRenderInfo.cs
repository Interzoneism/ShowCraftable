using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ItemRenderInfo
{
	public MultiTextureMeshRef ModelRef;

	public ModelTransform Transform;

	public bool CullFaces;

	public int TextureId;

	public Size2i TextureSize = new Size2i();

	public float AlphaTest;

	public bool HalfTransparent;

	public bool NormalShaded;

	public bool ApplyColor;

	public LoadedTexture OverlayTexture;

	public float OverlayOpacity;

	public float DamageEffect;

	public ItemSlot InSlot;

	public float dt;

	public void SetRotOverlay(ICoreClientAPI capi, float opacity)
	{
		if (OverlayTexture == null)
		{
			OverlayTexture = new LoadedTexture(capi);
		}
		capi.Render.GetOrLoadTexture(new AssetLocation("textures/gui/rot.png"), ref OverlayTexture);
		OverlayOpacity = opacity;
	}
}
