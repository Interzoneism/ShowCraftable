using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LightningFlash : IDisposable
{
	private MeshRef quadRef;

	private Vec4f color;

	private float linewidth;

	public List<Vec3d> points;

	public LightiningPointLight[] pointLights = new LightiningPointLight[2];

	public Vec3d origin;

	public float secondsAlive;

	public bool Alive = true;

	private ICoreAPI api;

	private ICoreClientAPI capi;

	public float flashAccum;

	public float rndVal;

	public float advanceWaitSec;

	private bool soundPlayed;

	private WeatherSystemBase weatherSys;

	private Random rand;

	public LightningFlash(WeatherSystemBase weatherSys, ICoreAPI api, int? seed, Vec3d startpoint)
	{
		this.weatherSys = weatherSys;
		capi = api as ICoreClientAPI;
		this.api = api;
		rand = new Random((!seed.HasValue) ? capi.World.Rand.Next() : seed.Value);
		color = new Vec4f(1f, 1f, 1f, 1f);
		linewidth = 0.33f;
		origin = startpoint.Clone();
		origin.Y = api.World.BlockAccessor.GetRainMapHeightAt((int)origin.X, (int)origin.Z) + 1;
	}

	public void ClientInit()
	{
		genPoints(weatherSys);
		genMesh(points);
		float num = 200f;
		pointLights[0] = new LightiningPointLight(new Vec3f(num, num, num), points[0].AddCopy(origin));
		pointLights[1] = new LightiningPointLight(new Vec3f(0f, 0f, 0f), points[points.Count - 1].AddCopy(origin));
		capi.Render.AddPointLight(pointLights[0]);
		capi.Render.AddPointLight(pointLights[1]);
		Vec3d vec3d = points[points.Count - 1];
		Vec3d pos = origin + vec3d;
		float num2 = (float)capi.World.Player.Entity.Pos.DistanceTo(pos);
		if (num2 < 150f)
		{
			AssetLocation location = new AssetLocation("sounds/weather/lightning-verynear.ogg");
			capi.World.PlaySoundAt(location, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - num2 / 180f);
		}
		else if (num2 < 200f)
		{
			AssetLocation location2 = new AssetLocation("sounds/weather/lightning-near.ogg");
			capi.World.PlaySoundAt(location2, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - num2 / 250f);
		}
		else if (num2 < 320f)
		{
			AssetLocation location3 = new AssetLocation("sounds/weather/lightning-distant.ogg");
			capi.World.PlaySoundAt(location3, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - num2 / 500f);
		}
	}

	protected void genPoints(WeatherSystemBase weatherSys)
	{
		Vec3d vec3d = new Vec3d();
		points = new List<Vec3d>();
		vec3d.Y = 0.0;
		float num = (float)((double)(weatherSys.CloudLevelRel * (float)capi.World.BlockAccessor.MapSizeY + 2f) - origin.Y);
		while (vec3d.Y < (double)num)
		{
			points.Add(vec3d.Clone());
			vec3d.Y += rand.NextDouble();
			vec3d.X += rand.NextDouble() * 2.0 - 1.0;
			vec3d.Z += rand.NextDouble() * 2.0 - 1.0;
		}
		if (points.Count == 0)
		{
			points.Add(vec3d.Clone());
		}
		points.Reverse();
	}

	protected void genMesh(List<Vec3d> points)
	{
		float[] array = new float[points.Count * 3];
		for (int i = 0; i < points.Count; i++)
		{
			Vec3d vec3d = points[i];
			array[i * 3] = (float)vec3d.X;
			array[i * 3 + 1] = (float)vec3d.Y;
			array[i * 3 + 2] = (float)vec3d.Z;
		}
		quadRef?.Dispose();
		MeshData cube = CubeMeshUtil.GetCube(0.5f, 0.5f, 0.5f, new Vec3f(0f, 0f, 0f));
		cube.Flags = null;
		cube.Rgba = null;
		cube.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			InterleaveOffsets = new int[2] { 0, 12 },
			InterleaveSizes = new int[2] { 3, 3 },
			InterleaveStride = 12,
			StaticDraw = false,
			Values = array,
			Count = array.Length
		};
		MeshData meshData = new MeshData(initialiseArrays: false);
		meshData.CustomFloats = cube.CustomFloats;
		quadRef = capi.Render.UploadMesh(cube);
		capi.Render.UpdateMesh(quadRef, meshData);
	}

	public void GameTick(float dt)
	{
		dt *= 3f;
		if (rand.NextDouble() < 0.4 && (double)(secondsAlive * 10f) < 0.6 && advanceWaitSec <= 0f)
		{
			advanceWaitSec = 0.05f + (float)rand.NextDouble() / 10f;
		}
		secondsAlive += Math.Max(0f, dt - advanceWaitSec);
		advanceWaitSec = Math.Max(0f, advanceWaitSec - dt);
		if ((double)secondsAlive > 0.7)
		{
			Alive = false;
		}
		if (api.Side != EnumAppSide.Server || !(secondsAlive > 0f))
		{
			return;
		}
		weatherSys.TriggerOnLightningImpactEnd(origin, out var handling);
		if (handling != EnumHandling.PassThrough || !api.World.Config.GetBool("lightningDamage", defaultValue: true))
		{
			return;
		}
		DamageSource dmgSrc = new DamageSource
		{
			KnockbackStrength = 2f,
			Source = EnumDamageSource.Weather,
			Type = EnumDamageType.Electricity,
			SourcePos = origin,
			HitPosition = new Vec3d()
		};
		api.ModLoader.GetModSystem<EntityPartitioning>().WalkEntities(origin, 8.0, delegate(Entity entity)
		{
			if (!entity.IsInteractable)
			{
				return true;
			}
			float damage = 6f;
			entity.ReceiveDamage(dmgSrc, damage);
			return true;
		}, EnumEntitySearchType.Creatures);
	}

	public void Render(float dt)
	{
		GameTick(dt);
		capi.Render.CurrentActiveShader.Uniform("color", color);
		capi.Render.CurrentActiveShader.Uniform("lineWidth", linewidth);
		IClientPlayer player = capi.World.Player;
		Vec3d cameraPos = player.Entity.CameraPos;
		capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f((float)(origin.X - cameraPos.X), (float)(origin.Y - cameraPos.Y), (float)(origin.Z - cameraPos.Z)));
		double num = GameMath.Clamp(secondsAlive * 10f, 0f, 1f);
		int num2 = (int)(num * (double)points.Count) - 1;
		if (num2 > 0)
		{
			capi.Render.RenderMeshInstanced(quadRef, num2);
		}
		if (num >= 0.9 && !soundPlayed)
		{
			soundPlayed = true;
			Vec3d vec3d = points[points.Count - 1];
			Vec3d vec3d2 = origin + vec3d;
			float num3 = (float)player.Entity.Pos.DistanceTo(vec3d2);
			if (num3 < 150f)
			{
				AssetLocation location = new AssetLocation("sounds/weather/lightning-nodistance.ogg");
				capi.World.PlaySoundAt(location, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, Math.Max(0.1f, 1f - num3 / 70f));
			}
			if (num3 < 100f)
			{
				(weatherSys as WeatherSystemClient).simLightning.lightningTime = 0.3f + (float)rand.NextDouble() * 0.17f;
				(weatherSys as WeatherSystemClient).simLightning.lightningIntensity = 1.5f + (float)rand.NextDouble() * 0.4f;
				int num4 = Math.Max(0, (int)num3 - 5) * 3;
				int num5 = ColorUtil.ToRgba(255, 255, 255, 255);
				SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(250 - num4, 300 - num4, num5, vec3d2.AddCopy(-0.5f, 0f, -0.5f), vec3d2.AddCopy(0.5f, 1f, 0.5f), new Vec3f(-5f, 0f, -5f), new Vec3f(5f, 10f, 5f), 3f, 0.3f, 0.4f, 2f);
				simpleParticleProperties.VertexFlags = 255;
				simpleParticleProperties.LightEmission = int.MaxValue;
				simpleParticleProperties.ShouldDieInLiquid = true;
				simpleParticleProperties.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEARREDUCE, 1f);
				capi.World.SpawnParticles(simpleParticleProperties);
				simpleParticleProperties.ParticleModel = EnumParticleModel.Quad;
				simpleParticleProperties.MinSize /= 2f;
				simpleParticleProperties.MaxSize /= 2f;
				capi.World.SpawnParticles(simpleParticleProperties);
			}
		}
		flashAccum += dt;
		if (flashAccum > rndVal)
		{
			rndVal = (float)rand.NextDouble() / 10f;
			flashAccum = 0f;
			float num6 = (float)rand.NextDouble();
			float num7 = 50f + num6 * 150f;
			pointLights[0].Color.Set(num7, num7, num7);
			linewidth = (0.4f + 0.6f * num6) / 3f;
			if (num < 1.0)
			{
				num7 = 0f;
			}
			pointLights[1].Color.Set(num7, num7, num7);
		}
	}

	public void Dispose()
	{
		quadRef?.Dispose();
		capi?.Render.RemovePointLight(pointLights[0]);
		capi?.Render.RemovePointLight(pointLights[1]);
	}
}
