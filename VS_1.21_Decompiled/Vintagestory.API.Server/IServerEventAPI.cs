using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IServerEventAPI : IEventAPI
{
	event ChunkColumnBeginLoadChunkThread BeginChunkColumnLoadChunkThread;

	event ChunkColumnLoadedDelegate ChunkColumnLoaded;

	event ChunkColumnUnloadDelegate ChunkColumnUnloaded;

	event CanUseDelegate CanUseBlock;

	event TrySpawnEntityDelegate OnTrySpawnEntity;

	event OnInteractDelegate OnPlayerInteractEntity;

	event PlayerDelegate PlayerCreate;

	event PlayerDelegate PlayerRespawn;

	event PlayerDelegate PlayerJoin;

	event PlayerDelegate PlayerNowPlaying;

	event PlayerDelegate PlayerLeave;

	event PlayerDelegate PlayerDisconnect;

	event PlayerChatDelegate PlayerChat;

	event PlayerDeathDelegate PlayerDeath;

	event PlayerDelegate PlayerSwitchGameMode;

	event Vintagestory.API.Common.Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	event Action<IServerPlayer, ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	[Obsolete("Override Method Modsystem.AssetsFinalize instead")]
	event Action AssetsFinalizers;

	event Action SaveGameLoaded;

	event Action SaveGameCreated;

	event Action WorldgenStartup;

	event Action PhysicsThreadStart;

	event Action GameWorldSave;

	event SuspendServerDelegate ServerSuspend;

	event ResumeServerDelegate ServerResume;

	event BlockPlacedDelegate DidPlaceBlock;

	event CanPlaceOrBreakDelegate CanPlaceOrBreakBlock;

	event BlockBreakDelegate BreakBlock;

	event BlockBrokenDelegate DidBreakBlock;

	event BlockUsedDelegate DidUseBlock;

	IWorldGenHandler GetRegisteredWorldGenHandlers(string worldType);

	bool TriggerTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d position, long herdId);

	void GetWorldgenBlockAccessor(WorldGenThreadDelegate handler);

	void InitWorldGenerator(Action handler, string forWorldType);

	void MapChunkGeneration(MapChunkGeneratorDelegate handler, string forWorldType);

	void MapRegionGeneration(MapRegionGeneratorDelegate handler, string forWorldType);

	void ChunkColumnGeneration(ChunkColumnGenerationDelegate handler, EnumWorldGenPass pass, string forWorldType);

	void WorldgenHook(WorldGenHookDelegate handler, string forWorldType, string hook);

	void TriggerWorldgenHook(string hook, IBlockAccessor blockAccessor, BlockPos pos, string param);

	void ServerRunPhase(EnumServerRunPhase runPhase, Action handler);

	void Timer(Action handler, double interval);

	object TriggerInitWorldGen();

	void PlayerChunkTransition(IServerPlayer player);
}
