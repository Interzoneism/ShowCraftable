namespace Vintagestory.API.MathTools;

public class Size2d
{
	public double Width;

	public double Height;

	public Size2d()
	{
	}

	public Size2d(double width, double height)
	{
		Width = width;
		Height = height;
	}

	public Size2d Clone()
	{
		return new Size2d(Width, Height);
	}
}
