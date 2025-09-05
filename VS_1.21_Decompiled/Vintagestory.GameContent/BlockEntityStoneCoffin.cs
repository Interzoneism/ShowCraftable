using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityStoneCoffin : BlockEntityContainer
{
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

	private MultiblockStructure ms;

	private MultiblockStructure msOpp;

	private MultiblockStructure msHighlighted;

	private BlockStoneCoffinSection blockScs;

	private InventoryStoneCoffin inv;

	private ICoreClientAPI capi;

	private bool receivesHeat;

	private float receivesHeatSmooth;

	private double progress;

	private double totalHoursLastUpdate;

	private bool processComplete;

	public bool StructureComplete;

	private int tickCounter;

	private int tempStoneCoffin;

	private BlockPos[] particlePositions = new BlockPos[8];

	private string[] selectiveElementsMain = Array.Empty<string>();

	private string[] selectiveElementsSecondary = Array.Empty<string>();

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "stonecoffin";

	public bool IsFull
	{
		get
		{
			if (IngotCount == 16)
			{
				return CoalLayerCount == 5;
			}
			return false;
		}
	}

	public int IngotCount => inv[1].StackSize;

	public int CoalLayerCount => inv[0].StackSize / 8;

	public int CoffinTemperature => tempStoneCoffin;

	public BlockEntityStoneCoffin()
	{
		inv = new InventoryStoneCoffin(2, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inv.LateInitialize(InventoryClassName + "-" + Pos, api);
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(onClientTick50ms, 50);
		}
		else
		{
			RegisterGameTickListener(onServerTick1s, 1000);
		}
		ms = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		msOpp = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		int num = 0;
		int num2 = 180;
		if (base.Block.Variant["side"] == "east")
		{
			num = 270;
			num2 = 90;
		}
		ms.InitForUse(num);
		msOpp.InitForUse(num2);
		blockScs = base.Block as BlockStoneCoffinSection;
		updateSelectiveElements();
		particlePositions[0] = Pos.DownCopy(2);
		particlePositions[1] = particlePositions[0].AddCopy(blockScs.Orientation.Opposite);
		particlePositions[2] = Pos.AddCopy(blockScs.Orientation.GetCW());
		particlePositions[3] = Pos.AddCopy(blockScs.Orientation.GetCCW());
		particlePositions[4] = Pos.AddCopy(blockScs.Orientation.GetCW()).Add(blockScs.Orientation.Opposite);
		particlePositions[5] = Pos.AddCopy(blockScs.Orientation.GetCCW()).Add(blockScs.Orientation.Opposite);
		particlePositions[6] = Pos.UpCopy().Add(blockScs.Orientation.Opposite);
		particlePositions[7] = Pos.UpCopy();
		inv.SetSecondaryPos(Pos.AddCopy(blockScs.Orientation.Opposite));
	}

	public bool Interact(IPlayer byPlayer, bool preferThis)
	{
		bool shiftKey = byPlayer.WorldData.EntityControls.ShiftKey;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		BlockPos centerPos = Pos;
		if (shiftKey)
		{
			int num4 = 0;
			int num5 = int.MaxValue;
			int dt = 0;
			int wt = 0;
			int num6 = 0;
			int wtOpp = 0;
			num4 = ms.InCompleteBlockCount(Api.World, Pos, delegate(Block haveBlock, AssetLocation wantLoc)
			{
				string text = haveBlock.FirstCodePart();
				if ((text == "refractorybricks" || text == "refractorybrickgrating") && haveBlock.Variant["state"] == "damaged")
				{
					dt++;
				}
				else
				{
					wt++;
				}
			});
			if (num4 > 0 && blockScs.IsCompleteCoffin(Pos))
			{
				num5 = msOpp.InCompleteBlockCount(Api.World, Pos.AddCopy(blockScs.Orientation.Opposite), delegate(Block haveBlock, AssetLocation wantLoc)
				{
					string text = haveBlock.FirstCodePart();
					if ((text == "refractorybricks" || text == "refractorybrickgrating") && haveBlock.Variant["state"] == "damaged")
					{
						dt++;
					}
					else
					{
						wtOpp++;
					}
				});
			}
			if ((wtOpp <= 3 && wt < wtOpp) || (wtOpp > 3 && wt < wtOpp - 3) || (preferThis && wt <= wtOpp) || (preferThis && wt > 3 && wt <= wtOpp + 3))
			{
				num3 = num4;
				num = dt;
				num2 = wt;
				if (num4 > 0)
				{
					msHighlighted = ms;
				}
			}
			else
			{
				num3 = num5;
				num = num6;
				num2 = wtOpp;
				msHighlighted = msOpp;
				centerPos = Pos.AddCopy(blockScs.Orientation.Opposite);
			}
		}
		if (shiftKey && num3 > 0)
		{
			if (num2 > 0 && num > 0)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", num2, num));
			}
			else if (num2 > 0)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", num2));
			}
			else if (num == 1)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tile is damaged!", num));
			}
			else
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tiles are damaged!", num));
			}
			if (Api.Side == EnumAppSide.Client)
			{
				msHighlighted.HighlightIncompleteParts(Api.World, byPlayer, centerPos);
			}
			return false;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, byPlayer);
		}
		if (!shiftKey)
		{
			return false;
		}
		if (!blockScs.IsCompleteCoffin(Pos))
		{
			capi?.TriggerIngameError(this, "incomplete", Lang.Get("Cannot fill an incomplete coffin, place the other half first"));
			return false;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			if (IngotCount / 4 >= CoalLayerCount)
			{
				return AddCoal(activeHotbarSlot);
			}
			return AddIngot(activeHotbarSlot);
		}
		return true;
	}

	private bool AddCoal(ItemSlot slot)
	{
		if (CoalLayerCount >= 5)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("This stone coffin is full already"));
			return false;
		}
		CombustibleProperties combustibleProps = slot.Itemstack.Collectible.CombustibleProps;
		if (combustibleProps == null || combustibleProps.BurnTemperature < 1300)
		{
			capi?.TriggerIngameError(this, "wrongfuel", Lang.Get("Needs a layer of high-quality carbon-bearing material (coke or charcoal)"));
			return false;
		}
		if (slot.Itemstack.StackSize < 8)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("Each layer requires 8 pieces of fuel"));
			return false;
		}
		if (slot.TryPutInto(Api.World, inv[0], 8) == 0)
		{
			capi?.TriggerIngameError(this, "cannotmixfuels", Lang.Get("Cannot mix materials, it will mess with the carburisation process!"));
			return false;
		}
		updateSelectiveElements();
		MarkDirty(redrawOnClient: true);
		return true;
	}

	private bool AddIngot(ItemSlot slot)
	{
		if (IngotCount >= 16)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("This stone coffin is full already"));
			return false;
		}
		JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
		if (itemAttributes != null && !itemAttributes["carburizableProps"].Exists)
		{
			capi?.TriggerIngameError(this, "wrongfuel", Lang.Get("Next add some carburizable metal ingots"));
			return false;
		}
		ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, 1);
		if (slot.TryPutInto(inv[1], ref op) == 0)
		{
			capi?.TriggerIngameError(this, "cannotmixfuels", Lang.Get("Cannot mix ingots, it will mess with the carburisation process!"));
			return false;
		}
		updateSelectiveElements();
		MarkDirty(redrawOnClient: true);
		return true;
	}

	private void updateSelectiveElements()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		bool flag = inv[1].Itemstack?.Collectible.FirstCodePart(1) == "blistersteel";
		for (int i = 0; i < IngotCount; i++)
		{
			List<string> obj = ((i % 4 >= 2) ? list2 : list);
			int num = 1 + i / 4 * 2 + i % 2;
			obj.Add("Charcoal" + (num + 1) / 2 + "/" + ((i >= 7 && flag) ? "Steel" : "Ingot") + num);
		}
		for (int j = 0; j < CoalLayerCount; j++)
		{
			list.Add("Charcoal" + (j + 1));
			list2.Add("Charcoal" + (j + 1));
		}
		selectiveElementsMain = list.ToArray();
		selectiveElementsSecondary = list2.ToArray();
	}

	private void onServerTick1s(float dt)
	{
		if (receivesHeat)
		{
			Vec3d vec3d = Pos.ToVec3d().Add(0.5, 0.5, 0.5).Add(blockScs.Orientation.Opposite.Normalf.X, 0.0, blockScs.Orientation.Opposite.Normalf.Z);
			if (msOpp.InCompleteBlockCount(Api.World, Pos.AddCopy(blockScs.Orientation.Opposite)) == 0)
			{
				vec3d = Pos.AddCopy(blockScs.Orientation.Opposite).ToVec3d().Add(0.5, 0.5, 0.5)
					.Add(blockScs.Orientation.Normalf.X, 0.0, blockScs.Orientation.Normalf.Z);
			}
			Entity[] entitiesAround = Api.World.GetEntitiesAround(vec3d, 2.5f, 1f, (Entity e) => e.Alive && e is EntityAgent);
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
			onServerTick3s(dt);
		}
	}

	private void onServerTick3s(float dt)
	{
		BlockPos blockPos = Pos.DownCopy(2);
		BlockPos position = blockPos.AddCopy(blockScs.Orientation.Opposite);
		bool num = receivesHeat;
		bool structureComplete = StructureComplete;
		if (!receivesHeat)
		{
			totalHoursLastUpdate = Api.World.Calendar.TotalHours;
		}
		float num2 = ((Api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityCoalPile { IsBurning: not false } blockEntityCoalPile) ? blockEntityCoalPile.GetHoursLeft(totalHoursLastUpdate) : 0f);
		float num3 = ((Api.World.BlockAccessor.GetBlockEntity(position) is BlockEntityCoalPile { IsBurning: not false } blockEntityCoalPile2) ? blockEntityCoalPile2.GetHoursLeft(totalHoursLastUpdate) : 0f);
		receivesHeat = num2 > 0f && num3 > 0f;
		MultiblockStructure multiblockStructure = null;
		BlockPos centerPos = null;
		StructureComplete = false;
		if (ms.InCompleteBlockCount(Api.World, Pos) == 0)
		{
			multiblockStructure = ms;
			centerPos = Pos;
			StructureComplete = true;
		}
		else if (msOpp.InCompleteBlockCount(Api.World, Pos.AddCopy(blockScs.Orientation.Opposite)) == 0)
		{
			multiblockStructure = msOpp;
			centerPos = Pos.AddCopy(blockScs.Orientation.Opposite);
			StructureComplete = true;
		}
		if (num != receivesHeat || structureComplete != StructureComplete)
		{
			MarkDirty();
		}
		if (processComplete || !IsFull || !hasLid())
		{
			return;
		}
		if (receivesHeat)
		{
			if (!StructureComplete)
			{
				return;
			}
			double num4 = Api.World.Calendar.TotalHours - totalHoursLastUpdate;
			double num5 = Math.Max(0f, GameMath.Min((float)num4, num2, num3));
			progress += num5 / 160.0;
			totalHoursLastUpdate = Api.World.Calendar.TotalHours;
			float temperature = inv[1].Itemstack.Collectible.GetTemperature(Api.World, inv[1].Itemstack);
			float num6 = (float)(num4 * 500.0);
			inv[1].Itemstack.Collectible.SetTemperature(Api.World, inv[1].Itemstack, Math.Min(800f, temperature + num6));
			if (Math.Abs((float)tempStoneCoffin - temperature) > 25f)
			{
				tempStoneCoffin = (int)temperature;
				if (tempStoneCoffin > 500)
				{
					MarkDirty(redrawOnClient: true);
				}
			}
			MarkDirty();
		}
		if (!(progress >= 0.995))
		{
			return;
		}
		int stackSize = inv[1].Itemstack.StackSize;
		JsonItemStack jsonItemStack = inv[1].Itemstack.ItemAttributes?["carburizableProps"]["carburizedOutput"].AsObject<JsonItemStack>(null, base.Block.Code.Domain);
		if (jsonItemStack.Resolve(Api.World, "carburizable output"))
		{
			float temperature2 = inv[1].Itemstack.Collectible.GetTemperature(Api.World, inv[0].Itemstack);
			inv[0].Itemstack.StackSize -= 8;
			inv[1].Itemstack = jsonItemStack.ResolvedItemstack.Clone();
			inv[1].Itemstack.StackSize = stackSize;
			inv[1].Itemstack.Collectible.SetTemperature(Api.World, inv[1].Itemstack, temperature2);
		}
		MarkDirty();
		multiblockStructure.WalkMatchingBlocks(Api.World, centerPos, delegate(Block block, BlockPos pos)
		{
			float num7 = block.Attributes?["heatResistance"].AsFloat(1f) ?? 1f;
			if (Api.World.Rand.NextDouble() > (double)num7)
			{
				Block block2 = Api.World.GetBlock(block.CodeWithVariant("state", "damaged"));
				Api.World.BlockAccessor.SetBlock(block2.Id, pos);
			}
		});
		processComplete = true;
	}

	private bool hasLid()
	{
		if (Api.World.BlockAccessor.GetBlockAbove(Pos, 1, 1).FirstCodePart() == "stonecoffinlid")
		{
			return Api.World.BlockAccessor.GetBlockAbove(Pos.AddCopy(blockScs.Orientation.Opposite), 1, 1).FirstCodePart() == "stonecoffinlid";
		}
		return false;
	}

	private void onClientTick50ms(float dt)
	{
		if (!receivesHeat)
		{
			return;
		}
		receivesHeatSmooth = GameMath.Clamp(receivesHeatSmooth + (receivesHeat ? (dt / 10f) : ((0f - dt) / 3f)), 0f, 1f);
		if (receivesHeatSmooth == 0f)
		{
			return;
		}
		Random rand = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			int num = Math.Min(Entity.FireParticleProps.Length - 1, Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
			AdvancedParticleProperties advancedParticleProperties = Entity.FireParticleProps[num];
			for (int j = 0; j < particlePositions.Length; j++)
			{
				BlockPos blockPos = particlePositions[j];
				if (j >= 6)
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
					}) / 2f;
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

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		int num = inv[0]?.StackSize ?? 0;
		int num2 = inv[1]?.StackSize ?? 0;
		base.FromTreeAttributes(tree, worldAccessForResolve);
		receivesHeat = tree.GetBool("receivesHeat");
		totalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate");
		progress = tree.GetDouble("progress");
		processComplete = tree.GetBool("processComplete");
		StructureComplete = tree.GetBool("structureComplete");
		tempStoneCoffin = tree.GetInt("tempStoneCoffin");
		if (worldAccessForResolve.Api.Side == EnumAppSide.Client && (num != (inv[0]?.StackSize ?? 0) || num2 != (inv[1]?.StackSize ?? 0)))
		{
			ItemStack itemStack = inv[1]?.Itemstack;
			if (itemStack != null && itemStack.Collectible == null)
			{
				itemStack.ResolveBlockOrItem(worldAccessForResolve);
			}
			updateSelectiveElements();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("receivesHeat", receivesHeat);
		tree.SetDouble("totalHoursLastUpdate", totalHoursLastUpdate);
		tree.SetDouble("progress", progress);
		tree.SetBool("processComplete", processComplete);
		tree.SetBool("structureComplete", StructureComplete);
		tree.SetInt("tempStoneCoffin", tempStoneCoffin);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (hasLid())
		{
			return false;
		}
		Shape cachedShape = capi.TesselatorManager.GetCachedShape(base.Block.Shape.Base);
		tessThreadTesselator.TesselateShape(base.Block, cachedShape, out var modeldata, null, null, selectiveElementsMain);
		tessThreadTesselator.TesselateShape(base.Block, cachedShape, out var modeldata2, null, null, selectiveElementsSecondary);
		if (blockScs.Orientation == BlockFacing.EAST)
		{
			modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -(float)Math.PI / 2f, 0f);
			modeldata2.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -(float)Math.PI / 2f, 0f);
		}
		modeldata2.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)Math.PI, 0f);
		modeldata2.Translate(blockScs.Orientation.Opposite.Normalf);
		mesher.AddMeshData(modeldata);
		mesher.AddMeshData(modeldata2);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (processComplete)
		{
			dsc.AppendLine(Lang.Get("Carburization process complete. Break to retrieve blister steel."));
			return;
		}
		if (IsFull)
		{
			if (!hasLid())
			{
				dsc.AppendLine(Lang.Get("Stone coffin lid is missing"));
			}
			else
			{
				if (!StructureComplete)
				{
					dsc.AppendLine(Lang.Get("Structure incomplete! Can't get hot enough, carburization paused."));
					return;
				}
				if (receivesHeat)
				{
					dsc.AppendLine(Lang.Get("Okay! Receives heat!"));
				}
				else
				{
					dsc.AppendLine(Lang.Get("Ready to be fired. Ignite a pile of coal below each stone coffin half."));
				}
			}
		}
		if (progress > 0.0)
		{
			dsc.AppendLine(Lang.Get("Carburization: {0}% complete", (int)(progress * 100.0)));
		}
	}
}
