using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ElementBounds
{
	public ElementBounds ParentBounds;

	public ElementBounds LeftOfBounds;

	public List<ElementBounds> ChildBounds = new List<ElementBounds>();

	protected bool IsWindowBounds;

	public string Code;

	public EnumDialogArea Alignment;

	public ElementSizing verticalSizing;

	public ElementSizing horizontalSizing;

	public double percentPaddingX;

	public double percentPaddingY;

	public double percentX;

	public double percentY;

	public double percentWidth;

	public double percentHeight;

	public double fixedMarginX;

	public double fixedMarginY;

	public double fixedPaddingX;

	public double fixedPaddingY;

	public double fixedX;

	public double fixedY;

	public double fixedWidth;

	public double fixedHeight;

	public double fixedOffsetX;

	public double fixedOffsetY;

	public double absPaddingX;

	public double absPaddingY;

	public double absMarginX;

	public double absMarginY;

	public double absOffsetX;

	public double absOffsetY;

	public double absFixedX;

	public double absFixedY;

	public double absInnerWidth;

	public double absInnerHeight;

	public string Name;

	public bool AllowNoChildren;

	public bool Initialized;

	public bool IsDrawingSurface;

	private bool requiresrelculation = true;

	public double renderOffsetX;

	public double renderOffsetY;

	public ElementSizing BothSizing
	{
		set
		{
			verticalSizing = value;
			horizontalSizing = value;
		}
	}

	public virtual bool RequiresRecalculation => requiresrelculation;

	public virtual double relX => absFixedX + absMarginX + absOffsetX;

	public virtual double relY => absFixedY + absMarginY + absOffsetY;

	public virtual double absX => absFixedX + absMarginX + absOffsetX + ParentBounds.absPaddingX + ParentBounds.absX;

	public virtual double absY => absFixedY + absMarginY + absOffsetY + ParentBounds.absPaddingY + ParentBounds.absY;

	public virtual double OuterWidth => absInnerWidth + 2.0 * absPaddingX;

	public virtual double OuterHeight => absInnerHeight + 2.0 * absPaddingY;

	public virtual int OuterWidthInt => (int)OuterWidth;

	public virtual int OuterHeightInt => (int)OuterHeight;

	public virtual double InnerWidth => absInnerWidth;

	public virtual double InnerHeight => absInnerHeight;

	public virtual double drawX => bgDrawX + absPaddingX;

	public virtual double drawY => bgDrawY + absPaddingY;

	public virtual double bgDrawX => absFixedX + absMarginX + absOffsetX + (ParentBounds.IsDrawingSurface ? ParentBounds.absPaddingX : ParentBounds.drawX);

	public virtual double bgDrawY => absFixedY + absMarginY + absOffsetY + (ParentBounds.IsDrawingSurface ? ParentBounds.absPaddingY : ParentBounds.drawY);

	public virtual double renderX => absFixedX + absMarginX + absOffsetX + ParentBounds.absPaddingX + ParentBounds.renderX + renderOffsetX;

	public virtual double renderY => absFixedY + absMarginY + absOffsetY + ParentBounds.absPaddingY + ParentBounds.renderY + renderOffsetY;

	public static ElementBounds Fill => new ElementBounds
	{
		Alignment = EnumDialogArea.None,
		BothSizing = ElementSizing.Percentual,
		percentWidth = 1.0,
		percentHeight = 1.0
	};

	public static ElementBounds Empty => new ElementEmptyBounds();

	public void MarkDirtyRecursive()
	{
		Initialized = false;
		foreach (ElementBounds childBound in ChildBounds)
		{
			if (ParentBounds != childBound)
			{
				if (this == childBound)
				{
					throw new Exception($"Fatal: Element bounds {this} self reference itself in child bounds, this would cause a stack overflow.");
				}
				childBound.MarkDirtyRecursive();
			}
		}
	}

	public virtual void CalcWorldBounds()
	{
		requiresrelculation = false;
		absOffsetX = scaled(fixedOffsetX);
		absOffsetY = scaled(fixedOffsetY);
		if (horizontalSizing == ElementSizing.FitToChildren && verticalSizing == ElementSizing.FitToChildren)
		{
			absFixedX = scaled(fixedX);
			absFixedY = scaled(fixedY);
			absPaddingX = scaled(fixedPaddingX);
			absPaddingY = scaled(fixedPaddingY);
			buildBoundsFromChildren();
		}
		else
		{
			switch (horizontalSizing)
			{
			case ElementSizing.Fixed:
				absFixedX = scaled(fixedX);
				if (LeftOfBounds != null)
				{
					absFixedX += LeftOfBounds.absFixedX + LeftOfBounds.OuterWidth;
				}
				absInnerWidth = scaled(fixedWidth);
				absPaddingX = scaled(fixedPaddingX);
				break;
			case ElementSizing.Percentual:
			case ElementSizing.PercentualSubstractFixed:
				absFixedX = percentX * ParentBounds.OuterWidth;
				absInnerWidth = percentWidth * ParentBounds.OuterWidth;
				absPaddingX = scaled(fixedPaddingX) + percentPaddingX * ParentBounds.OuterWidth;
				if (horizontalSizing == ElementSizing.PercentualSubstractFixed)
				{
					absInnerWidth -= scaled(fixedWidth);
				}
				break;
			case ElementSizing.FitToChildren:
				absFixedX = scaled(fixedX);
				absPaddingX = scaled(fixedPaddingX);
				buildBoundsFromChildren();
				break;
			}
			switch (verticalSizing)
			{
			case ElementSizing.Fixed:
				absFixedY = scaled(fixedY);
				absInnerHeight = scaled(fixedHeight);
				absPaddingY = scaled(fixedPaddingY);
				break;
			case ElementSizing.Percentual:
			case ElementSizing.PercentualSubstractFixed:
				absFixedY = percentY * ParentBounds.OuterHeight;
				absInnerHeight = percentHeight * ParentBounds.OuterHeight;
				absPaddingY = scaled(fixedPaddingY) + percentPaddingY * ParentBounds.OuterHeight;
				if (horizontalSizing == ElementSizing.PercentualSubstractFixed)
				{
					absInnerHeight -= scaled(fixedHeight);
				}
				break;
			case ElementSizing.FitToChildren:
				absFixedY = scaled(fixedY);
				absPaddingY = scaled(fixedPaddingY);
				buildBoundsFromChildren();
				break;
			}
		}
		ElementBounds parentBounds = ParentBounds;
		if (parentBounds != null && parentBounds.Initialized)
		{
			calcMarginFromAlignment(ParentBounds.InnerWidth, ParentBounds.InnerHeight);
		}
		Initialized = true;
		foreach (ElementBounds childBound in ChildBounds)
		{
			if (!childBound.Initialized)
			{
				childBound.CalcWorldBounds();
			}
		}
	}

	private void calcMarginFromAlignment(double dialogWidth, double dialogHeight)
	{
		int num = 0;
		int num2 = 0;
		ElementBounds parentBounds = ParentBounds;
		if (parentBounds != null && parentBounds.IsWindowBounds)
		{
			num = GuiStyle.LeftDialogMargin;
			num2 = GuiStyle.RightDialogMargin;
		}
		switch (Alignment)
		{
		case EnumDialogArea.FixedMiddle:
			absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
			break;
		case EnumDialogArea.FixedBottom:
			absMarginY = dialogHeight - OuterHeight;
			break;
		case EnumDialogArea.CenterFixed:
			absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
			break;
		case EnumDialogArea.CenterBottom:
			absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
			absMarginY = dialogHeight - OuterHeight;
			break;
		case EnumDialogArea.CenterMiddle:
			absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
			absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
			break;
		case EnumDialogArea.CenterTop:
			absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
			break;
		case EnumDialogArea.LeftBottom:
			absMarginX = num;
			absMarginY = dialogHeight - OuterHeight;
			break;
		case EnumDialogArea.LeftMiddle:
			absMarginX = num;
			absMarginY = dialogHeight / 2.0 - absInnerHeight / 2.0;
			break;
		case EnumDialogArea.LeftTop:
			absMarginX = num;
			absMarginY = 0.0;
			break;
		case EnumDialogArea.LeftFixed:
			absMarginX = num;
			break;
		case EnumDialogArea.RightBottom:
			absMarginX = dialogWidth - OuterWidth - (double)num2;
			absMarginY = dialogHeight - OuterHeight;
			break;
		case EnumDialogArea.RightMiddle:
			absMarginX = dialogWidth - OuterWidth - (double)num2;
			absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
			break;
		case EnumDialogArea.RightTop:
			absMarginX = dialogWidth - OuterWidth - (double)num2;
			absMarginY = 0.0;
			break;
		case EnumDialogArea.RightFixed:
			absMarginX = dialogWidth - OuterWidth - (double)num2;
			break;
		case EnumDialogArea.FixedTop:
			break;
		}
	}

	private void buildBoundsFromChildren()
	{
		if (ChildBounds == null || ChildBounds.Count == 0)
		{
			if (!AllowNoChildren)
			{
				throw new Exception("Cant build bounds from children elements, there are no children!");
			}
			return;
		}
		double num = 0.0;
		double num2 = 0.0;
		foreach (ElementBounds childBound in ChildBounds)
		{
			if (childBound == this)
			{
				throw new Exception("Endless loop detected. Bounds instance is contained itself in its ChildBounds List. Fix your code please :P");
			}
			EnumDialogArea alignment = childBound.Alignment;
			childBound.Alignment = EnumDialogArea.None;
			childBound.CalcWorldBounds();
			if (childBound.horizontalSizing != ElementSizing.Percentual)
			{
				num = Math.Max(num, childBound.OuterWidth + childBound.relX);
			}
			if (childBound.verticalSizing != ElementSizing.Percentual)
			{
				num2 = Math.Max(num2, childBound.OuterHeight + childBound.relY);
			}
			childBound.Alignment = alignment;
		}
		if (num == 0.0 || num2 == 0.0)
		{
			throw new Exception("Couldn't build bounds from children, there were probably no child elements using fixed sizing! (or they were size 0)");
		}
		if (horizontalSizing != ElementSizing.Fixed)
		{
			absInnerWidth = num;
		}
		if (verticalSizing != ElementSizing.Fixed)
		{
			absInnerHeight = num2;
		}
	}

	public static double scaled(double value)
	{
		return value * (double)RuntimeEnv.GUIScale;
	}

	public ElementBounds WithScale(double factor)
	{
		fixedX *= factor;
		fixedY *= factor;
		fixedWidth *= factor;
		fixedHeight *= factor;
		absPaddingX *= factor;
		absPaddingY *= factor;
		absMarginX *= factor;
		absMarginY *= factor;
		percentPaddingX *= factor;
		percentPaddingY *= factor;
		percentX *= factor;
		percentY *= factor;
		percentWidth *= factor;
		percentHeight *= factor;
		return this;
	}

	public ElementBounds WithChildren(params ElementBounds[] bounds)
	{
		foreach (ElementBounds bounds2 in bounds)
		{
			WithChild(bounds2);
		}
		return this;
	}

	public ElementBounds WithChild(ElementBounds bounds)
	{
		if (!ChildBounds.Contains(bounds))
		{
			ChildBounds.Add(bounds);
		}
		if (bounds.ParentBounds == null)
		{
			bounds.ParentBounds = this;
		}
		return this;
	}

	public ElementBounds RightOf(ElementBounds leftBounds, double leftMargin = 0.0)
	{
		LeftOfBounds = leftBounds;
		fixedX = leftMargin;
		return this;
	}

	public Vec2d PositionInside(int absPointX, int absPointY)
	{
		if (PointInside(absPointX, absPointY))
		{
			return new Vec2d((double)absPointX - absX, (double)absPointY - absY);
		}
		return null;
	}

	public bool PointInside(int absPointX, int absPointY)
	{
		if ((double)absPointX >= absX && (double)absPointX <= absX + OuterWidth && (double)absPointY >= absY)
		{
			return (double)absPointY <= absY + OuterHeight;
		}
		return false;
	}

	public bool PointInside(double absPointX, double absPointY)
	{
		if (absPointX >= absX && absPointX <= absX + OuterWidth && absPointY >= absY)
		{
			return absPointY <= absY + OuterHeight;
		}
		return false;
	}

	public bool PartiallyInside(ElementBounds boundingBounds)
	{
		if (!boundingBounds.PointInside(absX, absY) && !boundingBounds.PointInside(absX + OuterWidth, absY) && !boundingBounds.PointInside(absX, absY + OuterHeight))
		{
			return boundingBounds.PointInside(absX + OuterWidth, absY + OuterHeight);
		}
		return true;
	}

	public ElementBounds CopyOnlySize()
	{
		return new ElementBounds
		{
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			fixedHeight = fixedHeight,
			fixedWidth = fixedWidth,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			ParentBounds = Empty.WithSizing(ElementSizing.FitToChildren)
		};
	}

	public ElementBounds CopyOffsetedSibling(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
	{
		return new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedX = fixedX + fixedDeltaX,
			fixedY = fixedY + fixedDeltaY,
			fixedWidth = fixedWidth + fixedDeltaWidth,
			fixedHeight = fixedHeight + fixedDeltaHeight,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			fixedMarginX = fixedMarginX,
			fixedMarginY = fixedMarginY,
			percentPaddingX = percentPaddingX,
			percentPaddingY = percentPaddingY,
			ParentBounds = ParentBounds
		};
	}

	public ElementBounds BelowCopy(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
	{
		return new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			percentX = percentX,
			percentY = (percentY = percentHeight),
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedX = fixedX + fixedDeltaX,
			fixedY = fixedY + fixedDeltaY + fixedHeight + fixedPaddingY * 2.0,
			fixedWidth = fixedWidth + fixedDeltaWidth,
			fixedHeight = fixedHeight + fixedDeltaHeight,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			fixedMarginX = fixedMarginX,
			fixedMarginY = fixedMarginY,
			percentPaddingX = percentPaddingX,
			percentPaddingY = percentPaddingY,
			ParentBounds = ParentBounds
		};
	}

	public ElementBounds RightCopy(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
	{
		return new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			percentX = percentX,
			percentY = (percentY = percentHeight),
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedX = fixedX + fixedDeltaX + fixedWidth + fixedPaddingX * 2.0,
			fixedY = fixedY + fixedDeltaY,
			fixedWidth = fixedWidth + fixedDeltaWidth,
			fixedHeight = fixedHeight + fixedDeltaHeight,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			fixedMarginX = fixedMarginX,
			fixedMarginY = fixedMarginY,
			percentPaddingX = percentPaddingX,
			percentPaddingY = percentPaddingY,
			ParentBounds = ParentBounds
		};
	}

	public ElementBounds FlatCopy()
	{
		return new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedX = fixedX,
			fixedY = fixedY,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			fixedMarginX = fixedMarginX,
			fixedMarginY = fixedMarginY,
			percentPaddingX = percentPaddingX,
			percentPaddingY = percentPaddingY,
			ParentBounds = ParentBounds
		};
	}

	public ElementBounds ForkChild()
	{
		return ForkChildOffseted();
	}

	public ElementBounds ForkChildOffseted(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
	{
		return new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			percentHeight = percentHeight,
			percentWidth = percentHeight,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedX = fixedX + fixedDeltaX,
			fixedY = fixedY + fixedDeltaY,
			fixedWidth = fixedWidth + fixedDeltaWidth,
			fixedHeight = fixedHeight + fixedDeltaHeight,
			fixedPaddingX = fixedPaddingX,
			fixedPaddingY = fixedPaddingY,
			percentPaddingX = percentPaddingX,
			percentPaddingY = percentPaddingY,
			ParentBounds = this
		};
	}

	public ElementBounds ForkBoundingParent(double leftSpacing = 0.0, double topSpacing = 0.0, double rightSpacing = 0.0, double bottomSpacing = 0.0)
	{
		ElementBounds elementBounds = new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedWidth = fixedWidth + 2.0 * fixedPaddingX + leftSpacing + rightSpacing,
			fixedHeight = fixedHeight + 2.0 * fixedPaddingY + topSpacing + bottomSpacing,
			fixedX = fixedX,
			fixedY = fixedY,
			percentHeight = percentHeight,
			percentWidth = percentWidth
		};
		fixedX = leftSpacing;
		fixedY = topSpacing;
		percentWidth = 1.0;
		percentHeight = 1.0;
		ParentBounds = elementBounds;
		return elementBounds;
	}

	public ElementBounds ForkContainingChild(double leftSpacing = 0.0, double topSpacing = 0.0, double rightSpacing = 0.0, double bottomSpacing = 0.0)
	{
		ElementBounds elementBounds = new ElementBounds
		{
			Alignment = Alignment,
			verticalSizing = verticalSizing,
			horizontalSizing = horizontalSizing,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedWidth = fixedWidth - 2.0 * fixedPaddingX - leftSpacing - rightSpacing,
			fixedHeight = fixedHeight - 2.0 * fixedPaddingY - topSpacing - bottomSpacing,
			fixedX = fixedX,
			fixedY = fixedY,
			percentHeight = percentHeight,
			percentWidth = percentWidth
		};
		elementBounds.fixedX = leftSpacing;
		elementBounds.fixedY = topSpacing;
		percentWidth = 1.0;
		percentHeight = 1.0;
		ChildBounds.Add(elementBounds);
		elementBounds.ParentBounds = this;
		return elementBounds;
	}

	public override string ToString()
	{
		return absX + "/" + absY + " -> " + (absX + OuterWidth) + " / " + (absY + OuterHeight);
	}

	public ElementBounds FixedUnder(ElementBounds refBounds, double spacing = 0.0)
	{
		fixedY += refBounds.fixedY + refBounds.fixedHeight + spacing;
		return this;
	}

	public ElementBounds FixedRightOf(ElementBounds refBounds, double leftSpacing = 0.0)
	{
		fixedX = refBounds.fixedX + refBounds.fixedWidth + leftSpacing;
		return this;
	}

	public ElementBounds FixedLeftOf(ElementBounds refBounds, double rightSpacing = 0.0)
	{
		fixedX = refBounds.fixedX - fixedWidth - rightSpacing;
		return this;
	}

	public ElementBounds WithFixedSize(double width, double height)
	{
		fixedWidth = width;
		fixedHeight = height;
		return this;
	}

	public ElementBounds WithFixedWidth(double width)
	{
		fixedWidth = width;
		return this;
	}

	public ElementBounds WithFixedHeight(double height)
	{
		fixedHeight = height;
		return this;
	}

	public ElementBounds WithAlignment(EnumDialogArea alignment)
	{
		Alignment = alignment;
		return this;
	}

	public ElementBounds WithSizing(ElementSizing sizing)
	{
		verticalSizing = sizing;
		horizontalSizing = sizing;
		return this;
	}

	public ElementBounds WithSizing(ElementSizing horizontalSizing, ElementSizing verticalSizing)
	{
		this.verticalSizing = verticalSizing;
		this.horizontalSizing = horizontalSizing;
		return this;
	}

	public ElementBounds WithFixedMargin(double pad)
	{
		fixedMarginX = pad;
		fixedMarginY = pad;
		return this;
	}

	public ElementBounds WithFixedMargin(double padH, double padV)
	{
		fixedMarginX = padH;
		fixedMarginY = padV;
		return this;
	}

	public ElementBounds WithFixedPadding(double pad)
	{
		fixedPaddingX = pad;
		fixedPaddingY = pad;
		return this;
	}

	public ElementBounds WithFixedPadding(double leftRight, double upDown)
	{
		fixedPaddingX = leftRight;
		fixedPaddingY = upDown;
		return this;
	}

	public ElementBounds WithFixedAlignmentOffset(double x, double y)
	{
		fixedOffsetX = x;
		fixedOffsetY = y;
		return this;
	}

	public ElementBounds WithFixedPosition(double x, double y)
	{
		fixedX = x;
		fixedY = y;
		return this;
	}

	public ElementBounds WithFixedOffset(double offx, double offy)
	{
		fixedX += offx;
		fixedY += offy;
		return this;
	}

	public ElementBounds FixedShrink(double amount)
	{
		fixedWidth -= amount;
		fixedHeight -= amount;
		return this;
	}

	public ElementBounds FixedGrow(double amount)
	{
		fixedWidth += amount;
		fixedHeight += amount;
		return this;
	}

	public ElementBounds FixedGrow(double width, double height)
	{
		fixedWidth += width;
		fixedHeight += height;
		return this;
	}

	public ElementBounds WithParent(ElementBounds bounds)
	{
		ParentBounds = bounds;
		return this;
	}

	public ElementBounds WithEmptyParent()
	{
		ParentBounds = Empty;
		return this;
	}

	public static ElementBounds Fixed(int fixedX, int fixedY)
	{
		return Fixed(fixedX, fixedY, 0.0, 0.0);
	}

	public static ElementBounds FixedPos(EnumDialogArea alignment, double fixedX, double fixedY)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			BothSizing = ElementSizing.Fixed,
			fixedX = fixedX,
			fixedY = fixedY
		};
	}

	public static ElementBounds FixedSize(double fixedWidth, double fixedHeight)
	{
		return new ElementBounds
		{
			Alignment = EnumDialogArea.None,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds FixedSize(EnumDialogArea alignment, double fixedWidth, double fixedHeight)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds Fixed(double fixedX, double fixedY, double fixedWidth, double fixedHeight)
	{
		return new ElementBounds
		{
			fixedX = fixedX,
			fixedY = fixedY,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds FixedOffseted(EnumDialogArea alignment, double fixedOffsetX, double fixedOffsetY, double fixedWidth, double fixedHeight)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			fixedOffsetX = fixedOffsetX,
			fixedOffsetY = fixedOffsetY,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds Fixed(EnumDialogArea alignment, double fixedX, double fixedY, double fixedWidth, double fixedHeight)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			fixedX = fixedX,
			fixedY = fixedY,
			fixedWidth = fixedWidth,
			fixedHeight = fixedHeight,
			BothSizing = ElementSizing.Fixed
		};
	}

	public static ElementBounds Percentual(EnumDialogArea alignment, double percentWidth, double percentHeight)
	{
		return new ElementBounds
		{
			Alignment = alignment,
			percentWidth = percentWidth,
			percentHeight = percentHeight,
			BothSizing = ElementSizing.Percentual
		};
	}

	public static ElementBounds Percentual(double percentX, double percentY, double percentWidth, double percentHeight)
	{
		return new ElementBounds
		{
			percentX = percentX,
			percentY = percentY,
			percentWidth = percentWidth,
			percentHeight = percentHeight,
			BothSizing = ElementSizing.Percentual
		};
	}
}
