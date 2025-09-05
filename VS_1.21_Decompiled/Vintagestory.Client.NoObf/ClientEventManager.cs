using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientEventManager : EventManager
{
	public OnAmbientSoundScanCompleteDelegate OnAmbientSoundsScanComplete;

	public CurrentTrackSupplierDelegate CurrentTrackSupplier;

	public TrackStarterDelegate TrackStarter;

	public TrackStarterLoadedDelegate TrackStarterLoaded;

	public Dictionary<Vec3i, List<RetesselationListener>> OnChunkRetesselated = new Dictionary<Vec3i, List<RetesselationListener>>();

	public List<Action<bool>> OnGameWindowFocus = new List<Action<bool>>();

	public List<Action<GuiDialog>> OnDialogOpened = new List<Action<GuiDialog>>();

	public List<Action<GuiDialog>> OnDialogClosed = new List<Action<GuiDialog>>();

	public List<Action<BlockDamage>> OnUnBreakingBlock = new List<Action<BlockDamage>>();

	public List<Action<BlockDamage>> OnPlayerBreakingBlock = new List<Action<BlockDamage>>();

	public List<Action<BlockDamage>> OnPlayerBrokenBlock = new List<Action<BlockDamage>>();

	public List<BlockChangedDelegate> OnBlockChanged = new List<BlockChangedDelegate>();

	public List<Action> OnPlayerModeChange = new List<Action>();

	public List<ActionBoolReturn> OnReloadShaders = new List<ActionBoolReturn>();

	public List<EntityDelegate> OnEntitySpawn = new List<EntityDelegate>();

	public List<EntityDelegate> OnEntityLoaded = new List<EntityDelegate>();

	public List<EntityDespawnDelegate> OnEntityDespawn = new List<EntityDespawnDelegate>();

	public List<PlayerDeathDelegate> OnPlayerDeath = new List<PlayerDeathDelegate>();

	public Dictionary<EnumProperty, List<OnPlayerPropertyChanged>> OnPlayerPropertyChanged = new Dictionary<EnumProperty, List<OnPlayerPropertyChanged>>();

	public List<Action> OnActiveSlotChanged = new List<Action>();

	public List<ChatLineDelegate> OnNewServerToClientChatLine = new List<ChatLineDelegate>();

	public List<ChatLineDelegate> OnNewClientToServerChatLine = new List<ChatLineDelegate>();

	public List<ChatLineDelegate> OnNewClientOnlyChatLine = new List<ChatLineDelegate>();

	public List<EventBusListener> EventBusListeners = new List<EventBusListener>();

	public List<RenderHandler>[] renderersByStage;

	private ClientMain game;

	public override ILogger Logger => ScreenManager.Platform.Logger;

	public override string CommandPrefix => ".";

	public override long InWorldEllapsedMs => game.InWorldEllapsedMs;

	public event HighlightBlocksDelegate OnHighlightBlocks;

	public event UpdateLightingDelegate OnUpdateLighting;

	public event ChunkLoadedDelegate OnChunkLoaded;

	public event Action OnReloadShapes;

	public event Action OnReloadTextures;

	public event IngameErrorDelegate InGameError;

	public event IngameDiscoveryDelegate InGameDiscovery;

	public event Action ColorPresetChanged;

	public ClientEventManager(ClientMain game)
	{
		this.game = game;
		renderersByStage = new List<RenderHandler>[Enum.GetNames(typeof(EnumRenderStage)).Length];
		for (int i = 0; i < renderersByStage.Length; i++)
		{
			renderersByStage[i] = new List<RenderHandler>();
		}
	}

	public void RegisterRenderer(Action<float> handler, EnumRenderStage stage, string profilingName, double renderOrder)
	{
		RegisterRenderer(new DummyRenderer
		{
			action = handler,
			RenderOrder = renderOrder
		}, stage, profilingName);
	}

	public void RegisterRenderer(IRenderer handler, EnumRenderStage stage, string profilingName)
	{
		List<RenderHandler> list = renderersByStage[(int)stage];
		int num = 0;
		for (int i = 0; i < list.Count && handler.RenderOrder > list[i].Renderer.RenderOrder; i++)
		{
			num++;
		}
		list.Insert(num, new RenderHandler
		{
			Renderer = handler,
			ProfilingName = stage.ToString() + "-" + profilingName
		});
	}

	public void UnregisterRenderer(IRenderer handler, EnumRenderStage stage)
	{
		List<RenderHandler> list = renderersByStage[(int)stage];
		RenderHandler renderHandler = list.FirstOrDefault((RenderHandler x) => x.Renderer == handler);
		if (renderHandler != null)
		{
			list.Remove(renderHandler);
		}
	}

	public void TriggerHighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		this.OnHighlightBlocks?.Invoke(player, slotId, blocks, colors, mode, shape, scale);
	}

	public void TriggerRenderStage(EnumRenderStage stage, float dt)
	{
		List<RenderHandler> list = renderersByStage[(int)stage];
		if (game.extendedDebugInfo)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Renderer.OnRenderFrame(dt, stage);
				ScreenManager.Platform.CheckGlError(list[i].ProfilingName);
				ScreenManager.FrameProfiler.Mark(list[i].ProfilingName);
			}
		}
		else
		{
			for (int j = 0; j < list.Count; j++)
			{
				list[j].Renderer.OnRenderFrame(dt, stage);
			}
		}
	}

	public void TriggerLightingUpdate(int oldBlockId, int newBlockId, BlockPos pos, Dictionary<BlockPos, BlockUpdate> blockUpdatesBulk = null)
	{
		this.OnUpdateLighting?.Invoke(oldBlockId, newBlockId, pos, blockUpdatesBulk);
	}

	public void TriggerChunkLoaded(Vec3i chunkpos)
	{
		this.OnChunkLoaded?.Invoke(chunkpos);
	}

	public void RegisterReloadShaders(ActionBoolReturn handler)
	{
		OnReloadShaders.Add(handler);
	}

	public void UnregisterReloadShaders(ActionBoolReturn handler)
	{
		OnReloadShaders.Remove(handler);
	}

	public void RegisterOnChunkRetesselated(Vec3i chunkPos, int atQuantityDrawn, Action listener)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			game.EnqueueMainThreadTask(delegate
			{
				if (!OnChunkRetesselated.TryGetValue(chunkPos, out var value2))
				{
					value2 = new List<RetesselationListener>();
					OnChunkRetesselated[chunkPos] = value2;
				}
				if (listener == null)
				{
					throw new ArgumentNullException("Listener cannot be null");
				}
				value2.Add(new RetesselationListener
				{
					AtDrawCount = atQuantityDrawn,
					Handler = listener
				});
			}, "reg-chunkretess");
		}
		else
		{
			if (!OnChunkRetesselated.TryGetValue(chunkPos, out var value))
			{
				value = new List<RetesselationListener>();
				OnChunkRetesselated[chunkPos] = value;
			}
			if (listener == null)
			{
				throw new ArgumentNullException("Listener cannot be null");
			}
			value.Add(new RetesselationListener
			{
				AtDrawCount = atQuantityDrawn,
				Handler = listener
			});
		}
	}

	public void TriggerChunkRetesselated(Vec3i chunkPos, ClientChunk chunk)
	{
		if (!OnChunkRetesselated.TryGetValue(chunkPos, out var value))
		{
			return;
		}
		int i = 0;
		try
		{
			for (i = 0; i < value.Count; i++)
			{
				RetesselationListener retesselationListener = value[i];
				if (retesselationListener.AtDrawCount < chunk.quantityDrawn)
				{
					retesselationListener.Handler();
					value.RemoveAt(i);
					i--;
				}
			}
		}
		catch (Exception ex)
		{
			throw new Exception("Chunk retesselated listener number " + i + " threw an exception (a=" + (value == null) + ", b=" + (value?[i] == null) + ", b=" + (value?[i]?.Handler == null) + ")\n" + ex);
		}
	}

	public void TriggerDialogOpened(GuiDialog dialog)
	{
		foreach (Action<GuiDialog> item in OnDialogOpened)
		{
			item(dialog);
		}
	}

	public void TriggerDialogClosed(GuiDialog dialog)
	{
		foreach (Action<GuiDialog> item in OnDialogClosed)
		{
			item(dialog);
		}
	}

	public void TriggerGameWindowFocus(bool focus)
	{
		foreach (Action<bool> item in OnGameWindowFocus)
		{
			item(focus);
		}
	}

	public void TriggerNewModChatLine(int groupid, string message, EnumChatType chattype, string data)
	{
		foreach (ChatLineDelegate item in OnNewClientOnlyChatLine)
		{
			item(groupid, message, chattype, data);
		}
	}

	public void TriggerNewClientChatLine(int groupid, string message, EnumChatType chattype, string data)
	{
		foreach (ChatLineDelegate item in OnNewClientToServerChatLine)
		{
			item(groupid, message, chattype, data);
		}
	}

	public void TriggerIngameError(object sender, string errorCode, string text)
	{
		this.InGameError?.Invoke(sender, errorCode, text);
	}

	public void TriggerIngameDiscovery(object sender, string errorCode, string text)
	{
		this.InGameDiscovery?.Invoke(sender, errorCode, text);
	}

	public void TriggerColorPresetChanged()
	{
		this.ColorPresetChanged?.Invoke();
	}

	public void TriggerNewServerChatLine(int groupid, string message, EnumChatType chattype, string data)
	{
		foreach (ChatLineDelegate item in OnNewServerToClientChatLine)
		{
			item(groupid, message, chattype, data);
		}
	}

	public bool TriggerBeforeActiveSlotChanged(ClientMain game, int fromSlot, int toSlot)
	{
		if (!game.api.eventapi.TriggerBeforeActiveSlotChanged(game.Logger, fromSlot, toSlot))
		{
			return false;
		}
		foreach (Action item in OnActiveSlotChanged)
		{
			item();
		}
		return true;
	}

	public void TriggerAfterActiveSlotChanged(ClientMain game, int fromSlot, int toSlot)
	{
		game.api.eventapi.TriggerAfterActiveSlotChanged(game.Logger, fromSlot, toSlot);
	}

	public void TriggerBlockBreaking(BlockDamage blockDamage)
	{
		foreach (Action<BlockDamage> item in OnPlayerBreakingBlock)
		{
			item(blockDamage);
		}
	}

	internal void TriggerBlockUnbreaking(BlockDamage damagedBlock)
	{
		foreach (Action<BlockDamage> item in OnUnBreakingBlock)
		{
			item(damagedBlock);
		}
	}

	public void TriggerBlockBroken(BlockDamage blockDamage)
	{
		foreach (Action<BlockDamage> item in OnPlayerBrokenBlock)
		{
			item(blockDamage);
		}
	}

	public void TriggerPlayerModeChange()
	{
		foreach (Action item in OnPlayerModeChange)
		{
			item();
		}
	}

	public void TriggerReloadShapes()
	{
		this.OnReloadShapes?.Invoke();
	}

	public void TriggerReloadTextures()
	{
		this.OnReloadTextures?.Invoke();
	}

	public bool TriggerReloadShaders()
	{
		bool flag = true;
		foreach (ActionBoolReturn onReloadShader in OnReloadShaders)
		{
			flag &= onReloadShader();
		}
		return flag;
	}

	public void TriggerEntitySpawn(Entity entity)
	{
		foreach (EntityDelegate item in OnEntitySpawn)
		{
			item(entity);
		}
	}

	public void TriggerEntityLoaded(Entity entity)
	{
		foreach (EntityDelegate item in OnEntityLoaded)
		{
			item(entity);
		}
	}

	public void TriggerEntityDespawn(Entity entity, EntityDespawnData despawnReason)
	{
		foreach (EntityDespawnDelegate item in OnEntityDespawn)
		{
			item(entity, despawnReason);
		}
	}

	public void RegisterPlayerPropertyChangedWatcher(EnumProperty property, OnPlayerPropertyChanged handler)
	{
		OnPlayerPropertyChanged.TryGetValue(property, out var value);
		if (value == null)
		{
			value = (OnPlayerPropertyChanged[property] = new List<OnPlayerPropertyChanged>());
		}
		value.Add(handler);
	}

	internal void TriggerPlayerDeath(int clientId, int livesLeft)
	{
		foreach (PlayerDeathDelegate item in OnPlayerDeath)
		{
			item(clientId, livesLeft);
		}
	}

	internal void TriggerBlockChanged(ClientMain game, BlockPos pos, Block oldBlock)
	{
		game.api.eventapi.TriggerBlockChanged(pos, oldBlock);
		foreach (BlockChangedDelegate item in OnBlockChanged)
		{
			item(pos, oldBlock);
		}
	}

	public override bool HasPrivilege(string playerUid, string privilegecode)
	{
		return true;
	}

	public void TriggerOnMouseEnterSlot(ClientMain game, ItemSlot slot)
	{
		game.player.inventoryMgr.currentHoveredSlot = slot;
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			if (loadedGui.OnMouseEnterSlot(slot))
			{
				return;
			}
		}
		for (int i = 0; i < game.clientSystems.Length && !game.clientSystems[i].OnMouseEnterSlot(slot); i++)
		{
		}
	}

	public void TriggerOnMouseLeaveSlot(ClientMain game, ItemSlot itemSlot)
	{
		game.player.inventoryMgr.currentHoveredSlot = null;
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			if (loadedGui.OnMouseLeaveSlot(itemSlot))
			{
				return;
			}
		}
		for (int i = 0; i < game.clientSystems.Length && !game.clientSystems[i].OnMouseLeaveSlot(itemSlot); i++)
		{
		}
	}

	internal void Dispose()
	{
		List<RenderHandler>[] array = renderersByStage;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (RenderHandler item in new List<RenderHandler>(array[i]))
			{
				item.Renderer.Dispose();
			}
		}
		this.ColorPresetChanged = null;
	}
}
