using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramCloudvolumetric : ShaderProgram
{
	public float[] IMvpMatrix
	{
		set
		{
			UniformMatrix("iMvpMatrix", value);
		}
	}

	public int DepthTex2D
	{
		set
		{
			BindTexture2D("depthTex", value, 0);
		}
	}

	public int CloudMap2D
	{
		set
		{
			BindTexture2D("cloudMap", value, 1);
		}
	}

	public int CloudCol2D
	{
		set
		{
			BindTexture2D("cloudCol", value, 2);
		}
	}

	public float CloudMapWidth
	{
		set
		{
			Uniform("cloudMapWidth", value);
		}
	}

	public Vec3f CloudOffset
	{
		set
		{
			Uniform("cloudOffset", value);
		}
	}

	public int Frame
	{
		set
		{
			Uniform("frame", value);
		}
	}

	public float Time
	{
		set
		{
			Uniform("time", value);
		}
	}

	public int FrameWidth
	{
		set
		{
			Uniform("FrameWidth", value);
		}
	}

	public float PerceptionEffectIntensity
	{
		set
		{
			Uniform("PerceptionEffectIntensity", value);
		}
	}
}
