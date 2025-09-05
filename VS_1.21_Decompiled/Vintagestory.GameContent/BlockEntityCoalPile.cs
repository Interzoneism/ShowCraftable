using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCoalPile : BlockEntityItemPile, ITexPositionSource, IHeatSource
{
	private static SimpleParticleProperties smokeParticles;

	private static SimpleParticleProperties smallMetalSparks;

	private bool burning;

	private double burnStartTotalHours;

	private ICoreClientAPI capi;

	private ILoadedSound ambientSound;

	private float cokeConversionRate;

	public float BurnHoursPerLayer = 4f;

	private long listenerId;

	private static BlockFacing[] facings;

	private bool isCokable;

	public override AssetLocation SoundLocation => new AssetLocation("sounds/block/charcoal");

	public override string BlockCode => "coalpile";

	public override int MaxStackSize => 16;

	public override int DefaultTakeQuantity => 2;

	public override int BulkTakeQuantity => 2;

	public int Layers
	{
		get
		{
			if (inventory[0].StackSize != 1)
			{
				return inventory[0].StackSize / 2;
			}
			return 1;
		}
	}

	public bool IsBurning => burning;

	public bool CanIgnite => !burning;

	public int BurnTemperature => inventory[0].Itemstack.Collectible.CombustibleProps.BurnTemperature;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (IsBurning)
			{
				return capi.BlockTextureAtlas.Positions[capi.World.GetBlock(new AssetLocation("ember")).FirstTextureInventory.Baked.TextureSubId];
			}
			string path = inventory[0].Itemstack.Collectible.Code.Path;
			return capi.BlockTextureAtlas.Positions[base.Block.Textures[path].Baked.TextureSubId];
		}
	}

	static BlockEntityCoalPile()
	{
		facings = (BlockFacing[])BlockFacing.ALLFACES.Clone();
		smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(150, 40, 40, 40), new Vec3d(), new Vec3d(1.0, 0.0, 1.0), new Vec3f(-1f / 32f, 0.1f, -1f / 32f), new Vec3f(1f / 32f, 0.1f, 1f / 32f), 2f, -1f / 160f, 0.2f, 1f, EnumParticleModel.Quad);
		smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		smokeParticles.SelfPropelled = true;
		smokeParticles.AddPos.Set(1.0, 0.0, 1.0);
		smallMetalSparks = new SimpleParticleProperties(0.2f, 1f, ColorUtil.ToRgba(255, 255, 150, 0), new Vec3d(), new Vec3d(), new Vec3f(-2f, 2f, -2f), new Vec3f(2f, 5f, 2f), 0.04f, 1f, 0.2f, 0.25f);
		smallMetalSparks.WithTerrainCollision = false;
		smallMetalSparks.VertexFlags = 150;
		smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.2f);
		smallMetalSparks.SelfPropelled = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		updateBurningState();
	}

	public void TryIgnite()
	{
		if (!burning)
		{
			burning = true;
			burnStartTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty();
			updateBurningState();
		}
	}

	public void Extinguish()
	{
		if (burning)
		{
			burning = false;
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.0, null, randomizePitch: false, 16f);
		}
	}

	private void updateBurningState()
	{
		if (!burning)
		{
			return;
		}
		if (Api.World.Side == EnumAppSide.Client)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/embers.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				if (ambientSound != null)
				{
					ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
					ambientSound.Start();
				}
			}
			listenerId = RegisterGameTickListener(onBurningTickClient, 100);
		}
		else
		{
			listenerId = RegisterGameTickListener(onBurningTickServer, 10000);
		}
	}

	public static void SpawnBurningCoalParticles(ICoreAPI api, Vec3d pos, float addX = 1f, float addZ = 1f)
	{
		smokeParticles.MinQuantity = 0.25f;
		smokeParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -15f);
		smokeParticles.AddQuantity = 0f;
		smokeParticles.MinPos.Set(pos.X, pos.Y - 0.10000000149011612, pos.Z);
		smokeParticles.AddPos.Set(addX, 0.0, addZ);
		smallMetalSparks.MinPos.Set(pos.X, pos.Y, pos.Z);
		smallMetalSparks.AddPos.Set(addX, 0.10000000149011612, addZ);
		api.World.SpawnParticles(smallMetalSparks);
		int num = 30 + api.World.Rand.Next(30);
		smokeParticles.Color = ColorUtil.ToRgba(150, num, num, num);
		api.World.SpawnParticles(smokeParticles);
	}

	private void onBurningTickClient(float dt)
	{
		if (burning && Api.World.Rand.NextDouble() < 0.93)
		{
			if (isCokable)
			{
				smokeParticles.MinQuantity = 1f;
				smokeParticles.AddQuantity = 0f;
				smokeParticles.MinPos.Set(Pos.X, (float)(Pos.Y + 2) + 0.0625f, Pos.Z);
				int num = 30 + Api.World.Rand.Next(30);
				smokeParticles.Color = ColorUtil.ToRgba(150, num, num, num);
				Api.World.SpawnParticles(smokeParticles);
			}
			else
			{
				SpawnBurningCoalParticles(Api, Pos.ToVec3d().Add(0.0, (float)Layers / 8f, 0.0));
			}
		}
	}

	public float GetHoursLeft(double startTotalHours)
	{
		double num = startTotalHours - burnStartTotalHours;
		return (float)((double)((float)inventory[0].StackSize / 2f * BurnHoursPerLayer) - num);
	}

	private void onBurningTickServer(float dt)
	{
		facings.Shuffle(Api.World.Rand);
		BlockFacing[] array = facings;
		foreach (BlockFacing facing in array)
		{
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(facing));
			if (blockEntity is BlockEntityCoalPile blockEntityCoalPile)
			{
				blockEntityCoalPile.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (blockEntity is BlockEntityGroundStorage blockEntityGroundStorage)
			{
				blockEntityGroundStorage.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
		}
		cokeConversionRate = (inventory[0].Itemstack?.ItemAttributes?["cokeConversionRate"].AsFloat()).GetValueOrDefault();
		if (cokeConversionRate > 0f && (isCokable = TestCokable()))
		{
			if (Api.World.Calendar.TotalHours - burnStartTotalHours > 12.0)
			{
				inventory[0].Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("coke")), (int)((float)inventory[0].StackSize * cokeConversionRate));
				burning = false;
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				MarkDirty();
			}
			return;
		}
		bool flag = false;
		while (Api.World.Calendar.TotalHours - burnStartTotalHours > (double)(BurnHoursPerLayer / 2f))
		{
			burnStartTotalHours += BurnHoursPerLayer / 2f;
			inventory[0].TakeOut(1);
			if (inventory[0].Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				break;
			}
			flag = true;
		}
		if (flag)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	private bool TestCokable()
	{
		IBlockAccessor blockAccessor = Api.World.BlockAccessor;
		bool flag = false;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			Block block = blockAccessor.GetBlock(Pos.AddCopy(facing));
			flag |= block is BlockCokeOvenDoor && block.Variant["state"] == "closed";
		}
		int centerCount = 0;
		int cornerCount = 0;
		blockAccessor.WalkBlocks(Pos.AddCopy(-1, -1, -1), Pos.AddCopy(1, 1, 1), delegate(Block block2, int x, int y, int z)
		{
			int num = Math.Abs(Pos.X - x);
			int num2 = Math.Abs(Pos.Z - z);
			bool flag2 = num == 1 && num2 == 1;
			JsonObject attributes = block2.Attributes;
			if (attributes != null && attributes["cokeOvenViable"].AsBool())
			{
				centerCount += ((!flag2) ? 1 : 0);
				cornerCount += (flag2 ? 1 : 0);
			}
		});
		if (flag && centerCount >= 12 && cornerCount >= 8)
		{
			return blockAccessor.GetBlock(Pos.UpCopy()).Attributes?["cokeOvenViable"].AsBool() ?? false;
		}
		return false;
	}

	public override bool OnPlayerInteract(IPlayer byPlayer)
	{
		if (burning && !byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		bool result = base.OnPlayerInteract(byPlayer);
		TriggerPileChanged();
		return result;
	}

	private void TriggerPileChanged()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		int num = 4;
		BlockCoalPile blockCoalPile = Api.World.BlockAccessor.GetBlock(Pos.DownCopy()) as BlockCoalPile;
		int num2 = blockCoalPile?.GetLayercount(Api.World, Pos.DownCopy()) ?? 0;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			BlockPos blockPos = Pos.AddCopy(facing);
			Block block = Api.World.BlockAccessor.GetBlock(blockPos);
			BlockCoalPile obj = Api.World.BlockAccessor.GetBlock(blockPos) as BlockCoalPile;
			int num3 = obj?.GetLayercount(Api.World, blockPos) ?? 0;
			int num4 = ((block.Replaceable > 6000) ? (Layers - num) : 0);
			int num5 = ((obj != null) ? (Layers - num3 - num) : 0);
			BlockCoalPile blockCoalPile2 = Api.World.BlockAccessor.GetBlock(blockPos.DownCopy()) as BlockCoalPile;
			int num6 = blockCoalPile2?.GetLayercount(Api.World, blockPos.DownCopy()) ?? 0;
			int num7 = ((blockCoalPile != null && blockCoalPile2 != null) ? (Layers + num2 - num6 - num) : 0);
			int num8 = GameMath.Max(num4, num5, num7);
			if (Api.World.Rand.NextDouble() < (double)((float)num8 / (float)num) && TryPartialCollapse(blockPos.UpCopy(), 2))
			{
				break;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		bool flag = burning;
		burning = tree.GetBool("burning");
		burnStartTotalHours = tree.GetDouble("lastTickTotalHours");
		isCokable = tree.GetBool("isCokable");
		if (!burning)
		{
			if (listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			ambientSound?.Stop();
			listenerId = 0L;
		}
		if (Api != null && Api.Side == EnumAppSide.Client && !flag && burning)
		{
			updateBurningState();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("burning", burning);
		tree.SetDouble("lastTickTotalHours", burnStartTotalHours);
		tree.SetBool("isCokable", isCokable);
	}

	public bool MergeWith(TreeAttribute blockEntityAttributes)
	{
		InventoryGeneric inventoryGeneric = new InventoryGeneric(1, BlockCode, null, null, null);
		inventoryGeneric.FromTreeAttributes(blockEntityAttributes.GetTreeAttribute("inventory"));
		inventoryGeneric.Api = Api;
		inventoryGeneric.ResolveBlocksOrItems();
		if (!inventory[0].Empty && inventoryGeneric[0].Itemstack.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			int num = Math.Min(inventoryGeneric[0].StackSize, Math.Max(0, MaxStackSize - inventory[0].StackSize));
			inventory[0].Itemstack.StackSize += num;
			inventoryGeneric[0].TakeOut(num);
			if (inventoryGeneric[0].StackSize > 0)
			{
				BlockPos pos = Pos.UpCopy();
				if (Api.World.BlockAccessor.GetBlock(pos).Replaceable > 6000)
				{
					((IBlockItemPile)base.Block).Construct(inventoryGeneric[0], Api.World, pos, null);
				}
			}
			MarkDirty(redrawOnClient: true);
			TriggerPileChanged();
		}
		return true;
	}

	private bool TryPartialCollapse(BlockPos pos, int quantity)
	{
		if (inventory[0].Empty)
		{
			return false;
		}
		IWorldAccessor world = Api.World;
		if (world.Side == EnumAppSide.Server && !((world as IServerWorldAccessor).Api as ICoreServerAPI).World.Config.GetBool("allowFallingBlocks"))
		{
			return false;
		}
		if ((IsReplacableBeneath(world, pos) || IsReplacableBeneathAndSideways(world, pos)) && world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling && ((EntityBlockFalling)e).initialPos.Equals(pos)) == null)
		{
			ItemStack itemstack = inventory[0].TakeOut(quantity);
			ItemStack itemstack2 = inventory[0].Itemstack;
			inventory[0].Itemstack = itemstack;
			EntityBlockFalling entityBlockFalling = new EntityBlockFalling(base.Block, this, pos, null, 0f, canFallSideways: true, 0.5f);
			entityBlockFalling.maxSpawnHeightForParticles = 0.3f;
			entityBlockFalling.DoRemoveBlock = false;
			world.SpawnEntity(entityBlockFalling);
			entityBlockFalling.ServerPos.Y -= 0.25;
			entityBlockFalling.Pos.Y -= 0.25;
			inventory[0].Itemstack = itemstack2;
			if (inventory.Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
			}
			return true;
		}
		return false;
	}

	private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
	{
		for (int i = 0; i < 4; i++)
		{
			BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
			Block blockOrNull = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y, pos.Z + blockFacing.Normali.Z);
			Block blockOrNull2 = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y - 1, pos.Z + blockFacing.Normali.Z);
			if (blockOrNull != null && blockOrNull2 != null && blockOrNull.Replaceable >= 6000 && blockOrNull2.Replaceable >= 6000)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
	{
		Block blockBelow = world.BlockAccessor.GetBlockBelow(pos);
		if (blockBelow != null)
		{
			return blockBelow.Replaceable > 6000;
		}
		return false;
	}

	public void GetDecalMesh(ITexPositionSource decalTexSource, out MeshData meshdata)
	{
		int val = Layers * 2;
		Shape cachedShape = capi.TesselatorManager.GetCachedShape(new AssetLocation("block/basic/layers/" + GameMath.Clamp(val, 2, 16) + "voxel"));
		capi.Tesselator.TesselateShape("coalpile", cachedShape, out meshdata, decalTexSource, null, 0, 0, 0);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		lock (inventoryLock)
		{
			if (!inventory[0].Empty)
			{
				int val = Layers * 2;
				if (mesher is EntityBlockFallingRenderer)
				{
					val = 2;
				}
				Shape cachedShape = capi.TesselatorManager.GetCachedShape(new AssetLocation("block/basic/layers/" + GameMath.Clamp(val, 2, 16) + "voxel"));
				capi.Tesselator.TesselateShape("coalpile", cachedShape, out var modeldata, this, null, 0, 0, 0);
				if (burning)
				{
					for (int i = 0; i < modeldata.FlagsCount; i++)
					{
						modeldata.Flags[i] |= 196;
					}
				}
				mesher.AddMeshData(modeldata);
			}
		}
		return true;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		ambientSound?.Dispose();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (!burning)
		{
			base.OnBlockBroken(byPlayer);
		}
		ambientSound?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ambientSound?.Dispose();
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return (IsBurning && !isCokable) ? 10 : 0;
	}
}
