using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface ICoreClientAPI : ICoreAPI, ICoreAPICommon
{
	Dictionary<string, Action<LinkTextComponent>> LinkProtocols { get; }

	Dictionary<string, Tag2RichTextDelegate> TagConverters { get; }

	ISettings Settings { get; }

	IXPlatformInterface Forms { get; }

	IMacroManager MacroManager { get; }

	long ElapsedMilliseconds { get; }

	long InWorldEllapsedMilliseconds { get; }

	bool IsShuttingDown { get; }

	bool IsGamePaused { get; }

	bool IsSinglePlayer { get; }

	bool OpenedToLan { get; }

	bool HideGuis { get; }

	bool PlayerReadyFired { get; }

	IAmbientManager Ambient { get; }

	new IClientEventAPI Event { get; }

	IRenderAPI Render { get; }

	IGuiAPI Gui { get; }

	IInputAPI Input { get; }

	ITesselatorManager TesselatorManager { get; }

	ITesselatorAPI Tesselator { get; }

	IBlockTextureAtlasAPI BlockTextureAtlas { get; }

	IItemTextureAtlasAPI ItemTextureAtlas { get; }

	ITextureAtlasAPI EntityTextureAtlas { get; }

	IColorPresets ColorPreset { get; }

	IShaderAPI Shader { get; }

	new IClientNetworkAPI Network { get; }

	new IClientWorldAccessor World { get; }

	IEnumerable<object> OpenedGuis { get; }

	IMusicTrack CurrentMusicTrack { get; }

	[Obsolete("Use ChatCommand subapi instead")]
	bool RegisterCommand(ClientChatCommand chatcommand);

	[Obsolete("Use ChatCommand subapi instead")]
	bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler);

	void RegisterEntityRendererClass(string className, Type rendererType);

	void RegisterLinkProtocol(string protocolname, Action<LinkTextComponent> onLinkClicked);

	void ShowChatMessage(string message);

	void TriggerIngameDiscovery(object sender, string errorCode, string text);

	void TriggerIngameError(object sender, string errorCode, string text);

	void TriggerChatMessage(string message);

	void SendChatMessage(string message, int groupId, string data = null);

	void SendChatMessage(string message, string data = null);

	MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null);

	void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playnow = true);

	void PauseGame(bool paused);
}
