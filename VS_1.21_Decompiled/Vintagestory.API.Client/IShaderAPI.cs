namespace Vintagestory.API.Client;

public interface IShaderAPI
{
	IShaderProgram NewShaderProgram();

	IShader NewShader(EnumShaderType shaderType);

	int RegisterFileShaderProgram(string name, IShaderProgram program);

	int RegisterMemoryShaderProgram(string name, IShaderProgram program);

	IShaderProgram GetProgram(int renderPass);

	IShaderProgram GetProgramByName(string name);

	bool ReloadShaders();

	bool IsGLSLVersionSupported(string minVersion);
}
