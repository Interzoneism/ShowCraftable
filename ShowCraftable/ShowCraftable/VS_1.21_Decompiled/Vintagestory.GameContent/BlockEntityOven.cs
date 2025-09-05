using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityOven : BlockEntityDisplay, IHeatSource
{
	public static int BakingStageThreshold = 100;

	public const int maxBakingTemperatureAccepted = 260;

	private bool burning;

	private bool clientSidePrevBurning;

	public float prevOvenTemperature = 20f;

	public float ovenTemperature = 20f;

	private float fuelBurnTime;

	private readonly OvenItemData[] bakingData;

	private ItemStack lastRemoved;

	private int rotationDeg;

	private Random prng;

	private int syncCount;

	private ILoadedSound ambientSound;

	internal InventoryOven ovenInv;

	public virtual float maxTemperature => 300f;

	public virtual int bakeableCapacity => 4;

	public virtual int fuelitemCapacity => 6;

	private EnumOvenContentMode OvenContentMode
	{
		get
		{
			ItemSlot firstNonEmptySlot = ovenInv.FirstNonEmptySlot;
			if (firstNonEmptySlot == null)
			{
				return EnumOvenContentMode.Firewood;
			}
			BakingProperties bakingProperties = BakingProperties.ReadFrom(firstNonEmptySlot.Itemstack);
			if (bakingProperties == null)
			{
				return EnumOvenContentMode.Firewood;
			}
			if (!bakingProperties.LargeItem)
			{
				return EnumOvenContentMode.Quadrants;
			}
			return EnumOvenContentMode.SingleCenter;
		}
	}

	public override InventoryBase Inventory => ovenInv;

	public override string InventoryClassName => "oven";

	public ItemSlot FuelSlot => ovenInv[0];

	public bool HasFuel
	{
		get
		{
			ItemStack itemstack = FuelSlot.Itemstack;
			if (itemstack == null)
			{
				return false;
			}
			return itemstack.Collectible?.Attributes?.IsTrue("isClayOvenFuel") == true;
		}
	}

	public bool IsBurning => burning;

	public bool HasBakeables
	{
		get
		{
			for (int i = 0; i < bakeableCapacity; i++)
			{
				if (!ovenInv[i].Empty && (i != 0 || !HasFuel))
				{
					return true;
				}
			}
			return false;
		}
	}

	public override int DisplayedItems
	{
		get
		{
			if (OvenContentMode == EnumOvenContentMode.Quadrants)
			{
				return 4;
			}
			return 1;
		}
	}

	public BlockEntityOven()
	{
		bakingData = new OvenItemData[bakeableCapacity];
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i] = new OvenItemData();
		}
		ovenInv = new InventoryOven("oven-0", bakeableCapacity);
	}

	public override void Initialize(ICoreAPI api)
	{
		capi = api as ICoreClientAPI;
		base.Initialize(api);
		ovenInv.LateInitialize(InventoryClassName + "-" + Pos, api);
		RegisterGameTickListener(OnBurnTick, 100);
		prng = new Random(Pos.GetHashCode());
		SetRotation();
	}

	private void SetRotation()
	{
		switch (base.Block.Variant["side"])
		{
		case "south":
			rotationDeg = 270;
			break;
		case "west":
			rotationDeg = 180;
			break;
		case "east":
			rotationDeg = 0;
			break;
		default:
			rotationDeg = 90;
			break;
		}
	}

	public virtual bool OnInteract(IPlayer byPlayer, BlockSelection bs)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			if (TryTake(byPlayer))
			{
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				return true;
			}
			return false;
		}
		CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
		JsonObject attributes = collectible.Attributes;
		if (attributes != null && attributes.IsTrue("isClayOvenFuel"))
		{
			if (TryAddFuel(activeHotbarSlot))
			{
				AssetLocation assetLocation = activeHotbarSlot.Itemstack?.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return true;
			}
			return false;
		}
		if (collectible.Attributes?["bakingProperties"] == null)
		{
			CombustibleProperties combustibleProps = collectible.CombustibleProps;
			if (combustibleProps == null || combustibleProps.SmeltingType != EnumSmeltType.Bake || collectible.CombustibleProps.MeltingPoint >= 260)
			{
				if (TryTake(byPlayer))
				{
					byPlayer.InventoryManager.BroadcastHotbarSlot();
					return true;
				}
				return false;
			}
		}
		if (activeHotbarSlot.Itemstack.Equals(Api.World, lastRemoved, GlobalConstants.IgnoredStackAttributes) && !ovenInv[0].Empty)
		{
			if (TryTake(byPlayer))
			{
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				return true;
			}
		}
		else
		{
			AssetLocation assetLocation2 = activeHotbarSlot.Itemstack?.Collectible.Code;
			if (TryPut(activeHotbarSlot))
			{
				AssetLocation assetLocation3 = activeHotbarSlot.Itemstack?.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation3 != null) ? assetLocation3 : new AssetLocation("sounds/player/buildhigh"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				Api.World.Logger.Audit("{0} Put 1x{1} into Clay oven at {2}.", byPlayer.PlayerName, assetLocation2, Pos);
				return true;
			}
			if (activeHotbarSlot.Itemstack.Block?.GetBehavior<BlockBehaviorCanIgnite>() == null)
			{
				ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
				if (coreClientAPI != null && (activeHotbarSlot.Empty || !activeHotbarSlot.Itemstack.Attributes.GetBool("bakeable", defaultValue: true)))
				{
					coreClientAPI.TriggerIngameError(this, "notbakeable", Lang.Get("This item is not bakeable."));
				}
				else if (coreClientAPI != null && !activeHotbarSlot.Empty)
				{
					coreClientAPI.TriggerIngameError(this, "notbakeable", burning ? Lang.Get("Wait until the fire is out") : Lang.Get("Oven is full"));
				}
				return true;
			}
		}
		return false;
	}

	protected virtual bool TryAddFuel(ItemSlot slot)
	{
		if (IsBurning || HasBakeables)
		{
			return false;
		}
		if (FuelSlot.Empty || FuelSlot.Itemstack.StackSize < fuelitemCapacity)
		{
			int num = slot.TryPutInto(Api.World, FuelSlot);
			if (num > 0)
			{
				updateMesh(0);
				MarkDirty(redrawOnClient: true);
				lastRemoved = null;
			}
			return num > 0;
		}
		return false;
	}

	protected virtual bool TryPut(ItemSlot slot)
	{
		if (IsBurning || HasFuel)
		{
			return false;
		}
		BakingProperties bakingProperties = BakingProperties.ReadFrom(slot.Itemstack);
		if (bakingProperties == null)
		{
			return false;
		}
		if (!slot.Itemstack.Attributes.GetBool("bakeable", defaultValue: true))
		{
			return false;
		}
		if (bakingProperties.LargeItem && !ovenInv.Empty)
		{
			return false;
		}
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (ovenInv[i].Empty)
			{
				int num = slot.TryPutInto(Api.World, ovenInv[i]);
				if (num > 0)
				{
					bakingData[i] = new OvenItemData(ovenInv[i].Itemstack);
					updateMesh(i);
					MarkDirty(redrawOnClient: true);
					lastRemoved = null;
				}
				return num > 0;
			}
			if (i == 0)
			{
				BakingProperties bakingProperties2 = BakingProperties.ReadFrom(ovenInv[0].Itemstack);
				if (bakingProperties2 != null && bakingProperties2.LargeItem)
				{
					return false;
				}
			}
		}
		return false;
	}

	protected virtual bool TryTake(IPlayer byPlayer)
	{
		if (IsBurning)
		{
			return false;
		}
		for (int num = bakeableCapacity; num >= 0; num--)
		{
			if (!ovenInv[num].Empty)
			{
				ItemStack itemStack = ovenInv[num].TakeOut(1);
				lastRemoved = itemStack?.Clone();
				if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
				{
					AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
					Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/throw"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				}
				if (itemStack.StackSize > 0)
				{
					Api.World.SpawnItemEntity(itemStack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Clay oven at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
				bakingData[num].CurHeightMul = 1f;
				updateMesh(num);
				MarkDirty(redrawOnClient: true);
				return true;
			}
		}
		return false;
	}

	public virtual ItemStack[] CanAdd(ItemStack[] itemstacks)
	{
		if (IsBurning)
		{
			return null;
		}
		if (!FuelSlot.Empty)
		{
			return null;
		}
		if (ovenTemperature <= (float)(EnvironmentTemperature() + 25))
		{
			return null;
		}
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (ovenInv[i].Empty)
			{
				return itemstacks;
			}
		}
		return null;
	}

	public virtual ItemStack[] CanAddAsFuel(ItemStack[] itemstacks)
	{
		if (IsBurning)
		{
			return null;
		}
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (!ovenInv[i].Empty)
			{
				return null;
			}
		}
		if (FuelSlot.StackSize >= fuelitemCapacity)
		{
			return null;
		}
		return itemstacks;
	}

	public bool TryIgnite()
	{
		if (!CanIgnite())
		{
			return false;
		}
		burning = true;
		fuelBurnTime = 45 + FuelSlot.StackSize * 5;
		MarkDirty();
		ambientSound?.Start();
		return true;
	}

	public bool CanIgnite()
	{
		if (!FuelSlot.Empty)
		{
			return !burning;
		}
		return false;
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return Math.Max((ovenTemperature - 20f) / (maxTemperature - 20f) * 8f, 0f);
	}

	protected virtual void OnBurnTick(float dt)
	{
		dt *= 1.25f;
		if (Api is ICoreClientAPI)
		{
			return;
		}
		if (fuelBurnTime > 0f)
		{
			fuelBurnTime -= dt;
			if (fuelBurnTime <= 0f)
			{
				fuelBurnTime = 0f;
				burning = false;
				CombustibleProperties combustibleProperties = FuelSlot.Itemstack?.Collectible.CombustibleProps;
				if (combustibleProperties?.SmeltedStack == null)
				{
					FuelSlot.Itemstack = null;
					for (int i = 0; i < bakeableCapacity; i++)
					{
						bakingData[i].CurHeightMul = 1f;
					}
				}
				else
				{
					int stackSize = FuelSlot.StackSize;
					FuelSlot.Itemstack = combustibleProperties.SmeltedStack.ResolvedItemstack.Clone();
					FuelSlot.Itemstack.StackSize = stackSize * combustibleProperties.SmeltedRatio;
				}
				MarkDirty(redrawOnClient: true);
			}
		}
		if (IsBurning)
		{
			ovenTemperature = ChangeTemperature(ovenTemperature, maxTemperature, dt * (float)FuelSlot.StackSize / (float)fuelitemCapacity);
		}
		else
		{
			int num = EnvironmentTemperature();
			if (ovenTemperature > (float)num)
			{
				HeatInput(dt);
				ovenTemperature = ChangeTemperature(ovenTemperature, num, dt / 24f);
			}
		}
		if (++syncCount % 5 == 0 && (IsBurning || prevOvenTemperature != ovenTemperature || !Inventory[0].Empty || !Inventory[1].Empty || !Inventory[2].Empty || !Inventory[3].Empty))
		{
			MarkDirty();
			prevOvenTemperature = ovenTemperature;
		}
	}

	protected virtual void HeatInput(float dt)
	{
		for (int i = 0; i < bakeableCapacity; i++)
		{
			ItemStack itemstack = ovenInv[i].Itemstack;
			if (itemstack != null && HeatStack(itemstack, dt, i) >= 100f)
			{
				IncrementallyBake(dt * 1.2f, i);
			}
		}
	}

	protected virtual float HeatStack(ItemStack stack, float dt, int i)
	{
		float temp = bakingData[i].temp;
		float num = temp;
		if (temp < ovenTemperature)
		{
			float dt2 = (1f + GameMath.Clamp((ovenTemperature - temp) / 28f, 0f, 1.6f)) * dt;
			num = ChangeTemperature(temp, ovenTemperature, dt2);
			int num2 = Math.Max(stack.Collectible.CombustibleProps?.MaxTemperature ?? 0, stack.ItemAttributes?["maxTemperature"].AsInt() ?? 0);
			if (num2 > 0)
			{
				num = Math.Min(num2, num);
			}
		}
		else if (temp > ovenTemperature)
		{
			float dt3 = (1f + GameMath.Clamp((temp - ovenTemperature) / 28f, 0f, 1.6f)) * dt;
			num = ChangeTemperature(temp, ovenTemperature, dt3);
		}
		if (temp != num)
		{
			bakingData[i].temp = num;
		}
		return num;
	}

	protected virtual void IncrementallyBake(float dt, int slotIndex)
	{
		ItemSlot itemSlot = Inventory[slotIndex];
		OvenItemData ovenItemData = bakingData[slotIndex];
		float num = ovenItemData.BrowningPoint;
		if (num == 0f)
		{
			num = 160f;
		}
		float num2 = ovenItemData.temp / num;
		float num3 = ovenItemData.TimeToBake;
		if (num3 == 0f)
		{
			num3 = 1f;
		}
		float num4 = (float)GameMath.Clamp((int)num2, 1, 30) * dt / num3;
		float num5 = ovenItemData.BakedLevel;
		if (ovenItemData.temp > num)
		{
			num5 = (ovenItemData.BakedLevel += num4);
		}
		BakingProperties bakingProperties = BakingProperties.ReadFrom(itemSlot.Itemstack);
		float num6 = bakingProperties?.LevelFrom ?? 0f;
		float num7 = bakingProperties?.LevelTo ?? 1f;
		float v = bakingProperties?.StartScaleY ?? 1f;
		float v2 = bakingProperties?.EndScaleY ?? 1f;
		float t = GameMath.Clamp((num5 - num6) / (num7 - num6), 0f, 1f);
		float num8 = (float)(int)(GameMath.Mix(v, v2, t) * (float)BakingStageThreshold) / (float)BakingStageThreshold;
		bool flag = num8 != ovenItemData.CurHeightMul;
		ovenItemData.CurHeightMul = num8;
		if (num5 > num7)
		{
			float temp = ovenItemData.temp;
			string text = bakingProperties?.ResultCode;
			if (text != null)
			{
				ItemStack itemStack = null;
				if (itemSlot.Itemstack.Class == EnumItemClass.Block)
				{
					Block block = Api.World.GetBlock(new AssetLocation(text));
					if (block != null)
					{
						itemStack = new ItemStack(block);
					}
				}
				else
				{
					Item item = Api.World.GetItem(new AssetLocation(text));
					if (item != null)
					{
						itemStack = new ItemStack(item);
					}
				}
				if (itemStack != null)
				{
					ovenInv[slotIndex].Itemstack.Collectible.GetCollectibleInterface<IBakeableCallback>()?.OnBaked(ovenInv[slotIndex].Itemstack, itemStack);
					ovenInv[slotIndex].Itemstack = itemStack;
					bakingData[slotIndex] = new OvenItemData(itemStack);
					bakingData[slotIndex].temp = temp;
					flag = true;
				}
			}
			else
			{
				ItemSlot itemSlot2 = new DummySlot(null);
				if (itemSlot.Itemstack.Collectible.CanSmelt(Api.World, ovenInv, itemSlot.Itemstack, null))
				{
					itemSlot.Itemstack.Collectible.DoSmelt(Api.World, ovenInv, ovenInv[slotIndex], itemSlot2);
					if (!itemSlot2.Empty)
					{
						ovenInv[slotIndex].Itemstack = itemSlot2.Itemstack;
						bakingData[slotIndex] = new OvenItemData(itemSlot2.Itemstack);
						bakingData[slotIndex].temp = temp;
						flag = true;
					}
				}
			}
		}
		if (flag)
		{
			updateMesh(slotIndex);
			MarkDirty(redrawOnClient: true);
		}
	}

	protected virtual int EnvironmentTemperature()
	{
		return (int)Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
	}

	public virtual float ChangeTemperature(float fromTemp, float toTemp, float dt)
	{
		float num = Math.Abs(fromTemp - toTemp);
		num *= GameMath.Sqrt(num);
		dt += dt * (num / 480f);
		if (num < dt)
		{
			return toTemp;
		}
		if (fromTemp > toTemp)
		{
			dt = (0f - dt) / 2f;
		}
		if (Math.Abs(fromTemp - toTemp) < 1f)
		{
			return toTemp;
		}
		return fromTemp + dt;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		ovenInv.FromTreeAttributes(tree);
		burning = tree.GetInt("burn") > 0;
		rotationDeg = tree.GetInt("rota");
		ovenTemperature = tree.GetFloat("temp");
		fuelBurnTime = tree.GetFloat("tfuel");
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i] = OvenItemData.ReadFromTree(tree, i);
		}
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			updateMeshes();
			if (clientSidePrevBurning != IsBurning)
			{
				ToggleAmbientSounds(IsBurning);
				clientSidePrevBurning = IsBurning;
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ovenInv.ToTreeAttributes(tree);
		tree.SetInt("burn", burning ? 1 : 0);
		tree.SetInt("rota", rotationDeg);
		tree.SetFloat("temp", ovenTemperature);
		tree.SetFloat("tfuel", fuelBurnTime);
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i].WriteToTree(tree, i);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (ovenTemperature <= 25f)
		{
			sb.AppendLine(Lang.Get("Temperature: {0}", Lang.Get("Cold")));
			if (!IsBurning)
			{
				sb.AppendLine(Lang.Get("clayoven-preheat-warning"));
			}
		}
		else
		{
			sb.AppendLine(Lang.Get("Temperature: {0}°C", (int)ovenTemperature));
			if (ovenTemperature < 100f && !IsBurning)
			{
				sb.AppendLine(Lang.Get("Reheat to continue baking"));
			}
		}
		sb.AppendLine();
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (!ovenInv[i].Empty)
			{
				ItemStack itemstack = ovenInv[i].Itemstack;
				sb.Append(itemstack.GetName());
				sb.AppendLine(" (" + Lang.Get("{0}°C", (int)bakingData[i].temp) + ")");
			}
		}
	}

	public virtual void ToggleAmbientSounds(bool on)
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return;
		}
		if (on)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/environment/fireplace.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.1f, 0.5f),
					DisposeOnFinish = false,
					Volume = 0.66f
				});
				ambientSound.Start();
			}
		}
		else
		{
			ambientSound.Stop();
			ambientSound.Dispose();
			ambientSound = null;
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[DisplayedItems][];
		Vec3f[] array2 = new Vec3f[DisplayedItems];
		switch (OvenContentMode)
		{
		case EnumOvenContentMode.Firewood:
			array2[0] = new Vec3f();
			break;
		case EnumOvenContentMode.Quadrants:
			array2[0] = new Vec3f(-0.125f, 0.0625f, -5f / 32f);
			array2[1] = new Vec3f(-0.125f, 0.0625f, 5f / 32f);
			array2[2] = new Vec3f(0.1875f, 0.0625f, -5f / 32f);
			array2[3] = new Vec3f(0.1875f, 0.0625f, 5f / 32f);
			break;
		case EnumOvenContentMode.SingleCenter:
			array2[0] = new Vec3f(0f, 0.0625f, 0f);
			break;
		}
		for (int i = 0; i < array.Length; i++)
		{
			Vec3f vec3f = array2[i];
			float y = ((OvenContentMode == EnumOvenContentMode.Firewood) ? 0.9f : bakingData[i].CurHeightMul);
			array[i] = new Matrixf().Translate(vec3f.X, vec3f.Y, vec3f.Z).Translate(0.5f, 0f, 0.5f).RotateYDeg(rotationDeg + ((OvenContentMode == EnumOvenContentMode.Firewood) ? 270 : 0))
				.Scale(0.9f, y, 0.9f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array;
	}

	protected override string getMeshCacheKey(ItemStack stack)
	{
		string text = "";
		for (int i = 0; i < bakingData.Length; i++)
		{
			if (Inventory[i].Itemstack == stack)
			{
				text = "-" + bakingData[i].CurHeightMul;
				break;
			}
		}
		return ((OvenContentMode == EnumOvenContentMode.Firewood) ? (stack.StackSize + "x") : "") + base.getMeshCacheKey(stack) + text;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		tfMatrices = genTransformationMatrices();
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	protected override MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		if (OvenContentMode == EnumOvenContentMode.Firewood)
		{
			MeshData modeldata = getMesh(stack);
			if (modeldata != null)
			{
				return modeldata;
			}
			AssetLocation shapePath = AssetLocation.Create(FuelSlot?.Itemstack?.Collectible?.Attributes?["ovenFuelShape"].AsString() ?? base.Block.Attributes["ovenFuelShape"].AsString(), base.Block.Code.Domain).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			nowTesselatingShape = Shape.TryGet(capi, shapePath);
			nowTesselatingObj = stack.Collectible;
			if (nowTesselatingShape == null)
			{
				capi.Logger.Error(string.Concat("Stacking model shape for collectible ", stack.Collectible.Code, " not found. Block will be invisible!"));
				return null;
			}
			capi.Tesselator.TesselateShape("ovenFuelShape", nowTesselatingShape, out modeldata, this, null, 0, 0, 0, stack.StackSize);
			string meshCacheKey = getMeshCacheKey(stack);
			base.MeshCache[meshCacheKey] = modeldata;
			return modeldata;
		}
		return base.getOrCreateMesh(stack, index);
	}

	public virtual void RenderParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking, AdvancedParticleProperties[] particles)
	{
		if (fuelBurnTime < 3f)
		{
			return;
		}
		int stackSize = FuelSlot.StackSize;
		bool flag = stackSize > 3;
		double[] array = new double[4];
		float[] array2 = new float[4];
		for (int i = 0; i < particles.Length; i++)
		{
			if ((i >= 12 && (float)prng.Next(0, 90) > fuelBurnTime) || (i >= 8 && i < 12 && (float)prng.Next(0, 12) > fuelBurnTime) || (i >= 4 && i < 4 && prng.Next(0, 6) == 0))
			{
				continue;
			}
			if (i >= 4 && stackSize < 3)
			{
				bool flag2 = rotationDeg >= 180;
				if ((!flag2 && array2[i % 2] > (float)stackSize * 0.2f + 0.14f) || (flag2 && array2[i % 2] < (float)(3 - stackSize) * 0.2f + 0.14f))
				{
					continue;
				}
			}
			AdvancedParticleProperties advancedParticleProperties = particles[i];
			advancedParticleProperties.WindAffectednesAtPos = 0f;
			advancedParticleProperties.basePos.X = pos.X;
			advancedParticleProperties.basePos.Y = (float)pos.InternalY + (flag ? (3f / 32f) : (1f / 32f));
			advancedParticleProperties.basePos.Z = pos.Z;
			if (i >= 4)
			{
				bool flag3 = rotationDeg % 180 > 0;
				if (flag)
				{
					flag3 = !flag3;
				}
				advancedParticleProperties.basePos.Z += (flag3 ? array[i % 2] : ((double)array2[i % 2]));
				advancedParticleProperties.basePos.X += (flag3 ? ((double)array2[i % 2]) : array[i % 2]);
				advancedParticleProperties.basePos.Y += (float)(flag ? 4 : 3) / 32f;
				switch (rotationDeg)
				{
				case 0:
					advancedParticleProperties.basePos.X -= (flag ? 0.08f : 0.12f);
					break;
				case 180:
					advancedParticleProperties.basePos.X += (flag ? 0.08f : 0.12f);
					break;
				case 90:
					advancedParticleProperties.basePos.Z += (flag ? 0.08f : 0.12f);
					break;
				default:
					advancedParticleProperties.basePos.Z -= (flag ? 0.08f : 0.12f);
					break;
				}
			}
			else
			{
				array[i] = prng.NextDouble() * 0.4000000059604645 + 0.33000001311302185;
				array2[i] = 0.26f + (float)prng.Next(0, 3) * 0.2f + (float)prng.NextDouble() * 0.08f;
			}
			manager.Spawn(advancedParticleProperties);
		}
	}
}
