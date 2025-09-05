using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract]
public class MechanicalNetwork
{
	public Dictionary<BlockPos, IMechanicalPowerNode> nodes = new Dictionary<BlockPos, IMechanicalPowerNode>();

	internal MechanicalPowerMod mechanicalPowerMod;

	[ProtoMember(1)]
	public long networkId;

	[ProtoMember(2)]
	protected float totalAvailableTorque;

	[ProtoMember(3)]
	protected float networkResistance;

	[ProtoMember(4)]
	protected float speed;

	[ProtoMember(7)]
	protected float serverSideAngle;

	[ProtoMember(8)]
	protected float angle;

	[ProtoMember(9)]
	public Dictionary<Vec3i, int> inChunks = new Dictionary<Vec3i, int>();

	[ProtoMember(10)]
	private float networkTorque;

	public float clientSpeed;

	private const int chunksize = 32;

	public bool fullyLoaded;

	private bool firstTick = true;

	[ProtoMember(11)]
	public EnumRotDirection TurnDir { get; set; }

	public bool Valid { get; set; } = true;

	public float AngleRad
	{
		get
		{
			return angle;
		}
		set
		{
			angle = value;
		}
	}

	public float Speed
	{
		get
		{
			return speed;
		}
		set
		{
			speed = value;
		}
	}

	public bool DirectionHasReversed { get; set; }

	public float TotalAvailableTorque
	{
		get
		{
			return totalAvailableTorque;
		}
		set
		{
			totalAvailableTorque = value;
		}
	}

	public float NetworkTorque
	{
		get
		{
			return networkTorque;
		}
		set
		{
			networkTorque = value;
		}
	}

	public float NetworkResistance
	{
		get
		{
			return networkResistance;
		}
		set
		{
			networkResistance = value;
		}
	}

	public MechanicalNetwork()
	{
	}

	public MechanicalNetwork(MechanicalPowerMod mechanicalPowerMod, long networkId)
	{
		this.networkId = networkId;
		Init(mechanicalPowerMod);
	}

	public void Init(MechanicalPowerMod mechanicalPowerMod)
	{
		this.mechanicalPowerMod = mechanicalPowerMod;
	}

	public void Join(IMechanicalPowerNode node)
	{
		BlockPos position = node.GetPosition();
		nodes[position] = node;
		Vec3i key = new Vec3i(position.X / 32, position.Y / 32, position.Z / 32);
		inChunks.TryGetValue(key, out var value);
		inChunks[key] = value + 1;
	}

	public void DidUnload(IMechanicalPowerDevice node)
	{
		fullyLoaded = false;
	}

	public void Leave(IMechanicalPowerNode node)
	{
		BlockPos position = node.GetPosition();
		nodes.Remove(position);
		Vec3i key = new Vec3i(position.X / 32, position.Y / 32, position.Z / 32);
		inChunks.TryGetValue(key, out var value);
		if (value <= 1)
		{
			inChunks.Remove(key);
		}
		else
		{
			inChunks[key] = value - 1;
		}
	}

	internal void AwaitChunkThenDiscover(Vec3i missingChunkPos)
	{
		inChunks[missingChunkPos] = 1;
		fullyLoaded = false;
	}

	public void ClientTick(float dt)
	{
		if (firstTick)
		{
			firstTick = false;
			mechanicalPowerMod.SendNetworkBlocksUpdateRequestToServer(networkId);
		}
		if (!(speed < 0.001f))
		{
			float num = dt * 50f;
			clientSpeed += GameMath.Clamp(speed - clientSpeed, num * -0.01f, num * 0.01f);
			UpdateAngle(num * (((TurnDir == EnumRotDirection.Clockwise) ^ DirectionHasReversed) ? clientSpeed : (0f - clientSpeed)));
			float num2 = num * GameMath.AngleRadDistance(angle, serverSideAngle);
			angle += GameMath.Clamp(num2, -0.002f * Math.Abs(num2), 0.002f * Math.Abs(num2));
		}
	}

	public void ServerTick(float dt, long tickNumber)
	{
		UpdateAngle(speed * dt * 50f);
		if (tickNumber % 5 == 0L)
		{
			updateNetwork(tickNumber);
		}
		if (tickNumber % 40 == 0L)
		{
			broadcastData();
		}
	}

