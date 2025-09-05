using System;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CloudRenderer : CloudRendererBase, IRenderer, IDisposable
{
	private CloudTilesState committedState = new CloudTilesState();

	private CloudTilesState mainThreadState = new CloudTilesState();

	private CloudTilesState offThreadState = new CloudTilesState();

	private CloudTile[] Tiles;

	private CloudTile[] tempTiles;

	private bool newStateRready;

	private object cloudStateLock = new object();

	internal float blendedCloudDensity;

	internal float blendedGlobalCloudBrightness;

	public int QuantityCloudTiles = 25;

	private MeshRef cloudTilesMeshRef;

	private long windChangeTimer;

	private float cloudSpeedX;

	private float cloudSpeedZ;

	private float targetCloudSpeedX;

	private float targetCloudSpeedZ;

	private Random rand;

	private bool renderClouds;

	private WeatherSystemClient weatherSys;

	private Thread cloudTileUpdThread;

	private bool isShuttingDown;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private Matrixf mvMat = new Matrixf();

	private int cloudTileBlendSpeed = 32;

	private MeshData updateMesh = new MeshData
	{
		CustomShorts = new CustomMeshDataPartShort()
	};

	private WeatherDataReaderPreLoad wreaderpreload;

	private bool isFirstTick = true;

	private bool requireTileRebuild;

	public bool instantTileBlend;

	private int accum = 20;

	public double RenderOrder => 0.35;

	public int RenderRange => 9999;

	public CloudRenderer(ICoreClientAPI capi, WeatherSystemClient weatherSys)
	{
		this.capi = capi;
		this.weatherSys = weatherSys;
		wreaderpreload = weatherSys.getWeatherDataReaderPreLoad();
		rand = new Random(capi.World.Seed);
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "clouds");
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
		double num = capi.World.Calendar.TotalHours * 60.0;
		windOffsetX += 2.0 * num;
		windOffsetZ += 0.10000000149011612 * num;
		mainThreadState.WindTileOffsetX += (int)(windOffsetX / (double)base.CloudTileSize);
		windOffsetX %= base.CloudTileSize;
		mainThreadState.WindTileOffsetZ += (int)(windOffsetZ / (double)base.CloudTileSize);
		windOffsetZ %= base.CloudTileSize;
		offThreadState.Set(mainThreadState);
		committedState.Set(mainThreadState);
		InitCloudTiles(8 * capi.World.Player.WorldData.DesiredViewDistance);
		LoadCloudModel();
		capi.Settings.AddWatcher<int>("viewDistance", OnViewDistanceChanged);
		capi.Settings.AddWatcher("cloudRenderMode", delegate(int val)
		{
			renderClouds = val == 2;
		});
		renderClouds = capi.Settings.Int.Get("cloudRenderMode", 0) == 2;
		InitCustomDataBuffers(updateMesh);
		capi.Event.LeaveWorld += delegate
		{
			isShuttingDown = true;
		};
		cloudTileUpdThread = new Thread((ThreadStart)delegate
		{
			while (!isShuttingDown)
			{
				if (!newStateRready)
				{
					int num2 = (int)windOffsetX / base.CloudTileSize;
					int num3 = (int)windOffsetZ / base.CloudTileSize;
					int x = offThreadState.CenterTilePos.X;
					int z = offThreadState.CenterTilePos.Z;
					offThreadState.Set(mainThreadState);
					offThreadState.WindTileOffsetX += num2;
					offThreadState.WindTileOffsetZ += num3;
					int num4 = num2 + x - offThreadState.CenterTilePos.X;
					int num5 = num3 + z - offThreadState.CenterTilePos.Z;
					if (num4 != 0 || num5 != 0)
					{
						MoveCloudTilesOffThread(num4, num5);
					}
					UpdateCloudTilesOffThread(instantTileBlend ? 32767 : cloudTileBlendSpeed);
					instantTileBlend = false;
					newStateRready = true;
				}
				Thread.Sleep(40);
			}
		});
		cloudTileUpdThread.IsBackground = true;
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("clouds", prog);
		return prog.Compile();
	}

	private void OnViewDistanceChanged(int newValue)
	{
		requireTileRebuild = true;
	}

	public void CloudTick(float deltaTime)
	{
		blendedCloudDensity = capi.Ambient.BlendedCloudDensity;
		blendedGlobalCloudBrightness = capi.Ambient.BlendedCloudBrightness;
		if (isFirstTick)
		{
			weatherSys.ProcessWeatherUpdates();
			UpdateCloudTilesOffThread(32767);
			cloudTileUpdThread.Start();
			isFirstTick = false;
		}
		deltaTime = Math.Min(deltaTime, 1f);
		deltaTime *= capi.World.Calendar.SpeedOfTime / 60f;
		if (deltaTime > 0f)
		{
			if (windChangeTimer - capi.ElapsedMilliseconds < 0)
			{
				windChangeTimer = capi.ElapsedMilliseconds + rand.Next(20000, 120000);
				targetCloudSpeedX = (float)rand.NextDouble() * 5f;
				targetCloudSpeedZ = (float)rand.NextDouble() * 0.5f;
			}
			float num = 3f * (float)weatherSys.WeatherDataAtPlayer.GetWindSpeed(capi.World.Player.Entity.Pos.Y);
			cloudSpeedX += (targetCloudSpeedX + num - cloudSpeedX) * deltaTime;
			cloudSpeedZ += (targetCloudSpeedZ - cloudSpeedZ) * deltaTime;
		}
		lock (cloudStateLock)
		{
			if (deltaTime > 0f)
			{
				windOffsetX += cloudSpeedX * deltaTime;
				windOffsetZ += cloudSpeedZ * deltaTime;
			}
			mainThreadState.CenterTilePos.X = (int)capi.World.Player.Entity.Pos.X / base.CloudTileSize;
			mainThreadState.CenterTilePos.Z = (int)capi.World.Player.Entity.Pos.Z / base.CloudTileSize;
		}
		if (newStateRready)
		{
			int num2 = offThreadState.WindTileOffsetX - committedState.WindTileOffsetX;
			int num3 = offThreadState.WindTileOffsetZ - committedState.WindTileOffsetZ;
			committedState.Set(offThreadState);
			mainThreadState.WindTileOffsetX = committedState.WindTileOffsetX;
			mainThreadState.WindTileOffsetZ = committedState.WindTileOffsetZ;
			windOffsetX -= num2 * base.CloudTileSize;
			windOffsetZ -= num3 * base.CloudTileSize;
			UpdateBufferContents(updateMesh);
			capi.Render.UpdateMesh(cloudTilesMeshRef, updateMesh);
			weatherSys.ProcessWeatherUpdates();
			if (requireTileRebuild)
			{
				InitCloudTiles(8 * capi.World.Player.WorldData.DesiredViewDistance);
				UpdateCloudTiles();
				LoadCloudModel();
				InitCustomDataBuffers(updateMesh);
				requireTileRebuild = false;
				instantTileBlend = true;
			}
			newStateRready = false;
		}
		capi.World.FrameProfiler.Mark("gt-clouds");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (renderClouds)
		{
			if (!capi.IsGamePaused)
			{
				CloudTick(deltaTime);
			}
			if (capi.Render.FrameWidth != 0)
			{
				capi.Render.ShaderUniforms.PerceptionEffectIntensity *= 20f;
				prog.Use();
				capi.Render.ShaderUniforms.PerceptionEffectIntensity /= 20f;
				prog.Uniform("sunPosition", capi.World.Calendar.SunPositionNormalized);
				double x = capi.World.Player.Entity.Pos.X;
				double z = capi.World.Player.Entity.Pos.Z;
				double num = (double)(committedState.CenterTilePos.X * base.CloudTileSize) - x + windOffsetX;
				double num2 = (double)(committedState.CenterTilePos.Z * base.CloudTileSize) - z + windOffsetZ;
				prog.Uniform("sunColor", capi.World.Calendar.SunColor);
				prog.Uniform("dayLight", Math.Max(0f, capi.World.Calendar.DayLightStrength - capi.World.Calendar.MoonLightStrength * 0.95f));
				prog.Uniform("windOffset", new Vec3f((float)num, 0f, (float)num2));
				prog.Uniform("alpha", GameMath.Clamp(1f - 1.5f * Math.Max(0f, capi.Render.ShaderUniforms.GlitchStrength - 0.1f), 0f, 1f));
				prog.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
				prog.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
				prog.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
				prog.Uniform("playerPos", capi.Render.ShaderUniforms.PlayerPos);
				prog.Uniform("tileOffset", new Vec2f((committedState.CenterTilePos.X - committedState.TileOffsetX) * base.CloudTileSize, (committedState.CenterTilePos.Z - committedState.TileOffsetZ) * base.CloudTileSize));
				prog.Uniform("cloudTileSize", base.CloudTileSize);
				prog.Uniform("cloudsLength", (float)base.CloudTileSize * (float)CloudTileLength);
				prog.Uniform("globalCloudBrightness", blendedGlobalCloudBrightness);
				float num3 = (float)((double)(weatherSys.CloudLevelRel * (float)capi.World.BlockAccessor.MapSizeY) + 0.5 - capi.World.Player.Entity.CameraPos.Y);
				prog.Uniform("cloudYTranslate", num3);
				prog.Uniform("cloudCounter", (float)(capi.World.Calendar.TotalHours * 20.0 % 578.0));
				prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				prog.Uniform("flatFogDensity", capi.Ambient.BlendedFlatFogDensity);
				prog.Uniform("flatFogStart", capi.Ambient.BlendedFlatFogYPosForShader - (float)capi.World.Player.Entity.CameraPos.Y);
				mvMat.Set(capi.Render.CameraMatrixOriginf).Translate(num, num3, num2);
				prog.UniformMatrix("modelViewMatrix", mvMat.Values);
				capi.Render.RenderMeshInstanced(cloudTilesMeshRef, QuantityCloudTiles);
				prog.Stop();
			}
		}
	}

	public void InitCloudTiles(int viewDistance)
	{
		CloudTileLength = GameMath.Clamp(viewDistance / base.CloudTileSize, 20, 200);
		QuantityCloudTiles = CloudTileLength * CloudTileLength;
		Tiles = new CloudTile[QuantityCloudTiles];
		tempTiles = new CloudTile[QuantityCloudTiles];
		int num = rand.Next();
		for (int i = 0; i < CloudTileLength; i++)
		{
			for (int j = 0; j < CloudTileLength; j++)
			{
				Tiles[i * CloudTileLength + j] = new CloudTile
				{
					GridXOffset = (short)(i - CloudTileLength / 2),
					GridZOffset = (short)(j - CloudTileLength / 2),
					brightnessRand = new LCGRandom(num)
				};
			}
		}
	}

	public void UpdateCloudTilesOffThread(int changeSpeed)
	{
		bool flag = false;
		accum++;
		if (accum > 10)
		{
			accum = 0;
			flag = true;
		}
		int num = CloudTileLength * CloudTileLength;
		int num2 = -9999;
		int num3 = -9999;
		Vec3i tilePos = new Vec3i(offThreadState.TileOffsetX - offThreadState.WindTileOffsetX, 0, offThreadState.TileOffsetZ - offThreadState.WindTileOffsetZ);
		Vec3i centerTilePos = offThreadState.CenterTilePos;
		for (int i = 0; i < num; i++)
		{
			CloudTile cloudTile = Tiles[i];
			int num4 = centerTilePos.X + cloudTile.GridXOffset;
			int num5 = centerTilePos.Z + cloudTile.GridZOffset;
			cloudTile.brightnessRand.InitPositionSeed(num4 - offThreadState.WindTileOffsetX, num5 - offThreadState.WindTileOffsetZ);
			Vec3d vec3d = new Vec3d(num4 * base.CloudTileSize, capi.World.SeaLevel, num5 * base.CloudTileSize);
			int regionSize = capi.World.BlockAccessor.RegionSize;
			int num6 = (int)Math.Round(vec3d.X / (double)regionSize) - 1;
			int num7 = (int)Math.Round(vec3d.Z / (double)regionSize) - 1;
			if (num6 != num2 || num7 != num3)
			{
				num2 = num6;
				num3 = num7;
				wreaderpreload.LoadAdjacentSims(vec3d);
				wreaderpreload.EnsureCloudTileCacheIsFresh(tilePos);
			}
			if (flag || !cloudTile.rainValuesSet)
			{
				wreaderpreload.LoadLerp(vec3d);
				cloudTile.lerpRainCloudOverlay = wreaderpreload.lerpRainCloudOverlay;
				cloudTile.lerpRainOverlay = wreaderpreload.lerpRainOverlay;
				cloudTile.rainValuesSet = true;
			}
			else
			{
				wreaderpreload.LoadLerp(vec3d, useArgValues: true, cloudTile.lerpRainCloudOverlay, cloudTile.lerpRainOverlay);
			}
			int cloudTileX = (int)vec3d.X / base.CloudTileSize;
			int cloudTileZ = (int)vec3d.Z / base.CloudTileSize;
			double num8 = GameMath.Clamp(wreaderpreload.GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), 0.0, 1.0);
			double val = wreaderpreload.GetBlendedCloudBrightness(1f) * (double)(0.85f + cloudTile.brightnessRand.NextFloat() * 0.15f);
			cloudTile.TargetBrightnes = (short)(GameMath.Clamp(val, 0.0, 1.0) * 32767.0);
			cloudTile.TargetThickness = (short)GameMath.Clamp(num8 * 32767.0, 0.0, 32767.0);
			cloudTile.TargetThinCloudMode = (short)GameMath.Clamp(wreaderpreload.GetBlendedThinCloudModeness() * 32767.0, 0.0, 32767.0);
			cloudTile.TargetCloudOpaquenes = (short)GameMath.Clamp(wreaderpreload.GetBlendedCloudOpaqueness() * 32767.0, 0.0, 32767.0);
			cloudTile.TargetUndulatingCloudMode = (short)GameMath.Clamp(wreaderpreload.GetBlendedUndulatingCloudModeness() * 32767.0, 0.0, 32767.0);
			cloudTile.Brightness = LerpTileValue(cloudTile.TargetBrightnes, cloudTile.Brightness, changeSpeed);
			cloudTile.SelfThickness = LerpTileValue(cloudTile.TargetThickness, cloudTile.SelfThickness, changeSpeed);
			cloudTile.ThinCloudMode = LerpTileValue(cloudTile.TargetThinCloudMode, cloudTile.ThinCloudMode, changeSpeed);
			cloudTile.CloudOpaqueness = LerpTileValue(cloudTile.TargetCloudOpaquenes, cloudTile.CloudOpaqueness, changeSpeed);
			cloudTile.UndulatingCloudMode = LerpTileValue(cloudTile.TargetUndulatingCloudMode, cloudTile.UndulatingCloudMode, changeSpeed);
			if (i > 0)
			{
				Tiles[i - 1].NorthTileThickness = cloudTile.SelfThickness;
			}
			if (i < Tiles.Length - 1)
			{
				Tiles[i + 1].SouthTileThickness = cloudTile.SelfThickness;
			}
			if (i < CloudTileLength - 1)
			{
				Tiles[i + CloudTileLength].EastTileThickness = cloudTile.SelfThickness;
			}
			if (i > CloudTileLength - 1)
			{
				Tiles[i - CloudTileLength].WestTileThickness = cloudTile.SelfThickness;
			}
		}
	}

	private short LerpTileValue(int target, int current, int changeSpeed)
	{
		float num = GameMath.Clamp(target - current, -changeSpeed, changeSpeed);
		return (short)GameMath.Clamp((float)current + num, 0f, 32767f);
	}

	public void MoveCloudTilesOffThread(int dx, int dz)
	{
		for (int i = 0; i < CloudTileLength; i++)
		{
			for (int j = 0; j < CloudTileLength; j++)
			{
				int num = GameMath.Mod(i + dx, CloudTileLength);
				int num2 = GameMath.Mod(j + dz, CloudTileLength);
				CloudTile cloudTile = Tiles[i * CloudTileLength + j];
				cloudTile.GridXOffset = (short)(num - CloudTileLength / 2);
				cloudTile.GridZOffset = (short)(num2 - CloudTileLength / 2);
				tempTiles[num * CloudTileLength + num2] = cloudTile;
			}
		}
		CloudTile[] tiles = Tiles;
		Tiles = tempTiles;
		tempTiles = tiles;
	}

	public void LoadCloudModel()
	{
		MeshData meshData = new MeshData(24, 36, withNormals: false, withUv: false);
		meshData.Flags = new int[24];
		float[] blockSideShadings = new float[4] { 1f, 0.9f, 0.9f, 0.7f };
		MeshData cubeModelDataForClouds = CloudMeshUtil.GetCubeModelDataForClouds(base.CloudTileSize / 2, base.CloudTileSize / 4, new Vec3f(0f, 0f, 0f));
		byte[] shadedCubeRGBA = CubeMeshUtil.GetShadedCubeRGBA(-1, blockSideShadings, smoothShadedSides: false);
		cubeModelDataForClouds.SetRgba(shadedCubeRGBA);
		cubeModelDataForClouds.Flags = new int[24]
		{
			0, 0, 0, 0, 1, 1, 1, 1, 2, 2,
			2, 2, 3, 3, 3, 3, 4, 4, 4, 4,
			5, 5, 5, 5
		};
		meshData.AddMeshData(cubeModelDataForClouds);
		InitCustomDataBuffers(meshData);
		UpdateBufferContents(meshData);
		cloudTilesMeshRef?.Dispose();
		cloudTilesMeshRef = capi.Render.UploadMesh(meshData);
	}

	private void InitCustomDataBuffers(MeshData modeldata)
	{
		modeldata.CustomShorts = new CustomMeshDataPartShort
		{
			StaticDraw = false,
			Instanced = true,
			InterleaveSizes = new int[8] { 2, 4, 1, 1, 1, 1, 1, 1 },
			InterleaveOffsets = new int[8] { 0, 4, 12, 14, 16, 18, 20, 22 },
			InterleaveStride = 24,
			Conversion = DataConversion.NormalizedFloat,
			Values = new short[QuantityCloudTiles * 12],
			Count = QuantityCloudTiles * 12
		};
	}

	private void UpdateBufferContents(MeshData mesh)
	{
		int num = 0;
		for (int i = 0; i < Tiles.Length; i++)
		{
			CloudTile cloudTile = Tiles[i];
			mesh.CustomShorts.Values[num++] = (short)(base.CloudTileSize * cloudTile.GridXOffset);
			mesh.CustomShorts.Values[num++] = (short)(base.CloudTileSize * cloudTile.GridZOffset);
			mesh.CustomShorts.Values[num++] = cloudTile.NorthTileThickness;
			mesh.CustomShorts.Values[num++] = cloudTile.EastTileThickness;
			mesh.CustomShorts.Values[num++] = cloudTile.SouthTileThickness;
			mesh.CustomShorts.Values[num++] = cloudTile.WestTileThickness;
			mesh.CustomShorts.Values[num++] = cloudTile.SelfThickness;
			mesh.CustomShorts.Values[num++] = cloudTile.Brightness;
			mesh.CustomShorts.Values[num++] = cloudTile.ThinCloudMode;
			mesh.CustomShorts.Values[num++] = cloudTile.UndulatingCloudMode;
			mesh.CustomShorts.Values[num++] = cloudTile.CloudOpaqueness;
			mesh.CustomShorts.Values[num++] = 0;
		}
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(cloudTilesMeshRef);
	}
}
