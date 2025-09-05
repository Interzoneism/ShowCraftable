using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class CreativeRotorRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats1;

	private CustomMeshDataPartFloat matrixAndLightFloats2;

	private CustomMeshDataPartFloat matrixAndLightFloats3;

	private CustomMeshDataPartFloat matrixAndLightFloats4;

	private CustomMeshDataPartFloat matrixAndLightFloats5;

	private MeshRef blockMeshRef1;

	private MeshRef blockMeshRef2;

	private MeshRef blockMeshRef3;

	private MeshRef blockMeshRef4;

	private Vec3f axisCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	public CreativeRotorRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		AssetLocation shapePath = new AssetLocation("shapes/block/metal/mechanics/creativerotor-axle.json");
		Shape shape = Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var modeldata, meshRotationDeg);
		meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		Shape shape2 = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-contra.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, shape2, out var modeldata2, meshRotationDeg);
		Shape shape3 = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-spinbar.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, shape3, out var modeldata3, meshRotationDeg);
		Shape shape4 = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-spinball.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, shape4, out var modeldata4, meshRotationDeg);
		int count = 42000;
		modeldata.CustomFloats = (matrixAndLightFloats1 = createCustomFloats(count));
		modeldata2.CustomFloats = (matrixAndLightFloats2 = createCustomFloats(count));
		modeldata3.CustomFloats = (matrixAndLightFloats3 = createCustomFloats(count));
		modeldata4.CustomFloats = (matrixAndLightFloats4 = createCustomFloats(count));
		matrixAndLightFloats5 = createCustomFloats(count);
		blockMeshRef1 = capi.Render.UploadMesh(modeldata);
		blockMeshRef2 = capi.Render.UploadMesh(modeldata2);
		blockMeshRef3 = capi.Render.UploadMesh(modeldata3);
		blockMeshRef4 = capi.Render.UploadMesh(modeldata4);
	}

	private CustomMeshDataPartFloat createCustomFloats(int count)
	{
		CustomMeshDataPartFloat customMeshDataPartFloat = new CustomMeshDataPartFloat(count);
		customMeshDataPartFloat.Instanced = true;
		customMeshDataPartFloat.InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 };
		customMeshDataPartFloat.InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 };
		customMeshDataPartFloat.InterleaveStride = 80;
		customMeshDataPartFloat.StaticDraw = false;
		customMeshDataPartFloat.SetAllocationSize(count);
		return customMeshDataPartFloat;
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev)
	{
		float angleRad = dev.AngleRad;
		float num = (float)Math.PI * 2f - dev.AngleRad;
		float num2 = angleRad * 2f;
		float num3 = -Math.Abs(dev.AxisSign[0]);
		float num4 = -Math.Abs(dev.AxisSign[2]);
		float rotX = angleRad * num3;
		float rotZ = angleRad * num4;
		UpdateLightAndTransformMatrix(matrixAndLightFloats1.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = num * num3;
		rotZ = num * num4;
		UpdateLightAndTransformMatrix(matrixAndLightFloats2.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = num2 * num3;
		rotZ = num2 * num4;
		UpdateLightAndTransformMatrix(matrixAndLightFloats3.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = (num2 + (float)Math.PI / 4f) * num3;
		rotZ = (num2 + (float)Math.PI / 4f) * num4;
		TransformMatrix(distToCamera, rotX, rotZ, axisCenter);
		rotX = ((num3 == 0f) ? (angleRad * 2f) : 0f);
		rotZ = ((num4 == 0f) ? ((0f - angleRad) * 2f) : 0f);
		num3 = (float)dev.AxisSign[0] * 0.05f;
		num4 = (float)dev.AxisSign[2] * 0.05f;
		UpdateLightAndTransformMatrix(matrixAndLightFloats4.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, new Vec3f(0.5f + num3, 0.5f, 0.5f + num4), (float[])tmpMat.Clone());
		rotX = (num2 + 3.926991f) * (float)(-Math.Abs(dev.AxisSign[0]));
		rotZ = (num2 + 3.926991f) * (float)(-Math.Abs(dev.AxisSign[2]));
		TransformMatrix(distToCamera, rotX, rotZ, axisCenter);
		rotX = ((num3 == 0f) ? (angleRad * 2f) : 0f);
		rotZ = ((num4 == 0f) ? ((0f - angleRad) * 2f) : 0f);
		UpdateLightAndTransformMatrix(matrixAndLightFloats5.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, new Vec3f(0.5f + num3, 0.5f, 0.5f + num4), (float[])tmpMat.Clone());
	}

	private void TransformMatrix(Vec3f distToCamera, float rotX, float rotZ, Vec3f axis)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
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
		Mat4f.Translate(tmpMat, tmpMat, 0f - axis.X, 0f - axis.Y, 0f - axis.Z);
	}

	protected void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotZ, Vec3f axis, float[] initialTransform)
	{
		if (initialTransform == null)
		{
			Mat4f.Identity(tmpMat);
			Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
		}
		else
		{
			Mat4f.Translate(tmpMat, tmpMat, axis.X, axis.Y, axis.Z);
		}
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
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
		Mat4f.Translate(tmpMat, tmpMat, 0f - axis.X, 0f - axis.Y, 0f - axis.Z);
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

	public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (quantityBlocks > 0)
		{
			matrixAndLightFloats1.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats1;
			capi.Render.UpdateMesh(blockMeshRef1, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef1, quantityBlocks);
			matrixAndLightFloats2.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats2;
			capi.Render.UpdateMesh(blockMeshRef2, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef2, quantityBlocks);
			matrixAndLightFloats3.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats3;
			capi.Render.UpdateMesh(blockMeshRef3, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef3, quantityBlocks);
			matrixAndLightFloats4.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats4;
			capi.Render.UpdateMesh(blockMeshRef4, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef4, quantityBlocks);
			matrixAndLightFloats5.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats5;
			capi.Render.UpdateMesh(blockMeshRef4, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef4, quantityBlocks);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef1?.Dispose();
		blockMeshRef2?.Dispose();
		blockMeshRef3?.Dispose();
		blockMeshRef4?.Dispose();
	}
}
