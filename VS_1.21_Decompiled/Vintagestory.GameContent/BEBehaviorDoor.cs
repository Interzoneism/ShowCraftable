using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorDoor : BEBehaviorAnimatable, IInteractable, IRotatable
{
	public float RotateYRad;

	protected bool opened;

	protected bool invertHandles;

	protected MeshData mesh;

	protected Cuboidf[] boxesClosed;

	protected Cuboidf[] boxesOpened;

	protected Vec3i leftDoorOffset;

	protected Vec3i rightDoorOffset;

	public BlockBehaviorDoor doorBh;

	public string StoryLockedCode;

	public BlockFacing facingWhenClosed => BlockFacing.HorizontalFromYaw(RotateYRad);

	public BlockFacing facingWhenOpened
	{
		get
		{
			if (!invertHandles)
			{
				return facingWhenClosed.GetCW();
			}
			return facingWhenClosed.GetCCW();
		}
	}

	public BEBehaviorDoor LeftDoor
	{
		get
		{
			if (leftDoorOffset != null)
			{
				BEBehaviorDoor doorAt = BlockBehaviorDoor.getDoorAt(Api.World, base.Pos.AddCopy(leftDoorOffset));
				if (doorAt == null)
				{
					leftDoorOffset = null;
				}
				return doorAt;
			}
			return null;
		}
		protected set
		{
			leftDoorOffset = value?.Pos.SubCopy(base.Pos).ToVec3i();
		}
	}

	public BEBehaviorDoor RightDoor
	{
		get
		{
			if (rightDoorOffset != null)
			{
				BEBehaviorDoor doorAt = BlockBehaviorDoor.getDoorAt(Api.World, base.Pos.AddCopy(rightDoorOffset));
				if (doorAt == null)
				{
					rightDoorOffset = null;
				}
				return doorAt;
			}
			return null;
		}
		protected set
		{
			rightDoorOffset = value?.Pos.SubCopy(base.Pos).ToVec3i();
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

	public bool InvertHandles => invertHandles;

	public BEBehaviorDoor(BlockEntity blockentity)
		: base(blockentity)
	{
		boxesClosed = base.Block.CollisionBoxes;
		doorBh = base.Block.GetBehavior<BlockBehaviorDoor>();
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

	public Vec3i getAdjacentOffset(int right, int back = 0, int up = 0)
	{
		return getAdjacentOffset(right, back, up, RotateYRad, invertHandles);
	}

	public static Vec3i getAdjacentOffset(int right, int back, int up, float rotateYRad, bool invertHandles)
	{
		if (invertHandles)
		{
			right = -right;
		}
		return new Vec3i(right * (int)Math.Round(Math.Sin(rotateYRad + (float)Math.PI / 2f)) - back * (int)Math.Round(Math.Sin(rotateYRad)), up, right * (int)Math.Round(Math.Cos(rotateYRad + (float)Math.PI / 2f)) - back * (int)Math.Round(Math.Cos(rotateYRad)));
	}

	internal void SetupRotationsAndColSelBoxes(bool initalSetup)
	{
		if (initalSetup)
		{
			if (BlockBehaviorDoor.HasCombinableLeftDoor(Api.World, RotateYRad, base.Pos, doorBh.width, out var leftDoor, out var leftOffset) && leftDoor.LeftDoor == null && leftDoor.RightDoor == null && leftDoor.facingWhenClosed == facingWhenClosed)
			{
				if (leftDoor.invertHandles)
				{
					if (leftDoor.doorBh.width > 1)
					{
						Api.World.BlockAccessor.SetBlock(0, leftDoor.Pos);
						BlockPos pos = base.Pos.AddCopy(facingWhenClosed.GetCW(), leftDoor.doorBh.width + doorBh.width - 1);
						Api.World.BlockAccessor.SetBlock(leftDoor.Block.Id, pos);
						leftDoor = base.Block.GetBEBehavior<BEBehaviorDoor>(pos);
						leftDoor.RotateYRad = RotateYRad;
						leftDoor.doorBh.placeMultiblockParts(Api.World, pos);
						LeftDoor = leftDoor;
						LeftDoor.RightDoor = this;
						LeftDoor.SetupRotationsAndColSelBoxes(initalSetup: true);
					}
					else
					{
						leftDoor.invertHandles = false;
						LeftDoor = leftDoor;
						LeftDoor.RightDoor = this;
						LeftDoor.Blockentity.MarkDirty(redrawOnClient: true);
						LeftDoor.SetupRotationsAndColSelBoxes(initalSetup: false);
					}
				}
				else
				{
					LeftDoor = leftDoor;
					LeftDoor.RightDoor = this;
				}
				invertHandles = true;
				Blockentity.MarkDirty(redrawOnClient: true);
			}
			if (BlockBehaviorDoor.HasCombinableRightDoor(Api.World, RotateYRad, base.Pos, doorBh.width, out leftDoor, out leftOffset) && leftDoor.LeftDoor == null && leftDoor.RightDoor == null && leftDoor.facingWhenClosed == facingWhenClosed && Api.Side == EnumAppSide.Server)
			{
				if (!leftDoor.invertHandles)
				{
					if (leftDoor.doorBh.width > 1)
					{
						Api.World.BlockAccessor.SetBlock(0, leftDoor.Pos);
						BlockPos pos2 = base.Pos.AddCopy(facingWhenClosed.GetCCW(), leftDoor.doorBh.width + doorBh.width - 1);
						Api.World.BlockAccessor.SetBlock(leftDoor.Block.Id, pos2);
						leftDoor = base.Block.GetBEBehavior<BEBehaviorDoor>(pos2);
						leftDoor.RotateYRad = RotateYRad;
						leftDoor.invertHandles = true;
						leftDoor.doorBh.placeMultiblockParts(Api.World, pos2);
						RightDoor = leftDoor;
						RightDoor.LeftDoor = this;
						leftDoor.SetupRotationsAndColSelBoxes(initalSetup: true);
					}
					else
					{
						leftDoor.invertHandles = true;
						RightDoor = leftDoor;
						RightDoor.LeftDoor = this;
						RightDoor.Blockentity.MarkDirty(redrawOnClient: true);
						RightDoor.SetupRotationsAndColSelBoxes(initalSetup: false);
					}
				}
				else
				{
					RightDoor = leftDoor;
					RightDoor.LeftDoor = this;
				}
			}
		}
		if (Api.Side == EnumAppSide.Client)
		{
			if (doorBh.animatableOrigMesh == null)
			{
				string text = base.Block.Shape.ToString();
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
		if (RotateYRad != 0f)
		{
			float num = (invertHandles ? (0f - RotateYRad) : RotateYRad);
			mesh = mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, num, 0f);
			animUtil.renderer.rotationDeg.Y = num * (180f / (float)Math.PI);
		}
		if (invertHandles)
		{
			Matrixf matrixf = new Matrixf();
			matrixf.Translate(0.5f, 0.5f, 0.5f).Scale(-1f, 1f, 1f).Translate(-0.5f, -0.5f, -0.5f);
			mesh.MatrixTransform(matrixf.Values);
			animUtil.renderer.backfaceCulling = false;
			animUtil.renderer.ScaleX = -1f;
		}
	}

	protected virtual void UpdateHitBoxes()
	{
		if (RotateYRad != 0f)
		{
			boxesClosed = base.Block.CollisionBoxes;
			Cuboidf[] array = new Cuboidf[boxesClosed.Length];
			for (int i = 0; i < boxesClosed.Length; i++)
			{
				array[i] = boxesClosed[i].RotatedCopy(0f, RotateYRad * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
			}
			boxesClosed = array;
		}
		Cuboidf[] array2 = new Cuboidf[boxesClosed.Length];
		for (int j = 0; j < boxesClosed.Length; j++)
		{
			array2[j] = boxesClosed[j].RotatedCopy(0f, invertHandles ? 90 : (-90), 0f, new Vec3d(0.5, 0.5, 0.5));
		}
		boxesOpened = array2;
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byItemStack != null)
		{
			RotateYRad = getRotateYRad(byPlayer, blockSel);
			SetupRotationsAndColSelBoxes(initalSetup: true);
		}
	}

	public static float getRotateYRad(IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
		double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
		float num = (float)Math.Atan2(y, x);
		float num2 = (float)Math.PI / 2f;
		return (float)(int)Math.Round(num / num2) * num2;
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
		float num = base.Block.Attributes["breakOnTriggerChance"].AsFloat();
		if (Api.Side == EnumAppSide.Server && Api.World.Rand.NextDouble() < (double)num && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			Api.World.BlockAccessor.BreakBlock(base.Pos, byPlayer);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), base.Pos, 0.0);
			return;
		}
		this.opened = opened;
		ToggleDoorWing(opened);
		float pitch = (opened ? 1.1f : 0.9f);
		AssetLocation location = ((!opened) ? doorBh?.CloseSound : doorBh?.OpenSound);
		Api.World.PlaySoundAt(location, (float)base.Pos.X + 0.5f, (float)base.Pos.InternalY + 0.5f, (float)base.Pos.Z + 0.5f, byPlayer, EnumSoundType.Sound, pitch);
		if (LeftDoor != null && invertHandles)
		{
			LeftDoor.ToggleDoorWing(opened);
			LeftDoor.UpdateNeighbors();
		}
		else if (RightDoor != null)
		{
			RightDoor.ToggleDoorWing(opened);
			RightDoor.UpdateNeighbors();
		}
		Blockentity.MarkDirty(redrawOnClient: true);
		UpdateNeighbors();
	}

	private void UpdateNeighbors()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		BlockPos blockPos = new BlockPos(base.Pos.dimension);
		for (int i = 0; i < doorBh.height; i++)
		{
			blockPos.Set(base.Pos).Add(0, i, 0);
			BlockFacing facing = BlockFacing.ALLFACES[Opened ? facingWhenClosed.HorizontalAngleIndex : facingWhenOpened.HorizontalAngleIndex];
			for (int j = 0; j < doorBh.width; j++)
			{
				Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
				blockPos.Add(facing);
			}
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
			float num = base.Block.Attributes?["easingSpeed"].AsFloat(10f) ?? 10f;
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
		RotateYRad = tree.GetFloat("rotateYRad");
		opened = tree.GetBool("opened");
		invertHandles = tree.GetBool("invertHandles");
		leftDoorOffset = tree.GetVec3i("leftDoorPos");
		rightDoorOffset = tree.GetVec3i("rightDoorPos");
		StoryLockedCode = tree.GetString("storyLockedCode");
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
		tree.SetFloat("rotateYRad", RotateYRad);
		tree.SetBool("opened", opened);
		tree.SetBool("invertHandles", invertHandles);
		if (StoryLockedCode != null)
		{
			tree.SetString("storyLockedCode", StoryLockedCode);
		}
		if (leftDoorOffset != null)
		{
			tree.SetVec3i("leftDoorPos", leftDoorOffset);
		}
		if (rightDoorOffset != null)
		{
			tree.SetVec3i("rightDoorPos", rightDoorOffset);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine(facingWhenClosed?.ToString() + (invertHandles ? "-inv " : " ") + (opened ? "open" : "closed"));
			dsc.AppendLine(doorBh.height + "x" + doorBh.width + ((leftDoorOffset != null) ? (" leftdoor at:" + leftDoorOffset) : " ") + ((rightDoorOffset != null) ? (" rightdoor at:" + rightDoorOffset) : " "));
			EnumHandling handled = EnumHandling.PassThrough;
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.NORTH, base.Pos, ref handled) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: North");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.EAST, base.Pos, ref handled) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: East");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.SOUTH, base.Pos, ref handled) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: South");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.WEST, base.Pos, ref handled) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: West");
			}
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		RotateYRad = tree.GetFloat("rotateYRad");
		RotateYRad = (RotateYRad - (float)degreeRotation * ((float)Math.PI / 180f)) % ((float)Math.PI * 2f);
		tree.SetFloat("rotateYRad", RotateYRad);
	}
}
