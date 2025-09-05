using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class HudIngameDiscovery : HudElement
{
	private double x;

	private double y;

	private GuiElementHoverText elem;

	private Vec4f fadeCol = new Vec4f(1f, 1f, 1f, 1f);

	private long textActiveMs;

	private int durationVisibleMs = 6000;

	private Queue<string> messageQueue = new Queue<string>();

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudIngameDiscovery(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.InGameDiscovery += Event_InGameDiscovery;
		capi.Event.RegisterGameTickListener(OnGameTick, 20);
	}

	private void Event_InGameDiscovery(object sender, string errorCode, string text)
	{
		messageQueue.Enqueue(text);
		x = elem.Bounds.absFixedX;
		y = elem.Bounds.absFixedX;
	}

	private void OnGameTick(float dt)
	{
		if (textActiveMs == 0L && messageQueue.Count == 0)
		{
			return;
		}
		if (textActiveMs == 0L)
		{
			textActiveMs = capi.InWorldEllapsedMilliseconds;
			fadeCol.A = 0f;
			elem.SetNewText(messageQueue.Dequeue());
			elem.SetVisible(on: true);
			return;
		}
		long num = capi.InWorldEllapsedMilliseconds - textActiveMs;
		long num2 = durationVisibleMs - num;
		if (num2 <= 0)
		{
			textActiveMs = 0L;
			elem.SetVisible(on: false);
			return;
		}
		if (num < 250)
		{
			fadeCol.A = (float)num / 240f;
		}
		else
		{
			fadeCol.A = 1f;
		}
		if (num2 < 1000)
		{
			fadeCol.A = (float)num2 / 990f;
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		ComposeGuis();
	}

	public void ComposeGuis()
	{
		ElementBounds elementBounds = new ElementBounds
		{
			Alignment = EnumDialogArea.CenterMiddle,
			BothSizing = ElementSizing.Fixed,
			fixedWidth = 600.0,
			fixedHeight = 5.0
		};
		ElementBounds bounds = ElementBounds.Fixed(0.0, -155.0, 700.0, 30.0);
		ClearComposers();
		CairoFont font = CairoFont.WhiteMediumText().WithFont(GuiStyle.DecorativeFontName).WithColor(GuiStyle.DiscoveryTextColor)
			.WithStroke(GuiStyle.DialogBorderColor, 2.0)
			.WithOrientation(EnumTextOrientation.Center);
		Composers["ingameerror"] = capi.Gui.CreateCompo("ingameerror", elementBounds.FlatCopy()).PremultipliedAlpha(enable: false).BeginChildElements(elementBounds)
			.AddTranspHoverText("", font, 700, bounds, "discoverytext")
			.EndChildElements()
			.Compose();
		elem = Composers["ingameerror"].GetHoverText("discoverytext");
		elem.SetFollowMouse(on: false);
		elem.SetAutoWidth(on: false);
		elem.SetAutoDisplay(on: false);
		elem.fillBounds = true;
		elem.RenderColor = fadeCol;
		elem.ZPosition = 60f;
		elem.RenderAsPremultipliedAlpha = false;
		TryOpen();
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override bool ShouldReceiveMouseEvents()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (fadeCol.A > 0f)
		{
			base.OnRenderGUI(deltaTime);
		}
	}

	protected override void OnFocusChanged(bool on)
	{
	}
}
