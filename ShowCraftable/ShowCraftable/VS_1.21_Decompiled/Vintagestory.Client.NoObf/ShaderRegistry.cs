using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ShaderRegistry
{
	private static string[] shaderNames;

	private static ShaderProgram[] shaderPrograms;

	private static Dictionary<string, string> includes;

	private static Dictionary<string, int> shaderIdsByName;

	private static int nextPassId;

	public static bool NormalView;

	private static void registerDefaultShaderPrograms()
	{
		RegisterShaderProgram(EnumShaderProgram.Autocamera, ShaderPrograms.Autocamera = new ShaderProgramAutocamera());
		RegisterShaderProgram(EnumShaderProgram.Bilateralblur, ShaderPrograms.Bilateralblur = new ShaderProgramBilateralblur());
		RegisterShaderProgram(EnumShaderProgram.Blit, ShaderPrograms.Blit = new ShaderProgramBlit());
		RegisterShaderProgram(EnumShaderProgram.Blockhighlights, ShaderPrograms.Blockhighlights = new ShaderProgramBlockhighlights());
		RegisterShaderProgram(EnumShaderProgram.Blur, ShaderPrograms.Blur = new ShaderProgramBlur());
		RegisterShaderProgram(EnumShaderProgram.Celestialobject, ShaderPrograms.Celestialobject = new ShaderProgramCelestialobject());
		RegisterShaderProgram(EnumShaderProgram.Chunkliquid, ShaderPrograms.Chunkliquid = new ShaderProgramChunkliquid());
		RegisterShaderProgram(EnumShaderProgram.Chunkliquiddepth, ShaderPrograms.Chunkliquiddepth = new ShaderProgramChunkliquiddepth());
		RegisterShaderProgram(EnumShaderProgram.Chunkopaque, ShaderPrograms.Chunkopaque = new ShaderProgramChunkopaque());
		RegisterShaderProgram(EnumShaderProgram.Chunktopsoil, ShaderPrograms.Chunktopsoil = new ShaderProgramChunktopsoil());
		RegisterShaderProgram(EnumShaderProgram.Chunktransparent, ShaderPrograms.Chunktransparent = new ShaderProgramChunktransparent());
		RegisterShaderProgram(EnumShaderProgram.Colorgrade, ShaderPrograms.Colorgrade = new ShaderProgramColorgrade());
		RegisterShaderProgram(EnumShaderProgram.Debugdepthbuffer, ShaderPrograms.Debugdepthbuffer = new ShaderProgramDebugdepthbuffer());
		RegisterShaderProgram(EnumShaderProgram.Decals, ShaderPrograms.Decals = new ShaderProgramDecals());
		RegisterShaderProgram(EnumShaderProgram.Entityanimated, ShaderPrograms.Entityanimated = new ShaderProgramEntityanimated());
		RegisterShaderProgram(EnumShaderProgram.Final, ShaderPrograms.Final = new ShaderProgramFinal());
		RegisterShaderProgram(EnumShaderProgram.Findbright, ShaderPrograms.Findbright = new ShaderProgramFindbright());
		RegisterShaderProgram(EnumShaderProgram.Godrays, ShaderPrograms.Godrays = new ShaderProgramGodrays());
		RegisterShaderProgram(EnumShaderProgram.Gui, ShaderPrograms.Gui = new ShaderProgramGui());
		RegisterShaderProgram(EnumShaderProgram.Guigear, ShaderPrograms.Guigear = new ShaderProgramGuigear());
		RegisterShaderProgram(EnumShaderProgram.Guitopsoil, ShaderPrograms.Guitopsoil = new ShaderProgramGuitopsoil());
		RegisterShaderProgram(EnumShaderProgram.Helditem, ShaderPrograms.Helditem = new ShaderProgramHelditem());
		RegisterShaderProgram(EnumShaderProgram.Luma, ShaderPrograms.Luma = new ShaderProgramLuma());
		RegisterShaderProgram(EnumShaderProgram.Nightsky, ShaderPrograms.Nightsky = new ShaderProgramNightsky());
		RegisterShaderProgram(EnumShaderProgram.Particlescube, ShaderPrograms.Particlescube = new ShaderProgramParticlescube());
		RegisterShaderProgram(EnumShaderProgram.Particlesquad, ShaderPrograms.Particlesquad = new ShaderProgramParticlesquad());
		RegisterShaderProgram(EnumShaderProgram.Particlesquad2d, ShaderPrograms.Particlesquad2d = new ShaderProgramParticlesquad2d());
		RegisterShaderProgram(EnumShaderProgram.Shadowmapentityanimated, ShaderPrograms.Shadowmapentityanimated = new ShaderProgramShadowmapentityanimated());
		RegisterShaderProgram(EnumShaderProgram.Chunkshadowmap, ShaderPrograms.Chunkshadowmap = new ShaderProgramShadowmapgeneric());
		RegisterShaderProgram(EnumShaderProgram.Sky, ShaderPrograms.Sky = new ShaderProgramSky());
		RegisterShaderProgram(EnumShaderProgram.Ssao, ShaderPrograms.Ssao = new ShaderProgramSsao());
		RegisterShaderProgram(EnumShaderProgram.Standard, ShaderPrograms.Standard = new ShaderProgramStandard());
		RegisterShaderProgram(EnumShaderProgram.Texture2texture, ShaderPrograms.Texture2texture = new ShaderProgramTexture2texture());
		RegisterShaderProgram(EnumShaderProgram.Transparentcompose, ShaderPrograms.Transparentcompose = new ShaderProgramTransparentcompose());
		RegisterShaderProgram(EnumShaderProgram.Wireframe, ShaderPrograms.Wireframe = new ShaderProgramWireframe());
		RegisterShaderProgram(EnumShaderProgram.Woittest, ShaderPrograms.Woittest = new ShaderProgramWoittest());
	}

	static ShaderRegistry()
	{
		shaderNames = new string[100];
		shaderPrograms = new ShaderProgram[100];
		includes = new Dictionary<string, string>();
		shaderIdsByName = new Dictionary<string, int>();
		nextPassId = 0;
		registerDefaultShaderPrograms();
		nextPassId++;
	}

	public static int RegisterShaderProgram(string name, ShaderProgram program)
	{
		int num = nextPassId;
		if (shaderIdsByName.ContainsKey(name))
		{
			num = shaderIdsByName[name];
		}
		else
		{
			nextPassId++;
		}
		program.PassId = num;
		program.PassName = name;
		shaderNames[num] = name;
		shaderPrograms[num] = program;
		shaderIdsByName[name] = num;
		LoadShaderProgram(program, ScreenManager.Platform.UseSSBOs);
		return program.PassId;
	}

	public static void RegisterShaderProgram(EnumShaderProgram defaultProgram, ShaderProgram program)
	{
		program.PassId = (int)defaultProgram;
		string text = defaultProgram.ToString().ToLowerInvariant();
		shaderNames[(int)defaultProgram] = text;
		shaderPrograms[(int)defaultProgram] = program;
		nextPassId = Math.Max((int)(defaultProgram + 1), nextPassId);
		int num = text.IndexOf('_');
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		program.PassName = text;
	}

	public static ShaderProgram getProgram(EnumShaderProgram renderPass)
	{
		return shaderPrograms[(int)renderPass];
	}

	public static ShaderProgram getProgram(int renderPass)
	{
		return shaderPrograms[renderPass];
	}

	public static ShaderProgram getProgramByName(string shadername)
	{
		if (shaderIdsByName.TryGetValue(shadername, out var value))
		{
			return shaderPrograms[value];
		}
		return null;
	}

	public static void Load()
	{
		loadRegisteredShaderPrograms();
	}

	public static bool ReloadShaders()
	{
		ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaders);
		ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaderincludes);
		for (int i = 0; i < shaderPrograms.Length; i++)
		{
			if (shaderPrograms[i] != null)
			{
				shaderPrograms[i].Dispose();
				shaderPrograms[i] = null;
			}
		}
		registerDefaultShaderPrograms();
		bool result = loadRegisteredShaderPrograms();
		if (ScreenManager.Platform.UseSSBOs)
		{
			RegisterShaderProgram(EnumShaderProgram.Chunkshadowmap_NoSSBOs, new ShaderProgramShadowmapgeneric());
			ShaderProgram shaderProgram = shaderPrograms[42];
			if (shaderProgram != null)
			{
				LoadShaderProgram(shaderProgram, useSSBOs: false);
				shaderProgram.Compile();
			}
		}
		return result;
	}

	private static bool loadRegisteredShaderPrograms()
	{
		ScreenManager.Platform.Logger.Notification("Loading shaders...");
		bool flag = true;
		_ = ScreenManager.Platform.AssetManager;
		List<IAsset> many = ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaderincludes);
		many.AddRange(ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaders));
		foreach (IAsset item in many)
		{
			includes[item.Name] = item.ToText();
		}
		for (int i = 0; i < nextPassId; i++)
		{
			ShaderProgram shaderProgram = shaderPrograms[i];
			if (shaderProgram != null)
			{
				LoadShaderProgram(shaderProgram, ScreenManager.Platform.UseSSBOs);
				flag = shaderProgram.Compile() && flag;
			}
		}
		ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTex", isLinear: false);
		ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTexLinear", isLinear: true);
		ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTex", isLinear: false);
		ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTexLinear", isLinear: true);
		return flag;
	}

	private static void LoadShaderProgram(ShaderProgram program, bool useSSBOs)
	{
		if (program.LoadFromFile)
		{
			LoadShader(program, EnumShaderType.VertexShader);
			LoadShader(program, EnumShaderType.FragmentShader);
			LoadShader(program, EnumShaderType.GeometryShader);
		}
		if (program.VertexShader == null)
		{
			ScreenManager.Platform.Logger.Error("Vertex shader missing for shader {0}. Will probably crash.", program.PassName);
		}
		if (program.FragmentShader == null)
		{
			ScreenManager.Platform.Logger.Error("Fragment shader missing for shader {0}. Will probably crash.", program.PassName);
		}
		registerDefaultShaderCodePrefixes(program, useSSBOs);
	}

	private static void LoadShader(ShaderProgram program, EnumShaderType shaderType)
	{
		AssetManager assetManager = ScreenManager.Platform.AssetManager;
		string text = ".unknown";
		switch (shaderType)
		{
		case EnumShaderType.VertexShader:
			text = ".vsh";
			break;
		case EnumShaderType.FragmentShader:
			text = ".fsh";
			break;
		case EnumShaderType.GeometryShader:
			text = ".gsh";
			break;
		}
		string passName = program.PassName;
		AssetLocation assetLocation = new AssetLocation(program.AssetDomain, "shaders/" + passName + text);
		IAsset asset = assetManager.TryGet_BaseAssets(assetLocation);
		if (asset == null)
		{
			if (shaderType != EnumShaderType.GeometryShader)
			{
				ScreenManager.Platform.Logger.Error("Shader file {0} not found. Stack trace:\n{1}", assetLocation, Environment.StackTrace);
				program.LoadError = true;
			}
			return;
		}
		string code = HandleIncludes(program, asset.ToText());
		switch (shaderType)
		{
		case EnumShaderType.VertexShader:
			if (program.VertexShader == null)
			{
				program.VertexShader = new Shader(shaderType, code, passName + text);
				break;
			}
			program.VertexShader.Code = code;
			program.VertexShader.Type = shaderType;
			program.VertexShader.Filename = passName + text;
			break;
		case EnumShaderType.FragmentShader:
			if (program.FragmentShader == null)
			{
				program.FragmentShader = new Shader(shaderType, code, passName + text);
				break;
			}
			program.FragmentShader.Code = code;
			program.FragmentShader.Type = shaderType;
			program.FragmentShader.Filename = passName + text;
			break;
		case EnumShaderType.GeometryShader:
			if (program.GeometryShader == null)
			{
				program.GeometryShader = new Shader(shaderType, code, passName + text);
				break;
			}
			program.GeometryShader.Code = code;
			program.GeometryShader.Type = shaderType;
			program.GeometryShader.Filename = passName + text;
			break;
		}
	}

	private static void registerDefaultShaderCodePrefixes(ShaderProgram program, bool useSSBOs)
	{
		Shader fragmentShader = program.FragmentShader;
		fragmentShader.PrefixCode = fragmentShader.PrefixCode + "#define FXAA " + (ClientSettings.FXAA ? 1 : 0) + "\r\n";
		Shader fragmentShader2 = program.FragmentShader;
		fragmentShader2.PrefixCode = fragmentShader2.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality + "\r\n";
		Shader fragmentShader3 = program.FragmentShader;
		fragmentShader3.PrefixCode = fragmentShader3.PrefixCode + "#define NORMALVIEW " + (NormalView ? 1 : 0) + "\r\n";
		Shader fragmentShader4 = program.FragmentShader;
		fragmentShader4.PrefixCode = fragmentShader4.PrefixCode + "#define BLOOM " + (ClientSettings.Bloom ? 1 : 0) + "\r\n";
		Shader fragmentShader5 = program.FragmentShader;
		fragmentShader5.PrefixCode = fragmentShader5.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality + "\r\n";
		Shader fragmentShader6 = program.FragmentShader;
		fragmentShader6.PrefixCode = fragmentShader6.PrefixCode + "#define FOAMEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader fragmentShader7 = program.FragmentShader;
		fragmentShader7.PrefixCode = fragmentShader7.PrefixCode + "#define SHINYEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader fragmentShader8 = program.FragmentShader;
		fragmentShader8.PrefixCode = fragmentShader8.PrefixCode + "#define SHADOWQUALITY " + ClientSettings.ShadowMapQuality + "\r\n#define DYNLIGHTS " + ClientSettings.MaxDynamicLights + "\r\n";
		Shader vertexShader = program.VertexShader;
		vertexShader.PrefixCode = vertexShader.PrefixCode + "#define USESSBO " + (useSSBOs ? 1 : 0) + "\r\n";
		Shader vertexShader2 = program.VertexShader;
		vertexShader2.PrefixCode = vertexShader2.PrefixCode + "#define WAVINGSTUFF " + (ClientSettings.WavingFoliage ? 1 : 0) + "\r\n";
		Shader vertexShader3 = program.VertexShader;
		vertexShader3.PrefixCode = vertexShader3.PrefixCode + "#define FOAMEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader vertexShader4 = program.VertexShader;
		vertexShader4.PrefixCode = vertexShader4.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality + "\r\n";
		Shader vertexShader5 = program.VertexShader;
		vertexShader5.PrefixCode = vertexShader5.PrefixCode + "#define NORMALVIEW " + (NormalView ? 1 : 0) + "\r\n";
		Shader vertexShader6 = program.VertexShader;
		vertexShader6.PrefixCode = vertexShader6.PrefixCode + "#define SHINYEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader vertexShader7 = program.VertexShader;
		vertexShader7.PrefixCode = vertexShader7.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality + "\r\n";
		Shader vertexShader8 = program.VertexShader;
		vertexShader8.PrefixCode = vertexShader8.PrefixCode + "#define MINBRIGHT " + ClientSettings.Minbrightness + "\r\n";
		fragmentShader8 = program.VertexShader;
		fragmentShader8.PrefixCode = fragmentShader8.PrefixCode + "#define SHADOWQUALITY " + ClientSettings.ShadowMapQuality + "\r\n#define DYNLIGHTS " + ClientSettings.MaxDynamicLights + "\r\n";
		Shader vertexShader9 = program.VertexShader;
		vertexShader9.PrefixCode = vertexShader9.PrefixCode + "#define MAXANIMATEDELEMENTS " + GlobalConstants.MaxAnimatedElements + "\r\n";
	}

	private static string HandleIncludes(ShaderProgram program, string shaderCode, HashSet<string> filenames = null)
	{
		if (filenames == null)
		{
			filenames = new HashSet<string>();
		}
		return Regex.Replace(shaderCode, "^#include\\s+(.*)", delegate(Match m)
		{
			string text = m.Groups[1].Value.Trim().ToLowerInvariant();
			if (filenames.Contains(text))
			{
				return "";
			}
			filenames.Add(text);
			return InsertIncludedFile(program, text, filenames);
		}, RegexOptions.Multiline);
	}

	private static string InsertIncludedFile(ShaderProgram program, string filename, HashSet<string> filenames = null)
	{
		if (!includes.ContainsKey(filename))
		{
			ScreenManager.Platform.Logger.Warning("Error when loading shaders: Include file {0} not found. Ignoring.", filename);
			return "";
		}
		program.includes.Add(filename);
		string shaderCode = includes[filename];
		return HandleIncludes(program, shaderCode, filenames);
	}

	public static bool IsGLSLVersionSupported(string minVersion)
	{
		int.TryParse(Regex.Match(ScreenManager.Platform.GetGLShaderVersionString(), "(\\d\\.\\d+)").Groups[1].Value.Replace(".", ""), out var result);
		int.TryParse(minVersion, out var result2);
		return result2 <= result;
	}
}
