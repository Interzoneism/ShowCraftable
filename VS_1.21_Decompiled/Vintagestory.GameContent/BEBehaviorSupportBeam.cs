using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BEBehaviorSupportBeam : BlockEntityBehavior, IRotatable, IMaterialExchangeable
{
	public PlacedBeam[] Beams;

	private ModSystemSupportBeamPlacer sbp;

	private Cuboidf[] collBoxes;

	private Cuboidf[] selBoxes;

	private bool dropWhenBroken;

	public BEBehaviorSupportBeam(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		sbp = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>();
		if (Beams != null)
		{
			PlacedBeam[] beams = Beams;
			foreach (PlacedBeam placedBeam in beams)
			{
				placedBeam.Block = Api.World.GetBlock(placedBeam.BlockId);
			}
		}
		dropWhenBroken = properties?["dropWhenBroken"].AsBool(defaultValue: true) ?? true;
	}

	public void AddBeam(Vec3f start, Vec3f end, BlockFacing onFacing, Block block)
	{
		if (Beams == null)
		{
			Beams = Array.Empty<PlacedBeam>();
		}
		Beams = Beams.Append(new PlacedBeam
		{
			Start = start.Clone(),
			End = end.Clone(),
			FacingIndex = onFacing.Index,
			BlockId = block.Id,
			Block = block
		});
		collBoxes = null;
		sbp.OnBeamAdded(start.ToVec3d().Add(base.Pos), end.ToVec3d().Add(base.Pos));
	}

	public Cuboidf[] GetCollisionBoxes()
	{
		if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible is BlockSupportBeam)
		{
			return null;
		}
		if (Beams == null)
		{
			return null;
		}
		if (collBoxes != null)
		{
			return collBoxes;
		}
		float num = 1f / 6f;
		Cuboidf[] array = new Cuboidf[Beams.Length];
		for (int i = 0; i < Beams.Length; i++)
		{
			PlacedBeam placedBeam = Beams[i];
			Cuboidf cuboidf = (array[i] = new Cuboidf(placedBeam.Start.X - num, placedBeam.Start.Y - num, placedBeam.Start.Z - num, placedBeam.Start.X + num, placedBeam.Start.Y + num, placedBeam.Start.Z + num));
			for (int j = 0; j < 3; j++)
			{
				if (cuboidf[j] < 0f)
				{
					cuboidf[j] = 0f - num;
					cuboidf[j + 3] = num;
				}
				if (cuboidf[j] > 1f)
				{
					cuboidf[j] = 1f - num;
					cuboidf[j + 3] = 1f + num;
				}
			}
		}
		return collBoxes = array;
	}

	public Cuboidf[] GetSelectionBoxes()
	{
		if (selBoxes != null)
		{
			return selBoxes;
		}
		Cuboidf[] collisionBoxes = GetCollisionBoxes();
		if (collisionBoxes == null)
		{
			return null;
		}
		float num = 1f / 6f;
		for (int i = 0; i < collisionBoxes.Length; i++)
		{
			Cuboidf cuboidf = collisionBoxes[i].Clone();
			for (int j = 0; j < 3; j++)
			{
				if (cuboidf[j] < 0f)
				{
					cuboidf[j] = 0f - num;
					cuboidf[j + 3] = num;
				}
				if (cuboidf[j] > 1f)
				{
					cuboidf[j] = 1f - num;
					cuboidf[j + 3] = 1f + num;
				}
			}
		}
		return selBoxes = collisionBoxes;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		byte[] bytes = tree.GetBytes("beams");
		if (bytes == null)
		{
			return;
		}
		Beams = SerializerUtil.Deserialize<PlacedBeam[]>(bytes);
		if (Api != null && Beams != null)
		{
			PlacedBeam[] beams = Beams;
			foreach (PlacedBeam placedBeam in beams)
			{
				placedBeam.Block = Api.World.GetBlock(placedBeam.BlockId);
			}
		}
		collBoxes = null;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (Beams != null)
		{
			tree.SetBytes("beams", SerializerUtil.Serialize(Beams));
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		if (Beams == null)
		{
			return;
		}
		for (int i = 0; i < Beams.Length; i++)
		{
			if (oldBlockIdMapping.TryGetValue(Beams[i].BlockId, out var value))
			{
				Block block = worldForNewMappings.GetBlock(value);
				if (block == null)
				{
					worldForNewMappings.Logger.Warning("Cannot load support beam block id mapping @ {1}, block code {0} not found block registry. Will not display correctly.", value, Blockentity.Pos);
				}
				else
				{
					Beams[i].BlockId = block.Id;
					Beams[i].Block = block;
				}
			}
			else
			{
				worldForNewMappings.Logger.Warning("Cannot load support beam block id mapping @ {1}, block id {0} not found block registry. Will not display correctly.", Beams[i].BlockId, Blockentity.Pos);
			}
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		if (Beams != null)
		{
			for (int i = 0; i < Beams.Length; i++)
			{
				Block block = Api.World.GetBlock(Beams[i].BlockId);
				blockIdMapping[block.Id] = block.Code;
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (Beams == null)
		{
			return true;
		}
		for (int i = 0; i < Beams.Length; i++)
		{
			MeshData data = genMesh(i, null, null);
			mesher.AddMeshData(data);
		}
		return true;
	}

	public MeshData genMesh(int beamIndex, ITexPositionSource texSource, string texSourceKey)
	{
		BlockPos pos = Blockentity.Pos;
		PlacedBeam placedBeam = Beams[beamIndex];
		MeshData obj = ModSystemSupportBeamPlacer.generateMesh(placedBeam.Start, placedBeam.End, origMeshes: sbp.getOrCreateBeamMeshes(placedBeam.Block, (placedBeam.Block as BlockSupportBeam)?.PartialEnds ?? false, texSource, texSourceKey), facing: BlockFacing.ALLFACES[placedBeam.FacingIndex], slumpPerMeter: placedBeam.SlumpPerMeter);
		float x = (float)GameMath.MurmurHash3Mod(pos.X + beamIndex * 100, pos.Y + beamIndex * 100, pos.Z + beamIndex * 100, 500) / 50000f;
		float y = (float)GameMath.MurmurHash3Mod(pos.X - beamIndex * 100, pos.Y + beamIndex * 100, pos.Z + beamIndex * 100, 500) / 50000f;
		float z = (float)GameMath.MurmurHash3Mod(pos.X + beamIndex * 100, pos.Y + beamIndex * 100, pos.Z - beamIndex * 100, 500) / 50000f;
		obj.Translate(x, y, z);
		return obj;
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		FromTreeAttributes(tree, null);
		if (Beams == null)
		{
			return;
		}
		if (degreeRotation != 0)
		{
			Matrixf matrixf = new Matrixf();
			matrixf.Translate(0.5f, 0.5f, 0.5f);
			matrixf.RotateYDeg(-degreeRotation);
			matrixf.Translate(-0.5f, -0.5f, -0.5f);
			Vec4f vec4f = new Vec4f();
			vec4f.W = 1f;
			PlacedBeam[] beams = Beams;
			foreach (PlacedBeam placedBeam in beams)
			{
				vec4f.X = placedBeam.Start.X;
				vec4f.Y = placedBeam.Start.Y;
				vec4f.Z = placedBeam.Start.Z;
				Vec4f vec4f2 = matrixf.TransformVector(vec4f);
				placedBeam.Start.X = vec4f2.X;
				placedBeam.Start.Y = vec4f2.Y;
				placedBeam.Start.Z = vec4f2.Z;
				vec4f.X = placedBeam.End.X;
				vec4f.Y = placedBeam.End.Y;
				vec4f.Z = placedBeam.End.Z;
				vec4f2 = matrixf.TransformVector(vec4f);
				placedBeam.End.X = vec4f2.X;
				placedBeam.End.Y = vec4f2.Y;
				placedBeam.End.Z = vec4f2.Z;
			}
		}
		else if (flipAxis.HasValue)
		{
			PlacedBeam[] beams = Beams;
			foreach (PlacedBeam placedBeam2 in beams)
			{
				switch (flipAxis)
				{
				case EnumAxis.X:
					placedBeam2.Start.X = placedBeam2.Start.X * -1f + 1f;
					placedBeam2.End.X = placedBeam2.End.X * -1f + 1f;
					break;
				case EnumAxis.Y:
					placedBeam2.Start.Y = placedBeam2.Start.Y * -1f + 1f;
					placedBeam2.End.Y = placedBeam2.End.Y * -1f + 1f;
					break;
				case EnumAxis.Z:
					placedBeam2.Start.Z = placedBeam2.Start.Z * -1f + 1f;
					placedBeam2.End.Z = placedBeam2.End.Z * -1f + 1f;
					break;
				default:
					throw new ArgumentOutOfRangeException("flipAxis", flipAxis, null);
				}
			}
		}
		ToTreeAttributes(tree);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (Beams != null && sbp != null)
		{
			for (int num = Beams.Length - 1; num >= 0; num--)
			{
				BreakBeam(num, byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative);
			}
		}
		base.OnBlockBroken(byPlayer);
	}

	public void BreakBeam(int beamIndex, bool drop = true)
	{
		if (beamIndex >= 0 && beamIndex < Beams.Length)
		{
			PlacedBeam placedBeam = Beams[beamIndex];
			if (drop && dropWhenBroken)
			{
				Api.World.SpawnItemEntity(new ItemStack(placedBeam.Block, (int)Math.Ceiling(placedBeam.End.DistanceTo(placedBeam.Start))), base.Pos);
			}
			sbp.OnBeamRemoved(placedBeam.Start.ToVec3d().Add(base.Pos), placedBeam.End.ToVec3d().Add(base.Pos));
			Beams = Beams.RemoveAt(beamIndex);
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	public bool ExchangeWith(ItemSlot fromSlot, ItemSlot toSlot)
	{
		if (Beams == null || Beams.Length == 0)
		{
			return false;
		}
		Block block = fromSlot.Itemstack.Block;
		Block block2 = toSlot.Itemstack.Block;
		bool result = false;
		PlacedBeam[] beams = Beams;
		foreach (PlacedBeam placedBeam in beams)
		{
			if (placedBeam.BlockId == block.Id)
			{
				placedBeam.Block = block2;
				placedBeam.BlockId = block2.Id;
				result = true;
			}
		}
		Blockentity.MarkDirty(redrawOnClient: true);
		return result;
	}
}
