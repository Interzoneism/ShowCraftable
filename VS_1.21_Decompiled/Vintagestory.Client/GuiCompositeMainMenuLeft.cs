using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class GuiCompositeMainMenuLeft : GuiComposite
{
	private ElementBounds sidebarBounds;

	private ScreenManager screenManager;

	private LoadedTexture bgtex;

	private LoadedTexture logoTexture;

	private ParticleRenderer2D particleSystem;

	private long renderStartMs;

	private float curdx;

	private float curdy;

	private SimpleParticleProperties prop;

	private Vec3d minPos = new Vec3d();

	private Vec3d addPos = new Vec3d();

	private Random rand = new Random();

	public double Width => sidebarBounds.OuterWidth;

	public GuiCompositeMainMenuLeft(ScreenManager screenManager)
	{
		this.screenManager = screenManager;
		particleSystem = new ParticleRenderer2D(screenManager, screenManager.api);
		Compose();
	}

	internal void SetHasNewVersion(string versionnumber)
	{
		((GuiElementNewVersionText)screenManager.mainMenuComposer.GetElement("newversion")).Activate(versionnumber);
	}

	public void Compose()
	{
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_073e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0745: Expected O, but got Unknown
		particleSystem.Compose("textures/particle/white-spec.png");
		sidebarBounds = new ElementBounds();
		sidebarBounds.horizontalSizing = ElementSizing.Fixed;
		sidebarBounds.verticalSizing = ElementSizing.Percentual;
		sidebarBounds.percentHeight = 1.0;
		sidebarBounds.fixedWidth = ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding + ElementStdBounds.mainMenuUnscaledWoodPlankWidth;
		ElementBounds bounds = ElementBounds.Fixed(0.0, 25.0, ElementStdBounds.mainMenuUnscaledLogoSize, ElementStdBounds.mainMenuUnscaledLogoSize).WithFixedPadding(ElementStdBounds.mainMenuUnscaledLogoHorPadding, ElementStdBounds.mainMenuUnscaledLogoVerPadding);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding + 25 + 50, (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding) - 2.0 * GuiElementTextButton.Padding + 2.0, 33.0);
		CairoFont cairoFont = CairoFont.ButtonText().WithFontSize(22f).WithWeight((FontWeight)0);
		ElementBounds elementBounds2 = elementBounds;
		FontExtents fontExtents = cairoFont.GetFontExtents();
		elementBounds2.fixedHeight = ((FontExtents)(ref fontExtents)).Height / (double)ClientSettings.GUIScale + 2.0 * GuiElementTextButton.Padding + 5.0;
		CairoFont cairoFont2 = CairoFont.WhiteSmallText();
		cairoFont2.Color = GuiStyle.ButtonTextColor;
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, 300.0, 30.0).WithFixedAlignmentOffset(13.0, -8.0);
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, 0.0, ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding - 11, 60.0).WithFixedPadding(5.0);
		string text = Lang.Get("mainmenu-loggedin", ClientSettings.PlayerName);
		if (screenManager.ClientIsOffline)
		{
			text = text + "<br>" + Lang.Get("mainmenu-offline");
		}
		screenManager.mainMenuComposer?.Dispose();
		screenManager.mainMenuComposer = ScreenManager.GuiComposers.Create("compositemainmenu", ElementBounds.Fill).AddShadedDialogBG(sidebarBounds, withTitleBar: false).BeginChildElements()
			.AddStaticCustomDraw(bounds, OnDrawTree)
			.AddButton(Lang.Get("mainmenu-sp"), OnSingleplayer, elementBounds, cairoFont, EnumButtonStyle.MainMenu, "singleplayer")
			.AddButton(Lang.Get("mainmenu-mp"), OnMultiplayer, elementBounds = elementBounds.BelowCopy(), cairoFont, EnumButtonStyle.MainMenu, "multiplayer")
			.AddIf(ClientSettings.HasGameServer)
			.AddButton(Lang.Get("mainmenu-gameserver"), OnGameServer, elementBounds = elementBounds.BelowCopy(), cairoFont, EnumButtonStyle.MainMenu, "gameserver")
			.EndIf()
			.AddStaticCustomDraw(ElementBounds.Fill, OnDrawSidebar)
			.AddButton(Lang.Get("mainmenu-settings"), OnSettings, elementBounds = elementBounds.BelowCopy(0.0, 25.0), cairoFont, EnumButtonStyle.MainMenu, "settings")
			.AddButton(Lang.Get("mainmenu-mods"), OnMods, elementBounds = elementBounds.BelowCopy(), cairoFont, EnumButtonStyle.MainMenu, "mods")
			.AddButton(Lang.Get("mainmenu-credits"), OnCredits, elementBounds = elementBounds.BelowCopy(), cairoFont, EnumButtonStyle.MainMenu, "credits")
			.AddButton(Lang.Get("mainmenu-quit"), OnQuit, elementBounds = elementBounds.BelowCopy(0.0, 45.0), cairoFont, EnumButtonStyle.MainMenu, "quit")
			.AddRichtext(text, cairoFont2, elementBounds3, didClickLink, "logintext")
			.AddInteractiveElement(new GuiElementNewVersionText(screenManager.api, CairoFont.WhiteDetailText().WithWeight((FontWeight)1).WithColor(GuiStyle.DarkBrownColor), bounds2), "newversion")
			.EndChildElements()
			.Compose();
		(screenManager.mainMenuComposer.GetElement("newversion") as GuiElementNewVersionText).OnClicked = onUpdateGame;
		GuiElementRichtext.DebugLogging = false;
		screenManager.GamePlatform.Logger.VerboseDebug("Left bottom main menu text is at {0}/{1} w/h {2},{3}", elementBounds3.absX, elementBounds3.absY, elementBounds3.OuterWidth, elementBounds3.OuterHeight);
		int num = 6;
		string domainAndPath = "textures/gui/backgrounds/mainmenu" + (1 + (int)(UnixTimeNow() / 604800) % num) + ".png";
		int day = DateTime.Now.Day;
		bool flag = DateTime.Now.Month == 12 && day >= 20 && day <= 30;
		bool num2 = (DateTime.Now.Month == 10 && day > 18) || (DateTime.Now.Month == 11 && day < 12);
		if (flag)
		{
			domainAndPath = "textures/gui/backgrounds/mainmenu-xmas.png";
		}
		if (num2)
		{
			domainAndPath = "textures/gui/backgrounds/mainmenu-halloween.png";
		}
		BitmapRef bitmapRef = screenManager.GamePlatform.AssetManager.TryGet_BaseAssets(new AssetLocation(domainAndPath))?.ToBitmap(screenManager.api);
		if (bitmapRef != null)
		{
			bgtex = new LoadedTexture(screenManager.api, screenManager.GamePlatform.LoadTexture((IBitmap)bitmapRef, linearMag: true, 0, generateMipmaps: false), bitmapRef.Width, bitmapRef.Height);
			bitmapRef.Dispose();
		}
		else
		{
			bgtex = new LoadedTexture(screenManager.api, 0, 1, 1);
		}
		ClientSettings.Inst.AddWatcher<float>("guiScale", OnGuiScaleChanged);
		byte[] data = ScreenManager.Platform.AssetManager.Get("textures/gui/logo.png").Data;
		BitmapExternal bitmapExternal = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(data, data.Length);
		ImageSurface val = new ImageSurface((Format)0, bitmapExternal.Width, bitmapExternal.Height);
		val.Image(bitmapExternal, 0, 0, bitmapExternal.Width, bitmapExternal.Height);
		bitmapExternal.Dispose();
		logoTexture?.Dispose();
		logoTexture = new LoadedTexture(screenManager.api);
		screenManager.api.Gui.LoadOrUpdateCairoTexture(val, linearMag: true, ref logoTexture);
		((Surface)val).Dispose();
	}

	private void onUpdateGame(string versionnumber)
	{
		if (RuntimeEnv.OS == OS.Windows)
		{
			screenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("mainmenu-confirm-updategame", versionnumber), delegate(bool ok)
			{
				OnConfirmUpdateGame(ok, versionnumber);
			}, screenManager, screenManager.mainScreen));
		}
		else
		{
			NetUtil.OpenUrlInBrowser("https://account.vintagestory.at");
		}
	}

	private void OnConfirmUpdateGame(bool ok, string versionnumber)
	{
		if (!ok)
		{
			screenManager.StartMainMenu();
		}
		else
		{
			screenManager.LoadScreen(new GuiScreenGetUpdate(versionnumber, screenManager, screenManager.mainScreen));
		}
	}

	public long UnixTimeNow()
	{
		return (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
	}

	private void OnGuiScaleChanged(float newValue)
	{
		screenManager.versionNumberTexture?.Dispose();
		screenManager.versionNumberTexture = screenManager.api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText());
	}

	public void updateButtonActiveFlag(string key)
	{
		screenManager.mainMenuComposer.GetButton("singleplayer").SetActive(key == "singleplayer");
		screenManager.mainMenuComposer.GetButton("multiplayer").SetActive(key == "multiplayer");
		screenManager.mainMenuComposer.GetButton("gameserver")?.SetActive(key == "gameserver");
		screenManager.mainMenuComposer.GetButton("settings").SetActive(key == "settings");
		screenManager.mainMenuComposer.GetButton("credits").SetActive(key == "credits");
		screenManager.mainMenuComposer.GetButton("mods").SetActive(key == "mods");
		screenManager.mainMenuComposer.GetButton("quit").SetActive(key == "quit");
	}

	private void OnDrawTree(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		ctx.Antialias = (Antialias)6;
		double width = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding);
		double num = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding);
		double num2 = GuiElement.scaled(100.0);
		LinearGradient val = new LinearGradient(0.0, 0.0, 0.0, num + num2);
		((Gradient)val).AddColorStop(0.0, new Color(56.0 / 255.0, 0.17647058823529413, 32.0 / 255.0, 0.5));
		((Gradient)val).AddColorStop(0.5, new Color(73.0 / 255.0, 58.0 / 255.0, 41.0 / 255.0, 0.5));
		((Gradient)val).AddColorStop(1.0, new Color(73.0 / 255.0, 58.0 / 255.0, 41.0 / 255.0, 0.0));
		GuiElement.Rectangle(ctx, 0.0, 0.0, width, num + num2);
		ctx.SetSource((Pattern)(object)val);
		ctx.Fill();
		((Pattern)val).Dispose();
		byte[] data = ScreenManager.Platform.AssetManager.Get("textures/gui/tree.png").Data;
		BitmapExternal bitmapExternal = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(data, data.Length);
		surface.Image(bitmapExternal, (int)currentBounds.drawX, (int)currentBounds.drawY, (int)currentBounds.InnerWidth, (int)currentBounds.InnerHeight);
		bitmapExternal.Dispose();
	}

	private void OnDrawSidebar(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		double num = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledWoodPlankWidth);
		double num2 = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding) + num;
		SurfacePattern pattern = GuiElement.getPattern(screenManager.api, new AssetLocation("gui/backgrounds/oak.png"), doCache: true, 255, 0.125f);
		GuiElement.Rectangle(ctx, num2 - num, 0.0, num, currentBounds.OuterHeight);
		ctx.SetSource((Pattern)(object)pattern);
		ctx.Fill();
		LinearGradient val = new LinearGradient(num2 - 5.0 - num, 0.0, num2 - num, 0.0);
		((Gradient)val).AddColorStop(0.0, new Color(0.0, 0.0, 0.0, 0.0));
		((Gradient)val).AddColorStop(0.6, new Color(0.0, 0.0, 0.0, 0.38));
		((Gradient)val).AddColorStop(1.0, new Color(0.0, 0.0, 0.0, 0.38));
		ctx.Operator = (Operator)14;
		GuiElement.Rectangle(ctx, num2 - 5.0 - num, 0.0, 5.0, currentBounds.OuterHeight);
		ctx.SetSource((Pattern)(object)val);
		ctx.Fill();
		((Pattern)val).Dispose();
		ctx.Operator = (Operator)2;
	}

	private void didClickLink(LinkTextComponent comp)
	{
		string href = comp.Href;
		if (href.StartsWithOrdinal("https://"))
		{
			NetUtil.OpenUrlInBrowser(href);
		}
		if (href.Contains("logout"))
		{
			OnLogout();
		}
	}

	private void OnLogout()
	{
		screenManager.sessionManager.DoLogout();
		ClientSettings.UserEmail = "";
		ClientSettings.PlayerName = "";
		ClientSettings.Sessionkey = "";
		ClientSettings.SessionSignature = "";
		ClientSettings.MpToken = "";
		ClientSettings.Inst.Save(force: true);
		screenManager.LoadAndCacheScreen(typeof(GuiScreenLogin));
	}

	private bool OnCredits()
	{
		updateButtonActiveFlag("credits");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenCredits));
		return true;
	}

	private bool OnMods()
	{
		updateButtonActiveFlag("mods");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenMods));
		return true;
	}

	private bool OnSettings()
	{
		updateButtonActiveFlag("settings");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenSettings));
		return true;
	}

	public bool OnSingleplayer()
	{
		updateButtonActiveFlag("singleplayer");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	public bool OnMultiplayer()
	{
		updateButtonActiveFlag("multiplayer");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	private bool OnGameServer()
	{
		updateButtonActiveFlag("gameserver");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenServerDashboard));
		return true;
	}

	public bool OnQuit()
	{
		ClientSettings.Inst.Save(force: true);
		updateButtonActiveFlag("quit");
		particleSystem?.Dispose();
		screenManager.GamePlatform.WindowExit("Main screen quit button was pressed");
		return true;
	}

	public void OnMouseDown(MouseEvent e)
	{
		if ((double)e.X < GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + ElementStdBounds.mainMenuUnscaledLogoHorPadding) && (double)e.Y < GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize) && e.Y > 50)
		{
			screenManager.LoadScreen(screenManager.mainScreen);
			e.Handled = true;
		}
		else
		{
			screenManager.mainMenuComposer.OnMouseDown(e);
		}
	}

	public void OnMouseUp(MouseEvent e)
	{
		screenManager.mainMenuComposer.OnMouseUp(e);
	}

	internal void OnMouseMove(MouseEvent e)
	{
		screenManager.mainMenuComposer.OnMouseMove(e);
	}

	public void RenderBg(float dt, bool mainMenuVisible)
	{
		Render(dt, screenManager.GamePlatform.EllapsedMs, mainMenuVisible);
	}

	protected void Render(float dt, long ellapsedMs, bool mainMenuVisible, bool onlyBackground = false)
	{
		if (renderStartMs == 0L)
		{
			renderStartMs = ellapsedMs;
		}
		float num = (float)((double)ellapsedMs / 1500.0);
		float num2 = GameMath.Clamp((float)(ellapsedMs - renderStartMs) / 60000f, 0f, 1f);
		float num3 = (GameMath.Sin(num / 2.4f) * 12f + GameMath.Sin(num / 2f) * 8f + GameMath.Sin(num / 1.2f) * 4f) / 2f;
		float num4 = (GameMath.Sin(num / 2.3f) * 9f + GameMath.Sin(num / 1.5f) * 11f + GameMath.Sin(num / 1.4f) * 4f) / 2f;
		float num5 = Math.Max(10, screenManager.GamePlatform.WindowSize.Width);
		float num6 = Math.Max(10, screenManager.GamePlatform.WindowSize.Height);
		float num7 = (float)screenManager.api.inputapi.MouseX / num5;
		float num8 = (float)screenManager.api.inputapi.MouseY / num6;
		float num9 = Math.Min(1f / 30f, dt);
		curdx += (-30f * num7 + 15f - curdx) * 5f * num9;
		curdy += (-30f * num8 + 15f - curdy) * 1.5f * num9;
		num3 += curdx;
		float num10 = num4 + curdy;
		float num11 = Math.Max(1f, (num5 + 80f) / num5 + (1f + GameMath.Sin(num / 5f) + GameMath.Sin(num / 6f)) / 5f) + 0.05f;
		num3 *= 1f + num11 - (num5 + 40f) / num5;
		float val = num10 * (1f + num11 - (num5 + 40f) / num5);
		num3 = GameMath.Clamp(num3, -100f, 100f);
		float num12 = GameMath.Clamp(val, -100f, 100f);
		double num13 = num5 / (float)bgtex.Width;
		double num14 = num6 / (float)bgtex.Height;
		double num15 = ((num13 > num14) ? num13 : num14);
		float num16 = (float)((double)bgtex.Width * num15);
		float num17 = (float)((double)bgtex.Height * num15);
		num3 *= num2;
		float num18 = num12 * num2;
		num11 = 1f + (num11 - 1f) * num2;
		float posX = num3 + (1f - num11) * num16 / 2f;
		float posY = num18 + (1f - num11) * num17 / 2f;
		screenManager.api.Render.Render2DTexture(bgtex.TextureId, posX, posY, num16 * num11, num17 * num11, 10f);
		ShaderPrograms.Gui.Stop();
		screenManager.GamePlatform.GlDepthMask(flag: false);
		spawnParticles(num9);
		float[] pMatrix = screenManager.api.renderapi.pMatrix;
		particleSystem.pMatrix = pMatrix;
		particleSystem.mvMatrix = Mat4f.Identity(new float[16]);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, 2f * curdx, 2f * curdy, 0f);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, num16 / 2f, num17 / 2f, 0f);
		Mat4f.Scale(particleSystem.mvMatrix, particleSystem.mvMatrix, num11, num11, num11);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, (0f - num16) / 2f, (0f - num17) / 2f, 0f);
		particleSystem.Render(dt);
		screenManager.GamePlatform.GlDepthMask(flag: true);
		float num19 = screenManager.GamePlatform.WindowSize.Width;
		double num20 = screenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
		float num21 = (num19 - (float)num20) * 0.8f;
		num20 += (double)((num19 - num21) / 4f);
		if (!mainMenuVisible)
		{
			num20 = num19 * 0.15f;
			num21 = num19 * 0.7f;
		}
		float height = (float)logoTexture.Height * (num21 / (float)logoTexture.Width);
		screenManager.api.Render.Render2DTexture(logoTexture.TextureId, (float)num20 + (float)Math.Sin((double)ellapsedMs / 2000.0) * 10f, (float)GuiElement.scaled(25.0) + (float)Math.Sin(20.0 + (double)ellapsedMs / 2220.0) * 10f, num21, height, 20f);
	}

	private void spawnParticles(float dt)
	{
		ClientPlatformAbstract gamePlatform = screenManager.GamePlatform;
		minPos.X = 0.0;
		minPos.Y = (float)gamePlatform.WindowSize.Height * 0.5f;
		minPos.Z = -50.0;
		addPos.X = gamePlatform.WindowSize.Width;
		addPos.Y = (float)gamePlatform.WindowSize.Height * 0.75f;
		if (prop == null)
		{
			prop = new SimpleParticleProperties(0.025f, 0.125f, ColorUtil.ToRgba(40, 255, 255, 255), new Vec3d(0.0, 0.0, 0.0), new Vec3d(), new Vec3f(), new Vec3f(), 5f, 0f, 0f, 0.4f);
			prop.MinPos = minPos;
			prop.AddPos = addPos;
			prop.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
			minPos.X = 0.0;
			minPos.Y = (float)gamePlatform.WindowSize.Height * 0f;
			addPos.X = gamePlatform.WindowSize.Width;
			addPos.Y = (float)gamePlatform.WindowSize.Height * 1.25f;
			for (int i = 0; i < 1000; i++)
			{
				float num = 3f * (2.5f + (float)rand.NextDouble() * 4f);
				prop.MinVelocity.Set(-12f * (0.5f + num / 13f), -3f - num * 2f, 0f);
				prop.AddVelocity.Set(24f - 12f * (1f - num / 13f), 3f, 0f);
				prop.MinSize = Math.Max(1f, (float)Math.Pow(num, 1.6) / 17f);
				prop.MaxSize = prop.MinSize * 1.1f;
				prop.MinQuantity = 0.025f * dt * 33f;
				prop.AddQuantity = 0.1f * dt * 33f;
				PrepareParticleProps(dt);
				particleSystem.Spawn(prop);
			}
			for (ParticleBase particleBase = particleSystem.Pool.ParticlesPool.FirstAlive; particleBase != null; particleBase = particleBase.Next)
			{
				particleBase.SecondsAlive = (float)rand.NextDouble() * particleBase.LifeLength;
			}
		}
		PrepareParticleProps(dt);
		particleSystem.Spawn(prop);
	}

	private void PrepareParticleProps(float dt)
	{
		float num = 2.2f * (2.5f + (float)rand.NextDouble() * 4f);
		prop.MinVelocity.Set(-12f * (0.5f + num / 13f), -3f - num * 2f, 0f);
		prop.AddVelocity.Set(24f - 12f * (1f - num / 13f), 3f, 0f);
		prop.MinSize = Math.Max(1f, (float)Math.Pow(num, 1.6) / 17f);
		prop.MaxSize = prop.MinSize * 1.1f;
		prop.MinQuantity = 0.025f * dt * 33f;
		prop.AddQuantity = 0.1f * dt * 33f;
	}

	public void Dispose()
	{
		bgtex?.Dispose();
		bgtex = null;
		logoTexture?.Dispose();
		particleSystem?.Dispose();
	}
}
