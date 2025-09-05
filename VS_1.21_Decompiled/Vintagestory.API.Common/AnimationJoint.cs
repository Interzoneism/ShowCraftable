using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AnimationJoint
{
	public int JointId;

	public ShapeElement Element;

	public float[] ApplyInverseTransform(float[] frameModelTransform)
	{
		List<ShapeElement> parentPath = Element.GetParentPath();
		float[] array = Mat4f.Create();
		for (int i = 0; i < parentPath.Count; i++)
		{
			float[] localTransformMatrix = parentPath[i].GetLocalTransformMatrix(0);
			Mat4f.Mul(array, array, localTransformMatrix);
		}
		Mat4f.Mul(array, array, Element.GetLocalTransformMatrix(0));
		float[] b = Mat4f.Invert(Mat4f.Create(), array);
		return Mat4f.Mul(frameModelTransform, frameModelTransform, b);
	}
}
