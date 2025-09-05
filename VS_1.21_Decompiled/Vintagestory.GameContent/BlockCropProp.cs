using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCropProp : Block, ITexPositionSource
{
	private ICoreClientAPI capi;

	private string nowTesselatingType;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("block/meta/cropprop/" + nowTesselatingType), out var _, out var texPos);
			return texPos;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MultiTextureMeshRef> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "croprop-meshes");
		if (dictionary != null && dictionary.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item in dictionary)
			{
				item.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "croprop-meshes");
		}
		base.OnUnloaded(api);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "croprop-meshes", () => new Dictionary<string, MultiTextureMeshRef>());
		string key = itemstack.Attributes.GetString("type", "unknown");
		if (orCreate.TryGetValue(key, out var value))
		{
			renderinfo.ModelRef = value;
		}
		else
		{
			nowTesselatingType = key;
			capi.Tesselator.TesselateShape("croppropinv", Code, Shape, out var modeldata, this, 0, 0, 0);
			orCreate[key] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(modeldata));
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorCropProp bEBehavior = GetBEBehavior<BEBehaviorCropProp>(pos);
		string text = bEBehavior?.Type;
		if (text == null)
		{
			return base.GetPlacedBlockName(world, pos);
		}
		return Lang.GetMatching("block-crop-" + text + "-" + bEBehavior.Stage);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		dsc.AppendLine(string.Format(Lang.Get("Type: {0}", Lang.Get("cropprop-type-" + inSlot.Itemstack.Attributes.GetString("type")))));
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}
}
