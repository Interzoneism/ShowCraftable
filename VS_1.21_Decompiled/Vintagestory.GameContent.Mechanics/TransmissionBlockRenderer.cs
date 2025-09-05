using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class TransmissionBlockRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats1;

	private CustomMeshDataPartFloat matrixAndLightFloats2;

	private MeshRef blockMeshRef1;

	private MeshRef blockMeshRef2;

	public TransmissionBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		AssetLocation shapePath = shapeLoc.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var modeldata, meshRotationDeg);
		CompositeShape compositeShape = new CompositeShape
		{
			Base = new AssetLocation("shapes/block/wood/mechanics/transmission-rightgear.json")
		};
		meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		Shape shape2 = Shape.TryGet(capi, compositeShape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
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
		if (dev is BEBehaviorMPTransmission bEBehaviorMPTransmission)
		{
			float num = bEBehaviorMPTransmission.RotationNeighbour(1, allowIndirect: true);
			float num2 = bEBehaviorMPTransmission.RotationNeighbour(0, allowIndirect: true);
			UpdateLightAndTransformMatrix(rotX: num * (float)dev.AxisSign[0], rotY: num * (float)dev.AxisSign[1], rotZ: num * (float)dev.AxisSign[2], values: matrixAndLightFloats1.Values, index: index, distToCamera: distToCamera, lightRgba: dev.LightRgba);
			float rotX = num2 * (float)dev.AxisSign[0];
			float rotY = num2 * (float)dev.AxisSign[1];
			float rotZ = num2 * (float)dev.AxisSign[2];
			UpdateLightAndTransformMatrix(matrixAndLightFloats2.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
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
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef1?.Dispose();
		blockMeshRef2?.Dispose();
	}
}
