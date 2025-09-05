using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityToolMold : BlockEntity, ILiquidMetalSink, ITemperatureSensitive, ITexPositionSource, IRotatable
{
	protected ToolMoldRenderer renderer;

	public MeshData MoldMesh;

	protected Cuboidf[] fillQuadsByLevel;

	protected int requiredUnits = 100;

	protected float fillHeight = 1f;

	protected bool breaksWhenFilled;

	public ItemStack MetalContent;

	public int FillLevel;

	public bool FillSide;

	public bool Shattered;

	private ICoreClientAPI capi;

	public float MeshAngle;

	private bool hasMeshAngle = true;

	private ITexPositionSource tmpTextureSource;

	private AssetLocation metalTexLoc;

	private MeshData shatteredMesh;

	public float Temperature => MetalContent?.Collectible.GetTemperature(Api.World, MetalContent) ?? 0f;

	public bool IsHardened => Temperature < 0.3f * MetalContent?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(MetalContent));

	public bool IsLiquid => Temperature > 0.8f * MetalContent?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(MetalContent));

	public bool IsFull => FillLevel >= requiredUnits;

	public bool CanReceiveAny
	{
		get
		{
			if (!Shattered)
			{
				if (!(base.Block.Variant["materialtype"] == "fired"))
				{
					return base.Block.Code.Path.Contains("burned");
				}
				return true;
			}
			return false;
		}
	}

	public bool IsHot => Temperature >= 200f;

	public bool BreaksWhenFilled => breaksWhenFilled;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
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
		if (api is ICoreServerAPI sapi)
		{
			OnLoadWithoutMeshAngle(sapi);
		}
		if (base.Block == null || base.Block.Code == null || base.Block.Attributes == null)
		{
			return;
		}
		fillHeight = base.Block.Attributes["fillHeight"].AsFloat(1f);
		requiredUnits = base.Block.Attributes["requiredUnits"].AsInt(100);
		breaksWhenFilled = base.Block.Attributes["breaksWhenFilled"].AsBool();
		if (base.Block.Attributes["fillQuadsByLevel"].Exists)
		{
			fillQuadsByLevel = base.Block.Attributes["fillQuadsByLevel"].AsObject<Cuboidf[]>();
		}
		if (fillQuadsByLevel == null)
		{
			fillQuadsByLevel = new Cuboidf[1]
			{
				new Cuboidf(2f, 0f, 2f, 14f, 0f, 14f)
			};
		}
		capi = api as ICoreClientAPI;
		if (capi != null && !Shattered)
		{
			capi.Event.RegisterRenderer(renderer = new ToolMoldRenderer(this, capi, fillQuadsByLevel), EnumRenderStage.Opaque, "toolmoldrenderer");
			UpdateRenderer();
			if (MoldMesh == null)
			{
				GenMeshes();
			}
		}
		if (!Shattered)
		{
			RegisterGameTickListener(OnGameTick, 50);
		}
	}

	protected virtual void OnLoadWithoutMeshAngle(ICoreServerAPI sapi)
	{
		if (hasMeshAngle || (base.Block.Code.Domain != "game" && base.Block.FirstCodePart() != "toolmold"))
		{
			return;
		}
		string text = base.Block.Code.SecondCodePart();
		AssetLocation assetLocation = null;
		if (!(text == "burned"))
		{
			assetLocation = ((!(text != "blue")) ? base.Block.Code : new AssetLocation("toolmold-blue-" + base.Block.CodeEndWithoutParts(2)));
		}
		else
		{
			text = base.Block.LastCodePart();
			assetLocation = new AssetLocation(base.Block.Code.ToShortString());
			switch (text)
			{
			case "east":
			case "west":
			case "south":
				assetLocation.WithoutPathAppendix("-" + text);
				break;
			}
			assetLocation.WithPathAppendixOnce("-north");
		}
		int num = sapi.World.BlockAccessor.GetBlock(assetLocation)?.BlockId ?? 0;
		if (num != 0)
		{
			sapi.World.BlockAccessor.ExchangeBlock(num, Pos);
			switch (text)
			{
			case "gray":
			case "east":
				MeshAngle = -(float)Math.PI / 2f;
				break;
			case "blue":
			case "north":
				MeshAngle = 0f;
				break;
			case "west":
			case "brown":
				MeshAngle = (float)Math.PI / 2f;
				break;
			case "south":
			case "tan":
				MeshAngle = (float)Math.PI;
				break;
			}
		}
		hasMeshAngle = true;
	}

	private void OnGameTick(float dt)
	{
		if (renderer != null)
		{
			renderer.Level = (float)FillLevel * fillHeight / (float)requiredUnits;
		}
		if (MetalContent != null && renderer != null)
		{
			renderer.stack = MetalContent;
			renderer.Temperature = Math.Min(1300f, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
		}
	}

	public bool CanReceive(ItemStack metal)
	{
		if (MetalContent == null || (MetalContent.Collectible.Equals(MetalContent, metal, GlobalConstants.IgnoredStackAttributes) && !IsFull))
		{
			ItemStack[] moldedStacks = GetMoldedStacks(metal);
			if (moldedStacks != null && moldedStacks.Length != 0)
			{
				return !Shattered;
			}
		}
		return false;
	}

	public void BeginFill(Vec3d hitPosition)
	{
		FillSide = hitPosition.X >= 0.5;
	}

	public bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
	{
		if (Shattered)
		{
			return false;
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (byPlayer.Entity.Controls.HandUse != EnumHandInteract.None)
			{
				return false;
			}
			bool flag = TryTakeContents(byPlayer);
			if (!flag && FillLevel == 0)
			{
				ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
				if (itemstack != null)
				{
					CollectibleObject collectible = itemstack.Collectible;
					if (!(collectible is BlockToolMold) && !(collectible is BlockIngotMold))
					{
						return flag;
					}
				}
				ItemStack itemStack = new ItemStack(base.Block);
				if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
				{
					Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Tool mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
				Api.World.BlockAccessor.SetBlock(0, Pos);
				if (base.Block.Sounds?.Place != null)
				{
					Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.5, byPlayer, randomizePitch: false);
				}
				flag = true;
			}
			return flag;
		}
		return false;
	}

	protected virtual bool TryTakeContents(IPlayer byPlayer)
	{
		if (Shattered || MetalContent == null || FillLevel == 0)
		{
			return false;
		}
		if (BreaksWhenFilled)
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "breakswhenfilledrightclicked", Lang.Get("toolmold-breakswhenfilled-error"));
			return false;
		}
		if (Api is ICoreServerAPI)
		{
			MarkDirty();
		}
		if (IsFull && IsHardened)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos, -0.5, byPlayer, randomizePitch: false);
			if (Api is ICoreServerAPI)
			{
				ItemStack[] stateAwareMoldedStacks = GetStateAwareMoldedStacks();
				if (stateAwareMoldedStacks != null)
				{
					ItemStack[] array = stateAwareMoldedStacks;
					foreach (ItemStack itemStack in array)
					{
						int stackSize = itemStack.StackSize;
						if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
						{
							Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
						}
						Api.World.Logger.Audit("{0} Took {1}x{2} from Tool mold at {3}.", byPlayer.PlayerName, stackSize, itemStack.Collectible.Code, Pos);
					}
					MetalContent = null;
					FillLevel = 0;
				}
			}
			UpdateRenderer();
			return true;
		}
		return false;
	}

	public void UpdateRenderer()
	{
		if (renderer == null)
		{
			return;
		}
		if (Shattered && renderer != null)
		{
			(Api as ICoreClientAPI).Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
			renderer = null;
			return;
		}
		renderer.Level = (float)FillLevel * fillHeight / (float)requiredUnits;
		if (MetalContent?.Collectible != null)
		{
			renderer.TextureName = new AssetLocation("block/metal/ingot/" + MetalContent.Collectible.LastCodePart() + ".png");
		}
		else
		{
			renderer.TextureName = null;
		}
	}

	public void ReceiveLiquidMetal(ItemStack metal, ref int amount, float temperature)
	{
		if (!IsFull && (MetalContent == null || metal.Collectible.Equals(MetalContent, metal, GlobalConstants.IgnoredStackAttributes)))
		{
			if (MetalContent == null)
			{
				MetalContent = metal.Clone();
				MetalContent.ResolveBlockOrItem(Api.World);
				MetalContent.Collectible.SetTemperature(Api.World, MetalContent, temperature, delayCooldown: false);
				MetalContent.StackSize = 1;
				(MetalContent.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 300f);
			}
			else
			{
				MetalContent.Collectible.SetTemperature(Api.World, MetalContent, temperature, delayCooldown: false);
			}
			int num = Math.Min(amount, requiredUnits - FillLevel);
			FillLevel += num;
			amount -= num;
			UpdateRenderer();
		}
	}

	public void OnPourOver()
	{
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
		renderer = null;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		renderer = null;
	}

	public ItemStack[] GetStateAwareMold()
	{
		List<ItemStack> list = new List<ItemStack>();
		if (!Shattered)
		{
			if (!BreaksWhenFilled || FillLevel <= 0)
			{
				list.Add(new ItemStack(base.Block));
			}
		}
		else
		{
			BlockDropItemStack[] array = base.Block.Attributes?["shatteredDrops"].AsObject<BlockDropItemStack[]>();
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Resolve(Api.World, "shatteredDrops[" + i + "] for", base.Block.Code);
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
		if (MetalContent?.Collectible != null && IsHardened)
		{
			if (Shattered)
			{
				JsonItemStack jsonItemStack = MetalContent.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
				if (jsonItemStack != null)
				{
					jsonItemStack.Resolve(Api.World, "shatteredStack for" + MetalContent.Collectible.Code);
					ItemStack resolvedItemstack = jsonItemStack.ResolvedItemstack;
					if (resolvedItemstack != null)
					{
						resolvedItemstack.StackSize = (int)((float)FillLevel / 5f);
						return new ItemStack[1] { resolvedItemstack };
					}
				}
			}
			if (IsFull)
			{
				return GetMoldedStacks(MetalContent);
			}
		}
		return null;
	}

	public ItemStack GetChiseledStack()
	{
		if (MetalContent != null && FillLevel > 0 && !Shattered && IsHardened)
		{
			JsonItemStack obj = MetalContent.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
			obj?.Resolve(Api.World, "chiseledStack for" + MetalContent.Collectible.Code);
			ItemStack itemStack = obj?.ResolvedItemstack;
			if (itemStack != null)
			{
				itemStack.StackSize = (int)((float)FillLevel / 5f);
				return itemStack;
			}
		}
		return null;
	}

	public ItemStack[] GetMoldedStacks(ItemStack fromMetal)
	{
		try
		{
			if (base.Block.Attributes["drop"].Exists)
			{
				JsonItemStack jsonItemStack = base.Block.Attributes["drop"].AsObject<JsonItemStack>(null, base.Block.Code.Domain);
				if (jsonItemStack == null)
				{
					return null;
				}
				ItemStack itemStack = stackFromCode(jsonItemStack, fromMetal);
				if (itemStack == null)
				{
					return Array.Empty<ItemStack>();
				}
				if (MetalContent != null)
				{
					itemStack.Collectible.SetTemperature(Api.World, itemStack, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
				}
				return new ItemStack[1] { itemStack };
			}
			JsonItemStack[] array = base.Block.Attributes["drops"].AsObject<JsonItemStack[]>(null, base.Block.Code.Domain);
			List<ItemStack> list = new List<ItemStack>();
			JsonItemStack[] array2 = array;
			foreach (JsonItemStack jstack in array2)
			{
				ItemStack itemStack2 = stackFromCode(jstack, fromMetal);
				if (MetalContent != null)
				{
					itemStack2.Collectible.SetTemperature(Api.World, itemStack2, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
				}
				if (itemStack2 != null)
				{
					list.Add(itemStack2);
				}
			}
			return list.ToArray();
		}
		catch (JsonReaderException)
		{
			Api.World.Logger.Error("Failed getting molded stacks from tool mold of block {0}, probably unable to parse drop or drops attribute", base.Block.Code);
			throw;
		}
	}

	public ItemStack stackFromCode(JsonItemStack jstack, ItemStack fromMetal)
	{
		string newValue = fromMetal.Collectible.LastCodePart();
		jstack.Code.Path = jstack.Code.Path.Replace("{metal}", newValue);
		jstack.Resolve(Api.World, "tool mold drop for " + base.Block.Code);
		return jstack.ResolvedItemstack;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
	{
		base.FromTreeAttributes(tree, worldForResolve);
		MetalContent = tree.GetItemstack("contents");
		FillLevel = tree.GetInt("fillLevel");
		Shattered = tree.GetBool("shattered");
		if (worldForResolve != null && MetalContent != null)
		{
			MetalContent.ResolveBlockOrItem(worldForResolve);
		}
		hasMeshAngle = tree.HasAttribute("meshAngle");
		MeshAngle = tree.GetFloat("meshAngle");
		UpdateRenderer();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetItemstack("contents", MetalContent);
		tree.SetInt("fillLevel", FillLevel);
		tree.SetBool("shattered", Shattered);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (!Shattered)
		{
			string text = (IsLiquid ? Lang.Get("liquid") : (IsHardened ? Lang.Get("hardened") : Lang.Get("soft")));
			string key = "material-" + MetalContent?.Collectible.Variant["metal"];
			string text2 = (Lang.HasTranslation(key) ? Lang.Get(key) : MetalContent?.GetName());
			string text3 = ((Temperature < 21f) ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)Temperature));
			string withFallback = Lang.GetWithFallback("metalmold-blockinfo-unitsofmetal", "{0}/{4} units of {1} {2} ({3})", FillLevel, text, text2, text3, requiredUnits);
			dsc.AppendLine(((MetalContent != null) ? withFallback : Lang.GetWithFallback("metalmold-blockinfo-emptymold", "0/{0} units of metal", requiredUnits)) + "\n");
		}
		else
		{
			ItemStack[] stateAwareMoldedStacks = GetStateAwareMoldedStacks();
			ItemStack itemStack = ((stateAwareMoldedStacks != null) ? stateAwareMoldedStacks[0] : null);
			if (itemStack != null)
			{
				dsc.AppendLine(Lang.Get("metalmold-blockinfo-shatteredmetal", itemStack.StackSize, itemStack.GetName().ToLower()) + "\n");
			}
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		MetalContent?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(MetalContent), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack metalContent = MetalContent;
		if (metalContent != null)
		{
			metalContent.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve);
			if (0 == 0)
			{
				goto IL_0020;
			}
		}
		MetalContent = null;
		goto IL_0020;
		IL_0020:
		ITreeAttribute obj = MetalContent?.Attributes["temperature"] as ITreeAttribute;
		if (obj != null && obj.HasAttribute("temperatureLastUpdate"))
		{
			((ITreeAttribute)MetalContent.Attributes["temperature"]).SetDouble("temperatureLastUpdate", worldForResolve.Calendar.TotalHours);
		}
	}

	public void ShatterMold()
	{
		Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
		Shattered = true;
		base.Block.SpawnBlockBrokenParticles(Pos);
		base.Block.SpawnBlockBrokenParticles(Pos);
		MarkDirty(redrawOnClient: true);
	}

	public void CoolNow(float amountRel)
	{
		float num = Math.Max(0f, amountRel - 0.6f) * Math.Max(Temperature - 250f, 0f) / 5000f;
		if (Api.World.Rand.NextDouble() < (double)num)
		{
			ShatterMold();
			MetalContent.Collectible.SetTemperature(Api.World, MetalContent, 20f, delayCooldown: false);
			FillLevel = (int)((double)FillLevel * (0.699999988079071 + Api.World.Rand.NextDouble() * 0.10000000149011612));
		}
		else if (MetalContent != null)
		{
			float temperature = Temperature;
			if (temperature > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
			}
			MetalContent.Collectible.SetTemperature(Api.World, MetalContent, Math.Max(20f, temperature - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (Shattered)
		{
			EnsureShatteredMeshesLoaded();
		}
		float[] array = Mat4f.Create();
		Mat4f.Translate(array, array, 0.5f, 0f, 0.5f);
		Mat4f.RotateY(array, array, MeshAngle);
		Mat4f.Translate(array, array, -0.5f, -0f, -0.5f);
		mesher.AddMeshData(Shattered ? shatteredMesh : MoldMesh, array);
		return true;
	}

	private void EnsureShatteredMeshesLoaded()
	{
		if (Shattered && shatteredMesh == null)
		{
			metalTexLoc = ((MetalContent == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + MetalContent.Collectible.LastCodePart()));
			capi.Tesselator.TesselateShape("shatteredmold", getShatteredShape(base.Block), out shatteredMesh, this, null, 0, 0, 0);
		}
	}

	private Shape getShatteredShape(Block block)
	{
		tmpTextureSource = capi.Tesselator.GetTextureSource(block);
		CompositeShape compositeShape = block.Attributes["shatteredShape"].AsObject<CompositeShape>();
		compositeShape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		return Shape.TryGet(Api, compositeShape.Base);
	}

	private void GenMeshes()
	{
		MoldMesh = ObjectCacheUtil.GetOrCreate(Api, base.Block.Code.ToString(), delegate
		{
			CompositeShape shape = base.Block.Shape;
			ITexPositionSource textureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(base.Block);
			((ICoreClientAPI)Api).Tesselator.TesselateShape(shapeBase: Shape.TryGet(Api, base.Block.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")), typeForLogging: base.Block.Code.ToString(), modeldata: out var modeldata, texSource: textureSource, meshRotationDeg: new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), generalGlowLevel: 0, climateColorMapId: 0, seasonColorMapId: 0);
			return modeldata;
		});
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
