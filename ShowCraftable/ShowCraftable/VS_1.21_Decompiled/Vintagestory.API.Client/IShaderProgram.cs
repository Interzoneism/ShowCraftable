using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IShaderProgram : IDisposable
{
	int ProgramId { get; }

	string AssetDomain { get; set; }

	int PassId { get; }

	string PassName { get; }

	bool ClampTexturesToEdge { get; set; }

	IShader VertexShader { get; set; }

	IShader FragmentShader { get; set; }

	IShader GeometryShader { get; set; }

	bool Disposed { get; }

	bool LoadError { get; }

	OrderedDictionary<string, UBORef> UBOs { get; }

	void Use();

	void Stop();

	bool Compile();

	void Uniform(string uniformName, float value);

	void Uniform(string uniformName, int value);

	void Uniform(string uniformName, Vec2f value);

	void Uniform(string uniformName, Vec3f value);

	void Uniform(string uniformName, Vec4f value);

	void Uniforms4(string uniformName, int count, float[] values);

	void UniformMatrix(string uniformName, float[] matrix);

	void BindTexture2D(string samplerName, int textureId, int textureNumber);

	void BindTextureCube(string samplerName, int textureId, int textureNumber);

	void UniformMatrices(string uniformName, int count, float[] matrix);

	void UniformMatrices4x3(string uniformName, int count, float[] matrix);

	bool HasUniform(string uniformName);
}
