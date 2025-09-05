using OpenTK.Windowing.Desktop;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IXPlatformInterface
{
	GameWindow Window { get; set; }

	void SetClipboardText(string text);

	string GetClipboardText();

	void ShowMessageBox(string title, string text);

	Size2i GetScreenSize();

	IAviWriter GetAviWriter(int recordingBufferSize, double framerate, string codeccode);

	AvailableCodec[] AvailableCodecs();

	void MoveFileToRecyclebin(string filepath);

	long GetFreeDiskSpace(string filepath);

	long GetRamCapacity();

	string GetCpuInfo();

	void FocusWindow();
}
