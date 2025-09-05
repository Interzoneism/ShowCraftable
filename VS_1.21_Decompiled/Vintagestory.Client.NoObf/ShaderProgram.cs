using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ShaderProgram : ShaderProgramBase, IShaderProgram, IDisposable
{
	public Dictionary<int, string> attributes = new Dictionary<int, string>();

	public bool LoadFromFile = true;

	public override bool Compile()
	{
		bool flag = true;
		HashSet<string> hashSet = new HashSet<string>();
		VertexShader?.EnsureVersionSupported();
		GeometryShader?.EnsureVersionSupported();
		FragmentShader?.EnsureVersionSupported();
		if (VertexShader != null)
		{
			flag = flag && VertexShader.Compile();
			collectUniformNames(VertexShader.Code, hashSet);
		}
		if (FragmentShader != null)
		{
			flag = flag && FragmentShader.Compile();
			collectUniformNames(FragmentShader.Code, hashSet);
		}
		if (GeometryShader != null)
		{
			flag = flag && GeometryShader.Compile();
			collectUniformNames(GeometryShader.Code, hashSet);
		}
		flag = flag && ScreenManager.Platform.CreateShaderProgram(this);
		string text = "";
		foreach (string item in hashSet)
		{
			uniformLocations[item] = ScreenManager.Platform.GetUniformLocation(this, item);
			if (uniformLocations[item] == -1)
			{
				if (text.Length > 0)
				{
					text += ", ";
				}
				text += item;
			}
		}
		if (text.Length > 0 && ScreenManager.Platform.GlDebugMode)
		{
			ScreenManager.Platform.Logger.Notification("Shader {0}: Uniform locations for variables {1} not found (or not used).", PassName, text);
		}
		return flag;
	}

	private void collectUniformNames(string code, HashSet<string> list)
	{
		foreach (Match item in Regex.Matches(code, "(\\s|\\r\\n)uniform\\s*(?<type>float|int|ivec2|ivec3|ivec4|vec2|vec3|vec4|sampler2DShadow|sampler2D|samplerCube|mat3|mat4x3|mat4)\\s*(\\[[\\d\\w]+\\])?\\s*(?<var>[\\d\\w]+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
		{
			string value = item.Groups["var"].Value;
			list.Add(value);
			if (item.Groups["type"].ToString().Contains("sampler"))
			{
				textureLocations[value] = textureLocations.Count;
			}
		}
	}
}
