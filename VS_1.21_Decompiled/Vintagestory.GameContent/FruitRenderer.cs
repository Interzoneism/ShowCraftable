using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitRenderer
{
	private Dictionary<Vec3d, FruitData> positions = new Dictionary<Vec3d, FruitData>();

	private bool onGround;

	protected ICoreClientAPI capi;

	protected MeshData itemMesh;

	protected MeshRef meshref;

	private CustomMeshDataPartFloat matrixAndLightFloats;

	protected Vec3f tmp = new Vec3f();

	protected float[] tmpMat = Mat4f.Create();

	protected double[] quat = Quaterniond.Create();

	protected float[] qf = new float[4];

	protected float[] rotMat = Mat4f.Create();

	private static Vec3f noRotation = new Vec3f(0f, 0f, 0f);

	private Vec3f v = new Vec3f();

	private static int nextID = 0;

	private int id;

	private Vec3f i = new Vec3f();

	private Vec3f f = new Vec3f();

	public FruitRenderer(ICoreClientAPI capi, Item item)
	{
		this.capi = capi;
		id = nextID++;
		CompositeShape shape = item.Shape;
		if (item.Attributes != null && item.Attributes["onGround"].AsBool())
		{
			onGround = true;
		}
		AssetLocation shapePath = shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape2 = Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ);
		capi.Tesselator.TesselateShape(item, shape2, out itemMesh, meshRotationDeg, null, shape.SelectiveElements);
		itemMesh.CustomFloats = (matrixAndLightFloats = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		itemMesh.CustomFloats.SetAllocationSize(202000);
		meshref = capi.Render.UploadMesh(itemMesh);
	}

	internal void Dispose()
	{
		meshref?.Dispose();
	}

	internal void AddFruit(Vec3d position, FruitData data)
	{
		positions[position] = data;
	}

	internal void RemoveFruit(Vec3d position)
	{
		positions.Remove(position);
	}

	internal void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (positions.Count > 0)
		{
			matrixAndLightFloats.Count = positions.Count * 20;
			itemMesh.CustomFloats = matrixAndLightFloats;
			capi.Render.UpdateMesh(meshref, itemMesh);
			capi.Render.RenderMeshInstanced(meshref, positions.Count);
		}
	}

	protected virtual void UpdateCustomFloatBuffer()
	{
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		float x = GlobalConstants.CurrentWindSpeedClient.X;
		float num = 1f;
		float num2 = 105f;
		DefaultShaderUniforms shaderUniforms = capi.Render.ShaderUniforms;
		float windWaveCounterHighFreq = shaderUniforms.WindWaveCounterHighFreq;
		float windWaveCounter = shaderUniforms.WindWaveCounter;
		int num3 = 0;
		foreach (KeyValuePair<Vec3d, FruitData> position in positions)
		{
			Vec3d key = position.Key;
			Vec3f rotation = position.Value.rotation;
			double x2 = key.X;
			double y = key.Y;
			double z = key.Z;
			float rotY = rotation.Y;
			float num4 = rotation.X;
			float num5 = rotation.Z;
			if (onGround)
			{
				BlockPos pos = position.Value.behavior.Blockentity.Pos;
				y = (double)pos.Y - 0.0625;
				x2 += 1.1 * (x2 - (double)pos.X - 0.5);
				z += 1.1 * (z - (double)pos.Z - 0.5);
				rotation = noRotation;
				rotY = (float)((x2 + z) * 40.0 % 90.0);
			}
			else
			{
				double num6 = x2;
				double num7 = y;
				double num8 = z;
				float num9 = 0.7f * (0.5f + (float)num7 - (float)(int)num7);
				double num10 = (double)(num * (1f + x)) * (0.5 + (y - (double)position.Value.behavior.Blockentity.Pos.Y)) / 2.0;
				v.Set((float)num6 % 4096f / 10f, (float)num8 % 4096f / 10f, windWaveCounter % 1024f / 4f);
				float num11 = x * 0.2f + 1.4f * gnoise(v);
				float val = x * (0.8f + num11) * num9 * num;
				val = Math.Min(4f, val) * 0.2857143f / 2.8f;
				num6 += (double)windWaveCounterHighFreq;
				num7 += (double)windWaveCounterHighFreq;
				num8 += (double)windWaveCounterHighFreq;
				num10 *= 0.25;
				double num12 = num10 * (Math.Sin(num6 * 10.0) / 120.0 + (2.0 * Math.Sin(num6 / 2.0) + Math.Sin(num6 + num7) + Math.Sin(0.5 + 4.0 * num6 + 2.0 * num7) + Math.Sin(1.0 + 6.0 * num6 + 3.0 * num7) / 3.0) / (double)num2);
				double num13 = num10 * ((2.0 * Math.Sin(num8 / 4.0) + Math.Sin(num8 + 3.0 * num7) + Math.Sin(0.5 + 4.0 * num8 + 2.0 * num7) + Math.Sin(1.0 + 6.0 * num8 + num7) / 3.0) / (double)num2);
				x2 += num12;
				y += num10 * (Math.Sin(5.0 * num7) / 15.0 + Math.Cos(10.0 * num6) / 10.0 + Math.Sin(3.0 * num8) / 2.0 + Math.Cos(num6 * 2.0) / 2.2) / (double)num2;
				z += num13;
				num4 += (float)(num13 * 6.0 + (double)(val / 2f));
				num5 += (float)(num12 * 6.0 + (double)(val / 2f));
				x2 += (double)val;
			}
			tmp.Set((float)(x2 - cameraPos.X), (float)(y - cameraPos.Y), (float)(z - cameraPos.Z));
			UpdateLightAndTransformMatrix(matrixAndLightFloats.Values, num3, tmp, position.Value.behavior.LightRgba, num4, rotY, num5);
			num3++;
		}
	}

	private float ghashDot(Vec3f p, Vec3f q, float oX, float oY, float oZ)
	{
		float num = q.X - oX;
		float num2 = q.Y - oY;
		float num3 = q.Z - oZ;
		oX += p.X;
		oY += p.Y;
		oZ += p.Z;
		float num4 = 127.1f * oX + 311.7f * oY + 74.7f * oZ;
		float num5 = 269.5f * oX + 183.3f * oY + 246.1f * oZ;
		float num6 = 113.5f * oX + 271.9f * oY + 124.6f * oZ;
		return (float)((double)num * (-1.0 + 2.0 * fract((double)GameMath.Mod((num4 * 0.025f + 8f) * num4, 289f) / 41.0)) + (double)num2 * (-1.0 + 2.0 * fract((double)GameMath.Mod((num5 * 0.025f + 8f) * num5, 289f) / 41.0)) + (double)num3 * (-1.0 + 2.0 * fract((double)GameMath.Mod((num6 * 0.025f + 8f) * num6, 289f) / 41.0)));
	}

	private double fract(double v)
	{
		return v - Math.Floor(v);
	}

	private float gnoise(Vec3f p)
	{
		int num = (int)p.X;
		int num2 = (int)p.Y;
		int num3 = (int)p.Z;
		i.Set(num, num2, num3);
		f.Set(p.X - (float)num, p.Y - (float)num2, p.Z - (float)num3);
		float a = f.X * f.X * (3f - 2f * f.X);
		float a2 = f.Y * f.Y * (3f - 2f * f.Y);
		float a3 = f.Z * f.Z * (3f - 2f * f.Z);
		float x = ghashDot(i, f, 0f, 0f, 0f);
		float x2 = ghashDot(i, f, 0f, 0f, 1f);
		float x3 = ghashDot(i, f, 0f, 1f, 0f);
		float x4 = ghashDot(i, f, 0f, 1f, 1f);
		float y = ghashDot(i, f, 1f, 0f, 0f);
		float y2 = ghashDot(i, f, 1f, 0f, 1f);
		float y3 = ghashDot(i, f, 1f, 1f, 0f);
		float y4 = ghashDot(i, f, 1f, 1f, 1f);
		float x5 = mix(mix(x, y, a), mix(x3, y3, a), a2);
		float y5 = mix(mix(x2, y2, a), mix(x4, y4, a), a2);
		return 1.2f * mix(x5, y5, a3);
	}

	private float mix(float x, float y, float a)
	{
		return x * (1f - a) + y * a;
	}

	protected virtual void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotY, float rotZ)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X, distToCamera.Y, distToCamera.Z);
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
		}
		if (rotY != 0f)
		{
			Quaterniond.RotateY(quat, quat, rotY);
		}
		if (rotZ != 0f)
		{
			Quaterniond.RotateZ(quat, quat, rotZ);
		}
		for (int i = 0; i < quat.Length; i++)
		{
			qf[i] = (float)quat[i];
		}
		Mat4f.Mul(tmpMat, tmpMat, Mat4f.FromQuat(rotMat, qf));
		int num = index * 20;
		values[num] = lightRgba.R;
		values[++num] = lightRgba.G;
		values[++num] = lightRgba.B;
		values[++num] = lightRgba.A;
		for (int j = 0; j < 16; j++)
		{
			values[++num] = tmpMat[j];
		}
	}
}
