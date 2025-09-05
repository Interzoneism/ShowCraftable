using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public delegate void EventBusListenerDelegate(string eventName, ref EnumHandling handling, IAttribute data);
