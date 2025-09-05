using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFruitPress : BlockEntityContainer
{
	private const int PacketIdAnimUpdate = 1001;

	private const int PacketIdScrewStart = 1002;

	private const int PacketIdUnscrew = 1003;

	private const int PacketIdScrewContinue = 1004;

	private static SimpleParticleProperties liquidParticles;

	private InventoryGeneric inv;

	private ICoreClientAPI capi;

	private BlockFruitPress ownBlock;

	private MeshData meshMovable;

	private MeshData bucketMesh;

	private MeshData bucketMeshTmp;

	private FruitpressContentsRenderer renderer;

	private AnimationMetaData compressAnimMeta = new AnimationMetaData
	{
		Animation = "compress",
		Code = "compress",
		AnimationSpeed = 0.5f,
		EaseOutSpeed = 0.5f,
		EaseInSpeed = 3f
	};

	private float? loadedFrame;

	private bool serverListenerActive;

	private long listenerId;

	private double juiceableLitresCapacity = 10.0;

	private double screwPercent;

	private double squeezedLitresLeft;

	private double pressSqueezeRel;

	private bool squeezeSoundPlayed;

	private int dryStackSize;

	private double lastLiquidTransferTotalHours;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "fruitpress";

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

	public ItemSlot MashSlot => inv[0];

	public ItemSlot BucketSlot => inv[1];

	private ItemStack mashStack => MashSlot.Itemstack;

	private double juiceableLitresLeft
	{
		get
		{
			return (mashStack?.Attributes?.GetDouble("juiceableLitresLeft")).GetValueOrDefault();
		}
		set
		{
			mashStack.Attributes.SetDouble("juiceableLitresLeft", value);
		}
	}

	private double juiceableLitresTransfered
	{
		get
		{
			return (mashStack?.Attributes?.GetDouble("juiceableLitresTransfered")).GetValueOrDefault();
		}
		set
		{
			mashStack.Attributes.SetDouble("juiceableLitresTransfered", value);
		}
	}

	public bool CompressAnimFinished
	{
		get
		{
			RunningAnimation animationState = animUtil.animator.GetAnimationState("compress");
			return animationState.CurrentFrame >= (float)(animationState.Animation.QuantityFrames - 1);
		}
	}

	public bool CompressAnimActive
	{
		get
		{
			if (!animUtil.activeAnimationsByAnimCode.ContainsKey("compress"))
			{
				return animUtil.animator.GetAnimationState("compress")?.Active ?? false;
			}
			return true;
		}
	}

	public bool CanScrew => !CompressAnimFinished;

	public bool CanUnscrew
	{
		get
		{
			if (!CompressAnimFinished)
			{
				return CompressAnimActive;
			}
			return true;
		}
	}

	public bool CanFillRemoveItems => !CompressAnimActive;

	static BlockEntityFruitPress()
	{
		liquidParticles = new SimpleParticleProperties
		{
			MinVelocity = new Vec3f(-0.04f, 0f, -0.04f),
			AddVelocity = new Vec3f(0.08f, 0f, 0.08f),
			addLifeLength = 0.5f,
			LifeLength = 0.5f,
			MinQuantity = 0.25f,
			GravityEffect = 0.5f,
			SelfPropelled = true,
			MinSize = 0.1f,
			MaxSize = 0.2f
		};
	}

	public BlockEntityFruitPress()
	{
		inv = new InventoryGeneric(2, "fruitpress-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockFruitPress;
		capi = api as ICoreClientAPI;
		if (ownBlock == null)
		{
			return;
		}
		Shape shape = Shape.TryGet(api, "shapes/block/wood/fruitpress/part-movable.json");
		if (api.Side == EnumAppSide.Client)
		{
			capi.Tesselator.TesselateShape(ownBlock, shape, out meshMovable, new Vec3f(0f, ownBlock.Shape.rotateY, 0f));
			animUtil.InitializeAnimator("fruitpress", shape, null, new Vec3f(0f, ownBlock.Shape.rotateY, 0f));
		}
		else
		{
			shape.InitForAnimations(api.Logger, "shapes/block/wood/fruitpress/part-movable.json");
			animUtil.InitializeAnimatorServer("fruitpress", shape);
		}
		if (api.Side == EnumAppSide.Client)
		{
			renderer = new FruitpressContentsRenderer(api as ICoreClientAPI, Pos, this);
			(api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "fruitpress");
			renderer.reloadMeshes(getJuiceableProps(mashStack), mustReload: true);
			genBucketMesh();
		}
		else if (serverListenerActive)
		{
			if (loadedFrame > 0f)
			{
				animUtil.StartAnimation(compressAnimMeta);
			}
			if (listenerId == 0L)
			{
				listenerId = RegisterGameTickListener(onTick100msServer, 25);
			}
		}
	}

	private void onTick25msClient(float dt)
	{
		double num = mashStack?.Attributes.GetDouble("squeezeRel", 1.0) ?? 1.0;
		float num2 = (float)(juiceableLitresTransfered + juiceableLitresLeft) / 10f;
		if (MashSlot.Empty || renderer.juiceTexPos == null || num >= 1.0 || pressSqueezeRel > num || squeezedLitresLeft < 0.01)
		{
			return;
		}
		Random rand = Api.World.Rand;
		liquidParticles.MinQuantity = (float)juiceableLitresLeft / 10f;
		for (int i = 0; i < 4; i++)
		{
			BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
			liquidParticles.Color = capi.BlockTextureAtlas.GetRandomColor(renderer.juiceTexPos, rand.Next(30));
			Vec3d vec3d = blockFacing.Plane.Startd.Add(-0.5, 0.0, -0.5);
			Vec3d vec3d2 = blockFacing.Plane.Endd.Add(-0.5, 0.0, -0.5);
			vec3d.Mul(0.5);
			vec3d2.Mul(0.5);
			vec3d2.Y = 0.3125 - (1.0 - num + (double)Math.Max(0f, 0.9f - num2)) * 0.5;
			vec3d.Add(blockFacing.Normalf.X * 1.2f / 16f, 0.0, blockFacing.Normalf.Z * 1.2f / 16f);
			vec3d2.Add(blockFacing.Normalf.X * 1.2f / 16f, 0.0, blockFacing.Normalf.Z * 1.2f / 16f);
			liquidParticles.MinPos = vec3d;
			liquidParticles.AddPos = vec3d2.Sub(vec3d);
			liquidParticles.MinPos.Add(Pos).Add(0.5, 1.0, 0.5);
			Api.World.SpawnParticles(liquidParticles);
		}
		if (num < 0.8999999761581421)
		{
			liquidParticles.MinPos = Pos.ToVec3d().Add(0.375, 0.699999988079071, 0.375);
			liquidParticles.AddPos.Set(0.25, 0.0, 0.25);
			for (int j = 0; j < 3; j++)
			{
				liquidParticles.Color = capi.BlockTextureAtlas.GetRandomColor(renderer.juiceTexPos, rand.Next(30));
				Api.World.SpawnParticles(liquidParticles);
			}
		}
	}

	private void onTick100msServer(float dt)
	{
		RunningAnimation animationState = animUtil.animator.GetAnimationState("compress");
		if (serverListenerActive)
		{
			animationState.CurrentFrame = loadedFrame.GetValueOrDefault();
			updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
			serverListenerActive = false;
			loadedFrame = null;
			return;
		}
		if (CompressAnimActive)
		{
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = compressAnimMeta.AnimationSpeed,
				CurrentFrame = animationState.CurrentFrame
			});
		}
		if (MashSlot.Empty)
		{
			return;
		}
		JuiceableProperties juiceableProps = getJuiceableProps(mashStack);
		double totalHours = Api.World.Calendar.TotalHours;
		double num = mashStack.Attributes.GetDouble("squeezeRel", 1.0);
		double num2 = 0.0;
		if (Api.Side == EnumAppSide.Server && CompressAnimActive && num < 1.0 && pressSqueezeRel <= num && juiceableLitresLeft > 0.0)
		{
			squeezedLitresLeft = Math.Max(Math.Max(0.0, squeezedLitresLeft), juiceableLitresLeft - (juiceableLitresLeft + juiceableLitresTransfered) * screwPercent);
			num2 = Math.Min(squeezedLitresLeft, Math.Round((totalHours - lastLiquidTransferTotalHours) * (CompressAnimActive ? GameMath.Clamp(squeezedLitresLeft * (1.0 - num) * 500.0, 25.0, 100.0) : 5.0), 2));
			if (!squeezeSoundPlayed)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/player/wetclothsqueeze.ogg"), Pos, 0.0, null, randomizePitch: false);
				squeezeSoundPlayed = true;
			}
		}
		BlockLiquidContainerBase blockLiquidContainerBase = BucketSlot?.Itemstack?.Collectible as BlockLiquidContainerBase;
		if (Api.Side == EnumAppSide.Server && juiceableProps != null && squeezedLitresLeft > 0.0)
		{
			ItemStack resolvedItemstack = juiceableProps.LiquidStack.ResolvedItemstack;
			resolvedItemstack.StackSize = 999999;
			float num3;
			if (blockLiquidContainerBase != null && !blockLiquidContainerBase.IsFull(BucketSlot.Itemstack))
			{
				float currentLitres = blockLiquidContainerBase.GetCurrentLitres(BucketSlot.Itemstack);
				if (num2 > 0.0)
				{
					blockLiquidContainerBase.TryPutLiquid(BucketSlot.Itemstack, resolvedItemstack, (float)num2);
				}
				num3 = blockLiquidContainerBase.GetCurrentLitres(BucketSlot.Itemstack) - currentLitres;
			}
			else
			{
				num3 = (float)num2;
			}
			juiceableLitresLeft -= num3;
			squeezedLitresLeft -= ((pressSqueezeRel <= num) ? num3 : (num3 * 100f));
			juiceableLitresTransfered += num3;
			lastLiquidTransferTotalHours = totalHours;
			MarkDirty(redrawOnClient: true);
		}
		else if (Api.Side == EnumAppSide.Server && (!CompressAnimActive || juiceableLitresLeft <= 0.0))
		{
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
		}
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel, EnumFruitPressSection section, bool firstEvent)
	{
		firstEvent |= Api.Side == EnumAppSide.Server;
		if (section == EnumFruitPressSection.MashContainer && firstEvent)
		{
			return InteractMashContainer(byPlayer, blockSel);
		}
		if (section == EnumFruitPressSection.Ground && firstEvent)
		{
			return InteractGround(byPlayer, blockSel);
		}
		if (section == EnumFruitPressSection.Screw)
		{
			return InteractScrew(byPlayer, blockSel, firstEvent);
		}
		return false;
	}

	private bool InteractScrew(IPlayer byPlayer, BlockSelection blockSel, bool firstEvent)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			return true;
		}
		if (!CompressAnimActive && !byPlayer.Entity.Controls.CtrlKey && firstEvent)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1002);
			return true;
		}
		if (CanUnscrew && (byPlayer.Entity.Controls.CtrlKey || (CompressAnimFinished && !byPlayer.Entity.Controls.CtrlKey)) && firstEvent)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			return true;
		}
		if (compressAnimMeta.AnimationSpeed == 0f && !byPlayer.Entity.Controls.CtrlKey)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1004);
			return true;
		}
		return false;
	}

	private bool InteractMashContainer(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack itemstack = activeHotbarSlot.Itemstack;
		if (CompressAnimActive)
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "compressing", Lang.Get("Release the screw first to add/remove fruit"));
			return false;
		}
		if (!activeHotbarSlot.Empty)
		{
			JuiceableProperties juiceableProps = getJuiceableProps(itemstack);
			if (juiceableProps == null)
			{
				return false;
			}
			if (!juiceableProps.LitresPerItem.HasValue && !itemstack.Attributes.HasAttribute("juiceableLitresLeft"))
			{
				return false;
			}
			ItemStack itemStack = (juiceableProps.LitresPerItem.HasValue ? juiceableProps.PressedStack.ResolvedItemstack.Clone() : itemstack.GetEmptyClone());
			if (MashSlot.Empty)
			{
				MashSlot.Itemstack = itemStack;
				if (!juiceableProps.LitresPerItem.HasValue)
				{
					mashStack.StackSize = 1;
					dryStackSize = GameMath.RoundRandom(Api.World.Rand, ((float)juiceableLitresLeft + (float)juiceableLitresTransfered) * getJuiceableProps(mashStack).PressedDryRatio);
					activeHotbarSlot.TakeOut(1);
					MarkDirty(redrawOnClient: true);
					renderer?.reloadMeshes(juiceableProps, mustReload: true);
					(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					return true;
				}
			}
			else if (juiceableLitresLeft + juiceableLitresTransfered >= juiceableLitresCapacity)
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
				return false;
			}
			if (!mashStack.Equals(Api.World, itemStack, GlobalConstants.IgnoredStackAttributes.Append("juiceableLitresLeft", "juiceableLitresTransfered", "squeezeRel")))
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Cannot mix fruit"));
				return false;
			}
			float num = (float)itemstack.Attributes.GetDecimal("juiceableLitresLeft");
			float num2 = (float)itemstack.Attributes.GetDecimal("juiceableLitresTransfered");
			int num4;
			if (!juiceableProps.LitresPerItem.HasValue)
			{
				if (juiceableLitresLeft + juiceableLitresTransfered + (double)num + (double)num2 > juiceableLitresCapacity)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
					return false;
				}
				TransitionState[] array = itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, activeHotbarSlot);
				TransitionState[] array2 = mashStack.Collectible.UpdateAndGetTransitionStates(Api.World, MashSlot);
				if (array != null && array2 != null)
				{
					Dictionary<EnumTransitionType, TransitionState> dictionary = null;
					dictionary = new Dictionary<EnumTransitionType, TransitionState>();
					TransitionState[] array3 = array2;
					foreach (TransitionState transitionState in array3)
					{
						dictionary[transitionState.Props.Type] = transitionState;
					}
					float num3 = (num + num2) / (num + num2 + (float)juiceableLitresLeft + (float)juiceableLitresTransfered);
					array3 = array;
					foreach (TransitionState transitionState2 in array3)
					{
						TransitionState transitionState3 = dictionary[transitionState2.Props.Type];
						mashStack.Collectible.SetTransitionState(mashStack, transitionState2.Props.Type, transitionState2.TransitionedHours * num3 + transitionState3.TransitionedHours * (1f - num3));
					}
				}
				num4 = 1;
			}
			else
			{
				int num5 = Math.Min(itemstack.StackSize, byPlayer.Entity.Controls.ShiftKey ? 1 : (byPlayer.Entity.Controls.CtrlKey ? itemstack.Item.MaxStackSize : 4));
				while ((double)((float)num5 * juiceableProps.LitresPerItem.Value) + juiceableLitresLeft + juiceableLitresTransfered > juiceableLitresCapacity)
				{
					num5--;
				}
				if (num5 <= 0)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
					return false;
				}
				num = (float)num5 * juiceableProps.LitresPerItem.Value;
				num4 = num5;
			}
			if (num4 > 0)
			{
				AssetLocation code = activeHotbarSlot.Itemstack.Collectible.Code;
				activeHotbarSlot.TakeOut(num4);
				Api.World.Logger.Audit("{0} Put {1}x{2} into Fruitpress at {3}.", byPlayer.PlayerName, num4, code, blockSel.Position);
				mashStack.Attributes.SetDouble("juiceableLitresLeft", juiceableLitresLeft += num);
				mashStack.Attributes.SetDouble("juiceableLitresTransfered", juiceableLitresTransfered += num2);
				mashStack.StackSize = 1;
				dryStackSize = GameMath.RoundRandom(Api.World.Rand, ((float)juiceableLitresLeft + (float)juiceableLitresTransfered) * getJuiceableProps(mashStack).PressedDryRatio);
				activeHotbarSlot.MarkDirty();
				MarkDirty(redrawOnClient: true);
				renderer?.reloadMeshes(juiceableProps, mustReload: true);
				(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
			return true;
		}
		if (MashSlot.Empty)
		{
			return false;
		}
		convertDryMash();
		if (!byPlayer.InventoryManager.TryGiveItemstack(mashStack, slotNotifyEffect: true))
		{
			Api.World.SpawnItemEntity(mashStack, Pos);
		}
		Api.World.Logger.Audit("{0} Took 1x{1} from Fruitpress at {2}.", byPlayer.PlayerName, mashStack.Collectible.Code, blockSel.Position);
		MashSlot.Itemstack = null;
		renderer?.reloadMeshes(null, mustReload: true);
		if (Api.Side == EnumAppSide.Server)
		{
			MarkDirty(redrawOnClient: true);
		}
		return true;
	}

	private bool InteractGround(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack itemstack = activeHotbarSlot.Itemstack;
		if (activeHotbarSlot.Empty && !BucketSlot.Empty)
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(BucketSlot.Itemstack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(BucketSlot.Itemstack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Fruitpress at {2}.", byPlayer.PlayerName, BucketSlot.Itemstack.Collectible.Code, blockSel.Position);
			if (BucketSlot.Itemstack.Block != null)
			{
				Api.World.PlaySoundAt(BucketSlot.Itemstack.Block.Sounds.Place, Pos, -0.5, byPlayer);
			}
			BucketSlot.Itemstack = null;
			MarkDirty(redrawOnClient: true);
			bucketMesh?.Clear();
		}
		else if (itemstack != null && itemstack.Collectible is BlockLiquidContainerBase { AllowHeldLiquidTransfer: not false, IsTopOpened: not false, CapacityLitres: <20f } && BucketSlot.Empty && activeHotbarSlot.TryPutInto(Api.World, BucketSlot) > 0)
		{
			Api.World.Logger.Audit("{0} Put 1x{1} into Fruitpress at {2}.", byPlayer.PlayerName, BucketSlot.Itemstack.Collectible.Code, blockSel.Position);
			activeHotbarSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
			genBucketMesh();
			Api.World.PlaySoundAt(itemstack.Block.Sounds.Place, Pos, -0.5, byPlayer);
		}
		return true;
	}

	public bool OnBlockInteractStep(float secondsUsed, IPlayer byPlayer, EnumFruitPressSection section)
	{
		if (section != EnumFruitPressSection.Screw)
		{
			return false;
		}
		if (mashStack != null)
		{
			updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		}
		if (!CompressAnimActive)
		{
			return (base.Block as BlockFruitPress).RightMouseDown;
		}
		return true;
	}

	public void OnBlockInteractStop(float secondsUsed, IPlayer byPlayer)
	{
		updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		if (CompressAnimActive)
		{
			compressAnimMeta.AnimationSpeed = 0f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0f
			});
		}
	}

	private void updateSqueezeRel(RunningAnimation anim)
	{
		if (anim != null && mashStack != null)
		{
			double num = GameMath.Clamp(1f - anim.CurrentFrame / (float)anim.Animation.QuantityFrames / 2f, 0.1f, 1f);
			float num2 = (float)(juiceableLitresTransfered + juiceableLitresLeft) / 10f;
			num += (double)Math.Max(0f, 0.9f - num2);
			pressSqueezeRel = GameMath.Clamp(num, 0.10000000149011612, 1.0);
			num = GameMath.Clamp(Math.Min(mashStack.Attributes.GetDouble("squeezeRel", 1.0), num), 0.10000000149011612, 1.0);
			mashStack.Attributes.SetDouble("squeezeRel", num);
			screwPercent = GameMath.Clamp(1f - anim.CurrentFrame / (float)(anim.Animation.QuantityFrames - 1), 0f, 1f) / num2;
		}
	}

	private void convertDryMash()
	{
		if (!(juiceableLitresLeft < 0.01))
		{
			return;
		}
		JuiceableProperties juiceableProps = getJuiceableProps(mashStack);
		if (juiceableProps?.ReturnStack?.ResolvedItemstack != null && mashStack != null)
		{
			double num = Math.Round(juiceableLitresTransfered, 2, MidpointRounding.AwayFromZero);
			MashSlot.Itemstack = juiceableProps.ReturnStack.ResolvedItemstack.Clone();
			mashStack.StackSize = (int)((double)mashStack.StackSize * num);
		}
		else
		{
			mashStack?.Attributes?.RemoveAttribute("juiceableLitresTransfered");
			mashStack?.Attributes?.RemoveAttribute("juiceableLitresLeft");
			mashStack?.Attributes?.RemoveAttribute("squeezeRel");
			if (mashStack?.Collectible.Code.Path != "rot")
			{
				mashStack.StackSize = dryStackSize;
			}
		}
		dryStackSize = 0;
	}

	public bool OnBlockInteractCancel(float secondsUsed, IPlayer byPlayer)
	{
		updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		if (CompressAnimActive)
		{
			compressAnimMeta.AnimationSpeed = 0f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0f
			});
		}
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		switch (packetid)
		{
		case 1002:
			compressAnimMeta.AnimationSpeed = 0.5f;
			animUtil.StartAnimation(compressAnimMeta);
			squeezeSoundPlayed = false;
			lastLiquidTransferTotalHours = Api.World.Calendar.TotalHours;
			if (listenerId == 0L)
			{
				listenerId = RegisterGameTickListener(onTick100msServer, 25);
			}
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewStart,
				AnimationSpeed = 0.5f
			});
			break;
		case 1004:
			compressAnimMeta.AnimationSpeed = 0.5f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0.5f
			});
			break;
		case 1003:
			compressAnimMeta.AnimationSpeed = 1.5f;
			animUtil.StopAnimation("compress");
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.Unscrew,
				AnimationSpeed = 1.5f
			});
			animUtil.animator.GetAnimationState("compress").Stop();
			if (MashSlot.Empty && listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			break;
		}
		base.OnReceivedClientPacket(fromPlayer, packetid, data);
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			FruitPressAnimPacket fruitPressAnimPacket = SerializerUtil.Deserialize<FruitPressAnimPacket>(data);
			compressAnimMeta.AnimationSpeed = fruitPressAnimPacket.AnimationSpeed;
			if (fruitPressAnimPacket.AnimationState == EnumFruitPressAnimState.ScrewStart)
			{
				animUtil.StartAnimation(compressAnimMeta);
				squeezeSoundPlayed = false;
				lastLiquidTransferTotalHours = Api.World.Calendar.TotalHours;
				if (listenerId == 0L)
				{
					listenerId = RegisterGameTickListener(onTick25msClient, 25);
				}
			}
			else if (fruitPressAnimPacket.AnimationState == EnumFruitPressAnimState.ScrewContinue)
			{
				RunningAnimation animationState = animUtil.animator.GetAnimationState("compress");
				if (animationState.CurrentFrame <= 0f && fruitPressAnimPacket.CurrentFrame > 0f)
				{
					compressAnimMeta.AnimationSpeed = 0.0001f;
					animUtil.StartAnimation(compressAnimMeta);
				}
				if (animationState.CurrentFrame > 0f && animationState.CurrentFrame < fruitPressAnimPacket.CurrentFrame)
				{
					compressAnimMeta.AnimationSpeed = 0.0001f;
					while (animationState.CurrentFrame < fruitPressAnimPacket.CurrentFrame && animationState.CurrentFrame < (float)(animationState.Animation.QuantityFrames - 1))
					{
						animationState.Progress(1f, 1f);
					}
					compressAnimMeta.AnimationSpeed = fruitPressAnimPacket.AnimationSpeed;
					animationState.CurrentFrame = fruitPressAnimPacket.CurrentFrame;
					MarkDirty(redrawOnClient: true);
					updateSqueezeRel(animationState);
				}
				if (listenerId == 0L)
				{
					listenerId = RegisterGameTickListener(onTick25msClient, 25);
				}
			}
			else if (fruitPressAnimPacket.AnimationState == EnumFruitPressAnimState.Unscrew)
			{
				animUtil.StopAnimation("compress");
				if (listenerId != 0L)
				{
					UnregisterGameTickListener(listenerId);
					listenerId = 0L;
				}
			}
		}
		base.OnReceivedServerPacket(packetid, data);
	}

	public JuiceableProperties getJuiceableProps(ItemStack stack)
	{
		JuiceableProperties obj = ((stack != null && stack.ItemAttributes?["juiceableProperties"].Exists == true) ? stack.ItemAttributes["juiceableProperties"].AsObject<JuiceableProperties>(null, stack.Collectible.Code.Domain) : null);
		obj?.LiquidStack?.Resolve(Api.World, "juiceable properties liquidstack", stack.Collectible.Code);
		obj?.PressedStack?.Resolve(Api.World, "juiceable properties pressedstack", stack.Collectible.Code);
		if (obj != null)
		{
			JsonItemStack returnStack = obj.ReturnStack;
			if (returnStack != null)
			{
				returnStack.Resolve(Api.World, "juiceable properties returnstack", stack.Collectible.Code);
				return obj;
			}
			return obj;
		}
		return obj;
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (!MashSlot.Empty)
		{
			convertDryMash();
		}
		base.OnBlockBroken(byPlayer);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		bool empty = Inventory.Empty;
		ItemStack itemStack = mashStack;
		base.FromTreeAttributes(tree, worldForResolving);
		squeezedLitresLeft = tree.GetDouble("squeezedLitresLeft");
		squeezeSoundPlayed = tree.GetBool("squeezeSoundPlayed");
		dryStackSize = tree.GetInt("dryStackSize");
		lastLiquidTransferTotalHours = tree.GetDouble("lastLiquidTransferTotalHours");
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			if (listenerId > 0 && juiceableLitresLeft <= 0.0)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			renderer?.reloadMeshes(getJuiceableProps(mashStack), empty != Inventory.Empty || (itemStack != null && mashStack != null && !itemStack.Equals(Api.World, mashStack, GlobalConstants.IgnoredStackAttributes)));
			genBucketMesh();
			return;
		}
		if (listenerId == 0L)
		{
			serverListenerActive = tree.GetBool("ServerListenerActive");
		}
		if (listenerId != 0L || serverListenerActive)
		{
			loadedFrame = tree.GetFloat("CurrentFrame");
			compressAnimMeta.AnimationSpeed = tree.GetFloat("AnimationSpeed", compressAnimMeta.AnimationSpeed);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("squeezedLitresLeft", squeezedLitresLeft);
		tree.SetBool("squeezeSoundPlayed", squeezeSoundPlayed);
		tree.SetInt("dryStackSize", dryStackSize);
		tree.SetDouble("lastLiquidTransferTotalHours", lastLiquidTransferTotalHours);
		if (Api.Side == EnumAppSide.Server)
		{
			if (listenerId != 0L)
			{
				tree.SetBool("ServerListenerActive", value: true);
			}
			if (CompressAnimActive)
			{
				tree.SetFloat("CurrentFrame", animUtil.animator.GetAnimationState("compress").CurrentFrame);
				tree.SetFloat("AnimationSpeed", compressAnimMeta.AnimationSpeed);
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(meshMovable);
		}
		mesher.AddMeshData(bucketMesh);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!BucketSlot.Empty && BucketSlot.Itemstack.Collectible is BlockLiquidContainerBase blockLiquidContainerBase)
		{
			dsc.Append(Lang.Get("Container:") + " ");
			blockLiquidContainerBase.GetContentInfo(BucketSlot, dsc, Api.World);
			dsc.AppendLine();
		}
		if (MashSlot.Empty)
		{
			return;
		}
		JuiceableProperties juiceableProps = getJuiceableProps(mashStack);
		if (juiceableLitresLeft >= 0.01 && mashStack.Collectible.Code.Path != "rot")
		{
			string text = juiceableProps.LiquidStack.ResolvedItemstack.GetName().ToLowerInvariant();
			dsc.AppendLine(Lang.GetWithFallback("fruitpress-litreswhensqueezed", "Mash produces {0:0.##} litres of juice when squeezed", juiceableLitresLeft, text));
			return;
		}
		int num = ((mashStack.Collectible.Code.Path != "rot") ? dryStackSize : MashSlot.StackSize);
		string text2 = MashSlot.GetStackName().ToLowerInvariant();
		if (juiceableProps?.ReturnStack?.ResolvedItemstack != null)
		{
			num = (int)((double)juiceableProps.ReturnStack.ResolvedItemstack.StackSize * Math.Round(juiceableLitresTransfered, 2, MidpointRounding.AwayFromZero));
			text2 = juiceableProps.ReturnStack.ResolvedItemstack.GetName().ToLowerInvariant();
		}
		dsc.AppendLine(Lang.Get("{0}x {1}", num, text2));
	}

	private void genBucketMesh()
	{
		if (BucketSlot.Empty || capi == null)
		{
			bucketMesh?.Clear();
			return;
		}
		ItemStack itemstack = BucketSlot.Itemstack;
		IContainedMeshSource containedMeshSource = itemstack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
		if (containedMeshSource != null)
		{
			bucketMeshTmp = containedMeshSource.GenMesh(itemstack, capi.BlockTextureAtlas, Pos);
			bucketMeshTmp.CustomInts = new CustomMeshDataPartInt(bucketMeshTmp.FlagsCount);
			bucketMeshTmp.CustomInts.Count = bucketMeshTmp.FlagsCount;
			bucketMeshTmp.CustomInts.Values.Fill(268435456);
			bucketMeshTmp.CustomFloats = new CustomMeshDataPartFloat(bucketMeshTmp.FlagsCount * 2);
			bucketMeshTmp.CustomFloats.Count = bucketMeshTmp.FlagsCount * 2;
			bucketMesh = bucketMeshTmp.Clone();
		}
	}
}
