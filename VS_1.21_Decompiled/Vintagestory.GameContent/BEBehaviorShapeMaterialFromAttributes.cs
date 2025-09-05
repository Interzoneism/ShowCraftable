using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorShapeMaterialFromAttributes : BlockEntityBehavior, IRotatable
{
	private MeshData? mesh;

	private float[] mat;

	public string? Type { get; set; }

	public string? Material { get; private set; }

	public float MeshAngleY { get; set; }

	public float MeshAngleX { get; set; }

	public float MeshAngleZ { get; set; }

	public BEBehaviorShapeMaterialFromAttributes(BlockEntity blockEntity)
		: base(blockEntity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (mesh == null && Type != null)
		{
			Init();
		}
	}

	public void Init()
	{
		if (Api != null && Type != null && Material != null && base.Block is BlockShapeMaterialFromAttributes && Api.Side == EnumAppSide.Client)
		{
			mesh = (base.Block as BlockShapeMaterialFromAttributes)?.GetOrCreateMesh(Type, Material);
			mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).Rotate(MeshAngleX, MeshAngleY, MeshAngleZ)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("type", Type);
		tree.SetString("material", Material);
		tree.SetFloat("meshAngleRad", MeshAngleY);
		tree.SetFloat("meshAngleX", MeshAngleX);
		tree.SetFloat("meshAngleZ", MeshAngleZ);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Type = tree.GetString("type");
		Material = tree.GetString("material");
		MeshAngleY = tree.GetFloat("meshAngleRad");
		MeshAngleX = tree.GetFloat("meshAngleX");
		MeshAngleZ = tree.GetFloat("meshAngleZ");
		Init();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
	}

	public override void OnBlockPlaced(ItemStack? byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (Type == null)
		{
			string text = (Type = byItemStack?.Attributes.GetString("type"));
		}
		if (Material == null)
		{
			string text = (Material = byItemStack?.Attributes.GetString("material"));
		}
		Init();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(mesh, mat);
		return true;
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngleY = tree.GetFloat("meshAngleRad");
		MeshAngleY -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngleRad", MeshAngleY);
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		if (byEntity.Controls.ShiftKey)
		{
			if (blockSel.Face.Axis == EnumAxis.X)
			{
				MeshAngleX += (float)Math.PI / 2f * (float)dir;
			}
			if (blockSel.Face.Axis == EnumAxis.Y)
			{
				MeshAngleY += (float)Math.PI / 2f * (float)dir;
			}
			if (blockSel.Face.Axis == EnumAxis.Z)
			{
				MeshAngleZ += (float)Math.PI / 2f * (float)dir;
			}
		}
		else
		{
			float num = (float)Math.PI / 8f;
			MeshAngleY += num * (float)dir;
		}
		Init();
		Blockentity.MarkDirty(redrawOnClient: true);
	}
}
