using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class Block : CollectibleObject
{
	public static readonly CompositeShape DefaultCubeShape = new CompositeShape
	{
		Base = new AssetLocation("block/basic/cube")
	};

	public static readonly string[] DefaultAllowAllSpawns = new string[1] { "*" };

	public static Cuboidf DefaultCollisionBox = new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f);

	public static readonly Cuboidf[] DefaultCollisionSelectionBoxes = new Cuboidf[1] { DefaultCollisionBox };

	public BlockTagArray Tags = BlockTagArray.Empty;

	public int BlockId;

	public EnumDrawType DrawType = EnumDrawType.JSON;

	public EnumChunkRenderPass RenderPass;

	public bool Ambientocclusion = true;

	public float WalkSpeedMultiplier = 1f;

	public float DragMultiplier = 1f;

	public bool PartialSelection;

	public BlockSounds Sounds;

	public VertexFlags VertexFlags;

	public bool Frostable;

	public int LightAbsorption;

	public bool PlacedPriorityInteract;

	public int Replaceable;

	public int Fertility;

	public int RequiredMiningTier;

	public float Resistance = 2f;

	public EnumBlockMaterial BlockMaterial = EnumBlockMaterial.Stone;

	public EnumRandomizeAxes RandomizeAxes;

	public int RandomDrawOffset;

	public bool RandomizeRotations;

	public float RandomSizeAdjust;

	public bool alternatingVOffset;

	public int alternatingVOffsetFaces;

	public CompositeShape ShapeInventory;

	public CompositeShape Shape = DefaultCubeShape;

	public CompositeShape Lod0Shape;

	public CompositeShape Lod2Shape;

	public MeshData Lod0Mesh;

	public MeshData Lod2Mesh;

	public bool DoNotRenderAtLod2;

	public IDictionary<string, CompositeTexture> Textures;

	public BakedCompositeTexture[][] FastTextureVariants;

	public IDictionary<string, CompositeTexture> TexturesInventory;

	public SmallBoolArray SideOpaque = new SmallBoolArray(63);

	public SmallBoolArray SideSolid = new SmallBoolArray(63);

	public SmallBoolArray SideAo = new SmallBoolArray(63);

	public byte EmitSideAo;

	public string[] AllowSpawnCreatureGroups = DefaultAllowAllSpawns;

	public bool AllCreaturesAllowed;

	public EnumFaceCullMode FaceCullMode;

	public string ClimateColorMap;

	public ColorMap ClimateColorMapResolved;

	public string SeasonColorMap;

	public ColorMap SeasonColorMapResolved;

	public bool ShapeUsesColormap;

	public bool LoadColorMapAnyway;

	public int ExtraColorBits;

	public Cuboidf[] CollisionBoxes = DefaultCollisionSelectionBoxes;

	public Cuboidf[] SelectionBoxes = DefaultCollisionSelectionBoxes;

	public Cuboidf[] ParticleCollisionBoxes;

	public bool Climbable;

	public bool RainPermeable;

	public int LiquidLevel;

	public string LiquidCode;

	public bool HasAlternates;

	public bool HasTiles;

	public BlockBehavior[] BlockBehaviors = Array.Empty<BlockBehavior>();

	public BlockEntityBehaviorType[] BlockEntityBehaviors = Array.Empty<BlockEntityBehaviorType>();

	public BlockDropItemStack[] Drops;

	public bool SplitDropStacks = true;

	public BlockCropProperties CropProps;

	public string EntityClass;

	public bool CustomBlockLayerHandler;

	public bool CanStep = true;

	public bool AllowStepWhenStuck;

	public byte decorBehaviorFlags;

	public float DecorThickness;

	public float InteractionHelpYOffset = 0.9f;

	public int TextureSubIdForBlockColor = -1;

	private float humanoidTraversalCost;

	public int IceCheckOffset;

	protected static string[] miningTierNames = new string[7] { "tier_hands", "tier_stone", "tier_copper", "tier_bronze", "tier_iron", "tier_steel", "tier_titanium" };

	public Block notSnowCovered;

	public Block snowCovered1;

	public Block snowCovered2;

	public Block snowCovered3;

	public float snowLevel;

	protected float waveFlagMinY = 0.5625f;

	private float[] liquidBarrierHeightonSide;

	public override int Id => BlockId;

	public override EnumItemClass ItemClass => EnumItemClass.Block;

	public virtual bool ForFluidsLayer => false;

	public virtual string RemapToLiquidsLayer => null;

	public CompositeTexture FirstTextureInventory
	{
		get
		{
			if (Textures != null && Textures.Count != 0)
			{
				return Textures.First().Value;
			}
			return null;
		}
	}

	public Vec3d PushVector { get; set; }

	public virtual string ClimateColorMapForMap => ClimateColorMap;

	public virtual string SeasonColorMapForMap => SeasonColorMap;

	public bool AllSidesOpaque
	{
		get
		{
			return SideOpaque.All;
		}
		set
		{
			SideOpaque.All = value;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		humanoidTraversalCost = Attributes?["humanoidTraversalCost"]?.AsFloat(1f) ?? 1f;
		PushVector = Attributes?["pushVector"]?.AsObject<Vec3d>();
		AllowStepWhenStuck = Attributes?["allowStepWhenStuck"]?.AsBool() == true;
		CanStep = Attributes?["canStep"].AsBool(defaultValue: true) ?? true;
		base.OnLoaded(api);
		string text = Variant["cover"];
		if (text != null && (text == "free" || text.Contains("snow")))
		{
			notSnowCovered = api.World.GetBlock(CodeWithVariant("cover", "free"));
			snowCovered1 = api.World.GetBlock(CodeWithVariant("cover", "snow"));
			snowCovered2 = api.World.GetBlock(CodeWithVariant("cover", "snow2"));
			snowCovered3 = api.World.GetBlock(CodeWithVariant("cover", "snow3"));
			if (this == snowCovered1)
			{
				snowLevel = 1f;
			}
			if (this == snowCovered2)
			{
				snowLevel = 2f;
			}
			if (this == snowCovered3)
			{
				snowLevel = 3f;
			}
		}
		if (api.Side == EnumAppSide.Client)
		{
			LoadTextureSubIdForBlockColor();
		}
	}

	public virtual void LoadTextureSubIdForBlockColor()
	{
		TextureSubIdForBlockColor = -1;
		if (Textures == null)
		{
			return;
		}
		string text = Attributes?["textureCodeForBlockColor"].AsString();
		if (text != null && Textures.TryGetValue(text, out var value))
		{
			TextureSubIdForBlockColor = value.Baked.TextureSubId;
		}
		if (TextureSubIdForBlockColor < 0)
		{
			if (Textures.TryGetValue("up", out var value2))
			{
				TextureSubIdForBlockColor = value2.Baked.TextureSubId;
			}
			else if (Textures.Count > 0)
			{
				TextureSubIdForBlockColor = (Textures.First().Value?.Baked?.TextureSubId).GetValueOrDefault();
			}
		}
	}

	public virtual bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		return SideSolid.SidesAndBase;
	}

	public virtual bool SideIsSolid(BlockPos pos, int faceIndex)
	{
		return SideSolid[faceIndex];
	}

	public virtual bool SideIsSolid(IBlockAccessor blockAccess, BlockPos pos, int faceIndex)
	{
		return SideIsSolid(pos, faceIndex);
	}

	public virtual bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		return false;
	}

	public virtual Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		if (facing == null)
		{
			Cuboidf[] selectionBoxes = GetSelectionBoxes(blockAccess, pos);
			if (selectionBoxes == null || selectionBoxes.Length == 0)
			{
				return DefaultCollisionBox;
			}
			return selectionBoxes[0];
		}
		Cuboidf[] array = GetCollisionBoxes(blockAccess, pos);
		if (array == null || array.Length == 0)
		{
			array = GetSelectionBoxes(blockAccess, pos);
		}
		if (array == null || array.Length == 0)
		{
			return null;
		}
		return array[0];
	}

	public virtual Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (RandomDrawOffset != 0)
		{
			Cuboidf[] selectionBoxes = SelectionBoxes;
			if (selectionBoxes != null && selectionBoxes.Length >= 1)
			{
				float x = (float)(GameMath.oaatHash(pos.X, 0, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
				float z = (float)(GameMath.oaatHash(pos.X, 1, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
				return new Cuboidf[1] { SelectionBoxes[0].OffsetCopy(x, 0f, z) };
			}
		}
		Cuboidf[] selectionBoxes2 = SelectionBoxes;
		if (selectionBoxes2 == null || selectionBoxes2.Length != 1)
		{
			return SelectionBoxes;
		}
		IWorldChunk chunkAtBlockPos = blockAccessor.GetChunkAtBlockPos(pos);
		if (chunkAtBlockPos == null)
		{
			return SelectionBoxes;
		}
		return chunkAtBlockPos.AdjustSelectionBoxForDecor(blockAccessor, pos, SelectionBoxes);
	}

	public virtual Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxes;
	}

	public virtual Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return ParticleCollisionBoxes ?? CollisionBoxes;
	}

	public virtual EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return BlockMaterial;
	}

	public virtual float GetResistance(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return Resistance;
	}

	[Obsolete("Use GetSounds with BlockSelection instead")]
	public virtual BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		BlockSelection blockSel = new BlockSelection
		{
			Position = pos
		};
		return GetSounds(blockAccessor, blockSel, stack);
	}

	public virtual BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
	{
		Block block = ((blockSel.Face == null) ? null : blockAccessor.GetDecor(blockSel.Position, new DecorBits(blockSel.Face)));
		if (block != null)
		{
			JsonObject attributes = block.Attributes;
			if (attributes == null || !attributes["ignoreSounds"].AsBool())
			{
				return block.Sounds;
			}
		}
		return Sounds;
	}

	public virtual JsonObject GetAttributes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return Attributes;
	}

	public virtual bool DoEmitSideAo(IGeometryTester caller, BlockFacing facing)
	{
		return (EmitSideAo & facing.Flag) != 0;
	}

	public virtual bool DoEmitSideAoByFlag(IGeometryTester caller, Vec3iAndFacingFlags vec, int flags)
	{
		return (EmitSideAo & flags) != 0;
	}

	public virtual int GetLightAbsorption(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return LightAbsorption;
	}

	public virtual int GetLightAbsorption(IWorldChunk chunk, BlockPos pos)
	{
		return LightAbsorption;
	}

	public virtual string GetLiquidCode(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return LiquidCode;
	}

	public virtual void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
	}

	public virtual bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.CanAttachBlockAt(blockAccessor, block, pos, blockFace, ref handling, attachmentArea);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		return SideSolid[blockFace.Index];
	}

	public virtual bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.CanCreatureSpawnOn(blockAccessor, pos, type, sc, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag2 = true;
				flag = flag && flag3;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		bool flag4 = true;
		if (!AllCreaturesAllowed)
		{
			flag4 = AllowSpawnCreatureGroups != null && AllowSpawnCreatureGroups.Length != 0 && (AllowSpawnCreatureGroups.Contains("*") || AllowSpawnCreatureGroups.Contains(sc.Group));
		}
		if (flag4)
		{
			if (sc.RequireSolidGround)
			{
				return SideSolid[BlockFacing.UP.Index];
			}
			return true;
		}
		return false;
	}

	public virtual bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		Block block = blockAccessor.GetBlock(pos);
		if (block.IsReplacableBy(this))
		{
			if (block.EntityClass != null)
			{
				blockAccessor.RemoveBlockEntity(pos);
			}
			blockAccessor.SetBlock(BlockId, pos);
			if (EntityClass != null)
			{
				blockAccessor.SpawnBlockEntity(EntityClass, pos);
			}
			return true;
		}
		return false;
	}

	public virtual bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		return TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
	}

	public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		}
		return false;
	}

	public virtual bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
	{
		if (!world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
		{
			failureCode = "notreplaceable";
			return false;
		}
		if (CollisionBoxes != null && CollisionBoxes.Length != 0 && world.GetIntersectingEntities(blockSel.Position, GetCollisionBoxes(world.BlockAccessor, blockSel.Position), (Entity e) => e.IsInteractable).Length != 0)
		{
			failureCode = "entityintersecting";
			return false;
		}
		bool flag = true;
		if (byPlayer != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			failureCode = "claimed";
			return false;
		}
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.CanPlaceBlock(world, byPlayer, blockSel, ref handling, ref failureCode);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		return true;
	}

	public virtual bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		world.BlockAccessor.SetBlock(BlockId, blockSel.Position, byItemStack);
		return true;
	}

	public virtual void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
	{
	}

	public virtual float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		IItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
		float num = remainingResistance;
		if (RequiredMiningTier == 0)
		{
			if (dt > 0f)
			{
				BlockBehavior[] blockBehaviors = BlockBehaviors;
				foreach (BlockBehavior blockBehavior in blockBehaviors)
				{
					dt *= blockBehavior.GetMiningSpeedModifier(api.World, blockSel.Position, player);
				}
			}
			num -= dt;
		}
		if (itemstack != null)
		{
			num = itemstack.Collectible.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
		}
		long num2 = 0L;
		if (api.ObjectCache.TryGetValue("totalMsBlockBreaking", out var value))
		{
			num2 = (long)value;
		}
		long elapsedMilliseconds = api.World.ElapsedMilliseconds;
		if (elapsedMilliseconds - num2 > 225 || num <= 0f)
		{
			double posx = (double)blockSel.Position.X + blockSel.HitPosition.X;
			double posy = (double)blockSel.Position.InternalY + blockSel.HitPosition.Y;
			double posz = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
			BlockSounds sounds = GetSounds(api.World.BlockAccessor, blockSel);
			player.Entity.World.PlaySoundAt((num > 0f) ? sounds.GetHitSound(player) : sounds.GetBreakSound(player), posx, posy, posz, player, RandomSoundPitch(api.World), 16f);
			api.ObjectCache["totalMsBlockBreaking"] = elapsedMilliseconds;
		}
		return num;
	}

	public virtual float RandomSoundPitch(IWorldAccessor world)
	{
		return (float)world.Rand.NextDouble() * 0.5f + 0.75f;
	}

	public virtual void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventDefault)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (flag)
		{
			return;
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if (drops != null)
			{
				for (int j = 0; j < drops.Length; j++)
				{
					if (SplitDropStacks)
					{
						for (int k = 0; k < drops[j].StackSize; k++)
						{
							ItemStack itemStack = drops[j].Clone();
							itemStack.StackSize = 1;
							world.SpawnItemEntity(itemStack, pos);
						}
					}
					else
					{
						world.SpawnItemEntity(drops[j].Clone(), pos);
					}
				}
			}
			world.PlaySoundAt(Sounds?.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
		}
		SpawnBlockBrokenParticles(pos, byPlayer);
		world.BlockAccessor.SetBlock(0, pos);
	}

	public void SpawnBlockBrokenParticles(BlockPos pos, IPlayer plr = null)
	{
		BlockBrokenParticleProps blockBrokenParticleProps = new BlockBrokenParticleProps
		{
			blockdamage = new BlockDamage
			{
				Facing = BlockFacing.UP
			}
		};
		blockBrokenParticleProps.Init(api);
		blockBrokenParticleProps.blockdamage.Block = this;
		blockBrokenParticleProps.blockdamage.Position = pos;
		blockBrokenParticleProps.boyant = MaterialDensity < 1000;
		api.World.SpawnParticles(blockBrokenParticleProps, plr);
		if (plr != null && plr.WorldData?.CurrentGameMode == EnumGameMode.Creative)
		{
			api.World.SpawnParticles(blockBrokenParticleProps, plr);
		}
	}

	public virtual void OnBrokenAsDecor(IWorldAccessor world, BlockPos pos, BlockFacing side)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}
		ItemStack[] drops = GetDrops(world, pos, null);
		if (drops == null)
		{
			return;
		}
		Vec3d position = new Vec3d((double)pos.X + 0.5 + (double)side.Normali.X * 0.75, (double)pos.Y + 0.5 + (double)side.Normali.Y * 0.75, (double)pos.Z + 0.5 + (double)side.Normali.Z * 0.75);
		for (int i = 0; i < drops.Length; i++)
		{
			if (SplitDropStacks)
			{
				for (int j = 0; j < drops[i].StackSize; j++)
				{
					ItemStack itemStack = drops[i].Clone();
					itemStack.StackSize = 1;
					world.SpawnItemEntity(itemStack, position);
				}
			}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling bhHandling = EnumHandling.PassThrough;
			obj.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref bhHandling);
			if (bhHandling == EnumHandling.PreventDefault)
			{
				flag = true;
			}
			if (bhHandling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		if (HasBehavior("Unplaceable", api.ClassRegistry))
		{
			return null;
		}
		if (Drops != null)
		{
			IEnumerable<BlockDropItemStack> enumerable = Array.Empty<BlockDropItemStack>();
			BlockDropItemStack[] drops = Drops;
			foreach (BlockDropItemStack blockDropItemStack in drops)
			{
				if (blockDropItemStack.ResolvedItemstack.Collectible is IResolvableCollectible resolvableCollectible)
				{
					BlockDropItemStack[] dropsForHandbook = resolvableCollectible.GetDropsForHandbook(handbookStack, forPlayer);
					enumerable = enumerable.Concat(dropsForHandbook);
				}
				else
				{
					enumerable = enumerable.Append(blockDropItemStack);
				}
			}
			return enumerable.ToArray();
		}
		return Drops;
	}

	protected virtual BlockDropItemStack[] GetHandbookDropsFromBreakDrops(ItemStack handbookStack, IPlayer forPlayer)
	{
		if (HasBehavior("Unplaceable", api.ClassRegistry))
		{
			return null;
		}
		ItemStack[] drops = GetDrops(api.World, forPlayer.Entity.Pos.XYZ.AsBlockPos, forPlayer);
		if (drops == null)
		{
			return Array.Empty<BlockDropItemStack>();
		}
		BlockDropItemStack[] array = new BlockDropItemStack[drops.Length];
		for (int i = 0; i < drops.Length; i++)
		{
			array[i] = new BlockDropItemStack(drops[i]);
		}
		return array;
	}

	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool flag = false;
		List<ItemStack> list = new List<ItemStack>();
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			ItemStack[] drops = obj.GetDrops(world, pos, byPlayer, ref dropQuantityMultiplier, ref handling);
			if (drops != null)
			{
				list.AddRange(drops);
			}
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return drops;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return list.ToArray();
		}
		if (Drops == null)
		{
			return null;
		}
		List<ItemStack> list2 = new List<ItemStack>();
		for (int j = 0; j < Drops.Length; j++)
		{
			BlockDropItemStack blockDropItemStack = Drops[j];
			ItemStack itemStack = blockDropItemStack.ToRandomItemstackForPlayer(byPlayer, world, dropQuantityMultiplier);
			if (itemStack != null)
			{
				list2.Add(itemStack);
				if (blockDropItemStack.LastDrop)
				{
					break;
				}
			}
		}
		list2.AddRange(list);
		return list2.ToArray();
	}

	public virtual ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		EnumHandling enumHandling = EnumHandling.PassThrough;
		ItemStack result = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			ItemStack itemStack = obj.OnPickBlock(world, pos, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				result = itemStack;
				enumHandling = handling;
			}
			if (enumHandling == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (enumHandling == EnumHandling.PreventDefault)
		{
			return result;
		}
		return new ItemStack(this);
	}

	public virtual void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockRemoved(world, pos, ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
		}
		if (!flag && EntityClass != null)
		{
			world.BlockAccessor.RemoveBlockEntity(pos);
		}
	}

	public virtual void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		if (EntityClass != null)
		{
			world.BlockAccessor.SpawnBlockEntity(EntityClass, blockPos, byItemStack);
		}
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockPlaced(world, blockPos, ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
		}
	}

	public virtual void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnNeighbourBlockChange(world, pos, neibpos, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (handling == EnumHandling.PassThrough && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && pos.X == neibpos.X && pos.Z == neibpos.Z && pos.Y + 1 == neibpos.Y && world.BlockAccessor.GetBlock(neibpos).Id != 0)
		{
			world.BlockAccessor.SetBlock(notSnowCovered.Id, pos);
		}
	}

	public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool flag = true;
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		return false;
	}

	public virtual void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
	{
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.Activate(world, caller, blockSel, activationArgs, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool flag = false;
		bool flag2 = true;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag2 = flag2 && flag3;
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag2;
			}
		}
		if (flag)
		{
			return flag2;
		}
		return false;
	}

	public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		bool flag = false;
		bool flag2 = true;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag2 = flag2 && flag3;
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag2;
			}
		}
		if (flag)
		{
			return flag2;
		}
		return true;
	}

	public virtual void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
	}

	public virtual void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (entity.Properties.CanClimb && (IsClimbable(pos) || entity.Properties.CanClimbAnywhere) && facing.IsHorizontal && entity is EntityAgent)
		{
			EntityAgent entityAgent = entity as EntityAgent;
			if (new bool?(entityAgent.Controls.Sneak) != true)
			{
				entityAgent.SidedPos.Motion.Y = 0.04;
			}
		}
		if (!(api is ICoreServerAPI coreServerAPI))
		{
			return;
		}
		float impactBlockUpdateChance = entity.ImpactBlockUpdateChance;
		if (isImpact && collideSpeed.Y < -0.05 && world.Rand.NextDouble() < (double)impactBlockUpdateChance)
		{
			BlockPos updatePos = pos.Copy();
			coreServerAPI.Event.EnqueueMainThreadTask(delegate
			{
				OnNeighbourBlockChange(world, updatePos, updatePos.UpCopy());
			}, "entityBlockImpact");
		}
	}

	public virtual bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
	{
		return false;
	}

	public virtual bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
	{
		return false;
	}

	public virtual bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		bool flag = false;
		bool flag2 = false;
		isWindAffected = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.ShouldReceiveClientParticleTicks(world, player, pos, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag || flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		if (ParticleProperties != null && ParticleProperties.Length != 0)
		{
			for (int j = 0; j < ParticleProperties.Length; j++)
			{
				isWindAffected |= ParticleProperties[0].WindAffectednes > 0f;
			}
			return true;
		}
		return false;
	}

	[Obsolete("Use GetAmbientsoundStrength() instead. Method will be removed in 1.21")]
	public virtual bool ShouldPlayAmbientSound(IWorldAccessor world, BlockPos pos)
	{
		return GetAmbientSoundStrength(world, pos) > 0f;
	}

	public virtual float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		return 1f;
	}

	public virtual void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (ParticleProperties != null && ParticleProperties.Length != 0)
		{
			for (int i = 0; i < ParticleProperties.Length; i++)
			{
				AdvancedParticleProperties advancedParticleProperties = ParticleProperties[i];
				advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
				advancedParticleProperties.basePos.X = (float)pos.X + TopMiddlePos.X;
				advancedParticleProperties.basePos.Y = (float)pos.InternalY + TopMiddlePos.Y;
				advancedParticleProperties.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
				manager.Spawn(advancedParticleProperties);
			}
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int j = 0; j < blockBehaviors.Length; j++)
		{
			blockBehaviors[j].OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
	}

	public virtual bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		if (GlobalConstants.MeltingFreezingEnabled && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature > 4f)
		{
			extra = "melt";
			return true;
		}
		extra = null;
		return false;
	}

	public virtual void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		if (extra is string && (string)extra == "melt")
		{
			if (this == snowCovered3)
			{
				world.BlockAccessor.SetBlock(snowCovered2.Id, pos);
			}
			else if (this == snowCovered2)
			{
				world.BlockAccessor.SetBlock(snowCovered1.Id, pos);
			}
			else if (this == snowCovered1)
			{
				world.BlockAccessor.SetBlock(notSnowCovered.Id, pos);
			}
		}
	}

	public virtual void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.Leaves)
		{
			int verticesCount = decalMesh.VerticesCount;
			for (int i = 0; i < verticesCount; i++)
			{
				decalMesh.Flags[i] |= 100663296;
			}
		}
		else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
		{
			decalMesh.SetWindFlag();
		}
	}

	public virtual void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.Leaves)
		{
			int verticesCount = sourceMesh.VerticesCount;
			for (int i = 0; i < verticesCount; i++)
			{
				sourceMesh.Flags[i] |= 100663296;
			}
		}
		else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
		{
			sourceMesh.SetWindFlag(waveFlagMinY);
		}
	}

	public virtual void DetermineTopMiddlePos()
	{
		if (CollisionBoxes != null && CollisionBoxes.Length != 0)
		{
			Cuboidf cuboidf = CollisionBoxes[0];
			TopMiddlePos.X = (cuboidf.X1 + cuboidf.X2) / 2f;
			TopMiddlePos.Y = cuboidf.Y2;
			TopMiddlePos.Z = (cuboidf.Z1 + cuboidf.Z2) / 2f;
			for (int i = 1; i < CollisionBoxes.Length; i++)
			{
				TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, CollisionBoxes[i].Y2);
			}
		}
		else if (SelectionBoxes != null && SelectionBoxes.Length != 0)
		{
			Cuboidf cuboidf2 = SelectionBoxes[0];
			TopMiddlePos.X = (cuboidf2.X1 + cuboidf2.X2) / 2f;
			TopMiddlePos.Y = cuboidf2.Y2;
			TopMiddlePos.Z = (cuboidf2.Z1 + cuboidf2.Z2) / 2f;
			for (int j = 1; j < SelectionBoxes.Length; j++)
			{
				TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, SelectionBoxes[j].Y2);
			}
		}
	}

	public virtual bool IsReplacableBy(Block block)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.IsReplacableBy(block, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		if (IsLiquid() || Replaceable >= 6000)
		{
			return block.Replaceable < Replaceable;
		}
		return false;
	}

	public static BlockFacing[] SuggestedHVOrientation(IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		double num = byPlayer.Entity.Pos.X + byPlayer.Entity.LocalEyePos.X - ((double)blockPos.X + blockSel.HitPosition.X);
		double num2 = byPlayer.Entity.Pos.Y + byPlayer.Entity.LocalEyePos.Y - ((double)blockPos.Y + blockSel.HitPosition.Y);
		double num3 = byPlayer.Entity.Pos.Z + byPlayer.Entity.LocalEyePos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
		float radians = (float)Math.Atan2(num, num3) + (float)Math.PI / 2f;
		double y = num2;
		float num4 = (float)Math.Sqrt(num * num + num3 * num3);
		float num5 = (float)Math.Atan2(y, num4);
		BlockFacing blockFacing = (((double)num5 < -Math.PI / 4.0) ? BlockFacing.DOWN : (((double)num5 > Math.PI / 4.0) ? BlockFacing.UP : null));
		BlockFacing blockFacing2 = BlockFacing.HorizontalFromAngle(radians);
		return new BlockFacing[2] { blockFacing2, blockFacing };
	}

	public virtual void PerformSnowLevelUpdate(IBulkBlockAccessor ba, BlockPos pos, Block newBlock, float snowLevel)
	{
		if (newBlock.Id != Id && (BlockMaterial == EnumBlockMaterial.Snow || BlockId == 0 || FirstCodePart() == newBlock.FirstCodePart()))
		{
			ba.ExchangeBlock(newBlock.Id, pos);
		}
	}

	public virtual Block GetSnowCoveredVariant(BlockPos pos, float snowLevel)
	{
		if (snowCovered1 == null)
		{
			return null;
		}
		if (snowLevel >= 1f)
		{
			if (snowLevel >= 3f && snowCovered3 != null)
			{
				return snowCovered3;
			}
			if (snowLevel >= 2f && snowCovered2 != null)
			{
				return snowCovered2;
			}
			return snowCovered1;
		}
		if ((double)snowLevel < 0.1)
		{
			return notSnowCovered;
		}
		return this;
	}

	public virtual float GetSnowLevel(BlockPos pos)
	{
		return snowLevel;
	}

	[Obsolete("Use GetRetention() instead")]
	public virtual int GetHeatRetention(BlockPos pos, BlockFacing facing)
	{
		return GetRetention(pos, facing, EnumRetentionType.Heat);
	}

	public virtual int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		bool flag = false;
		int result = 0;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			int retention = obj.GetRetention(pos, facing, type, ref handled);
			if (handled != EnumHandling.PassThrough)
			{
				flag = true;
				result = retention;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return retention;
			}
		}
		if (flag)
		{
			return result;
		}
		if (SideSolid[facing.Index])
		{
			if (type == EnumRetentionType.Sound)
			{
				return 10;
			}
			EnumBlockMaterial blockMaterial = GetBlockMaterial(api.World.BlockAccessor, pos);
			if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
			{
				return -1;
			}
			return 1;
		}
		return 0;
	}

	public virtual bool IsClimbable(BlockPos pos)
	{
		return Climbable;
	}

	public virtual float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.Humanoid)
		{
			return humanoidTraversalCost;
		}
		return (1f - WalkSpeedMultiplier) * (float)((creatureType == EnumAICreatureType.Humanoid) ? 5 : 2);
	}

	public virtual AssetLocation GetRotatedBlockCode(int angle)
	{
		bool flag = false;
		AssetLocation result = Code;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			AssetLocation rotatedBlockCode = obj.GetRotatedBlockCode(angle, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
				result = rotatedBlockCode;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return rotatedBlockCode;
			}
		}
		if (flag)
		{
			return result;
		}
		return Code;
	}

	public virtual AssetLocation GetVerticallyFlippedBlockCode()
	{
		bool flag = false;
		AssetLocation result = Code;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			AssetLocation verticallyFlippedBlockCode = obj.GetVerticallyFlippedBlockCode(ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return verticallyFlippedBlockCode;
			case EnumHandling.PassThrough:
				continue;
			}
			flag = true;
			result = verticallyFlippedBlockCode;
		}
		if (flag)
		{
			return result;
		}
		return Code;
	}

	public virtual AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		AssetLocation result = Code;
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			AssetLocation horizontallyFlippedBlockCode = obj.GetHorizontallyFlippedBlockCode(axis, ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return horizontallyFlippedBlockCode;
			case EnumHandling.PassThrough:
				continue;
			}
			flag = true;
			result = horizontallyFlippedBlockCode;
		}
		if (flag)
		{
			return result;
		}
		return Code;
	}

	public BlockBehavior GetBehavior(Type type, bool withInheritance)
	{
		if (withInheritance)
		{
			for (int i = 0; i < BlockBehaviors.Length; i++)
			{
				Type type2 = BlockBehaviors[i].GetType();
				if (type2 == type || type.IsAssignableFrom(type2))
				{
					return BlockBehaviors[i];
				}
			}
			return null;
		}
		for (int j = 0; j < BlockBehaviors.Length; j++)
		{
			if (BlockBehaviors[j].GetType() == type)
			{
				return BlockBehaviors[j];
			}
		}
		return null;
	}

	public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		WorldInteraction[] array = Array.Empty<WorldInteraction>();
		bool flag = true;
		if (world.Claims != null && world is IClientWorldAccessor clientWorldAccessor)
		{
			IClientPlayer player = clientWorldAccessor.Player;
			if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Survival && world.Claims.TestAccess(clientWorldAccessor.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak) != EnumWorldAccessResponse.Granted)
			{
				flag = false;
			}
		}
		if (flag)
		{
			int num = 0;
			while (Drops != null && num < Drops.Length)
			{
				if (Drops[num].Tool.HasValue)
				{
					EnumTool tool = Drops[num].Tool.Value;
					array = array.Append(new WorldInteraction
					{
						ActionLangCode = "blockhelp-collect",
						MouseButton = EnumMouseButton.Left,
						Itemstacks = ObjectCacheUtil.GetOrCreate(api, "blockhelp-collect-withtool-" + tool, delegate
						{
							List<ItemStack> list = new List<ItemStack>();
							foreach (CollectibleObject collectible in api.World.Collectibles)
							{
								if (collectible.Tool == tool)
								{
									list.Add(new ItemStack(collectible));
								}
							}
							return list.ToArray();
						})
					});
				}
				num++;
			}
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int num2 = 0; num2 < blockBehaviors.Length; num2++)
		{
			WorldInteraction[] placedBlockInteractionHelp = blockBehaviors[num2].GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
			array = array.Append(placedBlockInteractionHelp);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return array;
	}

	public virtual string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(OnPickBlock(world, pos)?.GetName());
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].GetPlacedBlockName(stringBuilder, world, pos);
		}
		return stringBuilder.ToString().TrimEnd();
	}

	public virtual string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (EntityClass != null)
		{
			BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
			if (blockEntity != null)
			{
				try
				{
					blockEntity.GetBlockInfo(forPlayer, stringBuilder);
				}
				catch (Exception e)
				{
					stringBuilder.AppendLine("(error in " + blockEntity.GetType().Name + ")");
					api.Logger.Error(e);
				}
			}
		}
		if (Code == null)
		{
			stringBuilder.AppendLine("Unknown Block with ID " + BlockId);
			return stringBuilder.ToString();
		}
		string text = Code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code.Path;
		string matching = Lang.GetMatching(text);
		matching = ((matching != text) ? matching : "");
		stringBuilder.Append(matching);
		Block[] decors = world.BlockAccessor.GetDecors(pos);
		List<string> list = new List<string>();
		if (decors != null)
		{
			for (int i = 0; i < decors.Length; i++)
			{
				if (decors[i] != null)
				{
					AssetLocation code = decors[i].Code;
					string matching2 = Lang.GetMatching(code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "-" + code.Path);
					list.Add(Lang.Get("block-with-decorname", matching2));
				}
			}
		}
		stringBuilder.AppendLine(string.Join("\r\n", list.Distinct()));
		if (RequiredMiningTier > 0 && api.World.Claims.TestAccess(forPlayer, pos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
		{
			AddMiningTierInfo(stringBuilder);
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior blockBehavior in blockBehaviors)
		{
			stringBuilder.Append(blockBehavior.GetPlacedBlockInfo(world, pos, forPlayer));
		}
		return stringBuilder.ToString().TrimEnd();
	}

	public virtual void AddMiningTierInfo(StringBuilder sb)
	{
		string text = "?";
		if (RequiredMiningTier < miningTierNames.Length)
		{
			text = miningTierNames[RequiredMiningTier];
		}
		sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", RequiredMiningTier, (text == "?") ? text : Lang.Get(text)));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack itemstack = inSlot.Itemstack;
		if (DrawType == EnumDrawType.SurfaceLayer)
		{
			dsc.AppendLine(Lang.Get("Decor layer block"));
		}
		EnumBlockMaterial blockMaterial = GetBlockMaterial(world.BlockAccessor, null, itemstack);
		dsc.AppendLine(Lang.Get("Material: ") + Lang.Get("blockmaterial-" + blockMaterial));
		AddExtraHeldItemInfoPostMaterial(inSlot, dsc, world);
		byte[] lightHsv = GetLightHsv(world.BlockAccessor, null, itemstack);
		dsc.Append((!withDebugInfo) ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-hsv") + lightHsv[0] + ", " + lightHsv[1] + ", " + lightHsv[2] + "\n") : ""));
		dsc.Append(withDebugInfo ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-level") + lightHsv[2] + "\n") : ""));
		if (WalkSpeedMultiplier != 1f)
		{
			dsc.Append(Lang.Get("walk-multiplier") + WalkSpeedMultiplier + "\n");
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior blockBehavior in blockBehaviors)
		{
			dsc.Append(blockBehavior.GetHeldBlockInfo(world, inSlot));
		}
		if (world.Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			IEnumerable<string> source = GetTags(inSlot.Itemstack).ToArray().Select(coreClientAPI.TagRegistry.BlockTagIdToTag).Order();
			if (source.Any())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(35, 1, dsc);
				handler.AppendLiteral("<font color=\"#bbbbbb\">Tags: ");
				handler.AppendFormatted(source.Aggregate((string first, string second) => first + ", " + second));
				handler.AppendLiteral("</font>");
				dsc.AppendLine(ref handler);
			}
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public virtual void AddExtraHeldItemInfoPostMaterial(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
	}

	public virtual bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return PartialSelection;
	}

	public virtual Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
	{
		return new Vec4f(0f, 0f, 0f, 0.5f);
	}

	public virtual void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		(Textures as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked variant of block ");
		(TexturesInventory as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked inventory variant of block ");
		foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
		{
			AssetLocation anyWildCardNoFiles = texture.Value.AnyWildCardNoFiles;
			if (anyWildCardNoFiles != null)
			{
				api.Logger.Warning("Block {0} defines a wildcard texture {1} (or one of its alternates), key {2}, but no matching texture found", Code, anyWildCardNoFiles, texture.Key);
			}
		}
	}

	public virtual double GetBlastResistance(IWorldAccessor world, BlockPos pos, Vec3f blastDirectionVector, EnumBlastType blastType)
	{
		if (blastType == EnumBlastType.RockBlast)
		{
			return Math.Min(BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.RockBlast, GetBlockMaterial(world.BlockAccessor, pos)), BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.OreBlast, GetBlockMaterial(world.BlockAccessor, pos)));
		}
		return BlockMaterialUtil.MaterialBlastResistance(blastType, GetBlockMaterial(world.BlockAccessor, pos));
	}

	public virtual double ExplosionDropChance(IWorldAccessor world, BlockPos pos, EnumBlastType blastType)
	{
		return BlockMaterialUtil.MaterialBlastDropChances(blastType, GetBlockMaterial(world.BlockAccessor, pos));
	}

	[Obsolete("Please use OnBlockExploded() with parameter ignitedByPlayerUid")]
	public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
	{
		OnBlockExploded(world, pos, explosionCenter, blastType, null);
	}

	public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return;
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
		double num = ExplosionDropChance(world, pos, blastType);
		if (world.Rand.NextDouble() < num)
		{
			ItemStack[] drops = GetDrops(world, pos, null);
			int num2 = 0;
			while (drops != null && num2 < drops.Length)
			{
				if (SplitDropStacks)
				{
					for (int j = 0; j < drops[num2].StackSize; j++)
					{
						ItemStack itemStack = drops[num2].Clone();
						itemStack.StackSize = 1;
						world.SpawnItemEntity(itemStack, pos);
					}
				}
				else
				{
					world.SpawnItemEntity(drops[num2].Clone(), pos);
				}
				num2++;
			}
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
		}
	}

	public virtual int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		if (!Textures.TryGetValue(facing.Code, out var value))
		{
			value = Textures.First().Value;
		}
		if (value?.Baked == null)
		{
			return 0;
		}
		int num = capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
		if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
		{
			num = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num, pos.X, pos.Y, pos.Z);
		}
		return num;
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		if (TextureSubIdForBlockColor < 0)
		{
			return -1;
		}
		return capi.BlockTextureAtlas.GetRandomColor(TextureSubIdForBlockColor);
	}

	public virtual int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int num = GetColorWithoutTint(capi, pos);
		if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
		{
			num = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num, pos.X, pos.Y, pos.Z, flipRb: false);
		}
		return num;
	}

	public virtual int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		Block block = (HasBehavior("Decor", api.ClassRegistry) ? null : capi.World.BlockAccessor.GetDecor(pos, new DecorBits(BlockFacing.UP)));
		if (block != null && block != this)
		{
			return block.GetColorWithoutTint(capi, pos);
		}
		if (TextureSubIdForBlockColor < 0)
		{
			return -1;
		}
		return capi.BlockTextureAtlas.GetAverageColor(TextureSubIdForBlockColor);
	}

	public virtual bool AllowSnowCoverage(IWorldAccessor world, BlockPos blockPos)
	{
		return SideSolid[BlockFacing.UP.Index];
	}

	public virtual T GetBlockEntity<T>(BlockSelection blockSel) where T : BlockEntity
	{
		return api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as T;
	}

	public virtual T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
	{
		return api.World.BlockAccessor.GetBlockEntity(position) as T;
	}

	public virtual T GetBEBehavior<T>(BlockPos pos) where T : BlockEntityBehavior
	{
		BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity == null)
		{
			return null;
		}
		return blockEntity.GetBehavior<T>();
	}

	public virtual T GetInterface<T>(IWorldAccessor world, BlockPos pos) where T : class
	{
		if (this is T result)
		{
			return result;
		}
		BlockBehavior behavior = GetBehavior(typeof(T), withInheritance: true);
		if (behavior != null)
		{
			return behavior as T;
		}
		if (pos == null)
		{
			return null;
		}
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity is T result2)
		{
			return result2;
		}
		if (blockEntity != null)
		{
			T behavior2 = blockEntity.GetBehavior<T>();
			if (behavior2 != null)
			{
				return behavior2;
			}
		}
		return null;
	}

	public Block Clone()
	{
		Block block = (Block)MemberwiseClone();
		block.Code = Code.Clone();
		if (MiningSpeed != null)
		{
			block.MiningSpeed = new Dictionary<EnumBlockMaterial, float>(MiningSpeed);
		}
		if (Textures is FastSmallDictionary<string, CompositeTexture> fastSmallDictionary)
		{
			block.Textures = fastSmallDictionary.Clone();
		}
		else
		{
			block.Textures = new FastSmallDictionary<string, CompositeTexture>(Textures.Count);
			foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
			{
				block.Textures[texture.Key] = texture.Value.Clone();
			}
		}
		if (TexturesInventory is FastSmallDictionary<string, CompositeTexture> fastSmallDictionary2)
		{
			block.TexturesInventory = fastSmallDictionary2.Clone();
		}
		else
		{
			block.TexturesInventory = new Dictionary<string, CompositeTexture>();
			foreach (KeyValuePair<string, CompositeTexture> item in TexturesInventory)
			{
				block.TexturesInventory[item.Key] = item.Value.Clone();
			}
		}
		block.Shape = Shape.Clone();
		block.LightHsv = LightHsv;
		if (ParticleProperties != null)
		{
			block.ParticleProperties = new AdvancedParticleProperties[ParticleProperties.Length];
			for (int i = 0; i < ParticleProperties.Length; i++)
			{
				block.ParticleProperties[i] = ParticleProperties[i].Clone();
			}
		}
		if (Drops != null)
		{
			block.Drops = new BlockDropItemStack[Drops.Length];
			for (int j = 0; j < Drops.Length; j++)
			{
				block.Drops[j] = Drops[j].Clone();
			}
		}
		block.SideOpaque = SideOpaque;
		block.SideSolid = SideSolid;
		block.SideAo = SideAo;
		if (CombustibleProps != null)
		{
			block.CombustibleProps = CombustibleProps.Clone();
		}
		if (NutritionProps != null)
		{
			block.NutritionProps = NutritionProps.Clone();
		}
		if (GrindingProps != null)
		{
			block.GrindingProps = GrindingProps.Clone();
		}
		if (Attributes != null)
		{
			block.Attributes = Attributes.Clone();
		}
		return block;
	}

	public bool HasBlockBehavior<T>(bool withInheritance = false) where T : BlockBehavior
	{
		return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
	}

	public override bool HasBehavior<T>(bool withInheritance = false)
	{
		return HasBehavior(typeof(T), withInheritance);
	}

	public override bool HasBehavior(string type, IClassRegistryAPI classRegistry)
	{
		if (GetBehavior(classRegistry.GetCollectibleBehaviorClass(type), withInheritance: false) == null)
		{
			return GetBehavior(classRegistry.GetBlockBehaviorClass(type)) != null;
		}
		return true;
	}

	public override bool HasBehavior(Type type, bool withInheritance = false)
	{
		if (GetBehavior(CollectibleBehaviors, type, withInheritance) == null)
		{
			CollectibleBehavior[] blockBehaviors = BlockBehaviors;
			return GetBehavior(blockBehaviors, type, withInheritance) != null;
		}
		return true;
	}

	public virtual BlockTagArray GetTags(ItemStack stack)
	{
		return Tags;
	}

	internal void EnsureValidTextures(ILogger logger)
	{
		List<string> list = null;
		int num = 0;
		foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
		{
			if (texture.Value.Base == null)
			{
				logger.Error("The texture definition {0} for #{2} in block with code {1} is invalid. The base property is null. Will skip.", num, Code, texture.Key);
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(texture.Key);
			}
			num++;
		}
		if (list == null)
		{
			return;
		}
		foreach (string item in list)
		{
			Textures.Remove(item);
		}
	}

	public virtual float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		bool flag = false;
		float result = 0f;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			float liquidBarrierHeightOnSide = obj.GetLiquidBarrierHeightOnSide(face, pos, ref handled);
			if (handled != EnumHandling.PassThrough)
			{
				flag = true;
				result = liquidBarrierHeightOnSide;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return liquidBarrierHeightOnSide;
			}
		}
		if (flag)
		{
			return result;
		}
		if (liquidBarrierHeightonSide == null)
		{
			liquidBarrierHeightonSide = new float[6];
			for (int j = 0; j < 6; j++)
			{
				liquidBarrierHeightonSide[j] = (SideSolid.OnSide(BlockFacing.ALLFACES[j]) ? 1f : 0f);
			}
			float[] array = Attributes?["liquidBarrierOnSides"].AsArray<float>();
			int num = 0;
			while (array != null && num < array.Length)
			{
				liquidBarrierHeightonSide[num] = array[num];
				num++;
			}
		}
		return liquidBarrierHeightonSide[face.Index];
	}

	public override string ToString()
	{
		return Code.Domain + ":block " + Code.Path + "/" + BlockId;
	}

	public virtual void FreeRAMServer()
	{
		ShapeInventory = null;
		Lod0Shape = null;
		Lod2Shape = null;
		Textures = null;
		TexturesInventory = null;
		GuiTransform = null;
		FpHandTransform = null;
		TpHandTransform = null;
		TpOffHandTransform = null;
		GroundTransform = null;
	}
}
