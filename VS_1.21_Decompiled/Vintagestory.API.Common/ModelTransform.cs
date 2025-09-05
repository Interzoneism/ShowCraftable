using System.Runtime.Serialization;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class ModelTransform : ModelTransformNoDefaults
{
	public static ModelTransform NoTransform => new ModelTransform().EnsureDefaultValues();

	public ModelTransform(ModelTransformNoDefaults baseTf, ModelTransform defaults)
	{
		Rotate = baseTf.Rotate;
		if (baseTf.Translation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Translation = defaults.Translation;
		}
		else
		{
			Translation = baseTf.Translation;
		}
		if (baseTf.Rotation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Rotation = defaults.Rotation;
		}
		else
		{
			Rotation = baseTf.Rotation;
		}
		Origin = baseTf.Origin;
		ScaleXYZ = baseTf.ScaleXYZ;
	}

	public ModelTransform()
	{
	}

	public static ModelTransform BlockDefaultGui()
	{
		return new ModelTransform
		{
			Translation = default(FastVec3f),
			Rotation = new FastVec3f(-22.6f, -45.3f, 0f),
			Scale = 1f
		};
	}

	public static ModelTransform BlockDefaultFp()
	{
		return new ModelTransform
		{
			Translation = new FastVec3f(0f, -0.15f, 0.5f),
			Rotation = new FastVec3f(0f, -20f, 0f),
			Scale = 1.3f
		};
	}

	public static ModelTransform BlockDefaultTp()
	{
		return new ModelTransform
		{
			Translation = new FastVec3f(-2.1f, -1.8f, -1.5f),
			Rotation = new FastVec3f(0f, -45f, -25f),
			Scale = 0.3f
		};
	}

	public static ModelTransform BlockDefaultGround()
	{
		return new ModelTransform
		{
			Translation = default(FastVec3f),
			Rotation = new FastVec3f(0f, -45f, 0f),
			Origin = new FastVec3f(0.5f, 0f, 0.5f),
			Scale = 1.5f
		};
	}

	public static ModelTransform ItemDefaultGui()
	{
		return new ModelTransform
		{
			Translation = new FastVec3f(3f, 1f, 0f),
			Rotation = new FastVec3f(0f, 0f, 0f),
			Origin = new FastVec3f(0.6f, 0.6f, 0f),
			Scale = 1f,
			Rotate = false
		};
	}

	public static ModelTransform ItemDefaultFp()
	{
		return new ModelTransform
		{
			Translation = new FastVec3f(0.05f, 0f, 0f),
			Rotation = new FastVec3f(180f, 90f, -30f),
			Scale = 1f
		};
	}

	public static ModelTransform ItemDefaultTp()
	{
		return new ModelTransform
		{
			Translation = new FastVec3f(-1.5f, -1.6f, -1.4f),
			Rotation = new FastVec3f(0f, -62f, 18f),
			Scale = 0.33f
		};
	}

	public static ModelTransform ItemDefaultGround()
	{
		return new ModelTransform
		{
			Translation = default(FastVec3f),
			Rotation = new FastVec3f(90f, 0f, 0f),
			Origin = new FastVec3f(0.5f, 0.5f, 0.53f),
			Scale = 1.5f
		};
	}

	public ModelTransform EnsureDefaultValues()
	{
		if (Translation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Translation = default(FastVec3f);
		}
		if (Rotation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Rotation = default(FastVec3f);
		}
		return this;
	}

	public ModelTransform WithRotation(Vec3f rot)
	{
		Rotation = new FastVec3f(rot);
		return this;
	}

	public ModelTransform Clone()
	{
		return new ModelTransform
		{
			Rotate = Rotate,
			Rotation = Rotation,
			Translation = Translation,
			ScaleXYZ = ScaleXYZ,
			Origin = Origin
		};
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Translation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Translation = default(FastVec3f);
		}
		if (Rotation.Equals(ModelTransformNoDefaults.defaultTf))
		{
			Rotation = default(FastVec3f);
		}
	}
}
