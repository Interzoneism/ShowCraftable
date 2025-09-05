using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleFish : EntityParticle
{
	protected ICoreClientAPI capi;

	protected float dieAccum;

	protected static Random rand = new Random();

	private Vec3d StartingPosition = new Vec3d();

	private bool flee;

	private float maxspeed;

	public FastVec3i StartPos;

	private double wiggle;

	public static int[][] Colors = new int[7][]
	{
		new int[4] { 189, 187, 59, 255 },
		new int[4] { 192, 135, 53, 255 },
		new int[4] { 184, 88, 26, 255 },
		new int[4] { 180, 65, 47, 255 },
		new int[4] { 56, 125, 163, 255 },
		new int[4] { 57, 98, 193, 169 },
		new int[4] { 126, 90, 145, 255 }
	};

	public EntityParticleFish[] FriendFishes;

	public override string Type => "fish";

	public EntityParticleFish(ICoreClientAPI capi, double x, double y, double z, Vec3f size, int colorindex, float maxspeed)
	{
		this.capi = capi;
		Position.Set(x, y, z);
		StartingPosition.Set(x, y, z);
		this.maxspeed = maxspeed;
		Alive = true;
		base.SizeX = size.X;
		base.SizeY = size.Y;
		base.SizeZ = size.Z;
		base.GravityStrength = 0f;
		ColorBlue = (byte)Colors[colorindex][0];
		ColorGreen = (byte)Colors[colorindex][1];
		ColorRed = (byte)Colors[colorindex][2];
		ColorAlpha = (byte)Colors[colorindex][3];
	}

	public override void TickNow(float dt, float physicsdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		base.TickNow(dt, physicsdt, api, physicsSim);
		Velocity.X = GameMath.Clamp(Velocity.X, 0f - maxspeed, maxspeed);
		Velocity.Y = GameMath.Clamp(Velocity.Y, 0f - maxspeed, maxspeed);
		Velocity.Z = GameMath.Clamp(Velocity.Z, 0f - maxspeed, maxspeed);
		wiggle = GameMath.Mod(wiggle + (double)(dt * 30f * Velocity.Length()), 6.2831854820251465);
	}

	protected override void doSlowTick(ParticlePhysics physicsSim, float dt)
	{
		base.doSlowTick(physicsSim, dt);
		Vec3d vec3d = StartingPosition.SubCopy(Position);
		float num = (float)vec3d.Length();
		if (!flee)
		{
			vec3d.Normalize();
			float num2 = GameMath.Clamp((num - 3f) * 0.1f, 0f, 0.4f);
			Velocity.Add((float)vec3d.X * num2, (float)vec3d.Y * num2, (float)vec3d.Z * num2);
		}
		DoSchool();
		if (rand.NextDouble() < 0.01)
		{
			float num3 = (float)rand.NextDouble() * 0.66f - 0.33f;
			float num4 = (float)rand.NextDouble() * 0.2f - 0.1f;
			float num5 = (float)rand.NextDouble() * 0.66f - 0.33f;
			propel(num3 / 3f, num4 / 3f, num5 / 3f);
			DoSchool();
			return;
		}
		EntityPlayer entity = capi.World.NearestPlayer(Position.X, Position.Y, Position.Z).Entity;
		double num6 = 2500.0;
		flee = false;
		if (entity != null && (num6 = entity.Pos.SquareDistanceTo(Position)) < 25.0 && entity.Player.WorldData.CurrentGameMode != EnumGameMode.Creative && entity.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			Vec3d vec3d2 = entity.Pos.XYZ.Sub(Position).Normalize();
			propel((float)(0.0 - vec3d2.X), 0f, (float)(0.0 - vec3d2.Z));
			flee = true;
			DoSchool();
		}
		if (!capi.World.BlockAccessor.GetBlockRaw((int)Position.X, (int)(Position.Y + 0.4000000059604645), (int)Position.Z, 2).IsLiquid())
		{
			Velocity.Y -= 0.1f;
		}
		if (entity == null || num6 > 1600.0)
		{
			dieAccum += dt;
			if (dieAccum > 15f)
			{
				Alive = false;
			}
		}
		else
		{
			dieAccum = 0f;
		}
	}

	private void DoSchool()
	{
		if (!flee)
		{
			Vec3d vec3d = new Vec3d();
			Vec3f vec3f = new Vec3f();
			EntityParticleFish[] friendFishes = FriendFishes;
			int num = friendFishes.Length;
			for (int i = 0; i < num; i++)
			{
				Vec3d position = friendFishes[i].Position;
				vec3d.Add(position.X / (double)num, position.Y / (double)num, position.Z / (double)num);
				vec3f.Add(friendFishes[i].Velocity);
				Vec3d vec3d2 = Position.SubCopy(vec3d);
				float num2 = (float)vec3d2.Length();
				float num3 = GameMath.Clamp((0.05f - num2) / 2f, 0f, 0.03f);
				Velocity.Add((float)Math.Sign(vec3d2.X) * num3, (float)Math.Sign(vec3d2.Y) * num3, (float)Math.Sign(vec3d2.Z) * num3);
			}
			Vec3d vec3d3 = Position.SubCopy(vec3d);
			float num4 = GameMath.Clamp(((float)vec3d3.Length() - 0.25f) / 1f, 0f, 0.03f);
			Velocity.Add((0f - (float)Math.Sign(vec3d3.X)) * num4, (0f - (float)Math.Sign(vec3d3.Y)) * num4, (0f - (float)Math.Sign(vec3d3.Z)) * num4);
			Velocity.Add(vec3f.X / (float)num / 20f, vec3f.Y / (float)num / 20f, vec3f.Z / (float)num / 20f);
		}
	}

	private void propel(float dirx, float diry, float dirz)
	{
		Velocity.Add(dirx, diry, dirz);
	}

	public override int UpdateAngles(float[] customFloats, int posPosition)
	{
		customFloats[posPosition++] = dirNormalizedX;
		customFloats[posPosition++] = dirNormalizedY;
		customFloats[posPosition++] = dirNormalizedZ;
		customFloats[posPosition++] = GameMath.Sin((float)wiggle) / 5f;
		return posPosition;
	}
}
