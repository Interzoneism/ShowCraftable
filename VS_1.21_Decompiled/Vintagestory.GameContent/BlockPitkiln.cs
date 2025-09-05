using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPitkiln : BlockGroundStorage, IIgnitable, ISmokeEmitter
{
	public Dictionary<string, BuildStage[]> BuildStagesByBlock = new Dictionary<string, BuildStage[]>();

	public Dictionary<string, Shape> ShapesByBlock = new Dictionary<string, Shape>();

	public byte[] litKilnLightHsv = new byte[3] { 4, 7, 14 };

	private WorldInteraction[] ingiteInteraction;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Dictionary<string, BuildStageMaterial[]> dictionary = new Dictionary<string, BuildStageMaterial[]>();
		List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
		ingiteInteraction = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firepit-ignite",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = list.ToArray(),
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					BlockEntityPitKiln blockEntityPitKiln = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPitKiln;
					return ((blockEntityPitKiln == null || !blockEntityPitKiln.Lit) && blockEntityPitKiln != null && blockEntityPitKiln.CanIgnite) ? wi.Itemstacks : null;
				}
			}
		};
		Dictionary<string, PitKilnModelConfig> dictionary2 = Attributes["modelConfigs"].AsObject<Dictionary<string, PitKilnModelConfig>>();
		foreach (KeyValuePair<string, JsonItemStackBuildStage[]> item in Attributes["buildMats"].AsObject<Dictionary<string, JsonItemStackBuildStage[]>>())
		{
			dictionary[item.Key] = new BuildStageMaterial[item.Value.Length];
			int num = 0;
			JsonItemStackBuildStage[] value = item.Value;
			foreach (JsonItemStackBuildStage jsonItemStackBuildStage in value)
			{
				if (jsonItemStackBuildStage.Resolve(api.World, "pit kiln build material"))
				{
					dictionary[item.Key][num++] = new BuildStageMaterial
					{
						ItemStack = jsonItemStackBuildStage.ResolvedItemstack,
						EleCode = jsonItemStackBuildStage.EleCode,
						TextureCodeReplace = jsonItemStackBuildStage.TextureCodeReplace,
						BurnTimeHours = jsonItemStackBuildStage.BurnTimeHours
					};
				}
			}
		}
		foreach (KeyValuePair<string, PitKilnModelConfig> item2 in dictionary2)
		{
			if (item2.Value?.BuildStages == null || item2.Value.BuildMatCodes == null || item2.Value.Shape?.Base == null)
			{
				api.World.Logger.Error("Pit kiln model configs: Build stage array, build mat array or composite shape is null. Will ignore this config.");
				continue;
			}
			if (item2.Value.BuildStages.Length != item2.Value.BuildMatCodes.Length)
			{
				api.World.Logger.Error("Pit kiln model configs: Build stage array and build mat array not the same length, please fix. Will ignore this config.");
				continue;
			}
			AssetLocation shapePath = item2.Value.Shape.Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, shapePath);
			if (shape == null)
			{
				api.World.Logger.Error("Pit kiln model configs: Shape file {0} not found. Will ignore this config.", item2.Value.Shape.Base);
				continue;
			}
			string[] buildStages = item2.Value.BuildStages;
			string[] buildMatCodes = item2.Value.BuildMatCodes;
			BuildStage[] array = new BuildStage[buildStages.Length];
			for (int num3 = 0; num3 < buildStages.Length; num3++)
			{
				if (!dictionary.TryGetValue(buildMatCodes[num3], out var value2))
				{
					api.World.Logger.Error("Pit kiln model configs: No such mat code " + buildMatCodes[num3] + ". Please fix. Will ignore all configs.");
					return;
				}
				float minHitboxY = 0f;
				if (item2.Value.MinHitboxY2 != null)
				{
					minHitboxY = item2.Value.MinHitboxY2[GameMath.Clamp(num3, 0, item2.Value.MinHitboxY2.Length - 1)];
				}
				array[num3] = new BuildStage
				{
					ElementName = buildStages[num3],
					Materials = value2,
					MinHitboxY2 = minHitboxY,
					MatCode = buildMatCodes[num3]
				};
			}
			BuildStagesByBlock[item2.Key] = array;
			ShapesByBlock[item2.Key] = shape;
		}
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos != null && blockAccessor.GetBlockEntity(pos) is BlockEntityPitKiln { Lit: not false })
		{
			return litKilnLightHsv;
		}
		return base.GetLightHsv(blockAccessor, pos, stack);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			blockEntityGroundStorage.OnPlayerInteractStart(byPlayer, blockSel);
			return true;
		}
		return true;
	}

	public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return base.GetBlockMaterial(blockAccessor, pos, stack);
	}

	public bool TryCreateKiln(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			if (!blockEntityGroundStorage.OnTryCreateKiln())
			{
				return false;
			}
			ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
			bool flag = true;
			if (world.BlockAccessor.GetBlock(pos, 2).BlockId != 0)
			{
				flag = false;
				coreClientAPI?.TriggerIngameError(this, "submerged", Lang.Get("pitkilnerror-submerged"));
			}
			if (!flag)
			{
				return false;
			}
			BlockFacing[] array = BlockFacing.HORIZONTALS.Append(BlockFacing.DOWN);
			foreach (BlockFacing blockFacing in array)
			{
				BlockPos pos2 = pos.AddCopy(blockFacing);
				Block block = world.BlockAccessor.GetBlock(pos2);
				if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos2, blockFacing.Opposite))
				{
					coreClientAPI?.TriggerIngameError(this, "notsolid", Lang.Get("Pit kilns need to be surrounded by solid, non-flammable blocks"));
					flag = false;
					break;
				}
				if (block.CombustibleProps != null)
				{
					coreClientAPI?.TriggerIngameError(this, "notsolid", Lang.Get("Pit kilns need to be surrounded by solid, non-flammable blocks"));
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			if (world.BlockAccessor.GetBlock(pos.UpCopy()).Replaceable < 6000)
			{
				flag = false;
				coreClientAPI?.TriggerIngameError(this, "notairspace", Lang.Get("Pit kilns need one block of air space above them"));
			}
			if (!flag)
			{
				return false;
			}
			BuildStage[] value = null;
			bool flag2 = false;
			foreach (KeyValuePair<string, BuildStage[]> item in BuildStagesByBlock)
			{
				if (!blockEntityGroundStorage.Inventory[0].Empty && WildcardUtil.Match(new AssetLocation(item.Key), blockEntityGroundStorage.Inventory[0].Itemstack.Collectible.Code))
				{
					value = item.Value;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				BuildStagesByBlock.TryGetValue("*", out value);
			}
			if (value == null)
			{
				return false;
			}
			if (!activeHotbarSlot.Itemstack.Equals(world, value[0].Materials[0].ItemStack, GlobalConstants.IgnoredStackAttributes) || activeHotbarSlot.StackSize < value[0].Materials[0].ItemStack.StackSize)
			{
				return false;
			}
			InventoryBase inventory = blockEntityGroundStorage.Inventory;
			world.BlockAccessor.SetBlock(Id, pos);
			BlockEntityPitKiln blockEntityPitKiln = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln;
			for (int j = 0; j < inventory.Count; j++)
			{
				blockEntityPitKiln.Inventory[j] = inventory[j];
			}
			blockEntityPitKiln.MeshAngle = blockEntityGroundStorage.MeshAngle;
			blockEntityPitKiln.OnCreated(byPlayer);
			blockEntityPitKiln.updateMeshes();
			blockEntityPitKiln.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public new EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln).CanIgnite)
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 4f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public new void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln)?.TryIgnite((byEntity as EntityPlayer).Player);
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln).Lit)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityPitKiln blockEntityPitKiln)
		{
			if (!blockEntityPitKiln.IsComplete)
			{
				ItemStack[] stacks = blockEntityPitKiln.NextBuildStage.Materials.Select((BuildStageMaterial bsm) => bsm.ItemStack).ToArray();
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-pitkiln-build",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = stacks.ToArray(),
						GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => stacks
					}
				};
			}
			return ingiteInteraction;
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(this).GetName();
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityPitKiln blockEntity = GetBlockEntity<BlockEntityPitKiln>(pos);
			if (blockEntity == null || !blockEntity.IsBurning)
			{
				return 1f;
			}
			return 10000f;
		}
		return base.GetTraversalCost(pos, creatureType);
	}

	public bool EmitsSmoke(BlockPos pos)
	{
		return (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln)?.IsBurning ?? false;
	}
}
