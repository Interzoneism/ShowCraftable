using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityIngotMold : BlockEntity, ILiquidMetalSink, ITemperatureSensitive, ITexPositionSource, IRotatable
{
	protected long lastPouringMarkdirtyMs;

	protected IngotMoldRenderer? ingotRenderer;

	public MeshData? MoldMeshLeft;

	public MeshData? MoldMeshRight;

	public ItemStack? MoldLeft;

	public ItemStack? MoldRight;

	public ItemStack? ContentsLeft;

	public ItemStack? ContentsRight;

	public int FillLevelLeft;

	public int FillLevelRight;

	public int QuantityMolds = 1;

	public bool IsRightSideSelected;

	public bool ShatteredLeft;

	public bool ShatteredRight;

	public int RequiredUnits = 100;

	private ICoreClientAPI? capi;

	public float MeshAngle;

	private ITexPositionSource? tmpTextureSource;

	private AssetLocation? metalTexLoc;

	private MeshData? shatteredMeshLeft;

	private MeshData? shatteredMeshRight;

	public static readonly Vec3f left = new Vec3f(-0.25f, 0f, 0f);

	public static readonly Vec3f right = new Vec3f(0.1875f, 0f, 0f);

	public float TemperatureLeft => ContentsLeft?.Collectible.GetTemperature(Api.World, ContentsLeft) ?? 0f;

	public float TemperatureRight => ContentsRight?.Collectible.GetTemperature(Api.World, ContentsRight) ?? 0f;

	public bool IsHardenedLeft => TemperatureLeft < 0.3f * ContentsLeft?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsLeft));

	public bool IsHardenedRight => TemperatureRight < 0.3f * ContentsRight?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsRight));

	public bool IsLiquidLeft => TemperatureLeft > 0.8f * ContentsLeft?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsLeft));

	public bool IsLiquidRight => TemperatureRight > 0.8f * ContentsRight?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsRight));

	public bool IsFullLeft => FillLevelLeft >= RequiredUnits;

	public bool IsFullRight => FillLevelRight >= RequiredUnits;

	public bool IsHot
	{
		get
		{
			if (!(TemperatureLeft >= 200f))
			{
				return TemperatureRight >= 200f;
			}
			return true;
		}
	}

	public bool CanReceiveAny
	{
		get
		{
			if (!BothShattered)
			{
				if (!(MoldLeft?.Block?.Variant["type"] == "fired"))
				{
					ItemStack? moldLeft = MoldLeft;
					if ((moldLeft == null || moldLeft.Block?.Code.Path.Contains("burned") != true) && !(MoldRight?.Block.Variant["type"] == "fired"))
					{
						ItemStack? moldRight = MoldRight;
						if (moldRight == null)
						{
							return false;
						}
						return moldRight.Block?.Code.Path.Contains("burned") == true;
					}
				}
				return true;
			}
			return false;
		}
	}

	private bool BothShattered
	{
		get
		{
			if (ShatteredLeft)
			{
				return ShatteredRight;
			}
			return false;
		}
	}

	public ItemStack? SelectedMold
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return MoldLeft;
			}
			return MoldRight;
		}
		set
		{
			IsRightSideSelected ? ref MoldRight : ref MoldLeft = value;
		}
	}

	public ItemStack? SelectedContents
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return ContentsLeft;
			}
			return ContentsRight;
		}
		set
		{
			IsRightSideSelected ? ref ContentsRight : ref ContentsLeft = value;
		}
	}

	public int SelectedFillLevel
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return FillLevelLeft;
			}
			return FillLevelRight;
		}
		set
		{
			IsRightSideSelected ? ref FillLevelRight : ref FillLevelLeft = value;
		}
	}

	public bool SelectedShattered
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return ShatteredLeft;
			}
			return ShatteredRight;
		}
		set
		{
			IsRightSideSelected ? ref ShatteredRight : ref ShatteredLeft = value;
		}
	}

	public float SelectedTemperature
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return TemperatureLeft;
			}
			return TemperatureRight;
		}
	}

	public bool SelectedIsHardened
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return IsHardenedLeft;
			}
			return IsHardenedRight;
		}
	}

	public bool SelectedIsLiquid
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return IsLiquidLeft;
			}
			return IsLiquidRight;
		}
	}

	public bool SelectedIsFull
	{
		get
		{
			if (!IsRightSideSelected)
			{
				return IsFullLeft;
			}
			return IsFullRight;
		}
	}

	public Size2i AtlasSize => capi?.BlockTextureAtlas.Size ?? new Size2i();

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (capi == null || tmpTextureSource == null)
			{
				return new TextureAtlasPosition();
			}
			if (textureCode == "metal")
			{
				return capi.BlockTextureAtlas[metalTexLoc];
			}
			return tmpTextureSource[textureCode];
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (capi != null && !BothShattered)
		{
			capi.Event.RegisterRenderer(ingotRenderer = new IngotMoldRenderer(this, capi), EnumRenderStage.Opaque, "ingotmold");
			UpdateIngotRenderer();
			if (MoldMeshLeft == null || MoldMeshRight == null)
			{
				GenMeshes();
			}
		}
		if (!BothShattered)
		{
			RegisterGameTickListener(OnGameTick, 50);
		}
	}

	private void OnGameTick(float dt)
	{
		if (ingotRenderer != null)
		{
			ingotRenderer.QuantityMolds = QuantityMolds;
			ingotRenderer.LevelLeft = ((!ShatteredLeft) ? FillLevelLeft : 0);
			ingotRenderer.LevelRight = ((!ShatteredRight) ? FillLevelRight : 0);
		}
		if (ContentsLeft != null && ingotRenderer != null)
		{
			ingotRenderer.stack = ContentsLeft;
			ingotRenderer.TemperatureLeft = Math.Min(1300f, ContentsLeft.Collectible.GetTemperature(Api.World, ContentsLeft));
		}
		if (ContentsRight != null && ingotRenderer != null)
		{
			ingotRenderer.stack = ContentsRight;
			ingotRenderer.TemperatureRight = Math.Min(1300f, ContentsRight.Collectible.GetTemperature(Api.World, ContentsRight));
		}
	}

	public bool CanReceive(ItemStack metal)
	{
		if (ContentsLeft != null && ContentsRight != null && (!ContentsLeft.Collectible.Equals(ContentsLeft, metal, GlobalConstants.IgnoredStackAttributes) || IsFullLeft))
		{
			if (ContentsRight.Collectible.Equals(ContentsRight, metal, GlobalConstants.IgnoredStackAttributes))
			{
				return !IsFullRight;
			}
			return false;
		}
		return true;
	}

	public void BeginFill(Vec3d hitPosition)
	{
		SetSelectedSide(hitPosition);
	}

	public void SetSelectedSide(Vec3d hitPosition)
	{
		if (QuantityMolds > 1)
		{
			switch (BlockFacing.HorizontalFromAngle(MeshAngle).Index)
			{
			case 0:
				IsRightSideSelected = hitPosition.Z < 0.5;
				break;
			case 1:
				IsRightSideSelected = hitPosition.X >= 0.5;
				break;
			case 2:
				IsRightSideSelected = hitPosition.Z >= 0.5;
				break;
			case 3:
				IsRightSideSelected = hitPosition.X < 0.5;
				break;
			}
		}
		else
		{
			IsRightSideSelected = false;
		}
	}

	public bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
	{
		if (BothShattered)
		{
			return false;
		}
		bool flag = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is BlockIngotMold;
		bool shiftKey = byPlayer.Entity.Controls.ShiftKey;
		if (!shiftKey)
		{
			if (byPlayer.Entity.Controls.HandUse != EnumHandInteract.None)
			{
				return false;
			}
			bool flag2 = TryTakeIngot(byPlayer, hitPosition);
			if (!flag2)
			{
				flag2 = TryTakeMold(byPlayer, hitPosition);
			}
			return flag2;
		}
		if (shiftKey && flag)
		{
			return TryPutMold(byPlayer);
		}
		return false;
	}

	public ItemStack[] GetStateAwareMolds()
	{
		ItemStack[] stateAwareMoldSided = GetStateAwareMoldSided(MoldLeft, ShatteredLeft);
		ItemStack[] stateAwareMoldSided2 = GetStateAwareMoldSided(MoldRight, ShatteredRight);
		int num = 0;
		ItemStack[] array = new ItemStack[stateAwareMoldSided.Length + stateAwareMoldSided2.Length];
		ReadOnlySpan<ItemStack> readOnlySpan = new ReadOnlySpan<ItemStack>(stateAwareMoldSided);
		readOnlySpan.CopyTo(new Span<ItemStack>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<ItemStack> readOnlySpan2 = new ReadOnlySpan<ItemStack>(stateAwareMoldSided2);
		readOnlySpan2.CopyTo(new Span<ItemStack>(array).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		return array;
	}

	public ItemStack[] GetStateAwareMoldSided(ItemStack? mold, bool shattered)
	{
		if (mold == null)
		{
			return Array.Empty<ItemStack>();
		}
		List<ItemStack> list = new List<ItemStack>();
		if (!shattered)
		{
			list.Add(mold.Clone());
		}
		else
		{
			BlockDropItemStack[] array = mold.Block.Attributes?["shatteredDrops"].AsObject<BlockDropItemStack[]>();
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Resolve(Api.World, "shatteredDrops[" + i + "] for", mold.Block.Code);
					ItemStack nextItemStack = array[i].GetNextItemStack();
					if (nextItemStack != null)
					{
						list.Add(nextItemStack);
						if (array[i].LastDrop)
						{
							break;
						}
					}
				}
			}
		}
		return list.ToArray();
	}

	public ItemStack[] GetStateAwareMoldedStacks()
	{
		List<ItemStack> list = new List<ItemStack>();
		ItemStack stateAwareContentsLeft = GetStateAwareContentsLeft();
		if (stateAwareContentsLeft != null)
		{
			list.Add(stateAwareContentsLeft);
		}
		ItemStack stateAwareContentsRight = GetStateAwareContentsRight();
		if (stateAwareContentsRight != null)
		{
			list.Add(stateAwareContentsRight);
		}
		return list.ToArray();
	}

	public ItemStack? GetSelectedStateAwareContents()
	{
		return GetStateAwareContentsSided(SelectedContents, SelectedFillLevel, SelectedShattered, SelectedIsHardened);
	}

	public ItemStack? GetStateAwareContentsLeft()
	{
		return GetStateAwareContentsSided(ContentsLeft, FillLevelLeft, ShatteredLeft, IsHardenedLeft);
	}

	public ItemStack? GetStateAwareContentsRight()
	{
		return GetStateAwareContentsSided(ContentsRight, FillLevelRight, ShatteredRight, IsHardenedRight);
	}

	public ItemStack? GetStateAwareContentsSided(ItemStack? contents, int fillLevel, bool shattered, bool isHardened)
	{
		if (contents != null && isHardened)
		{
			if (shattered)
			{
				JsonItemStack obj = contents.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
				obj?.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
				ItemStack itemStack = obj?.ResolvedItemstack;
				if (itemStack != null)
				{
					itemStack.StackSize = (int)((float)fillLevel / 5f);
					return itemStack;
				}
				return null;
			}
			if (fillLevel >= RequiredUnits)
			{
				ItemStack itemStack2 = contents.Clone();
				ITreeAttribute obj2 = itemStack2.Attributes["temperature"] as ITreeAttribute;
				if (obj2 != null)
				{
					obj2.RemoveAttribute("cooldownSpeed");
					return itemStack2;
				}
				return itemStack2;
			}
		}
		return null;
	}

	public ItemStack? GetChiseledStack(ItemStack? contents, int fillLevel, bool shattered, bool isHardened)
	{
		if (contents != null && fillLevel > 0 && !shattered && isHardened)
		{
			JsonItemStack obj = contents.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
			obj?.Resolve(Api.World, "chiseledStack for" + contents.Collectible.Code);
			ItemStack itemStack = obj?.ResolvedItemstack;
			if (itemStack != null)
			{
				itemStack.StackSize = (int)((float)fillLevel / 5f);
				return itemStack;
			}
		}
		return null;
	}

	protected bool TryTakeIngot(IPlayer byPlayer, Vec3d hitPosition)
	{
		if (Api is ICoreServerAPI)
		{
			MarkDirty();
		}
		SetSelectedSide(hitPosition);
		ItemStack selectedStateAwareContents = GetSelectedStateAwareContents();
		if (selectedStateAwareContents != null && SelectedIsHardened && !SelectedShattered)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos, -0.5, byPlayer, randomizePitch: false);
			if (Api is ICoreServerAPI)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(selectedStateAwareContents))
				{
					Api.World.SpawnItemEntity(selectedStateAwareContents, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, selectedStateAwareContents.Collectible.Code, Pos);
				SelectedContents = null;
				SelectedFillLevel = 0;
			}
			return true;
		}
		return false;
	}

	protected bool TryTakeMold(IPlayer byPlayer, Vec3d hitPosition)
	{
		ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (itemstack != null)
		{
			CollectibleObject collectible = itemstack.Collectible;
			if (!(collectible is BlockToolMold) && !(collectible is BlockIngotMold))
			{
				return false;
			}
		}
		if (FillLevelLeft != 0 && FillLevelRight != 0)
		{
			return false;
		}
		SetSelectedSide(hitPosition);
		if (SelectedMold == null)
		{
			return false;
		}
		ItemStack itemStack = new ItemStack(SelectedMold.Block);
		AssetLocation assetLocation = SelectedMold.Block.Sounds?.Place;
		if (SelectedFillLevel == 0 && !SelectedShattered)
		{
			if (!IsRightSideSelected && QuantityMolds > 1)
			{
				if (MoldRight == null)
				{
					QuantityMolds--;
					ContentsRight = null;
					FillLevelRight = 0;
					ShatteredRight = false;
					return false;
				}
				MoldLeft = MoldRight;
				MoldMeshLeft = MoldMeshRight;
				ContentsLeft = ContentsRight;
				FillLevelLeft = FillLevelRight;
				ShatteredLeft = ShatteredRight;
				Api.World.BlockAccessor.ExchangeBlock(MoldLeft.Block.BlockId, Pos);
				MoldRight = null;
				ContentsRight = null;
				FillLevelRight = 0;
				ShatteredRight = false;
			}
			QuantityMolds--;
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
			if (QuantityMolds <= 0)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
			}
			else
			{
				MoldRight = null;
				MarkDirty(redrawOnClient: true);
			}
			if (assetLocation != null)
			{
				Api.World.PlaySoundAt(assetLocation, Pos, -0.5, byPlayer, randomizePitch: false);
			}
			return true;
		}
		return false;
	}

	protected bool TryPutMold(IPlayer byPlayer)
	{
		if (QuantityMolds >= 2)
		{
			return false;
		}
		QuantityMolds++;
		MoldRight = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Clone();
		if (MoldRight == null)
		{
			return false;
		}
		MoldRight.StackSize = 1;
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize--;
			if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize == 0)
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;
			}
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		}
		AssetLocation assetLocation = MoldRight.Block.Sounds?.Place;
		if ((object)assetLocation != null)
		{
			Api.World.PlaySoundAt(assetLocation, Pos, -0.5, byPlayer, randomizePitch: false);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			GenMeshes();
		}
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public void UpdateIngotRenderer()
	{
		if (ingotRenderer == null)
		{
			return;
		}
		if (BothShattered)
		{
			capi?.Event.UnregisterRenderer(ingotRenderer, EnumRenderStage.Opaque);
			return;
		}
		ingotRenderer.QuantityMolds = QuantityMolds;
		ingotRenderer.LevelLeft = ((!ShatteredLeft) ? FillLevelLeft : 0);
		ingotRenderer.LevelRight = ((!ShatteredRight) ? FillLevelRight : 0);
		if (ContentsLeft?.Collectible != null)
		{
			ingotRenderer.TextureNameLeft = new AssetLocation("block/metal/ingot/" + ContentsLeft.Collectible.LastCodePart() + ".png");
		}
		else
		{
			ingotRenderer.TextureNameLeft = null;
		}
		if (ContentsRight?.Collectible != null)
		{
			ingotRenderer.TextureNameRight = new AssetLocation("block/metal/ingot/" + ContentsRight.Collectible.LastCodePart() + ".png");
		}
		else
		{
			ingotRenderer.TextureNameRight = null;
		}
	}

	public void ReceiveLiquidMetal(ItemStack metal, ref int amount, float temperature)
	{
		if (lastPouringMarkdirtyMs + 500 < Api.World.ElapsedMilliseconds)
		{
			MarkDirty(redrawOnClient: true);
			lastPouringMarkdirtyMs = Api.World.ElapsedMilliseconds + 500;
		}
		if (!SelectedIsFull && (SelectedContents == null || metal.Collectible.Equals(SelectedContents, metal, GlobalConstants.IgnoredStackAttributes)))
		{
			if (SelectedContents == null)
			{
				SelectedContents = metal.Clone();
				SelectedContents.ResolveBlockOrItem(Api.World);
				SelectedContents.Collectible.SetTemperature(Api.World, SelectedContents, temperature, delayCooldown: false);
				SelectedContents.StackSize = 1;
				(SelectedContents.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 300f);
			}
			else
			{
				SelectedContents.Collectible.SetTemperature(Api.World, SelectedContents, temperature, delayCooldown: false);
			}
			int num = Math.Min(amount, RequiredUnits - SelectedFillLevel);
			SelectedFillLevel += num;
			amount -= num;
			UpdateIngotRenderer();
		}
	}

	public void OnPourOver()
	{
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (ingotRenderer != null)
		{
			ingotRenderer.Dispose();
			ingotRenderer = null;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		switch (QuantityMolds)
		{
		case 0:
			return true;
		case 1:
		{
			if (ShatteredLeft)
			{
				EnsureShatteredMeshesLoaded();
			}
			float[] array = Mat4f.Create();
			Mat4f.Translate(array, array, 0.5f, 0f, 0.5f);
			Mat4f.RotateY(array, array, MeshAngle);
			Mat4f.Translate(array, array, -0.5f, -0f, -0.5f);
			mesher.AddMeshData(ShatteredLeft ? shatteredMeshLeft : MoldMeshLeft, array);
			break;
		}
		case 2:
		{
			if (ShatteredLeft || ShatteredRight)
			{
				EnsureShatteredMeshesLoaded();
			}
			Matrixf matrixf = new Matrixf().Identity();
			matrixf.Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle).Translate(-0.5f, -0f, -0.5f)
				.Translate(left);
			Matrixf matrixf2 = new Matrixf().Identity();
			matrixf2.Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle).Translate(-0.5f, -0f, -0.5f)
				.Translate(right);
			mesher.AddMeshData(ShatteredLeft ? shatteredMeshLeft : MoldMeshLeft, matrixf.Values);
			mesher.AddMeshData(ShatteredRight ? shatteredMeshRight : MoldMeshRight, matrixf2.Values);
			break;
		}
		}
		return true;
	}

	private void EnsureShatteredMeshesLoaded()
	{
		if (ShatteredLeft && shatteredMeshLeft == null)
		{
			metalTexLoc = ((ContentsLeft == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + ContentsLeft.Collectible.LastCodePart()));
			capi?.Tesselator.TesselateShape("shatteredmold", getShatteredShape(MoldLeft?.Block ?? base.Block), out shatteredMeshLeft, this, null, 0, 0, 0);
		}
		if (ShatteredRight && shatteredMeshRight == null)
		{
			metalTexLoc = ((ContentsRight == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + ContentsRight.Collectible.LastCodePart()));
			capi?.Tesselator.TesselateShape("shatteredmold", getShatteredShape(MoldRight?.Block ?? base.Block), out shatteredMeshRight, this, null, 0, 0, 0);
		}
	}

	private Shape getShatteredShape(Block block)
	{
		tmpTextureSource = capi?.Tesselator.GetTextureSource(block);
		CompositeShape compositeShape = block.Attributes["shatteredShape"].AsObject<CompositeShape>();
		compositeShape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		return Shape.TryGet(Api, compositeShape.Base);
	}

	private void GenMeshes()
	{
		MoldMeshLeft = ObjectCacheUtil.GetOrCreate(Api, (MoldLeft?.Block ?? base.Block).Code.ToString(), delegate
		{
			ITexPositionSource textureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(MoldLeft?.Block ?? base.Block);
			((ICoreClientAPI)Api).Tesselator.TesselateShape(shapeBase: Shape.TryGet(Api, "shapes/block/clay/mold/ingot.json"), typeForLogging: (MoldLeft?.Block ?? base.Block).Code.ToString(), modeldata: out var modeldata, texSource: textureSource, meshRotationDeg: null, generalGlowLevel: 0, climateColorMapId: 0, seasonColorMapId: 0);
			return modeldata;
		});
		if (MoldRight != null)
		{
			MoldMeshRight = ObjectCacheUtil.GetOrCreate(Api, (MoldRight?.Block ?? base.Block).Code.ToString(), delegate
			{
				ITexPositionSource textureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(MoldRight?.Block ?? base.Block);
				((ICoreClientAPI)Api).Tesselator.TesselateShape(shapeBase: Shape.TryGet(Api, "shapes/block/clay/mold/ingot.json"), typeForLogging: (MoldRight?.Block ?? base.Block).Code.ToString(), modeldata: out var modeldata, texSource: textureSource, meshRotationDeg: null, generalGlowLevel: 0, climateColorMapId: 0, seasonColorMapId: 0);
				return modeldata;
			});
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		ContentsLeft = tree.GetItemstack("contentsLeft");
		FillLevelLeft = tree.GetInt("fillLevelLeft");
		if (worldForResolving != null && ContentsLeft != null)
		{
			ContentsLeft.ResolveBlockOrItem(worldForResolving);
		}
		ContentsRight = tree.GetItemstack("contentsRight");
		FillLevelRight = tree.GetInt("fillLevelRight");
		if (worldForResolving != null && ContentsRight != null)
		{
			ContentsRight.ResolveBlockOrItem(worldForResolving);
		}
		QuantityMolds = tree.GetInt("quantityMolds");
		ShatteredLeft = tree.GetBool("shatteredLeft");
		ShatteredRight = tree.GetBool("shatteredRight");
		MeshAngle = tree.GetFloat("meshAngle");
		MoldLeft = tree.GetItemstack("moldLeft");
		if (worldForResolving != null && MoldLeft == null)
		{
			MoldLeft = new ItemStack(base.Block);
			if (ShatteredLeft)
			{
				FillLevelLeft = (int)((double)FillLevelLeft * (0.699999988079071 + worldForResolving.Rand.NextDouble() * 0.10000000149011612));
			}
			if (QuantityMolds > 1 && ShatteredRight)
			{
				FillLevelRight = (int)((double)FillLevelRight * (0.699999988079071 + worldForResolving.Rand.NextDouble() * 0.10000000149011612));
			}
		}
		if (worldForResolving != null && MoldLeft != null)
		{
			MoldLeft.ResolveBlockOrItem(worldForResolving);
		}
		MoldRight = tree.GetItemstack("moldRight", (QuantityMolds > 1) ? new ItemStack(base.Block) : null);
		if (worldForResolving != null && MoldRight != null)
		{
			MoldRight.ResolveBlockOrItem(worldForResolving);
		}
		UpdateIngotRenderer();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			GenMeshes();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetItemstack("contentsLeft", ContentsLeft);
		tree.SetInt("fillLevelLeft", FillLevelLeft);
		tree.SetItemstack("contentsRight", ContentsRight);
		tree.SetInt("fillLevelRight", FillLevelRight);
		tree.SetInt("quantityMolds", QuantityMolds);
		tree.SetBool("shatteredLeft", ShatteredLeft);
		tree.SetBool("shatteredRight", ShatteredRight);
		tree.SetFloat("meshAngle", MeshAngle);
		tree.SetItemstack("moldLeft", (MoldLeft != null) ? MoldLeft : new ItemStack(base.Block));
		tree.SetItemstack("moldRight", MoldRight);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		BlockSelection blockSelection = forPlayer?.CurrentBlockSelection;
		if (blockSelection == null)
		{
			return;
		}
		SetSelectedSide(blockSelection.HitPosition);
		ItemStack selectedContents = SelectedContents;
		if (!SelectedShattered)
		{
			string text = (SelectedIsLiquid ? Lang.Get("liquid") : (SelectedIsHardened ? Lang.Get("hardened") : Lang.Get("soft")));
			string key = "material-" + selectedContents?.Collectible.Variant["metal"];
			string text2 = (Lang.HasTranslation(key) ? Lang.Get(key) : selectedContents?.GetName());
			string text3 = ((SelectedTemperature < 21f) ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)SelectedTemperature));
			string withFallback = Lang.GetWithFallback("metalmold-blockinfo-unitsofmetal", "{0}/{4} units of {1} {2} ({3})", SelectedFillLevel, text, text2, text3, RequiredUnits);
			dsc.AppendLine(((selectedContents != null) ? withFallback : Lang.GetWithFallback("metalmold-blockinfo-emptymold", "0/{0} units of metal", RequiredUnits)) + "\n");
		}
		else
		{
			ItemStack selectedStateAwareContents = GetSelectedStateAwareContents();
			if (selectedStateAwareContents != null)
			{
				dsc.AppendLine(Lang.Get("metalmold-blockinfo-shatteredmetal", selectedStateAwareContents.StackSize, selectedStateAwareContents.GetName().ToLower()) + "\n");
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ingotRenderer?.Dispose();
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		ContentsLeft?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(ContentsLeft), blockIdMapping, itemIdMapping);
		ContentsRight?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(ContentsRight), blockIdMapping, itemIdMapping);
		MoldLeft?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(MoldLeft), blockIdMapping, itemIdMapping);
		MoldRight?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(MoldRight), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack? contentsLeft = ContentsLeft;
		if (contentsLeft != null && !contentsLeft.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			ContentsLeft = null;
		}
		ItemStack? contentsRight = ContentsRight;
		if (contentsRight != null && !contentsRight.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			ContentsRight = null;
		}
		ItemStack? moldLeft = MoldLeft;
		if (moldLeft != null && !moldLeft.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			MoldLeft = null;
		}
		ItemStack? moldRight = MoldRight;
		if (moldRight != null && !moldRight.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			MoldRight = null;
		}
	}

	public void ShatterMoldSided(bool shatterRight)
	{
		Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
		(shatterRight ? MoldRight : MoldLeft)?.Block.SpawnBlockBrokenParticles(Pos);
		(shatterRight ? MoldRight : MoldLeft)?.Block.SpawnBlockBrokenParticles(Pos);
		shatterRight ? ref ShatteredRight : ref ShatteredLeft = true;
		MarkDirty(redrawOnClient: true);
	}

	public void CoolNow(float amountRel)
	{
		float num = Math.Max(0f, amountRel - 0.6f) * Math.Max(TemperatureLeft - 250f, 0f) / 5000f;
		float num2 = Math.Max(0f, amountRel - 0.6f) * Math.Max(TemperatureRight - 250f, 0f) / 5000f;
		if (Api.World.Rand.NextDouble() < (double)num)
		{
			FillLevelLeft = (int)((double)FillLevelLeft * (0.699999988079071 + Api.World.Rand.NextDouble() * 0.10000000149011612));
			ContentsLeft?.Collectible.SetTemperature(Api.World, ContentsLeft, 20f, delayCooldown: false);
			ShatterMoldSided(shatterRight: false);
		}
		else if (ContentsLeft != null)
		{
			float temperatureLeft = TemperatureLeft;
			if (temperatureLeft > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.4, null, randomizePitch: false, 16f);
			}
			ContentsLeft.Collectible.SetTemperature(Api.World, ContentsLeft, Math.Max(20f, temperatureLeft - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
		if (Api.World.Rand.NextDouble() < (double)num2)
		{
			FillLevelRight = (int)((double)FillLevelRight * (0.699999988079071 + Api.World.Rand.NextDouble() * 0.10000000149011612));
			ContentsRight?.Collectible.SetTemperature(Api.World, ContentsRight, 20f, delayCooldown: false);
			ShatterMoldSided(shatterRight: true);
		}
		else if (ContentsRight != null)
		{
			float temperatureRight = TemperatureRight;
			if (temperatureRight > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
			}
			ContentsRight.Collectible.SetTemperature(Api.World, ContentsRight, Math.Max(20f, temperatureRight - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
