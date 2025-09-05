using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GltfTesselator
{
	private List<Vec3f> temp_vertices = new List<Vec3f>();

	private List<float> temp_uvs = new List<float>();

	private List<int> temp_normals = new List<int>();

	private List<int> temp_indices = new List<int>();

	private List<Vec3f> temp_material = new List<Vec3f>();

	private List<Vec4us> temp_vertexcolor = new List<Vec4us>();

	private List<int> temp_flags = new List<int>();

	private List<MeshData> meshPieces = new List<MeshData>();

	private int[] capacities;

	private VertexFlags tempFlag = new VertexFlags();

	public void Load(GltfType asset, out MeshData mesh, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
	{
		meshPieces.Clear();
		capacities = new int[2];
		ParseGltf(asset, pos, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, renderPass, out bakedTextures);
		mesh = new MeshData(capacities[0] + 32, capacities[1] + 32).WithXyzFaces().WithRenderpasses().WithColorMaps();
		mesh.IndicesPerFace = 3;
		mesh.VerticesPerFace = 3;
		mesh.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[meshPieces.Count * 5],
			InterleaveSizes = new int[2] { 3, 2 },
			InterleaveStride = 20,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = 5
		};
		mesh.CustomInts = new CustomMeshDataPartInt
		{
			Values = new int[meshPieces.Count],
			InterleaveSizes = new int[meshPieces.Count].Fill(1),
			InterleaveStride = 4,
			InterleaveOffsets = new int[meshPieces.Count],
			Count = 0
		};
		for (int i = 0; i < mesh.CustomInts.Values.Length; i++)
		{
			mesh.CustomInts.InterleaveOffsets[i] = 4 * i;
		}
		for (int j = 0; j < meshPieces.Count; j++)
		{
			MeshData meshData = meshPieces[j];
			mesh.CustomFloats.Values[j * 5] = meshData.CustomFloats.Values[0];
			mesh.CustomFloats.Values[j * 5 + 1] = meshData.CustomFloats.Values[1];
			mesh.CustomFloats.Values[j * 5 + 2] = meshData.CustomFloats.Values[2];
			mesh.CustomFloats.Values[j * 5 + 3] = meshData.CustomFloats.Values[3];
			mesh.CustomFloats.Values[j * 5 + 4] = meshData.CustomFloats.Values[4];
			mesh.AddMeshData(meshData);
		}
	}

	public void ParseGltf(GltfType gltf, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
	{
		GltfBuffer[] buffers = gltf.Buffers;
		GltfBufferView[] bufferViews = gltf.BufferViews;
		int? num = gltf.Materials?.Length;
		bakedTextures = (num.HasValue ? new byte[num.Value][][] : null);
		long num2 = 0L;
		long[] nodes = gltf.Scenes[gltf.Scene].Nodes;
		foreach (long num3 in nodes)
		{
			GltfNode node = gltf.Nodes[num3];
			GltfPrimitive[] primitives = gltf.Meshes[gltf.Nodes[num3].Mesh].Primitives;
			foreach (GltfPrimitive gltfPrimitive in primitives)
			{
				Dictionary<string, long> dictionary = new Dictionary<string, long>();
				Dictionary<string, byte[]> dictionary2 = new Dictionary<string, byte[]>();
				float[] array = new float[3] { 1f, 1f, 1f };
				float[] array2 = new float[2] { 0f, 1f };
				long? position = gltfPrimitive.Attributes.Position;
				long? texcoord = gltfPrimitive.Attributes.Texcoord0;
				long? normal = gltfPrimitive.Attributes.Normal;
				long? vertexColor = gltfPrimitive.Attributes.VertexColor;
				long? glowLevel = gltfPrimitive.Attributes.GlowLevel;
				long? reflective = gltfPrimitive.Attributes.Reflective;
				long? bMWindLeaves = gltfPrimitive.Attributes.BMWindLeaves;
				long? bMWindLeavesWeakBend = gltfPrimitive.Attributes.BMWindLeavesWeakBend;
				long? bMWindNormal = gltfPrimitive.Attributes.BMWindNormal;
				long? bMWindWater = gltfPrimitive.Attributes.BMWindWater;
				long? bMWindWeakBend = gltfPrimitive.Attributes.BMWindWeakBend;
				long? bMWindWeakWind = gltfPrimitive.Attributes.BMWindWeakWind;
				long? indices = gltfPrimitive.Indices;
				long? material = gltfPrimitive.Material;
				if (position.HasValue)
				{
					dictionary.Add("vtx", position.Value);
				}
				if (texcoord.HasValue)
				{
					dictionary.Add("uvs", texcoord.Value);
				}
				if (vertexColor.HasValue)
				{
					dictionary.Add("vtc", vertexColor.Value);
				}
				if (glowLevel.HasValue)
				{
					dictionary.Add("vtg", glowLevel.Value);
				}
				if (reflective.HasValue)
				{
					dictionary.Add("vtr", reflective.Value);
				}
				if (bMWindLeaves.HasValue)
				{
					dictionary.Add("wa", bMWindLeaves.Value);
				}
				if (bMWindLeavesWeakBend.HasValue)
				{
					dictionary.Add("wb", bMWindLeavesWeakBend.Value);
				}
				if (bMWindNormal.HasValue)
				{
					dictionary.Add("wc", bMWindNormal.Value);
				}
				if (bMWindWater.HasValue)
				{
					dictionary.Add("wd", bMWindWater.Value);
				}
				if (bMWindWeakBend.HasValue)
				{
					dictionary.Add("we", bMWindWeakBend.Value);
				}
				if (bMWindWeakWind.HasValue)
				{
					dictionary.Add("wf", bMWindWeakWind.Value);
				}
				if (normal.HasValue)
				{
					dictionary.Add("nrm", normal.Value);
				}
				if (indices.HasValue)
				{
					dictionary.Add("ind", indices.Value);
				}
				if (material.HasValue)
				{
					dictionary.Add("mat", material.Value);
				}
				GltfMaterial[] materials = gltf.Materials;
				GltfMaterial gltfMaterial = ((materials != null) ? materials[material.Value] : null);
				if (gltfMaterial != null)
				{
					new Dictionary<string, long>();
					if (gltfMaterial?.PbrMetallicRoughness != null)
					{
						GltfPbrMetallicRoughness pbrMetallicRoughness = gltfMaterial.PbrMetallicRoughness;
						array = gltfMaterial.PbrMetallicRoughness.BaseColorFactor ?? array;
						array2 = gltfMaterial.PbrMetallicRoughness.PbrFactor ?? array2;
						GltfMatTexture baseColorTexture = pbrMetallicRoughness.BaseColorTexture;
						if (baseColorTexture != null)
						{
							_ = baseColorTexture.Index;
							if (true)
							{
								dictionary.Add("bcr", gltf.Images[pbrMetallicRoughness.BaseColorTexture.Index].BufferView);
							}
						}
						GltfMatTexture metallicRoughnessTexture = pbrMetallicRoughness.MetallicRoughnessTexture;
						if (metallicRoughnessTexture != null)
						{
							_ = metallicRoughnessTexture.Index;
							if (true)
							{
								dictionary.Add("pbr", gltf.Images[pbrMetallicRoughness.MetallicRoughnessTexture.Index].BufferView);
							}
						}
					}
					if (gltfMaterial != null)
					{
						GltfMatTexture normalTexture = gltfMaterial.NormalTexture;
						if (normalTexture != null)
						{
							_ = normalTexture.Index;
							if (true)
							{
								dictionary.Add("ntx", gltf.Images[gltfMaterial.NormalTexture.Index].BufferView);
							}
						}
					}
				}
				foreach (KeyValuePair<string, long> item in dictionary)
				{
					GltfBufferView gltfBufferView = bufferViews[item.Value];
					GltfBuffer obj = buffers[gltfBufferView.Buffer];
					if (!dictionary2.TryGetValue(item.Key, out var value))
					{
						dictionary2.Add(item.Key, new byte[gltfBufferView.ByteLength]);
					}
					value = dictionary2[item.Key];
					byte[] array3 = Convert.FromBase64String(obj.Uri.Replace("data:application/octet-stream;base64,", "")).Copy(gltfBufferView.ByteOffset, gltfBufferView.ByteLength);
					for (int k = 0; k < array3.Length; k++)
					{
						value[k] = array3[k];
					}
				}
				if (dictionary2.TryGetValue("vtx", out var value2))
				{
					temp_vertices.AddRange(value2.ToVec3fs());
				}
				if (dictionary2.TryGetValue("uvs", out var value3))
				{
					temp_uvs.AddRange(value3.ToFloats());
				}
				if (dictionary2.TryGetValue("nrm", out var value4))
				{
					Vec3f[] array4 = value4.ToVec3fs();
					foreach (Vec3f normal2 in array4)
					{
						temp_normals.Add(VertexFlags.PackNormal(normal2) + generalGlowLevel);
					}
				}
				if (dictionary2.TryGetValue("ind", out var value5))
				{
					temp_indices.AddRange(value5.ToUShorts().ToInts());
				}
				if (dictionary2.TryGetValue("mat", out var value6))
				{
					temp_material.AddRange(value6.ToVec3fs());
				}
				if (dictionary2.TryGetValue("vtc", out var value7))
				{
					temp_vertexcolor.AddRange(value7.ToVec4uss());
				}
				dictionary2.TryGetValue("vtg", out var value8);
				ulong[] array5 = value8?.BytesToULongs();
				dictionary2.TryGetValue("vtr", out value8);
				ulong[] array6 = value8?.BytesToULongs();
				dictionary2.TryGetValue("wa", out value8);
				ulong[] array7 = value8?.BytesToULongs();
				dictionary2.TryGetValue("wb", out value8);
				ulong[] array8 = value8?.BytesToULongs();
				dictionary2.TryGetValue("wc", out value8);
				ulong[] array9 = value8?.BytesToULongs();
				dictionary2.TryGetValue("wd", out value8);
				ulong[] array10 = value8?.BytesToULongs();
				dictionary2.TryGetValue("we", out value8);
				ulong[] array11 = value8?.BytesToULongs();
				dictionary2.TryGetValue("wf", out value8);
				ulong[] array12 = value8?.BytesToULongs();
				for (int m = 0; m < temp_vertices.Count; m++)
				{
					tempFlag.All = 0;
					tempFlag.GlowLevel = (byte)((array5 != null && array5[m] != 0) ? ((byte)((double)(array5[m] >> 16) / 281474976710655.0 * 255.0)) : 0);
					tempFlag.Reflective = ((array6 != null) ? array6[m] : 0) >> 16 != 0;
					tempFlag.WindMode = ((((array7 != null) ? array7[m] : 0) >> 16 != 0) ? EnumWindBitMode.Leaves : ((((array8 != null) ? array8[m] : 0) >> 16 != 0) ? EnumWindBitMode.TallBend : ((((array9 != null) ? array9[m] : 0) >> 16 != 0) ? EnumWindBitMode.NormalWind : ((((array10 != null) ? array10[m] : 0) >> 16 != 0) ? EnumWindBitMode.Water : ((((array11 != null) ? array11[m] : 0) >> 16 != 0) ? EnumWindBitMode.Bend : ((((array12 != null) ? array12[m] : 0) >> 16 != 0) ? EnumWindBitMode.WeakWind : EnumWindBitMode.NoWind))))));
					temp_flags.Add(tempFlag.All);
				}
				if (bakedTextures != null)
				{
					byte[] array13 = (dictionary2.ContainsKey("bcr") ? dictionary2["bcr"] : null);
					byte[] array14 = (dictionary2.ContainsKey("pbr") ? dictionary2["pbr"] : null);
					byte[] array15 = (dictionary2.ContainsKey("pbr") ? dictionary2["pbr"] : null);
					bakedTextures[num2] = new byte[3][] { array13, array14, array15 };
				}
				num2++;
				BuildMeshDataPart(node, pos, climateColorMapIndex, seasonColorMapIndex, renderPass, array, array2);
			}
		}
	}

	public void BuildMeshDataPart(GltfNode node, TextureAtlasPosition pos, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, float[] colorFactor, float[] pbrFactor)
	{
		MeshData meshData = new MeshData(temp_vertices.Count, temp_vertices.Count);
		meshData.WithXyzFaces();
		meshData.WithRenderpasses();
		meshData.WithColorMaps();
		meshData.IndicesPerFace = 3;
		meshData.VerticesPerFace = 3;
		meshData.Rgba.Fill(byte.MaxValue);
		capacities[0] += temp_vertices.Count * 3;
		meshData.Flags = new int[temp_vertices.Count];
		for (int i = 0; i < temp_vertices.Count; i++)
		{
			meshData.Flags[i] = temp_flags[i];
			meshData.Flags[i] |= temp_normals[i];
			if (temp_vertexcolor.Count > 0)
			{
				Vec4us vec4us = temp_vertexcolor[i];
				int color = ((byte)((float)(int)vec4us.W / 65535f * 255f) << 24) | ((byte)((float)(int)vec4us.X / 65535f * 255f) << 16) | ((byte)((float)(int)vec4us.Y / 65535f * 255f) << 8) | (byte)((float)(int)vec4us.Z / 65535f * 255f);
				meshData.AddVertexSkipTex(temp_vertices[i].X + 0.5f, temp_vertices[i].Y + 0.5f, temp_vertices[i].Z + 0.5f, color);
			}
			else
			{
				meshData.AddVertexSkipTex(temp_vertices[i].X + 0.5f, temp_vertices[i].Y + 0.5f, temp_vertices[i].Z + 0.5f);
			}
		}
		for (int j = 0; j < temp_indices.Count / 3; j++)
		{
			meshData.AddXyzFace(0);
			meshData.AddTextureId(pos.atlasTextureId);
			if (meshData.ClimateColorMapIds != null)
			{
				meshData.AddColorMapIndex(climateColorMapIndex, seasonColorMapIndex);
			}
			if (meshData.RenderPassesAndExtraBits != null)
			{
				meshData.AddRenderPass(renderPass);
			}
		}
		meshData.Uv = temp_uvs.ToArray();
		meshData.AddIndices(temp_indices.ToArray());
		capacities[1] += temp_indices.Count;
		meshData.VerticesCount = temp_vertices.Count;
		if (pos != null)
		{
			meshData.SetTexPos(pos);
		}
		meshData.XyzFacesCount = temp_indices.Count / 3;
		Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
		if (node.Rotation != null)
		{
			Vec3f vec3f = GameMath.ToEulerAngles(new Vec4f((float)node.Rotation[0], (float)node.Rotation[1], (float)node.Rotation[2], (float)node.Rotation[3]));
			meshData.Rotate(origin, vec3f.X, vec3f.Y, vec3f.Z);
		}
		if (node.Scale != null)
		{
			meshData.Scale(origin, (float)node.Scale[0], (float)node.Scale[1], (float)node.Scale[2]);
		}
		if (node.Translation != null)
		{
			meshData.Translate((float)node.Translation[0], (float)node.Translation[1], (float)node.Translation[2]);
		}
		meshData.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[5]
			{
				colorFactor[0],
				colorFactor[1],
				colorFactor[2],
				pbrFactor[0],
				pbrFactor[1]
			},
			InterleaveSizes = new int[2] { 3, 2 },
			InterleaveStride = 20,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = 5
		};
		meshData.CustomInts = new CustomMeshDataPartInt
		{
			Values = new int[1] { meshPieces.Count * meshData.XyzCount },
			InterleaveSizes = new int[1] { 1 },
			InterleaveStride = 4,
			InterleaveOffsets = new int[1],
			Count = 1
		};
		meshPieces.Add(meshData);
		temp_vertices.Clear();
		temp_uvs.Clear();
		temp_normals.Clear();
		temp_indices.Clear();
		temp_material.Clear();
		temp_vertexcolor.Clear();
		temp_flags.Clear();
	}
}
