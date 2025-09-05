using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class GuiComposerManager : IGuiComposerManager
{
	internal Dictionary<string, GuiComposer> dialogComposers = new Dictionary<string, GuiComposer>();

	private ICoreClientAPI api;

	public Dictionary<string, GuiComposer> Composers => dialogComposers;

	public void ClearCache()
	{
		foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
		{
			dialogComposer.Value.Dispose();
		}
		dialogComposers.Clear();
	}

	public void ClearCached(string dialogName)
	{
		if (dialogComposers.TryGetValue(dialogName, out var value))
		{
			value.Dispose();
			dialogComposers.Remove(dialogName);
		}
	}

	public void Dispose(string dialogName)
	{
		if (dialogComposers.TryGetValue(dialogName, out var value))
		{
			value?.Dispose();
			dialogComposers.Remove(dialogName);
		}
	}

	public GuiComposerManager(ICoreClientAPI api)
	{
		this.api = api;
	}

	public GuiComposer Create(string dialogName, ElementBounds bounds)
	{
		GuiComposer guiComposer;
		if (dialogComposers.ContainsKey(dialogName))
		{
			guiComposer = dialogComposers[dialogName];
			guiComposer.Dispose();
		}
		if (bounds.ParentBounds == null)
		{
			bounds.ParentBounds = new ElementWindowBounds();
		}
		guiComposer = new GuiComposer(api, bounds, dialogName);
		guiComposer.composerManager = this;
		dialogComposers[dialogName] = guiComposer;
		return guiComposer;
	}

	public void RecomposeAllDialogs()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		foreach (GuiComposer value in dialogComposers.Values)
		{
			stopwatch.Restart();
			value.Composed = false;
			value.Compose();
			ScreenManager.Platform.CheckGlError("recomp - " + value.DialogName);
			ScreenManager.Platform.Logger.Notification("Recomposed dialog {0} in {1}s", value.DialogName, Math.Round((float)stopwatch.ElapsedMilliseconds / 1000f, 3));
		}
	}

	public void MarkAllDialogsForRecompose()
	{
		foreach (GuiComposer value in dialogComposers.Values)
		{
			value.recomposeOnRender = true;
		}
	}

	public void UnfocusElements()
	{
		UnfocusElementsExcept(null, null);
	}

	public void UnfocusElementsExcept(GuiComposer newFocusedComposer, GuiElement newFocusedElement)
	{
		foreach (GuiComposer value in dialogComposers.Values)
		{
			if (newFocusedComposer != value)
			{
				value.UnfocusOwnElementsExcept(newFocusedElement);
			}
		}
	}
}
