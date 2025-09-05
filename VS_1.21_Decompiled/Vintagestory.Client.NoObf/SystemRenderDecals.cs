using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class SystemRenderDecals : ClientSystem
{
	private MeshDataPool decalPool;

	public MeshData[][] decalModeldatas;

	private LoadedTexture decalTextureAtlas;

	private int nextDecalId = 1;

	private Size2i decalAtlasSize = new Size2i(512, 512);

	internal static int decalPoolSize = 200;

	internal Dictionary<int, BlockDecal> decals = new Dictionary<int, BlockDecal>(decalPoolSize);

	internal TextureAtlasPosition[] DecalTextureAtlasPositionsByTextureSubId;

	internal Dictionary<string, int> TextureNameToIdMapping;

	private Vec3d decalOrigin = new Vec3d();

	private float[] floatpool = new float[4];

	private bool[] leavesWaveTileSide = new bool[6];

	public override string Name => "rede";

	public SystemRenderDecals(ClientMain game)
		: base(game)
	{
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("spawndecal").WithDescription("Spawn a decal at position")
			.HandleWith(OnSpawnDecal)
			.EndSubCommand();
		game.eventManager.OnPlayerBreakingBlock.Add(OnPlayerBreakingBlock);
		game.eventManager.OnUnBreakingBlock.Add(OnUnBreakingBlock);
		game.eventManager.OnPlayerBrokenBlock.Add(OnPlayerBrokenBlock);
		game.RegisterGameTickListener(OnGameTick, 500);
		game.eventManager.OnBlockChanged.Add(OnBlockChanged);
		game.eventManager.OnReloadShapes += TesselateDecalsFromBlockShapes;
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.AfterOIT, "decals", 0.5);
	}

	public override void OnBlockTexturesLoaded()
	{
		InitAtlasAndModelPool();
		TesselateDecalsFromBlockShapes();
	}

	private void TesselateDecalsFromBlockShapes()
	{
		decalModeldatas = new MeshData[game.Blocks.Count][];
	}

	private void tesselateBlockDecal(int blockId)
	{
		Block block = game.Blocks[blockId];
		TextureSource textureSource = new TextureSource(game, decalAtlasSize, block);
		textureSource.isDecalUv = true;
		int num = ((block.Shape.BakedAlternates != null) ? block.Shape.BakedAlternates.Length : 0);
		try
		{
			if (num > 0)
			{
				decalModeldatas[block.BlockId] = new MeshData[block.Shape.BakedAlternates.Length];
				for (int i = 0; i < block.Shape.BakedAlternates.Length; i++)
				{
					game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape.BakedAlternates[i % num], out var modeldata, textureSource);
					addLod0Mesh(modeldata, block, textureSource, i);
					decalModeldatas[block.BlockId][i] = modeldata;
				}
			}
			else
			{
				game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape, out var modeldata2, textureSource);
				addLod0Mesh(modeldata2, block, textureSource, 0);
				decalModeldatas[block.BlockId] = new MeshData[1] { modeldata2 };
			}
			MeshData[] array = decalModeldatas[block.BlockId];
			foreach (MeshData mesh in array)
			{
				addZOffset(block, mesh);
			}
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block for decal system {0}. Will use invisible decal.", block);
			game.Platform.Logger.Error(e);
			decalModeldatas[block.BlockId] = new MeshData[1]
			{
				new MeshData(4, 6)
			};
		}
	}

	private void addZOffset(Block block, MeshData mesh)
	{
		int num = block.VertexFlags.ZOffset << 8;
		for (int i = 0; i < mesh.FlagsCount; i++)
		{
			mesh.Flags[i] |= num;
		}
	}

	private void addLod0Mesh(MeshData altModeldata, Block block, TextureSource texSource, int alternateIndex)
	{
		if (block.Lod0Shape != null)
		{
			game.TesselatorManager.Tesselator.TesselateBlock(block, block.Lod0Shape.BakedAlternates[alternateIndex], out var modeldata, texSource);
			altModeldata.AddMeshData(modeldata);
		}
	}

	private TextCommandResult OnSpawnDecal(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (game.BlockSelection != null)
		{
			AddBlockBreakDecal(game.BlockSelection.Position, 3);
		}
		return TextCommandResult.Success();
	}

	private void OnGameTick(float dt)
	{
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		if (decals.Count == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, BlockDecal> decal in decals)
		{
			if (decal.Value.pos.Equals(pos))
			{
				list.Add(decal.Key);
			}
		}
		foreach (int item in list)
		{
			BlockDecal blockDecal = decals[item];
			if (blockDecal.PoolLocation != null)
			{
				decalPool.RemoveLocation(blockDecal.PoolLocation);
			}
			blockDecal.PoolLocation = null;
			decals.Remove(item);
		}
	}

	private void OnPlayerBrokenBlock(BlockDamage blockDamage)
	{
		if (blockDamage.DecalId != 0)
		{
			decals.TryGetValue(blockDamage.DecalId, out var value);
			if (value != null && value.PoolLocation != null)
			{
				decalPool.RemoveLocation(value.PoolLocation);
				value.PoolLocation = null;
			}
			decals.Remove(blockDamage.DecalId);
		}
	}

	private void OnUnBreakingBlock(BlockDamage blockDamage)
	{
		if (blockDamage.DecalId != 0)
		{
			if (blockDamage.RemainingResistance >= blockDamage.Block.GetResistance(game.BlockAccessor, blockDamage.Position))
			{
				OnPlayerBrokenBlock(blockDamage);
			}
			else
			{
				OnPlayerBreakingBlock(blockDamage);
			}
		}
	}

	private void OnPlayerBreakingBlock(BlockDamage blockDamage)
	{
		float resistance = blockDamage.Block.GetResistance(game.BlockAccessor, blockDamage.Position);
		if (blockDamage.RemainingResistance == resistance)
		{
			return;
		}
		if (blockDamage.DecalId == 0 || !decals.ContainsKey(blockDamage.DecalId))
		{
			BlockDecal blockDecal = AddBlockBreakDecal(blockDamage.Position, 0);
			if (blockDecal != null)
			{
				blockDamage.DecalId = blockDecal.DecalId;
			}
			return;
		}
		BlockDecal blockDecal2 = decals[blockDamage.DecalId];
		int num = 10;
		int animationStage = blockDecal2.AnimationStage;
		int val = (int)((float)num * (resistance - blockDamage.RemainingResistance) / resistance);
		blockDecal2.AnimationStage = GameMath.Clamp(val, 1, num - 1);
		blockDecal2.LastModifiedMilliseconds = game.ElapsedMilliseconds;
		if (animationStage != blockDecal2.AnimationStage)
		{
			UpdateDecal(blockDecal2);
		}
	}

	internal BlockDecal AddBlockBreakDecal(BlockPos pos, int stage)
	{
		BlockDecal blockDecal = new BlockDecal
		{
			AnimationStage = stage,
			DecalId = nextDecalId++,
			pos = pos.Copy(),
			LastModifiedMilliseconds = game.ElapsedMilliseconds
		};
		if (UpdateDecal(blockDecal))
		{
			decals.Add(blockDecal.DecalId, blockDecal);
			return blockDecal;
		}
		return null;
	}

	internal bool UpdateDecal(BlockDecal decal)
	{
		if (decal.PoolLocation != null)
		{
			decalPool.RemoveLocation(decal.PoolLocation);
		}
		TextureNameToIdMapping.TryGetValue("destroy_stage_" + decal.AnimationStage + ".png", out var value);
		Block block = game.WorldMap.RelaxedBlockAccess.GetBlock(decal.pos);
		if (block.BlockId == 0)
		{
			decal.PoolLocation = null;
			decals.Remove(decal.DecalId);
			return false;
		}
		MeshData blockModelData = game.TesselatorManager.GetDefaultBlockMesh(block);
		if (decalModeldatas[block.BlockId] == null)
		{
			tesselateBlockDecal(block.BlockId);
		}
		MeshData decalModelData;
		if (block.HasAlternates)
		{
			int k = GameMath.MurmurHash3(decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z);
			int num = GameMath.Mod(k, decalModeldatas[block.BlockId].Length);
			decalModelData = decalModeldatas[block.BlockId][num].Clone();
			int num2 = GameMath.Mod(k, game.TesselatorManager.altblockModelDatasLod1[block.BlockId].Length);
			blockModelData = game.TesselatorManager.altblockModelDatasLod1[block.BlockId][num2];
			if (block.Lod0Shape != null)
			{
				blockModelData = blockModelData.Clone();
				blockModelData.AddMeshData(game.TesselatorManager.altblockModelDatasLod0[block.BlockId][num2]);
			}
		}
		else
		{
			decalModelData = decalModeldatas[block.BlockId][0].Clone();
			if (block.Lod0Shape != null)
			{
				blockModelData = blockModelData.Clone();
				blockModelData.AddMeshData(block.Lod0Mesh);
			}
		}
		TextureSource textureSource = new TextureSource(game, decalAtlasSize, block);
		textureSource.isDecalUv = true;
		block.GetDecal(game, decal.pos, textureSource, ref decalModelData, ref blockModelData);
		decalModelData.CustomFloats = new CustomMeshDataPartFloat(4 * decalModelData.VerticesCount)
		{
			InterleaveSizes = new int[3] { 2, 2, 2 },
			InterleaveStride = 24,
			InterleaveOffsets = new int[3] { 0, 8, 16 }
		};
		if (decalModelData.VerticesCount == 0)
		{
			decal.PoolLocation = null;
			return false;
		}
		double num3 = 0.0;
		double num4 = 0.0;
		if (block.RandomDrawOffset != 0)
		{
			num3 = (float)(GameMath.oaatHash(decal.pos.X, 0, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
			num4 = (float)(GameMath.oaatHash(decal.pos.X, 1, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
		}
		if (block.RandomizeRotations)
		{
			int num5 = GameMath.MurmurHash3Mod(-decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z, TesselationMetaData.randomRotations.Length);
			decalModelData = decalModelData.MatrixTransform(TesselationMetaData.randomRotMatrices[num5], floatpool);
		}
		int lightRGBsAsInt = game.WorldMap.GetLightRGBsAsInt(decal.pos.X, decal.pos.Y, decal.pos.Z);
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			Block blockOnSide = game.BlockAccessor.GetBlockOnSide(decal.pos, blockFacing);
			leavesWaveTileSide[i] = !blockOnSide.SideSolid[blockFacing.Opposite.Index] || blockOnSide.BlockMaterial == EnumBlockMaterial.Leaves;
		}
		byte b = (byte)(lightRGBsAsInt >> 24);
		byte b2 = (byte)(lightRGBsAsInt >> 16);
		byte b3 = (byte)(lightRGBsAsInt >> 8);
		byte b4 = (byte)lightRGBsAsInt;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		for (int j = 0; j < decalModelData.VerticesCount; j++)
		{
			TextureAtlasPosition textureAtlasPosition = DecalTextureAtlasPositionsByTextureSubId[value];
			decalModelData.Uv[num6] = GameMath.Clamp(decalModelData.Uv[num6] + textureAtlasPosition.x1, 0f, 1f);
			num6++;
			decalModelData.Uv[num6] = GameMath.Clamp(decalModelData.Uv[num6] + textureAtlasPosition.y1, 0f, 1f);
			num6++;
			if (blockModelData.UvCount > 2 * j + 1)
			{
				decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j]);
				decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j + 1]);
			}
			decalModelData.CustomFloats.Add(textureAtlasPosition.x2 - textureAtlasPosition.x1);
			decalModelData.CustomFloats.Add(textureAtlasPosition.y2 - textureAtlasPosition.y1);
			decalModelData.CustomFloats.Add(textureAtlasPosition.x1);
			decalModelData.CustomFloats.Add(textureAtlasPosition.y1);
			decalModelData.Rgba[num7++] = b2;
			decalModelData.Rgba[num7++] = b3;
			decalModelData.Rgba[num7++] = b4;
			decalModelData.Rgba[num7++] = b;
			decalModelData.Flags[j] = decalModelData.Flags[j];
		}
		block.OnDecalTesselation(game, decalModelData, decal.pos);
		for (int l = 0; l < decalModelData.VerticesCount; l++)
		{
			decalModelData.xyz[num8++] += (float)((double)decal.pos.X + num3 - decalOrigin.X);
			decalModelData.xyz[num8++] += (float)((double)decal.pos.Y - decalOrigin.Y);
			decalModelData.xyz[num8++] += (float)((double)decal.pos.Z + num4 - decalOrigin.Z);
		}
		Sphere frustumCullSphere = Sphere.BoundingSphereForCube(decal.pos.X, decal.pos.Y, decal.pos.Z, 1f);
		if ((decal.PoolLocation = decalPool.TryAdd(game.api, decalModelData, null, 0, frustumCullSphere)) == null)
		{
			return false;
		}
		return true;
	}

	internal void UpdateAllDecals()
	{
		foreach (BlockDecal item in new List<BlockDecal>(decals.Values))
		{
			UpdateDecal(item);
		}
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		if (decalOrigin.SquareDistanceTo(cameraPos) > 1000000f)
		{
			decalOrigin = cameraPos.Clone();
			UpdateAllDecals();
		}
		if (decals.Count > 0)
		{
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			game.Platform.GlToggleBlend(on: true);
			game.Platform.GlDisableCullFace();
			ShaderProgramDecals shaderProgramDecals = ShaderPrograms.Decals;
			shaderProgramDecals.Use();
			shaderProgramDecals.WindWaveCounter = game.shUniforms.WindWaveCounter;
			shaderProgramDecals.WindWaveCounterHighFreq = game.shUniforms.WindWaveCounterHighFreq;
			shaderProgramDecals.BlockTexture2D = game.BlockAtlasManager.AtlasTextures[0].TextureId;
			shaderProgramDecals.DecalTexture2D = decalTextureAtlas.TextureId;
			shaderProgramDecals.RgbaFogIn = game.AmbientManager.BlendedFogColor;
			shaderProgramDecals.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
			shaderProgramDecals.FogDensityIn = game.AmbientManager.BlendedFogDensity;
			shaderProgramDecals.FogMinIn = game.AmbientManager.BlendedFogMin;
			shaderProgramDecals.Origin = new Vec3f((float)(decalOrigin.X - cameraPos.X), (float)(decalOrigin.Y - cameraPos.Y), (float)(decalOrigin.Z - cameraPos.Z));
			shaderProgramDecals.ProjectionMatrix = game.CurrentProjectionMatrix;
			shaderProgramDecals.ModelViewMatrix = game.CurrentModelViewMatrix;
			decalPool.Draw(game.api, game.frustumCuller, EnumFrustumCullMode.CullInstant);
			shaderProgramDecals.Stop();
			game.Platform.GlToggleBlend(on: true);
			game.Platform.GlEnableCullFace();
			game.GlPopMatrix();
		}
	}

	private void InitAtlasAndModelPool()
	{
		List<IAsset> manyInCategory = game.Platform.AssetManager.GetManyInCategory("textures", "decal/");
		int num = game.textureSize * (int)Math.Ceiling(Math.Sqrt(manyInCategory.Count));
		decalAtlasSize = new Size2i(num, num);
		game.Logger.Notification("Texture size is {0} so decal atlas size of {1}x{2} should suffice", game.textureSize, decalAtlasSize.Width, decalAtlasSize.Height);
		TextureAtlas textureAtlas = new TextureAtlas(decalAtlasSize.Width, decalAtlasSize.Height, 0f, 0f);
		DecalTextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[manyInCategory.Count];
		TextureNameToIdMapping = new Dictionary<string, int>();
		for (int i = 0; i < manyInCategory.Count; i++)
		{
			if (!textureAtlas.InsertTexture(i, game.api, manyInCategory[i]))
			{
				throw new Exception("Texture decal atlas overflow. Did you create a high res texture pack without setting the correct textureSize value in the modinfo.json?");
			}
			TextureNameToIdMapping[manyInCategory[i].Name] = i;
		}
		decalTextureAtlas = textureAtlas.Upload(game);
		game.Platform.BuildMipMaps(decalTextureAtlas.TextureId);
		textureAtlas.PopulateAtlasPositions(DecalTextureAtlasPositionsByTextureSubId, 0);
		int num2 = decalPoolSize * 24 * 10;
		CustomMeshDataPartFloat customMeshDataPartFloat = new CustomMeshDataPartFloat();
		customMeshDataPartFloat.Instanced = false;
		customMeshDataPartFloat.StaticDraw = true;
		customMeshDataPartFloat.InterleaveSizes = new int[3] { 2, 2, 2 };
		customMeshDataPartFloat.InterleaveStride = 24;
		customMeshDataPartFloat.InterleaveOffsets = new int[3] { 0, 8, 16 };
		customMeshDataPartFloat.Count = num2;
		CustomMeshDataPartFloat customFloats = customMeshDataPartFloat;
		decalPool = MeshDataPool.AllocateNewPool(game.api, num2, (int)((float)num2 * 1.5f), 2 * decalPoolSize, customFloats);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}

	public override void Dispose(ClientMain game)
	{
		decalPool?.Dispose(game.api);
		decalTextureAtlas?.Dispose();
	}
}
