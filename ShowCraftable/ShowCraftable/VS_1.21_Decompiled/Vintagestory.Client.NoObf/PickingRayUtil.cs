using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class PickingRayUtil
{
	private Unproject unproject;

	private double[] tempViewport;

	private double[] tempRay;

	private double[] tempRayStartPoint;

	public PickingRayUtil()
	{
		unproject = new Unproject();
		tempViewport = new double[4];
		tempRay = new double[4];
		tempRayStartPoint = new double[4];
	}

	public Ray GetPickingRayByMouseCoordinates(ClientMain game)
	{
		int mouseCurrentX = game.MouseCurrentX;
		int mouseCurrentY = game.MouseCurrentY;
		tempViewport[0] = 0.0;
		tempViewport[1] = 0.0;
		tempViewport[2] = game.Width;
		tempViewport[3] = game.Height;
		unproject.UnProject(mouseCurrentX, game.Height - mouseCurrentY, 1, game.MvMatrix.Top, game.PMatrix.Top, tempViewport, tempRay);
		unproject.UnProject(mouseCurrentX, game.Height - mouseCurrentY, 0, game.MvMatrix.Top, game.PMatrix.Top, tempViewport, tempRayStartPoint);
		double num = tempRay[0] - tempRayStartPoint[0];
		double num2 = tempRay[1] - tempRayStartPoint[1];
		double num3 = tempRay[2] - tempRayStartPoint[2];
		float num4 = Length((float)num, (float)num2, (float)num3);
		num /= (double)num4;
		num2 /= (double)num4;
		num3 /= (double)num4;
		float pickingRange = game.player.WorldData.PickingRange;
		bool flag = game.MainCamera.CameraMode != EnumCameraMode.FirstPerson && (game.MouseGrabbed || game.mouseWorldInteractAnyway);
		Ray ray = new Ray(new Vec3d(tempRayStartPoint[0] + (flag ? (num * (double)game.MainCamera.Tppcameradistance) : 0.0), tempRayStartPoint[1] + (flag ? (num2 * (double)game.MainCamera.Tppcameradistance) : 0.0), tempRayStartPoint[2] + (flag ? (num3 * (double)game.MainCamera.Tppcameradistance) : 0.0)), new Vec3d(num * (double)pickingRange, num2 * (double)pickingRange, num3 * (double)pickingRange));
		if (double.IsNaN(ray.origin.X))
		{
			return null;
		}
		return ray;
	}

	internal float Length(float x, float y, float z)
	{
		return (float)Math.Sqrt(x * x + y * y + z * z);
	}
}
