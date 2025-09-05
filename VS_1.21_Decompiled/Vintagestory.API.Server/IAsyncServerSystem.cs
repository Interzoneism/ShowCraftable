namespace Vintagestory.API.Server;

public interface IAsyncServerSystem
{
	int OffThreadInterval();

	void OnSeparateThreadTick();

	void ThreadDispose();
}
