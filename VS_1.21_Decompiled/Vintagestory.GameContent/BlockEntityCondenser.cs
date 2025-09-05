using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCondenser : BlockEntityLiquidContainer
{
	private MeshData currentMesh;

	private BlockCondenser ownBlock;

	private MeshData bucketMesh;

	private MeshData bucketMeshTmp;

	private long lastReceivedDistillateTotalMs = -99999L;

	private ItemStack lastReceivedDistillate;

	private Vec3f spoutPos;

	private Vec3d steamposmin;

	private Vec3d steamposmax;

	private float partialStackAccum;

	public override string InventoryClassName => "condenser";

	public BlockEntityCondenser()
	{
		inventory = new InventoryGeneric(2, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockCondenser;
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
			RegisterGameTickListener(clientTick, 200, Api.World.Rand.Next(50));
			if (!inventory[1].Empty && bucketMesh == null)
			{
				genBucketMesh();
			}
		}
		Matrixf matrixf = new Matrixf();
		matrixf.Translate(0.5f, 0f, 0.5f).RotateYDeg(base.Block.Shape.rotateY - 90f).Translate(-0.5f, 0f, -0.5f);
		spoutPos = matrixf.TransformVector(new Vec4f(0.5f, 15f / 32f, 7f / 32f, 1f)).XYZ;
		Vec3f xYZ = matrixf.TransformVector(new Vec4f(0.375f, 0.8125f, 0.5625f, 1f)).XYZ;
		Vec3f xYZ2 = matrixf.TransformVector(new Vec4f(0.625f, 0.8125f, 0.8125f, 1f)).XYZ;
		steamposmin = Pos.ToVec3d().Add(xYZ);
		steamposmax = Pos.ToVec3d().Add(xYZ2);
	}

	private void clientTick(float dt)
	{
		if (Api.World.ElapsedMilliseconds - lastReceivedDistillateTotalMs < 10000)
		{
			int randomColor = lastReceivedDistillate.Collectible.GetRandomColor(Api as ICoreClientAPI, lastReceivedDistillate);
			Vec3d vec3d = Pos.ToVec3d().Add(spoutPos);
			if (!inventory[0].Empty)
			{
				Api.World.SpawnParticles(0.5f, ColorUtil.ToRgba(64, 255, 255, 255), steamposmin, steamposmax, new Vec3f(-0.1f, 0.2f, -0.1f), new Vec3f(0.1f, 0.3f, 0.1f), 1.5f, 0f, 0.25f);
				Api.World.SpawnParticles(1f, randomColor, vec3d, vec3d, new Vec3f(), new Vec3f(), 0.08f, 1f, 0.15f);
			}
			else
			{
				Api.World.SpawnParticles(0.33f, randomColor, vec3d, vec3d, new Vec3f(), new Vec3f(), 0.08f, 1f, 0.15f);
			}
		}
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack itemstack = activeHotbarSlot.Itemstack;
		if (blockSel.SelectionBoxIndex < 2)
		{
			if (activeHotbarSlot.Empty && !inventory[1].Empty)
			{
				AssetLocation assetLocation = inventory[1].Itemstack?.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				if (!byPlayer.InventoryManager.TryGiveItemstack(inventory[1].Itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(inventory[1].Itemstack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Condenser at {2}.", byPlayer.PlayerName, inventory[1].Itemstack?.Collectible.Code, blockSel.Position);
				inventory[1].Itemstack = null;
				MarkDirty(redrawOnClient: true);
				bucketMesh?.Clear();
				return true;
			}
			if (itemstack != null && itemstack.Collectible is BlockLiquidContainerTopOpened { CapacityLitres: >=1f, CapacityLitres: <20f } && inventory[1].Empty)
			{
				if (activeHotbarSlot.TryPutInto(Api.World, inventory[1]) > 0)
				{
					AssetLocation assetLocation2 = inventory[1].Itemstack?.Block?.Sounds?.Place;
					Api.World.PlaySoundAt((assetLocation2 != null) ? assetLocation2 : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
					activeHotbarSlot.MarkDirty();
					MarkDirty(redrawOnClient: true);
					genBucketMesh();
				}
				return true;
			}
		}
		return false;
	}

	private void genBucketMesh()
	{
		if (Api != null && inventory != null && inventory.Count >= 2 && !inventory[1].Empty && Api.Side != EnumAppSide.Server)
		{
			ItemStack itemstack = inventory[1].Itemstack;
			IContainedMeshSource containedMeshSource = itemstack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
			if (containedMeshSource != null)
			{
				bucketMeshTmp = containedMeshSource.GenMesh(itemstack, (Api as ICoreClientAPI).BlockTextureAtlas, Pos);
				bucketMeshTmp.CustomInts = new CustomMeshDataPartInt(bucketMeshTmp.FlagsCount);
				bucketMeshTmp.CustomInts.Count = bucketMeshTmp.FlagsCount;
				bucketMeshTmp.CustomInts.Values.Fill(268435456);
				bucketMeshTmp.CustomFloats = new CustomMeshDataPartFloat(bucketMeshTmp.FlagsCount * 2);
				bucketMeshTmp.CustomFloats.Count = bucketMeshTmp.FlagsCount * 2;
				bucketMesh = bucketMeshTmp.Clone().Translate(0f, 0f, 0.375f).Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)Math.PI / 2f + base.Block.Shape.rotateY * ((float)Math.PI / 180f), 0f);
			}
		}
	}

	public bool ReceiveDistillate(ItemSlot sourceSlot, DistillationProps props)
	{
		if (sourceSlot.Empty)
		{
			lastReceivedDistillateTotalMs = -99999L;
			return true;
		}
		if (inventory[1].Empty)
		{
			lastReceivedDistillateTotalMs = -99999L;
			return false;
		}
		ItemStack itemStack = props.DistilledStack.ResolvedItemstack.Clone();
		lastReceivedDistillate = itemStack.Clone();
		ItemStack itemstack = inventory[1].Itemstack;
		BlockLiquidContainerTopOpened blockLiquidContainerTopOpened = itemstack.Collectible as BlockLiquidContainerTopOpened;
		if (blockLiquidContainerTopOpened.IsEmpty(itemstack))
		{
			if (Api.Side == EnumAppSide.Server)
			{
				itemStack.StackSize = 1;
				blockLiquidContainerTopOpened.SetContent(itemstack, itemStack);
			}
		}
		else
		{
			ItemStack content = blockLiquidContainerTopOpened.GetContent(itemstack);
			if (!content.Equals(Api.World, itemStack, GlobalConstants.IgnoredStackAttributes) || blockLiquidContainerTopOpened.IsFull(itemstack))
			{
				lastReceivedDistillateTotalMs = -99999L;
				return false;
			}
			if (Api.Side == EnumAppSide.Server)
			{
				if (!inventory[0].Empty || Api.World.Rand.NextDouble() > 0.5)
				{
					content.StackSize++;
					blockLiquidContainerTopOpened.SetContent(itemstack, content);
				}
				if (!inventory[0].Empty && Api.World.Rand.NextDouble() < 0.5)
				{
					inventory[0].TakeOut(1);
				}
			}
		}
		float num = 1f / props.Ratio - partialStackAccum;
		int num2 = (int)Math.Ceiling(num);
		partialStackAccum = num - (float)num2;
		if (num2 > 0)
		{
			sourceSlot.TakeOut(num2);
		}
		MarkDirty(redrawOnClient: true);
		lastReceivedDistillateTotalMs = Api.World.ElapsedMilliseconds;
		return true;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	internal MeshData GenMesh()
	{
		if (ownBlock == null)
		{
			return null;
		}
		MeshData meshData = ownBlock.GenMesh(Api as ICoreClientAPI, GetContent(), Pos);
		if (meshData.CustomInts != null)
		{
			for (int i = 0; i < meshData.CustomInts.Count; i++)
			{
				meshData.CustomInts.Values[i] |= 536870912;
				meshData.CustomInts.Values[i] |= 268435456;
			}
		}
		return meshData;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh);
		mesher.AddMeshData(bucketMesh);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			genBucketMesh();
		}
		partialStackAccum = tree.GetFloat("partialStackAccum");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("partialStackAccum", partialStackAccum);
	}
}
