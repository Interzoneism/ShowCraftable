using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementCellList<T> : GuiElement
{
	public List<IGuiElementCell> elementCells = new List<IGuiElementCell>();

	private List<IGuiElementCell> visibleCells = new List<IGuiElementCell>();

	public int unscaledCellSpacing = 10;

	public int UnscaledCellVerPadding = 4;

	public int UnscaledCellHorPadding = 7;

	private Func<IGuiElementCell, bool> cellFilter;

	private OnRequireCell<T> cellcreator;

	private bool didInitialize;

	private IEnumerable<T> cellsTmp;

	public override ElementBounds InsideClipBounds
	{
		get
		{
			return base.InsideClipBounds;
		}
		set
		{
			base.InsideClipBounds = value;
			foreach (IGuiElementCell elementCell in elementCells)
			{
				elementCell.InsideClipBounds = InsideClipBounds;
			}
		}
	}

	public GuiElementCellList(ICoreClientAPI capi, ElementBounds bounds, OnRequireCell<T> cellCreator, IEnumerable<T> cells = null)
		: base(capi, bounds)
	{
		cellcreator = cellCreator;
		cellsTmp = cells;
		Bounds.IsDrawingSurface = true;
	}

	private void Initialize()
	{
		if (cellsTmp != null)
		{
			foreach (T item in cellsTmp)
			{
				AddCell(item);
			}
			visibleCells.Clear();
			visibleCells.AddRange(elementCells);
		}
		CalcTotalHeight();
		didInitialize = true;
	}

	public void ReloadCells(IEnumerable<T> cells)
	{
		foreach (IGuiElementCell elementCell in elementCells)
		{
			elementCell?.Dispose();
		}
		elementCells.Clear();
		foreach (T cell in cells)
		{
			AddCell(cell);
		}
		visibleCells.Clear();
		visibleCells.AddRange(elementCells);
		CalcTotalHeight();
	}

	public override void BeforeCalcBounds()
	{
		if (!didInitialize)
		{
			Initialize();
		}
		else
		{
			CalcTotalHeight();
		}
	}

	public void CalcTotalHeight()
	{
		Bounds.CalcWorldBounds();
		double num = 0.0;
		double num2 = 0.0;
		foreach (IGuiElementCell visibleCell in visibleCells)
		{
			visibleCell.UpdateCellHeight();
			visibleCell.Bounds.WithFixedPosition(0.0, num2);
			visibleCell.Bounds.CalcWorldBounds();
			num += visibleCell.Bounds.fixedHeight + (double)unscaledCellSpacing + (double)(2 * UnscaledCellVerPadding);
			num2 += visibleCell.Bounds.OuterHeight / (double)RuntimeEnv.GUIScale + (double)unscaledCellSpacing;
		}
		Bounds.fixedHeight = num + (double)unscaledCellSpacing;
		Bounds.CalcWorldBounds();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	internal void FilterCells(Func<IGuiElementCell, bool> onFilter)
	{
		cellFilter = onFilter;
		visibleCells.Clear();
		foreach (IGuiElementCell elementCell in elementCells)
		{
			if (cellFilter(elementCell))
			{
				visibleCells.Add(elementCell);
			}
		}
		CalcTotalHeight();
	}

	protected void AddCell(T cell, int afterPosition = -1)
	{
		ElementBounds bounds = new ElementBounds
		{
			fixedPaddingX = UnscaledCellHorPadding,
			fixedPaddingY = UnscaledCellVerPadding,
			fixedWidth = Bounds.fixedWidth - 2.0 * Bounds.fixedPaddingX - (double)(2 * UnscaledCellHorPadding),
			fixedHeight = 0.0,
			BothSizing = ElementSizing.Fixed
		}.WithParent(Bounds);
		IGuiElementCell guiElementCell = cellcreator(cell, bounds);
		guiElementCell.InsideClipBounds = InsideClipBounds;
		if (afterPosition == -1)
		{
			elementCells.Add(guiElementCell);
		}
		else
		{
			elementCells.Insert(afterPosition, guiElementCell);
		}
	}

	protected void RemoveCell(int position)
	{
		elementCells.RemoveAt(position);
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		foreach (IGuiElementCell visibleCell in visibleCells)
		{
			if (visibleCell.Bounds.PositionInside(mouseX, mouseY) != null)
			{
				visibleCell.OnMouseUpOnElement(args, elementCells.IndexOf(visibleCell));
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		foreach (IGuiElementCell visibleCell in visibleCells)
		{
			if (visibleCell.Bounds.PositionInside(mouseX, mouseY) != null)
			{
				visibleCell.OnMouseDownOnElement(args, elementCells.IndexOf(visibleCell));
			}
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		foreach (IGuiElementCell visibleCell in visibleCells)
		{
			if (visibleCell.Bounds.PositionInside(mouseX, mouseY) != null)
			{
				visibleCell.OnMouseMoveOnElement(args, elementCells.IndexOf(visibleCell));
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		MouseOverCursor = null;
		foreach (IGuiElementCell visibleCell in visibleCells)
		{
			if (visibleCell.Bounds.PartiallyInside(Bounds.ParentBounds))
			{
				visibleCell.OnRenderInteractiveElements(api, deltaTime);
				if (visibleCell.MouseOverCursor != null)
				{
					MouseOverCursor = visibleCell.MouseOverCursor;
				}
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		foreach (IGuiElementCell elementCell in elementCells)
		{
			elementCell.Dispose();
		}
	}
}
