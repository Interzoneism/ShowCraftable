using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEHelveHammer : BlockEntity, ITexPositionSource
{
	private int count = 40;

	private bool obstructed;

	private BEBehaviorMPToggle mptoggle;

	private ITexPositionSource blockTexSource;

	private ItemStack hammerStack;

	private double ellapsedInameSecGrow;

	private float rnd;

	public BlockFacing facing;

	private BlockPos togglePos;

	private BlockPos anvilPos;

	private BlockEntityAnvil targetAnvil;

	private double angleBefore;

	private bool didHit;

	private float vibrate;

	private ICoreClientAPI capi;

	private HelveHammerRenderer renderer;

	private float accumHits;

	public ItemStack HammerStack
	{
		get
		{
			return hammerStack;
		}
		set
		{
			hammerStack = value;
			MarkDirty();
			setRenderer();
		}
	}

	public float Angle
	{
		get
		{
			if (mptoggle == null)
			{
				return 0f;
			}
			if (obstructed)
			{
				return (float)angleBefore;
			}
			double num = Api.World.Calendar.TotalHours * 60.0 * 2.0;
			double num2 = facing.Index switch
			{
				3 => mptoggle.isRotationReversed() ? 1.9 : 0.6, 
				1 => mptoggle.isRotationReversed() ? (-0.65) : (-1.55), 
				0 => mptoggle.isRotationReversed() ? (-0.4) : 1.2, 
				_ => mptoggle.isRotationReversed() ? 1.8 : 1.2, 
			};
			double num3 = Math.Abs(Math.Sin(GameMath.Mod((double)mptoggle.AngleRad * 2.0 + num2 - (double)rnd, Math.PI * 20.0)) / 4.5);
			float num4 = (float)num3;
			if (angleBefore > num3)
			{
				num4 -= (float)(num - ellapsedInameSecGrow) * 1.5f;
			}
			else
			{
				ellapsedInameSecGrow = num;
			}
			num4 = Math.Max(0f, num4);
			vibrate *= 0.5f;
			if (num4 <= 0.01f && !didHit)
			{
				didHit = true;
				vibrate = 0.02f;
				if (Api.Side == EnumAppSide.Client && targetAnvil != null)
				{
					Api.World.PlaySoundAt(new AssetLocation("sounds/effect/anvilhit"), (float)(Pos.X + facing.Normali.X * 3) + 0.5f, (float)Pos.Y + 0.5f, (float)(Pos.Z + facing.Normali.Z * 3) + 0.5f, null, 0.3f + (float)Api.World.Rand.NextDouble() * 0.2f, 12f);
					targetAnvil.OnHelveHammerHit();
				}
			}
			if ((double)num4 > 0.2)
			{
				didHit = false;
			}
			angleBefore = num3;
			float num5 = num4 + (float)Math.Sin(num) * vibrate;
			if (targetAnvil?.WorkItemStack != null)
			{
				num5 = Math.Max(3f / 64f, num5);
			}
			return num5;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "metal" && hammerStack.Item.Textures.TryGetValue(textureCode, out var value))
			{
				AssetLocation textureLocation = value.Base;
				return capi.BlockTextureAtlas[textureLocation];
			}
			return blockTexSource[textureCode];
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		if (facing == null)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			return;
		}
		Vec3i normali = facing.Normali;
		anvilPos = Pos.AddCopy(normali.X * 3, 0, normali.Z * 3);
		togglePos = Pos.AddCopy(normali);
		RegisterGameTickListener(onEvery25ms, 25);
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			blockTexSource = capi.Tesselator.GetTextureSource(base.Block);
		}
		setRenderer();
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		rnd = (float)Api.World.Rand.NextDouble() / 10f;
	}

	public void updateAngle()
	{
	}

	private void setRenderer()
	{
		if (HammerStack != null && renderer == null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				renderer = new HelveHammerRenderer(Api as ICoreClientAPI, this, Pos, GenHammerMesh());
				(Api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "helvehammer");
				(Api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.ShadowFar, "helvehammer");
				(Api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.ShadowNear, "helvehammer");
			}
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (HammerStack != null)
		{
			Api.World.SpawnItemEntity(HammerStack, Pos);
		}
		base.OnBlockBroken(byPlayer);
	}

	private MeshData GenHammerMesh()
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		if (block.BlockId == 0)
		{
			return null;
		}
		ITesselatorAPI tesselator = ((ICoreClientAPI)Api).Tesselator;
		Shape shapeBase = Shape.TryGet(Api, "shapes/block/wood/mechanics/helvehammer.json");
		tesselator.TesselateShape("helvehammerhead", shapeBase, out var modeldata, this, new Vec3f(0f, block.Shape.rotateY, 0f), 0, 0, 0);
		return modeldata;
	}

	private void onEvery25ms(float dt)
	{
		if (count >= 40)
		{
			count = 0;
			CheckValidToggleAndNotObstructed();
		}
		if (Api.World.Side == EnumAppSide.Server && targetAnvil != null && mptoggle?.Network != null && HammerStack != null && !obstructed)
		{
			float num = 0.62f;
			float num2 = Math.Abs(mptoggle.Network.Speed) * mptoggle.GearedRatio;
			accumHits += num2 * num * dt * 8f;
			if (accumHits > (float)Math.PI / 2f)
			{
				targetAnvil.OnHelveHammerHit();
				accumHits -= (float)Math.PI / 2f;
			}
		}
		count++;
	}

	private void CheckValidToggleAndNotObstructed()
	{
		targetAnvil = Api.World.BlockAccessor.GetBlockEntity(anvilPos) as BlockEntityAnvil;
		obstructed = false;
		if (renderer != null)
		{
			renderer.Obstructed = false;
		}
		mptoggle = Api.World.BlockAccessor.GetBlockEntity(togglePos)?.GetBehavior<BEBehaviorMPToggle>();
		BEBehaviorMPToggle bEBehaviorMPToggle = mptoggle;
		if (bEBehaviorMPToggle != null && !bEBehaviorMPToggle.ValidHammerBase(Pos))
		{
			mptoggle = null;
			obstructed = true;
			if (renderer != null)
			{
				renderer.Obstructed = true;
			}
			return;
		}
		BlockPos blockPos = Pos.AddCopy(0, 1, 0);
		for (int i = 0; i < 3; i++)
		{
			Cuboidf[] collisionBoxes = Api.World.BlockAccessor.GetBlock(blockPos).GetCollisionBoxes(Api.World.BlockAccessor, blockPos);
			bool flag = collisionBoxes != null && collisionBoxes.Length != 0;
			if (flag && i <= 1)
			{
				flag = false;
				for (int j = 0; j < collisionBoxes.Length; j++)
				{
					if (i == 1)
					{
						flag |= collisionBoxes[j].Y1 < 0.375f;
						continue;
					}
					bool flag2 = collisionBoxes[j].Y1 < 0.2f;
					switch (facing.Index)
					{
					case 0:
						flag2 = flag2 && (double)collisionBoxes[j].Z1 < 0.5;
						break;
					case 1:
						flag2 = flag2 && (double)collisionBoxes[j].X2 > 0.5;
						break;
					case 2:
						flag2 = flag2 && (double)collisionBoxes[j].Z2 > 0.5;
						break;
					case 3:
						flag2 = flag2 && (double)collisionBoxes[j].X1 < 0.5;
						break;
					}
					flag = flag || flag2;
				}
			}
			if (flag)
			{
				obstructed = true;
				if (renderer != null)
				{
					renderer.Obstructed = true;
				}
				break;
			}
			blockPos.Add(facing.Normali);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		hammerStack = tree.GetItemstack("hammerStack");
		hammerStack?.ResolveBlockOrItem(worldAccessForResolve);
		HammerStack = hammerStack;
		rnd = tree.GetFloat("rnd");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetItemstack("hammerStack", HammerStack);
		tree.SetFloat("rnd", rnd);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		hammerStack?.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings);
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		hammerStack?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(HammerStack), blockIdMapping, itemIdMapping);
	}
}
