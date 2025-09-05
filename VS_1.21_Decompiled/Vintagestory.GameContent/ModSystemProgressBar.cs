using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemProgressBar : ModSystem
{
	private List<ProgressBarRenderer> pbrenderer = new List<ProgressBarRenderer>();

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
	}

	public IProgressBar AddProgressbar()
	{
		ProgressBarRenderer progressBarRenderer = new ProgressBarRenderer(capi, pbrenderer.Count * 30);
		capi.Event.RegisterRenderer(progressBarRenderer, EnumRenderStage.Ortho);
		pbrenderer.Add(progressBarRenderer);
		return progressBarRenderer;
	}

	public void RemoveProgressbar(IProgressBar pbr)
	{
		if (pbr != null)
		{
			ProgressBarRenderer progressBarRenderer = pbr as ProgressBarRenderer;
			pbrenderer.Remove(progressBarRenderer);
			capi.Event.UnregisterRenderer(progressBarRenderer, EnumRenderStage.Ortho);
			progressBarRenderer.Dispose();
		}
	}
}
