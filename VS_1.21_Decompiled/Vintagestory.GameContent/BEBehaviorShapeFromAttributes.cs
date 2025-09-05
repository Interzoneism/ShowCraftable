using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorShapeFromAttributes : BlockEntityBehavior, IRotatable, IExtraWrenchModes
{
	public string Type;

	public BlockShapeFromAttributes clutterBlock;

	protected MeshData mesh;

	public float rotateX;

	public float rotateZ;

	public bool Collected;

	public string overrideTextureCode;

	public float repairState;

	public int reparability;

	protected static Vec3f Origin = new Vec3f(0.5f, 0.5f, 0.5f);

	public float offsetX;

	public float offsetY;

	public float offsetZ;

	protected bool loadMeshDuringTesselation;

	public float rotateY { get; internal set; }

	public BEBehaviorShapeFromAttributes(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public SkillItem[] GetExtraWrenchModes(IPlayer byPlayer, BlockSelection blockSelection)
	{
		return clutterBlock?.extraWrenchModes;
	}

	public void OnWrenchInteract(IPlayer player, BlockSelection blockSel, int mode, int rightmouseBtn)
	{
		switch (mode)
		{
		case 0:
			offsetZ += (float)(1 - rightmouseBtn * 2) / 16f;
			break;
		case 1:
			offsetX += (float)(1 - rightmouseBtn * 2) / 16f;
			break;
		case 2:
			offsetY += (float)(1 - rightmouseBtn * 2) / 16f;
			break;
		}
		loadMesh();
		Blockentity.MarkDirty(redrawOnClient: true);
		Api.World.PlaySoundAt(base.Block.Sounds.Place, blockSel.Position, 0.0, player);
		(Api.World as IClientWorldAccessor)?.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		clutterBlock = base.Block as BlockShapeFromAttributes;
		if (Type != null)
		{
			MaybeInitialiseMesh_OnMainThread();
			clutterBlock.GetBehavior<BlockBehaviorReparable>()?.Initialize(Type, this);
		}
	}

	public virtual void loadMesh()
	{
		if (Type == null || Api == null || Api.Side == EnumAppSide.Server)
		{
			return;
		}
		IShapeTypeProps shapeTypeProps = clutterBlock?.GetTypeProps(Type, null, this);
		if (shapeTypeProps == null)
		{
			return;
		}
		bool flag = offsetX == 0f && offsetY == 0f && offsetZ == 0f;
		float num = rotateY + shapeTypeProps.Rotation.Y * ((float)Math.PI / 180f);
		MeshData orCreateMesh = clutterBlock.GetOrCreateMesh(shapeTypeProps, null, overrideTextureCode);
		if (shapeTypeProps.RandomizeYSize)
		{
			BlockShapeFromAttributes blockShapeFromAttributes = clutterBlock;
			if (blockShapeFromAttributes == null || blockShapeFromAttributes.AllowRandomizeDims)
			{
				mesh = orCreateMesh.Clone().Rotate(Origin, rotateX, num, rotateZ).Scale(Vec3f.Zero, 1f, 0.98f + (float)GameMath.MurmurHash3Mod(base.Pos.X, base.Pos.Y, base.Pos.Z, 1000) / 1000f * 0.04f, 1f);
				goto IL_0183;
			}
		}
		if (rotateX == 0f && num == 0f && rotateZ == 0f && flag)
		{
			mesh = orCreateMesh;
		}
		else
		{
			mesh = orCreateMesh.Clone().Rotate(Origin, rotateX, num, rotateZ);
		}
		goto IL_0183;
		IL_0183:
		if (!flag)
		{
			mesh.Translate(offsetX, offsetY, offsetZ);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			Type = byItemStack.Attributes.GetString("type");
			Collected = byItemStack.Attributes.GetBool("collected");
		}
		loadMesh();
		clutterBlock.GetBehavior<BlockBehaviorReparable>()?.Initialize(Type, this);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		IShapeTypeProps shapeTypeProps = clutterBlock?.GetTypeProps(Type, null, this);
		if (shapeTypeProps?.LightHsv != null)
		{
			Api.World.BlockAccessor.RemoveBlockLight(shapeTypeProps.LightHsv, base.Pos);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		string type = Type;
		string text = overrideTextureCode;
		float num = rotateX;
		float num2 = rotateY;
		float num3 = rotateZ;
		float num4 = offsetX;
		float num5 = offsetY;
		float num6 = offsetZ;
		Type = tree.GetString("type");
		if (Type != null)
		{
			Type = BlockClutter.Remap(worldAccessForResolve, Type);
		}
		rotateX = tree.GetFloat("rotateX");
		rotateY = tree.GetFloat("meshAngle");
		rotateZ = tree.GetFloat("rotateZ");
		overrideTextureCode = tree.GetString("overrideTextureCode");
		Collected = tree.GetBool("collected");
		repairState = tree.GetFloat("repairState");
		offsetX = tree.GetFloat("offsetX");
		offsetY = tree.GetFloat("offsetY");
		offsetZ = tree.GetFloat("offsetZ");
		if (worldAccessForResolve.Side == EnumAppSide.Client && Api != null && (mesh == null || type != Type || text != overrideTextureCode || rotateX != num || rotateY != num2 || rotateZ != num3 || offsetX != num4 || offsetY != num5 || offsetZ != num6))
		{
			MaybeInitialiseMesh_OnMainThread();
			relight(type);
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	protected void relight(string oldType)
	{
		IShapeTypeProps shapeTypeProps = clutterBlock?.GetTypeProps(oldType, null, this);
		if (shapeTypeProps?.LightHsv != null)
		{
			Api.World.BlockAccessor.RemoveBlockLight(shapeTypeProps.LightHsv, base.Pos);
		}
		if ((clutterBlock?.GetTypeProps(Type, null, this))?.LightHsv != null)
		{
			Api.World.BlockAccessor.ExchangeBlock(base.Block.Id, base.Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		tree.SetString("type", Type);
		tree.SetFloat("rotateX", rotateX);
		tree.SetFloat("meshAngle", rotateY);
		tree.SetFloat("rotateZ", rotateZ);
		tree.SetBool("collected", Collected);
		tree.SetFloat("repairState", repairState);
		tree.SetFloat("offsetX", offsetX);
		tree.SetFloat("offsetY", offsetY);
		tree.SetFloat("offsetZ", offsetZ);
		if (overrideTextureCode != null)
		{
			tree.SetString("overrideTextureCode", overrideTextureCode);
		}
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		MaybeInitialiseMesh_OffThread();
		mesher.AddMeshData(mesh);
		return true;
	}

	protected void MaybeInitialiseMesh_OnMainThread()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			if (RequiresTextureUploads())
			{
				loadMesh();
			}
			else
			{
				loadMeshDuringTesselation = true;
			}
		}
	}

	protected void MaybeInitialiseMesh_OffThread()
	{
		if (loadMeshDuringTesselation)
		{
			loadMeshDuringTesselation = false;
			loadMesh();
		}
	}

	private bool RequiresTextureUploads()
	{
		IShapeTypeProps shapeTypeProps = clutterBlock?.GetTypeProps(Type, null, this);
		if (shapeTypeProps == null)
		{
			return false;
		}
		if (shapeTypeProps?.Textures == null && overrideTextureCode == null)
		{
			return false;
		}
		return !MSShapeFromAttrCacheHelper.IsInCache(Api as ICoreClientAPI, base.Block, shapeTypeProps, overrideTextureCode);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		float thetaX = tree.GetFloat("rotateX");
		float thetaY = tree.GetFloat("meshAngle");
		float thetaZ = tree.GetFloat("rotateZ");
		IShapeTypeProps shapeTypeProps = clutterBlock?.GetTypeProps(Type, null, this);
		if (shapeTypeProps != null)
		{
			thetaY += shapeTypeProps.Rotation.Y * ((float)Math.PI / 180f);
		}
		float[] array = Mat4f.Create();
		Mat4f.RotateY(array, array, (float)(-degreeRotation) * ((float)Math.PI / 180f));
		Mat4f.RotateX(array, array, thetaX);
		Mat4f.RotateY(array, array, thetaY);
		Mat4f.RotateZ(array, array, thetaZ);
		Mat4f.ExtractEulerAngles(array, ref thetaX, ref thetaY, ref thetaZ);
		if (shapeTypeProps != null)
		{
			thetaY -= shapeTypeProps.Rotation.Y * ((float)Math.PI / 180f);
		}
		tree.SetFloat("rotateX", thetaX);
		tree.SetFloat("meshAngle", thetaY);
		tree.SetFloat("rotateZ", thetaZ);
		rotateX = thetaX;
		rotateY = thetaY;
		rotateZ = thetaZ;
		float num = tree.GetFloat("offsetX");
		offsetY = tree.GetFloat("offsetY");
		float num2 = tree.GetFloat("offsetZ");
		switch (degreeRotation)
		{
		case 90:
			offsetX = 0f - num2;
			offsetZ = num;
			break;
		case 180:
			offsetX = 0f - num;
			offsetZ = 0f - num2;
			break;
		case 270:
			offsetX = num2;
			offsetZ = 0f - num;
			break;
		}
		tree.SetFloat("offsetX", offsetX);
		tree.SetFloat("offsetY", offsetY);
		tree.SetFloat("offsetZ", offsetZ);
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		if (byEntity.Controls.ShiftKey)
		{
			if (blockSel.Face.Axis == EnumAxis.X)
			{
				rotateX += (float)Math.PI / 2f * (float)dir;
			}
			if (blockSel.Face.Axis == EnumAxis.Y)
			{
				rotateY += (float)Math.PI / 2f * (float)dir;
			}
			if (blockSel.Face.Axis == EnumAxis.Z)
			{
				rotateZ += (float)Math.PI / 2f * (float)dir;
			}
		}
		else
		{
			float num = (float)Math.PI / 8f;
			rotateY += num * (float)dir;
		}
		loadMesh();
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("<font color=\"#bbbbbb\">Type:" + Type + "</font>");
		}
	}

	public string GetFullCode()
	{
		return clutterBlock.BaseCodeForName() + Type?.Replace("/", "-");
	}
}
