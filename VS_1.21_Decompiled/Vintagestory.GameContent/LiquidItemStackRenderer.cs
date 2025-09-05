using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class LiquidItemStackRenderer : ModSystem
{
	private ICoreClientAPI capi;

	private Dictionary<string, LoadedTexture> litreTextTextures;

	private CairoFont stackSizeFont;

	public override void StartClientSide(ICoreClientAPI api)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		capi = api;
		stackSizeFont = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.DetailFontSize);
		stackSizeFont.FontWeight = (FontWeight)1;
		stackSizeFont.Color = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		stackSizeFont.StrokeColor = new double[4] { 0.0, 0.0, 0.0, 1.0 };
		stackSizeFont.StrokeWidth = (double)RuntimeEnv.GUIScale + 0.25;
		litreTextTextures = new Dictionary<string, LoadedTexture>();
		api.Settings.AddWatcher("guiScale", delegate(float newvalue)
		{
			stackSizeFont.StrokeWidth = (double)newvalue + 0.25;
			foreach (KeyValuePair<string, LoadedTexture> litreTextTexture in litreTextTextures)
			{
				litreTextTexture.Value.Dispose();
			}
			litreTextTextures.Clear();
		});
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	private void Event_LevelFinalize()
	{
		foreach (CollectibleObject collectible in capi.World.Collectibles)
		{
			JsonObject attributes = collectible.Attributes;
			if (attributes != null && attributes["waterTightContainerProps"].Exists)
			{
				RegisterLiquidStackRenderer(collectible);
			}
		}
		capi.Logger.VerboseDebug("Done scanning for liquid containers ");
	}

	private void Event_LeaveWorld()
	{
		foreach (KeyValuePair<string, LoadedTexture> litreTextTexture in litreTextTextures)
		{
			litreTextTexture.Value.Dispose();
		}
		litreTextTextures.Clear();
	}

	public void RegisterLiquidStackRenderer(CollectibleObject obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj cannot be null");
		}
		JsonObject attributes = obj.Attributes;
		if (attributes != null)
		{
			_ = attributes["waterTightContainerProps"].Exists;
			if (0 == 0)
			{
				capi.Event.RegisterItemstackRenderer(obj, RenderLiquidItemStackGui, EnumItemRenderTarget.Gui);
				return;
			}
		}
		throw new ArgumentException("This collectible object has no waterTightContainerProps");
	}

	public void RenderLiquidItemStackGui(ItemSlot inSlot, ItemRenderInfo renderInfo, Matrixf modelMat, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true)
	{
		ItemStack itemstack = inSlot.Itemstack;
		WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemstack);
		capi.Render.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d");
		if (showStackSize)
		{
			float num = ((float)itemstack.StackSize / containableProps?.ItemsPerLitre) ?? 1f;
			string litres = ((!((double)num < 0.1)) ? Lang.Get("{0:0.##} L", num) : Lang.Get("{0} mL", (int)(num * 1000f)));
			float num2 = size / (float)GuiElement.scaled(25.600000381469727);
			LoadedTexture orCreateLitreTexture = GetOrCreateLitreTexture(litres, size, num2);
			capi.Render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
			capi.Render.Render2DLoadedTexture(orCreateLitreTexture, (int)(posX + (double)size + 1.0 - (double)orCreateLitreTexture.Width - GuiElement.scaled(1.0)), (int)(posY + (double)size - (double)orCreateLitreTexture.Height + (double)num2 * GuiElement.scaled(3.0) - GuiElement.scaled(5.0)), (int)posZ + 60);
			capi.Render.GlToggleBlend(blend: true);
		}
	}

	public LoadedTexture GetOrCreateLitreTexture(string litres, float size, float fontSizeMultiplier = 1f)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		string key = litres + "-" + fontSizeMultiplier + "-1";
		if (!litreTextTextures.TryGetValue(key, out var value))
		{
			CairoFont cairoFont = stackSizeFont.Clone();
			cairoFont.UnscaledFontsize *= fontSizeMultiplier;
			TextExtents textExtents = cairoFont.GetTextExtents(litres);
			double width = ((TextExtents)(ref textExtents)).Width;
			double num = Math.Min(1.0, (double)(1.5f * size) / width);
			cairoFont.UnscaledFontsize *= num;
			return litreTextTextures[key] = capi.Gui.TextTexture.GenTextTexture(litres, cairoFont);
		}
		return value;
	}
}
