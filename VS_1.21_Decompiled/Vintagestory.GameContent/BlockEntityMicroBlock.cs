using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityMicroBlock : BlockEntity, IRotatable, IAcceptsDecor, IMaterialExchangeable
{
	[StructLayout(LayoutKind.Sequential, Size = 8)]
	public struct VoxelInfo
	{
		public int Material;

		public ushort MainIndex;

		public byte Size;

		public bool CullFace;
	}

	public ref struct GenFaceInfo
	{
		public ICoreClientAPI capi;

		public MeshData targetMesh;

		public SizeConverter converter;

		public unsafe int* originalBounds;

		public int posPacked;

		public int width;

		public int length;

		public int face;

		public BlockFacing facing;

		public bool AnyFrostable;

		public float subPixelPaddingx;

		public float subPixelPaddingy;

		public int flags;

		private VoxelMaterial decorMat;

		private float uSize;

		private float vSize;

		private float uOffset;

		private float vOffset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetInfo(SizeConverter converter, int face, VoxelMaterial decorMat)
		{
			this.decorMat = decorMat;
			this.face = face;
			facing = BlockFacing.ALLFACES[face];
			this.converter = converter;
		}

		public unsafe void GenFace(in VoxelMaterial blockMat)
		{
			MeshData meshData = targetMesh;
			XYZ xYZ = default(XYZ);
			xYZ.Z = posPacked / 324;
			posPacked -= xYZ.Z * 324;
			xYZ.Y = posPacked / 18;
			posPacked -= xYZ.Y * 18;
			xYZ.Z--;
			xYZ.Y--;
			xYZ.X = posPacked - 1;
			converter(width, length, out var sx, out var sy, out var sz);
			float num = (float)xYZ.X * 0.0625f;
			float num2 = (float)xYZ.Y * 0.0625f;
			float num3 = (float)xYZ.Z * 0.0625f;
			float num4 = num + sx;
			float num5 = num2 + sy;
			float num6 = num3 + sz;
			int axis = (int)facing.Axis;
			TextureAtlasPosition textureAtlasPosition = ((originalBounds[face] == xYZ[axis] + shiftOffsetByFace[face]) ? blockMat.Texture[face] : blockMat.TextureInside[face]);
			TextureAtlasPosition textureTopSoil = blockMat.TextureTopSoil;
			TextureAtlasPosition[] texture = decorMat.Texture;
			TextureAtlasPosition textureAtlasPosition2 = ((texture != null) ? texture[face] : null) ?? null;
			uSize = 2f * ((axis == 0) ? sz : sx);
			vSize = 2f * ((axis == 1) ? sz : sy);
			uOffset = ((axis == 0) ? num3 : num);
			vOffset = ((axis == 1) ? num3 : num2);
			float num7 = textureAtlasPosition.x2 - textureAtlasPosition.x1;
			float num8 = textureAtlasPosition.y2 - textureAtlasPosition.y1;
			float num9 = textureAtlasPosition.x1 - subPixelPaddingx;
			float num10 = textureAtlasPosition.y1 - subPixelPaddingy;
			int num11 = 256;
			int[] cubeVertices = CubeMeshUtil.CubeVertices;
			int num12 = ((meshData.IndicesCount > 0) ? (meshData.Indices[meshData.IndicesCount - 1] + 1) : 0);
			int num13 = face * 4;
			for (int i = 0; i < 4; i++)
			{
				int num14 = num13 + i;
				GetRelativeUV(num14 * 2, out var u, out var v);
				meshData.AddVertexWithFlagsSkipColor((float)cubeVertices[num14 * 3] * sx + num4, (float)cubeVertices[num14 * 3 + 1] * sy + num5, (float)cubeVertices[num14 * 3 + 2] * sz + num6, num9 + u * num7, num10 + v * num8, flags);
				if (meshData.CustomShorts != null)
				{
					if (textureTopSoil == null)
					{
						meshData.CustomShorts.Add(default(short), default(short));
					}
					else
					{
						meshData.CustomShorts.AddPackedUV(textureTopSoil.x1 + u * num7, textureTopSoil.y1 + v * num8);
					}
				}
			}
			int num15 = face * 6;
			for (int j = 0; j < 6; j++)
			{
				meshData.AddIndex(num12 + CubeMeshUtil.CubeVertexIndices[num15 + j] - num13);
			}
			meshData.AddXyzFace(facing.MeshDataIndex);
			meshData.AddTextureId(textureAtlasPosition.atlasTextureId);
			if (AnyFrostable)
			{
				meshData.AddColorMapIndex(blockMat.ClimateMapIndex, blockMat.SeasonMapIndex, blockMat.Frostable);
			}
			else
			{
				meshData.AddColorMapIndex(blockMat.ClimateMapIndex, blockMat.SeasonMapIndex);
			}
			meshData.AddRenderPass((short)blockMat.RenderPass);
			if (textureAtlasPosition2 == null)
			{
				return;
			}
			num12 = ((meshData.IndicesCount > 0) ? (meshData.Indices[meshData.IndicesCount - 1] + 1) : 0);
			num13 = face * 4;
			num7 = textureAtlasPosition2.x2 - textureAtlasPosition2.x1;
			num8 = textureAtlasPosition2.y2 - textureAtlasPosition2.y1;
			num9 = textureAtlasPosition2.x1 - subPixelPaddingx;
			num10 = textureAtlasPosition2.y1 - subPixelPaddingy;
			Vec3i vec3i = BlockFacing.ALLNORMALI[face];
			float num16 = (1f + Math.Abs((float)vec3i.X * 0.01f)) * sx;
			float num17 = (1f + Math.Abs((float)vec3i.Y * 0.01f)) * sy;
			float num18 = (1f + Math.Abs((float)vec3i.Z * 0.01f)) * sz;
			int textureRotation = decorMat.TextureRotation;
			for (int k = 0; k < 4; k++)
			{
				int num19 = num13 + k;
				GetRelativeUV(num19 * 2, out var u2, out var v2);
				if ((textureRotation & 4) == 0)
				{
					u2 = 1f - u2;
				}
				switch (textureRotation % 8)
				{
				case 3:
				case 5:
				{
					float num20 = v2;
					v2 = 1f - u2;
					u2 = num20;
					break;
				}
				case 2:
				case 6:
					u2 = 1f - u2;
					v2 = 1f - v2;
					break;
				case 1:
				case 7:
				{
					float num20 = v2;
					v2 = u2;
					u2 = 1f - num20;
					break;
				}
				}
				meshData.AddVertexWithFlagsSkipColor((float)cubeVertices[num19 * 3] * num16 + num4, (float)cubeVertices[num19 * 3 + 1] * num17 + num5, (float)cubeVertices[num19 * 3 + 2] * num18 + num6, num9 + u2 * num7, num10 + v2 * num8, flags | num11);
			}
			if (meshData.CustomShorts != null)
			{
				meshData.CustomShorts.Add(default(short), default(short), default(short), default(short), default(short), default(short), default(short), default(short));
			}
			num15 = face * 6;
			for (int l = 0; l < 6; l++)
			{
				meshData.AddIndex(num12 + CubeMeshUtil.CubeVertexIndices[num15 + l] - num13);
			}
			meshData.AddXyzFace(facing.MeshDataIndex);
			meshData.AddTextureId(textureAtlasPosition2.atlasTextureId);
			meshData.AddColorMapIndex(decorMat.ClimateMapIndex, decorMat.SeasonMapIndex);
			meshData.AddRenderPass((short)decorMat.RenderPass);
		}

		private void GetRelativeUV(int vIndex, out float u, out float v)
		{
			float num = CubeMeshUtil.CubeUvCoords[vIndex];
			float num2 = CubeMeshUtil.CubeUvCoords[vIndex + 1];
			switch (facing.Index)
			{
			case 0:
				u = (num - 1f) * uSize + 1f - uOffset;
				v = (0f - num2) * vSize + 1f - vOffset;
				break;
			case 1:
				u = (num - 1f) * uSize + 1f - uOffset;
				v = (0f - num2) * vSize + 1f - vOffset;
				break;
			case 2:
				u = num * uSize + uOffset;
				v = (0f - num2) * vSize + 1f - vOffset;
				break;
			case 3:
				u = num * uSize + uOffset;
				v = (0f - num2) * vSize + 1f - vOffset;
				break;
			case 4:
				u = (0f - num) * uSize + 1f - uOffset;
				v = (num2 - 1f) * vSize + 1f - vOffset;
				break;
			case 5:
				u = (num - 1f) * uSize + 1f - uOffset;
				v = (1f - num2) * vSize + vOffset;
				break;
			default:
				throw new Exception();
			}
		}
	}

	public ref struct GenPlaneInfo
	{
		public RefList<VoxelMaterial> blockMaterials;

		public RefList<VoxelMaterial> decorMaterials;

		public unsafe VoxelInfo* voxels;

		public int materialIndex;

		public int fromA;

		public int toA;

		public int fromB;

		public int toB;

		public int c;

		public int stepA;

		public int stepB;

		public int faceOffsetZ;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetCoords(int fromA, int toA, int stepA, int fromB, int toY, int stepY, int c, int faceOffsetZ)
		{
			this.fromA = fromA;
			this.toA = toA;
			this.stepA = stepA;
			this.fromB = fromB;
			toB = toY;
			stepB = stepY;
			this.c = c;
			this.faceOffsetZ = faceOffsetZ;
		}

		public unsafe void GenPlaneMesh(ref GenFaceInfo faceGenInfo)
		{
			for (int i = fromA; i < toA; i += stepA)
			{
				int num = 1;
				int num2 = i + fromB + c;
				bool flag = isMergableMaterial(materialIndex, voxels[i + fromB + faceOffsetZ].Material, blockMaterials);
				for (int j = fromB + stepB; j < toB; j += stepB)
				{
					if (isMergableMaterial(materialIndex, voxels[i + j + faceOffsetZ].Material, blockMaterials) == flag)
					{
						num++;
						voxels[i + j + c].MainIndex = (ushort)num2;
						continue;
					}
					voxels[num2].Size = (byte)num;
					voxels[num2].CullFace = flag;
					voxels[num2].MainIndex = (ushort)num2;
					num = 1;
					num2 = i + j + c;
					flag = !flag;
				}
				voxels[num2].Size = (byte)num;
				voxels[num2].CullFace = flag;
				voxels[num2].MainIndex = (ushort)num2;
			}
			ref VoxelMaterial reference = ref blockMaterials[materialIndex];
			if (reference.BlockId == 0)
			{
				return;
			}
			faceGenInfo.flags = faceGenInfo.facing.NormalPackedFlags | reference.Flags;
			for (int j = fromB; j < toB; j += stepB)
			{
				int num = 1;
				int num2 = fromA + j + c;
				ref VoxelInfo reference2 = ref voxels[num2];
				int size = reference2.Size;
				int posPacked = num2;
				bool flag = reference2.CullFace;
				bool flag2 = reference2.MainIndex != num2;
				for (int i = fromA + stepA; i < toA; i += stepA)
				{
					num2 = i + j + c;
					reference2 = ref voxels[num2];
					if (reference2.MainIndex != num2)
					{
						if (flag2)
						{
							continue;
						}
						flag2 = true;
					}
					else
					{
						if (flag2)
						{
							flag2 = false;
							num = 1;
							posPacked = num2;
							size = reference2.Size;
							flag = reference2.CullFace;
							continue;
						}
						if (flag == reference2.CullFace && size == reference2.Size)
						{
							num++;
							continue;
						}
					}
					if (!flag)
					{
						faceGenInfo.posPacked = posPacked;
						faceGenInfo.width = num;
						faceGenInfo.length = size;
						faceGenInfo.GenFace(in reference);
					}
					num = 1;
					posPacked = num2;
					size = reference2.Size;
					flag = reference2.CullFace;
				}
				if (!flag && !flag2)
				{
					faceGenInfo.posPacked = posPacked;
					faceGenInfo.width = num;
					faceGenInfo.length = size;
					faceGenInfo.GenFace(in reference);
				}
			}
		}
	}

	public delegate void SizeConverter(int width, int height, out float sx, out float sy, out float sz);

	public readonly struct VoxelMaterial
	{
		public readonly int BlockId;

		public readonly TextureAtlasPosition[] Texture;

		public readonly TextureAtlasPosition[] TextureInside;

		public readonly TextureAtlasPosition TextureTopSoil;

		public readonly EnumChunkRenderPass RenderPass;

		public readonly int Flags;

		public readonly bool CullBetweenTransparents;

		public readonly byte ClimateMapIndex;

		public readonly bool Frostable;

		public readonly byte SeasonMapIndex;

		public readonly int TextureRotation;

		public VoxelMaterial(int blockId, TextureAtlasPosition[] texture, TextureAtlasPosition[] textureInside, TextureAtlasPosition textureTopSoil, EnumChunkRenderPass renderPass, int flags, byte climateMapIndex, byte seasonMapIndex, bool frostable, bool cullBetweenTransparents, int textureRotation)
		{
			ClimateMapIndex = climateMapIndex;
			SeasonMapIndex = seasonMapIndex;
			BlockId = blockId;
			Texture = texture;
			TextureInside = textureInside;
			RenderPass = renderPass;
			Frostable = frostable;
			Flags = flags;
			TextureTopSoil = textureTopSoil;
			CullBetweenTransparents = cullBetweenTransparents;
			TextureRotation = textureRotation;
		}

		public VoxelMaterial(int blockId, TextureAtlasPosition[] texture, TextureAtlasPosition[] textureInside, TextureAtlasPosition textureTopSoil, EnumChunkRenderPass renderPass, int flags, byte climateMapIndex, byte seasonMapIndex, bool frostable, bool cullBetweenTransparents)
			: this(blockId, texture, textureInside, textureTopSoil, renderPass, flags, climateMapIndex, seasonMapIndex, frostable, cullBetweenTransparents, 0)
		{
		}

		public static VoxelMaterial FromBlock(ICoreClientAPI capi, Block block, BlockPos posForRnd = null, bool cullBetweenTransparents = false, int decorRotation = 0)
		{
			int altTextureNumber = 0;
			if (block.HasAlternates && posForRnd != null)
			{
				int num = 0;
				foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
				{
					BakedCompositeTexture baked = texture.Value.Baked;
					if (baked.BakedVariants != null)
					{
						num = Math.Max(num, baked.BakedVariants.Length);
					}
				}
				if (num > 0)
				{
					altTextureNumber = ((block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? GameMath.MurmurHash3Mod(posForRnd.X, posForRnd.Y, posForRnd.Z, num) : GameMath.MurmurHash3Mod(posForRnd.X, 0, posForRnd.Z, num));
				}
			}
			TextureAtlasPosition[] array = new TextureAtlasPosition[6];
			TextureAtlasPosition[] array2 = new TextureAtlasPosition[6];
			TextureAtlasPosition textureAtlasPosition = null;
			ITexPositionSource textureSource = capi.Tesselator.GetTextureSource(block, altTextureNumber, returnNullWhenMissing: true);
			BlockFacing blockFacing;
			for (int i = 0; i < 6; array2[i] = textureSource["inside-" + blockFacing.Code] ?? array[i], i++)
			{
				blockFacing = BlockFacing.ALLFACES[i];
				if (block.HasTiles)
				{
					BakedCompositeTexture[] array3 = block.FastTextureVariants[i];
					if (array3 != null && posForRnd != null)
					{
						int tiledTexturesSelector = BakedCompositeTexture.GetTiledTexturesSelector(array3, i, posForRnd.X, posForRnd.Y, posForRnd.Z);
						int textureSubId = array3[GameMath.Mod(tiledTexturesSelector, array3.Length)].TextureSubId;
						array[i] = ((textureSubId >= 0) ? capi.BlockTextureAtlas.Positions[textureSubId] : capi.BlockTextureAtlas.UnknownTexturePosition);
						continue;
					}
				}
				if ((textureSource[blockFacing.Code] == null || textureSource["inside-" + blockFacing.Code] == null) && textureAtlasPosition == null)
				{
					textureAtlasPosition = capi.BlockTextureAtlas.UnknownTexturePosition;
					if (block.Textures.Count > 0)
					{
						textureAtlasPosition = textureSource[block.Textures.First().Key] ?? capi.BlockTextureAtlas.UnknownTexturePosition;
					}
				}
				array[i] = textureSource[blockFacing.Code] ?? textureAtlasPosition;
			}
			byte climateMapIndex = (byte)((block.ClimateColorMapResolved != null) ? ((byte)(block.ClimateColorMapResolved.RectIndex + 1)) : 0);
			byte seasonMapIndex = (byte)((block.SeasonColorMapResolved != null) ? ((byte)(block.SeasonColorMapResolved.RectIndex + 1)) : 0);
			TextureAtlasPosition textureTopSoil = null;
			if (block.RenderPass == EnumChunkRenderPass.TopSoil)
			{
				textureTopSoil = capi.BlockTextureAtlas[block.Textures["specialSecondTexture"].Baked.BakedName];
			}
			return new VoxelMaterial(block.Id, array, array2, textureTopSoil, block.RenderPass, block.VertexFlags.All, climateMapIndex, seasonMapIndex, block.Frostable, cullBetweenTransparents, decorRotation);
		}

		public static VoxelMaterial FromTexSource(ICoreClientAPI capi, ITexPositionSource texSource, bool cullBetweenTransparents = false)
		{
			TextureAtlasPosition[] array = new TextureAtlasPosition[6];
			TextureAtlasPosition[] array2 = new TextureAtlasPosition[6];
			for (int i = 0; i < 6; i++)
			{
				BlockFacing blockFacing = BlockFacing.ALLFACES[i];
				array[i] = texSource[blockFacing.Code] ?? capi.BlockTextureAtlas.UnknownTexturePosition;
				array2[i] = texSource["inside-" + blockFacing.Code] ?? texSource[blockFacing.Code] ?? capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			return new VoxelMaterial(0, array, array2, null, EnumChunkRenderPass.Opaque, 0, 0, 0, frostable: false, cullBetweenTransparents, 0);
		}
	}

	protected static ThreadLocal<CuboidWithMaterial[]> tmpCuboidTL = new ThreadLocal<CuboidWithMaterial[]>(delegate
	{
		CuboidWithMaterial[] array = new CuboidWithMaterial[4096];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new CuboidWithMaterial();
		}
		return array;
	});

	private static Cuboidf[] noSelectionBox = Array.Empty<Cuboidf>();

	private static byte[] singleByte255 = new byte[1] { 255 };

	private static Vec3f centerBase = new Vec3f(0.5f, 0f, 0.5f);

	public List<uint> VoxelCuboids = new List<uint>();

	protected uint[] originalCuboids;

	public int[] BlockIds;

	public int[] DecorIds;

	public int DecorRotations;

	protected int[] BlockIdsRotated;

	protected int[] DecorIdsRotated;

	public int rotated;

	public MeshData Mesh;

	protected Cuboidf[] selectionBoxesMetaMode;

	protected Cuboidf[] selectionBoxesStd = noSelectionBox;

	protected Cuboidf[] selectionBoxesVoxels = noSelectionBox;

	protected int prevSize = -1;

	protected int emitSideAo = 63;

	protected bool absorbAnyLight;

	public SmallBoolArray sidecenterSolid = new SmallBoolArray(0);

	public SmallBoolArray sideAlmostSolid = new SmallBoolArray(0);

	protected short rotationY;

	public float sizeRel = 1f;

	protected int totalVoxels;

	protected bool withColorMapData;

	public const int EXT_VOXELS_PER_SIDE = 18;

	public const int EXT_VOXELS_SQ = 324;

	[ThreadStatic]
	public static VoxelInfo[] tmpVoxels;

	[ThreadStatic]
	public static RefList<VoxelMaterial> tmpBlockMaterials;

	[ThreadStatic]
	public static RefList<VoxelMaterial> tmpDecorMaterials;

	private static readonly SizeConverter ConvertPlaneX = ConvertPlaneXImpl;

	private static readonly SizeConverter ConvertPlaneY = ConvertPlaneYImpl;

	private static readonly SizeConverter ConvertPlaneZ = ConvertPlaneZImpl;

	private static readonly int[] shiftOffsetByFace = new int[6] { 0, 1, 1, 0, 1, 0 };

	private static VoxelMaterial noMat = default(VoxelMaterial);

	private const int clearMaterialMask = 16777215;

	protected static CuboidWithMaterial[] tmpCuboids => tmpCuboidTL.Value;

	protected static uint[] defaultOriginalVoxelCuboids => new uint[1] { ToUint(0, 0, 0, 16, 16, 16, 0) };

	public uint[] OriginalVoxelCuboids
	{
		get
		{
			if (originalCuboids != null)
			{
				return originalCuboids;
			}
			return defaultOriginalVoxelCuboids;
		}
	}

	[Obsolete("Use BlockIds instead")]
	public int[] MaterialIds => BlockIds;

	public string BlockName { get; set; } = "";

	public float VolumeRel => (float)totalVoxels / 4096f;

	public Shape GenShape()
	{
		Shape shape = new Shape();
		CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
		shape.Elements = new ShapeElement[VoxelCuboids.Count];
		shape.Textures = new Dictionary<string, AssetLocation>();
		shape.TextureWidth = 16;
		shape.TextureHeight = 16;
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			FromUint(VoxelCuboids[i], cuboidWithMaterial);
			ShapeElement shapeElement = new ShapeElement();
			shapeElement.Name = "Cuboid" + i;
			shapeElement.From = new double[3] { cuboidWithMaterial.X1, cuboidWithMaterial.Y1, cuboidWithMaterial.Z1 };
			shapeElement.To = new double[3] { cuboidWithMaterial.X2, cuboidWithMaterial.Y2, cuboidWithMaterial.Z2 };
			Block block = Api.World.Blocks[BlockIds[cuboidWithMaterial.Material]];
			shapeElement.Faces = new Dictionary<string, ShapeElementFace>();
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				string text = block.Code.ToShortString();
				if (!block.Textures.TryGetValue(blockFacing.Code, out var value))
				{
					block.Textures.TryGetValue("all", out value);
				}
				shape.Textures[text] = value.Base.Path.Replace("*", "1");
				shapeElement.Faces[blockFacing.Code] = new ShapeElementFace
				{
					Texture = text,
					Uv = new float[4] { 0f, 0f, 16f, 16f }
				};
			}
			shape.Elements[i] = shapeElement;
		}
		return shape;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
	}

	public virtual void WasPlaced(Block block, string blockName)
	{
		bool flag = block.Attributes?.IsTrue("chiselShapeFromCollisionBox") ?? false;
		BlockIds = new int[1] { block.BlockId };
		if (!flag)
		{
			VoxelCuboids.Add(ToUint(0, 0, 0, 16, 16, 16, 0));
		}
		else
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
			originalCuboids = new uint[collisionBoxes.Length];
			for (int i = 0; i < collisionBoxes.Length; i++)
			{
				Cuboidf cuboidf = collisionBoxes[i];
				uint num = ToUint((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1), (int)(16f * cuboidf.X2), (int)(16f * cuboidf.Y2), (int)(16f * cuboidf.Z2), 0);
				VoxelCuboids.Add(num);
				originalCuboids[i] = num;
			}
		}
		BlockName = blockName;
		RebuildCuboidList();
		RegenSelectionBoxes(Api.World, null);
		if (Api.Side == EnumAppSide.Client && Mesh == null)
		{
			MarkMeshDirty();
		}
	}

	public override void OnBlockRemoved()
	{
		UpdateNeighbors(this);
		base.OnBlockRemoved();
	}

	public byte[] GetLightHsv(IBlockAccessor ba)
	{
		int[] blockIds = BlockIds;
		byte[] array = new byte[3];
		int num = 0;
		if (blockIds == null)
		{
			return array;
		}
		for (int i = 0; i < blockIds.Length; i++)
		{
			Block block = ba.GetBlock(blockIds[i]);
			if (block != null && block.LightHsv[2] > 0)
			{
				array[0] += block.LightHsv[0];
				array[1] += block.LightHsv[1];
				array[2] += block.LightHsv[2];
				num++;
			}
		}
		if (num == 0)
		{
			return array;
		}
		array[0] = (byte)(array[0] / num);
		array[1] = (byte)(array[1] / num);
		array[2] = (byte)(array[2] / num);
		return array;
	}

	public BlockSounds GetSounds()
	{
		MicroBlockSounds value = (base.Block as BlockMicroBlock).MBSounds.Value;
		value.Init(this, base.Block);
		return value;
	}

	public int GetLightAbsorption()
	{
		if (BlockIds == null || !absorbAnyLight || Api == null)
		{
			return 0;
		}
		int num = 99;
		for (int i = 0; i < BlockIds.Length; i++)
		{
			Block block = Api.World.GetBlock(BlockIds[i]);
			num = Math.Min(num, block.LightAbsorption);
		}
		return num;
	}

	public bool CanAttachBlockAt(BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (attachmentArea == null)
		{
			return sidecenterSolid[blockFace.Index];
		}
		HashSet<XYZ> hashSet = new HashSet<XYZ>();
		for (int i = attachmentArea.X1; i <= attachmentArea.X2; i++)
		{
			for (int j = attachmentArea.Y1; j <= attachmentArea.Y2; j++)
			{
				for (int k = attachmentArea.Z1; k <= attachmentArea.Z2; k++)
				{
					hashSet.Add(blockFace.Index switch
					{
						0 => new XYZ(i, j, 0), 
						1 => new XYZ(15, j, k), 
						2 => new XYZ(i, j, 15), 
						3 => new XYZ(0, j, k), 
						4 => new XYZ(i, 15, k), 
						5 => new XYZ(i, 0, k), 
						_ => new XYZ(0, 0, 0), 
					});
				}
			}
		}
		CuboidWithMaterial cuboidWithMaterial = tmpCuboids[0];
		for (int l = 0; l < VoxelCuboids.Count; l++)
		{
			FromUint(VoxelCuboids[l], cuboidWithMaterial);
			for (int m = cuboidWithMaterial.X1; m < cuboidWithMaterial.X2; m++)
			{
				for (int n = cuboidWithMaterial.Y1; n < cuboidWithMaterial.Y2; n++)
				{
					for (int num = cuboidWithMaterial.Z1; num < cuboidWithMaterial.Z2; num++)
					{
						if (m == 0 || m == 15 || n == 0 || n == 15 || num == 0 || num == 15)
						{
							hashSet.Remove(new XYZ(m, n, num));
						}
					}
				}
			}
		}
		return hashSet.Count == 0;
	}

	public virtual Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos, IPlayer forPlayer = null)
	{
		if (selectionBoxesMetaMode != null && Api.Side == EnumAppSide.Client && (Api as ICoreClientAPI).Settings.Bool["renderMetaBlocks"])
		{
			return selectionBoxesMetaMode;
		}
		if (selectionBoxesStd.Length == 0 && selectionBoxesMetaMode == null)
		{
			return new Cuboidf[1] { Cuboidf.Default() };
		}
		return selectionBoxesStd;
	}

	public Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (selectionBoxesMetaMode != null)
		{
			return selectionBoxesMetaMode;
		}
		return selectionBoxesStd;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		string blockName = BlockName;
		if (blockName != null && blockName.IndexOf('\n') > 0)
		{
			dsc.AppendLine(Lang.Get(BlockName.Substring(BlockName.IndexOf('\n') + 1)));
		}
		else if (forPlayer.Entity?.RightHandItemSlot?.Itemstack?.Collectible is ItemChisel)
		{
			dsc.AppendLine(Lang.Get("block-chiseledblock"));
		}
		if (forPlayer?.CurrentBlockSelection?.Face != null && BlockIds != null)
		{
			EnumBlockMaterial blockMaterial = Api.World.GetBlock(BlockIds[0]).BlockMaterial;
			if ((blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic) && (sideAlmostSolid[forPlayer.CurrentBlockSelection.Face.Index] || (sideAlmostSolid[forPlayer.CurrentBlockSelection.Face.Opposite.Index] && VolumeRel >= 0.5f)))
			{
				dsc.AppendLine(Lang.Get("Insulating block face"));
			}
		}
	}

	public string GetPlacedBlockName()
	{
		return GetPlacedBlockName(Api, VoxelCuboids, BlockIds, BlockName);
	}

	public static string GetPlacedBlockName(ICoreAPI api, List<uint> voxelCuboids, int[] blockIds, string blockName)
	{
		if ((blockName == null || blockName == "") && blockIds != null)
		{
			int majorityMaterial = getMajorityMaterial(voxelCuboids, blockIds);
			Block block = api.World.Blocks[majorityMaterial];
			return block.GetHeldItemName(new ItemStack(block));
		}
		int num = blockName.IndexOf('\n');
		return Lang.Get((num > 0) ? blockName.Substring(0, num) : blockName);
	}

	public int GetMajorityMaterialId(ActionBoolReturn<int> filterblockId = null)
	{
		return getMajorityMaterial(VoxelCuboids, BlockIds, filterblockId);
	}

	public static int getMajorityMaterial(List<uint> voxelCuboids, int[] blockIds, ActionBoolReturn<int> filterblockId = null)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
		for (int i = 0; i < voxelCuboids.Count; i++)
		{
			FromUint(voxelCuboids[i], cuboidWithMaterial);
			if (blockIds.Length > cuboidWithMaterial.Material)
			{
				int key = blockIds[cuboidWithMaterial.Material];
				if (dictionary.ContainsKey(key))
				{
					dictionary[key] += cuboidWithMaterial.SizeXYZ;
				}
				else
				{
					dictionary[key] = cuboidWithMaterial.SizeXYZ;
				}
			}
		}
		if (dictionary.Count == 0)
		{
			return 0;
		}
		if (filterblockId != null)
		{
			dictionary = dictionary.Where((KeyValuePair<int, int> vbb) => filterblockId?.Invoke(vbb.Key) ?? false).ToDictionary((KeyValuePair<int, int> kv) => kv.Key, (KeyValuePair<int, int> kv) => kv.Value);
		}
		if (dictionary.Count == 0)
		{
			return 0;
		}
		return dictionary.MaxBy((KeyValuePair<int, int> vbb) => vbb.Value).Key;
	}

	public void ConvertToVoxels(out BoolArray16x16x16 voxels, out byte[,,] materials)
	{
		voxels = new BoolArray16x16x16();
		materials = new byte[16, 16, 16];
		CuboidWithMaterial cuboidWithMaterial = tmpCuboids[0];
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			FromUint(VoxelCuboids[i], cuboidWithMaterial);
			for (int j = cuboidWithMaterial.X1; j < cuboidWithMaterial.X2; j++)
			{
				for (int k = cuboidWithMaterial.Y1; k < cuboidWithMaterial.Y2; k++)
				{
					for (int l = cuboidWithMaterial.Z1; l < cuboidWithMaterial.Z2; l++)
					{
						voxels[j, k, l] = true;
						materials[j, k, l] = cuboidWithMaterial.Material;
					}
				}
			}
		}
	}

	public void RebuildCuboidList()
	{
		ConvertToVoxels(out var voxels, out var materials);
		RebuildCuboidList(voxels, materials);
	}

	public void FlipVoxels(BlockFacing frontFacing)
	{
		ConvertToVoxels(out var voxels, out var materials);
		BoolArray16x16x16 boolArray16x16x = new BoolArray16x16x16();
		byte[,,] array = new byte[16, 16, 16];
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					boolArray16x16x[i, j, k] = voxels[(frontFacing.Axis == EnumAxis.Z) ? (15 - i) : i, j, (frontFacing.Axis == EnumAxis.X) ? (15 - k) : k];
					array[i, j, k] = materials[(frontFacing.Axis == EnumAxis.Z) ? (15 - i) : i, j, (frontFacing.Axis == EnumAxis.X) ? (15 - k) : k];
				}
			}
		}
		RebuildCuboidList(boolArray16x16x, array);
	}

	public void TransformList(int degrees, EnumAxis? flipAroundAxis, List<uint> list)
	{
		CuboidWithMaterial cuboidWithMaterial = tmpCuboids[0];
		Vec3d origin = new Vec3d(8.0, 8.0, 8.0);
		for (int i = 0; i < list.Count; i++)
		{
			FromUint(list[i], cuboidWithMaterial);
			if (flipAroundAxis == EnumAxis.X)
			{
				cuboidWithMaterial.X1 = 16 - cuboidWithMaterial.X1;
				cuboidWithMaterial.X2 = 16 - cuboidWithMaterial.X2;
			}
			if (flipAroundAxis == EnumAxis.Y)
			{
				cuboidWithMaterial.Y1 = 16 - cuboidWithMaterial.Y1;
				cuboidWithMaterial.Y2 = 16 - cuboidWithMaterial.Y2;
			}
			if (flipAroundAxis == EnumAxis.Z)
			{
				cuboidWithMaterial.Z1 = 16 - cuboidWithMaterial.Z1;
				cuboidWithMaterial.Z2 = 16 - cuboidWithMaterial.Z2;
			}
			Cuboidi cuboidi = cuboidWithMaterial.RotatedCopy(0, -degrees, 0, origin);
			cuboidWithMaterial.Set(cuboidi.X1, cuboidi.Y1, cuboidi.Z1, cuboidi.X2, cuboidi.Y2, cuboidi.Z2);
			list[i] = ToUint(cuboidWithMaterial);
		}
	}

	public void RotateModel(int degrees, EnumAxis? flipAroundAxis)
	{
		TransformList(degrees, flipAroundAxis, VoxelCuboids);
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IMicroblockBehavior microblockBehavior)
			{
				microblockBehavior.RotateModel(degrees, flipAroundAxis);
			}
		}
		if (flipAroundAxis.HasValue)
		{
			if (originalCuboids != null)
			{
				List<uint> list = new List<uint>(originalCuboids);
				TransformList(degrees, flipAroundAxis, list);
				originalCuboids = list.ToArray();
			}
			int num = -degrees / 90;
			SmallBoolArray smallBoolArray = sidecenterSolid;
			SmallBoolArray smallBoolArray2 = sideAlmostSolid;
			for (int i = 0; i < 4; i++)
			{
				sidecenterSolid[i] = smallBoolArray[GameMath.Mod(i + num, 4)];
				sideAlmostSolid[i] = smallBoolArray2[GameMath.Mod(i + num, 4)];
			}
		}
		Api?.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		rotationY = (short)((rotationY + degrees) % 360);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int byDegrees, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAroundAxis)
	{
		uint[] array = (tree["cuboids"] as IntArrayAttribute)?.AsUint;
		VoxelCuboids = ((array == null) ? new List<uint>(0) : new List<uint>(array));
		RotateModel(byDegrees, flipAroundAxis);
		tree["cuboids"] = new IntArrayAttribute(VoxelCuboids.ToArray());
		int[] array2 = (tree["materials"] as IntArrayAttribute)?.value;
		if (array2 != null)
		{
			int[] array3 = new int[array2.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				int num = array2[i];
				if (oldBlockIdMapping.TryGetValue(num, out var value))
				{
					Block block = worldAccessor.GetBlock(value);
					if (block != null)
					{
						AssetLocation rotatedBlockCode = block.GetRotatedBlockCode(byDegrees);
						Block block2 = worldAccessor.GetBlock(rotatedBlockCode);
						array3[i] = block2.Id;
					}
					else
					{
						array3[i] = num;
						worldAccessor.Logger.Warning("Cannot load chiseled block id mapping for rotation @ {1}, block id {0} not found block registry. Will not display correctly.", value, Pos);
					}
				}
				else
				{
					array3[i] = num;
					if (num >= worldAccessor.Blocks.Count)
					{
						worldAccessor.Logger.Warning("Cannot load chiseled block id mapping for rotation @ {1}, block code {0} not found block registry. Will not display correctly.", num, Pos);
					}
				}
			}
			tree["materials"] = new IntArrayAttribute(array3);
		}
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IRotatable rotatable)
			{
				rotatable.OnTransformed(worldAccessor, tree, byDegrees, oldBlockIdMapping, oldItemIdMapping, flipAroundAxis);
			}
		}
	}

	public int GetVoxelMaterialAt(Vec3i voxelPos)
	{
		ConvertToVoxels(out var voxels, out var materials);
		if (voxels[voxelPos.X, voxelPos.Y, voxelPos.Z])
		{
			return BlockIds[materials[voxelPos.X, voxelPos.Y, voxelPos.Z]];
		}
		return 0;
	}

	public bool SetVoxel(Vec3i voxelPos, bool state, byte materialId, int size)
	{
		ConvertToVoxels(out var voxels, out var materials);
		bool flag = false;
		int num = voxelPos.X + size;
		int num2 = voxelPos.Y + size;
		int num3 = voxelPos.Z + size;
		for (int i = voxelPos.X; i < num; i++)
		{
			for (int j = voxelPos.Y; j < num2; j++)
			{
				for (int k = voxelPos.Z; k < num3; k++)
				{
					if (i < 16 && j < 16 && k < 16)
					{
						if (state)
						{
							flag |= !voxels[i, j, k] || materials[i, j, k] != materialId;
							voxels[i, j, k] = true;
							materials[i, j, k] = materialId;
						}
						else
						{
							flag |= voxels[i, j, k];
							voxels[i, j, k] = false;
						}
					}
				}
			}
		}
		if (!flag)
		{
			return false;
		}
		RebuildCuboidList(voxels, materials);
		Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		return true;
	}

	public void BeginEdit(out BoolArray16x16x16 voxels, out byte[,,] voxelMaterial)
	{
		ConvertToVoxels(out voxels, out voxelMaterial);
	}

	public void EndEdit(BoolArray16x16x16 voxels, byte[,,] voxelMaterial)
	{
		RebuildCuboidList(voxels, voxelMaterial);
		Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
	}

	public void SetData(BoolArray16x16x16 Voxels, byte[,,] VoxelMaterial)
	{
		RebuildCuboidList(Voxels, VoxelMaterial);
		if (Api.Side == EnumAppSide.Client)
		{
			MarkMeshDirty();
		}
		RegenSelectionBoxes(Api.World, null);
		MarkDirty(redrawOnClient: true);
		if (VoxelCuboids.Count == 0)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
	}

	public bool DoEmitSideAo(int facing)
	{
		return (emitSideAo & (1 << facing)) != 0;
	}

	public bool DoEmitSideAoByFlag(int flag)
	{
		return (emitSideAo & flag) != 0;
	}

	public override void HistoryStateRestore()
	{
		RebuildCuboidList();
		MarkDirty(redrawOnClient: true);
	}

	protected void RebuildCuboidList(BoolArray16x16x16 Voxels, byte[,,] VoxelMaterial)
	{
		BoolArray16x16x16 boolArray16x16x = new BoolArray16x16x16();
		emitSideAo = 63;
		sidecenterSolid = new SmallBoolArray(63);
		float num = 0f;
		List<uint> list = new List<uint>();
		int[] array = new int[6];
		int[] array2 = new int[6];
		byte[] lightHsv = GetLightHsv(Api.World.BlockAccessor);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (!Voxels[i, j, k])
					{
						if (k == 0)
						{
							array[BlockFacing.NORTH.Index]++;
							if (Math.Abs(j - 8) < 5 && Math.Abs(i - 8) < 5)
							{
								array2[BlockFacing.NORTH.Index]++;
							}
						}
						if (i == 15)
						{
							array[BlockFacing.EAST.Index]++;
							if (Math.Abs(j - 8) < 5 && Math.Abs(k - 8) < 5)
							{
								array2[BlockFacing.EAST.Index]++;
							}
						}
						if (k == 15)
						{
							array[BlockFacing.SOUTH.Index]++;
							if (Math.Abs(j - 8) < 5 && Math.Abs(i - 8) < 5)
							{
								array2[BlockFacing.SOUTH.Index]++;
							}
						}
						if (i == 0)
						{
							array[BlockFacing.WEST.Index]++;
							if (Math.Abs(j - 8) < 5 && Math.Abs(k - 8) < 5)
							{
								array2[BlockFacing.WEST.Index]++;
							}
						}
						if (j == 15)
						{
							array[BlockFacing.UP.Index]++;
							if (Math.Abs(k - 8) < 5 && Math.Abs(i - 8) < 5)
							{
								array2[BlockFacing.UP.Index]++;
							}
						}
						if (j == 0)
						{
							array[BlockFacing.DOWN.Index]++;
							if (Math.Abs(k - 8) < 5 && Math.Abs(i - 8) < 5)
							{
								array2[BlockFacing.DOWN.Index]++;
							}
						}
						continue;
					}
					num += 1f;
					if (!boolArray16x16x[i, j, k])
					{
						CuboidWithMaterial cub = new CuboidWithMaterial
						{
							Material = VoxelMaterial[i, j, k],
							X1 = i,
							Y1 = j,
							Z1 = k,
							X2 = i + 1,
							Y2 = j + 1,
							Z2 = k + 1
						};
						bool flag = true;
						while (flag)
						{
							flag = false;
							flag |= TryGrowX(cub, Voxels, boolArray16x16x, VoxelMaterial);
							flag |= TryGrowY(cub, Voxels, boolArray16x16x, VoxelMaterial);
							flag |= TryGrowZ(cub, Voxels, boolArray16x16x, VoxelMaterial);
						}
						list.Add(ToUint(cub));
					}
				}
			}
		}
		VoxelCuboids = list;
		bool flag2 = array[0] < 64 || array[1] < 64 || array[2] < 64 || array[3] < 64 || array[4] < 64 || array[5] < 64;
		if (absorbAnyLight != flag2)
		{
			int lightAbsorption = GetLightAbsorption();
			absorbAnyLight = flag2;
			int lightAbsorption2 = GetLightAbsorption();
			if (lightAbsorption != lightAbsorption2)
			{
				Api.World.BlockAccessor.MarkAbsorptionChanged(lightAbsorption, lightAbsorption2, Pos);
			}
		}
		int num2 = 0;
		for (int l = 0; l < 6; l++)
		{
			sidecenterSolid[l] = array2[l] < 5;
			if (sideAlmostSolid[l] = array[l] <= 32)
			{
				num2 += 1 << l;
			}
		}
		emitSideAo = ((lightHsv[2] < 10 && flag2) ? num2 : 0);
		if (BlockIds.Length == 1 && Api.World.GetBlock(BlockIds[0]).RenderPass == EnumChunkRenderPass.Meta)
		{
			emitSideAo = 0;
		}
		sizeRel = num / 4096f;
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IMicroblockBehavior microblockBehavior)
			{
				microblockBehavior.RebuildCuboidList(Voxels, VoxelMaterial);
			}
		}
		if (DisplacesLiquid())
		{
			Api.World.BlockAccessor.SetBlock(0, Pos, 2);
		}
	}

	public bool DisplacesLiquid()
	{
		if (sideAlmostSolid[0] && sideAlmostSolid[1] && sideAlmostSolid[2] && sideAlmostSolid[3])
		{
			return sideAlmostSolid[5];
		}
		return false;
	}

	protected bool TryGrowX(CuboidWithMaterial cub, BoolArray16x16x16 voxels, BoolArray16x16x16 voxelVisited, byte[,,] voxelMaterial)
	{
		if (cub.X2 > 15)
		{
			return false;
		}
		for (int i = cub.Y1; i < cub.Y2; i++)
		{
			for (int j = cub.Z1; j < cub.Z2; j++)
			{
				if (!voxels[cub.X2, i, j] || voxelVisited[cub.X2, i, j] || voxelMaterial[cub.X2, i, j] != cub.Material)
				{
					return false;
				}
			}
		}
		for (int k = cub.Y1; k < cub.Y2; k++)
		{
			for (int l = cub.Z1; l < cub.Z2; l++)
			{
				voxelVisited[cub.X2, k, l] = true;
			}
		}
		cub.X2++;
		return true;
	}

	protected bool TryGrowY(CuboidWithMaterial cub, BoolArray16x16x16 voxels, BoolArray16x16x16 voxelVisited, byte[,,] voxelMaterial)
	{
		if (cub.Y2 > 15)
		{
			return false;
		}
		for (int i = cub.X1; i < cub.X2; i++)
		{
			for (int j = cub.Z1; j < cub.Z2; j++)
			{
				if (!voxels[i, cub.Y2, j] || voxelVisited[i, cub.Y2, j] || voxelMaterial[i, cub.Y2, j] != cub.Material)
				{
					return false;
				}
			}
		}
		for (int k = cub.X1; k < cub.X2; k++)
		{
			for (int l = cub.Z1; l < cub.Z2; l++)
			{
				voxelVisited[k, cub.Y2, l] = true;
			}
		}
		cub.Y2++;
		return true;
	}

	protected bool TryGrowZ(CuboidWithMaterial cub, BoolArray16x16x16 voxels, BoolArray16x16x16 voxelVisited, byte[,,] voxelMaterial)
	{
		if (cub.Z2 > 15)
		{
			return false;
		}
		for (int i = cub.X1; i < cub.X2; i++)
		{
			for (int j = cub.Y1; j < cub.Y2; j++)
			{
				if (!voxels[i, j, cub.Z2] || voxelVisited[i, j, cub.Z2] || voxelMaterial[i, j, cub.Z2] != cub.Material)
				{
					return false;
				}
			}
		}
		for (int k = cub.X1; k < cub.X2; k++)
		{
			for (int l = cub.Y1; l < cub.Y2; l++)
			{
				voxelVisited[k, l, cub.Z2] = true;
			}
		}
		cub.Z2++;
		return true;
	}

	public virtual void RegenSelectionBoxes(IWorldAccessor worldForResolve, IPlayer byPlayer)
	{
		Cuboidf[] array = new Cuboidf[VoxelCuboids.Count];
		CuboidWithMaterial cuboidWithMaterial = tmpCuboids[0];
		totalVoxels = 0;
		List<Cuboidf> list = null;
		bool flag = false;
		for (int i = 0; i < BlockIds.Length; i++)
		{
			flag |= worldForResolve.Blocks[BlockIds[i]].RenderPass == EnumChunkRenderPass.Meta;
		}
		if (flag)
		{
			list = new List<Cuboidf>();
			for (int j = 0; j < VoxelCuboids.Count; j++)
			{
				FromUint(VoxelCuboids[j], cuboidWithMaterial);
				Cuboidf cuboidf = cuboidWithMaterial.ToCuboidf();
				list.Add(cuboidf);
				Block block = worldForResolve.Blocks[BlockIds[cuboidWithMaterial.Material]];
				if (block.RenderPass == EnumChunkRenderPass.Meta)
				{
					IMetaBlock metaBlock = block.GetInterface<IMetaBlock>(worldForResolve, Pos);
					if (metaBlock == null || !metaBlock.IsSelectable(Pos))
					{
						continue;
					}
				}
				array[j] = cuboidf;
				totalVoxels += cuboidWithMaterial.Volume;
			}
			selectionBoxesStd = array.Where((Cuboidf ele) => ele != null).ToArray();
			selectionBoxesMetaMode = list.ToArray();
		}
		else
		{
			for (int num = 0; num < VoxelCuboids.Count; num++)
			{
				FromUint(VoxelCuboids[num], cuboidWithMaterial);
				array[num] = cuboidWithMaterial.ToCuboidf();
				totalVoxels += cuboidWithMaterial.Volume;
			}
			selectionBoxesStd = array;
			selectionBoxesMetaMode = null;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		BlockIds = MaterialIdsFromAttributes(tree, worldAccessForResolve);
		DecorIds = (tree["decorIds"] as IntArrayAttribute)?.value;
		BlockName = tree.GetString("blockName");
		int num = tree.GetInt("rotation");
		rotationY = (short)(((num >> 10) & 0x3FF) - 360);
		DecorRotations = tree.GetInt("decorRot");
		VoxelCuboids = new List<uint>(GetVoxelCuboids(tree));
		byte[] bytes = tree.GetBytes("emitSideAo", singleByte255);
		if (bytes.Length != 0)
		{
			emitSideAo = bytes[0];
			absorbAnyLight = emitSideAo != 0;
		}
		byte[] bytes2 = tree.GetBytes("sideSolid", singleByte255);
		if (bytes2.Length != 0)
		{
			sidecenterSolid = new SmallBoolArray(bytes2[0] & 0x3F);
		}
		byte[] bytes3 = tree.GetBytes("sideAlmostSolid", singleByte255);
		if (bytes3.Length != 0)
		{
			sideAlmostSolid = new SmallBoolArray(bytes3[0] & 0x3F);
		}
		if (tree.HasAttribute("originalCuboids"))
		{
			originalCuboids = (tree["originalCuboids"] as IntArrayAttribute)?.AsUint;
		}
		if (worldAccessForResolve.Side == EnumAppSide.Client)
		{
			if (Api != null)
			{
				Mesh = GenMesh();
				Api.World.BlockAccessor.MarkBlockModified(Pos);
			}
			int num2 = Pos.X % 32;
			int num3 = Pos.X % 32;
			if (Api != null)
			{
				UpdateNeighbors(this);
			}
			else if (num2 == 0 || num2 == 31 || num3 == 0 || num3 == 31)
			{
				if (num2 == 0)
				{
					UpdateNeighbour(worldAccessForResolve, Pos.WestCopy());
				}
				if (num3 == 0)
				{
					UpdateNeighbour(worldAccessForResolve, Pos.NorthCopy());
				}
				if (num2 == 31)
				{
					UpdateNeighbour(worldAccessForResolve, Pos.EastCopy());
				}
				if (num3 == 31)
				{
					UpdateNeighbour(worldAccessForResolve, Pos.SouthCopy());
				}
			}
		}
		else if (!tree.HasAttribute("sideAlmostSolid"))
		{
			if (Api == null)
			{
				Api = worldAccessForResolve.Api;
			}
			RebuildCuboidList();
		}
		RegenSelectionBoxes(worldAccessForResolve, null);
	}

	public static uint[] GetVoxelCuboids(ITreeAttribute tree)
	{
		uint[] array = (tree["cuboids"] as IntArrayAttribute)?.AsUint;
		if (array == null)
		{
			array = (tree["cuboids"] as LongArrayAttribute)?.AsUint;
		}
		if (array == null)
		{
			array = new uint[1] { ToUint(0, 0, 0, 16, 16, 16, 0) };
		}
		return array;
	}

	public static int[] MaterialIdsFromAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		if (tree["materials"] is IntArrayAttribute intArrayAttribute)
		{
			return intArrayAttribute.value;
		}
		if (!(tree["materials"] is StringArrayAttribute))
		{
			return new int[1] { worldAccessForResolve.GetBlock(new AssetLocation("rock-granite")).Id };
		}
		string[] value = (tree["materials"] as StringArrayAttribute).value;
		int[] array = new int[value.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Block block = worldAccessForResolve.GetBlock(new AssetLocation(value[i]));
			if (block == null)
			{
				block = worldAccessForResolve.GetBlock(new AssetLocation(value[i] + "-free"));
				if (block == null)
				{
					block = worldAccessForResolve.GetBlock(new AssetLocation("rock-granite"));
				}
			}
			array[i] = block.BlockId;
		}
		return array;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (BlockIds != null)
		{
			tree["materials"] = new IntArrayAttribute(BlockIds);
		}
		if (DecorIds != null)
		{
			tree["decorIds"] = new IntArrayAttribute(DecorIds);
		}
		tree.SetInt("decorRot", DecorRotations);
		tree.SetInt("rotation", rotationY + 360 << 10);
		tree["cuboids"] = new IntArrayAttribute(VoxelCuboids.ToArray());
		tree.SetBytes("emitSideAo", new byte[1] { (byte)emitSideAo });
		tree.SetBytes("sideSolid", new byte[1] { (byte)sidecenterSolid.Value() });
		tree.SetBytes("sideAlmostSolid", new byte[1] { (byte)sideAlmostSolid.Value() });
		tree.SetString("blockName", BlockName);
		if (originalCuboids != null)
		{
			tree["originalCuboids"] = new IntArrayAttribute(originalCuboids);
		}
	}

	public static uint ToUint(int minx, int miny, int minz, int maxx, int maxy, int maxz, int material)
	{
		return (uint)(minx | (miny << 4) | (minz << 8) | (maxx - 1 << 12) | (maxy - 1 << 16) | (maxz - 1 << 20) | (material << 24));
	}

	public static uint ToUint(CuboidWithMaterial cub)
	{
		return (uint)(cub.X1 | (cub.Y1 << 4) | (cub.Z1 << 8) | (cub.X2 - 1 << 12) | (cub.Y2 - 1 << 16) | (cub.Z2 - 1 << 20) | (cub.Material << 24));
	}

	public static void FromUint(uint val, CuboidWithMaterial tocuboid)
	{
		tocuboid.X1 = (int)(val & 0xF);
		tocuboid.Y1 = (int)((val >> 4) & 0xF);
		tocuboid.Z1 = (int)((val >> 8) & 0xF);
		tocuboid.X2 = (int)(((val >> 12) & 0xF) + 1);
		tocuboid.Y2 = (int)(((val >> 16) & 0xF) + 1);
		tocuboid.Z2 = (int)(((val >> 20) & 0xF) + 1);
		tocuboid.Material = (byte)((val >> 24) & 0xFF);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		for (int i = 0; i < BlockIds.Length; i++)
		{
			if (oldBlockIdMapping != null && oldBlockIdMapping.TryGetValue(BlockIds[i], out var value))
			{
				Block block = worldForNewMappings.GetBlock(value);
				if (block == null)
				{
					worldForNewMappings.Logger.Warning("Cannot load chiseled block id mapping @ {1}, block code {0} not found block registry. Will not display correctly.", value, Pos);
				}
				else
				{
					BlockIds[i] = block.Id;
				}
			}
			else if (worldForNewMappings.GetBlock(BlockIds[i]) == null)
			{
				worldForNewMappings.Logger.Warning("Cannot load chiseled block id mapping @ {1}, block id {0} not found block registry. Will not display correctly.", BlockIds[i], Pos);
			}
		}
		if (DecorIds == null)
		{
			return;
		}
		for (int j = 0; j < DecorIds.Length; j++)
		{
			if (oldBlockIdMapping.TryGetValue(DecorIds[j], out var value2))
			{
				Block block2 = worldForNewMappings.GetBlock(value2);
				if (block2 == null)
				{
					worldForNewMappings.Logger.Warning("Cannot load chiseled decor block id mapping @ {1}, block code {0} not found block registry. Will not display correctly.", value2, Pos);
				}
				else
				{
					DecorIds[j] = block2.Id;
				}
			}
			else
			{
				worldForNewMappings.Logger.Warning("Cannot load chiseled decor block id mapping @ {1}, block id {0} not found block registry. Will not display correctly.", DecorIds[j], Pos);
			}
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		for (int i = 0; i < BlockIds.Length; i++)
		{
			Block block = Api.World.GetBlock(BlockIds[i]);
			if (!(block.Code == null))
			{
				blockIdMapping[BlockIds[i]] = block.Code;
			}
		}
		if (DecorIds == null)
		{
			return;
		}
		for (int j = 0; j < DecorIds.Length; j++)
		{
			Block block2 = Api.World.GetBlock(DecorIds[j]);
			if (!(block2.Code == null))
			{
				blockIdMapping[DecorIds[j]] = block2.Code;
			}
		}
	}

	public bool NoVoxelsWithMaterial(uint index)
	{
		foreach (uint voxelCuboid in VoxelCuboids)
		{
			uint num = (voxelCuboid >> 24) & 0xFF;
			if (index == num)
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool RemoveMaterial(Block block)
	{
		if (BlockIds.Contains(block.Id))
		{
			int num = BlockIds.IndexOf(block.Id);
			BlockIds = BlockIds.Remove(block.Id);
			for (int i = 0; i < VoxelCuboids.Count; i++)
			{
				int num2 = (int)((VoxelCuboids[i] >> 24) & 0xFF);
				if (num == num2)
				{
					VoxelCuboids.RemoveAt(i);
					i--;
				}
			}
			ShiftMaterialIndicesAt(num);
			return true;
		}
		return false;
	}

	private void ShiftMaterialIndicesAt(int index)
	{
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			uint num = (VoxelCuboids[i] >> 24) & 0xFF;
			if (num >= index)
			{
				VoxelCuboids[i] = (VoxelCuboids[i] & 0xFFFFFF) | (num - 1 << 24);
			}
		}
	}

	public MeshData GenMesh()
	{
		if (BlockIds == null)
		{
			return null;
		}
		GenRotatedMaterialIds();
		MeshData result = CreateMesh(Api as ICoreClientAPI, VoxelCuboids, BlockIdsRotated, DecorIdsRotated, DecorRotations, OriginalVoxelCuboids, Pos);
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IMicroblockBehavior microblockBehavior)
			{
				microblockBehavior.RegenMesh();
			}
		}
		withColorMapData = false;
		int num = 0;
		while (!withColorMapData && num < BlockIds.Length)
		{
			withColorMapData |= Api.World.Blocks[BlockIds[num]].ClimateColorMapResolved != null;
			num++;
		}
		return result;
	}

	private void GenRotatedMaterialIds()
	{
		if (rotationY == 0)
		{
			BlockIdsRotated = BlockIds;
			DecorIdsRotated = DecorIds;
			return;
		}
		if (BlockIdsRotated == null || BlockIdsRotated.Length < BlockIds.Length)
		{
			BlockIdsRotated = new int[BlockIds.Length];
		}
		for (int i = 0; i < BlockIds.Length; i++)
		{
			int num = BlockIds[i];
			AssetLocation rotatedBlockCode = Api.World.GetBlock(num).GetRotatedBlockCode(rotationY);
			Block obj = ((rotatedBlockCode == null) ? null : Api.World.GetBlock(rotatedBlockCode));
			BlockIdsRotated[i] = obj?.Id ?? num;
		}
		if (DecorIds != null)
		{
			if (DecorIdsRotated == null || DecorIdsRotated.Length < DecorIds.Length)
			{
				DecorIdsRotated = new int[DecorIds.Length];
			}
			for (int j = 0; j < 4; j++)
			{
				DecorIdsRotated[j] = DecorIds[GameMath.Mod(j + rotationY / 90, 4)];
			}
			DecorIdsRotated[4] = DecorIds[4];
			DecorIdsRotated[5] = DecorIds[5];
		}
	}

	public void RegenMesh(ICoreClientAPI capi)
	{
		GenRotatedMaterialIds();
		Mesh = CreateMesh(capi, VoxelCuboids, BlockIdsRotated, DecorIdsRotated, DecorRotations, OriginalVoxelCuboids, Pos);
	}

	public void MarkMeshDirty()
	{
		Mesh = null;
	}

	private static RefList<VoxelMaterial> getOrCreateBlockMatRefList()
	{
		return tmpBlockMaterials ?? (tmpBlockMaterials = new RefList<VoxelMaterial>());
	}

	private static RefList<VoxelMaterial> getOrCreateDecorMatRefList()
	{
		return tmpDecorMaterials ?? (tmpDecorMaterials = new RefList<VoxelMaterial>());
	}

	private static VoxelInfo[] getOrCreateCuboidInfoArray()
	{
		return tmpVoxels ?? (tmpVoxels = new VoxelInfo[5832]);
	}

	public static MeshData CreateMesh(ICoreClientAPI capi, List<uint> voxelCuboids, int[] blockIds, int[] decorIds, BlockPos posForRnd = null, uint[] originalCuboids = null, int decorRotations = 0)
	{
		return CreateMesh(capi, voxelCuboids, blockIds, decorIds, decorRotations, originalCuboids ?? defaultOriginalVoxelCuboids, posForRnd);
	}

	public unsafe static MeshData CreateMesh(ICoreClientAPI capi, List<uint> voxelCuboids, int[] blockIds, int[] decorIds, int decorRotations, uint[] originalVoxelCuboids, BlockPos pos = null)
	{
		MeshData meshData = new MeshData(24, 36).WithColorMaps().WithRenderpasses().WithXyzFaces();
		if (voxelCuboids == null || blockIds == null)
		{
			return meshData;
		}
		RefList<VoxelMaterial> orCreateBlockMatRefList = getOrCreateBlockMatRefList();
		orCreateBlockMatRefList.Clear();
		VoxelInfo[] orCreateCuboidInfoArray = getOrCreateCuboidInfoArray();
		fixed (VoxelInfo* ptr = orCreateCuboidInfoArray)
		{
			Unsafe.InitBlockUnaligned(ptr, byte.MaxValue, (uint)(sizeof(VoxelInfo) * orCreateCuboidInfoArray.Length));
			if (pos != null)
			{
				FetchNeighborVoxels(capi, orCreateBlockMatRefList, ptr, pos);
			}
		}
		bool flag = false;
		bool flag2 = false;
		int count = orCreateBlockMatRefList.Count;
		for (int i = 0; i < blockIds.Length; i++)
		{
			Block block = capi.World.GetBlock(blockIds[i]);
			orCreateBlockMatRefList.Add(VoxelMaterial.FromBlock(capi, block, pos, cullBetweenTransparents: true));
			flag |= block.RenderPass == EnumChunkRenderPass.TopSoil;
			flag2 |= block.Frostable;
		}
		if (flag)
		{
			meshData.CustomShorts = new CustomMeshDataPartShort
			{
				InterleaveOffsets = new int[1],
				InterleaveSizes = new int[1] { 2 },
				InterleaveStride = 4,
				Conversion = DataConversion.NormalizedFloat
			};
		}
		RefList<VoxelMaterial> decorMaterials = loadDecor(capi, voxelCuboids, decorIds, pos, meshData, decorRotations);
		int* ptr2 = stackalloc int[6];
		FromUint(originalVoxelCuboids[0], out ptr2[3], out ptr2[5], out *ptr2, out ptr2[1], out ptr2[4], out ptr2[2], out var _);
		_ = voxelCuboids.Count;
		fixed (VoxelInfo* ptr3 = orCreateCuboidInfoArray)
		{
			int x;
			int y;
			int z;
			int x2;
			int y2;
			int z2;
			int material2;
			foreach (uint voxelCuboid in voxelCuboids)
			{
				FromUint(voxelCuboid, out x, out y, out z, out x2, out y2, out z2, out material2);
				FillCuboidEdges(ptr3, x, y, z, x2, y2, z2, count + material2);
			}
			GenFaceInfo genFaceInfo = new GenFaceInfo
			{
				capi = capi,
				targetMesh = meshData,
				originalBounds = ptr2,
				subPixelPaddingx = capi.BlockTextureAtlas.SubPixelPaddingX,
				subPixelPaddingy = capi.BlockTextureAtlas.SubPixelPaddingY,
				AnyFrostable = flag2
			};
			GenPlaneInfo genPlaneInfo = new GenPlaneInfo
			{
				blockMaterials = orCreateBlockMatRefList,
				decorMaterials = decorMaterials,
				voxels = ptr3
			};
			foreach (uint voxelCuboid2 in voxelCuboids)
			{
				FromUint(voxelCuboid2, out x, out y, out z, out x2, out y2, out z2, out material2);
				genPlaneInfo.materialIndex = count + material2;
				GenCuboidMesh(ref genFaceInfo, ref genPlaneInfo, x, y, z, x2, y2, z2);
			}
		}
		return meshData;
	}

	private static RefList<VoxelMaterial> loadDecor(ICoreClientAPI capi, List<uint> voxelCuboids, int[] decorIds, BlockPos pos, MeshData mesh, int decorRotations)
	{
		RefList<VoxelMaterial> refList = null;
		if (decorIds != null)
		{
			refList = getOrCreateDecorMatRefList();
			refList.Clear();
			for (int i = 0; i < decorIds.Length; i++)
			{
				int num = decorIds[i];
				if (num == 0)
				{
					refList.Add(noMat);
					continue;
				}
				Block block = capi.World.GetBlock(num);
				JsonObject attributes = block.Attributes;
				if (attributes == null || !attributes["attachas3d"].AsBool())
				{
					int decorRotation = (decorRotations >> i * 3) & 7;
					refList.Add(VoxelMaterial.FromBlock(capi, block, pos, cullBetweenTransparents: true, decorRotation));
					continue;
				}
				int num2 = ((decorRotations >> i * 3) & 7) % 4;
				MeshData meshData = capi.TesselatorManager.GetDefaultBlockMesh(block).Clone();
				if (num2 > 0)
				{
					meshData.Rotate(centerBase, 0f, (float)num2 * ((float)Math.PI / 2f), 0f);
				}
				meshData.Translate(BlockFacing.ALLFACES[i].Normalf * getOutermostVoxelDistanceToCenter(voxelCuboids, i));
				mesh.AddMeshData(meshData);
			}
		}
		return refList;
	}

	private static float getOutermostVoxelDistanceToCenter(List<uint> voxelCuboids, int faceindex)
	{
		int num = 0;
		int num2 = 0;
		switch (faceindex)
		{
		case 0:
			num2 = (num = 16);
			foreach (uint voxelCuboid in voxelCuboids)
			{
				num = Math.Min(num, (int)((voxelCuboid >> 8) & 0xF));
			}
			break;
		case 1:
			num2 = (num = 0);
			foreach (uint voxelCuboid2 in voxelCuboids)
			{
				num = Math.Max(num, (int)(((voxelCuboid2 >> 12) & 0xF) + 1));
			}
			break;
		case 2:
			num2 = (num = 0);
			foreach (uint voxelCuboid3 in voxelCuboids)
			{
				num = Math.Max(num, (int)(((voxelCuboid3 >> 20) & 0xF) + 1));
			}
			break;
		case 3:
			num2 = (num = 16);
			foreach (uint voxelCuboid4 in voxelCuboids)
			{
				num = Math.Min(num, (int)(voxelCuboid4 & 0xF));
			}
			break;
		case 4:
			num2 = (num = 0);
			foreach (uint voxelCuboid5 in voxelCuboids)
			{
				num = Math.Max(num, (int)(((voxelCuboid5 >> 16) & 0xF) + 1));
			}
			break;
		case 5:
			num2 = (num = 16);
			foreach (uint voxelCuboid6 in voxelCuboids)
			{
				num = Math.Min(num, (int)((voxelCuboid6 >> 4) & 0xF));
			}
			break;
		}
		return (float)Math.Abs(num2 - num) / 16f;
	}

	public MeshData CreateDecalMesh(ITexPositionSource decalTexSource)
	{
		return CreateDecalMesh(Api as ICoreClientAPI, VoxelCuboids, decalTexSource, OriginalVoxelCuboids);
	}

	public unsafe static MeshData CreateDecalMesh(ICoreClientAPI capi, List<uint> voxelCuboids, ITexPositionSource decalTexSource, uint[] originalVoxelCuboids)
	{
		MeshData meshData = new MeshData(24, 36).WithColorMaps().WithRenderpasses().WithXyzFaces();
		if (voxelCuboids == null)
		{
			return meshData;
		}
		RefList<VoxelMaterial> orCreateBlockMatRefList = getOrCreateBlockMatRefList();
		orCreateBlockMatRefList.Clear();
		orCreateBlockMatRefList.Add(VoxelMaterial.FromTexSource(capi, decalTexSource, cullBetweenTransparents: true));
		int* ptr = stackalloc int[6];
		FromUint(originalVoxelCuboids[0], out ptr[3], out ptr[5], out *ptr, out ptr[1], out ptr[4], out ptr[2], out var material);
		int count = voxelCuboids.Count;
		VoxelInfo[] orCreateCuboidInfoArray = getOrCreateCuboidInfoArray();
		fixed (VoxelInfo* ptr2 = orCreateCuboidInfoArray)
		{
			Unsafe.InitBlockUnaligned(ptr2, byte.MaxValue, (uint)(sizeof(VoxelInfo) * orCreateCuboidInfoArray.Length));
			int x;
			int y;
			int z;
			int x2;
			int y2;
			int z2;
			for (int i = 0; i < count; i++)
			{
				FromUint(voxelCuboids[i], out x, out y, out z, out x2, out y2, out z2, out material);
				FillCuboidEdges(ptr2, x, y, z, x2, y2, z2, 0);
			}
			GenFaceInfo genFaceInfo = new GenFaceInfo
			{
				capi = capi,
				targetMesh = meshData,
				originalBounds = ptr,
				subPixelPaddingx = capi.BlockTextureAtlas.SubPixelPaddingX,
				subPixelPaddingy = capi.BlockTextureAtlas.SubPixelPaddingY
			};
			GenPlaneInfo genPlaneInfo = new GenPlaneInfo
			{
				blockMaterials = orCreateBlockMatRefList,
				voxels = ptr2,
				materialIndex = 0
			};
			for (int j = 0; j < count; j++)
			{
				FromUint(voxelCuboids[j], out x, out y, out z, out x2, out y2, out z2, out material);
				GenCuboidMesh(ref genFaceInfo, ref genPlaneInfo, x, y, z, x2, y2, z2);
			}
		}
		return meshData;
	}

	private unsafe static void FetchNeighborVoxels(ICoreClientAPI capi, RefList<VoxelMaterial> matList, VoxelInfo* voxels, BlockPos pos)
	{
		IBlockAccessor blockAccessor = capi.World.BlockAccessor;
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos blockPos = pos.AddCopy(blockFacing);
			if (!(blockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock { BlockIds: var blockIds, VoxelCuboids: var voxelCuboids }) || blockIds == null || voxelCuboids == null)
			{
				continue;
			}
			List<uint> list = voxelCuboids;
			int count = matList.Count;
			int[] array = blockIds;
			foreach (int blockId in array)
			{
				matList.Add(VoxelMaterial.FromBlock(capi, capi.World.GetBlock(blockId), blockPos, cullBetweenTransparents: true));
			}
			for (int k = 0; k < list.Count; k++)
			{
				FromUint(list[k], out var x, out var y, out var z, out var x2, out var y2, out var z2, out var material);
				if (material >= blockIds.Length)
				{
					break;
				}
				FillCuboidFace(voxels, x, y, z, x2, y2, z2, count + material, blockFacing);
			}
		}
	}

	private unsafe static void FillCuboidFace(VoxelInfo* cuboids, int x0, int y0, int z0, int x1, int y1, int z1, int material, BlockFacing face)
	{
		switch (face.Index)
		{
		case 0:
			if (z1 != 16)
			{
				return;
			}
			break;
		case 1:
			if (x0 != 0)
			{
				return;
			}
			break;
		case 2:
			if (z0 != 0)
			{
				return;
			}
			break;
		case 3:
			if (x1 != 16)
			{
				return;
			}
			break;
		case 4:
			if (y0 != 0)
			{
				return;
			}
			break;
		case 5:
			if (y1 != 16)
			{
				return;
			}
			break;
		}
		x0++;
		x1++;
		y0++;
		y1++;
		z0++;
		z1++;
		y0 *= 18;
		y1 *= 18;
		z0 *= 324;
		z1 *= 324;
		switch (face.Index)
		{
		case 0:
			FillPlane(cuboids, material, x0, x1, 1, y0, y1, 18, 0);
			break;
		case 1:
			FillPlane(cuboids, material, y0, y1, 18, z0, z1, 324, 17);
			break;
		case 2:
			FillPlane(cuboids, material, x0, x1, 1, y0, y1, 18, 5508);
			break;
		case 3:
			FillPlane(cuboids, material, y0, y1, 18, z0, z1, 324, 0);
			break;
		case 4:
			FillPlane(cuboids, material, x0, x1, 1, z0, z1, 324, 306);
			break;
		case 5:
			FillPlane(cuboids, material, x0, x1, 1, z0, z1, 324, 0);
			break;
		}
	}

	public unsafe static void FillCuboidEdges(VoxelInfo* cuboids, int x0, int y0, int z0, int x1, int y1, int z1, int material)
	{
		x0++;
		x1++;
		y0++;
		y1++;
		z0++;
		z1++;
		y0 *= 18;
		y1 *= 18;
		z0 *= 324;
		z1 *= 324;
		FillPlane(cuboids, material, x0, x1, 1, y0, y1, 18, z0);
		FillPlane(cuboids, material, x0, x1, 1, y0, y1, 18, z1 - 324);
		FillPlane(cuboids, material, x0, x1, 1, z0, z1, 324, y0);
		FillPlane(cuboids, material, x0, x1, 1, z0, z1, 324, y1 - 18);
		FillPlane(cuboids, material, y0, y1, 18, z0, z1, 324, x0);
		FillPlane(cuboids, material, y0, y1, 18, z0, z1, 324, x1 - 1);
	}

	public unsafe static void FillPlane(VoxelInfo* ptr, int value, int fromX, int toX, int stepX, int fromY, int toY, int stepY, int z)
	{
		for (int i = fromX; i < toX; i += stepX)
		{
			for (int j = fromY; j < toY; j += stepY)
			{
				ptr[i + j + z].Material = value;
			}
		}
	}

	public static void GenCuboidMesh(ref GenFaceInfo genFaceInfo, ref GenPlaneInfo genPlaneInfo, int x0, int y0, int z0, int x1, int y1, int z1)
	{
		x0++;
		x1++;
		y0++;
		y1++;
		z0++;
		z1++;
		y0 *= 18;
		y1 *= 18;
		z0 *= 324;
		z1 *= 324;
		genFaceInfo.SetInfo(ConvertPlaneX, 3, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[3]);
		genPlaneInfo.SetCoords(z0, z1, 324, y0, y1, 18, x0, x0 - 1);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
		genFaceInfo.SetInfo(ConvertPlaneX, 1, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[1]);
		genPlaneInfo.SetCoords(z0, z1, 324, y0, y1, 18, x1 - 1, x1);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
		genFaceInfo.SetInfo(ConvertPlaneY, 5, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[5]);
		genPlaneInfo.SetCoords(x0, x1, 1, z0, z1, 324, y0, y0 - 18);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
		genFaceInfo.SetInfo(ConvertPlaneY, 4, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[4]);
		genPlaneInfo.SetCoords(x0, x1, 1, z0, z1, 324, y1 - 18, y1);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
		genFaceInfo.SetInfo(ConvertPlaneZ, 0, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[0]);
		genPlaneInfo.SetCoords(x0, x1, 1, y0, y1, 18, z0, z0 - 324);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
		genFaceInfo.SetInfo(ConvertPlaneZ, 2, (genPlaneInfo.decorMaterials == null) ? noMat : genPlaneInfo.decorMaterials[2]);
		genPlaneInfo.SetCoords(x0, x1, 1, y0, y1, 18, z1 - 324, z1);
		genPlaneInfo.GenPlaneMesh(ref genFaceInfo);
	}

	public static void FromUint(uint val, out int x0, out int y0, out int z0, out int x1, out int y1, out int z1, out int material)
	{
		x0 = (int)(val & 0xF);
		y0 = (int)((val >> 4) & 0xF);
		z0 = (int)((val >> 8) & 0xF);
		x1 = (int)(((val >> 12) & 0xF) + 1);
		y1 = (int)(((val >> 16) & 0xF) + 1);
		z1 = (int)(((val >> 20) & 0xF) + 1);
		material = (int)((val >> 24) & 0xFF);
	}

	private static bool isMergableMaterial(int selfMat, int otherMat, RefList<VoxelMaterial> materials)
	{
		if (selfMat == otherMat)
		{
			return true;
		}
		if (otherMat >= 0)
		{
			VoxelMaterial voxelMaterial = materials[selfMat];
			VoxelMaterial voxelMaterial2 = materials[otherMat];
			if (voxelMaterial.BlockId == voxelMaterial2.BlockId)
			{
				return true;
			}
			if (voxelMaterial2.BlockId == 0)
			{
				return false;
			}
			bool flag = true;
			switch (voxelMaterial.RenderPass)
			{
			case EnumChunkRenderPass.OpaqueNoCull:
			case EnumChunkRenderPass.BlendNoCull:
			case EnumChunkRenderPass.Liquid:
				return false;
			case EnumChunkRenderPass.Transparent:
			case EnumChunkRenderPass.TopSoil:
			case EnumChunkRenderPass.Meta:
				flag = false;
				break;
			}
			bool flag2 = true;
			EnumChunkRenderPass renderPass = voxelMaterial2.RenderPass;
			if ((uint)(renderPass - 2) <= 1u || (uint)(renderPass - 5) <= 1u)
			{
				flag2 = false;
			}
			if (flag && flag2)
			{
				return true;
			}
			if (flag)
			{
				return false;
			}
			return flag2 | voxelMaterial.CullBetweenTransparents;
		}
		return false;
	}

	private static void ConvertPlaneXImpl(int width, int height, out float sx, out float sy, out float sz)
	{
		sx = 1f / 32f;
		sy = (float)height * (1f / 32f);
		sz = (float)width * (1f / 32f);
	}

	private static void ConvertPlaneYImpl(int width, int height, out float sx, out float sy, out float sz)
	{
		sx = (float)width * (1f / 32f);
		sy = 1f / 32f;
		sz = (float)height * (1f / 32f);
	}

	private static void ConvertPlaneZImpl(int width, int height, out float sx, out float sy, out float sz)
	{
		sx = (float)width * (1f / 32f);
		sy = (float)height * (1f / 32f);
		sz = 1f / 32f;
	}

	public static void UpdateNeighbors(BlockEntityMicroBlock bm)
	{
		if (bm.Api != null && bm.Api.Side == EnumAppSide.Client)
		{
			BlockPos pos = bm.Pos;
			IWorldAccessor world = bm.Api.World;
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			for (int i = 0; i < aLLFACES.Length; i++)
			{
				aLLFACES[i].IterateThruFacingOffsets(pos);
				UpdateNeighbour(world, pos);
			}
			BlockFacing.FinishIteratingAllFaces(pos);
		}
	}

	private static void UpdateNeighbour(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock { BlockIds: not null, VoxelCuboids: not null } blockEntityMicroBlock)
		{
			blockEntityMicroBlock.MarkMeshDirty();
			blockEntityMicroBlock.MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		if (replaceBlocks != null)
		{
			if (BlockName != null && BlockName.Length > 0 && GetPlacedBlockName(api, VoxelCuboids, BlockIds, null) == BlockName)
			{
				BlockName = null;
			}
			for (int i = 0; i < BlockIds.Length; i++)
			{
				if (replaceBlocks.TryGetValue(BlockIds[i], out var value) && value.TryGetValue(centerrockblockid, out var value2))
				{
					BlockIds[i] = blockAccessor.GetBlock(value2).Id;
				}
			}
		}
		if (!resolveImports)
		{
			return;
		}
		int num = -1;
		int num2 = BlockIds.Length;
		for (int j = 0; j < num2; j++)
		{
			if (BlockIds[j] != BlockMicroBlock.BlockLayerMetaBlockId)
			{
				continue;
			}
			for (int k = 0; k < VoxelCuboids.Count; k++)
			{
				if (((VoxelCuboids[k] >> 24) & 0xFF) != j)
				{
					continue;
				}
				if (layerBlock == null)
				{
					VoxelCuboids.RemoveAt(k);
					k--;
					continue;
				}
				if (num < 0)
				{
					BlockIds = BlockIds.Append(layerBlock.Id);
					num = BlockIds.Length - 1;
				}
				VoxelCuboids[k] = (VoxelCuboids[k] & 0xFFFFFF) | (uint)(num << 24);
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (Mesh == null)
		{
			Mesh = GenMesh();
		}
		base.OnTesselation(mesher, tesselator);
		if (withColorMapData)
		{
			ColorMapData colorMapData = (Api as ICoreClientAPI).World.GetColorMapData(Api.World.Blocks[BlockIds[0]], Pos.X, Pos.Y, Pos.Z);
			mesher.AddMeshData(Mesh, colorMapData);
		}
		else
		{
			mesher.AddMeshData(Mesh);
		}
		base.Block = Api.World.BlockAccessor.GetBlock(Pos);
		return true;
	}

	public bool CanAccept(Block decorBlock)
	{
		JsonObject attributes = decorBlock.Attributes;
		if (attributes == null)
		{
			return true;
		}
		return attributes["chiselBlockAttachable"]?.AsBool(defaultValue: true) != false;
	}

	public void SetDecor(Block blockToPlace, BlockFacing face)
	{
		if (DecorIds == null)
		{
			DecorIds = new int[6];
		}
		int num = (face.IsVertical ? face.Index : BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(face.HorizontalAngleIndex - rotationY / 90, 4)].Index);
		DecorIds[num] = blockToPlace.Id;
		MarkDirty(redrawOnClient: true);
	}

	public int GetDecor(BlockFacing face)
	{
		if (DecorIds == null)
		{
			return 0;
		}
		int num = (face.IsVertical ? face.Index : BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(face.HorizontalAngleIndex - rotationY / 90, 4)].Index);
		return DecorIds[num];
	}

	public bool ExchangeWith(ItemSlot fromSlot, ItemSlot toSlot)
	{
		Block block = fromSlot.Itemstack?.Block;
		Block block2 = toSlot.Itemstack?.Block;
		if (block == null || block2 == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < BlockIds.Length; i++)
		{
			if (BlockIds[i] == block.Id)
			{
				BlockIds[i] = block2.Id;
				result = true;
			}
		}
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IMaterialExchangeable materialExchangeable)
			{
				materialExchangeable.ExchangeWith(fromSlot, toSlot);
			}
		}
		RegenSelectionBoxes(Api.World, null);
		MarkDirty(redrawOnClient: true);
		return result;
	}
}
