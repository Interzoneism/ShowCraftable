using System;
using System.Collections.Generic;
using System.Runtime;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

public class GuiScreenMainRight : GuiScreen
{
	private LoadedTexture grayBg;

	private LoadedTexture quoteTexture;

	private float gcCollectAccum = -2f;

	private int gcCollectAttempts;

	private long renderStartMs;

	private string quote;

	public GuiScreenMainRight(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		ShowMainMenu = true;
		quote = getQuote();
	}

	public override void OnScreenLoaded()
	{
		ScreenManager.guiMainmenuLeft.updateButtonActiveFlag("home");
		gcCollectAttempts = 0;
		gcCollectAccum = -5f;
	}

	public void Compose()
	{
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-50.0, -50.0);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 450.0, 170.0);
		CairoFont baseFont = CairoFont.WhiteDetailText().WithFontSize(15f).WithLineHeightMultiplier(1.100000023841858);
		ElementComposer = ScreenManager.GuiComposers.Create("welcomedialog", bounds).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false, 5.0, 0.8f).BeginChildElements()
			.AddRichtext(Lang.Get("mainmenu-greeting"), baseFont, elementBounds.FlatCopy())
			.EndChildElements()
			.Compose();
		quoteTexture = ScreenManager.api.Gui.TextTexture.GenUnscaledTextTexture("„" + quote + "‟", CairoFont.WhiteDetailText().WithSlant((FontSlant)1));
		grayBg = new LoadedTexture(ScreenManager.api);
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.25);
		val2.Paint();
		ScreenManager.api.Gui.LoadOrUpdateCairoTexture(val, linearMag: true, ref grayBg);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		Render(dt, ScreenManager.GamePlatform.EllapsedMs);
	}

	public void Render(float dt, long ellapsedMs, bool onlyBackground = false)
	{
		if (renderStartMs == 0L)
		{
			renderStartMs = ellapsedMs;
		}
		ensureLOHCompacted(dt);
		float num = ScreenManager.GamePlatform.WindowSize.Width;
		float num2 = ScreenManager.GamePlatform.WindowSize.Height;
		double posX = ScreenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
		if (!onlyBackground)
		{
			ElementComposer.Render(dt);
			if (ElementComposer.MouseOverCursor != null)
			{
				FocusedMouseCursor = ElementComposer.MouseOverCursor;
			}
			ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(grayBg.TextureId, ScreenManager.guiMainmenuLeft.Width, (double)(num2 - (float)quoteTexture.Height) - GuiElement.scaled(10.0), num, (double)quoteTexture.Height + GuiElement.scaled(10.0));
			ScreenManager.RenderMainMenuParts(dt, ElementComposer.Bounds, ShowMainMenu, darkenEdges: false);
			if (ScreenManager.mainMenuComposer.MouseOverCursor != null)
			{
				FocusedMouseCursor = ScreenManager.mainMenuComposer.MouseOverCursor;
			}
			ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(quoteTexture.TextureId, posX, (double)(num2 - (float)quoteTexture.Height) - GuiElement.scaled(5.0), quoteTexture.Width, quoteTexture.Height);
			ElementComposer.PostRender(dt);
			ScreenManager.GamePlatform.UseMouseCursor((FocusedMouseCursor == null) ? "normal" : FocusedMouseCursor);
		}
	}

	private void ensureLOHCompacted(float dt)
	{
		if (ScreenManager.CurrentScreen is GuiScreenConnectingToServer || ScreenManager.CurrentScreen is GuiScreenExitingServer || gcCollectAttempts > 6)
		{
			return;
		}
		int num = 300;
		long num2 = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
		gcCollectAccum += dt;
		if (gcCollectAccum > 1f && num2 > num)
		{
			if (ClientSettings.OptimizeRamMode > 0)
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
			}
			gcCollectAccum = 0f;
			gcCollectAttempts++;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		quoteTexture?.Dispose();
		quoteTexture = null;
		grayBg?.Dispose();
		grayBg = null;
	}

	private string getQuote()
	{
		List<string> list = new List<string>();
		int num = 1;
		while (Lang.HasTranslation("mainscreen-quote" + num, findWildcarded: false))
		{
			list.Add(Lang.Get("mainscreen-quote" + num));
			num++;
		}
		Random random = new Random();
		if (list.Count == 0)
		{
			return "";
		}
		return list[random.Next(list.Count)];
	}
}
