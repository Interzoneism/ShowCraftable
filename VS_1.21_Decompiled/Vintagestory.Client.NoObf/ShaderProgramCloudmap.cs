using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramCloudmap : ShaderProgram
{
	public float DayLight
	{
		set
		{
			Uniform("dayLight", value);
		}
	}

	public float GlobalCloudBrightness
	{
		set
		{
			Uniform("globalCloudBrightness", value);
		}
	}

	public float Time
	{
		set
		{
			Uniform("time", value);
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

	public Vec3f SunPosition
	{
		set
		{
			Uniform("sunPosition", value);
		}
	}

	public float NightVisionStrength
	{
		set
		{
			Uniform("nightVisionStrength", value);
		}
	}

	public float Alpha
	{
		set
		{
			Uniform("alpha", value);
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
			BindTexture2D("shadowMapFar", value, 0);
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
			BindTexture2D("shadowMapNear", value, 1);
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

	public float PlayerToSealevelOffset
	{
		set
		{
			Uniform("playerToSealevelOffset", value);
		}
	}

	public int DitherSeed
	{
		set
		{
			Uniform("ditherSeed", value);
		}
	}

	public int HorizontalResolution
	{
		set
		{
			Uniform("horizontalResolution", value);
		}
	}

	public float FogWaveCounter
	{
		set
		{
			Uniform("fogWaveCounter", value);
		}
	}

	public int Glow2D
	{
		set
		{
			BindTexture2D("glow", value, 2);
		}
	}

	public int Sky2D
	{
		set
		{
			BindTexture2D("sky", value, 3);
		}
	}

	public float SunsetMod
	{
		set
		{
			Uniform("sunsetMod", value);
		}
	}

	public float Width
	{
		set
		{
			Uniform("width", value);
		}
	}

	public int MapData12D
	{
		set
		{
			BindTexture2D("mapData1", value, 4);
		}
	}

	public int MapData22D
	{
		set
		{
			BindTexture2D("mapData2", value, 5);
		}
	}

	public Vec3f MapOffset
	{
		set
		{
			Uniform("mapOffset", value);
		}
	}

	public Vec2f MapOffsetCentre
	{
		set
		{
			Uniform("mapOffsetCentre", value);
		}
	}

	public float[] ViewMatrix
	{
		set
		{
			UniformMatrix("viewMatrix", value);
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
