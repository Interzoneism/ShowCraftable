namespace Vintagestory.API.Client;

public static class ElementStdBounds
{
	public static int mainMenuUnscaledLogoSize = 230;

	public static int mainMenuUnscaledLogoHorPadding = 30;

	public static int mainMenuUnscaledLogoVerPadding = 10;

	public static int mainMenuUnscaledWoodPlankWidth = 13;

	public static ElementBounds AutosizedMainDialog => new ElementBounds
	{
		Alignment = EnumDialogArea.CenterMiddle,
		BothSizing = ElementSizing.FitToChildren
	};

	public static ElementBounds Statbar(EnumDialogArea alignment, double width)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			fixedWidth = width,
			fixedHeight = GuiElementStatbar.DefaultHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds MainScreenRightPart()
	{
		ElementBounds elementBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 1.0, 1.0);
		elementBounds.horizontalSizing = ElementSizing.PercentualSubstractFixed;
		elementBounds.fixedWidth = mainMenuUnscaledLogoSize + mainMenuUnscaledLogoHorPadding * 2 + mainMenuUnscaledWoodPlankWidth;
		return elementBounds;
	}

	public static ElementBounds AutosizedMainDialogAtPos(double fixedY)
	{
		return new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, fixedY);
	}

	public static ElementBounds DialogBackground()
	{
		return new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(GuiStyle.ElementToDialogPadding);
	}

	public static ElementBounds DialogBackground(double horPadding, double verPadding)
	{
		return new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(horPadding, verPadding);
	}

	public static ElementBounds MenuButton(float rowIndex, EnumDialogArea alignment = EnumDialogArea.CenterFixed)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			BothSizing = ElementSizing.Fixed,
			fixedY = 80f * rowIndex,
			fixedPaddingX = 2.0,
			fixedPaddingY = 2.0
		};
	}

	public static ElementBounds Rowed(float rowIndex, double padding, EnumDialogArea alignment = EnumDialogArea.None)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			BothSizing = ElementSizing.Fixed,
			fixedY = 70f * rowIndex,
			fixedPaddingX = padding,
			fixedPaddingY = padding
		};
	}

	public static ElementBounds Sign(double fixedX, double fixedY, double fixedWith, double fixedHeight = 80.0)
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			BothSizing = ElementSizing.Fixed,
			fixedX = fixedX,
			fixedY = fixedY,
			fixedWidth = fixedWith,
			fixedHeight = fixedHeight
		};
	}

	public static ElementBounds Slider(double x, double y, double width)
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			BothSizing = ElementSizing.Fixed,
			fixedX = x,
			fixedY = y,
			fixedWidth = width,
			fixedHeight = 20.0
		};
	}

	public static ElementBounds VerticalScrollbar(ElementBounds leftElement)
	{
		return new ElementBounds
		{
			Alignment = leftElement.Alignment,
			BothSizing = ElementSizing.Fixed,
			fixedOffsetX = leftElement.fixedX + leftElement.fixedWidth + 3.0,
			fixedOffsetY = leftElement.fixedY,
			fixedPaddingX = GuiElementScrollbar.DeafultScrollbarPadding,
			fixedWidth = GuiElementScrollbar.DefaultScrollbarWidth,
			fixedHeight = leftElement.fixedHeight,
			percentHeight = leftElement.percentHeight
		};
	}

	public static ElementBounds Slot(double x = 0.0, double y = 0.0)
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			BothSizing = ElementSizing.Fixed,
			fixedX = x,
			fixedY = y,
			fixedWidth = GuiElementPassiveItemSlot.unscaledSlotSize,
			fixedHeight = GuiElementPassiveItemSlot.unscaledSlotSize
		};
	}

	public static ElementBounds SlotGrid(EnumDialogArea alignment, double x, double y, int cols, int rows)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			BothSizing = ElementSizing.Fixed,
			fixedX = x,
			fixedY = y,
			fixedWidth = (double)cols * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding),
			fixedHeight = (double)rows * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding)
		};
	}

	public static ElementBounds ToggleButton(double fixedX, double fixedY, double width, double height)
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			BothSizing = ElementSizing.Fixed,
			fixedX = fixedX,
			fixedY = fixedY,
			fixedWidth = width,
			fixedHeight = height
		};
	}

	public static ElementBounds TitleBar()
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			verticalSizing = ElementSizing.Fixed,
			horizontalSizing = ElementSizing.Percentual,
			percentWidth = 1.0,
			fixedHeight = (float)GuiStyle.TitleBarHeight
		};
	}
}
