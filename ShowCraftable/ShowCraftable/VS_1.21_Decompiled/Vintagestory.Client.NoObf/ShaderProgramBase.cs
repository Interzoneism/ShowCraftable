using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public abstract class ShaderProgramBase : IShaderProgram, IDisposable
{
	public static int shadowmapQuality;

	public static ShaderProgramBase CurrentShaderProgram;

	public int PassId;

	public int ProgramId;

	public string PassName;

	public Shader VertexShader;

	public Shader GeometryShader;

	public Shader FragmentShader;

	public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

	public Dictionary<string, int> textureLocations = new Dictionary<string, int>();

	public OrderedDictionary<string, UBORef> ubos = new OrderedDictionary<string, UBORef>();

	public bool clampTToEdge;

	public HashSet<string> includes = new HashSet<string>();

	public Dictionary<string, int> customSamplers = new Dictionary<string, int>();

	private bool disposed;

	public bool Disposed => disposed;

	int IShaderProgram.PassId => PassId;

	string IShaderProgram.PassName => PassName;

	public bool ClampTexturesToEdge
	{
		get
		{
			return clampTToEdge;
		}
		set
		{
			clampTToEdge = value;
		}
	}

	IShader IShaderProgram.VertexShader
	{
		get
		{
			return VertexShader;
		}
		set
		{
			VertexShader = (Shader)value;
		}
	}

	IShader IShaderProgram.FragmentShader
	{
		get
		{
			return FragmentShader;
		}
		set
		{
			FragmentShader = (Shader)value;
		}
	}

	IShader IShaderProgram.GeometryShader
	{
		get
		{
			return GeometryShader;
		}
		set
		{
			GeometryShader = (Shader)value;
		}
	}

	public bool LoadError { get; set; }

	public OrderedDictionary<string, UBORef> UBOs => ubos;

	public string AssetDomain { get; set; }

	int IShaderProgram.ProgramId => ProgramId;

	public void SetCustomSampler(string uniformName, bool isLinear)
	{
		int value = ScreenManager.Platform.GenSampler(isLinear);
		customSamplers.Add(uniformName, value);
	}

	public void Uniform(string uniformName, float value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], value);
	}

	public void Uniform(string uniformName, int count, float[] value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], count, value);
	}

	public void Uniform(string uniformName, int value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], value);
	}

	public void Uniform(string uniformName, Vec2f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform2(uniformLocations[uniformName], value.X, value.Y);
	}

	public void Uniform(string uniformName, Vec3f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
	}

	public void Uniform(string uniformName, Vec3i value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
	}

	public void Uniforms2(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform2(uniformLocations[uniformName], count, values);
	}

	public void Uniforms3(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], count, values);
	}

	public void Uniform(string uniformName, Vec4f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform4(uniformLocations[uniformName], value.X, value.Y, value.Z, value.W);
	}

	public void Uniforms4(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform4(uniformLocations[uniformName], count, values);
	}

	public void UniformMatrix(string uniformName, float[] matrix)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.UniformMatrix4(uniformLocations[uniformName], 1, false, matrix);
	}

	public void UniformMatrix(string uniformName, ref Matrix4 matrix)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.UniformMatrix4(uniformLocations[uniformName], false, ref matrix);
	}

	public bool HasUniform(string uniformName)
	{
		return uniformLocations.ContainsKey(uniformName);
	}

	public void BindTexture2D(string samplerName, int textureId, int textureNumber)
	{
		GL.Uniform1(uniformLocations[samplerName], textureNumber);
		GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
		GL.BindTexture((TextureTarget)3553, textureId);
		if (customSamplers.TryGetValue(samplerName, out var value))
		{
			GL.BindSampler(textureNumber, value);
		}
		if (clampTToEdge)
		{
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, Convert.ToInt32((object)(TextureWrapMode)33071));
		}
	}

	public void BindTexture2D(string samplerName, int textureId)
	{
		BindTexture2D(samplerName, textureId, textureLocations[samplerName]);
	}

	public void BindTextureCube(string samplerName, int textureId, int textureNumber)
	{
		GL.Uniform1(uniformLocations[samplerName], textureNumber);
		GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
		GL.BindTexture((TextureTarget)34067, textureId);
		if (clampTToEdge)
		{
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, Convert.ToInt32((object)(TextureWrapMode)33071));
		}
	}

	public void UniformMatrices4x3(string uniformName, int count, float[] matrix)
	{
		GL.UniformMatrix4x3(uniformLocations[uniformName], count, false, matrix);
	}

	public void UniformMatrices(string uniformName, int count, float[] matrix)
	{
		GL.UniformMatrix4(uniformLocations[uniformName], count, false, matrix);
	}

	public void Use()
	{
		if (CurrentShaderProgram != null && CurrentShaderProgram != this)
		{
			throw new InvalidOperationException("Already a different shader (" + CurrentShaderProgram.PassName + ") in use!");
		}
		if (disposed)
		{
			throw new InvalidOperationException("Can't use a disposed shader!");
		}
		GL.UseProgram(ProgramId);
		CurrentShaderProgram = this;
		DefaultShaderUniforms shaderUniforms = ScreenManager.Platform.ShaderUniforms;
		if (includes.Contains("fogandlight.fsh"))
		{
			Uniform("zNear", shaderUniforms.ZNear);
			Uniform("zFar", shaderUniforms.ZFar);
			Uniform("lightPosition", shaderUniforms.LightPosition3D);
			Uniform("shadowIntensity", shaderUniforms.DropShadowIntensity);
			Uniform("glitchStrength", shaderUniforms.GlitchStrength);
			if (shadowmapQuality > 0)
			{
				FrameBufferRef frameBufferRef = ScreenManager.Platform.FrameBuffers[11];
				FrameBufferRef frameBufferRef2 = ScreenManager.Platform.FrameBuffers[12];
				BindTexture2D("shadowMapFar", frameBufferRef.DepthTextureId);
				BindTexture2D("shadowMapNear", frameBufferRef2.DepthTextureId);
				Uniform("shadowMapWidthInv", 1f / (float)frameBufferRef.Width);
				Uniform("shadowMapHeightInv", 1f / (float)frameBufferRef.Height);
				Uniform("viewDistance", (float)ClientSettings.ViewDistance);
				Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
			}
		}
		if (includes.Contains("fogandlight.vsh"))
		{
			int fogSphereQuantity = shaderUniforms.FogSphereQuantity;
			Uniform("fogSphereQuantity", fogSphereQuantity);
			Uniform("fogSpheres", fogSphereQuantity * 8, shaderUniforms.FogSpheres);
			int pointLightsCount = shaderUniforms.PointLightsCount;
			Uniform("pointLightQuantity", pointLightsCount);
			Uniforms3("pointLights", pointLightsCount, shaderUniforms.PointLights3);
			Uniforms3("pointLightColors", pointLightsCount, shaderUniforms.PointLightColors3);
			Uniform("flatFogDensity", shaderUniforms.FlagFogDensity);
			Uniform("flatFogStart", shaderUniforms.FlatFogStartYPos - shaderUniforms.PlayerPos.Y);
			Uniform("glitchStrengthFL", shaderUniforms.GlitchStrength);
			Uniform("viewDistance", (float)ClientSettings.ViewDistance);
			Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
			Uniform("nightVisionStrength", shaderUniforms.NightVisionStrength);
		}
		if (includes.Contains("shadowcoords.vsh"))
		{
			Uniform("shadowRangeNear", shaderUniforms.ShadowRangeNear);
			Uniform("shadowRangeFar", shaderUniforms.ShadowRangeFar);
			UniformMatrix("toShadowMapSpaceMatrixNear", shaderUniforms.ToShadowMapSpaceMatrixNear);
			UniformMatrix("toShadowMapSpaceMatrixFar", shaderUniforms.ToShadowMapSpaceMatrixFar);
		}
		if (includes.Contains("vertexwarp.vsh"))
		{
			Uniform("timeCounter", shaderUniforms.TimeCounter);
			Uniform("windWaveCounter", shaderUniforms.WindWaveCounter);
			Uniform("windWaveCounterHighFreq", shaderUniforms.WindWaveCounterHighFreq);
			Uniform("windSpeed", shaderUniforms.WindSpeed);
			Uniform("waterWaveCounter", shaderUniforms.WaterWaveCounter);
			Uniform("playerpos", shaderUniforms.PlayerPos);
			Uniform("globalWarpIntensity", shaderUniforms.GlobalWorldWarp);
			Uniform("glitchWaviness", shaderUniforms.GlitchWaviness);
			Uniform("windWaveIntensity", shaderUniforms.WindWaveIntensity);
			Uniform("waterWaveIntensity", shaderUniforms.WaterWaveIntensity);
			Uniform("perceptionEffectId", shaderUniforms.PerceptionEffectId);
			Uniform("perceptionEffectIntensity", shaderUniforms.PerceptionEffectIntensity);
		}
		if (includes.Contains("skycolor.fsh"))
		{
			Uniform("fogWaveCounter", shaderUniforms.FogWaveCounter);
			BindTexture2D("sky", shaderUniforms.SkyTextureId);
			BindTexture2D("glow", shaderUniforms.GlowTextureId);
			Uniform("sunsetMod", shaderUniforms.SunsetMod);
			Uniform("ditherSeed", shaderUniforms.DitherSeed);
			Uniform("horizontalResolution", shaderUniforms.FrameWidth);
			Uniform("playerToSealevelOffset", shaderUniforms.PlayerToSealevelOffset);
		}
		if (includes.Contains("colormap.vsh"))
		{
			Uniforms4("colorMapRects", 40, shaderUniforms.ColorMapRects4);
			Uniform("seasonRel", shaderUniforms.SeasonRel);
			Uniform("seaLevel", shaderUniforms.SeaLevel);
			Uniform("atlasHeight", shaderUniforms.BlockAtlasHeight);
			Uniform("seasonTemperature", shaderUniforms.SeasonTemperature);
		}
		if (includes.Contains("underwatereffects.fsh"))
		{
			FrameBufferRef frameBufferRef3 = ScreenManager.Platform.FrameBuffers[5];
			BindTexture2D("liquidDepth", frameBufferRef3.DepthTextureId);
			Uniform("cameraUnderwater", shaderUniforms.CameraUnderwater);
			Uniform("waterMurkColor", shaderUniforms.WaterMurkColor);
			FrameBufferRef frameBufferRef4 = ScreenManager.Platform.FrameBuffers[0];
			Uniform("frameSize", new Vec2f(frameBufferRef4.Width, frameBufferRef4.Height));
		}
		if (this == ShaderPrograms.Gui)
		{
			ShaderPrograms.Gui.LightPosition = new Vec3f(1f, -1f, 0f).Normalize();
		}
		foreach (KeyValuePair<string, UBORef> ubo in ubos)
		{
			ubo.Value.Bind();
		}
	}

	public void Stop()
	{
		GL.UseProgram(0);
		for (int i = 0; i < customSamplers.Count; i++)
		{
			GL.BindSampler(i, 0);
		}
		foreach (KeyValuePair<string, UBORef> ubo in ubos)
		{
			ubo.Value.Unbind();
		}
		CurrentShaderProgram = null;
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		if (VertexShader != null)
		{
			GL.DetachShader(ProgramId, VertexShader.ShaderId);
			GL.DeleteShader(VertexShader.ShaderId);
		}
		if (FragmentShader != null)
		{
			GL.DetachShader(ProgramId, FragmentShader.ShaderId);
			GL.DeleteShader(FragmentShader.ShaderId);
		}
		if (GeometryShader != null)
		{
			GL.DetachShader(ProgramId, GeometryShader.ShaderId);
			GL.DeleteShader(GeometryShader.ShaderId);
		}
		foreach (KeyValuePair<string, int> customSampler in customSamplers)
		{
			GL.DeleteSampler(customSampler.Value);
		}
		GL.DeleteProgram(ProgramId);
	}

	public abstract bool Compile();
}
