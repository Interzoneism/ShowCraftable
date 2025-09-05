using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityMycelium : BlockEntity
{
	private Vec3i[] grownMushroomOffsets = Array.Empty<Vec3i>();

	private double mushroomsGrownTotalDays;

	private double mushroomsDiedTotalDays = -999999.0;

	private double mushroomsGrowingDays;

	private double lastUpdateTotalDays;

	private AssetLocation mushroomBlockCode;

	private MushroomProps props;

	private Block mushroomBlock;

	private double fruitingDays = 20.0;

	private double growingDays = 20.0;

	private int growRange = 7;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		int num = 10000;
		RegisterGameTickListener(onServerTick, num, -api.World.Rand.Next(num));
		if (mushroomBlockCode != null && !setMushroomBlock(Api.World.GetBlock(mushroomBlockCode)))
		{
			api.Logger.Error("Invalid mycelium mushroom type '{0}' at {1}. Will delete block entity.", mushroomBlockCode, Pos);
			Api.Event.EnqueueMainThreadTask(delegate
			{
				Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			}, "deletemyceliumBE");
		}
	}

	private void onServerTick(float dt)
	{
		bool flag = grownMushroomOffsets.Length != 0;
		if (flag && props.DieWhenTempBelow > -99f && Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature < props.DieWhenTempBelow)
		{
			DestroyGrownMushrooms();
		}
		else if (props.DieAfterFruiting && flag && mushroomsGrownTotalDays + fruitingDays < Api.World.Calendar.TotalDays)
		{
			DestroyGrownMushrooms();
		}
		else if (!flag)
		{
			lastUpdateTotalDays = Math.Max(lastUpdateTotalDays, Api.World.Calendar.TotalDays - 50.0);
			while (Api.World.Calendar.TotalDays - lastUpdateTotalDays > 1.0)
			{
				if (Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastUpdateTotalDays + 0.5).Temperature > 5f)
				{
					mushroomsGrowingDays += Api.World.Calendar.TotalDays - lastUpdateTotalDays;
				}
				lastUpdateTotalDays += 1.0;
			}
			if (mushroomsGrowingDays > growingDays)
			{
				growMushrooms(Api.World.BlockAccessor, MyceliumSystem.rndn);
				mushroomsGrowingDays = 0.0;
			}
		}
		else
		{
			if (!(Api.World.Calendar.TotalDays - lastUpdateTotalDays > 0.1))
			{
				return;
			}
			lastUpdateTotalDays = Api.World.Calendar.TotalDays;
			for (int i = 0; i < grownMushroomOffsets.Length; i++)
			{
				Vec3i vector = grownMushroomOffsets[i];
				BlockPos pos = Pos.AddCopy(vector);
				if (Api.World.BlockAccessor.GetChunkAtBlockPos(pos) == null)
				{
					break;
				}
				if (!Api.World.BlockAccessor.GetBlock(pos).Code.Equals(mushroomBlockCode))
				{
					grownMushroomOffsets = grownMushroomOffsets.RemoveAt(i);
					i--;
				}
			}
		}
	}

	public void Regrow()
	{
		DestroyGrownMushrooms();
		growMushrooms(Api.World.BlockAccessor, MyceliumSystem.rndn);
	}

	private void DestroyGrownMushrooms()
	{
		mushroomsDiedTotalDays = Api.World.Calendar.TotalDays;
		Vec3i[] array = grownMushroomOffsets;
		foreach (Vec3i vector in array)
		{
			BlockPos pos = Pos.AddCopy(vector);
			if (Api.World.BlockAccessor.GetBlock(pos).Variant["mushroom"] == mushroomBlock.Variant["mushroom"])
			{
				Api.World.BlockAccessor.SetBlock(0, pos);
			}
		}
		grownMushroomOffsets = Array.Empty<Vec3i>();
	}

	private bool setMushroomBlock(Block block)
	{
		mushroomBlock = block;
		mushroomBlockCode = block?.Code;
		if (block != null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Server)
			{
				JsonObject attributes = block.Attributes;
				if (attributes == null || !attributes["mushroomProps"].Exists)
				{
					return false;
				}
				props = block.Attributes["mushroomProps"].AsObject<MushroomProps>();
				MyceliumSystem.lcgrnd.InitPositionSeed(mushroomBlockCode.GetHashCode(), (int)(Api.World.Calendar.GetHemisphere(Pos) + 5));
				fruitingDays = 20.0 + MyceliumSystem.lcgrnd.NextDouble() * 20.0;
				growingDays = 10.0 + MyceliumSystem.lcgrnd.NextDouble() * 10.0;
				return true;
			}
		}
		return false;
	}

	public void OnGenerated(IBlockAccessor blockAccessor, IRandom rnd, BlockMushroom block)
	{
		setMushroomBlock(block);
		MyceliumSystem.lcgrnd.InitPositionSeed(mushroomBlockCode.GetHashCode(), (int)(mushroomBlock as BlockMushroom).Api.World.Calendar.GetHemisphere(Pos));
		if (MyceliumSystem.lcgrnd.NextDouble() < 0.33)
		{
			mushroomsGrowingDays = MyceliumSystem.lcgrnd.NextDouble() * 10.0;
		}
		else
		{
			growMushrooms(blockAccessor, rnd);
		}
	}

	private void growMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		if (mushroomBlock.Variant.ContainsKey("side"))
		{
			generateSideGrowingMushrooms(blockAccessor, rnd);
		}
		else
		{
			generateUpGrowingMushrooms(blockAccessor, rnd);
		}
		mushroomsGrownTotalDays = (mushroomBlock as BlockMushroom).Api.World.Calendar.TotalDays - rnd.NextDouble() * fruitingDays;
	}

	private void generateUpGrowingMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		if (mushroomBlock == null)
		{
			return;
		}
		int num = 2 + rnd.NextInt(11);
		BlockPos blockPos = new BlockPos();
		List<Vec3i> list = new List<Vec3i>();
		if (!isChunkAreaLoaded(blockAccessor, growRange))
		{
			return;
		}
		while (num-- > 0)
		{
			int num2 = growRange - rnd.NextInt(2 * growRange + 1);
			int num3 = growRange - rnd.NextInt(2 * growRange + 1);
			blockPos.Set(Pos.X + num2, 0, Pos.Z + num3);
			IMapChunk mapChunkAtBlockPos = blockAccessor.GetMapChunkAtBlockPos(blockPos);
			if (mapChunkAtBlockPos != null)
			{
				int num4 = GameMath.Mod(blockPos.X, 32);
				int num5 = GameMath.Mod(blockPos.Z, 32);
				blockPos.Y = mapChunkAtBlockPos.WorldGenTerrainHeightMap[num5 * 32 + num4] + 1;
				Block block = blockAccessor.GetBlock(blockPos);
				if (blockAccessor.GetBlockBelow(blockPos).Fertility >= 10 && block.LiquidCode == null && ((mushroomsGrownTotalDays == 0.0 && block.Replaceable >= 6000) || block.Id == 0))
				{
					blockAccessor.SetBlock(mushroomBlock.Id, blockPos);
					list.Add(new Vec3i(num2, blockPos.Y - Pos.Y, num3));
				}
			}
		}
		grownMushroomOffsets = list.ToArray();
	}

	private bool isChunkAreaLoaded(IBlockAccessor blockAccessor, int growRange)
	{
		int num = (Pos.X - growRange) / 32;
		int num2 = (Pos.X + growRange) / 32;
		int num3 = (Pos.Z - growRange) / 32;
		int num4 = (Pos.Z + growRange) / 32;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				if (blockAccessor.GetChunk(i, Pos.InternalY / 32, j) == null)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void generateSideGrowingMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		int num = 1 + rnd.NextInt(5);
		BlockPos blockPos = new BlockPos();
		List<Vec3i> list = new List<Vec3i>();
		while (num-- > 0)
		{
			int num2 = 0;
			int num3 = rnd.NextInt(5) - 2;
			int num4 = 0;
			blockPos.Set(Pos.X + num2, Pos.Y + num3, Pos.Z + num4);
			Block block = blockAccessor.GetBlock(blockPos);
			if (!(block is BlockLog) || block.Variant["type"] == "resin")
			{
				continue;
			}
			BlockFacing blockFacing = null;
			int num5 = rnd.NextInt(4);
			for (int i = 0; i < 4; i++)
			{
				BlockFacing blockFacing2 = BlockFacing.HORIZONTALS[(i + num5) % 4];
				blockPos.Set(Pos.X + num2, Pos.Y + num3, Pos.Z + num4).Add(blockFacing2);
				if (blockAccessor.GetBlock(blockPos).Id == 0)
				{
					blockFacing = blockFacing2.Opposite;
					break;
				}
			}
			if (blockFacing != null)
			{
				Block block2 = blockAccessor.GetBlock(mushroomBlock.CodeWithVariant("side", blockFacing.Code));
				blockAccessor.SetBlock(block2.Id, blockPos);
				list.Add(new Vec3i(blockPos.X - Pos.X, blockPos.Y - Pos.Y, blockPos.Z - Pos.Z));
			}
		}
		grownMushroomOffsets = list.ToArray();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		mushroomBlockCode = new AssetLocation(tree.GetString("mushroomBlockCode"));
		grownMushroomOffsets = tree.GetVec3is("grownMushroomOffsets");
		mushroomsGrownTotalDays = tree.GetDouble("mushromsGrownTotalDays");
		mushroomsDiedTotalDays = tree.GetDouble("mushroomsDiedTotalDays");
		lastUpdateTotalDays = tree.GetDouble("lastUpdateTotalDays");
		mushroomsGrowingDays = tree.GetDouble("mushroomsGrowingDays");
		setMushroomBlock(worldAccessForResolve.GetBlock(mushroomBlockCode));
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("mushroomBlockCode", mushroomBlockCode.ToShortString());
		tree.SetVec3is("grownMushroomOffsets", grownMushroomOffsets);
		tree.SetDouble("mushromsGrownTotalDays", mushroomsGrownTotalDays);
		tree.SetDouble("mushroomsDiedTotalDays", mushroomsDiedTotalDays);
		tree.SetDouble("lastUpdateTotalDays", lastUpdateTotalDays);
		tree.SetDouble("mushroomsGrowingDays", mushroomsGrowingDays);
	}
}
