using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class Shader : IShader
{
	private static string shaderVersionPattern = "\\#version (\\d+)";

	public int ShaderId;

	private string prefixCode = "";

	private string code = "";

	public EnumShaderType shaderType;

	internal string Filename = "";

	public EnumShaderType Type
	{
		get
		{
			return shaderType;
		}
		set
		{
			shaderType = value;
		}
	}

	public string PrefixCode
	{
		get
		{
			return prefixCode;
		}
		set
		{
			prefixCode = value;
		}
	}

	public string Code
	{
		get
		{
			return code;
		}
		set
		{
			code = value;
		}
	}

	public Shader()
	{
	}

	public Shader(EnumShaderType shaderType, string code, string filename)
	{
		this.shaderType = shaderType;
		this.code = code;
		Filename = filename;
	}

	public bool Compile()
	{
		return ScreenManager.Platform.CompileShader(this);
	}

	public void EnsureVersionSupported()
	{
		Match match = Regex.Match(code, shaderVersionPattern);
		if (match.Groups.Count > 1)
		{
			string versionUsed = match.Groups[1].Value;
			if (ScreenManager.Platform.UseSSBOs && UsesSSBOs())
			{
				versionUsed = "430";
			}
			EnsureVersionSupported(versionUsed, Filename);
		}
	}

	public Shader Clone()
	{
		return new Shader(shaderType, code, Filename)
		{
			prefixCode = prefixCode
		};
	}

	public static void EnsureVersionSupported(string versionUsed, string ownFilename)
	{
		string gLShaderVersionString = ScreenManager.Platform.GetGLShaderVersionString();
		string text = Regex.Match(gLShaderVersionString, "(\\d\\.\\d+)").Groups[1].Value.Replace(".", "");
		int.TryParse(text, out var result);
		int.TryParse(versionUsed, out var result2);
		if (result2 <= result)
		{
			return;
		}
		string text2 = $"Your graphics card supports only OpenGL version {text} ({gLShaderVersionString}), but OpenGL version {versionUsed} is required.\n";
		if (result == 330 && ClientSettings.GlContextVersion == "3.3" && RuntimeEnv.OS != OS.Mac)
		{
			text2 += "===>  In your clientsettings.json file try searching and setting this string setting:  \"glContextVersion\": \"4.3\",  then start the game again <===";
			if (ScreenManager.Platform.UseSSBOs)
			{
				text2 += "\n(You can also try setting bool setting \"allowSSBOs\" to false, but try out \"glContextVersion\" first)";
			}
		}
		else if (ScreenManager.Platform.UseSSBOs && versionUsed == "430")
		{
			text2 = ((result >= 430 || RuntimeEnv.OS == OS.Mac) ? (text2 + "***In your clientsettings.json file please set bool setting \"allowSSBOs\" to false, and try again.***\n") : (text2 + "***In your clientsettings.json file please either set bool setting \"allowSSBOs\" to false, or set \"glContextVersion\" to 4.3, and try again.***\n(AllowSSBOs true should only be used if your hardware supports OpenGL 4.3 or later. Ask for support if necessary!)\n"));
			text2 += "Please then check if you have installed the latest version of your graphics card driver. If so, your graphics card may be to old to play Vintage Story.(Note: In case of modded gameplay with modded shaders, the mod author may be able to lower the OpenGL version requirements)";
		}
		else
		{
			text2 += "*** First check clientsettings.json setting \"glContextVersion\", this may have modified the version reported by the hardware, try increasing it.***\nPlease then check if you have installed the latest version of your graphics card driver. If so, your graphics card may be to old to play Vintage Story.(Note: In case of modded gameplay with modded shaders, the mod author may be able to lower the OpenGL version requirements)";
		}
		throw new NotSupportedException(text2);
	}

	public bool UsesSSBOs()
	{
		if (Type != EnumShaderType.VertexShader)
		{
			return false;
		}
		if (Filename.StartsWith("chunk") || Filename.Equals("decals.vsh"))
		{
			return true;
		}
		return false;
	}
}
