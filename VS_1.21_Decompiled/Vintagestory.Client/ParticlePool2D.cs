using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class ParticlePool2D
{
	public FastParticlePool ParticlesPool;

	protected MeshRef particleModelRef;

	protected MeshData particleData;

	protected int poolSize;

	private ICoreClientAPI capi;

	protected Random rand = new Random();

	private Matrixf mat;

	private Vec4d tmpVec = new Vec4d();

	public virtual bool RenderTransparent => true;

	public MeshRef Model => particleModelRef;

	public int QuantityAlive => ParticlesPool.AliveCount;

	internal virtual float ParticleHeight => 0.5f;

	public virtual MeshData LoadModel()
	{
		MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 10f, 10f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		customQuadModelData.Flags = null;
		return customQuadModelData;
	}

	public ParticlePool2D(ICoreClientAPI capi, int poolSize)
	{
		this.capi = capi;
		this.poolSize = poolSize;
		ParticlesPool = new FastParticlePool(poolSize, () => new Particle2D());
		MeshData meshData = LoadModel();
		meshData.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			StaticDraw = false,
			Values = new float[poolSize * 9],
			InterleaveSizes = new int[4] { 4, 3, 1, 1 },
			InterleaveStride = 36,
			InterleaveOffsets = new int[4] { 0, 16, 28, 32 },
			Count = poolSize * 9
		};
		particleModelRef = ScreenManager.Platform.UploadMesh(meshData);
		particleData = new MeshData();
		particleData.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[poolSize * 9],
			Count = poolSize * 9
		};
	}

	public int Spawn(IParticlePropertiesProvider particleProperties)
	{
		float num = 5f;
		int i = 0;
		if (QuantityAlive >= poolSize)
		{
			return 0;
		}
		for (float quantity = particleProperties.Quantity; (float)i < quantity; i++)
		{
			if (ParticlesPool.FirstDead == null)
			{
				break;
			}
			if (rand.NextDouble() > (double)(quantity - (float)i))
			{
				break;
			}
			Particle2D particle2D = ParticlesPool.ReviveOne() as Particle2D;
			particle2D.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
			particle2D.ParentVelocity = particleProperties.ParentVelocity?.Clone();
			particleProperties.BeginParticle();
			particle2D.Position.Set(particleProperties.Pos);
			particle2D.Velocity.Set(particleProperties.GetVelocity(particle2D.Position));
			particle2D.StartingVelocity = particle2D.Velocity.Clone();
			particle2D.SizeMultiplier = particleProperties.Size;
			particle2D.ParticleHeight = ParticleHeight;
			particle2D.Color = ColorUtil.ToRGBABytes(particleProperties.GetRgbaColor(null));
			particle2D.VertexFlags = particleProperties.VertexFlags;
			particle2D.LifeLength = particleProperties.LifeLength * num;
			particle2D.SetAlive(particleProperties.GravityEffect);
			particle2D.OpacityEvolve = particleProperties.OpacityEvolve;
			particle2D.SizeEvolve = particleProperties.SizeEvolve;
			particle2D.VelocityEvolve = particleProperties.VelocityEvolve;
		}
		return i;
	}

	internal void TransformNextUpdate(Matrixf mat)
	{
		this.mat = mat;
	}

	public bool ShouldRender()
	{
		return ParticlesPool.AliveCount > 0;
	}

	public void OnNewFrame(float dt)
	{
		ParticleBase particleBase = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		while (particleBase != null)
		{
			particleBase.TickFixedStep(dt, capi, null);
			if (mat != null)
			{
				tmpVec.Set(particleBase.Position.X, particleBase.Position.Y, particleBase.Position.Z, 1.0);
				tmpVec = mat.TransformVector(tmpVec);
				particleBase.Position.X = tmpVec.X;
				particleBase.Position.Y = tmpVec.Y;
				particleBase.Position.Z = tmpVec.Z;
			}
			if (!particleBase.Alive)
			{
				ParticleBase next = particleBase.Next;
				ParticlesPool.Kill(particleBase);
				particleBase = next;
			}
			else
			{
				particleBase.UpdateBuffers(particleData, null, ref posPosition, ref rgbaPosition, ref flagPosition);
				particleBase = particleBase.Next;
			}
		}
		particleData.CustomFloats.Count = ParticlesPool.AliveCount * 9;
		particleData.VerticesCount = ParticlesPool.AliveCount;
		ScreenManager.Platform.UpdateMesh(particleModelRef, particleData);
		mat = null;
	}

	public void Dispose()
	{
		particleModelRef.Dispose();
	}
}
