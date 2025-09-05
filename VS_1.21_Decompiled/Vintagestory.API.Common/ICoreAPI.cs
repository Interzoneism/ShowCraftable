using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public interface ICoreAPI : ICoreAPICommon
{
	ILogger Logger { get; }

	string[] CmdlArguments { get; }

	IChatCommandApi ChatCommands { get; }

	EnumAppSide Side { get; }

	IEventAPI Event { get; }

	IWorldAccessor World { get; }

	IClassRegistryAPI ClassRegistry { get; }

	INetworkAPI Network { get; }

	IAssetManager Assets { get; }

	IModLoader ModLoader { get; }

	ITagRegistry TagRegistry { get; }

	void RegisterEntityClass(string entityClassName, EntityProperties config);
}
