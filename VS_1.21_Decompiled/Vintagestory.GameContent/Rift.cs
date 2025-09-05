using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class Rift
{
	[ProtoMember(1)]
	public int RiftId;

	[ProtoMember(2)]
	public float Size = 1f;

	[ProtoMember(3)]
	public Vec3d Position;

	[ProtoMember(4)]
	public double SpawnedTotalHours;

	[ProtoMember(5)]
	public double DieAtTotalHours;

	public bool Visible = true;

	public bool HasLineOfSight;

	public float VolumeMul;

	public float accum = 2f;

	public float GetNowSize(ICoreAPI api)
	{
		float num = (float)GameMath.Clamp((DieAtTotalHours - api.World.Calendar.TotalHours) * 10.0, 0.0, 1.0);
		float num2 = GameMath.Serp(0f, 1f, (float)GameMath.Clamp((api.World.Calendar.TotalHours - SpawnedTotalHours) * 20.0, 0.0, 1.0));
		return Size * num * num2;
	}

	public void OnNearTick(ICoreClientAPI capi, float dt)
	{
		if (Size <= 0f)
		{
			return;
		}
		accum += dt;
		if (accum > 2f)
		{
			accum = 0f;
			Vec3d vec3d = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
			if ((double)Position.DistanceTo(vec3d) < 24.0)
			{
				BlockSelection selectedBlock = capi.World.InteresectionTester.GetSelectedBlock(vec3d, Position, (BlockPos pos, Block block) => block.CollisionBoxes != null && block.CollisionBoxes.Length != 0 && block.BlockMaterial != EnumBlockMaterial.Leaves);
				HasLineOfSight = selectedBlock == null;
			}
		}
		VolumeMul = GameMath.Clamp(VolumeMul + dt * (HasLineOfSight ? 0.5f : (-0.5f)), 0.15f, 1f);
	}

	internal void SetFrom(Rift rift)
	{
		Size = rift.Size;
		Position = rift.Position;
		SpawnedTotalHours = rift.SpawnedTotalHours;
		DieAtTotalHours = rift.DieAtTotalHours;
	}
}
