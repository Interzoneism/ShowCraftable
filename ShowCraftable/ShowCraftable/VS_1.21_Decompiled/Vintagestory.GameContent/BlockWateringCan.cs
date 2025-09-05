using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWateringCan : Block
{
	public float CapacitySeconds = 32f;

	public static SimpleParticleProperties WaterParticles;

	private ILoadedSound pouringLoop;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		WaterParticles = new SimpleParticleProperties(1f, 1f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), 1f, 1f, 0.33f, 0.75f);
		WaterParticles.AddPos = new Vec3d(0.0625, 0.125, 0.0625);
		WaterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.7f);
		WaterParticles.ClimateColorMap = "climateWaterTint";
		WaterParticles.AddQuantity = 1f;
		CapacitySeconds = Attributes?["capacitySeconds"].AsFloat(32f) ?? 32f;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		slot.Itemstack.TempAttributes.SetFloat("secondsUsed", 0f);
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position, 2);
		if (block.LiquidCode == "water")
		{
			BlockPos position = blockSel.Position;
			SetRemainingWateringSeconds(slot.Itemstack, CapacitySeconds);
			slot.Itemstack.TempAttributes.SetInt("refilled", 1);
			slot.MarkDirty();
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/water"), position, 0.35, dualCallByPlayer);
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		BlockBucket blockBucket = byEntity.World.BlockAccessor.GetBlock(blockSel.Position) as BlockBucket;
		Block block2 = blockBucket?.GetContent(blockSel.Position)?.Block;
		if (blockBucket != null && block2?.LiquidCode == "water")
		{
			WaterTightContainableProps waterTightContainableProps = block2.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>(null, block.Code.Domain);
			int quantityItem = (int)(5f / waterTightContainableProps.ItemsPerLitre);
			blockBucket.GetCurrentLitres(blockSel.Position);
			BlockPos position2 = blockSel.Position;
			ItemStack itemStack = blockBucket.TryTakeContent(blockSel.Position, quantityItem);
			SetRemainingWateringSeconds(slot.Itemstack, CapacitySeconds * (float)itemStack.StackSize * waterTightContainableProps.ItemsPerLitre);
			slot.Itemstack.TempAttributes.SetInt("refilled", 1);
			slot.MarkDirty();
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/water"), position2, 0.35, dualCallByPlayer);
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		slot.Itemstack.TempAttributes.SetInt("refilled", 0);
		if (GetRemainingWateringSeconds(slot.Itemstack) <= 0f)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			byEntity.World.RegisterCallback(After350ms, 350);
		}
		handHandling = EnumHandHandling.PreventDefault;
	}

	private void After350ms(float dt)
	{
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		IClientPlayer player = coreClientAPI.World.Player;
		EntityPlayer entity = player.Entity;
		if (entity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
		{
			coreClientAPI.World.PlaySoundAt(new AssetLocation("sounds/effect/watering"), entity, player);
		}
		if (entity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
		{
			if (pouringLoop != null)
			{
				pouringLoop.FadeIn(0.3f, null);
				return;
			}
			pouringLoop = coreClientAPI.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/effect/watering-loop.ogg"),
				Position = new Vec3f(),
				RelativePosition = true,
				ShouldLoop = true,
				Range = 16f,
				Volume = 0.2f,
				Pitch = 0.5f
			});
			pouringLoop.Start();
			pouringLoop.FadeIn(0.15f, null);
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		if (entityItem.FeetInLiquid)
		{
			SetRemainingWateringSeconds(entityItem.Itemstack, CapacitySeconds);
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityWateringCan blockEntityWateringCan)
		{
			ItemStack itemStack = new ItemStack(this);
			SetRemainingWateringSeconds(itemStack, blockEntityWateringCan.SecondsWateringLeft);
			return itemStack;
		}
		return base.OnPickBlock(world, pos);
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		if (slot.Itemstack.TempAttributes.GetInt("refilled") > 0)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		float num = slot.Itemstack.TempAttributes.GetFloat("secondsUsed");
		slot.Itemstack.TempAttributes.SetFloat("secondsUsed", secondsUsed);
		float remainingWateringSeconds = GetRemainingWateringSeconds(slot.Itemstack);
		SetRemainingWateringSeconds(slot.Itemstack, remainingWateringSeconds -= secondsUsed - num);
		if (remainingWateringSeconds <= 0f)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		IWorldAccessor world = byEntity.World;
		BlockPos blockPos = blockSel.Position;
		if (api.World.Side == EnumAppSide.Server)
		{
			(world.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face))?.GetBehavior<BEBehaviorBurning>())?.KillFire(consumeFuel: false);
			(world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorBurning>())?.KillFire(consumeFuel: false);
			GetBEBehavior<BEBehaviorTemperatureSensitive>(blockSel.Position)?.OnWatered(secondsUsed - num);
			for (int i = -2; i < 2; i++)
			{
				for (int j = -2; j < 2; j++)
				{
					for (int k = -2; k < 2; k++)
					{
						int num2 = (int)(blockSel.HitPosition.X * 16.0) + i;
						int num3 = (int)(blockSel.HitPosition.Y * 16.0) + j;
						int num4 = (int)(blockSel.HitPosition.Z * 16.0) + k;
						if (num2 >= 0 && num2 <= 15 && num3 >= 0 && num3 <= 15 && num4 >= 0 && num4 <= 15)
						{
							DecorBits decorBits = new DecorBits(blockSel.Face, num2, 15 - num3, num4);
							if (world.BlockAccessor.GetDecor(blockSel.Position, decorBits)?.FirstCodePart() == "caveart")
							{
								world.BlockAccessor.BreakDecor(blockSel.Position, blockSel.Face, decorBits);
							}
						}
					}
				}
			}
		}
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		bool flag = false;
		if (block.CollisionBoxes == null || block.CollisionBoxes.Length == 0)
		{
			block = world.BlockAccessor.GetBlock(blockSel.Position, 2);
			if ((block.CollisionBoxes == null || block.CollisionBoxes.Length == 0) && !block.IsLiquid())
			{
				flag = true;
				blockPos = blockPos.DownCopy();
			}
		}
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityFarmland blockEntityFarmland)
		{
			blockEntityFarmland.WaterFarmland(secondsUsed - num);
		}
		float num5 = 3f;
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (secondsUsed > 1f / num5)
		{
			Vec3d vec3d = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
			if (flag)
			{
				vec3d.Y = (double)(int)vec3d.Y + 0.05;
			}
			WaterParticles.MinPos = vec3d.Add(-0.0625, 0.0625, -0.0625);
			byEntity.World.SpawnParticles(WaterParticles, dualCallByPlayer);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		pouringLoop?.Stop();
		pouringLoop?.Dispose();
		pouringLoop = null;
		slot.MarkDirty();
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWateringCan blockEntityWateringCan)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			float num3 = (float)Math.PI / 8f;
			float meshAngle = (float)(int)Math.Round(num2 / num3) * num3;
			blockEntityWateringCan.MeshAngle = meshAngle;
		}
		return num;
	}

	public float GetRemainingWateringSeconds(ItemStack stack)
	{
		return stack.Attributes.GetFloat("wateringSeconds");
	}

	public void SetRemainingWateringSeconds(ItemStack stack, float seconds)
	{
		stack.Attributes.SetFloat("wateringSeconds", seconds);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		double num = Math.Round(100f * GetRemainingWateringSeconds(inSlot.Itemstack) / CapacitySeconds);
		string arg = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)GameMath.Clamp(num, 0.0, 99.0)]);
		if (num < 1.0)
		{
			dsc.AppendLine(string.Format("<font color=\"{0}\">" + Lang.Get("Empty") + "</font>", arg));
			return;
		}
		dsc.AppendLine(string.Format("<font color=\"{0}\">" + Lang.Get("{0}% full", num) + "</font>", arg));
	}
}
