namespace Vintagestory.API.MathTools;

[DocumentAsJson]
public class Size3f
{
	[DocumentAsJson]
	public float Width;

	[DocumentAsJson]
	public float Height;

	[DocumentAsJson]
	public float Length;

	public Size3f()
	{
	}

	public Size3f(float width, float height, float length)
	{
		Width = width;
		Height = height;
		Length = length;
	}

	public Size3f Clone()
	{
		return new Size3f(Width, Height, Length);
	}

	public bool CanContain(Size3f obj)
	{
		if (Width >= obj.Width && Height >= obj.Height)
		{
			return Length >= obj.Length;
		}
		return false;
	}
}
