using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class PlantAirParticles : ParticlesProviderBase
{
	private Random rand = new Random();

	public Vec3d BasePos = new Vec3d();

	public Vec3d AddPos = new Vec3d();

	public override bool DieInAir => false;

	public override bool DieInLiquid => false;

	public override float GravityEffect => 1f;

	public override float LifeLength => 2.25f;

	public override bool SwimOnLiquid => true;

	public override Vec3d Pos => new Vec3d(BasePos.X + rand.NextDouble() * AddPos.X, BasePos.Y + rand.NextDouble() * AddPos.Y, BasePos.Z + AddPos.Z * rand.NextDouble());

	public override float Quantity => 1f;

	public override float Size => 0.07f;

	public override EvolvingNatFloat SizeEvolve => new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.2f);

	public override EvolvingNatFloat OpacityEvolve => new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -6f);

	public override int GetRgbaColor(ICoreClientAPI capi)
	{
		return ColorUtil.HsvToRgba(110, 40 + rand.Next(50), 200 + rand.Next(30), 50 + rand.Next(40));
	}

	public override Vec3f GetVelocity(Vec3d pos)
	{
		return new Vec3f(((float)rand.NextDouble() - 0.5f) / 4f, (float)(rand.NextDouble() + 1.0) / 10f, ((float)rand.NextDouble() - 0.5f) / 4f);
	}
}
