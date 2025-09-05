using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramLines : ShaderProgram
{
	public Vec4f Color
	{
		set
		{
			Uniform("color", value);
		}
	}

	public float GlowLevel
	{
		set
		{
			Uniform("glowLevel", value);
		}
	}

	public float LineWidth
	{
		set
		{
			Uniform("lineWidth", value);
		}
	}

	public float[] Projection
	{
		set
		{
			UniformMatrix("projection", value);
		}
	}

	public float[] View
	{
		set
		{
			UniformMatrix("view", value);
		}
	}

	public Vec3f Origin
	{
		set
		{
			Uniform("origin", value);
		}
	}
}
