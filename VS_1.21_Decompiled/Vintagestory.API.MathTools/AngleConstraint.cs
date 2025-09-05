namespace Vintagestory.API.MathTools;

public class AngleConstraint : Vec2f
{
	public float CenterRad => X;

	public float RangeRad => Y;

	public AngleConstraint(float centerRad, float rangeRad)
		: base(centerRad, rangeRad)
	{
	}
}
