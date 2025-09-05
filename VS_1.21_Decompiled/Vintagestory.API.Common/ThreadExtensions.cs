using System.Threading;

namespace Vintagestory.API.Common;

public static class ThreadExtensions
{
	public static void TryStart(this Thread t)
	{
		if (!t.IsAlive)
		{
			t.Start();
		}
	}
}
