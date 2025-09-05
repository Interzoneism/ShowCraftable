using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class AiTaskTargetableAt : AiTaskBaseTargetable
{
	public Vec3d SpawnPos;

	public Vec3d CenterPos;

	protected AiTaskTargetableAt(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
	}

	public override void OnEntityLoaded()
	{
		LoadOrCreateSpawnPos();
	}

	public override void OnEntitySpawn()
	{
		LoadOrCreateSpawnPos();
	}

	public void LoadOrCreateSpawnPos()
	{
		if (entity.WatchedAttributes.HasAttribute("spawnPosX"))
		{
			SpawnPos = new Vec3d(entity.WatchedAttributes.GetDouble("spawnPosX"), entity.WatchedAttributes.GetDouble("spawnPosY"), entity.WatchedAttributes.GetDouble("spawnPosZ"));
			return;
		}
		SpawnPos = entity.ServerPos.XYZ;
		entity.WatchedAttributes.SetDouble("spawnPosX", SpawnPos.X);
		entity.WatchedAttributes.SetDouble("spawnPosY", SpawnPos.Y);
		entity.WatchedAttributes.SetDouble("spawnPosZ", SpawnPos.Z);
	}
}
