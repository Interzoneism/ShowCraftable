using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEPulverizer : BlockEntityDisplay
{
	private readonly AssetLocation pounderName = new AssetLocation("pounder-oak");

	private readonly AssetLocation toggleName = new AssetLocation("pulverizertoggle-oak");

	public Vec4f lightRbs = new Vec4f();

	private float rotateY;

	private InventoryPulverizer inv;

	internal Matrixf mat = new Matrixf();

	private BEBehaviorMPPulverizer pvBh;

	public bool hasAxle;

	public bool hasLPounder;

	public bool hasRPounder;

	public int CapMetalTierL;

	public int CapMetalTierR;

	public int CapMetalIndexL;

	public int CapMetalIndexR;

	private float accumLeft;

	private float accumRight;

	public BlockFacing Facing { get; protected set; } = BlockFacing.NORTH;

	public virtual Vec4f LightRgba => lightRbs;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "pulverizer";

	public bool hasPounderCaps => !inv[2].Empty;

	public bool IsComplete
	{
		get
		{
			if (hasAxle && hasLPounder && hasRPounder)
			{
				return hasPounderCaps;
			}
			return false;
		}
	}

	public override int DisplayedItems => 2;

	public BEPulverizer()
	{
		inv = new InventoryPulverizer(this, 3);
		inv.SlotModified += Inv_SlotModified;
	}

	private void Inv_SlotModified(int t1)
	{
		updateMeshes();
	}

	public override void Initialize(ICoreAPI api)
	{
		Facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		if (Facing == null)
		{
			Facing = BlockFacing.NORTH;
		}
		switch (Facing.Index)
		{
		case 0:
			rotateY = 180f;
			break;
		case 1:
			rotateY = 90f;
			break;
		case 3:
			rotateY = 270f;
			break;
		}
		mat.Translate(0.5f, 0.5f, 0.5f);
		mat.RotateYDeg(rotateY);
		mat.Translate(-0.5f, -0.5f, -0.5f);
		base.Initialize(api);
		inv.LateInitialize(InventoryClassName + "-" + Pos, api);
		if (api.World.Side == EnumAppSide.Server)
		{
			RegisterGameTickListener(OnServerTick, 200);
		}
		pvBh = GetBehavior<BEBehaviorMPPulverizer>();
	}

	private void OnServerTick(float dt)
	{
		if (!IsComplete)
		{
			return;
		}
		float num = pvBh.Network?.Speed ?? 0f;
		num = Math.Abs(num * 3f) * pvBh.GearedRatio;
		if (!inv[0].Empty)
		{
			accumLeft += dt * num;
			if (accumLeft > 5f)
			{
				accumLeft = 0f;
				Crush(0, CapMetalTierL, -0.25);
			}
		}
		if (!inv[1].Empty)
		{
			accumRight += dt * num;
			if (accumRight > 5f)
			{
				accumRight = 0f;
				Crush(1, CapMetalTierR, 0.25);
			}
		}
	}

	private void Crush(int slot, int capTier, double xOffset)
	{
		ItemStack itemStack = inv[slot].TakeOut(1);
		CrushingProperties crushingProps = itemStack.Collectible.CrushingProps;
		ItemStack itemStack2 = null;
		if (crushingProps != null)
		{
			itemStack2 = crushingProps.CrushedStack?.ResolvedItemstack.Clone();
			if (itemStack2 != null)
			{
				itemStack2.StackSize = GameMath.RoundRandom(Api.World.Rand, crushingProps.Quantity.nextFloat(itemStack2.StackSize, Api.World.Rand));
			}
			if (itemStack2.StackSize <= 0)
			{
				return;
			}
		}
		Vec3d position = mat.TransformVector(new Vec4d(xOffset * 0.999, 0.1, 0.8, 0.0)).XYZ.Add(Pos).Add(0.5, 0.0, 0.5);
		double num = Api.World.Rand.NextDouble() * 0.07 - 0.035;
		double num2 = Api.World.Rand.NextDouble() * 0.03 - 0.005;
		Vec3d velocity = new Vec3d((Facing.Axis == EnumAxis.Z) ? num2 : num, Api.World.Rand.NextDouble() * 0.02 - 0.01, (Facing.Axis == EnumAxis.Z) ? num : num2);
		bool flag = itemStack2 != null && itemStack.Collectible.CrushingProps.HardnessTier <= capTier;
		Api.World.SpawnItemEntity(flag ? itemStack2 : itemStack, position, velocity);
		MarkDirty(redrawOnClient: true);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		ICoreClientAPI capi = Api as ICoreClientAPI;
		MeshData orCreate = ObjectCacheUtil.GetOrCreate(capi, "pulverizertopmesh-" + rotateY, delegate
		{
			Shape shape = Shape.TryGet(capi, "shapes/block/wood/mechanics/pulverizer-top.json");
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata, new Vec3f(0f, rotateY, 0f));
			return modeldata;
		});
		MeshData orCreate2 = ObjectCacheUtil.GetOrCreate(capi, "pulverizerbasemesh-" + rotateY, delegate
		{
			Shape shape = Shape.TryGet(capi, "shapes/block/wood/mechanics/pulverizer-base.json");
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata, new Vec3f(0f, rotateY, 0f));
			return modeldata;
		});
		mesher.AddMeshData(orCreate);
		mesher.AddMeshData(orCreate2);
		for (int num = 0; num < Behaviors.Count; num++)
		{
			Behaviors[num].OnTesselation(mesher, tesselator);
		}
		return true;
	}

	public ItemStack[] getDrops(IWorldAccessor world, ItemStack pulvFrame)
	{
		int num = 0;
		if (hasLPounder)
		{
			num++;
		}
		if (hasRPounder)
		{
			num++;
		}
		ItemStack[] array = new ItemStack[num + ((!hasAxle) ? 1 : 2)];
		int num2 = 0;
		array[num2++] = pulvFrame;
		for (int i = 0; i < num; i++)
		{
			array[num2++] = new ItemStack(world.GetItem(pounderName));
		}
		if (hasAxle)
		{
			array[num2] = new ItemStack(world.GetItem(toggleName));
		}
		return array;
	}

	public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		Vec4d vec = new Vec4d(blockSel.HitPosition.X, blockSel.HitPosition.Y, blockSel.HitPosition.Z, 1.0);
		Vec4d vec4d = mat.TransformVector(vec);
		int num = ((Facing.Axis == EnumAxis.Z) ? 1 : 0);
		ItemSlot itemSlot = ((vec4d.X < 0.5) ? inv[num] : inv[1 - num]);
		if (activeHotbarSlot.Empty)
		{
			TryTake(itemSlot, byPlayer);
		}
		else
		{
			if (TryAddPart(activeHotbarSlot, byPlayer))
			{
				Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.25, byPlayer);
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return true;
			}
			if (activeHotbarSlot.Itemstack.Collectible.CrushingProps != null)
			{
				TryPut(activeHotbarSlot, itemSlot);
			}
		}
		return true;
	}

	private bool TryAddPart(ItemSlot slot, IPlayer toPlayer)
	{
		if (!hasAxle && slot.Itemstack.Collectible.Code.Path == "pulverizertoggle-oak")
		{
			if (toPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			hasAxle = true;
			MarkDirty(redrawOnClient: true);
			return true;
		}
		if ((!hasLPounder || !hasRPounder) && slot.Itemstack.Collectible.Code.Path == "pounder-oak")
		{
			if (toPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			if (hasLPounder)
			{
				hasRPounder = true;
			}
			hasLPounder = true;
			MarkDirty(redrawOnClient: true);
			return true;
		}
		if (slot.Itemstack.Collectible.FirstCodePart() == "poundercap")
		{
			if (hasLPounder && hasRPounder)
			{
				if (slot.Itemstack.StackSize < 2)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "require2caps", Lang.Get("Please add 2 caps at the same time!"));
					return true;
				}
				ItemStack itemstack = slot.TakeOut(2);
				if (!inv[2].Empty && !toPlayer.InventoryManager.TryGiveItemstack(inv[2].Itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(inv[2].Itemstack, Pos);
				}
				inv[2].Itemstack = itemstack;
				slot.MarkDirty();
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "require2pounders", Lang.Get("Please add pounders before adding caps!"));
			}
			return true;
		}
		return false;
	}

	private void TryPut(ItemSlot fromSlot, ItemSlot intoSlot)
	{
		if (fromSlot.TryPutInto(Api.World, intoSlot) > 0)
		{
			fromSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void TryTake(ItemSlot fromSlot, IPlayer toPlayer)
	{
		ItemStack itemstack = fromSlot.TakeOut(1);
		if (!toPlayer.InventoryManager.TryGiveItemstack(itemstack))
		{
			Api.World.SpawnItemEntity(itemstack, Pos.ToVec3d().Add(0.5, 0.1, 0.5));
		}
		MarkDirty(redrawOnClient: true);
	}

	public override void updateMeshes()
	{
		string text = "nometal";
		if (!inv[2].Empty)
		{
			text = inv[2].Itemstack.Collectible.Variant["metal"];
		}
		MetalPropertyVariant value = null;
		if (text != null)
		{
			Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(text, out value);
		}
		CapMetalTierL = (CapMetalTierR = Math.Max(value?.Tier ?? 0, 0));
		CapMetalIndexL = (CapMetalIndexR = Math.Max(0, PulverizerRenderer.metals.IndexOf(text)));
		base.updateMeshes();
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[2][];
		for (int i = 0; i < 2; i++)
		{
			float num = ((i % 2 == 0) ? (23f / 32f) : (9f / 32f));
			Matrixf matrixf = new Matrixf().Set(mat.Values);
			matrixf.Translate(num - 0.5f, 0.25f, -9f / 32f);
			matrixf.Translate(0.5f, 0f, 0.5f);
			matrixf.Scale(0.6f, 0.6f, 0.6f);
			matrixf.Translate(-0.5f, 0f, -0.5f);
			array[i] = matrixf.Values;
		}
		return array;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		hasLPounder = tree.GetBool("hasLPounder");
		hasRPounder = tree.GetBool("hasRPounder");
		hasAxle = tree.GetBool("hasAxle");
		RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("hasLPounder", hasLPounder);
		tree.SetBool("hasRPounder", hasRPounder);
		tree.SetBool("hasAxle", hasAxle);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		sb.AppendLine(Lang.Get("Pulverizing:"));
		bool flag = true;
		for (int i = 0; i < 2; i++)
		{
			if (!inv[i].Empty)
			{
				flag = false;
				sb.AppendLine("  " + inv[i].StackSize + " x " + inv[i].GetStackName());
			}
		}
		if (flag)
		{
			sb.AppendLine("  " + Lang.Get("nothing"));
		}
	}
}
