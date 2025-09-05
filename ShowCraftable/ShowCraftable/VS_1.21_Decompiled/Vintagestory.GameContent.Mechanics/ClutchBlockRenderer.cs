using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class ClutchBlockRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats1;

	private CustomMeshDataPartFloat matrixAndLightFloats2;

	private MeshRef blockMeshRef1;

	private MeshRef blockMeshRef2;

	public ClutchBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		AssetLocation shapePath = new AssetLocation("shapes/block/wood/mechanics/clutch-arm.json");
		Shape shape = Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var modeldata, meshRotationDeg);
		meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		Shape shape2 = Shape.TryGet(capi, new AssetLocation("shapes/block/wood/mechanics/clutch-drum.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, shape2, out var modeldata2, meshRotationDeg);
		modeldata.CustomFloats = (matrixAndLightFloats1 = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		modeldata.CustomFloats.SetAllocationSize(202000);
		modeldata2.CustomFloats = (matrixAndLightFloats2 = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		modeldata2.CustomFloats.SetAllocationSize(202000);
		blockMeshRef1 = capi.Render.UploadMesh(modeldata);
		blockMeshRef2 = capi.Render.UploadMesh(modeldata2);
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev)
	{
		if (dev is BEClutch { AngleRad: var angleRad } bEClutch)
		{
			float num = bEClutch.RotationNeighbour();
			float rotX = angleRad * (float)dev.AxisSign[0];
			float rotY = angleRad * (float)dev.AxisSign[1];
			float rotZ = angleRad * (float)dev.AxisSign[2];
			UpdateLightAndTransformMatrix(matrixAndLightFloats1.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ, bEClutch.hinge, null);
			Vec3f axis = new Vec3f(0.5f - (float)dev.AxisSign[2] * 0.125f, 0.625f, 0.5f + (float)dev.AxisSign[0] * 0.125f);
			rotX = num * (float)dev.AxisSign[0];
			rotY = num * (float)dev.AxisSign[1];
			rotZ = num * (float)dev.AxisSign[2];
			UpdateLightAndTransformMatrix(matrixAndLightFloats2.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ, axis, (float[])tmpMat.Clone());
		}
	}

	protected float[] UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotY, float rotZ, Vec3f axis, float[] xtraTransform)
	{
		if (xtraTransform == null)
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
		return tmpMat;
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
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef1?.Dispose();
		blockMeshRef2?.Dispose();
	}
}
