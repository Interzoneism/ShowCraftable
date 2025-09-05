using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IModLoader
{
	IEnumerable<Mod> Mods { get; }

	IEnumerable<ModSystem> Systems { get; }

	Mod GetMod(string modID);

	bool IsModEnabled(string modID);

	ModSystem GetModSystem(string fullName);

	T GetModSystem<T>(bool withInheritance = true) where T : ModSystem;

	bool IsModSystemEnabled(string fullName);
}
