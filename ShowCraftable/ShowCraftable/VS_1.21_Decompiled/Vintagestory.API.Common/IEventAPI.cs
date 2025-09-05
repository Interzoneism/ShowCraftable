using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IEventAPI
{
	event EntityMountDelegate EntityMounted;

	event EntityMountDelegate EntityUnmounted;

	event PlayerCommonDelegate PlayerDimensionChanged;

	event TestBlockAccessDelegate OnTestBlockAccess;

	event TestBlockAccessClaimDelegate OnTestBlockAccessClaim;

	event EntityDelegate OnEntitySpawn;

	event EntityDelegate OnEntityLoaded;

	event EntityDeathDelegate OnEntityDeath;

	event EntityDespawnDelegate OnEntityDespawn;

	event ChunkDirtyDelegate ChunkDirty;

	event MapRegionLoadedDelegate MapRegionLoaded;

	event MapRegionUnloadDelegate MapRegionUnloaded;

	event OnGetClimateDelegate OnGetClimate;

	event OnGetWindSpeedDelegate OnGetWindSpeed;

	event MatchGridRecipeDelegate MatchesGridRecipe;

	void PushEvent(string eventName, IAttribute data = null);

	void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null);

	long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0);

	long RegisterGameTickListener(Action<float> onGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0);

	long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> onGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0);

	long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay);

	long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused);

	long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay);

	void UnregisterCallback(long listenerId);

	void UnregisterGameTickListener(long listenerId);

	void EnqueueMainThreadTask(Action action, string code);

	void TriggerPlayerDimensionChanged(IPlayer player);

	void TriggerEntityDeath(Entity entity, DamageSource damageSourceForDeath);

	bool TriggerMatchesRecipe(IPlayer forPlayer, GridRecipe gridRecipe, ItemSlot[] ingredients, int gridWidth);

	void TriggerEntityMounted(EntityAgent entityAgent, IMountableSeat entityRideableSeat);

	void TriggerEntityUnmounted(EntityAgent entityAgent, IMountableSeat entityRideableSeat);
}
