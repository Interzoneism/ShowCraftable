using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ControlMeta : AnimationMetaData
{
	public float MoveSpeedMultiplier;

	public float MoveSpeed;

	public AnimationMetaData RiderAnim;

	public AnimationMetaData PassengerAnim;

	public AnimationMetaData GetSeatAnimation(IMountableSeat seat)
	{
		if (seat.CanControl || PassengerAnim == null)
		{
			return RiderAnim;
		}
		return PassengerAnim;
	}

	public AnimationMetaData GetPassengerAnim()
	{
		if (PassengerAnim == null)
		{
			return RiderAnim;
		}
		return PassengerAnim;
	}
}
