using System;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class ModelTransformNoDefaults
{
	public static readonly FastVec3f defaultTf = new FastVec3f(-9.9E-05f, -9.9E-05f, -9.9E-05f);

	[JsonProperty]
	public FastVec3f Translation = defaultTf;

	[JsonProperty]
	public FastVec3f Rotation = defaultTf;

	[JsonProperty]
	public FastVec3f Origin = new FastVec3f(0.5f, 0.5f, 0.5f);

	[JsonProperty]
	public bool Rotate = true;

	[JsonProperty]
	public FastVec3f ScaleXYZ = new FastVec3f(1f, 1f, 1f);

	[JsonProperty]
	public float Scale
	{
		set
		{
			ScaleXYZ.Set(value, value, value);
		}
	}

	public float[] AsMatrix
	{
		get
		{
			float[] array = Mat4f.Create();
			Mat4f.Translate(array, array, Translation.X, Translation.Y, Translation.Z);
			Mat4f.Translate(array, array, Origin.X, Origin.Y, Origin.Z);
			if (Rotation.X != 0f || Rotation.Y != 0f || Rotation.Z != 0f)
			{
				Mat4f.RotateX(array, array, Rotation.X * ((float)Math.PI / 180f));
				Mat4f.RotateY(array, array, Rotation.Y * ((float)Math.PI / 180f));
				Mat4f.RotateZ(array, array, Rotation.Z * ((float)Math.PI / 180f));
			}
			Mat4f.Scale(array, array, ScaleXYZ.X, ScaleXYZ.Y, ScaleXYZ.Z);
			Mat4f.Translate(array, array, 0f - Origin.X, 0f - Origin.Y, 0f - Origin.Z);
			return array;
		}
	}

	public void Clear()
	{
		Rotation.Set(0f, 0f, 0f);
		Translation.Set(0f, 0f, 0f);
		Origin.Set(0f, 0f, 0f);
		Scale = 1f;
	}
}
