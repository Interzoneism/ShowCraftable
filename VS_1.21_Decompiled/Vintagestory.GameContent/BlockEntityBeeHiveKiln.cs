using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityBeeHiveKiln : BlockEntity, IRotatable
{
	public BlockFacing Orientation;

	private MultiblockStructure structure;

	private MultiblockStructure highlightedStructure;

	private BlockPos CenterPos;

	private bool receivesHeat;

	private float receivesHeatSmooth;

	public double TotalHoursLastUpdate;

	public double TotalHoursHeatReceived;

	public bool StructureComplete;

	private int tickCounter;

	private bool wasNotProcessing;

	private BlockPos[] particlePositions;

	private BEBehaviorDoor beBehaviorDoor;

	public static int KilnBreakAfterHours = 168;

	public static int ItemBurnTimeHours = 9;

	public static int ItemBurnTemperature = 950;

	public static int ItemMaxTemperature = 1200;

	public static int ItemTemperatureGainPerHour = 500;

	private static AdvancedParticleProperties smokeParticles = new AdvancedParticleProperties
	{
		HsvaColor = new NatFloat[4]
		{
			NatFloat.createUniform(0f, 0f),
			NatFloat.createUniform(0f, 0f),
			NatFloat.createUniform(40f, 30f),
			NatFloat.createUniform(220f, 50f)
		},
		OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
		GravityEffect = NatFloat.createUniform(0f, 0f),
		Velocity = new NatFloat[3]
		{
			NatFloat.createUniform(0f, 0.05f),
			NatFloat.createUniform(0.2f, 0.3f),
			NatFloat.createUniform(0f, 0.05f)
		},
		Size = NatFloat.createUniform(0.3f, 0.05f),
		Quantity = NatFloat.createUniform(0.25f, 0f),
		SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
		LifeLength = NatFloat.createUniform(4.5f, 0f),
		ParticleModel = EnumParticleModel.Quad,
		SelfPropelled = true
	};

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		structure = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		if (Orientation != null)
		{
			Init();
		}
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		if (Orientation != null && CenterPos == null)
		{
			Init();
		}
	}

	public void Init()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientTick50ms, 50);
		}
		else
		{
			RegisterGameTickListener(OnServerTick1s, 1000);
		}
		int num = 0;
		switch (Orientation.Code)
		{
		case "east":
			num = 270;
			break;
		case "west":
			num = 90;
			break;
		case "south":
			num = 180;
			break;
		}
		structure.InitForUse(num);
		CenterPos = Pos.AddCopy(Orientation.Normali * 2);
		particlePositions = new BlockPos[10];
		BlockPos blockPos = CenterPos.Down();
		particlePositions[0] = blockPos;
		particlePositions[1] = blockPos.AddCopy(Orientation.Opposite);
		particlePositions[2] = blockPos.AddCopy(Orientation);
		particlePositions[3] = blockPos.AddCopy(Orientation.GetCW());
		particlePositions[4] = blockPos.AddCopy(Orientation.GetCW()).Add(Orientation.Opposite);
		particlePositions[5] = blockPos.AddCopy(Orientation.GetCW()).Add(Orientation);
		particlePositions[6] = blockPos.AddCopy(Orientation.GetCCW());
		particlePositions[7] = blockPos.AddCopy(Orientation.GetCCW()).Add(Orientation.Opposite);
		particlePositions[8] = blockPos.AddCopy(Orientation.GetCCW()).Add(Orientation);
		particlePositions[9] = blockPos.UpCopy(3);
		beBehaviorDoor = GetBehavior<BEBehaviorDoor>();
	}

	private void OnClientTick50ms(float dt)
	{
		receivesHeatSmooth = GameMath.Clamp(receivesHeatSmooth + (receivesHeat ? (dt / 10f) : ((0f - dt) / 3f)), 0f, 1f);
		if (receivesHeatSmooth == 0f)
		{
			return;
		}
		Random rand = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			int num = Math.Min(Entity.FireParticleProps.Length - 1, Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
			for (int j = 0; j < particlePositions.Length; j++)
			{
				AdvancedParticleProperties advancedParticleProperties = Entity.FireParticleProps[num];
				BlockPos blockPos = particlePositions[j];
				if (j == 9)
				{
					advancedParticleProperties = smokeParticles;
					advancedParticleProperties.Quantity.avg = 0.2f;
					advancedParticleProperties.basePos.Set((double)blockPos.X + 0.5, (double)blockPos.InternalY + 0.75, (double)blockPos.Z + 0.5);
					advancedParticleProperties.Velocity[1].avg = (float)(0.3 + 0.3 * rand.NextDouble()) * 2f;
					advancedParticleProperties.PosOffset[1].var = 0.2f;
					advancedParticleProperties.Velocity[0].avg = (float)(rand.NextDouble() - 0.5) / 4f;
					advancedParticleProperties.Velocity[2].avg = (float)(rand.NextDouble() - 0.5) / 4f;
				}
				else
				{
					advancedParticleProperties.Quantity.avg = GameMath.Sqrt(0.5f * num switch
					{
						1 => 5f, 
						0 => 0.5f, 
						_ => 0.6f, 
					}) / 4f;
					advancedParticleProperties.basePos.Set((double)blockPos.X + 0.5, (double)blockPos.InternalY + 0.5, (double)blockPos.Z + 0.5);
					advancedParticleProperties.Velocity[1].avg = (float)(0.5 + 0.5 * rand.NextDouble()) * 2f;
					advancedParticleProperties.PosOffset[1].var = 1f;
					advancedParticleProperties.Velocity[0].avg = (float)(rand.NextDouble() - 0.5);
					advancedParticleProperties.Velocity[2].avg = (float)(rand.NextDouble() - 0.5);
				}
				advancedParticleProperties.PosOffset[0].var = 0.49f;
				advancedParticleProperties.PosOffset[2].var = 0.49f;
				Api.World.SpawnParticles(advancedParticleProperties);
			}
		}
	}

	private void OnServerTick1s(float dt)
	{
		if (receivesHeat)
		{
			Vec3d vec3d = CenterPos.ToVec3d().Add(0.5, 0.0, 0.5);
			Entity[] entitiesAround = Api.World.GetEntitiesAround(vec3d, 1.75f, 3f, (Entity e) => e.Alive && e is EntityAgent);
			for (int num = 0; num < entitiesAround.Length; num++)
			{
				entitiesAround[num].ReceiveDamage(new DamageSource
				{
					DamageTier = 1,
					SourcePos = vec3d,
					SourceBlock = base.Block,
					Type = EnumDamageType.Fire
				}, 4f);
			}
		}
		if (++tickCounter % 3 == 0)
		{
			OnServerTick3s();
		}
	}

	private void OnServerTick3s()
	{
		bool markDirty = false;
		float num = float.MaxValue;
		bool flag = receivesHeat;
		bool structureComplete = StructureComplete;
		if (!receivesHeat)
		{
			TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
		}
		receivesHeat = true;
		for (int i = 0; i < 9; i++)
		{
			BlockPos position = particlePositions[i].DownCopy();
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(position);
			float num2 = 0f;
			if (blockEntity is BlockEntityCoalPile { IsBurning: not false } blockEntityCoalPile)
			{
				num2 = blockEntityCoalPile.GetHoursLeft(TotalHoursLastUpdate);
			}
			else if (blockEntity is BlockEntityGroundStorage { IsBurning: not false } blockEntityGroundStorage)
			{
				num2 = blockEntityGroundStorage.GetHoursLeft(TotalHoursLastUpdate);
			}
			num = Math.Min(num, num2);
			receivesHeat &= num2 > 0f;
		}
		StructureComplete = structure.InCompleteBlockCount(Api.World, Pos) == 0;
		if (flag != receivesHeat || structureComplete != StructureComplete)
		{
			markDirty = true;
		}
		if (receivesHeat)
		{
			if (!StructureComplete || beBehaviorDoor.Opened)
			{
				wasNotProcessing = true;
				TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
				MarkDirty();
				return;
			}
			if (wasNotProcessing)
			{
				wasNotProcessing = false;
				TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
			}
			double num3 = Api.World.Calendar.TotalHours - TotalHoursLastUpdate;
			float num4 = Math.Max(0f, GameMath.Min((float)num3, num));
			TotalHoursHeatReceived += num4;
			UpdateGroundStorage(num4);
			TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
			markDirty = true;
		}
		if (TotalHoursHeatReceived >= (double)KilnBreakAfterHours)
		{
			TotalHoursHeatReceived = 0.0;
			structure.WalkMatchingBlocks(Api.World, Pos, delegate(Block block, BlockPos pos)
			{
				float num5 = block.Attributes?["heatResistance"].AsFloat(1f) ?? 1f;
				if (Api.World.Rand.NextDouble() > (double)num5)
				{
					Block block2 = Api.World.GetBlock(block.CodeWithVariant("state", "damaged"));
					Api.World.BlockAccessor.SetBlock(block2.Id, pos);
					StructureComplete = false;
					markDirty = true;
				}
			});
		}
		if (markDirty)
		{
			MarkDirty();
		}
	}

	private void UpdateGroundStorage(float hoursHeatReceived)
	{
		int openDoors = GetOpenDoors();
		for (int i = 0; i < 9; i++)
		{
			for (int j = 1; j < 4; j++)
			{
				BlockPos position = particlePositions[i].UpCopy(j);
				BlockEntityGroundStorage blockEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(position);
				if (blockEntity == null)
				{
					continue;
				}
				for (int k = 0; k < blockEntity.Inventory.Count; k++)
				{
					ItemSlot itemSlot = blockEntity.Inventory[k];
					if (itemSlot.Empty)
					{
						continue;
					}
					float num = 0f;
					CollectibleObject collectible = itemSlot.Itemstack.Collectible;
					CombustibleProperties combustibleProps = collectible.CombustibleProps;
					if (combustibleProps == null || combustibleProps.SmeltedStack?.ResolvedItemstack.Block?.BlockMaterial != EnumBlockMaterial.Ceramic)
					{
						CombustibleProperties combustibleProps2 = collectible.CombustibleProps;
						if (combustibleProps2 == null || combustibleProps2.SmeltingType != EnumSmeltType.Fire)
						{
							JsonObject attributes = collectible.Attributes;
							if (attributes == null || !attributes["beehivekiln"].Exists)
							{
								goto IL_01ef;
							}
						}
					}
					float num2 = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack, hoursHeatReceived);
					float num3 = hoursHeatReceived * (float)ItemTemperatureGainPerHour;
					float num4 = ((float)ItemBurnTemperature - num2) / (float)ItemTemperatureGainPerHour;
					if (num4 < 0f)
					{
						num4 = 0f;
					}
					if (num2 < (float)ItemMaxTemperature)
					{
						num2 = GameMath.Min(ItemMaxTemperature, num2 + num3);
						collectible.SetTemperature(Api.World, itemSlot.Itemstack, num2);
					}
					float num5 = hoursHeatReceived - num4;
					if (num2 >= (float)ItemBurnTemperature && num5 > 0f)
					{
						num = itemSlot.Itemstack.Attributes.GetFloat("hoursHeatReceived") + num5;
						itemSlot.Itemstack.Attributes.SetFloat("hoursHeatReceived", num);
					}
					itemSlot.MarkDirty();
					goto IL_01ef;
					IL_01ef:
					if (num >= (float)ItemBurnTimeHours)
					{
						ConvertItemToBurned(blockEntity, itemSlot, openDoors);
					}
				}
				blockEntity.MarkDirty();
			}
		}
	}

	private void ConvertItemToBurned(BlockEntityGroundStorage groundStorage, ItemSlot itemSlot, int doorOpen)
	{
		groundStorage.forceStorageProps = true;
		if (itemSlot != null && !itemSlot.Empty)
		{
			ItemStack itemstack = itemSlot.Itemstack;
			ItemStack itemStack = itemstack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
			JsonObject obj = itemstack.Collectible.Attributes?["beehivekiln"];
			JsonItemStack jsonItemStack = obj?[doorOpen.ToString()]?.AsObject<JsonItemStack>();
			float temperature = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack);
			if (obj != null && obj.Exists && jsonItemStack != null && jsonItemStack.Resolve(Api.World, "beehivekiln-burn"))
			{
				itemSlot.Itemstack = jsonItemStack.ResolvedItemstack.Clone();
				itemSlot.Itemstack.StackSize = itemstack.StackSize / itemstack.Collectible.CombustibleProps.SmeltedRatio;
			}
			else if (itemStack != null)
			{
				itemSlot.Itemstack = itemStack.Clone();
				itemSlot.Itemstack.StackSize = itemstack.StackSize / itemstack.Collectible.CombustibleProps.SmeltedRatio;
			}
			itemSlot.Itemstack.Collectible.SetTemperature(Api.World, itemSlot.Itemstack, temperature);
			itemSlot.MarkDirty();
		}
		groundStorage.MarkDirty(redrawOnClient: true);
	}

	private int GetOpenDoors()
	{
		BlockPos blockPos = CenterPos.AddCopy(Orientation.Normali * 2).Up();
		BlockPos blockPos2 = CenterPos.AddCopy(Orientation.GetCW().Normali * 2).Up();
		BlockPos blockPos3 = CenterPos.AddCopy(Orientation.GetCCW().Normali * 2).Up();
		int num = 0;
		BlockPos[] array = new BlockPos[3] { blockPos, blockPos2, blockPos3 };
		foreach (BlockPos pos in array)
		{
			Block block = Api.World.BlockAccessor.GetBlock(pos);
			if (block.Variant["state"] != null && block.Variant["state"] == "opened")
			{
				num++;
			}
		}
		return num;
	}

	public void Interact(IPlayer byPlayer)
	{
		if (!(Api is ICoreClientAPI coreClientAPI))
		{
			return;
		}
		bool shiftKey = byPlayer.WorldData.EntityControls.ShiftKey;
		int damagedTiles = 0;
		int wrongTiles = 0;
		int num = 0;
		BlockPos pos = Pos;
		if (!shiftKey)
		{
			num = structure.InCompleteBlockCount(Api.World, Pos, delegate(Block haveBlock, AssetLocation wantLoc)
			{
				switch (haveBlock.FirstCodePart())
				{
				case "refractorybricks":
				case "claybricks":
				case "refractorybrickgrating":
					if (haveBlock.Variant["state"] == "damaged")
					{
						damagedTiles++;
						return;
					}
					break;
				}
				wrongTiles++;
			});
			if (num > 0)
			{
				highlightedStructure = structure;
			}
		}
		if (!shiftKey && num > 0)
		{
			if (wrongTiles > 0 && damagedTiles > 0)
			{
				coreClientAPI.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", wrongTiles, damagedTiles));
			}
			else if (wrongTiles > 0)
			{
				coreClientAPI.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", wrongTiles));
			}
			else if (damagedTiles == 1)
			{
				coreClientAPI.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tile is damaged!", damagedTiles));
			}
			else
			{
				coreClientAPI.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tiles are damaged!", damagedTiles));
			}
			highlightedStructure.HighlightIncompleteParts(Api.World, byPlayer, pos);
		}
		else
		{
			highlightedStructure?.ClearHighlights(Api.World, byPlayer);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		receivesHeat = tree.GetBool("receivesHeat");
		TotalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate");
		StructureComplete = tree.GetBool("structureComplete");
		Orientation = BlockFacing.FromFirstLetter(tree.GetString("orientation"));
		TotalHoursHeatReceived = tree.GetDouble("totalHoursHeatReceived");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("receivesHeat", receivesHeat);
		tree.SetDouble("totalHoursLastUpdate", TotalHoursLastUpdate);
		tree.SetBool("structureComplete", StructureComplete);
		tree.SetString("orientation", Orientation.Code);
		tree.SetDouble("totalHoursHeatReceived", TotalHoursHeatReceived);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			highlightedStructure?.ClearHighlights(Api.World, coreClientAPI.World.Player);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			highlightedStructure?.ClearHighlights(Api.World, coreClientAPI.World.Player);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (GetBehavior<BEBehaviorDoor>().Opened)
		{
			dsc.AppendLine(Lang.Get("Door must be closed for firing!"));
		}
		if (!StructureComplete)
		{
			dsc.AppendLine(Lang.Get("Structure incomplete! Can't get hot enough, paused."));
			return;
		}
		if (receivesHeat)
		{
			dsc.AppendLine(Lang.Get("Okay! Receives heat!"));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Ready to be fired. Ignite 3x3 piles of coal below. (progress will proceed once all 9 piles have ignited)"));
		}
		if (TotalHoursHeatReceived > 0.0)
		{
			dsc.AppendLine(Lang.Get("Firing: for {0:0.##} hours", TotalHoursHeatReceived));
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		BlockFacing horizontalRotated = BlockFacing.FromFirstLetter(tree.GetString("orientation")).GetHorizontalRotated(-degreeRotation - 180);
		tree.SetString("orientation", horizontalRotated.Code);
		float num = tree.GetFloat("rotateYRad");
		num = (num - (float)degreeRotation * ((float)Math.PI / 180f)) % ((float)Math.PI * 2f);
		tree.SetFloat("rotateYRad", num);
	}
}
