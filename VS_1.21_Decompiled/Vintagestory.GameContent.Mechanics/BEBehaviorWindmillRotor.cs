using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorWindmillRotor : BEBehaviorMPRotor
{
	private WeatherSystemBase weatherSystem;

	private double windSpeed;

	private int sailLength;

	private AssetLocation sound;

	public int SailLength => sailLength;

	protected override AssetLocation Sound => sound;

	protected override float Resistance => 0.003f;

	protected override double AccelerationFactor => 0.05;

	protected override float TargetSpeed => (float)Math.Min(0.6000000238418579, windSpeed);

	protected override float TorqueFactor => (float)sailLength / 4f;

	protected override float GetSoundVolume()
	{
		return (0.5f + 0.5f * (float)windSpeed) * (float)sailLength / 3f;
	}

	public BEBehaviorWindmillRotor(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		sound = new AssetLocation("sounds/effect/swoosh");
		weatherSystem = Api.ModLoader.GetModSystem<WeatherSystemBase>();
		Blockentity.RegisterGameTickListener(CheckWindSpeed, 1000);
	}

	private void CheckWindSpeed(float dt)
	{
		windSpeed = weatherSystem.WeatherDataSlowAccess.GetWindSpeed(Blockentity.Pos.ToVec3d());
		if (Api.World.BlockAccessor.GetLightLevel(Blockentity.Pos, EnumLightLevelType.OnlySunLight) < 5 && Api.World.Config.GetString("undergroundWindmills", "false") != "true")
		{
			windSpeed = 0.0;
		}
		if (Api.Side == EnumAppSide.Server && sailLength > 0 && Api.World.Rand.NextDouble() < 0.2 && obstructed(sailLength + 1))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), Position, 0.0, null, randomizePitch: false, 20f);
			while (sailLength-- > 0)
			{
				ItemStack itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("sail")), 4);
				Api.World.SpawnItemEntity(itemstack, Blockentity.Pos);
			}
			sailLength = 0;
			Blockentity.MarkDirty(redrawOnClient: true);
			network.updateNetwork(manager.getTickNumber());
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		while (sailLength-- > 0)
		{
			ItemStack itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("sail")), 4);
			Api.World.SpawnItemEntity(itemstack, Blockentity.Pos);
		}
		base.OnBlockBroken(byPlayer);
	}

	internal bool OnInteract(IPlayer byPlayer)
	{
		if (sailLength >= 5)
		{
			return false;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty || activeHotbarSlot.StackSize < 4)
		{
			return false;
		}
		ItemStack sourceStack = new ItemStack(Api.World.GetItem(new AssetLocation("sail")));
		if (!activeHotbarSlot.Itemstack.Equals(Api.World, sourceStack, GlobalConstants.IgnoredStackAttributes))
		{
			return false;
		}
		int len = sailLength + 2;
		if (obstructed(len))
		{
			if (Api.Side == EnumAppSide.Client)
			{
				(Api as ICoreClientAPI).TriggerIngameError(this, "notenoughspace", Lang.Get("Cannot add more sails. Make sure there's space for the sails to rotate freely"));
			}
			return false;
		}
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			activeHotbarSlot.TakeOut(4);
			activeHotbarSlot.MarkDirty();
		}
		sailLength++;
		updateShape(Api.World);
		Blockentity.MarkDirty(redrawOnClient: true);
		return true;
	}

	private bool obstructed(int len)
	{
		BlockPos blockPos = new BlockPos();
		for (int i = -len; i <= len; i++)
		{
			for (int j = -len; j <= len; j++)
			{
				if ((i != 0 || j != 0) && (len <= 1 || Math.Abs(i) != len || Math.Abs(j) != len))
				{
					int num = ((ownFacing.Axis == EnumAxis.Z) ? i : 0);
					int num2 = ((ownFacing.Axis == EnumAxis.X) ? i : 0);
					blockPos.Set(Position.X + num, Position.Y + j, Position.Z + num2);
					Block block = Api.World.BlockAccessor.GetBlock(blockPos);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(Api.World.BlockAccessor, blockPos);
					if (collisionBoxes != null && collisionBoxes.Length != 0 && !(block is BlockSnowLayer) && !(block is BlockSnow))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		sailLength = tree.GetInt("sailLength");
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		tree.SetInt("sailLength", sailLength);
		base.ToTreeAttributes(tree);
	}

	protected override void updateShape(IWorldAccessor worldForResolve)
	{
		if (worldForResolve.Side == EnumAppSide.Client && base.Block != null)
		{
			if (sailLength == 0)
			{
				Shape = new CompositeShape
				{
					Base = new AssetLocation("block/wood/mechanics/windmillrotor"),
					rotateY = base.Block.Shape.rotateY
				};
			}
			else
			{
				Shape = new CompositeShape
				{
					Base = new AssetLocation("block/wood/mechanics/windmill-" + sailLength + "blade"),
					rotateY = base.Block.Shape.rotateY
				};
			}
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		sb.AppendLine(string.Format(Lang.Get("Wind speed: {0}%", (int)(100.0 * windSpeed))));
		sb.AppendLine(Lang.Get("Sails power output: {0} kN", (int)((float)sailLength / 5f * 100f)));
	}
}
