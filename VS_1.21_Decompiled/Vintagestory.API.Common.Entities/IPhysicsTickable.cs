namespace Vintagestory.API.Common.Entities;

public interface IPhysicsTickable
{
	bool Ticking { get; set; }

	Entity Entity { get; }

	void OnPhysicsTick(float dt);

	void AfterPhysicsTick(float dt);
}
