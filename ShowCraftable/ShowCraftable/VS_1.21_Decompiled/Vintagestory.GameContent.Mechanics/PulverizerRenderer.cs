using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class PulverizerRenderer : MechBlockRenderer, ITexPositionSource
{
	public static string[] metals = new string[7] { "nometal", "tinbronze", "bismuthbronze", "blackbronze", "iron", "meteoriciron", "steel" };

	private CustomMeshDataPartFloat matrixAndLightFloatsAxle;

	private CustomMeshDataPartFloat[] matrixAndLightFloatsLPounder = new CustomMeshDataPartFloat[metals.Length];

	private CustomMeshDataPartFloat[] matrixAndLightFloatsRPounder = new CustomMeshDataPartFloat[metals.Length];

	private readonly MeshRef toggleMeshref;

	private readonly MeshRef[] lPoundMeshrefs = new MeshRef[metals.Length];

	private readonly MeshRef[] rPounderMeshrefs = new MeshRef[metals.Length];

	private readonly Vec3f axisCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	private int quantityAxles;

	private int[] quantityLPounders = new int[metals.Length];

	private int[] quantityRPounders = new int[metals.Length];

	private ITexPositionSource texSource;

	private string metal;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "cap")
			{
				return texSource["capmetal-" + metal];
			}
			return texSource[textureCode];
		}
	}

	public PulverizerRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		int count = 4000;
		AssetLocation shapePath = new AssetLocation("shapes/block/wood/mechanics/pulverizer-moving.json");
		Shape shape = Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY + 90f, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var modeldata, meshRotationDeg);
		modeldata.CustomFloats = (matrixAndLightFloatsAxle = createCustomFloats(count));
		toggleMeshref = capi.Render.UploadMesh(modeldata);
		AssetLocation shapePath2 = new AssetLocation("shapes/block/wood/mechanics/pulverizer-pounder-l.json");
		AssetLocation shapePath3 = new AssetLocation("shapes/block/wood/mechanics/pulverizer-pounder-r.json");
		Shape shapeBase = Shape.TryGet(capi, shapePath2);
		Shape shapeBase2 = Shape.TryGet(capi, shapePath3);
		texSource = capi.Tesselator.GetTextureSource(textureSoureBlock);
		for (int i = 0; i < metals.Length; i++)
		{
			metal = metals[i];
			matrixAndLightFloatsLPounder[i] = createCustomFloats(count);
			matrixAndLightFloatsRPounder[i] = createCustomFloats(count);
			capi.Tesselator.TesselateShape("pulverizer-pounder-l", shapeBase, out var modeldata2, this, meshRotationDeg, 0, 0, 0);
			capi.Tesselator.TesselateShape("pulverizer-pounder-r", shapeBase2, out var modeldata3, this, meshRotationDeg, 0, 0, 0);
			modeldata2.CustomFloats = matrixAndLightFloatsLPounder[i];
			modeldata3.CustomFloats = matrixAndLightFloatsRPounder[i];
			lPoundMeshrefs[i] = capi.Render.UploadMesh(modeldata2);
			rPounderMeshrefs[i] = capi.Render.UploadMesh(modeldata3);
		}
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
		BEBehaviorMPPulverizer bEBehaviorMPPulverizer = dev as BEBehaviorMPPulverizer;
		float num = (bEBehaviorMPPulverizer.bepu.hasAxle ? dev.AngleRad : 0f);
		float num2 = -Math.Abs(dev.AxisSign[0]);
		float num3 = -Math.Abs(dev.AxisSign[2]);
		if (bEBehaviorMPPulverizer.bepu.hasAxle)
		{
			float rotX = num * num2;
			float rotZ = num * num3;
			UpdateLightAndTransformMatrix(matrixAndLightFloatsAxle.Values, quantityAxles, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, 0f);
			quantityAxles++;
		}
		if (bEBehaviorMPPulverizer.isRotationReversed())
		{
			num = 0f - num;
		}
		int capMetalIndexL = bEBehaviorMPPulverizer.bepu.CapMetalIndexL;
		if (bEBehaviorMPPulverizer.bepu.hasLPounder && capMetalIndexL >= 0)
		{
			bool empty = bEBehaviorMPPulverizer.bepu.Inventory[1].Empty;
			float progress = GetProgress(bEBehaviorMPPulverizer.bepu.hasAxle ? (num - 0.45f + (float)Math.PI / 4f) : 0f, 0f);
			UpdateLightAndTransformMatrix(matrixAndLightFloatsLPounder[capMetalIndexL].Values, quantityLPounders[capMetalIndexL], distToCamera, dev.LightRgba, 0f, 0f, axisCenter, Math.Max(progress / 6f + 0.0071f, empty ? (-1f) : (1f / 32f)));
			if (progress < bEBehaviorMPPulverizer.prevProgressLeft && progress < 0.25f)
			{
				if (bEBehaviorMPPulverizer.leftDir == 1)
				{
					bEBehaviorMPPulverizer.OnClientSideImpact(right: false);
				}
				bEBehaviorMPPulverizer.leftDir = -1;
			}
			else
			{
				bEBehaviorMPPulverizer.leftDir = 1;
			}
			bEBehaviorMPPulverizer.prevProgressLeft = progress;
			quantityLPounders[capMetalIndexL]++;
		}
		int capMetalIndexR = bEBehaviorMPPulverizer.bepu.CapMetalIndexR;
		if (!bEBehaviorMPPulverizer.bepu.hasRPounder || capMetalIndexR < 0)
		{
			return;
		}
		bool empty2 = bEBehaviorMPPulverizer.bepu.Inventory[0].Empty;
		float progress2 = GetProgress(bEBehaviorMPPulverizer.bepu.hasAxle ? (num - 0.45f) : 0f, 0f);
		UpdateLightAndTransformMatrix(matrixAndLightFloatsRPounder[capMetalIndexR].Values, quantityRPounders[capMetalIndexR], distToCamera, dev.LightRgba, 0f, 0f, axisCenter, Math.Max(progress2 / 6f + 0.0071f, empty2 ? (-1f) : (1f / 32f)));
		if (progress2 < bEBehaviorMPPulverizer.prevProgressRight && progress2 < 0.25f)
		{
			if (bEBehaviorMPPulverizer.rightDir == 1)
			{
				bEBehaviorMPPulverizer.OnClientSideImpact(right: true);
			}
			bEBehaviorMPPulverizer.rightDir = -1;
		}
		else
		{
			bEBehaviorMPPulverizer.rightDir = 1;
		}
		bEBehaviorMPPulverizer.prevProgressRight = progress2;
		quantityRPounders[capMetalIndexR]++;
	}

	private float GetProgress(float rot, float offset)
	{
		float num = rot % ((float)Math.PI / 2f) / ((float)Math.PI / 2f);
		if (num < 0f)
		{
			num += 1f;
		}
		num = 0.6355f * (float)Math.Atan(2.2f * num - 1.2f) + 0.5f;
		if (num > 0.9f)
		{
			num = 2.7f - 3f * num;
			num = 0.9f - num * num * 10f;
		}
		if (num < 0f)
		{
			num = 0f;
		}
		return num;
	}

	protected void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotZ, Vec3f axis, float translate)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y + translate, distToCamera.Z + axis.Z);
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
		quantityAxles = 0;
		for (int i = 0; i < metals.Length; i++)
		{
			quantityLPounders[i] = 0;
			quantityRPounders[i] = 0;
		}
		UpdateCustomFloatBuffer();
		if (quantityAxles > 0)
		{
			matrixAndLightFloatsAxle.Count = quantityAxles * 20;
			updateMesh.CustomFloats = matrixAndLightFloatsAxle;
			capi.Render.UpdateMesh(toggleMeshref, updateMesh);
			capi.Render.RenderMeshInstanced(toggleMeshref, quantityAxles);
		}
		for (int j = 0; j < metals.Length; j++)
		{
			int num = quantityLPounders[j];
			int num2 = quantityRPounders[j];
			if (num > 0)
			{
				matrixAndLightFloatsLPounder[j].Count = num * 20;
				updateMesh.CustomFloats = matrixAndLightFloatsLPounder[j];
				capi.Render.UpdateMesh(lPoundMeshrefs[j], updateMesh);
				capi.Render.RenderMeshInstanced(lPoundMeshrefs[j], num);
			}
			if (num2 > 0)
			{
				matrixAndLightFloatsRPounder[j].Count = num2 * 20;
				updateMesh.CustomFloats = matrixAndLightFloatsRPounder[j];
				capi.Render.UpdateMesh(rPounderMeshrefs[j], updateMesh);
				capi.Render.RenderMeshInstanced(rPounderMeshrefs[j], num2);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		toggleMeshref?.Dispose();
		for (int i = 0; i < metals.Length; i++)
		{
			lPoundMeshrefs[i]?.Dispose();
			rPounderMeshrefs[i]?.Dispose();
		}
	}
}
