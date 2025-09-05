using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IClientEventAPI : IEventAPI
{
	event ChatLineDelegate ChatMessage;

	event ClientChatLineDelegate OnSendChatMessage;

	event PlayerEventDelegate PlayerJoin;

	event PlayerEventDelegate PlayerLeave;

	event PlayerEventDelegate PlayerDeath;

	event IsPlayerReadyDelegate IsPlayerReady;

	event PlayerEventDelegate PlayerEntitySpawn;

	event PlayerEventDelegate PlayerEntityDespawn;

	event OnGamePauseResume PauseResume;

	event Action LeaveWorld;

	event Action LeftWorld;

	event BlockChangedDelegate BlockChanged;

	event TestBlockAccessDelegate TestBlockAccess;

	event Vintagestory.API.Common.Func<ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	event Action<ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	event IngameErrorDelegate InGameError;

	event IngameDiscoveryDelegate InGameDiscovery;

	event Action ColorsPresetChanged;

	event Action BlockTexturesLoaded;

	event ActionBoolReturn ReloadShader;

	event Action ReloadTextures;

	event Action LevelFinalize;

	event Action ReloadShapes;

	event Action HotkeysChanged;

	event MouseEventDelegate MouseDown;

	event MouseEventDelegate MouseUp;

	event MouseEventDelegate MouseMove;

	event KeyEventDelegate KeyDown;

	event KeyEventDelegate KeyUp;

	event FileDropDelegate FileDrop;

	void RegisterRenderer(IRenderer renderer, EnumRenderStage renderStage, string profilingName = null);

	void UnregisterRenderer(IRenderer renderer, EnumRenderStage renderStage);

	void RegisterItemstackRenderer(CollectibleObject forObj, ItemRenderDelegate rendererDelegate, EnumItemRenderTarget target);

	void UnregisterItemstackRenderer(CollectibleObject forObj, EnumItemRenderTarget target);

	void RegisterAsyncParticleSpawner(ContinousParticleSpawnTaskDelegate handler);
}
