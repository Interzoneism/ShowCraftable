using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class DimensionSwitchForEntity
{
	[ProtoMember(1)]
	public long entityId;

	[ProtoMember(2)]
	public int dimension;

	public DimensionSwitchForEntity()
	{
	}

	public DimensionSwitchForEntity(long entityId, int dimension)
	{
		this.entityId = entityId;
		this.dimension = dimension;
	}
}
