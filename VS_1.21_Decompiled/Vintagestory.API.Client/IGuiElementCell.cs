using System;

namespace Vintagestory.API.Client;

public interface IGuiElementCell : IDisposable
{
	ElementBounds InsideClipBounds { get; set; }

	ElementBounds Bounds { get; }

	string MouseOverCursor { get; }

	void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime);

	void UpdateCellHeight();

	void OnMouseUpOnElement(MouseEvent args, int elementIndex);

	void OnMouseDownOnElement(MouseEvent args, int elementIndex);

	void OnMouseMoveOnElement(MouseEvent args, int elementIndex);
}
