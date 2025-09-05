using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate void PlayerChatDelegate(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed);