	public void broadcastData()
	{
		mechanicalPowerMod.broadcastNetwork(new MechNetworkPacket
		{
			angle = angle,
			networkId = networkId,
			speed = speed,
			direction = ((speed >= 0f) ? 1 : (-1)),
			totalAvailableTorque = totalAvailableTorque,
			networkResistance = networkResistance,
			networkTorque = networkTorque
		});
	}

	public void UpdateAngle(float speed)
	{
		angle += speed / 10f;
		serverSideAngle += speed / 10f;
	}

	public void updateNetwork(long tick)
	{
		if (DirectionHasReversed)
		{
			speed = 0f - speed;
			DirectionHasReversed = false;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = speed;
		foreach (IMechanicalPowerNode value in nodes.Values)
		{
			float gearedRatio = value.GearedRatio;
			num += gearedRatio * value.GetTorque(tick, num3 * gearedRatio, out var resistance);
			num2 += gearedRatio * resistance;
			num2 += speed * speed * gearedRatio * gearedRatio / 1000f;
		}
		networkTorque = num;
		networkResistance = num2;
		float num4 = Math.Abs(num) - networkResistance;
		float num5 = ((num >= 0f) ? 1f : (-1f));
		float num6 = Math.Max(1f, (float)Math.Pow(nodes.Count, 0.25));
		float num7 = 1f / num6;
		bool flag = speed * num5 < 0f;
		if (num4 > 0f && !flag)
		{
			speed += Math.Min(0.05f, num7 * num4) * num5;
		}
		else
		{
			float num8 = num4;
			if (flag)
			{
				num8 = 0f - networkResistance;
			}
			if (num8 < 0f - Math.Abs(speed))
			{
				num8 = 0f - Math.Abs(speed);
			}
			if (num8 < -1E-06f || Math.Abs(speed) > 1E-06f)
			{
				float num9 = ((speed < 0f) ? (-1f) : 1f);
				speed = Math.Max(1E-06f, Math.Abs(speed) + num7 * num8) * num9;
			}
			else if (Math.Abs(num4) > 0f)
			{
				speed = num5 / 1000000f;
			}
		}
		if (num4 > Math.Abs(totalAvailableTorque))
		{
			if (num > 0f)
			{
				totalAvailableTorque = Math.Min(num, totalAvailableTorque + num7);
			}
			else
			{
				totalAvailableTorque = Math.Max(Math.Min(num, -1E-08f), totalAvailableTorque - num7);
			}
		}
		else
		{
			totalAvailableTorque *= 0.9f;
		}
		TurnDir = ((!(speed >= 0f)) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
	}

	public void UpdateFromPacket(MechNetworkPacket packet, bool isNew)
	{
		totalAvailableTorque = packet.totalAvailableTorque;
		networkResistance = packet.networkResistance;
		networkTorque = packet.networkTorque;
		speed = Math.Abs(packet.speed);
		if (isNew)
		{
			angle = packet.angle;
			clientSpeed = speed;
		}
		serverSideAngle = packet.angle;
		TurnDir = ((packet.direction < 0) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
		DirectionHasReversed = false;
	}

	public bool testFullyLoaded(ICoreAPI api)
	{
		foreach (Vec3i key in inChunks.Keys)
		{
			if (api.World.BlockAccessor.GetChunk(key.X, key.Y, key.Z) == null)
			{
				return false;
			}
		}
		return true;
	}

	public void ReadFromTreeAttribute(ITreeAttribute tree)
	{
		networkId = tree.GetLong("networkId", 0L);
		totalAvailableTorque = tree.GetFloat("totalAvailableTorque");
		networkResistance = tree.GetFloat("totalResistance");
		speed = tree.GetFloat("speed");
		angle = tree.GetFloat("angle");
		TurnDir = (EnumRotDirection)tree.GetInt("rot");
	}

	public void WriteToTreeAttribute(ITreeAttribute tree)
	{
		tree.SetLong("networkId", networkId);
		tree.SetFloat("totalAvailableTorque", totalAvailableTorque);
		tree.SetFloat("totalResistance", networkResistance);
		tree.SetFloat("speed", speed);
		tree.SetFloat("angle", angle);
		tree.SetInt("rot", (int)TurnDir);
	}

	public void SendBlocksUpdateToClient(IServerPlayer player)
	{
		foreach (IMechanicalPowerNode value in nodes.Values)
		{
			if (value is BEBehaviorMPBase bEBehaviorMPBase)
			{
				bEBehaviorMPBase.Blockentity.MarkDirty();
			}
		}
	}
}
