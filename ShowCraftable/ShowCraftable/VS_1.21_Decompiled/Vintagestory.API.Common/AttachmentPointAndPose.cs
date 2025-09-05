using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AttachmentPointAndPose
{
	public float[] AnimModelMatrix;

	public ElementPose CachedPose;

	public AttachmentPoint AttachPoint;

	public AttachmentPointAndPose()
	{
		AnimModelMatrix = Mat4f.Create();
	}

	public Matrixf Mul(Matrixf m)
	{
		AttachmentPoint attachPoint = AttachPoint;
		m.Mul(AnimModelMatrix);
		m.Translate(attachPoint.PosX / 16.0, attachPoint.PosY / 16.0, attachPoint.PosZ / 16.0);
		m.Translate(-0.5f, -0.5f, -0.5f);
		m.RotateX((float)attachPoint.RotationX * ((float)Math.PI / 180f));
		m.RotateY((float)attachPoint.RotationY * ((float)Math.PI / 180f));
		m.RotateZ((float)attachPoint.RotationZ * ((float)Math.PI / 180f));
		m.Translate(0.5f, 0.5f, 0.5f);
		return m;
	}

	public Matrixf MulUncentered(Matrixf m)
	{
		AttachmentPoint attachPoint = AttachPoint;
		m.Mul(AnimModelMatrix);
		m.Translate(attachPoint.PosX / 16.0, attachPoint.PosY / 16.0, attachPoint.PosZ / 16.0);
		m.RotateX((float)attachPoint.RotationX * ((float)Math.PI / 180f));
		m.RotateY((float)attachPoint.RotationY * ((float)Math.PI / 180f));
		m.RotateZ((float)attachPoint.RotationZ * ((float)Math.PI / 180f));
		return m;
	}
}
