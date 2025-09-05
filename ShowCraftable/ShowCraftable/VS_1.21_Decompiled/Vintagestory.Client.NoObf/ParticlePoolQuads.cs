using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ParticlePoolQuads : IParticlePool
{
	public FastParticlePool ParticlesPool;

	protected MeshRef particleModelRef;

	protected MeshData updateBuffer;

	protected MeshData[] updateBuffers;

	protected Vec3d[] cameraPos;

	protected float[] tickTimes;

	protected float[][] velocities;

	private int writePosition = 1;

	private int readPosition;

	private object advanceCountLock = new object();

	private int advanceCount;

	protected int poolSize;

	protected ClientMain game;

	protected Random rand = new Random();

	private float currentGamespeed;

	private ParticlePhysics partPhysics;

	private bool offthread;

	protected EnumParticleModel ModelType;

	private float accumPhysics;

	public MeshRef Model => particleModelRef;

	public int QuantityAlive { get; set; }

	internal virtual float ParticleHeight => 0.5f;

	public IBlockAccessor BlockAccess => partPhysics.BlockAccess;

	public virtual MeshData LoadModel()
	{
		return QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 0.25f, 0.25f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
	}

	public ParticlePoolQuads(int poolSize, ClientMain game, bool offthread)
	{
		this.offthread = offthread;
		this.poolSize = poolSize;
		this.game = game;
		partPhysics = new ParticlePhysics(new BlockAccessorReadLockfree(game.WorldMap, game));
		if (offthread)
		{
			partPhysics.PhysicsTickTime = 0.125f;
		}
		ParticlesPool = new FastParticlePool(poolSize, () => new ParticleGeneric());
		MeshData meshData = LoadModel();
		meshData.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			StaticDraw = false,
			Values = new float[poolSize * 4],
			InterleaveSizes = new int[2] { 3, 1 },
			InterleaveStride = 16,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = poolSize * 4
		};
		meshData.CustomBytes = new CustomMeshDataPartByte
		{
			Conversion = DataConversion.NormalizedFloat,
			Instanced = true,
			StaticDraw = false,
			Values = new byte[poolSize * 12],
			InterleaveSizes = new int[3] { 4, 4, 4 },
			InterleaveStride = 12,
			InterleaveOffsets = new int[3] { 0, 4, 8 },
			Count = poolSize * 12
		};
		meshData.Flags = new int[poolSize];
		meshData.FlagsInstanced = true;
		particleModelRef = game.Platform.UploadMesh(meshData);
		if (offthread)
		{
			updateBuffers = new MeshData[5];
			cameraPos = new Vec3d[5];
			tickTimes = new float[5];
			velocities = new float[5][];
			for (int num = 0; num < 5; num++)
			{
				tickTimes[num] = partPhysics.PhysicsTickTime;
				velocities[num] = new float[3 * poolSize];
				cameraPos[num] = new Vec3d();
				updateBuffers[num] = genUpdateBuffer();
			}
		}
		else
		{
			updateBuffer = genUpdateBuffer();
		}
	}

	private MeshData genUpdateBuffer()
	{
		return new MeshData
		{
			CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[poolSize * 4],
				Count = poolSize * 4
			},
			CustomBytes = new CustomMeshDataPartByte
			{
				Values = new byte[poolSize * 12],
				Count = poolSize * 12
			},
			Flags = new int[poolSize],
			FlagsInstanced = true
		};
	}

	public int SpawnParticles(IParticlePropertiesProvider particleProperties)
	{
		float num = 5f / GameMath.Sqrt(currentGamespeed);
		int num2 = 0;
		if (QuantityAlive * 100 >= game.particleLevel * poolSize)
		{
			return 0;
		}
		float num3 = particleProperties.Quantity * currentGamespeed;
		while ((float)num2 < num3 && ParticlesPool.FirstDead != null && !(rand.NextDouble() > (double)(num3 - (float)num2)))
		{
			int rgbaColor = particleProperties.GetRgbaColor(game.api);
			if (rgbaColor == 0)
			{
				num3 -= 0.5f;
				continue;
			}
			ParticleGeneric particleGeneric = (ParticleGeneric)ParticlesPool.ReviveOne();
			particleGeneric.SecondaryParticles = particleProperties.SecondaryParticles;
			particleGeneric.DeathParticles = particleProperties.DeathParticles;
			particleProperties.BeginParticle();
			particleGeneric.Position.Set(particleProperties.Pos);
			particleGeneric.Velocity.Set(particleProperties.GetVelocity(particleGeneric.Position));
			particleGeneric.ParentVelocity = particleProperties.ParentVelocity;
			particleGeneric.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
			particleGeneric.Bounciness = particleProperties.Bounciness;
			particleGeneric.StartingVelocity.Set(particleGeneric.Velocity);
			particleGeneric.SizeMultiplier = particleProperties.Size;
			particleGeneric.ParticleHeight = ParticleHeight;
			particleGeneric.ColorRed = (byte)rgbaColor;
			particleGeneric.ColorGreen = (byte)(rgbaColor >> 8);
			particleGeneric.ColorBlue = (byte)(rgbaColor >> 16);
			particleGeneric.ColorAlpha = (byte)(rgbaColor >> 24);
			particleGeneric.LightEmission = particleProperties.LightEmission;
			particleGeneric.VertexFlags = particleProperties.VertexFlags;
			particleGeneric.SelfPropelled = particleProperties.SelfPropelled;
			particleGeneric.LifeLength = particleProperties.LifeLength * num;
			particleGeneric.TerrainCollision = particleProperties.TerrainCollision;
			particleGeneric.GravityStrength = particleProperties.GravityEffect * GlobalConstants.GravityStrengthParticle * 40f;
			particleGeneric.SwimOnLiquid = particleProperties.SwimOnLiquid;
			particleGeneric.DieInLiquid = particleProperties.DieInLiquid;
			particleGeneric.DieInAir = particleProperties.DieInAir;
			particleGeneric.DieOnRainHeightmap = particleProperties.DieOnRainHeightmap;
			particleGeneric.OpacityEvolve = particleProperties.OpacityEvolve;
			particleGeneric.RedEvolve = particleProperties.RedEvolve;
			particleGeneric.GreenEvolve = particleProperties.GreenEvolve;
			particleGeneric.BlueEvolve = particleProperties.BlueEvolve;
			particleGeneric.SizeEvolve = particleProperties.SizeEvolve;
			particleGeneric.VelocityEvolve = particleProperties.VelocityEvolve;
			particleGeneric.RandomVelocityChange = particleProperties.RandomVelocityChange;
			particleGeneric.Spawned(partPhysics);
			num2++;
		}
		return num2;
	}

	public bool ShouldRender()
	{
		return ParticlesPool.AliveCount > 0;
	}

	public void OnNewFrame(float dt, Vec3d cameraPos)
	{
		if (game.IsPaused)
		{
			return;
		}
		if (offthread)
		{
			ProcessParticlesFromOffThread(dt, cameraPos);
			return;
		}
		currentGamespeed = game.Calendar.SpeedOfTime / 60f;
		dt *= currentGamespeed;
		ParticleBase particleBase = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		while (particleBase != null)
		{
			particleBase.TickFixedStep(dt, game.api, partPhysics);
			if (!particleBase.Alive)
			{
				ParticleBase next = particleBase.Next;
				ParticlesPool.Kill(particleBase);
				particleBase = next;
			}
			else
			{
				particleBase.UpdateBuffers(updateBuffer, cameraPos, ref posPosition, ref rgbaPosition, ref flagPosition);
				particleBase = particleBase.Next;
			}
		}
		((IWorldAccessor)game).FrameProfiler.Mark("particles-tick");
		updateBuffer.CustomFloats.Count = ParticlesPool.AliveCount * 4;
		updateBuffer.CustomBytes.Count = ParticlesPool.AliveCount * 12;
		updateBuffer.VerticesCount = ParticlesPool.AliveCount;
		QuantityAlive = ParticlesPool.AliveCount;
		UpdateDebugScreen();
		game.Platform.UpdateMesh(particleModelRef, updateBuffer);
		((IWorldAccessor)game).FrameProfiler.Mark("particles-updatemesh");
	}

	private void ProcessParticlesFromOffThread(float dt, Vec3d cameraPos)
	{
		accumPhysics += dt;
		float num = tickTimes[readPosition];
		if (accumPhysics >= num)
		{
			lock (advanceCountLock)
			{
				if (advanceCount > 0)
				{
					readPosition = (readPosition + 1) % updateBuffers.Length;
					advanceCount--;
					accumPhysics -= num;
					num = tickTimes[readPosition];
				}
			}
			if (accumPhysics > 1f)
			{
				accumPhysics = 0f;
			}
		}
		float num2 = dt / num;
		MeshData meshData = updateBuffers[readPosition];
		float[] array = velocities[readPosition];
		int num3 = (QuantityAlive = meshData.VerticesCount);
		int num4 = num3;
		float num5 = (float)(this.cameraPos[readPosition].X - cameraPos.X);
		float num6 = (float)(this.cameraPos[readPosition].Y - cameraPos.Y);
		float num7 = (float)(this.cameraPos[readPosition].Z - cameraPos.Z);
		this.cameraPos[readPosition].X -= num5;
		this.cameraPos[readPosition].Y -= num6;
		this.cameraPos[readPosition].Z -= num7;
		float[] values = meshData.CustomFloats.Values;
		for (int i = 0; i < num4; i++)
		{
			int num8 = i * 4;
			values[num8] += num5 + array[i * 3] * num2;
			num8++;
			values[num8] += num6 + array[i * 3 + 1] * num2;
			num8++;
			values[num8] += num7 + array[i * 3 + 2] * num2;
		}
		game.Platform.UpdateMesh(particleModelRef, meshData);
		if (ModelType == EnumParticleModel.Quad)
		{
			if (game.extendedDebugInfo)
			{
				game.DebugScreenInfo["asyncquadparticlepool"] = "Async Quad Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
			}
			else
			{
				game.DebugScreenInfo["asyncquadparticlepool"] = "";
			}
		}
		else if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["asynccubeparticlepool"] = "Async Cube Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
		}
		else
		{
			game.DebugScreenInfo["asynccubeparticlepool"] = "";
		}
		((IWorldAccessor)game).FrameProfiler.Mark("otparticles-tick");
	}

	public void OnNewFrameOffThread(float dt, Vec3d cameraPos)
	{
		if (game.IsPaused || !offthread)
		{
			return;
		}
		lock (advanceCountLock)
		{
			if (advanceCount >= updateBuffers.Length - 1)
			{
				return;
			}
		}
		currentGamespeed = game.Calendar.SpeedOfTime / 60f;
		ParticleBase particleBase = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		MeshData meshData = updateBuffers[writePosition];
		float[] array = velocities[writePosition];
		Vec3d vec3d = this.cameraPos[writePosition].Set(cameraPos);
		if (ParticlesPool.AliveCount < 20000)
		{
			partPhysics.PhysicsTickTime = 0.0625f;
		}
		else
		{
			partPhysics.PhysicsTickTime = 0.125f;
		}
		float num = Math.Max(partPhysics.PhysicsTickTime, dt);
		float num2 = num * currentGamespeed;
		int num3 = 0;
		while (particleBase != null)
		{
			double x = particleBase.Position.X;
			double y = particleBase.Position.Y;
			double z = particleBase.Position.Z;
			particleBase.TickNow(num2, num2, game.api, partPhysics);
			if (!particleBase.Alive)
			{
				ParticleBase next = particleBase.Next;
				ParticlesPool.Kill(particleBase);
				particleBase = next;
				continue;
			}
			array[num3 * 3] = (particleBase.prevPosDeltaX = (float)(particleBase.Position.X - x));
			array[num3 * 3 + 1] = (particleBase.prevPosDeltaY = (float)(particleBase.Position.Y - y));
			array[num3 * 3 + 2] = (particleBase.prevPosDeltaZ = (float)(particleBase.Position.Z - z));
			num3++;
			particleBase.UpdateBuffers(meshData, vec3d, ref posPosition, ref rgbaPosition, ref flagPosition);
			particleBase = particleBase.Next;
		}
		meshData.CustomFloats.Count = num3 * 4;
		meshData.CustomBytes.Count = num3 * 12;
		meshData.VerticesCount = num3;
		tickTimes[writePosition] = Math.Min(num, 1f);
		writePosition = (writePosition + 1) % updateBuffers.Length;
		lock (advanceCountLock)
		{
			advanceCount++;
		}
	}

	internal virtual void UpdateDebugScreen()
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["quadparticlepool"] = "Quad Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
		}
		else
		{
			game.DebugScreenInfo["quadparticlepool"] = "";
		}
	}

	public void Dipose()
	{
		particleModelRef.Dispose();
	}
}
