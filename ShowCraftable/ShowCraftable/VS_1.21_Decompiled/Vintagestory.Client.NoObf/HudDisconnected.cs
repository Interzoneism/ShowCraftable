using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class HudDisconnected : HudElement
{
	private GuiComposer disconnectedDialog;

	public override bool Focusable => false;

	public HudDisconnected(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnBlockTexturesLoaded()
	{
		ElementBounds bounds = ElementBounds.Fixed(EnumDialogArea.RightBottom, 0.0, 0.0, 45.0, 20.0);
		ElementBounds bounds2 = ElementBounds.Fixed(10.0, 10.0, 330.0, 50.0);
		string key = (ScreenManager.Platform.IsServerRunning ? "Server overloaded or crashed" : "Bad connection or server overloaded/crashed");
		disconnectedDialog = capi.Gui.CreateCompo("disconnecteddialog", ElementBounds.Fixed(EnumDialogArea.RightTop, 0.0, 0.0, 380.0, 60.0).WithFixedAlignmentOffset(-10.0, 10.0)).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false).AddDynamicText("", CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), bounds, "countertext")
			.AddStaticText(Lang.Get("Host not responding. Possible causes: {0}", Lang.Get(key)), CairoFont.WhiteDetailText(), bounds2)
			.AddStaticCustomDraw(ElementBounds.Fixed(EnumDialogArea.RightTop, 0.0, 0.0, 25.0, 25.0).WithFixedPadding(10.0, 10.0), OnDialogDraw)
			.Compose();
		TryOpen();
		ClientMain obj = capi.World as ClientMain;
		obj.LastReceivedMilliseconds = obj.ElapsedMilliseconds;
	}

	public override bool TryClose()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		ClientMain clientMain = capi.World as ClientMain;
		if (!clientMain.IsPaused)
		{
			float num = (float)(clientMain.ElapsedMilliseconds - clientMain.LastReceivedMilliseconds) / 1000f;
			if (disconnectedDialog != null && (num >= 5f || (clientMain.IsSingleplayer && !ScreenManager.Platform.IsServerRunning)))
			{
				disconnectedDialog.GetDynamicText("countertext").SetNewText(((int)num).ToString() ?? "");
				disconnectedDialog.Render(deltaTime);
				disconnectedDialog.PostRender(deltaTime);
			}
		}
	}

	private void OnDialogDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		capi.Gui.Icons.DrawConnectionQuality(ctx, currentBounds.drawX, currentBounds.drawY, 0, currentBounds.InnerWidth);
	}

	public override void Dispose()
	{
		base.Dispose();
		disconnectedDialog?.Dispose();
	}
}
