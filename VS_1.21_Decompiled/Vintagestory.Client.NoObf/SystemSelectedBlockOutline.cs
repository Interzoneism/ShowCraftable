using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemSelectedBlockOutline : ClientSystem
{
	private WireframeCube cubeWireFrame;

	public override string Name => "sbo";

	public SystemSelectedBlockOutline(ClientMain game)
		: base(game)
	{
		cubeWireFrame = WireframeCube.CreateUnitCube(game.api, -1);
		game.eventManager.RegisterRenderer(OnRenderFrame3DPost, EnumRenderStage.AfterFinalComposition, Name, 0.9);
	}

	public override void Dispose(ClientMain game)
	{
		cubeWireFrame.Dispose();
	}

	public void OnRenderFrame3DPost(float deltaTime)
	{
		if (!ClientSettings.SelectedBlockOutline)
		{
			return;
		}
		float wireframethickness = ClientSettings.Wireframethickness;
		if (!game.ShouldRender2DOverlays || game.BlockSelection == null)
		{
			return;
		}
		BlockPos blockPos = game.BlockSelection.Position;
		if (game.BlockSelection.DidOffset)
		{
			blockPos = blockPos.AddCopy(game.BlockSelection.Face.Opposite);
		}
		Block block = game.WorldMap.RelaxedBlockAccess.GetBlock(blockPos, 2);
		Cuboidf[] array;
		if (block.SideSolid.Any)
		{
			array = block.GetSelectionBoxes(game.WorldMap.RelaxedBlockAccess, blockPos);
		}
		else
		{
			block = game.WorldMap.RelaxedBlockAccess.GetBlock(blockPos);
			array = game.GetBlockIntersectionBoxes(blockPos);
		}
		if (array == null || array.Length == 0)
		{
			return;
		}
		bool flag = block.DoParticalSelection(game, blockPos);
		Vec4f selectionColor = block.GetSelectionColor(game.api, blockPos);
		double num = (double)blockPos.X + game.Player.Entity.CameraPosOffset.X;
		double num2 = (double)blockPos.InternalY + game.Player.Entity.CameraPosOffset.Y;
		double num3 = (double)blockPos.Z + game.Player.Entity.CameraPosOffset.Z;
		for (int i = 0; i < array.Length; i++)
		{
			if (flag)
			{
				i = game.BlockSelection.SelectionBoxIndex;
			}
			if (array.Length <= i)
			{
				break;
			}
			Cuboidf cuboidf = array[i];
			if (cuboidf is DecorSelectionBox)
			{
				if (flag)
				{
					break;
				}
				continue;
			}
			double posx = num + (double)cuboidf.X1;
			double posy = num2 + (double)cuboidf.Y1;
			double posz = num3 + (double)cuboidf.Z1;
			cubeWireFrame.Render(game.api, posx, posy, posz, cuboidf.XSize, cuboidf.YSize, cuboidf.ZSize, 1.6f * wireframethickness, selectionColor);
			if (flag)
			{
				break;
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
