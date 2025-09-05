using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleSystem : ModSystem, IRenderer, IDisposable
{
	protected MeshRef particleModelRef;

	protected MeshData[] updateBuffers;

	protected Vec3d[] cameraPos;

	protected float[] tickTimes;

	protected float[][] velocities;

	protected int writePosition = 1;

	protected int readPosition;

	protected object advanceCountLock = new object();

	protected int advanceCount;

	protected Random rand = new Random();

	protected ICoreClientAPI capi;

	protected float currentGamespeed;

	protected ParticlePhysics partPhysics;

	protected EnumParticleModel ModelType = EnumParticleModel.Cube;

	private IShaderProgram vec3ScaleCubeParticleShader;

	private int poolSize = 10000;

	private int quantityAlive;

	private int offthreadid;

	private EPCounter counter = new EPCounter();

	public HashSet<FastVec3i> SpawnedFish = new HashSet<FastVec3i>();

	private bool isShuttingDown;

	public EntityParticle FirstAlive;

	public EntityParticle LastAlive;

	private float accumPhysics;

	public MeshRef Model => particleModelRef;

	public IBlockAccessor BlockAccess => partPhysics.BlockAccess;

	public double RenderOrder => 1.0;

	public int RenderRange => 50;

	public EPCounter Count => counter;

	public event Action<float> OnSimTick;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public virtual MeshData LoadModel()
	{
		MeshData cubeOnlyScaleXyz = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f());
		cubeOnlyScaleXyz.WithNormals();
		cubeOnlyScaleXyz.Rgba = null;
		for (int i = 0; i < 24; i++)
		{
			BlockFacing facing = BlockFacing.ALLFACES[i / 4];
			cubeOnlyScaleXyz.AddNormal(facing);
		}
		return cubeOnlyScaleXyz;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "reeps-op");
		Thread thread = TyronThreadPool.CreateDedicatedThread(onThreadStart, "entityparticlesim");
		offthreadid = thread.ManagedThreadId;
		capi.Event.LeaveWorld += delegate
		{
			isShuttingDown = true;
		};
		thread.Start();
		partPhysics = new ParticlePhysics(api.World.GetLockFreeBlockAccessor());
		partPhysics.PhysicsTickTime = 0.125f;
		MeshData meshData = LoadModel();
		meshData.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			StaticDraw = false,
			Values = new float[poolSize * 10],
			InterleaveSizes = new int[3] { 3, 3, 4 },
			InterleaveStride = 40,
			InterleaveOffsets = new int[3] { 0, 12, 24 },
			Count = poolSize * 10
		};
		meshData.CustomBytes = new CustomMeshDataPartByte
		{
			Conversion = DataConversion.NormalizedFloat,
			Instanced = true,
			StaticDraw = false,
			Values = new byte[poolSize * 8],
			InterleaveSizes = new int[2] { 4, 4 },
			InterleaveStride = 8,
			InterleaveOffsets = new int[2] { 0, 4 },
			Count = poolSize * 8
		};
		meshData.Flags = new int[poolSize];
		meshData.FlagsInstanced = true;
		particleModelRef = api.Render.UploadMesh(meshData);
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
		api.Event.ReloadShader += LoadShader;
		LoadShader();
	}

	public bool LoadShader()
	{
		IShaderProgram shaderProgram = (vec3ScaleCubeParticleShader = capi.Shader.NewShaderProgram());
		shaderProgram.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		shaderProgram.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		shaderProgram.VertexShader.PrefixCode += "#define VEC3SCALE 1\n";
		capi.Shader.RegisterFileShaderProgram("particlescube", shaderProgram);
		return shaderProgram.Compile();
	}

	private void onThreadStart()
	{
		while (!isShuttingDown)
		{
			Thread.Sleep(10);
			if (!capi.IsGamePaused)
			{
				Vec3d vec3d = capi.World.Player?.Entity?.CameraPos.Clone();
				if (vec3d != null)
				{
					OnNewFrameOffThread(0.01f, vec3d);
				}
			}
		}
	}

	private MeshData genUpdateBuffer()
	{
		return new MeshData
		{
			CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[poolSize * 10],
				Count = poolSize * 10
			},
			CustomBytes = new CustomMeshDataPartByte
			{
				Values = new byte[poolSize * 8],
				Count = poolSize * 8
			},
			Flags = new int[poolSize],
			FlagsInstanced = true
		};
	}

	public void SpawnParticle(EntityParticle eparticle)
	{
		if (Environment.CurrentManagedThreadId != offthreadid)
		{
			throw new InvalidOperationException("Only in the entityparticle thread");
		}
		eparticle.Prev = null;
		eparticle.Next = null;
		if (FirstAlive == null)
		{
			FirstAlive = eparticle;
			LastAlive = eparticle;
		}
		else
		{
			eparticle.Prev = LastAlive;
			LastAlive.Next = eparticle;
			LastAlive = eparticle;
		}
		eparticle.OnSpawned(partPhysics);
		counter.Inc(eparticle.Type);
		quantityAlive++;
	}

	protected void KillParticle(EntityParticle entityParticle)
	{
		if (Environment.CurrentManagedThreadId != offthreadid)
		{
			throw new InvalidOperationException("Only in the entityparticle thread");
		}
		ParticleBase prev = entityParticle.Prev;
		ParticleBase next = entityParticle.Next;
		if (prev != null)
		{
			prev.Next = next;
		}
		if (next != null)
		{
			next.Prev = prev;
		}
		if (FirstAlive == entityParticle)
		{
			FirstAlive = (EntityParticle)next;
		}
		if (LastAlive == entityParticle)
		{
			LastAlive = ((EntityParticle)prev) ?? FirstAlive;
		}
		entityParticle.Prev = null;
		entityParticle.Next = null;
		if (entityParticle is EntityParticleFish entityParticleFish)
		{
			SpawnedFish.Remove(entityParticleFish.StartPos);
		}
		quantityAlive--;
		counter.Dec(entityParticle.Type);
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		IShaderProgram shaderProgram = vec3ScaleCubeParticleShader;
		shaderProgram.Use();
		capi.Render.GlToggleBlend(blend: true);
		capi.Render.GlPushMatrix();
		capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
		shaderProgram.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
		shaderProgram.Uniform("rgbaAmbientIn", capi.Ambient.BlendedAmbientColor);
		shaderProgram.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
		shaderProgram.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
		shaderProgram.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		shaderProgram.UniformMatrix("modelViewMatrix", capi.Render.CurrentModelviewMatrix);
		OnNewFrame(dt, capi.World.Player.Entity.CameraPos);
		capi.Render.RenderMeshInstanced(Model, quantityAlive);
		shaderProgram.Stop();
		capi.Render.GlPopMatrix();
	}

	public void OnNewFrame(float dt, Vec3d cameraPos)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
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
		int num3 = (quantityAlive = meshData.VerticesCount);
		float num4 = (float)(this.cameraPos[readPosition].X - cameraPos.X);
		float num5 = (float)(this.cameraPos[readPosition].Y - cameraPos.Y);
		float num6 = (float)(this.cameraPos[readPosition].Z - cameraPos.Z);
		this.cameraPos[readPosition].X -= num4;
		this.cameraPos[readPosition].Y -= num5;
		this.cameraPos[readPosition].Z -= num6;
		float[] values = meshData.CustomFloats.Values;
		for (int i = 0; i < num3; i++)
		{
			int num7 = i * 10;
			values[num7] += num4 + array[i * 3] * num2;
			num7++;
			values[num7] += num5 + array[i * 3 + 1] * num2;
			num7++;
			values[num7] += num6 + array[i * 3 + 2] * num2;
		}
		capi.Render.UpdateMesh(particleModelRef, meshData);
	}

	public void OnNewFrameOffThread(float dt, Vec3d cameraPos)
	{
		if (capi.IsGamePaused)
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
		this.OnSimTick?.Invoke(dt);
		currentGamespeed = capi.World.Calendar.SpeedOfTime / 60f * 5f;
		ParticleBase particleBase = FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		MeshData meshData = updateBuffers[writePosition];
		float[] array = velocities[writePosition];
		Vec3d vec3d = this.cameraPos[writePosition].Set(cameraPos);
		partPhysics.PhysicsTickTime = 1f / 64f;
		float num = Math.Max(partPhysics.PhysicsTickTime, dt);
		float num2 = num * currentGamespeed;
		int num3 = 0;
		while (particleBase != null)
		{
			double x = particleBase.Position.X;
			double y = particleBase.Position.Y;
			double z = particleBase.Position.Z;
			particleBase.TickNow(num2, num2, capi, partPhysics);
			if (!particleBase.Alive)
			{
				ParticleBase next = particleBase.Next;
				KillParticle((EntityParticle)particleBase);
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
		meshData.CustomFloats.Count = num3 * 10;
		meshData.CustomBytes.Count = num3 * 8;
		meshData.VerticesCount = num3;
		tickTimes[writePosition] = Math.Min(num, 1f);
		writePosition = (writePosition + 1) % updateBuffers.Length;
		lock (advanceCountLock)
		{
			advanceCount++;
		}
	}

	public override void Dispose()
	{
		particleModelRef?.Dispose();
	}

	public void Clear()
	{
		FirstAlive = null;
		LastAlive = null;
		quantityAlive = 0;
		counter.Clear();
	}
}
