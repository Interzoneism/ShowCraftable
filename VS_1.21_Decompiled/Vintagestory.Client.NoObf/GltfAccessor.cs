using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;

namespace Vintagestory.Client.NoObf;

public class GltfAccessor
{
	[JsonProperty("bufferView")]
	public long BufferView { get; set; }

	[JsonProperty("componentType")]
	public VertexAttribPointerType ComponentType { get; set; }

	[JsonProperty("count")]
	public int Count { get; set; }

	[JsonProperty(/*Could not decode attribute arguments.*/)]
	public double[] Max { get; set; }

	[JsonProperty(/*Could not decode attribute arguments.*/)]
	public double[] Min { get; set; }

	[JsonProperty("type")]
	public EnumGltfAccessorType Type { get; set; }
}
