using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSmoothTextureTransition : Block
{
	private Dictionary<string, MeshData> sludgeMeshByNESWFlag;

	private ICoreClientAPI capi;

	public Dictionary<string, HashSet<string>> cornersByNEWSFlag = new Dictionary<string, HashSet<string>>
	{
		{
			"flat",
			new HashSet<string>(new string[4] { "wn", "ne", "es", "sw" })
		},
		{
			"n",
			new HashSet<string>(new string[2] { "es", "sw" })
		},
		{
			"e",
			new HashSet<string>(new string[2] { "wn", "sw" })
		},
		{
			"s",
			new HashSet<string>(new string[2] { "wn", "ne" })
		},
		{
			"w",
			new HashSet<string>(new string[2] { "ne", "es" })
		},
		{
			"sw",
			new HashSet<string>(new string[1] { "ne" })
		},
		{
			"nw",
			new HashSet<string>(new string[1] { "se" })
		},
		{
			"ne",
			new HashSet<string>(new string[1] { "sw" })
		},
		{
			"es",
			new HashSet<string>(new string[1] { "nw" })
		}
	};

	private string[] cornerCodes = new string[4] { "wn", "ne", "es", "sw" };

	private int[] cornerOffest = new int[4]
	{
		-1 * TileSideEnum.MoveIndex[1] - TileSideEnum.MoveIndex[2],
		TileSideEnum.MoveIndex[1] - TileSideEnum.MoveIndex[2],
		TileSideEnum.MoveIndex[1] + TileSideEnum.MoveIndex[2],
		-1 * TileSideEnum.MoveIndex[1] + TileSideEnum.MoveIndex[2]
	};

	public override void OnLoaded(ICoreAPI api)
	{
		capi = api as ICoreClientAPI;
		base.OnLoaded(api);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (sludgeMeshByNESWFlag == null)
		{
			genMeshes();
		}
		string text = getNESWFlag(chunkExtBlocks, extIndex3d);
		if (cornersByNEWSFlag.TryGetValue(text, out var value))
		{
			string cornerFlags = getCornerFlags(value, chunkExtBlocks, extIndex3d);
			if (cornerFlags.Length > 0)
			{
				text = text + "-cornercut-" + cornerFlags;
			}
		}
		if (sludgeMeshByNESWFlag.TryGetValue(text, out var value2))
		{
			sourceMesh = value2;
		}
		else
		{
			base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);
		}
	}

	private string getCornerFlags(HashSet<string> corners, Block[] chunkExtBlocks, int extIndex3d)
	{
		string text = "";
		for (int i = 0; i < cornerOffest.Length; i++)
		{
			if (corners.Contains(cornerCodes[i]) && !chunkExtBlocks[extIndex3d + cornerOffest[i]].SideSolid[BlockFacing.UP.Index])
			{
				text += cornerCodes[i];
			}
		}
		return text;
	}

	private void genMeshes()
	{
		sludgeMeshByNESWFlag = new Dictionary<string, MeshData>();
		foreach (KeyValuePair<string, CompositeShape> item in Attributes["shapeByOrient"].AsObject<Dictionary<string, CompositeShape>>())
		{
			Shape shape = capi.Assets.TryGet(item.Value.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
			if (shape == null)
			{
				api.Logger.Warning("Smooth texture transition shape for block {0}: Shape {1} not found. Block will be invisible.", Code.Path, item.Value.Base);
			}
			else
			{
				capi.Tesselator.TesselateShape(this, shape, out var modeldata, new Vec3f(item.Value.rotateX, item.Value.rotateY, item.Value.rotateZ));
				sludgeMeshByNESWFlag[item.Key] = modeldata;
			}
		}
	}

	public string getNESWFlag(Block[] chunkExtBlocks, int extIndex3d)
	{
		string text = "";
		for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			int num = blockFacing.Normali.X * TileSideEnum.MoveIndex[1] + blockFacing.Normali.Z * TileSideEnum.MoveIndex[2];
			if (!chunkExtBlocks[extIndex3d + num].SideSolid[BlockFacing.UP.Index])
			{
				text += blockFacing.Code[0];
			}
		}
		if (text.Length != 0)
		{
			return text;
		}
		return "flat";
	}
}
