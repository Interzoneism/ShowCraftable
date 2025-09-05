namespace Vintagestory.API.MathTools;

public class Size2f
{
	public float Width;

	public float Height;

	public Size2f()
	{
	}

	public Size2f(float width, float height)
	{
		Width = width;
		Height = height;
	}

	public Size2f Clone()
	{
		return new Size2f(Width, Height);
	}
}
