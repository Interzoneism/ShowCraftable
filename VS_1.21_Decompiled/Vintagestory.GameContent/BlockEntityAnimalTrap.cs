using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityAnimalTrap : BlockEntityDisplay, IAnimalFoodSource, IPointOfInterest
{
	protected ICoreServerAPI sapi;

	protected CompositeShape destroyedShape;

	protected CompositeShape trappedShape;

	protected float rotationYDeg;

	protected float[] rotMat;

	protected string traptype;

	protected ModelTransform baitTransform;

	protected float foodTagMinWeight;

	protected InventoryGeneric inv;

	public EnumTrapState TrapState;

	protected BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "baskettrap";

	public override int DisplayedItems => (TrapState == EnumTrapState.Ready) ? 1 : 0;

	public override string AttributeTransformCode => "baskettrap";

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.25, 0.5);

	public string Type
	{
		get
		{
			if (!inv.Empty)
			{
				return "food";
			}
			return "nothing";
		}
	}

	public float RotationYDeg
	{
		get
		{
			return rotationYDeg;
		}
		set
		{
			rotationYDeg = value;
			rotMat = Matrixf.Create().Translate(0.5f, 0f, 0.5f).RotateYDeg(rotationYDeg - 90f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
	}

	public BlockEntityAnimalTrap()
	{
		inv = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		traptype = base.Block.Attributes["traptype"].AsString("small");
		foodTagMinWeight = base.Block.Attributes["foodTagMinWeight"].AsFloat(0.1f);
		baitTransform = base.Block.Attributes["baitTransform"].AsObject(ModelTransform.NoTransform);
		base.Initialize(api);
		inv.LateInitialize("baskettrap-" + Pos, api);
		destroyedShape = base.Block.Attributes["destroyedShape"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
		trappedShape = base.Block.Attributes["trappedShape"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
		destroyedShape.Bake(api.Assets, api.Logger);
		trappedShape.Bake(api.Assets, api.Logger);
		sapi = api as ICoreServerAPI;
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientTick, 1000);
			animUtil?.InitializeAnimator("baskettrap", null, null, new Vec3f(0f, rotationYDeg, 0f));
			if (TrapState == EnumTrapState.Trapped)
			{
				animUtil?.StartAnimation(new AnimationMetaData
				{
					Animation = "triggered",
					Code = "triggered"
				});
			}
		}
		else
		{
			sapi.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
	}

	private void OnClientTick(float dt)
	{
		if (TrapState == EnumTrapState.Trapped && !inv.Empty && Api.World.Rand.NextDouble() > 0.8 && BlockBehaviorCreatureContainer.GetStillAliveDays(Api.World, inv[0].Itemstack) > 0.0 && animUtil.activeAnimationsByAnimCode.Count < 2)
		{
			string text = ((Api.World.Rand.NextDouble() > 0.5) ? "hopshake" : "shaking");
			animUtil?.StartAnimation(new AnimationMetaData
			{
				Animation = text,
				Code = text
			});
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/reedtrapshake*"), Pos, -0.25, null, randomizePitch: true, 16f);
		}
	}

	public bool Interact(IPlayer player, BlockSelection blockSel)
	{
		EnumTrapState trapState = TrapState;
		if ((trapState == EnumTrapState.Ready || trapState == EnumTrapState.Destroyed) ? true : false)
		{
			return true;
		}
		if (!Api.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		if (inv[0].Empty)
		{
			ItemStack itemStack = new ItemStack(base.Block);
			if (TrapState == EnumTrapState.Empty)
			{
				tryReadyTrap(player);
			}
			else
			{
				if (!player.InventoryManager.ActiveHotbarSlot.Empty)
				{
					return true;
				}
				if (!player.InventoryManager.TryGiveItemstack(itemStack))
				{
					Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.BlockAccessor.SetBlock(0, Pos);
				Api.World.Logger.Audit("{0} Took 1x{1} at {2}.", player.PlayerName, itemStack.Collectible.Code, blockSel.Position);
			}
		}
		else
		{
			if (!player.InventoryManager.TryGiveItemstack(inv[0].Itemstack))
			{
				Api.World.SpawnItemEntity(inv[0].Itemstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.Logger.Audit("{0} Took 1x{1} with {2} at {3}.", player.PlayerName, inv[0].Itemstack.Collectible.Code, inv[0].Itemstack.Attributes.GetString("creaturecode"), blockSel.Position);
		}
		return true;
	}

	private void tryReadyTrap(IPlayer player)
	{
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot != null && !activeHotbarSlot.Empty && base.Block is BlockAnimalTrap blockAnimalTrap)
		{
			if (!blockAnimalTrap.IsAppetizingBait(Api, activeHotbarSlot.Itemstack))
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "unappetizingbait", Lang.Get("animaltrap-unappetizingbait-error"));
				return;
			}
			if (!blockAnimalTrap.CanFitBait(Api, activeHotbarSlot.Itemstack))
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "cannotfitintrap", Lang.Get("animaltrap-cannotfitintrap-error"));
				return;
			}
			TrapState = EnumTrapState.Ready;
			inv[0].Itemstack = activeHotbarSlot.TakeOut(1);
			activeHotbarSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
		}
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (TrapState != EnumTrapState.Ready)
		{
			return false;
		}
		if (inv[0]?.Itemstack == null || diet == null)
		{
			return false;
		}
		bool num = TrapChances.IsTrappable(entity, traptype);
		bool flag = diet.Matches(inv[0].Itemstack, checkCategory: false, foodTagMinWeight);
		return num && flag;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		sapi.Event.EnqueueMainThreadTask(delegate
		{
			TrapAnimal(entity);
		}, "trapanimal");
		return 1f;
	}

	private void TrapAnimal(Entity entity)
	{
		animUtil?.StartAnimation(new AnimationMetaData
		{
			Animation = "triggered",
			Code = "triggered"
		});
		if (!TrapChances.FromEntityAttr(entity).TryGetValue(traptype, out var value))
		{
			return;
		}
		if (Api.World.Rand.NextDouble() < (double)value.TrapChance)
		{
			JsonItemStack jsonItemStack = base.Block.Attributes["creatureContainer"].AsObject<JsonItemStack>();
			jsonItemStack.Resolve(Api.World, "creature container of " + base.Block.Code);
			inv[0].Itemstack = jsonItemStack.ResolvedItemstack;
			BlockBehaviorCreatureContainer.CatchCreature(inv[0], entity);
		}
		else
		{
			inv[0].Itemstack = null;
			float trapDestroyChance = value.TrapDestroyChance;
			if (Api.World.Rand.NextDouble() < (double)trapDestroyChance)
			{
				TrapState = EnumTrapState.Destroyed;
				MarkDirty(redrawOnClient: true);
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), Pos, -0.25, null, randomizePitch: false, 16f);
				return;
			}
		}
		TrapState = EnumTrapState.Trapped;
		MarkDirty(redrawOnClient: true);
		Api.World.PlaySoundAt(new AssetLocation("sounds/block/reedtrapshut"), Pos, -0.25, null, randomizePitch: false, 16f);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		TrapState = (EnumTrapState)tree.GetInt("trapState");
		RotationYDeg = tree.GetFloat("rotationYDeg");
		if (TrapState == EnumTrapState.Trapped)
		{
			animUtil?.StartAnimation(new AnimationMetaData
			{
				Animation = "triggered",
				Code = "triggered"
			});
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("trapState", (int)TrapState);
		tree.SetFloat("rotationYDeg", rotationYDeg);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (TrapState == EnumTrapState.Trapped && !inv.Empty)
		{
			ItemStack itemstack = inv[0].Itemstack;
			itemstack.Collectible.GetBehavior<BlockBehaviorCreatureContainer>()?.AddCreatureInfo(itemstack, dsc, Api.World);
		}
		else
		{
			dsc.Append(BlockEntityShelf.PerishableInfoCompact(Api, inv[0], 0f));
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		tfMatrices = new float[1][];
		for (int i = 0; i < 1; i++)
		{
			tfMatrices[i] = new float[16];
			if (!inv[i].Empty)
			{
				JsonObject attributes = inv[i].Itemstack.Collectible.Attributes;
				ModelTransform modelTransform = attributes?["inTrapTransform"][traptype].AsObject<ModelTransform>() ?? attributes?["inTrapTransform"].AsObject<ModelTransform>();
				if (modelTransform == null)
				{
					ModelTransform modelTransform2 = inv[i].Itemstack.Collectible.GroundTransform.Clone();
					modelTransform2.ScaleXYZ *= 0.2f;
					modelTransform = modelTransform2;
				}
				float[] values = new Matrixf().Set(baitTransform.AsMatrix).Translate(0.5f, 0f, 0.5f).RotateYDeg(RotationYDeg - 90f)
					.Translate(-0.5f, 0f, -0.5f)
					.Values;
				Mat4f.Mul(tfMatrices[i], values, modelTransform.AsMatrix);
			}
		}
		return tfMatrices;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (TrapState == EnumTrapState.Destroyed)
		{
			mesher.AddMeshData(GetOrCreateMesh(destroyedShape, tessThreadTesselator.GetTextureSource(base.Block)), rotMat);
			return true;
		}
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(capi.TesselatorManager.GetDefaultBlockMesh(base.Block), rotMat);
		}
		return true;
	}

	public MeshData GetCurrentMesh(ITexPositionSource texSource)
	{
		switch (TrapState)
		{
		case EnumTrapState.Empty:
		case EnumTrapState.Ready:
			return GetOrCreateMesh(base.Block.Shape);
		case EnumTrapState.Trapped:
			return GetOrCreateMesh(trappedShape, texSource);
		case EnumTrapState.Destroyed:
			return GetOrCreateMesh(destroyedShape, texSource);
		default:
			return null;
		}
	}

	public MeshData GetOrCreateMesh(CompositeShape cshape, ITexPositionSource texSource = null)
	{
		string key = base.Block.Variant["material"] + "BasketTrap-" + cshape.ToString();
		return ObjectCacheUtil.GetOrCreate(capi, key, () => capi.TesselatorManager.CreateMesh("basket trap decal", cshape, (Shape shape, string name) => new ShapeTextureSource(capi, shape, name), texSource));
	}
}
