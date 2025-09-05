using System;

namespace Vintagestory.API.Client;

public class SavegameCellEntry
{
	public string Title;

	public string DetailText;

	public string RightTopText;

	public string HoverText;

	public float RightTopOffY;

	public float LeftOffY;

	public double DetailTextOffY;

	public Action OnClick;

	public CairoFont TitleFont;

	public CairoFont DetailTextFont;

	public bool DrawAsButton = true;

	public bool Enabled;

	public bool Selected;
}
