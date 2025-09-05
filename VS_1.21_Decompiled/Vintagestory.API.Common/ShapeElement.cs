using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class ShapeElement
{
	public static ILogger Logger;

	public static object locationForLogging;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public double[] From;

	[JsonProperty]
	public double[] To;

	[JsonProperty]
	public bool Shade = true;

	[JsonProperty]
	public bool GradientShade;

	[JsonProperty]
	[Obsolete("Use FacesResolved instead")]
	public Dictionary<string, ShapeElementFace> Faces;

	public ShapeElementFace[] FacesResolved = new ShapeElementFace[6];

	[JsonProperty]
	public double[] RotationOrigin;

	[JsonProperty]
	public double RotationX;

	[JsonProperty]
	public double RotationY;

	[JsonProperty]
	public double RotationZ;

	[JsonProperty]
	public double ScaleX = 1.0;

	[JsonProperty]
	public double ScaleY = 1.0;

	[JsonProperty]
	public double ScaleZ = 1.0;

	[JsonProperty]
	public string ClimateColorMap;

	[JsonProperty]
	public string SeasonColorMap;

	[JsonProperty]
	public short RenderPass = -1;

	[JsonProperty]
	public short ZOffset;

	[JsonProperty]
	public bool DisableRandomDrawOffset;

	[JsonProperty]
	public ShapeElement[] Children;

	[JsonProperty]
	public AttachmentPoint[] AttachmentPoints;

	[JsonProperty]
	public string StepParentName;

	public ShapeElement ParentElement;

	public int JointId;

	public int Color = -1;

	public float DamageEffect;

	public float[] inverseModelTransform;

	private static ElementPose noTransform = new ElementPose();

	public List<ShapeElement> GetParentPath()
	{
		List<ShapeElement> list = new List<ShapeElement>();
		for (ShapeElement parentElement = ParentElement; parentElement != null; parentElement = parentElement.ParentElement)
		{
			list.Add(parentElement);
		}
		list.Reverse();
		return list;
	}

	public int CountParents()
	{
		int num = 0;
		for (ShapeElement parentElement = ParentElement; parentElement != null; parentElement = parentElement.ParentElement)
		{
			num++;
		}
		return num;
	}

	public void CacheInverseTransformMatrix()
	{
		if (inverseModelTransform == null)
		{
			inverseModelTransform = GetInverseModelMatrix();
		}
	}

	public float[] GetInverseModelMatrix()
	{
		List<ShapeElement> parentPath = GetParentPath();
		float[] array = Mat4f.Create();
		for (int i = 0; i < parentPath.Count; i++)
		{
			float[] localTransformMatrix = parentPath[i].GetLocalTransformMatrix(0);
			Mat4f.Mul(array, array, localTransformMatrix);
		}
		Mat4f.Mul(array, array, GetLocalTransformMatrix(0));
		return Mat4f.Invert(Mat4f.Create(), array);
	}

	internal void SetJointId(int jointId)
	{
		JointId = jointId;
		ShapeElement[] children = Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				children[i].SetJointId(jointId);
			}
		}
	}

	internal void ResolveRefernces()
	{
		ShapeElement[] children = Children;
		if (children != null)
		{
			foreach (ShapeElement obj in children)
			{
				obj.ParentElement = this;
				obj.ResolveRefernces();
			}
		}
		AttachmentPoint[] attachmentPoints = AttachmentPoints;
		if (attachmentPoints != null)
		{
			for (int j = 0; j < attachmentPoints.Length; j++)
			{
				attachmentPoints[j].ParentElement = this;
			}
		}
	}

	internal void TrimTextureNamesAndResolveFaces()
	{
		if (Faces != null)
		{
			foreach (KeyValuePair<string, ShapeElementFace> face in Faces)
			{
				ShapeElementFace value = face.Value;
				if (value.Enabled)
				{
					BlockFacing blockFacing = BlockFacing.FromFirstLetter(face.Key);
					if (blockFacing == null)
					{
						Logger?.Warning("Shape element in " + locationForLogging?.ToString() + ": Unknown facing '" + blockFacing.Code + "'. Ignoring face.");
					}
					else
					{
						FacesResolved[blockFacing.Index] = value;
						value.Texture = value.Texture.Substring(1).DeDuplicate();
					}
				}
			}
		}
		Faces = null;
		if (Children != null)
		{
			ShapeElement[] children = Children;
			for (int i = 0; i < children.Length; i++)
			{
				children[i].TrimTextureNamesAndResolveFaces();
			}
		}
		Name = Name.DeDuplicate();
		StepParentName = StepParentName.DeDuplicate();
		AttachmentPoint[] attachmentPoints = AttachmentPoints;
		if (attachmentPoints != null)
		{
			for (int j = 0; j < attachmentPoints.Length; j++)
			{
				attachmentPoints[j].DeDuplicate();
			}
		}
	}

	public unsafe float[] GetLocalTransformMatrix(int animVersion, float[] output = null, ElementPose tf = null)
	{
		if (tf == null)
		{
			tf = noTransform;
		}
		if (output == null)
		{
			output = Mat4f.Create();
		}
		byte* intPtr = stackalloc byte[12];
		// IL initblk instruction
		Unsafe.InitBlock(intPtr, 0, 12);
		Span<float> span = new Span<float>(intPtr, 3);
		if (RotationOrigin != null)
		{
			span[0] = (float)RotationOrigin[0] / 16f;
			span[1] = (float)RotationOrigin[1] / 16f;
			span[2] = (float)RotationOrigin[2] / 16f;
		}
		if (animVersion == 1)
		{
			Mat4f.Translate(output, output, span[0], span[1], span[2]);
			Mat4f.Scale(output, output, (float)ScaleX, (float)ScaleY, (float)ScaleZ);
			if (RotationX != 0.0)
			{
				Mat4f.RotateX(output, output, (float)(RotationX * 0.01745329238474369));
			}
			if (RotationY != 0.0)
			{
				Mat4f.RotateY(output, output, (float)(RotationY * 0.01745329238474369));
			}
			if (RotationZ != 0.0)
			{
				Mat4f.RotateZ(output, output, (float)(RotationZ * 0.01745329238474369));
			}
			Mat4f.Translate(output, output, 0f - span[0] + (float)From[0] / 16f + tf.translateX, 0f - span[1] + (float)From[1] / 16f + tf.translateY, 0f - span[2] + (float)From[2] / 16f + tf.translateZ);
			Mat4f.Scale(output, output, tf.scaleX, tf.scaleY, tf.scaleZ);
			if (tf.degX + tf.degOffX != 0f)
			{
				Mat4f.RotateX(output, output, (tf.degX + tf.degOffX) * ((float)Math.PI / 180f));
			}
			if (tf.degY + tf.degOffY != 0f)
			{
				Mat4f.RotateY(output, output, (tf.degY + tf.degOffY) * ((float)Math.PI / 180f));
			}
			if (tf.degZ + tf.degOffZ != 0f)
			{
				Mat4f.RotateZ(output, output, (tf.degZ + tf.degOffZ) * ((float)Math.PI / 180f));
			}
		}
		else
		{
			Mat4f.Translate(output, output, span[0], span[1], span[2]);
			if (RotationX + (double)tf.degX + (double)tf.degOffX != 0.0)
			{
				Mat4f.RotateX(output, output, (float)(RotationX + (double)tf.degX + (double)tf.degOffX) * ((float)Math.PI / 180f));
			}
			if (RotationY + (double)tf.degY + (double)tf.degOffY != 0.0)
			{
				Mat4f.RotateY(output, output, (float)(RotationY + (double)tf.degY + (double)tf.degOffY) * ((float)Math.PI / 180f));
			}
			if (RotationZ + (double)tf.degZ + (double)tf.degOffZ != 0.0)
			{
				Mat4f.RotateZ(output, output, (float)(RotationZ + (double)tf.degZ + (double)tf.degOffZ) * ((float)Math.PI / 180f));
			}
			Mat4f.Scale(output, output, (float)ScaleX * tf.scaleX, (float)ScaleY * tf.scaleY, (float)ScaleZ * tf.scaleZ);
			Mat4f.Translate(output, output, (float)From[0] / 16f + tf.translateX, (float)From[1] / 16f + tf.translateY, (float)From[2] / 16f + tf.translateZ);
			Mat4f.Translate(output, output, 0f - span[0], 0f - span[1], 0f - span[2]);
		}
		return output;
	}

	public ShapeElement Clone()
	{
		ShapeElement shapeElement = new ShapeElement
		{
			AttachmentPoints = (AttachmentPoint[])AttachmentPoints?.Clone(),
			FacesResolved = (ShapeElementFace[])FacesResolved?.Clone(),
			From = (double[])From?.Clone(),
			To = (double[])To?.Clone(),
			inverseModelTransform = (float[])inverseModelTransform?.Clone(),
			JointId = JointId,
			RenderPass = RenderPass,
			RotationX = RotationX,
			RotationY = RotationY,
			RotationZ = RotationZ,
			RotationOrigin = (double[])RotationOrigin?.Clone(),
			SeasonColorMap = SeasonColorMap,
			ClimateColorMap = ClimateColorMap,
			StepParentName = StepParentName,
			Shade = Shade,
			DisableRandomDrawOffset = DisableRandomDrawOffset,
			ZOffset = ZOffset,
			GradientShade = GradientShade,
			ScaleX = ScaleX,
			ScaleY = ScaleY,
			ScaleZ = ScaleZ,
			Name = Name
		};
		ShapeElement[] children = Children;
		if (children != null)
		{
			shapeElement.Children = new ShapeElement[children.Length];
			for (int i = 0; i < children.Length; i++)
			{
				ShapeElement shapeElement2 = children[i].Clone();
				shapeElement2.ParentElement = shapeElement;
				shapeElement.Children[i] = shapeElement2;
			}
		}
		return shapeElement;
	}

	public void SetJointIdRecursive(int jointId)
	{
		JointId = jointId;
		ShapeElement[] children = Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				children[i].SetJointIdRecursive(jointId);
			}
		}
	}

	public void CacheInverseTransformMatrixRecursive()
	{
		CacheInverseTransformMatrix();
		ShapeElement[] children = Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				children[i].CacheInverseTransformMatrixRecursive();
			}
		}
	}

	public void WalkRecursive(Action<ShapeElement> onElem)
	{
		onElem(this);
		ShapeElement[] children = Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				children[i].WalkRecursive(onElem);
			}
		}
	}

	internal bool HasFaces()
	{
		for (int i = 0; i < 6; i++)
		{
			if (FacesResolved[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void FreeRAMServer()
	{
		Faces = null;
		FacesResolved = null;
		ShapeElement[] children = Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				children[i].FreeRAMServer();
			}
		}
	}
}
