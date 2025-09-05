using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityTapestry : BlockEntity
{
	private bool rotten;

	private bool preserveType;

	private bool preserve;

	private string type;

	private MeshData meshData;

	private bool needsToDie;

	private bool didInitialize;

	public string Type => type;

	public bool Rotten => rotten;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		didInitialize = true;
		if (needsToDie)
		{
			RegisterDelayedCallback(delegate
			{
				api.World.BlockAccessor.SetBlock(0, Pos);
			}, 50);
		}
		else if (type != null)
		{
			genMesh();
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			type = byItemStack.Attributes?.GetString("type");
			preserveType = byItemStack.Attributes?.GetBool("preserveType") ?? false;
			preserve = byItemStack.Attributes?.GetBool("preserve") ?? false;
		}
		genMesh();
	}

	private void genMesh()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			int rotVariant = 1 + GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 4);
			meshData = (base.Block as BlockTapestry)?.genMesh(rotten, type, rotVariant).Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * ((float)Math.PI / 180f), 0f);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		rotten = tree.GetBool("rotten");
		preserveType = tree.GetBool("preserveType");
		preserve = tree.GetBool("preserve");
		type = tree.GetString("type");
		if (worldForResolving.Side == EnumAppSide.Client && Api != null && type != null)
		{
			genMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("rotten", rotten);
		tree.SetBool("preserveType", preserveType);
		tree.SetBool("preserve", preserve);
		tree.SetString("type", type);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		if (!resolveImports || preserve)
		{
			return;
		}
		bool flag = base.Block.FirstCodePart() == "wallcarving";
		if (!preserveType)
		{
			bool flag2 = false;
			double num = (double)(uint)schematicSeed / 4294967295.0;
			string[][] array = (flag ? BlockTapestry.wallcarvingGroups : BlockTapestry.tapestryGroups);
			int num2 = 0;
			while (!flag2 && num2 < array.Length)
			{
				int num3 = 0;
				while (!flag2 && num3 < array[num2].Length)
				{
					if (array[num2][num3] == type)
					{
						int num4 = GameMath.oaatHashMany(schematicSeed + ((num2 >= 3) ? 87987 : 0), 20);
						num = (double)GameMath.Mod((uint)(schematicSeed + num4), uint.MaxValue) / 4294967295.0;
						int n = array[num2].Length;
						int k = GameMath.oaatHashMany(num3 + schematicSeed, 20);
						type = array[num2][GameMath.Mod(k, n)];
						flag2 = true;
					}
					num3++;
				}
				num2++;
			}
			if (num < 0.6)
			{
				needsToDie = true;
				if (didInitialize)
				{
					Api.World.BlockAccessor.SetBlock(0, Pos);
				}
				return;
			}
		}
		if (!flag)
		{
			rotten = worldForNewMappings.Rand.NextDouble() < 0.75;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(meshData);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Rotten)
		{
			dsc.AppendLine(Lang.Get("Will fall apart when broken"));
		}
	}
}
