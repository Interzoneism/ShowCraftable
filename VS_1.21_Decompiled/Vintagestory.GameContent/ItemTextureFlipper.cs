using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemTextureFlipper : Item
{
	private SkillItem[] skillitems;

	private BlockPos pos;

	private ICoreClientAPI capi;

	private Dictionary<AssetLocation, MultiTextureMeshRef> skillTextures = new Dictionary<AssetLocation, MultiTextureMeshRef>();

	private void renderSkillItem(AssetLocation code, float dt, double atPosX, double atPosY)
	{
		if (!(api.World.BlockAccessor.GetBlock(pos) is ITextureFlippable textureFlippable))
		{
			return;
		}
		OrderedDictionary<string, CompositeTexture> availableTextures = textureFlippable.GetAvailableTextures(pos);
		if (availableTextures != null)
		{
			if (!skillTextures.TryGetValue(code, out var value))
			{
				int textureSubId = availableTextures[code.Path].Baked.TextureSubId;
				TextureAtlasPosition textureAtlasPosition = capi.BlockTextureAtlas.Positions[textureSubId];
				MeshData customQuadModelData = QuadMeshUtil.GetCustomQuadModelData(textureAtlasPosition.x1, textureAtlasPosition.y1, textureAtlasPosition.x2, textureAtlasPosition.y2, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
				customQuadModelData.TextureIds = new int[1] { textureAtlasPosition.atlasTextureId };
				customQuadModelData.TextureIndices = new byte[1];
				value = capi.Render.UploadMultiTextureMesh(customQuadModelData);
				skillTextures[code] = value;
			}
			float gUIScale = RuntimeEnv.GUIScale;
			capi.Render.Render2DTexture(value, (float)atPosX - 24f * gUIScale, (float)atPosY - 24f * gUIScale, gUIScale * 64f, gUIScale * 64f);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (capi != null)
		{
			DisposeMeshes();
		}
	}

	public virtual void DisposeMeshes()
	{
		foreach (MultiTextureMeshRef value in skillTextures.Values)
		{
			value?.Dispose();
		}
		skillTextures.Clear();
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return base.GetToolMode(slot, byPlayer, blockSelection);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return null;
		}
		BlockPos position = blockSel.Position;
		if (position != pos)
		{
			if (!(api.World.BlockAccessor.GetBlock(position) is ITextureFlippable textureFlippable))
			{
				return null;
			}
			OrderedDictionary<string, CompositeTexture> availableTextures = textureFlippable.GetAvailableTextures(position);
			if (availableTextures == null)
			{
				return null;
			}
			GuiDialog? guiDialog = capi.Gui.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg.ToggleKeyCombinationCode == "toolmodeselect");
			if (guiDialog != null && guiDialog.IsOpened())
			{
				return null;
			}
			skillitems = new SkillItem[availableTextures.Count];
			int num = 0;
			foreach (KeyValuePair<string, CompositeTexture> item in availableTextures)
			{
				skillitems[num++] = new SkillItem
				{
					Code = new AssetLocation(item.Key),
					Name = item.Key,
					Data = item.Key,
					RenderHandler = renderSkillItem
				};
			}
			DisposeMeshes();
			pos = position;
		}
		return skillitems;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
		base.SetToolMode(slot, byPlayer, blockSelection, toolMode);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault || blockSel == null)
		{
			return;
		}
		int num = slot.Itemstack.Attributes.GetInt("toolMode");
		BlockPos position = blockSel.Position;
		if (api.World.BlockAccessor.GetBlock(position) is ITextureFlippable textureFlippable)
		{
			OrderedDictionary<string, CompositeTexture> availableTextures = textureFlippable.GetAvailableTextures(position);
			if (availableTextures == null || num >= availableTextures.Count)
			{
				return;
			}
			textureFlippable.FlipTexture(position, availableTextures.GetKeyAtIndex(num));
		}
		handling = EnumHandHandling.PreventDefault;
	}
}
