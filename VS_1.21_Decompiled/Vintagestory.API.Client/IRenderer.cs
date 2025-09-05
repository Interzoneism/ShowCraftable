using System;

namespace Vintagestory.API.Client;

public interface IRenderer : IDisposable
{
	double RenderOrder { get; }

	int RenderRange { get; }

	void OnRenderFrame(float deltaTime, EnumRenderStage stage);
}
