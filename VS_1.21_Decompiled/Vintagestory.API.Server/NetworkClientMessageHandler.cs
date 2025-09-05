namespace Vintagestory.API.Server;

public delegate void NetworkClientMessageHandler<T>(IServerPlayer fromPlayer, T packet);
