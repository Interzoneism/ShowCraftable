using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class SyncedEntityPos : EntityPos
{
	private bool dirty;

	public long LastReceivedClientPosition;

	public override double X
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
			Dirty = true;
		}
	}

	public override double Y
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
			Dirty = true;
		}
	}

	public override double Z
	{
		get
		{
			return z;
		}
		set
		{
			z = value;
			Dirty = true;
		}
	}

	public override float Roll
	{
		get
		{
			return roll;
		}
		set
		{
			roll = value;
			Dirty = true;
		}
	}

	public override float Yaw
	{
		get
		{
			return yaw;
		}
		set
		{
			yaw = value;
			Dirty = true;
		}
	}

	public override float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			pitch = value;
			Dirty = true;
		}
	}

	public double XInternal
	{
		set
		{
			x = value;
		}
	}

	public double YInternal
	{
		set
		{
			y = value;
		}
	}

	public double ZInternal
	{
		set
		{
			z = value;
		}
	}

	public float RollInternal
	{
		set
		{
			roll = value;
		}
	}

	public float YawInternal
	{
		set
		{
			yaw = value;
		}
	}

	public float PitchInternal
	{
		set
		{
			pitch = value;
		}
	}

	public int StanceInternal
	{
		set
		{
			stance = value;
		}
	}

	public bool Dirty
	{
		get
		{
			return dirty;
		}
		set
		{
			dirty = value;
		}
	}

	public SyncedEntityPos()
	{
	}

	public SyncedEntityPos(Vec3d position)
		: base(position.X, position.Y, position.Z)
	{
		dirty = true;
	}

	public SyncedEntityPos(double x, double y, double z, float heading = 0f, float pitch = 0f)
		: base(x, y, z, heading, pitch)
	{
		dirty = true;
	}

	public void MarkClean()
	{
		dirty = false;
	}
}
