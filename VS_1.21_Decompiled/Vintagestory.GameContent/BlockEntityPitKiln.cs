using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityPitKiln : BlockEntityGroundStorage, IHeatSource
{
	protected ILoadedSound ambientSound;

	protected BuildStage[] buildStages;

	protected Shape shape;

	protected MeshData mesh;

	protected string[] selectiveElements;

	protected Dictionary<string, string> textureCodeReplace = new Dictionary<string, string>();

	protected int currentBuildStage;

	public bool Lit;

	public double BurningUntilTotalHours;

	public float BurnTimeHours = 20f;

	private bool nowTesselatingKiln;

	private ITexPositionSource blockTexPos;

	public bool IsComplete => currentBuildStage >= buildStages.Length;

	public BuildStage NextBuildStage => buildStages[currentBuildStage];

	protected override int invSlotCount => 10;

	public override TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (nowTesselatingKiln)
			{
				if (textureCodeReplace.TryGetValue(textureCode, out var value))
				{
					textureCode = value;
				}
				return blockTexPos[textureCode];
			}
			return base[textureCode];
		}
	}

	public override bool CanIgnite
	{
		get
		{
			if (IsComplete && IsValidPitKiln())
			{
				return !GetBehavior<BEBehaviorBurning>().IsBurning;
			}
			return false;
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		if (Lit)
		{
			BurningUntilTotalHours = Math.Min(api.World.Calendar.TotalHours + (double)BurnTimeHours, BurningUntilTotalHours);
		}
		base.Initialize(api);
		behavior.OnFireTick = delegate
		{
			if (api.World.Calendar.TotalHours >= BurningUntilTotalHours && IsAreaLoaded())
			{
				OnFired();
			}
		};
		behavior.OnFireDeath = KillFire;
		behavior.ShouldBurn = () => Lit;
		behavior.OnCanBurn = delegate(BlockPos pos)
		{
			if (pos == Pos && !Lit && IsComplete)
			{
				return true;
			}
			Block block = Api.World.BlockAccessor.GetBlock(pos);
			Block block2 = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
			return block?.CombustibleProps != null && block.CombustibleProps.BurnDuration > 0f && (!IsAreaLoaded() || block2.Replaceable >= 6000);
		};
		DetermineBuildStages();
		behavior.FuelPos = Pos.Copy();
		behavior.FirePos = Pos.UpCopy();
	}

	public bool IsAreaLoaded()
	{
		if (Api == null || Api.Side == EnumAppSide.Client)
		{
			return true;
		}
		ICoreServerAPI coreServerAPI = Api as ICoreServerAPI;
		int num = coreServerAPI.WorldManager.MapSizeX / 32;
		int num2 = coreServerAPI.WorldManager.MapSizeY / 32;
		int num3 = coreServerAPI.WorldManager.MapSizeZ / 32;
		int num4 = GameMath.Clamp((Pos.X - 1) / 32, 0, num - 1);
		int num5 = GameMath.Clamp((Pos.X + 1) / 32, 0, num - 1);
		int num6 = GameMath.Clamp((Pos.Y - 1) / 32, 0, num2 - 1);
		int num7 = GameMath.Clamp((Pos.Y + 1) / 32, 0, num2 - 1);
		int num8 = GameMath.Clamp((Pos.Z - 1) / 32, 0, num3 - 1);
		int num9 = GameMath.Clamp((Pos.Z + 1) / 32, 0, num3 - 1);
		for (int i = num4; i <= num5; i++)
		{
			for (int j = num6; j <= num7; j++)
			{
				for (int k = num8; k <= num9; k++)
				{
					if (coreServerAPI.WorldManager.GetChunk(i, j, k) == null)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public override bool OnPlayerInteractStart(IPlayer player, BlockSelection bs)
	{
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return false;
		}
		if (currentBuildStage < buildStages.Length)
		{
			BuildStageMaterial[] materials = buildStages[currentBuildStage].Materials;
			for (int i = 0; i < materials.Length; i++)
			{
				ItemStack itemStack = materials[i].ItemStack;
				if (!itemStack.Equals(Api.World, activeHotbarSlot.Itemstack, GlobalConstants.IgnoredStackAttributes) || itemStack.StackSize > activeHotbarSlot.StackSize || !isSameMatAsPreviouslyAdded(itemStack))
				{
					continue;
				}
				int num = itemStack.StackSize;
				for (int j = 4; j < invSlotCount; j++)
				{
					if (num <= 0)
					{
						break;
					}
					num -= activeHotbarSlot.TryPutInto(Api.World, inventory[j], num);
				}
				activeHotbarSlot.MarkDirty();
				currentBuildStage++;
				mesh = null;
				MarkDirty(redrawOnClient: true);
				updateSelectiveElements();
				(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				JsonObject attributes = itemStack.Collectible.Attributes;
				if (attributes != null && attributes["placeSound"].Exists)
				{
					AssetLocation assetLocation = AssetLocation.Create(itemStack.Collectible.Attributes["placeSound"].AsString(), itemStack.Collectible.Code.Domain);
					if (assetLocation != null)
					{
						Api.World.PlaySoundAt(assetLocation.WithPathPrefixOnce("sounds/"), Pos, -0.4, player, randomizePitch: true, 12f);
					}
				}
			}
		}
		DetermineStorageProperties(null);
		return true;
	}

	protected bool isSameMatAsPreviouslyAdded(ItemStack newStack)
	{
		BuildStage buildStage = buildStages[currentBuildStage];
		for (int i = 0; i < inventory.Count; i++)
		{
			ItemSlot slot = inventory[i];
			if (!slot.Empty && buildStage.Materials.FirstOrDefault((BuildStageMaterial bsm) => bsm.ItemStack.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes)) != null && !newStack.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes))
			{
				return false;
			}
		}
		return true;
	}

	public override void DetermineStorageProperties(ItemSlot sourceSlot)
	{
		base.DetermineStorageProperties(sourceSlot);
		if (buildStages != null)
		{
			colBoxes[0].X1 = 0f;
			colBoxes[0].X2 = 1f;
			colBoxes[0].Z1 = 0f;
			colBoxes[0].Z2 = 1f;
			colBoxes[0].Y2 = Math.Max(colBoxes[0].Y2, buildStages[Math.Min(buildStages.Length - 1, currentBuildStage)].MinHitboxY2 / 16f);
			selBoxes[0] = colBoxes[0];
		}
	}

	protected override void FixBrokenStorageLayout()
	{
	}

	public override float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return Lit ? 10 : 0;
	}

	public void OnFired()
	{
		if (IsValidPitKiln())
		{
			for (int i = 0; i < 4; i++)
			{
				ItemSlot itemSlot = inventory[i];
				if (!itemSlot.Empty)
				{
					ItemStack itemstack = itemSlot.Itemstack;
					ItemStack itemStack = itemstack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
					if (itemStack != null)
					{
						itemSlot.Itemstack = itemStack.Clone();
						itemSlot.Itemstack.StackSize = itemstack.StackSize / itemstack.Collectible.CombustibleProps.SmeltedRatio;
					}
				}
			}
			MarkDirty(redrawOnClient: true);
		}
		KillFire(consumefuel: true);
	}

	protected bool IsValidPitKiln()
	{
		IWorldAccessor world = Api.World;
		if (world.BlockAccessor.GetBlock(Pos, 2).BlockId != 0)
		{
			return false;
		}
		BlockFacing[] array = BlockFacing.HORIZONTALS.Append(BlockFacing.DOWN);
		foreach (BlockFacing blockFacing in array)
		{
			BlockPos pos = Pos.AddCopy(blockFacing);
			Block block = world.BlockAccessor.GetBlock(pos);
			if (!block.CanAttachBlockAt(world.BlockAccessor, base.Block, pos, blockFacing.Opposite))
			{
				return false;
			}
			if (block.CombustibleProps != null)
			{
				return false;
			}
		}
		if (world.BlockAccessor.GetBlock(Pos.UpCopy()).Replaceable < 6000)
		{
			return false;
		}
		return true;
	}

	public void OnCreated(IPlayer byPlayer)
	{
		base.StorageProps = null;
		mesh = null;
		DetermineBuildStages();
		DetermineStorageProperties(null);
		ItemStack itemStack = (inventory[4].Itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(buildStages[0].Materials[0].ItemStack.StackSize));
		ItemStack itemStack3 = itemStack;
		currentBuildStage++;
		JsonObject attributes = itemStack3.Collectible.Attributes;
		if (attributes != null && attributes["placeSound"].Exists)
		{
			AssetLocation assetLocation = AssetLocation.Create(itemStack3.Collectible.Attributes["placeSound"].AsString(), itemStack3.Collectible.Code.Domain);
			if (assetLocation != null)
			{
				Api.World.PlaySoundAt(assetLocation.WithPathPrefixOnce("sounds/"), Pos, -0.4, byPlayer, randomizePitch: true, 12f);
			}
		}
		byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		updateSelectiveElements();
	}

	public void DetermineBuildStages()
	{
		BlockPitkiln blockPitkiln = base.Block as BlockPitkiln;
		bool flag = false;
		foreach (KeyValuePair<string, BuildStage[]> item in blockPitkiln.BuildStagesByBlock)
		{
			if (!inventory[0].Empty && WildcardUtil.Match(new AssetLocation(item.Key), inventory[0].Itemstack.Collectible.Code))
			{
				buildStages = item.Value;
				shape = blockPitkiln.ShapesByBlock[item.Key];
				flag = true;
				break;
			}
		}
		if (!flag && blockPitkiln.BuildStagesByBlock.TryGetValue("*", out buildStages))
		{
			shape = blockPitkiln.ShapesByBlock["*"];
		}
		updateSelectiveElements();
	}

	private void updateSelectiveElements()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			textureCodeReplace.Clear();
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			for (int i = 0; i < currentBuildStage; i++)
			{
				BuildStage buildStage = buildStages[i];
				if (!dictionary.ContainsKey(buildStage.MatCode))
				{
					BuildStageMaterial buildStageMaterial = currentlyUsedMaterialOfStage(buildStage);
					dictionary[buildStage.MatCode] = buildStageMaterial?.EleCode;
					if (buildStageMaterial.TextureCodeReplace != null)
					{
						textureCodeReplace[buildStageMaterial.TextureCodeReplace.From] = buildStageMaterial.TextureCodeReplace.To;
					}
				}
			}
			selectiveElements = new string[currentBuildStage];
			for (int j = 0; j < currentBuildStage; j++)
			{
				string text = buildStages[j].ElementName;
				if (dictionary.TryGetValue(buildStages[j].MatCode, out var value))
				{
					text = text.Replace("{eleCode}", value);
				}
				selectiveElements[j] = text;
			}
		}
		else
		{
			for (int k = 0; k < currentBuildStage; k++)
			{
				BuildStage buildStage2 = buildStages[k];
				BuildStageMaterial buildStageMaterial2 = currentlyUsedMaterialOfStage(buildStage2);
				if (buildStageMaterial2 != null && buildStageMaterial2.BurnTimeHours.HasValue)
				{
					BurnTimeHours = buildStageMaterial2.BurnTimeHours.Value;
				}
			}
		}
		colBoxes[0].X1 = 0f;
		colBoxes[0].X2 = 1f;
		colBoxes[0].Z1 = 0f;
		colBoxes[0].Z2 = 1f;
		colBoxes[0].Y2 = Math.Max(colBoxes[0].Y2, buildStages[Math.Min(buildStages.Length - 1, currentBuildStage)].MinHitboxY2 / 16f);
		selBoxes[0] = colBoxes[0];
	}

	private BuildStageMaterial currentlyUsedMaterialOfStage(BuildStage buildStage)
	{
		BuildStageMaterial[] materials = buildStage.Materials;
		foreach (BuildStageMaterial buildStageMaterial in materials)
		{
			foreach (ItemSlot item in inventory)
			{
				if (!item.Empty && item.Itemstack.Equals(Api.World, buildStageMaterial.ItemStack, GlobalConstants.IgnoredStackAttributes))
				{
					return buildStageMaterial;
				}
			}
		}
		return null;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		BurningUntilTotalHours = tree.GetDouble("burnUntil");
		int num = currentBuildStage;
		bool lit = Lit;
		currentBuildStage = tree.GetInt("currentBuildStage");
		Lit = tree.GetBool("lit");
		if (Api != null)
		{
			DetermineBuildStages();
			if (Api.Side == EnumAppSide.Client)
			{
				if (num != currentBuildStage)
				{
					mesh = null;
				}
				if (!lit && Lit)
				{
					TryIgnite(null);
				}
				if (lit && !Lit)
				{
					GetBehavior<BEBehaviorBurning>().KillFire(consumeFuel: false);
				}
			}
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("currentBuildStage", currentBuildStage);
		tree.SetBool("lit", Lit);
		tree.SetDouble("burnUntil", BurningUntilTotalHours);
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		DetermineBuildStages();
		if (mesh == null)
		{
			nowTesselatingKiln = true;
			blockTexPos = tesselator.GetTextureSource(base.Block);
			tesselator.TesselateShape("pitkiln", shape, out mesh, this, null, 0, 0, 0, null, selectiveElements);
			nowTesselatingKiln = false;
			mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1.005f, 1.005f, 1.005f);
			mesh.Translate(0f, (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 10) / 500f, 0f);
		}
		meshdata.AddMeshData(mesh);
		base.OnTesselation(meshdata, tesselator);
		return true;
	}

	public void TryIgnite(IPlayer byPlayer)
	{
		BurningUntilTotalHours = Api.World.Calendar.TotalHours + (double)BurnTimeHours;
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		Lit = true;
		behavior.OnFirePlaced(Pos.UpCopy(), Pos.Copy(), byPlayer?.PlayerUID);
		Api.World.BlockAccessor.ExchangeBlock(base.Block.Id, Pos);
		MarkDirty(redrawOnClient: true);
	}

	public override string GetBlockName()
	{
		return Lang.Get("Pit kiln");
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (!inventory.Empty)
		{
			string[] contentSummary = getContentSummary();
			foreach (string value in contentSummary)
			{
				dsc.AppendLine(value);
			}
			if (Lit)
			{
				dsc.AppendLine(Lang.Get("Lit"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Unlit"));
			}
		}
	}

	public override string[] getContentSummary()
	{
		OrderedDictionary<string, int> orderedDictionary = new OrderedDictionary<string, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlot itemSlot = inventory[i];
			if (!itemSlot.Empty)
			{
				string name = itemSlot.Itemstack.GetName();
				if (!orderedDictionary.TryGetValue(name, out var value))
				{
					value = 0;
				}
				orderedDictionary[name] = value + itemSlot.StackSize;
			}
		}
		return orderedDictionary.Select((KeyValuePair<string, int> elem) => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
	}

	public void KillFire(bool consumefuel)
	{
		if (!consumefuel)
		{
			Lit = false;
			Api.World.BlockAccessor.RemoveBlockLight((base.Block as BlockPitkiln).litKilnLightHsv, Pos);
			MarkDirty(redrawOnClient: true);
		}
		else if (Api.Side != EnumAppSide.Client)
		{
			Block block = Api.World.GetBlock(new AssetLocation("groundstorage"));
			Api.World.BlockAccessor.SetBlock(block.Id, Pos);
			Api.World.BlockAccessor.RemoveBlockLight((base.Block as BlockPitkiln).litKilnLightHsv, Pos);
			BlockEntityGroundStorage blockEntityGroundStorage = Api.World.BlockAccessor.GetBlockEntity(Pos) as BlockEntityGroundStorage;
			GroundStorageProperties groundStorageProperties = (inventory.FirstNonEmptySlot?.Itemstack)?.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
			blockEntityGroundStorage.ForceStorageProps(groundStorageProperties ?? base.StorageProps);
			for (int i = 0; i < 4; i++)
			{
				blockEntityGroundStorage.Inventory[i] = inventory[i];
			}
			MarkDirty(redrawOnClient: true);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
	}
}
