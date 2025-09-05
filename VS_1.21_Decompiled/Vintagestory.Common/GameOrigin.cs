using System.IO;

namespace Vintagestory.Common;

public class GameOrigin : PathOrigin
{
	public GameOrigin(string assetsPath)
		: this(assetsPath, null)
	{
	}

	public GameOrigin(string assetsPath, string pathForReservedCharsCheck)
		: base("game", assetsPath, pathForReservedCharsCheck)
	{
		domain = "game";
		fullPath = Path.Combine(Path.GetFullPath(assetsPath), "game") + Path.DirectorySeparatorChar;
	}
}
