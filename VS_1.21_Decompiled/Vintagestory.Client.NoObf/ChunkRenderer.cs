using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChunkRenderer
{
	protected MeshDataPoolMasterManager masterPool;

	protected ClientPlatformAbstract platform;

	protected ClientMain game;

	protected ChunkCuller culler;

	protected CustomMeshDataPartFloat twoCustomFloats;

	protected CustomMeshDataPartInt twoCustomInts;

	protected CustomMeshDataPartInt oneCustomInt;

	protected CustomMeshDataPartShort twoShortsNormalised;

	protected Vec2f blockTextureSize = new Vec2f();

	public int[] textureIds;

	public int QuantityRenderingChunks;

	public MeshDataPoolManager[][] poolsByRenderPass;

	private float subPixelPaddingX;

	private float subPixelPaddingY;

	private float curRainFall;

	private float lastSetRainFall;

	private float accum;

	public ChunkRenderer(int[] textureIds, ClientMain game)
	{
		this.textureIds = textureIds;
		platform = game.Platform;
		this.game = game;
		culler = new ChunkCuller(game);
		game.api.eventapi.ReloadShader += Eventapi_ReloadShader;
		twoCustomFloats = new CustomMeshDataPartFloat
		{
			InterleaveOffsets = new int[1],
			InterleaveSizes = new int[1] { 2 },
			InterleaveStride = 8
		};
		twoCustomInts = new CustomMeshDataPartInt
		{
			InterleaveOffsets = new int[2] { 0, 4 },
			InterleaveSizes = new int[2] { 1, 1 },
			InterleaveStride = 8,
			Conversion = DataConversion.Integer
		};
		oneCustomInt = new CustomMeshDataPartInt
		{
			InterleaveOffsets = new int[1],
			InterleaveSizes = new int[1] { 1 },
			InterleaveStride = 4,
			Conversion = DataConversion.Integer
		};
		twoShortsNormalised = new CustomMeshDataPartShort
		{
			InterleaveOffsets = new int[1],
			InterleaveSizes = new int[1] { 2 },
			InterleaveStride = 4,
			Conversion = DataConversion.NormalizedFloat
		};
		masterPool = new MeshDataPoolMasterManager(game.api);
		masterPool.DelayedPoolLocationRemoval = true;
		Array values = Enum.GetValues(typeof(EnumChunkRenderPass));
		poolsByRenderPass = new MeshDataPoolManager[values.Length][];
		int modelDataPoolMaxVertexSize = ClientSettings.ModelDataPoolMaxVertexSize;
		int modelDataPoolMaxIndexSize = ClientSettings.ModelDataPoolMaxIndexSize;
		int maxPartsPerPool = ClientSettings.ModelDataPoolMaxParts * 2;
		foreach (EnumChunkRenderPass item in values)
		{
			poolsByRenderPass[(int)item] = new MeshDataPoolManager[textureIds.Length + 3];
			for (int i = 0; i < textureIds.Length; i++)
			{
				AddPoolsForAtlasAndPass(i, item, modelDataPoolMaxVertexSize, modelDataPoolMaxIndexSize, maxPartsPerPool);
			}
		}
		blockTextureSize.X = (float)game.textureSize / (float)game.BlockAtlasManager.Size.Width;
		blockTextureSize.Y = (float)game.textureSize / (float)game.BlockAtlasManager.Size.Height;
	}

	internal void RuntimeAddBlockTextureAtlas(int[] textureIds)
	{
		Array values = Enum.GetValues(typeof(EnumChunkRenderPass));
		int modelDataPoolMaxVertexSize = ClientSettings.ModelDataPoolMaxVertexSize;
		int modelDataPoolMaxIndexSize = ClientSettings.ModelDataPoolMaxIndexSize;
		int maxPartsPerPool = ClientSettings.ModelDataPoolMaxParts * 2;
		int atlas = textureIds.Length - 1;
		foreach (EnumChunkRenderPass item in values)
		{
			AddPoolsForAtlasAndPass(atlas, item, modelDataPoolMaxVertexSize, modelDataPoolMaxIndexSize, maxPartsPerPool);
		}
		this.textureIds = textureIds;
	}

	private void AddPoolsForAtlasAndPass(int atlas, EnumChunkRenderPass pass, int maxVertices, int maxIndices, int maxPartsPerPool)
	{
		switch (pass)
		{
		case EnumChunkRenderPass.Liquid:
			poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(masterPool, game.frustumCuller, game.api, maxVertices, maxIndices, maxPartsPerPool, twoCustomFloats, null, null, twoCustomInts);
			break;
		case EnumChunkRenderPass.TopSoil:
			poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(masterPool, game.frustumCuller, game.api, maxVertices, maxIndices, maxPartsPerPool, null, twoShortsNormalised, null, oneCustomInt);
			break;
		default:
			poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(masterPool, game.frustumCuller, game.api, maxVertices, maxIndices, maxPartsPerPool, null, null, null, oneCustomInt);
			break;
		}
	}

	private bool Eventapi_ReloadShader()
	{
		lastSetRainFall = -1f;
		return true;
	}

	internal void SwapVisibleBuffers()
	{
		ClientChunk.bufIndex = (ClientChunk.bufIndex + 1) % 2;
		ModelDataPoolLocation.VisibleBufIndex = ClientChunk.bufIndex;
	}

	public void OnSeperateThreadTick(float dt)
	{
		culler.CullInvisibleChunks();
	}

	public void OnRenderBefore(float dt)
	{
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.LiquidDepth);
		game.Platform.ClearFrameBuffer(EnumFrameBuffer.LiquidDepth);
		subPixelPaddingX = game.BlockAtlasManager.SubPixelPaddingX;
		subPixelPaddingY = game.BlockAtlasManager.SubPixelPaddingY;
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		ShaderProgramChunkliquiddepth chunkliquiddepth = ShaderPrograms.Chunkliquiddepth;
		chunkliquiddepth.Use();
		chunkliquiddepth.ViewDistance = ClientSettings.ViewDistance;
		chunkliquiddepth.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunkliquiddepth.ModelViewMatrix = game.CurrentModelViewMatrix;
		bool useSSBOs = game.api.renderapi.useSSBOs;
		game.api.renderapi.useSSBOs = false;
		for (int i = 0; i < textureIds.Length; i++)
		{
			poolsByRenderPass[4][i].Render(cameraPos, "origin");
		}
		game.api.renderapi.useSSBOs = useSSBOs;
		chunkliquiddepth.Stop();
		ScreenManager.FrameProfiler.Mark("rend3D-ret-lide");
		game.GlPopMatrix();
		game.Platform.UnloadFrameBuffer(EnumFrameBuffer.LiquidDepth);
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
	}

	public void OnBeforeRenderOpaque(float dt)
	{
		masterPool.OnFrame(dt, game.CurrentModelViewMatrix, game.shadowMvpMatrix);
		RuntimeStats.renderedTriangles = 0;
		RuntimeStats.availableTriangles = 0;
		accum += dt;
		if (accum > 5f)
		{
			accum = 0f;
			ClimateCondition climateAt = game.BlockAccessor.GetClimateAt(game.EntityPlayer.Pos.AsBlockPos);
			float num = GameMath.Clamp((climateAt.Temperature + 1f) / 4f, 0f, 1f);
			curRainFall = climateAt.Rainfall * num;
		}
	}

	public void RenderShadow(float dt)
	{
		ShaderProgramShadowmapgeneric chunkshadowmap = ShaderPrograms.Chunkshadowmap;
		chunkshadowmap.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunkshadowmap.Uniform("subpixelPaddingY", subPixelPaddingY);
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		ScreenManager.FrameProfiler.Mark("rend3D-rets-begin");
		platform.GlDepthMask(flag: true);
		platform.GlToggleBlend(on: false);
		platform.GlEnableDepthTest();
		platform.GlDisableCullFace();
		EnumFrustumCullMode frustumCullMode = ((game.currentRenderStage == EnumRenderStage.ShadowFar) ? EnumFrustumCullMode.CullInstantShadowPassFar : EnumFrustumCullMode.CullInstantShadowPassNear);
		for (int i = 0; i < textureIds.Length; i++)
		{
			chunkshadowmap.Tex2d2D = textureIds[i];
			poolsByRenderPass[0][i].Render(cameraPos, "origin", frustumCullMode);
		}
		ScreenManager.FrameProfiler.Mark("rend3D-rets-op");
		for (int j = 0; j < textureIds.Length; j++)
		{
			chunkshadowmap.Tex2d2D = textureIds[j];
			poolsByRenderPass[5][j].Render(cameraPos, "origin", frustumCullMode);
		}
		ScreenManager.FrameProfiler.Mark("rend3D-rets-tpp");
		platform.GlDisableCullFace();
		for (int k = 0; k < textureIds.Length; k++)
		{
			chunkshadowmap.Tex2d2D = textureIds[k];
			poolsByRenderPass[2][k].Render(cameraPos, "origin", frustumCullMode);
		}
		for (int l = 0; l < textureIds.Length; l++)
		{
			chunkshadowmap.Tex2d2D = textureIds[l];
			poolsByRenderPass[1][l].Render(cameraPos, "origin", frustumCullMode);
		}
		platform.GlToggleBlend(on: true);
	}

	public void RenderOpaque(float dt)
	{
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		ScreenManager.FrameProfiler.Mark("rend3D-ret-begin");
		platform.GlDepthMask(flag: true);
		platform.GlEnableDepthTest();
		platform.GlToggleBlend(on: true);
		platform.GlEnableCullFace();
		game.GlMatrixModeModelView();
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		ShaderProgramChunkopaque chunkopaque = ShaderPrograms.Chunkopaque;
		chunkopaque.Use();
		chunkopaque.CameraUnderwater = game.shUniforms.CameraUnderwater;
		chunkopaque.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunkopaque.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunkopaque.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunkopaque.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunkopaque.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunkopaque.AlphaTest = 0.001f;
		chunkopaque.HaxyFade = 0;
		chunkopaque.LiquidDepth2D = game.Platform.FrameBuffers[5].DepthTextureId;
		chunkopaque.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunkopaque.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunkopaque.Uniform("subpixelPaddingY", subPixelPaddingY);
		for (int i = 0; i < textureIds.Length; i++)
		{
			chunkopaque.TerrainTex2D = textureIds[i];
			chunkopaque.TerrainTexLinear2D = textureIds[i];
			poolsByRenderPass[0][i].Render(cameraPos, "origin");
		}
		ScreenManager.FrameProfiler.Mark("rend3D-ret-op");
		chunkopaque.Stop();
		ShaderProgramChunktopsoil chunktopsoil = ShaderPrograms.Chunktopsoil;
		chunktopsoil.Use();
		chunktopsoil.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunktopsoil.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunktopsoil.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunktopsoil.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunktopsoil.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunktopsoil.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunktopsoil.BlockTextureSize = blockTextureSize;
		chunktopsoil.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunktopsoil.Uniform("subpixelPaddingY", subPixelPaddingY);
		for (int j = 0; j < textureIds.Length; j++)
		{
			chunktopsoil.TerrainTex2D = textureIds[j];
			chunktopsoil.TerrainTexLinear2D = textureIds[j];
			poolsByRenderPass[5][j].Render(cameraPos, "origin");
		}
		ScreenManager.FrameProfiler.Mark("rend3D-ret-tpp");
		chunktopsoil.Stop();
		platform.GlDisableCullFace();
		chunkopaque.Use();
		chunkopaque.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunkopaque.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunkopaque.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunkopaque.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunkopaque.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunkopaque.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunkopaque.AlphaTest = 0.25f;
		chunkopaque.HaxyFade = 0;
		platform.GlToggleBlend(on: true);
		for (int k = 0; k < textureIds.Length; k++)
		{
			chunkopaque.TerrainTex2D = textureIds[k];
			chunkopaque.TerrainTexLinear2D = textureIds[k];
			poolsByRenderPass[2][k].Render(cameraPos, "origin");
		}
		platform.GlToggleBlend(on: false);
		chunkopaque.AlphaTest = 0.42f;
		chunkopaque.SunPosition = game.GameWorldCalendar.SunPositionNormalized;
		chunkopaque.DayLight = game.shUniforms.SkyDaylight;
		chunkopaque.HorizonFog = game.AmbientManager.BlendedCloudDensity;
		chunkopaque.HaxyFade = 1;
		for (int l = 0; l < textureIds.Length; l++)
		{
			chunkopaque.TerrainTex2D = textureIds[l];
			chunkopaque.TerrainTexLinear2D = textureIds[l];
			poolsByRenderPass[1][l].Render(cameraPos, "origin");
		}
		chunkopaque.Stop();
		ScreenManager.FrameProfiler.Mark("rend3D-ret-opnc");
		game.GlPopMatrix();
		if (game.unbindSamplers)
		{
			GL.BindSampler(0, 0);
			GL.BindSampler(1, 0);
			GL.BindSampler(2, 0);
			GL.BindSampler(3, 0);
			GL.BindSampler(4, 0);
			GL.BindSampler(5, 0);
			GL.BindSampler(6, 0);
			GL.BindSampler(7, 0);
			GL.BindSampler(8, 0);
		}
	}

	internal void RenderOIT(float deltaTime)
	{
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		game.GlPushMatrix();
		ShaderProgramChunkliquid chunkliquid = ShaderPrograms.Chunkliquid;
		chunkliquid.Use();
		chunkliquid.WaterStillCounter = game.shUniforms.WaterStillCounter;
		chunkliquid.WaterFlowCounter = game.shUniforms.WaterFlowCounter;
		chunkliquid.WindWaveIntensity = game.shUniforms.WindWaveIntensity;
		chunkliquid.SunPosRel = game.shUniforms.SunPosition3D;
		chunkliquid.SunColor = game.Calendar.SunColor;
		chunkliquid.ReflectColor = game.Calendar.ReflectColor;
		chunkliquid.PlayerPosForFoam = game.shUniforms.PlayerPosForFoam;
		chunkliquid.CameraUnderwater = game.shUniforms.CameraUnderwater;
		chunkliquid.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunkliquid.Uniform("subpixelPaddingY", subPixelPaddingY);
		if (Math.Abs(lastSetRainFall - curRainFall) > 0.05f || curRainFall == 0f)
		{
			chunkliquid.DropletIntensity = (lastSetRainFall = curRainFall);
		}
		FrameBufferRef frameBufferRef = game.api.Render.FrameBuffers[0];
		chunkliquid.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunkliquid.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunkliquid.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunkliquid.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunkliquid.BlockTextureSize = blockTextureSize;
		chunkliquid.TextureAtlasSize = new Vec2f(game.BlockAtlasManager.Size);
		chunkliquid.ToShadowMapSpaceMatrixFar = game.toShadowMapSpaceMatrixFar;
		chunkliquid.ToShadowMapSpaceMatrixNear = game.toShadowMapSpaceMatrixNear;
		chunkliquid.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunkliquid.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunkliquid.PlayerViewVec = game.shUniforms.PlayerViewVector;
		chunkliquid.DepthTex2D = frameBufferRef.DepthTextureId;
		chunkliquid.FrameSize = new Vec2f(frameBufferRef.Width, frameBufferRef.Height);
		chunkliquid.SunSpecularIntensity = game.shUniforms.SunSpecularIntensity;
		bool useSSBOs = game.api.renderapi.useSSBOs;
		game.api.renderapi.useSSBOs = false;
		for (int i = 0; i < textureIds.Length; i++)
		{
			chunkliquid.TerrainTex2D = textureIds[i];
			poolsByRenderPass[4][i].Render(cameraPos, "origin");
		}
		game.api.renderapi.useSSBOs = useSSBOs;
		chunkliquid.Stop();
		ScreenManager.FrameProfiler.Mark("rend3D-ret-lp");
		game.GlPopMatrix();
		ShaderProgramChunktransparent chunktransparent = ShaderPrograms.Chunktransparent;
		chunktransparent.Use();
		chunktransparent.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunktransparent.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunktransparent.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunktransparent.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunktransparent.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunktransparent.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunktransparent.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunktransparent.Uniform("subpixelPaddingY", subPixelPaddingY);
		for (int j = 0; j < textureIds.Length; j++)
		{
			chunktransparent.TerrainTex2D = textureIds[j];
			poolsByRenderPass[3][j].Render(cameraPos, "origin");
			if (ClientSettings.RenderMetaBlocks)
			{
				poolsByRenderPass[6][j].Render(cameraPos, "origin");
			}
		}
		chunktransparent.Stop();
		game.GlPopMatrix();
		ScreenManager.FrameProfiler.Mark("rend3D-ret-tp");
	}

	internal void RenderAfterOIT(float deltaTime)
	{
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		ShaderProgramChunkopaque chunkopaque = ShaderPrograms.Chunkopaque;
		platform.GlDisableCullFace();
		platform.GlToggleBlend(on: false);
		platform.GlEnableDepthTest();
		chunkopaque.Use();
		chunkopaque.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		chunkopaque.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
		chunkopaque.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		chunkopaque.FogMinIn = game.AmbientManager.BlendedFogMin;
		chunkopaque.ProjectionMatrix = game.CurrentProjectionMatrix;
		chunkopaque.ModelViewMatrix = game.CurrentModelViewMatrix;
		chunkopaque.AlphaTest = 0.42f;
		chunkopaque.SunPosition = game.GameWorldCalendar.SunPositionNormalized;
		chunkopaque.DayLight = game.shUniforms.SkyDaylight;
		chunkopaque.HorizonFog = game.AmbientManager.BlendedCloudDensity;
		chunkopaque.HaxyFade = 1;
		chunkopaque.Uniform("subpixelPaddingX", subPixelPaddingX);
		chunkopaque.Uniform("subpixelPaddingY", subPixelPaddingY);
		for (int i = 0; i < textureIds.Length; i++)
		{
			chunkopaque.TerrainTex2D = textureIds[i];
			chunkopaque.TerrainTexLinear2D = textureIds[i];
			poolsByRenderPass[7][i].Render(cameraPos, "origin");
		}
		chunkopaque.Stop();
		game.GlPopMatrix();
	}

	public void Dispose()
	{
		masterPool.DisposeAllPools(game.api);
	}

	public void AddTesselatedChunk(TesselatedChunk tesschunk, ClientChunk hostChunk)
	{
		Vec3i chunkOrigin = new Vec3i(tesschunk.positionX, tesschunk.positionYAndDimension % 32768, tesschunk.positionZ);
		int dimension = tesschunk.positionYAndDimension / 32768;
		Sphere boundingSphere = tesschunk.boundingSphere;
		tesschunk.AddCenterToPools(this, chunkOrigin, dimension, boundingSphere, hostChunk);
		tesschunk.AddEdgeToPools(this, chunkOrigin, dimension, boundingSphere, hostChunk);
		tesschunk.centerParts = null;
		tesschunk.edgeParts = null;
		tesschunk.chunk = null;
	}

	public void RemoveDataPoolLocations(ModelDataPoolLocation[] locations)
	{
		masterPool.RemoveDataPoolLocations(locations);
	}

	public void GetStats(out long usedVideoMemory, out long renderedTris, out long allocatedTris)
	{
		usedVideoMemory = 0L;
		renderedTris = 0L;
		allocatedTris = 0L;
		foreach (EnumChunkRenderPass value in Enum.GetValues(typeof(EnumChunkRenderPass)))
		{
			for (int i = 0; i < textureIds.Length; i++)
			{
				poolsByRenderPass[(int)value][i].GetStats(ref usedVideoMemory, ref renderedTris, ref allocatedTris);
			}
		}
	}

	public float CalcFragmentation()
	{
		return masterPool.CalcFragmentation();
	}

	public int QuantityModelDataPools()
	{
		return masterPool.QuantityModelDataPools();
	}

	internal void SetInterleaveStrides(MeshData modelDataLod0, EnumChunkRenderPass pass)
	{
		if (pass == EnumChunkRenderPass.Liquid)
		{
			modelDataLod0.CustomFloats.InterleaveStride = twoCustomFloats.InterleaveStride;
			modelDataLod0.CustomInts.InterleaveStride = twoCustomInts.InterleaveStride;
			return;
		}
		modelDataLod0.CustomInts.InterleaveStride = oneCustomInt.InterleaveStride;
		if (pass == EnumChunkRenderPass.TopSoil)
		{
			modelDataLod0.CustomShorts.InterleaveStride = twoShortsNormalised.InterleaveStride;
		}
	}
}
