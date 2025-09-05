using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramAurora : ShaderProgram
{
	public int Tex2D
	{
		set
		{
			BindTexture2D("tex", value, 0);
		}
	}

	public float ExtraGodray
	{
		set
		{
			Uniform("extraGodray", value);
		}
	}

	public float AlphaTest
	{
		set
		{
			Uniform("alphaTest", value);
		}
	}

	public float AuroraCounter
	{
		set
		{
			Uniform("auroraCounter", value);
		}
	}

	public float FlatFogDensity
	{
		set
		{
			Uniform("flatFogDensity", value);
		}
	}

	public float FlatFogStart
	{
		set
		{
			Uniform("flatFogStart", value);
		}
	}

	public float ViewDistance
	{
		set
		{
			Uniform("viewDistance", value);
		}
	}

	public float ViewDistanceLod0
	{
		set
		{
			Uniform("viewDistanceLod0", value);
		}
	}

	public float ZNear
	{
		set
		{
			Uniform("zNear", value);
		}
	}

	public float ZFar
	{
		set
		{
			Uniform("zFar", value);
		}
	}

	public Vec3f LightPosition
	{
		set
		{
			Uniform("lightPosition", value);
		}
	}

	public float ShadowIntensity
	{
		set
		{
			Uniform("shadowIntensity", value);
		}
	}

	public int ShadowMapFar2D
	{
		set
		{
			BindTexture2D("shadowMapFar", value, 1);
		}
	}

	public float ShadowMapWidthInv
	{
		set
		{
			Uniform("shadowMapWidthInv", value);
		}
	}

	public float ShadowMapHeightInv
	{
		set
		{
			Uniform("shadowMapHeightInv", value);
		}
	}

	public int ShadowMapNear2D
	{
		set
		{
			BindTexture2D("shadowMapNear", value, 2);
		}
	}

	public float WindWaveCounter
	{
		set
		{
			Uniform("windWaveCounter", value);
		}
	}

	public float GlitchStrength
	{
		set
		{
			Uniform("glitchStrength", value);
		}
	}

	public float[] FogSpheres
	{
		set
		{
			Uniform("fogSpheres", value.Length, value);
		}
	}

	public int FogSphereQuantity
	{
		set
		{
			Uniform("fogSphereQuantity", value);
		}
	}

	public Vec4f Color
	{
		set
		{
			Uniform("color", value);
		}
	}

	public Vec4f RgbaTint
	{
		set
		{
			Uniform("rgbaTint", value);
		}
	}

	public Vec3f RgbaAmbientIn
	{
		set
		{
			Uniform("rgbaAmbientIn", value);
		}
	}

	public Vec4f RgbaLightIn
	{
		set
		{
			Uniform("rgbaLightIn", value);
		}
	}

	public Vec4f RgbaBlockIn
	{
		set
		{
			Uniform("rgbaBlockIn", value);
		}
	}

	public Vec4f RgbaFogIn
	{
		set
		{
			Uniform("rgbaFogIn", value);
		}
	}

	public float FogMinIn
	{
		set
		{
			Uniform("fogMinIn", value);
		}
	}

	public float FogDensityIn
	{
		set
		{
			Uniform("fogDensityIn", value);
		}
	}

	public float[] ProjectionMatrix
	{
		set
		{
			UniformMatrix("projectionMatrix", value);
		}
	}

	public float[] ModelViewMatrix
	{
		set
		{
			UniformMatrix("modelViewMatrix", value);
		}
	}

	public float GlitchStrengthFL
	{
		set
		{
			Uniform("glitchStrengthFL", value);
		}
	}

	public float NightVisionStrength
	{
		set
		{
			Uniform("nightVisionStrength", value);
		}
	}

	public int PointLightQuantity
	{
		set
		{
			Uniform("pointLightQuantity", value);
		}
	}

	public void PointLightsArray(int count, float[] values)
	{
		Uniforms3("pointLights", count, values);
	}

	public void PointLightColorsArray(int count, float[] values)
	{
		Uniforms3("pointLightColors", count, values);
	}
}
