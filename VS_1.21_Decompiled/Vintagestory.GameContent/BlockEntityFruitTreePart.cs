using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockEntityFruitTreePart : BlockEntity, ITexPositionSource
{
	public EnumFoliageState FoliageState = EnumFoliageState.Plain;

	protected ICoreClientAPI capi;

	protected MeshData sticksMesh;

	protected MeshData leavesMesh;

	public int[] LeafParticlesColor;

	public int[] BlossomParticlesColor;

	public string TreeType;

	public int Height;

	public Vec3i RootOff;

	protected bool listenerOk;

	protected Shape nowTesselatingShape;

	public int fruitingSide;

	protected string foliageDictCacheKey;

	protected BlockFruitTreeFoliage blockFoliage;

	public BlockFruitTreeBranch blockBranch;

	public BlockFacing GrowthDir = BlockFacing.UP;

	public EnumTreePartType PartType = EnumTreePartType.Cutting;

	public AssetLocation harvestingSound;

	protected bool harvested;

	private long listenerId;

	private FruitTreeRootBH rootBh;

	public EnumFruitTreeState FruitTreeState
	{
		get
		{
			if (rootBh == null)
			{
				return EnumFruitTreeState.Empty;
			}
			if (rootBh.propsByType.TryGetValue(TreeType, out var value))
			{
				return value.State;
			}
			return EnumFruitTreeState.Empty;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public double Progress => rootBh.GetCurrentStateProgress(TreeType);

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			IDictionary<string, CompositeTexture> textures = base.Block.Textures;
			AssetLocation value = null;
			if ((this is BlockEntityFruitTreeBranch || this is BlockEntityFruitTreeFoliage) && FoliageState == EnumFoliageState.Dead && (textureCode == "bark" || textureCode == "treetrunk"))
			{
				textureCode = "deadtree";
			}
			DynFoliageProperties value2 = null;
			blockFoliage.foliageProps?.TryGetValue(TreeType, out value2);
			if (value2 != null)
			{
				string key = textureCode + "-" + FoliageUtil.FoliageStates[(int)FoliageState];
				TextureAtlasPosition orLoadTexture = value2.GetOrLoadTexture(capi, key);
				if (orLoadTexture != null)
				{
					return orLoadTexture;
				}
				orLoadTexture = value2.GetOrLoadTexture(capi, textureCode);
				if (orLoadTexture != null)
				{
					return orLoadTexture;
				}
			}
			if (textures.TryGetValue(textureCode, out var value3))
			{
				value = value3.Baked.BakedName;
			}
			if (value == null && textures.TryGetValue("all", out value3))
			{
				value = value3.Baked.BakedName;
			}
			if (value == null)
			{
				nowTesselatingShape?.Textures.TryGetValue(textureCode, out value);
			}
			if (value == null)
			{
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			return getOrCreateTexPos(value);
		}
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
		if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
		{
			capi.World.Logger.Warning(string.Concat("For render in fruit tree block ", base.Block.Code, ", defined texture {1}, no such texture found."), texturePath);
			return capi.BlockTextureAtlas.UnknownTexturePosition;
		}
		return texPos;
	}

	public abstract void GenMesh();

	public virtual bool GenFoliageMesh(bool withSticks, out MeshData foliageMesh, out MeshData sticksMesh)
	{
		foliageMesh = null;
		sticksMesh = null;
		ICoreAPI api = Api;
		if ((api != null && api.Side == EnumAppSide.Server) || TreeType == null || TreeType == "" || blockFoliage?.foliageProps == null)
		{
			return false;
		}
		DynFoliageProperties foliageProps = blockFoliage.foliageProps[TreeType];
		LeafParticlesColor = capi.BlockTextureAtlas.GetRandomColors(getOrCreateTexPos(foliageProps.LeafParticlesTexture.Base));
		BlossomParticlesColor = capi.BlockTextureAtlas.GetRandomColors(getOrCreateTexPos(foliageProps.BlossomParticlesTexture.Base));
		Dictionary<int, MeshData[]> orCreate = ObjectCacheUtil.GetOrCreate(Api, foliageDictCacheKey, () => new Dictionary<int, MeshData[]>());
		int hashCodeLeaves = getHashCodeLeaves();
		if (orCreate.TryGetValue(hashCodeLeaves, out var value))
		{
			sticksMesh = value[0];
			foliageMesh = value[1];
			return true;
		}
		value = new MeshData[2];
		string key = "foliage-ver";
		BlockFacing growthDir = GrowthDir;
		if (growthDir != null && growthDir.IsHorizontal)
		{
			key = "foliage-hor-" + GrowthDir?.Code[0];
		}
		if (blockBranch?.Shapes == null || !blockBranch.Shapes.TryGetValue(key, out var value2))
		{
			return false;
		}
		nowTesselatingShape = value2.Shape;
		List<string> list = new List<string>();
		bool flag = false;
		FruitTreeRootBH fruitTreeRootBH = rootBh;
		if (fruitTreeRootBH != null && fruitTreeRootBH.propsByType.TryGetValue(TreeType, out var value3))
		{
			flag = value3.CycleType == EnumTreeCycleType.Evergreen;
		}
		if (withSticks)
		{
			list.Add("sticks/*");
			capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out value[0], this, new Vec3f(value2.CShape.rotateX, value2.CShape.rotateY, value2.CShape.rotateZ), 0, 0, 0, null, list.ToArray());
		}
		list.Clear();
		if (FoliageState == EnumFoliageState.Flowering)
		{
			list.Add("blossom/*");
		}
		if (FoliageState != EnumFoliageState.Dead && FoliageState != EnumFoliageState.DormantNoLeaves && (FoliageState != EnumFoliageState.Flowering || flag))
		{
			nowTesselatingShape.WalkElements("leaves/*", delegate(ShapeElement elem)
			{
				elem.SeasonColorMap = foliageProps.SeasonColorMap;
				elem.ClimateColorMap = foliageProps.ClimateColorMap;
			});
			list.Add("leaves/*");
		}
		float num = (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3) * 22.5f - 22.5f;
		capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out value[1], this, new Vec3f(value2.CShape.rotateX, value2.CShape.rotateY + num, value2.CShape.rotateZ), 0, 0, 0, null, list.ToArray());
		sticksMesh = value[0];
		foliageMesh = value[1];
		if (FoliageState == EnumFoliageState.Fruiting || FoliageState == EnumFoliageState.Ripe)
		{
			string text = "fruit-" + TreeType;
			if ((FoliageState != EnumFoliageState.Ripe || !blockBranch.Shapes.TryGetValue(text + "-ripe", out var value4)) && !blockBranch.Shapes.TryGetValue(text, out value4))
			{
				return false;
			}
			nowTesselatingShape = value4.Shape;
			List<string> list2 = new List<string>();
			for (int num2 = 0; num2 < 4; num2++)
			{
				char c = BlockFacing.HORIZONTALS[num2].Code[0];
				if ((fruitingSide & (1 << num2)) > 0)
				{
					list2.Add("fruits-" + c + "/*");
				}
			}
			GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3);
			capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out var modeldata, this, new Vec3f(value4.CShape.rotateX, value4.CShape.rotateY, value4.CShape.rotateZ), 0, 0, 0, null, list2.ToArray());
			foliageMesh.AddMeshData(modeldata);
		}
		orCreate[hashCodeLeaves] = value;
		return true;
	}

	protected virtual int getHashCodeLeaves()
	{
		return (GrowthDir.Code[0] + "-" + TreeType + "-" + FoliageState.ToString() + "-" + fruitingSide + "-" + ((float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3) * 22.5f - 22.5f)).GetHashCode();
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		foliageDictCacheKey = "fruitTreeFoliageMeshes" + base.Block.Code.ToShortString();
		capi = api as ICoreClientAPI;
		listenerId = RegisterGameTickListener(trySetup, 1000);
		if (api.Side == EnumAppSide.Client)
		{
			string text = base.Block.Attributes["harvestingSound"].AsString("sounds/block/plant");
			if (text != null)
			{
				harvestingSound = AssetLocation.Create(text, base.Block.Code.Domain);
			}
			GenMesh();
		}
	}

	private void trySetup(float dt)
	{
		if (!(RootOff != null) || RootOff.IsZero || Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.AddCopy(RootOff)) != null)
		{
			getRootBhSetupListener();
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
		}
	}

	protected bool getRootBhSetupListener()
	{
		if (RootOff == null || RootOff.IsZero)
		{
			rootBh = GetBehavior<FruitTreeRootBH>();
		}
		else
		{
			rootBh = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		}
		if (TreeType == null)
		{
			Api.World.Logger.Error("Coding error. Fruit tree without fruit tree type @" + Pos);
			return false;
		}
		if (rootBh != null && rootBh.propsByType.TryGetValue(TreeType, out var value))
		{
			switch (value.State)
			{
			case EnumFruitTreeState.EnterDormancy:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Dormant:
				FoliageState = EnumFoliageState.DormantNoLeaves;
				harvested = false;
				break;
			case EnumFruitTreeState.DormantVernalized:
				FoliageState = EnumFoliageState.DormantNoLeaves;
				harvested = false;
				break;
			case EnumFruitTreeState.Flowering:
				FoliageState = EnumFoliageState.Flowering;
				harvested = false;
				break;
			case EnumFruitTreeState.Fruiting:
				FoliageState = EnumFoliageState.Fruiting;
				harvested = false;
				break;
			case EnumFruitTreeState.Ripe:
				FoliageState = (harvested ? EnumFoliageState.Plain : EnumFoliageState.Ripe);
				break;
			case EnumFruitTreeState.Empty:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Young:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Dead:
				FoliageState = EnumFoliageState.Dead;
				break;
			}
			if (Api.Side == EnumAppSide.Server)
			{
				rootBh.propsByType[TreeType].OnFruitingStateChange += RootBh_OnFruitingStateChange;
			}
			listenerOk = true;
			return true;
		}
		return false;
	}

	protected void RootBh_OnFruitingStateChange(EnumFruitTreeState nowState)
	{
		switch (nowState)
		{
		case EnumFruitTreeState.EnterDormancy:
			FoliageState = EnumFoliageState.Plain;
			harvested = false;
			break;
		case EnumFruitTreeState.Dormant:
			FoliageState = EnumFoliageState.DormantNoLeaves;
			harvested = false;
			break;
		case EnumFruitTreeState.DormantVernalized:
			FoliageState = EnumFoliageState.DormantNoLeaves;
			harvested = false;
			break;
		case EnumFruitTreeState.Flowering:
			FoliageState = EnumFoliageState.Flowering;
			harvested = false;
			break;
		case EnumFruitTreeState.Fruiting:
			FoliageState = EnumFoliageState.Fruiting;
			harvested = false;
			break;
		case EnumFruitTreeState.Ripe:
			if (!harvested)
			{
				FoliageState = EnumFoliageState.Ripe;
			}
			break;
		case EnumFruitTreeState.Empty:
			FoliageState = EnumFoliageState.Plain;
			break;
		case EnumFruitTreeState.Young:
			FoliageState = EnumFoliageState.Plain;
			break;
		case EnumFruitTreeState.Dead:
			FoliageState = EnumFoliageState.Dead;
			break;
		}
		calcFruitingSide();
		MarkDirty();
	}

	protected void calcFruitingSide()
	{
		fruitingSide = 0;
		for (int i = 0; i < 4; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(Pos);
			if (Api.World.BlockAccessor.GetBlock(Pos).Id == 0)
			{
				fruitingSide |= 1 << i;
			}
		}
		Pos.East();
	}

	public void OnGrown()
	{
		if (!listenerOk)
		{
			getRootBhSetupListener();
		}
		GenMesh();
		calcFruitingSide();
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (FoliageState == EnumFoliageState.Ripe && PartType != EnumTreePartType.Stem)
		{
			Api.World.PlaySoundAt(harvestingSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
			return true;
		}
		return false;
	}

	public bool OnBlockInteractStep(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (PartType == EnumTreePartType.Stem)
		{
			return false;
		}
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
		if (Api.World.Rand.NextDouble() < 0.1)
		{
			Api.World.PlaySoundAt(harvestingSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
		}
		if (FoliageState == EnumFoliageState.Ripe)
		{
			return (double)secondsUsed < 1.3;
		}
		return false;
	}

	public void OnBlockInteractStop(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!((double)secondsUsed > 1.1) || FoliageState != EnumFoliageState.Ripe)
		{
			return;
		}
		FoliageState = EnumFoliageState.Plain;
		MarkDirty(redrawOnClient: true);
		harvested = true;
		AssetLocation blockCode = AssetLocation.Create(base.Block.Attributes["branchBlock"].AsString(), base.Block.Code.Domain);
		BlockDropItemStack[] fruitStacks = (Api.World.GetBlock(blockCode) as BlockFruitTreeBranch).TypeProps[TreeType].FruitStacks;
		foreach (BlockDropItemStack blockDropItemStack in fruitStacks)
		{
			ItemStack nextItemStack = blockDropItemStack.GetNextItemStack();
			if (nextItemStack != null)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(nextItemStack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(nextItemStack, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
				}
				if (blockDropItemStack.LastDrop)
				{
					break;
				}
			}
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		if (FoliageState != EnumFoliageState.Ripe)
		{
			return;
		}
		AssetLocation blockCode = AssetLocation.Create(base.Block.Attributes["branchBlock"].AsString(), base.Block.Code.Domain);
		BlockDropItemStack[] fruitStacks = (Api.World.GetBlock(blockCode) as BlockFruitTreeBranch).TypeProps[TreeType].FruitStacks;
		foreach (BlockDropItemStack blockDropItemStack in fruitStacks)
		{
			ItemStack nextItemStack = blockDropItemStack.GetNextItemStack();
			if (nextItemStack != null)
			{
				Api.World.SpawnItemEntity(nextItemStack, Pos);
				if (blockDropItemStack.LastDrop)
				{
					break;
				}
			}
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (FoliageState == EnumFoliageState.Dead && PartType != EnumTreePartType.Cutting)
		{
			dsc.AppendLine("<font color=\"#ff8080\">" + Lang.Get("Dead tree.") + "</font>");
		}
		if (rootBh != null && rootBh.propsByType.Count > 0 && TreeType != null && PartType != EnumTreePartType.Cutting)
		{
			FruitTreeProperties fruitTreeProperties = rootBh?.propsByType[TreeType];
			if (fruitTreeProperties.State == EnumFruitTreeState.Ripe)
			{
				double num = fruitTreeProperties.lastStateChangeTotalDays + (double)fruitTreeProperties.RipeDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Fresh fruit for about {0:0.#} days.", num));
			}
			if (fruitTreeProperties.State == EnumFruitTreeState.Fruiting)
			{
				double num2 = fruitTreeProperties.lastStateChangeTotalDays + (double)fruitTreeProperties.FruitingDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Ripe in about {0:0.#} days, weather permitting.", num2));
			}
			if (fruitTreeProperties.State == EnumFruitTreeState.Flowering)
			{
				double num3 = fruitTreeProperties.lastStateChangeTotalDays + (double)fruitTreeProperties.FloweringDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Flowering for about {0:0.#} days, weather permitting.", num3));
			}
			dsc.AppendLine(Lang.Get("treestate", Lang.Get("treestate-" + fruitTreeProperties.State.ToString().ToLowerInvariant())));
		}
		base.GetBlockInfo(forPlayer, dsc);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		EnumFoliageState foliageState = FoliageState;
		PartType = (EnumTreePartType)tree.GetInt("partType");
		FoliageState = (EnumFoliageState)tree.GetInt("foliageState");
		GrowthDir = BlockFacing.ALLFACES[tree.GetInt("growthDir")];
		TreeType = tree.GetString("treeType");
		Height = tree.GetInt("height");
		fruitingSide = tree.GetInt("fruitingSide", fruitingSide);
		harvested = tree.GetBool("harvested");
		if (tree.HasAttribute("rootOffX"))
		{
			RootOff = new Vec3i(tree.GetInt("rootOffX"), tree.GetInt("rootOffY"), tree.GetInt("rootOffZ"));
		}
		if (Api != null && Api.Side == EnumAppSide.Client && foliageState != FoliageState)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("partType", (int)PartType);
		tree.SetInt("foliageState", (int)FoliageState);
		tree.SetInt("growthDir", GrowthDir.Index);
		tree.SetString("treeType", TreeType);
		tree.SetInt("height", Height);
		tree.SetInt("fruitingSide", fruitingSide);
		tree.SetBool("harvested", harvested);
		if (RootOff != null)
		{
			tree.SetInt("rootOffX", RootOff.X);
			tree.SetInt("rootOffY", RootOff.Y);
			tree.SetInt("rootOffZ", RootOff.Z);
		}
	}
}
