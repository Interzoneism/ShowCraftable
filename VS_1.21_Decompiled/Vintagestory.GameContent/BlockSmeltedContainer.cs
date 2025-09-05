using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockSmeltedContainer : Block, IGroundStoredParticleEmitter
{
	public static SimpleParticleProperties smokeHeld;

	public static SimpleParticleProperties smokePouring;

	public static SimpleParticleProperties bigMetalSparks;

	private Vec3d gsSmokePos = new Vec3d(0.45, 0.44, 0.45);

	static BlockSmeltedContainer()
	{
		smokeHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 180, 180, 180), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, 0.1f, -0.25f), new Vec3f(0.25f, 0.1f, 0.25f), 1.5f, -0.075f, 0.25f, 0.25f, EnumParticleModel.Quad);
		smokeHeld.AddPos.Set(0.1, 0.1, 0.1);
		smokePouring = new SimpleParticleProperties(1f, 2f, ColorUtil.ToRgba(50, 180, 180, 180), new Vec3d(), new Vec3d(), new Vec3f(-0.5f, 0f, -0.5f), new Vec3f(0.5f, 0f, 0.5f), 1.5f, -0.1f, 0.75f, 0.75f, EnumParticleModel.Quad);
		smokePouring.AddPos.Set(0.3, 0.3, 0.3);
		bigMetalSparks = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 255, 169, 83), new Vec3d(), new Vec3d(), new Vec3f(-3f, 1f, -3f), new Vec3f(3f, 8f, 3f), 0.5f, 1f, 0.25f, 0.25f);
		bigMetalSparks.VertexFlags = 128;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		gsSmokePos.Y = CollisionBoxes.FirstOrDefault().MaxY;
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		return "pour";
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		if (byEntity.World is IClientWorldAccessor && byEntity.World.Rand.NextDouble() < 0.02)
		{
			KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);
			if (contents.Key != null && !HasSolidifed(slot.Itemstack, contents.Key, byEntity.World))
			{
				Vec3d vec3d = byEntity.Pos.XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.5, byEntity.LocalEyePos.Z).Ahead(0.30000001192092896, byEntity.Pos.Pitch, byEntity.Pos.Yaw).Ahead(0.4699999988079071, 0f, byEntity.Pos.Yaw + (float)Math.PI / 2f);
				smokeHeld.MinPos = vec3d.AddCopy(-0.05, -0.05, -0.05);
				byEntity.World.SpawnParticles(smokeHeld);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		ILiquidMetalSink liquidMetalSink = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ILiquidMetalSink;
		if (liquidMetalSink != null)
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
		if (liquidMetalSink != null && liquidMetalSink.CanReceiveAny)
		{
			KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);
			if (contents.Key == null)
			{
				string domainAndPath = Attributes["emptiedBlockCode"].AsString();
				slot.Itemstack = new ItemStack(byEntity.World.GetBlock(AssetLocation.Create(domainAndPath, Code.Domain)));
				slot.MarkDirty();
				handHandling = EnumHandHandling.PreventDefault;
				return;
			}
			if (HasSolidifed(slot.Itemstack, contents.Key, byEntity.World))
			{
				handHandling = EnumHandHandling.NotHandled;
				return;
			}
			if (contents.Value <= 0 || !liquidMetalSink.CanReceive(contents.Key))
			{
				return;
			}
			liquidMetalSink.BeginFill(blockSel.HitPosition);
			byEntity.World.RegisterCallback(delegate(IWorldAccessor world, BlockPos pos, float dt)
			{
				if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
				{
					IPlayer dualCallByPlayer = null;
					if (byEntity is EntityPlayer)
					{
						dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
					}
					world.PlaySoundAt(new AssetLocation("sounds/pourmetal"), byEntity, dualCallByPlayer);
				}
			}, blockSel.Position, 666);
			handHandling = EnumHandHandling.PreventDefault;
		}
		if (handHandling == EnumHandHandling.NotHandled)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return false;
		}
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is ILiquidMetalSink liquidMetalSink))
		{
			return false;
		}
		if (!liquidMetalSink.CanReceiveAny)
		{
			return false;
		}
		KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);
		if (!liquidMetalSink.CanReceive(contents.Key))
		{
			return false;
		}
		float num = 1.5f;
		float temperature = GetTemperature(byEntity.World, slot.Itemstack);
		IPlayer player = (byEntity as EntityPlayer).Player;
		if (secondsUsed > 1f / num)
		{
			if (!slot.Itemstack.Attributes.HasAttribute("nowPouringEntityId"))
			{
				slot.Itemstack.Attributes.SetLong("nowPouringEntityId", byEntity.EntityId);
				slot.MarkDirty();
			}
			if ((int)(30f * secondsUsed) % 3 == 1)
			{
				Vec3d vec3d = byEntity.Pos.XYZ.Ahead(0.10000000149011612, byEntity.Pos.Pitch, byEntity.Pos.Yaw).Ahead(1.0, byEntity.Pos.Pitch, byEntity.Pos.Yaw - (float)Math.PI / 2f);
				vec3d.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
				smokePouring.MinPos = vec3d.AddCopy(-0.15, -0.15, -0.15);
				Vec3d vec3d2 = blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5);
				bigMetalSparks.MinQuantity = Math.Max(0.2f, 1f - (secondsUsed - 1f) / 4f);
				float num2 = 0f;
				Cuboidf[] collisionBoxes = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).GetCollisionBoxes(byEntity.World.BlockAccessor, blockSel.Position);
				int num3 = 0;
				while (collisionBoxes != null && num3 < collisionBoxes.Length)
				{
					num2 = Math.Max(num2, collisionBoxes[num3].Y2);
					num3++;
				}
				bigMetalSparks.MinVelocity.Set(-2f, 1f, -2f);
				bigMetalSparks.AddVelocity.Set(4f, 5f, 4f);
				bigMetalSparks.MinPos = vec3d2.AddCopy(-0.25, num2 - 0.125f, -0.25);
				bigMetalSparks.AddPos.Set(0.5, 0.0, 0.5);
				bigMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)temperature - 770, 48, 128);
				byEntity.World.SpawnParticles(bigMetalSparks, player);
				byEntity.World.SpawnParticles(Math.Max(1f, 12f - (secondsUsed - 1f) * 6f), ColorUtil.ToRgba(50, 180, 180, 180), vec3d2.AddCopy(-0.5, num2 - 0.125f, -0.5), vec3d2.Add(0.5, (double)(num2 - 0.125f) + 0.15, 0.5), new Vec3f(-0.5f, 0f, -0.5f), new Vec3f(0.5f, 0f, 0.5f), 1.5f, -0.05f, 0.4f, EnumParticleModel.Quad, player);
			}
			int transferedAmount = Math.Min(2, contents.Value);
			liquidMetalSink.ReceiveLiquidMetal(contents.Key, ref transferedAmount, temperature);
			int num4 = Math.Max(0, contents.Value - (2 - transferedAmount));
			slot.Itemstack.Attributes.SetInt("units", num4);
			if (num4 <= 0 && byEntity.World is IServerWorldAccessor)
			{
				string domainAndPath = Attributes["emptiedBlockCode"].AsString();
				slot.Itemstack = new ItemStack(byEntity.World.GetBlock(AssetLocation.Create(domainAndPath, Code.Domain)));
				slot.MarkDirty();
				OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
				return false;
			}
			return true;
		}
		return true;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		if (target == EnumItemRenderTarget.HandTp || target == EnumItemRenderTarget.HandTpOff)
		{
			long num = itemstack.Attributes.GetLong("nowPouringEntityId", 0L);
			if (num != 0L && capi.World.GetEntityById(num) is EntityAgent byEntity && (num != capi.World.Player.Entity.EntityId || capi.World.Player.CameraMode != EnumCameraMode.FirstPerson || capi.Settings.Bool["immersiveFpMode"]))
			{
				SpawnPouringParticles(byEntity);
			}
		}
	}

	private void SpawnPouringParticles(EntityAgent byEntity)
	{
		EntityPlayer entityPlayer = byEntity as EntityPlayer;
		_ = entityPlayer.Player;
		AttachmentPointAndPose attachmentPointPose = byEntity.AnimManager.Animator.GetAttachmentPointPose("RightHand");
		if (attachmentPointPose != null && api.World.Rand.NextDouble() < 0.25)
		{
			AttachmentPoint attachPoint = attachmentPointPose.AttachPoint;
			float bodyYaw = entityPlayer.BodyYaw;
			float num = ((entityPlayer.Properties.Client.Shape != null) ? entityPlayer.Properties.Client.Shape.rotateX : 0f);
			float num2 = ((entityPlayer.Properties.Client.Shape != null) ? entityPlayer.Properties.Client.Shape.rotateY : 0f);
			float num3 = ((entityPlayer.Properties.Client.Shape != null) ? entityPlayer.Properties.Client.Shape.rotateZ : 0f);
			float walkPitch = entityPlayer.WalkPitch;
			Matrixf matrixf = new Matrixf().RotateX(entityPlayer.SidedPos.Roll + num * ((float)Math.PI / 180f)).RotateY(bodyYaw + (180f + num2) * ((float)Math.PI / 180f)).RotateZ(walkPitch + num3 * ((float)Math.PI / 180f))
				.Scale(entityPlayer.Properties.Client.Size, entityPlayer.Properties.Client.Size, entityPlayer.Properties.Client.Size)
				.Translate(-0.5f, 0f, -0.5f)
				.RotateX(entityPlayer.sidewaysSwivelAngle)
				.Translate(attachPoint.PosX / 16.0, attachPoint.PosY / 16.0, attachPoint.PosZ / 16.0)
				.Mul(attachmentPointPose.AnimModelMatrix)
				.Translate(-0.15f, 0f, 0.15f);
			float[] vec = new float[4] { 0f, 0f, 0f, 1f };
			float[] array = Mat4f.MulWithVec4(matrixf.Values, vec);
			bigMetalSparks.GravityEffect = 0.5f;
			bigMetalSparks.Bounciness = 0.6f;
			bigMetalSparks.MinQuantity = 1f;
			bigMetalSparks.AddQuantity = 1f;
			bigMetalSparks.MinPos = new Vec3d(entityPlayer.Pos.X + (double)array[0], entityPlayer.Pos.InternalY + (double)array[1], entityPlayer.Pos.Z + (double)array[2]);
			bigMetalSparks.AddPos.Set(0.0, 0.0, 0.0);
			bigMetalSparks.MinSize = 0.75f;
			float num4 = (float)Math.Sin(bodyYaw + (float)Math.PI / 2f) / 2f;
			float num5 = (float)Math.Cos(bodyYaw + (float)Math.PI / 2f) / 2f;
			bigMetalSparks.MinVelocity.Set(-0.1f + num4, -1f, -0.1f + num5);
			bigMetalSparks.AddVelocity.Set(0.2f + num5, 1f, 0.2f + num5);
			byEntity.World.SpawnParticles(bigMetalSparks, entityPlayer?.Player);
			byEntity.World.SpawnParticles(4f, ColorUtil.ToRgba(50, 180, 180, 180), bigMetalSparks.MinPos, bigMetalSparks.MinPos.AddCopy(num4 / 5f, -0.3, num5 / 5f), new Vec3f(-0.5f, 0f, -0.5f), new Vec3f(0.5f, 0f, 0.5f), 1.5f, -0.05f, 0.4f, EnumParticleModel.Quad, entityPlayer.Player);
		}
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		slot.Itemstack?.Attributes.RemoveAttribute("nowPouringEntityId");
		slot.MarkDirty();
		if (blockSel != null)
		{
			(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ILiquidMetalSink)?.OnPourOver();
		}
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		slot.Itemstack?.Attributes.RemoveAttribute("nowPouringEntityId");
		return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return GetDrops(world, pos, null)[0];
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer);
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity is BlockEntitySmeltedContainer)
		{
			BlockEntitySmeltedContainer blockEntitySmeltedContainer = (BlockEntitySmeltedContainer)blockEntity;
			ItemStack itemStack = blockEntitySmeltedContainer.contents.Clone();
			SetContents(drops[0], itemStack, blockEntitySmeltedContainer.units);
			blockEntitySmeltedContainer.contents?.ResolveBlockOrItem(world);
			drops[0].Collectible.SetTemperature(world, drops[0], blockEntitySmeltedContainer.contents.Collectible.GetTemperature(world, itemStack));
		}
		return drops;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		KeyValuePair<ItemStack, int> contents = GetContents(api.World, itemStack);
		string text = contents.Key?.Collectible?.Variant["metal"];
		string text2 = ((text != null) ? Lang.Get("material-" + text) : contents.Key?.GetName());
		if (HasSolidifed(itemStack, contents.Key, api.World))
		{
			return Lang.GetWithFallback("crucible-smelted-solid", "Crucible (Contains solidified {0})", text2, base.GetHeldItemName(itemStack));
		}
		return Lang.GetWithFallback("crucible-smelted-molten", "Crucible (Contains molten {0})", text2, base.GetHeldItemName(itemStack));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity is BlockEntitySmeltedContainer)
		{
			BlockEntitySmeltedContainer blockEntitySmeltedContainer = (BlockEntitySmeltedContainer)blockEntity;
			blockEntitySmeltedContainer.contents.ResolveBlockOrItem(world);
			string metal = BlockSmeltingContainer.GetMetal(blockEntitySmeltedContainer.contents);
			return Lang.Get("blocksmeltedcontainer-contents", blockEntitySmeltedContainer.units, metal, (int)blockEntitySmeltedContainer.Temperature);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		KeyValuePair<ItemStack, int> contents = GetContents(world, inSlot.Itemstack);
		if (contents.Key != null)
		{
			string metal = BlockSmeltingContainer.GetMetal(contents.Key);
			dsc.Append(Lang.Get("item-unitdrop", contents.Value, metal));
			if (HasSolidifed(inSlot.Itemstack, contents.Key, world))
			{
				dsc.Append(Lang.Get("metalwork-toocold"));
			}
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public bool HasSolidifed(ItemStack ownStack, ItemStack contentstack, IWorldAccessor world)
	{
		if (ownStack?.Collectible == null || contentstack == null)
		{
			return false;
		}
		return (double)ownStack.Collectible.GetTemperature(world, ownStack) < 0.9 * (double)contentstack.Collectible.GetMeltingPoint(world, null, null);
	}

	public void SetContents(ItemStack stack, ItemStack output, int units)
	{
		stack.Attributes.SetItemstack("output", output);
		stack.Attributes.SetInt("units", units);
	}

	public KeyValuePair<ItemStack, int> GetContents(IWorldAccessor world, ItemStack stack)
	{
		ItemStack itemstack = stack.Attributes.GetItemstack("output");
		itemstack?.ResolveBlockOrItem(world);
		return new KeyValuePair<ItemStack, int>(itemstack, stack.Attributes.GetInt("units"));
	}

	public virtual bool ShouldSpawnGSParticles(IWorldAccessor world, ItemStack stack)
	{
		ItemStack key = GetContents(world, stack).Key;
		if (key != null && !HasSolidifed(stack, key, world))
		{
			return world.Rand.NextDouble() <= 0.05;
		}
		return false;
	}

	public virtual void DoSpawnGSParticles(IAsyncParticleManager manager, BlockPos pos, Vec3f offset)
	{
		smokeHeld.MinPos = pos.ToVec3d().AddCopy(gsSmokePos).AddCopy(offset);
		manager.Spawn(smokeHeld);
	}
}
