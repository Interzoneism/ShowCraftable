using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class BlockEntityQuern : BlockEntityOpenableContainer
{
	private static SimpleParticleProperties FlourParticles;

	private static SimpleParticleProperties FlourDustParticles;

	private ILoadedSound ambientSound;

	internal InventoryQuern inventory;

	public float inputGrindTime;

	public float prevInputGrindTime;

	private GuiDialogBlockEntityQuern clientDialog;

	private QuernTopRenderer renderer;

	private bool automated;

	private BEBehaviorMPConsumer mpc;

	private float prevSpeed = float.NaN;

	private Dictionary<string, long> playersGrinding = new Dictionary<string, long>();

	private int quantityPlayersGrinding;

	private int nowOutputFace;

	private bool beforeGrinding;

	public string Material => base.Block.LastCodePart();

	public float GrindSpeed
	{
		get
		{
			if (quantityPlayersGrinding > 0)
			{
				return 1f;
			}
			if (automated && mpc.Network != null)
			{
				return mpc.TrueSpeed;
			}
			return 0f;
		}
	}

	private MeshData quernBaseMesh
	{
		get
		{
			Api.ObjectCache.TryGetValue("quernbasemesh-" + Material, out var value);
			return (MeshData)value;
		}
		set
		{
			Api.ObjectCache["quernbasemesh-" + Material] = value;
		}
	}

	private MeshData quernTopMesh
	{
		get
		{
			Api.ObjectCache.TryGetValue("querntopmesh-" + Material, out var value);
			return (MeshData)value;
		}
		set
		{
			Api.ObjectCache["querntopmesh-" + Material] = value;
		}
	}

	public override string InventoryClassName => "quern";

	public virtual string DialogTitle => Lang.Get("Quern");

	public override InventoryBase Inventory => inventory;

	public ItemSlot InputSlot => inventory[0];

	public ItemSlot OutputSlot => inventory[1];

	public ItemStack InputStack
	{
		get
		{
			return inventory[0].Itemstack;
		}
		set
		{
			inventory[0].Itemstack = value;
			inventory[0].MarkDirty();
		}
	}

	public ItemStack OutputStack
	{
		get
		{
			return inventory[1].Itemstack;
		}
		set
		{
			inventory[1].Itemstack = value;
			inventory[1].MarkDirty();
		}
	}

	public GrindingProperties InputGrindProps
	{
		get
		{
			ItemSlot itemSlot = inventory[0];
			if (itemSlot.Itemstack == null)
			{
				return null;
			}
			return itemSlot.Itemstack.Collectible.GrindingProps;
		}
	}

	static BlockEntityQuern()
	{
		FlourParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
		FlourParticles.AddPos.Set(1.0625, 0.0, 1.0625);
		FlourParticles.AddQuantity = 20f;
		FlourParticles.MinVelocity.Set(-0.25f, 0f, -0.25f);
		FlourParticles.AddVelocity.Set(0.5f, 1f, 0.5f);
		FlourParticles.WithTerrainCollision = true;
		FlourParticles.ParticleModel = EnumParticleModel.Cube;
		FlourParticles.LifeLength = 1.5f;
		FlourParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.4f);
		FlourDustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
		FlourDustParticles.AddPos.Set(1.0625, 0.0, 1.0625);
		FlourDustParticles.AddQuantity = 5f;
		FlourDustParticles.MinVelocity.Set(-0.05f, 0f, -0.05f);
		FlourDustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
		FlourDustParticles.WithTerrainCollision = false;
		FlourDustParticles.ParticleModel = EnumParticleModel.Quad;
		FlourDustParticles.LifeLength = 1.5f;
		FlourDustParticles.SelfPropelled = true;
		FlourDustParticles.GravityEffect = 0f;
		FlourDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
		FlourDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
	}

	public virtual float maxGrindingTime()
	{
		return 4f;
	}

	public BlockEntityQuern()
	{
		inventory = new InventoryQuern(null, null);
		inventory.SlotModified += OnSlotModifid;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.LateInitialize("quern-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
		RegisterGameTickListener(Every100ms, 100);
		RegisterGameTickListener(Every500ms, 500);
		if (api.Side == EnumAppSide.Client)
		{
			renderer = new QuernTopRenderer(api as ICoreClientAPI, Pos, GenMesh("top"));
			renderer.mechPowerPart = mpc;
			if (automated)
			{
				renderer.ShouldRender = true;
				renderer.ShouldRotateAutomated = true;
			}
			(api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "quern");
			if (quernBaseMesh == null)
			{
				quernBaseMesh = GenMesh();
			}
			if (quernTopMesh == null)
			{
				quernTopMesh = GenMesh("top");
			}
		}
	}

	public void updateSoundState(bool nowGrinding)
	{
		if (nowGrinding)
		{
			startSound();
		}
		else
		{
			stopSound();
		}
	}

	public void startSound()
	{
		if (ambientSound == null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/block/quern.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 0.75f
				});
				ambientSound.Start();
			}
		}
	}

	public void stopSound()
	{
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
			ambientSound = null;
		}
	}

	public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
	{
		base.CreateBehaviors(block, worldForResolve);
		mpc = GetBehavior<BEBehaviorMPConsumer>();
		if (mpc == null)
		{
			return;
		}
		mpc.OnConnected = delegate
		{
			automated = true;
			quantityPlayersGrinding = 0;
			if (renderer != null)
			{
				renderer.ShouldRender = true;
				renderer.ShouldRotateAutomated = true;
			}
		};
		mpc.OnDisconnected = delegate
		{
			automated = false;
			if (renderer != null)
			{
				renderer.ShouldRender = false;
				renderer.ShouldRotateAutomated = false;
			}
		};
	}

	public void IsGrinding(IPlayer byPlayer)
	{
		SetPlayerGrinding(byPlayer, playerGrinding: true);
	}

	private void Every100ms(float dt)
	{
		float grindSpeed = GrindSpeed;
		if (Api.Side == EnumAppSide.Client)
		{
			if (InputStack != null)
			{
				float minQuantity = 1f * grindSpeed;
				float addQuantity = 5f * grindSpeed;
				float minQuantity2 = 1f * grindSpeed;
				float addQuantity2 = 20f * grindSpeed;
				FlourDustParticles.Color = (FlourParticles.Color = InputStack.Collectible.GetRandomColor(Api as ICoreClientAPI, InputStack));
				FlourDustParticles.Color &= 16777215;
				FlourDustParticles.Color |= -939524096;
				FlourDustParticles.MinQuantity = minQuantity;
				FlourDustParticles.AddQuantity = addQuantity;
				FlourDustParticles.MinPos.Set((float)Pos.X - 1f / 32f, (float)Pos.Y + 0.6875f, (float)Pos.Z - 1f / 32f);
				FlourDustParticles.MinVelocity.Set(-0.1f, 0f, -0.1f);
				FlourDustParticles.AddVelocity.Set(0.2f, 0.2f, 0.2f);
				FlourParticles.MinPos.Set((float)Pos.X - 1f / 32f, (float)Pos.Y + 0.6875f, (float)Pos.Z - 1f / 32f);
				FlourParticles.AddQuantity = addQuantity2;
				FlourParticles.MinQuantity = minQuantity2;
				Api.World.SpawnParticles(FlourParticles);
				Api.World.SpawnParticles(FlourDustParticles);
			}
			if (ambientSound != null && automated && mpc.TrueSpeed != prevSpeed)
			{
				prevSpeed = mpc.TrueSpeed;
				ambientSound.SetPitch((0.5f + prevSpeed) * 0.9f);
				ambientSound.SetVolume(Math.Min(1f, prevSpeed * 3f));
			}
			else
			{
				prevSpeed = float.NaN;
			}
		}
		else if (CanGrind() && grindSpeed > 0f)
		{
			inputGrindTime += dt * grindSpeed;
			if (inputGrindTime >= maxGrindingTime())
			{
				grindInput();
				inputGrindTime = 0f;
			}
			MarkDirty();
		}
	}

	private void grindInput()
	{
		ItemStack itemStack = InputGrindProps.GroundStack.ResolvedItemstack.Clone();
		if (OutputSlot.Itemstack == null)
		{
			OutputSlot.Itemstack = itemStack;
		}
		else if (OutputSlot.Itemstack.Collectible.GetMergableQuantity(OutputSlot.Itemstack, itemStack, EnumMergePriority.AutoMerge) > 0)
		{
			OutputSlot.Itemstack.StackSize += itemStack.StackSize;
		}
		else
		{
			BlockFacing blockFacing = BlockFacing.HORIZONTALS[nowOutputFace];
			nowOutputFace = (nowOutputFace + 1) % 4;
			if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(blockFacing)).Replaceable < 6000)
			{
				return;
			}
			Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5 + (double)blockFacing.Normalf.X * 0.7, 0.75, 0.5 + (double)blockFacing.Normalf.Z * 0.7), new Vec3d(blockFacing.Normalf.X * 0.02f, 0.0, blockFacing.Normalf.Z * 0.02f));
		}
		InputSlot.TakeOut(1);
		InputSlot.MarkDirty();
		OutputSlot.MarkDirty();
	}

	private void Every500ms(float dt)
	{
		if (Api.Side == EnumAppSide.Server && (GrindSpeed > 0f || prevInputGrindTime != inputGrindTime) && inventory[0].Itemstack?.Collectible.GrindingProps != null)
		{
			MarkDirty();
		}
		prevInputGrindTime = inputGrindTime;
		foreach (KeyValuePair<string, long> item in playersGrinding)
		{
			if (Api.World.ElapsedMilliseconds - item.Value > 1000)
			{
				playersGrinding.Remove(item.Key);
				break;
			}
		}
	}

	public void SetPlayerGrinding(IPlayer player, bool playerGrinding)
	{
		if (!automated)
		{
			if (playerGrinding)
			{
				playersGrinding[player.PlayerUID] = Api.World.ElapsedMilliseconds;
			}
			else
			{
				playersGrinding.Remove(player.PlayerUID);
			}
			quantityPlayersGrinding = playersGrinding.Count;
		}
		updateGrindingState();
	}

	private void updateGrindingState()
	{
		if (Api?.World == null)
		{
			return;
		}
		bool flag = quantityPlayersGrinding > 0 || (automated && mpc.TrueSpeed > 0f);
		if (flag != beforeGrinding)
		{
			if (renderer != null)
			{
				renderer.ShouldRotateManual = quantityPlayersGrinding > 0;
			}
			Api.World.BlockAccessor.MarkBlockDirty(Pos, OnRetesselated);
			updateSoundState(flag);
			if (Api.Side == EnumAppSide.Server)
			{
				MarkDirty();
			}
		}
		beforeGrinding = flag;
	}

	private void OnSlotModifid(int slotid)
	{
		if (Api is ICoreClientAPI)
		{
			clientDialog.Update(inputGrindTime, maxGrindingTime());
		}
		if (slotid == 0)
		{
			if (InputSlot.Empty)
			{
				inputGrindTime = 0f;
			}
			MarkDirty();
			if (clientDialog != null && clientDialog.IsOpened())
			{
				clientDialog.SingleComposer.ReCompose();
			}
		}
	}

	private void OnRetesselated()
	{
		if (renderer != null)
		{
			renderer.ShouldRender = quantityPlayersGrinding > 0 || automated;
		}
	}

	internal MeshData GenMesh(string type = "base")
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		if (block.BlockId == 0)
		{
			return null;
		}
		((ICoreClientAPI)Api).Tesselator.TesselateShape(block, Shape.TryGet(Api, "shapes/block/stone/quern/" + type + ".json"), out var modeldata);
		return modeldata;
	}

	public bool CanGrind()
	{
		if (InputGrindProps == null)
		{
			return false;
		}
		return true;
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel.SelectionBoxIndex == 1)
		{
			return false;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			toggleInventoryDialogClient(byPlayer, delegate
			{
				clientDialog = new GuiDialogBlockEntityQuern(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
				clientDialog.Update(inputGrindTime, maxGrindingTime());
				return clientDialog;
			});
		}
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(player, packetid, data);
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid == 1001)
		{
			(Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
			invDialog?.TryClose();
			invDialog?.Dispose();
			invDialog = null;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
		if (Api != null)
		{
			Inventory.AfterBlocksLoaded(Api.World);
		}
		inputGrindTime = tree.GetFloat("inputGrindTime");
		nowOutputFace = tree.GetInt("nowOutputFace");
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			List<int> clientIds = new List<int>((tree["clientIdsGrinding"] as IntArrayAttribute).value);
			quantityPlayersGrinding = clientIds.Count;
			string[] array = playersGrinding.Keys.ToArray();
			foreach (string text in array)
			{
				IPlayer player = Api.World.PlayerByUid(text);
				if (!clientIds.Contains(player.ClientId))
				{
					playersGrinding.Remove(text);
				}
				else
				{
					clientIds.Remove(player.ClientId);
				}
			}
			int j;
			for (j = 0; j < clientIds.Count; j++)
			{
				IPlayer player2 = worldForResolving.AllPlayers.FirstOrDefault((IPlayer p) => p.ClientId == clientIds[j]);
				if (player2 != null)
				{
					playersGrinding.Add(player2.PlayerUID, worldForResolving.ElapsedMilliseconds);
				}
			}
			updateGrindingState();
		}
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && clientDialog != null)
		{
			clientDialog.Update(inputGrindTime, maxGrindingTime());
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ITreeAttribute treeAttribute = new TreeAttribute();
		Inventory.ToTreeAttributes(treeAttribute);
		tree["inventory"] = treeAttribute;
		tree.SetFloat("inputGrindTime", inputGrindTime);
		tree.SetInt("nowOutputFace", nowOutputFace);
		List<int> list = new List<int>();
		foreach (KeyValuePair<string, long> item in playersGrinding)
		{
			IPlayer player = Api.World.PlayerByUid(item.Key);
			if (player != null)
			{
				list.Add(player.ClientId);
			}
		}
		tree["clientIdsGrinding"] = new IntArrayAttribute(list.ToArray());
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
		}
		clientDialog?.TryClose();
		renderer?.Dispose();
		renderer = null;
	}

	~BlockEntityQuern()
	{
		if (ambientSound != null)
		{
			ambientSound.Dispose();
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot item in Inventory)
		{
			if (item.Itemstack != null)
			{
				if (item.Itemstack.Class == EnumItemClass.Item)
				{
					itemIdMapping[item.Itemstack.Item.Id] = item.Itemstack.Item.Code;
				}
				else
				{
					blockIdMapping[item.Itemstack.Block.BlockId] = item.Itemstack.Block.Code;
				}
				item.Itemstack?.Collectible.OnStoreCollectibleMappings(Api.World, item, blockIdMapping, itemIdMapping);
			}
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (ItemSlot item in Inventory)
		{
			if (item.Itemstack != null)
			{
				if (!item.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
				{
					item.Itemstack = null;
				}
				item.Itemstack?.Collectible.OnLoadCollectibleMappings(worldForResolve, item, oldBlockIdMapping, oldItemIdMapping, resolveImports);
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (base.Block == null)
		{
			return false;
		}
		mesher.AddMeshData(quernBaseMesh);
		if (quantityPlayersGrinding == 0 && !automated)
		{
			mesher.AddMeshData(quernTopMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, renderer.AngleRad, 0f).Translate(0f, 0.6875f, 0f));
		}
		return true;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
			ambientSound = null;
		}
	}
}
