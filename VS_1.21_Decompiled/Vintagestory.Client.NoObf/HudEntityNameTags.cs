using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf;

public class HudEntityNameTags : HudElement
{
	private ClientMain game;

	public override double DrawOrder => -0.1;

	public HudEntityNameTags(ICoreClientAPI capi)
		: base(capi)
	{
		TryOpen();
		game = (ClientMain)capi.World;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		int dimension = game.EntityPlayer.Pos.Dimension;
		foreach (Entity value2 in game.LoadedEntities.Values)
		{
			if (game.frustumCuller.SphereInFrustum((float)value2.Pos.X, (float)(value2.Pos.Y + value2.LocalEyePos.Y), (float)value2.Pos.Z, 0.5) && value2.Pos.Dimension == dimension)
			{
				game.EntityRenderers.TryGetValue(value2.EntityId, out var value);
				value?.DoRender2D(deltaTime);
			}
		}
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override bool ShouldReceiveRenderEvents()
	{
		return true;
	}
}
