using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[ProtoContract]
public class SoundParams
{
	[ProtoMember(1)]
	public AssetLocation Location;

	[ProtoMember(2)]
	public Vec3f Position;

	[ProtoMember(3)]
	public bool RelativePosition;

	[ProtoMember(4)]
	public bool ShouldLoop;

	[ProtoMember(5)]
	public bool DisposeOnFinish = true;

	[ProtoMember(6)]
	public float Pitch = 1f;

	[ProtoMember(7)]
	public float LowPassFilter = 1f;

	[ProtoMember(8)]
	public float ReverbDecayTime;

	[ProtoMember(9)]
	public float ReferenceDistance = 3f;

	[ProtoMember(10)]
	private float volume = 1f;

	[ProtoMember(11)]
	public float Range = 32f;

	[ProtoMember(12)]
	public EnumSoundType SoundType;

	public float Volume
	{
		get
		{
			return volume;
		}
		set
		{
			if (value > 1f)
			{
				volume = 1f;
			}
			else if (value < 0f)
			{
				volume = 0f;
			}
			else
			{
				volume = value;
			}
		}
	}

	public SoundParams()
	{
	}

	public SoundParams(AssetLocation location)
	{
		Location = location;
		Position = new Vec3f();
		ShouldLoop = false;
		RelativePosition = false;
	}
}
