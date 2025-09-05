using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPRotor : BEBehaviorMPBase
{
	protected double capableSpeed;

	protected double lastMsAngle;

	protected BlockFacing ownFacing;

	private EntityPartitioning partitionUtil;

	private ICoreClientAPI capi;

	protected virtual AssetLocation Sound { get; }

	protected virtual float Resistance { get; }

	protected virtual double AccelerationFactor { get; }

	protected virtual float TargetSpeed { get; }

	protected virtual float TorqueFactor { get; }

	public override float AngleRad
	{
		get
		{
			if (Sound != null)
			{
				MechanicalNetwork mechanicalNetwork = network;
				if (mechanicalNetwork != null && mechanicalNetwork.Speed > 0f && (double)Api.World.ElapsedMilliseconds - lastMsAngle > (double)(500f / network.Speed) && Api.Side == EnumAppSide.Client)
				{
					Api.World.PlaySoundAt(Sound, Position, 0.0, null, randomizePitch: false, 18f, GetSoundVolume());
					lastMsAngle = Api.World.ElapsedMilliseconds;
				}
			}
			return base.AngleRad;
		}
	}

	protected virtual float GetSoundVolume()
	{
		return 0f;
	}

	public BEBehaviorMPRotor(BlockEntity blockentity)
		: base(blockentity)
	{
		Blockentity = blockentity;
		string code = blockentity.Block.Variant["side"];
		ownFacing = BlockFacing.FromCode(code);
		OutFacingForNetworkDiscovery = ownFacing.Opposite;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		capi = api as ICoreClientAPI;
		switch (ownFacing.Code)
		{
		case "north":
		case "south":
			AxisSign = new int[3] { 0, 0, -1 };
			break;
		case "east":
		case "west":
			AxisSign = new int[3] { -1, 0, 0 };
			break;
		}
		if (api.Side == EnumAppSide.Server)
		{
			partitionUtil = Api.ModLoader.GetModSystem<EntityPartitioning>();
		}
		if (Api.Side == EnumAppSide.Client)
		{
			updateShape(api.World);
		}
	}

	public override float GetResistance()
	{
		return Resistance;
	}

	public override float GetTorque(long tick, float speed, out float resistance)
	{
		float targetSpeed = TargetSpeed;
		capableSpeed += ((double)targetSpeed - capableSpeed) * AccelerationFactor;
		float num = (float)capableSpeed;
		float num2 = ((propagationDir == OutFacingForNetworkDiscovery) ? 1f : (-1f));
		float num3 = Math.Abs(speed);
		float num4 = num3 - num;
		bool flag = num2 * speed < 0f;
		resistance = (flag ? (Resistance * TorqueFactor * Math.Min(0.8f, num3 * 400f)) : ((num4 > 0f) ? (Resistance * Math.Min(0.2f, num4 * num4 * 80f)) : 0f));
		float val = (flag ? num : (num - num3));
		return Math.Max(0f, val) * TorqueFactor * num2;
	}

	public override void WasPlaced(BlockFacing connectedOnFacing)
	{
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir)
	{
		return Array.Empty<MechPowerPath>();
	}
}
