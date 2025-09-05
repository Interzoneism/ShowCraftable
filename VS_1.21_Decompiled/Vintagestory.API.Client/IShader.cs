namespace Vintagestory.API.Client;

public interface IShader
{
	EnumShaderType Type { get; }

	string PrefixCode { get; set; }

	string Code { get; set; }
}
