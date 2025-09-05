using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IGuiAPI
{
	MeshRef QuadMeshRef { get; }

	List<GuiDialog> LoadedGuis { get; }

	List<GuiDialog> OpenedGuis { get; }

	TextTextureUtil TextTexture { get; }

	TextDrawUtil Text { get; }

	IconUtil Icons { get; }

	ElementBounds WindowBounds { get; }

	List<ElementBounds> GetDialogBoundsInArea(EnumDialogArea area);

	GuiComposer CreateCompo(string dialogName, ElementBounds bounds);

	void RegisterDialog(params GuiDialog[] dialogs);

	void DeleteTexture(int textureid);

	LoadedTexture LoadSvg(AssetLocation loc, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0);

	void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0);

	void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix transform, int posx, int posy, int width = 0, int height = 0, int? color = 0);

	LoadedTexture LoadSvgWithPadding(AssetLocation loc, int textureWidth, int textureHeight, int padding = 0, int? color = 0);

	int LoadCairoTexture(ImageSurface surface, bool linearMag);

	void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture);

	Vec2i GetDialogPosition(string key);

	void SetDialogPosition(string key, Vec2i pos);

	void PlaySound(string soundname, bool randomizePitch = false, float volume = 1f);

	void PlaySound(AssetLocation soundname, bool randomizePitch = false, float volume = 1f);

	void RequestFocus(GuiDialog guiDialog);

	void TriggerDialogOpened(GuiDialog guiDialog);

	void TriggerDialogClosed(GuiDialog guiDialog);

	void OpenLink(string href);
}
