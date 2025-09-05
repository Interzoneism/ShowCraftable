using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorTrapDoor : BEBehaviorAnimatable, IInteractable, IRotatable
{
	protected bool opened;

	protected MeshData mesh;

	protected Cuboidf[] boxesClosed;

	protected Cuboidf[] boxesOpened;

	public int AttachedFace;

	public int RotDeg;

	protected BlockBehaviorTrapDoor doorBh;

	public float RotRad => (float)RotDeg * ((float)Math.PI / 180f);

	public BlockFacing facingWhenClosed
	{
		get
		{
			if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
			{
				return BlockFacing.ALLFACES[AttachedFace].Opposite;
			}
			return BlockFacing.DOWN.FaceWhenRotatedBy(0f, (float)BlockFacing.ALLFACES[AttachedFace].HorizontalAngleIndex * 90f * ((float)Math.PI / 180f) + (float)Math.PI / 2f, RotRad);
		}
	}

	public BlockFacing facingWhenOpened
	{
		get
		{
			if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
			{
				return BlockFacing.ALLFACES[AttachedFace].Opposite.FaceWhenRotatedBy((BlockFacing.ALLFACES[AttachedFace].Negative ? (-90f) : 90f) * ((float)Math.PI / 180f), 0f, 0f).FaceWhenRotatedBy(0f, RotRad, 0f);
			}
			return BlockFacing.ALLFACES[AttachedFace].Opposite;
		}
	}

	public Cuboidf[] ColSelBoxes
	{
		get
		{
			if (!opened)
			{
				return boxesClosed;
			}
			return boxesOpened;
		}
	}

	public bool Opened => opened;

	public BEBehaviorTrapDoor(BlockEntity blockentity)
		: base(blockentity)
	{
		boxesClosed = blockentity.Block.CollisionBoxes;
		doorBh = blockentity.Block.GetBehavior<BlockBehaviorTrapDoor>();
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		SetupRotationsAndColSelBoxes(initalSetup: false);
		if (opened && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened"))
		{
			ToggleDoorWing(opened: true);
		}
	}

	protected void SetupRotationsAndColSelBoxes(bool initalSetup)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			if (doorBh.animatableOrigMesh == null)
			{
				string text = "trapdoor-" + Blockentity.Block.Variant["style"];
				doorBh.animatableOrigMesh = animUtil.CreateMesh(text, null, out var resultingShape, null);
				doorBh.animatableShape = resultingShape;
				doorBh.animatableDictKey = text;
			}
			if (doorBh.animatableOrigMesh != null)
			{
				animUtil.InitializeAnimator(doorBh.animatableDictKey, doorBh.animatableOrigMesh, doorBh.animatableShape, null);
				UpdateMeshAndAnimations();
			}
		}
		UpdateHitBoxes();
	}

	protected virtual void UpdateMeshAndAnimations()
	{
		mesh = doorBh.animatableOrigMesh.Clone();
		Matrixf tfMatrix = getTfMatrix();
		mesh.MatrixTransform(tfMatrix.Values);
		animUtil.renderer.CustomTransform = tfMatrix.Values;
	}

	private Matrixf getTfMatrix(float rotz = 0f)
	{
		if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
		{
			return new Matrixf().Translate(0.5f, 0.5f, 0.5f).RotateYDeg(RotDeg).RotateZDeg(BlockFacing.ALLFACES[AttachedFace].Negative ? 180 : 0)
				.Translate(-0.5f, -0.5f, -0.5f);
		}
		int horizontalAngleIndex = BlockFacing.ALLFACES[AttachedFace].HorizontalAngleIndex;
		Matrixf matrixf = new Matrixf();
		matrixf.Translate(0.5f, 0.5f, 0.5f).RotateYDeg(horizontalAngleIndex * 90).RotateYDeg(90f)
			.RotateZDeg(RotDeg)
			.Translate(-0.5f, -0.5f, -0.5f);
		return matrixf;
	}

	protected virtual void UpdateHitBoxes()
	{
		Matrixf tfMatrix = getTfMatrix();
		boxesClosed = Blockentity.Block.CollisionBoxes;
		Cuboidf[] array = new Cuboidf[boxesClosed.Length];
		for (int i = 0; i < boxesClosed.Length; i++)
		{
			array[i] = boxesClosed[i].TransformedCopy(tfMatrix.Values);
		}
		Cuboidf[] array2 = new Cuboidf[boxesClosed.Length];
		for (int j = 0; j < boxesClosed.Length; j++)
		{
			array2[j] = boxesClosed[j].RotatedCopy(90f, 0f, 0f, new Vec3d(0.5, 0.5, 0.5)).TransformedCopy(tfMatrix.Values);
		}
		boxesOpened = array2;
		boxesClosed = array;
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byItemStack != null)
		{
			AttachedFace = blockSel.Face.Index;
			Vec2f vec2f = blockSel.Face.ToAB(blockSel.Face.PlaneCenter);
			Vec2f vec2f2 = blockSel.Face.ToAB(blockSel.HitPosition.ToVec3f());
			RotDeg = (int)Math.Round(180f / (float)Math.PI * (float)Math.Atan2(vec2f.A - vec2f2.A, vec2f.B - vec2f2.B) / 90f) * 90;
			if (blockSel.Face == BlockFacing.WEST || blockSel.Face == BlockFacing.SOUTH)
			{
				RotDeg *= -1;
			}
			SetupRotationsAndColSelBoxes(initalSetup: true);
		}
	}

	public bool IsSideSolid(BlockFacing facing)
	{
		if (opened || facing != facingWhenClosed)
		{
			if (opened)
			{
				return facing == facingWhenOpened;
			}
			return false;
		}
		return true;
	}

	public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (!doorBh.handopenable && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			(Api as ICoreClientAPI).TriggerIngameError(this, "nothandopenable", Lang.Get("This door cannot be opened by hand."));
			return true;
		}
		ToggleDoorState(byPlayer, !opened);
		handling = EnumHandling.PreventDefault;
		return true;
	}

	public void ToggleDoorState(IPlayer byPlayer, bool opened)
	{
		this.opened = opened;
		ToggleDoorWing(opened);
		BlockEntity blockentity = Blockentity;
		float pitch = (opened ? 1.1f : 0.9f);
		BlockBehaviorTrapDoor behavior = Blockentity.Block.GetBehavior<BlockBehaviorTrapDoor>();
		AssetLocation location = ((!opened) ? behavior?.CloseSound : behavior?.OpenSound);
		Api.World.PlaySoundAt(location, (float)blockentity.Pos.X + 0.5f, (float)blockentity.Pos.Y + 0.5f, (float)blockentity.Pos.Z + 0.5f, byPlayer, EnumSoundType.Sound, pitch);
		blockentity.MarkDirty(redrawOnClient: true);
		if (Api.Side == EnumAppSide.Server)
		{
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(base.Pos);
		}
	}

	private void ToggleDoorWing(bool opened)
	{
		this.opened = opened;
		if (!opened)
		{
			animUtil.StopAnimation("opened");
		}
		else
		{
			float num = Blockentity.Block.Attributes?["easingSpeed"].AsFloat(10f) ?? 10f;
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "opened",
				Code = "opened",
				EaseInSpeed = num,
				EaseOutSpeed = num
			});
		}
		Blockentity.MarkDirty();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(mesh);
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		bool flag = opened;
		AttachedFace = tree.GetInt("attachedFace");
		RotDeg = tree.GetInt("rotDeg");
		opened = tree.GetBool("opened");
		if (opened != flag && animUtil != null)
		{
			ToggleDoorWing(opened);
		}
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			UpdateMeshAndAnimations();
			if (opened && !flag && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened"))
			{
				ToggleDoorWing(opened: true);
			}
			UpdateHitBoxes();
			Api.World.BlockAccessor.MarkBlockDirty(base.Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("attachedFace", AttachedFace);
		tree.SetInt("rotDeg", RotDeg);
		tree.SetBool("opened", opened);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		AttachedFace = tree.GetInt("attachedFace");
		BlockFacing blockFacing = BlockFacing.ALLFACES[AttachedFace];
		if (blockFacing.IsVertical)
		{
			RotDeg = tree.GetInt("rotDeg");
			RotDeg = GameMath.Mod(RotDeg - degreeRotation, 360);
			tree.SetInt("rotDeg", RotDeg);
		}
		else
		{
			int num = degreeRotation / 90;
			int num2 = GameMath.Mod(blockFacing.HorizontalAngleIndex - num, 4);
			BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[num2];
			AttachedFace = blockFacing2.Index;
			tree.SetInt("attachedFace", AttachedFace);
		}
	}
}
