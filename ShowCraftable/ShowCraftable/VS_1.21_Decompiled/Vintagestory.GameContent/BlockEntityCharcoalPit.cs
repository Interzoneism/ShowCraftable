using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCharcoalPit : BlockEntity
{
	protected ActionBoolReturn<BlockPos, BlockPos, BlockFacing, IWorldChunk> defaultCheckAction;

	private static float BurnHours = 18f;

	protected Dictionary<BlockPos, int> smokeLocations = new Dictionary<BlockPos, int>();

	protected double finishedAfterTotalHours;

	protected double startingAfterTotalHours;

	protected EnumCharcoalPitState state;

	protected string startedByPlayerUid = string.Empty;

	public int charcoalPitId;

	public int fireBlockId;

	public int[] charcoalPileId = new int[8];

	public bool Lit { get; protected set; }

	public virtual int MaxSize { get; set; } = 11;

	protected virtual float PitEfficiency { get; set; } = 1f;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		MaxSize = base.Block.Attributes?["maxSize"].AsInt(11) ?? 11;
		charcoalPitId = base.Block.BlockId;
		fireBlockId = Api.World.GetBlock(new AssetLocation("fire")).BlockId;
		for (int i = 0; i < 8; i++)
		{
			charcoalPileId[i] = Api.World.GetBlock(new AssetLocation("charcoalpile-" + (i + 1))).BlockId;
		}
		defaultCheckAction = (BlockPos bpos, BlockPos npos, BlockFacing facing, IWorldChunk chunk) => BlockFirepit.IsFirewoodPile(Api.World, npos) ? true : false;
		if (api.Side == EnumAppSide.Client)
		{
			if (Lit)
			{
				UpdateSmokeLocations();
			}
			RegisterGameTickListener(OnClientTick, 150);
		}
		else
		{
			RegisterGameTickListener(OnServerTick, 3000);
		}
	}

	protected virtual void OnClientTick(float dt)
	{
		if (state != EnumCharcoalPitState.Sealed)
		{
			return;
		}
		AdvancedParticleProperties[] array = base.Block?.ParticleProperties;
		if (array == null)
		{
			return;
		}
		BlockPos blockPos = new BlockPos();
		foreach (KeyValuePair<BlockPos, int> smokeLocation in smokeLocations)
		{
			if (Api.World.Rand.NextDouble() < 0.20000000298023224 && array.Length != 0)
			{
				blockPos.Set(smokeLocation.Key.X, smokeLocation.Value + 1, smokeLocation.Key.Z);
				Block block = Api.World.BlockAccessor.GetBlock(blockPos);
				AdvancedParticleProperties advancedParticleProperties = array[0];
				advancedParticleProperties.basePos = BEBehaviorBurning.RandomBlockPos(Api.World.BlockAccessor, blockPos, block, BlockFacing.UP);
				advancedParticleProperties.Quantity.avg = 1f;
				Api.World.SpawnParticles(advancedParticleProperties);
				advancedParticleProperties.Quantity.avg = 0f;
			}
		}
	}

	protected virtual void OnServerTick(float dt)
	{
		if (!Lit)
		{
			return;
		}
		if (startingAfterTotalHours <= Api.World.Calendar.TotalHours && state == EnumCharcoalPitState.Warmup)
		{
			finishedAfterTotalHours = Api.World.Calendar.TotalHours + (double)BurnHours;
			state = EnumCharcoalPitState.Sealed;
			MarkDirty();
		}
		if (state == EnumCharcoalPitState.Warmup)
		{
			return;
		}
		HashSet<BlockPos> hashSet = FindHolesInPit();
		if (hashSet == null)
		{
			return;
		}
		EnumCharcoalPitState enumCharcoalPitState = state;
		if (hashSet.Count > 0)
		{
			state = EnumCharcoalPitState.Unsealed;
			finishedAfterTotalHours = Api.World.Calendar.TotalHours + (double)BurnHours;
			float num = Math.Clamp(1f - 0.1f * (float)(hashSet.Count - 1), 0.5f, 1f);
			foreach (BlockPos item in hashSet)
			{
				BlockPos blockPos = item.Copy();
				Block block = Api.World.BlockAccessor.GetBlock(item);
				EntityPlayer entityPlayer = Api.World.PlayerByUid(startedByPlayerUid)?.Entity ?? Api.World.NearestPlayer(Pos.X, Pos.InternalY, Pos.Z)?.Entity;
				IIgnitable ignitable = block.GetInterface<IIgnitable>(Api.World, item);
				bool flag = entityPlayer != null;
				bool flag2;
				if (flag)
				{
					EnumIgniteState? enumIgniteState = ignitable?.OnTryIgniteBlock(entityPlayer, item, 10f);
					if (enumIgniteState.HasValue)
					{
						EnumIgniteState valueOrDefault = enumIgniteState.GetValueOrDefault();
						if ((uint)(valueOrDefault - 2) <= 1u)
						{
							flag2 = true;
							goto IL_01dc;
						}
					}
					flag2 = false;
					goto IL_01dc;
				}
				goto IL_01e0;
				IL_01e0:
				if (flag)
				{
					if (Api.World.Rand.NextDouble() < (double)num)
					{
						EnumHandling handling = EnumHandling.PassThrough;
						ignitable.OnTryIgniteBlockOver(entityPlayer, item, 10f, ref handling);
					}
				}
				else
				{
					if (block.BlockId == 0 || block.BlockId == charcoalPitId)
					{
						continue;
					}
					BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
					foreach (BlockFacing blockFacing in aLLFACES)
					{
						blockFacing.IterateThruFacingOffsets(blockPos);
						if (Api.World.BlockAccessor.GetBlock(blockPos).BlockId == 0 && Api.World.Rand.NextDouble() < (double)num)
						{
							Api.World.BlockAccessor.SetBlock(fireBlockId, blockPos);
							Api.World.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(blockFacing, startedByPlayerUid);
						}
					}
				}
				continue;
				IL_01dc:
				flag = flag2;
				goto IL_01e0;
			}
			MarkDirty();
		}
		else
		{
			state = EnumCharcoalPitState.Sealed;
			if (enumCharcoalPitState != state)
			{
				MarkDirty();
			}
			if (finishedAfterTotalHours <= Api.World.Calendar.TotalHours)
			{
				ConvertPit();
			}
		}
	}

	public void IgniteNow()
	{
		if (!Lit)
		{
			Lit = true;
			startingAfterTotalHours = Api.World.Calendar.TotalHours + 0.5;
			MarkDirty(redrawOnClient: true);
			if (Api.Side == EnumAppSide.Client)
			{
				UpdateSmokeLocations();
			}
		}
	}

	protected bool WalkPit(Action<BlockPos>? bAction, ActionBoolReturn<BlockPos, BlockPos, BlockFacing, IWorldChunk>? nAction)
	{
		HashSet<BlockPos> hashSet = new HashSet<BlockPos>();
		Queue<BlockPos> queue = new Queue<BlockPos>();
		queue.Enqueue(Pos.Copy());
		hashSet.Add(Pos.Copy());
		BlockPos minPos = Pos.Copy();
		BlockPos maxPos = Pos.Copy();
		IWorldChunk worldChunk = null;
		BlockPos pos = Pos;
		BlockPos pos2 = Pos;
		while (queue.Count > 0)
		{
			pos = queue.Dequeue();
			pos2 = pos.Copy();
			bAction?.Invoke(pos);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				blockFacing.IterateThruFacingOffsets(pos2);
				worldChunk = Api.World.BlockAccessor.GetChunkAtBlockPos(pos2);
				if (worldChunk == null)
				{
					return false;
				}
				if ((nAction == null || nAction(pos, pos2, blockFacing, worldChunk)) && InCube(pos2, ref minPos, ref maxPos) && !hashSet.Contains(pos2))
				{
					queue.Enqueue(pos2.Copy());
					hashSet.Add(pos2.Copy());
				}
			}
		}
		return true;
	}

	protected virtual void ConvertPit()
	{
		Dictionary<BlockPos, Vec3i> quantityPerColumn = new Dictionary<BlockPos, Vec3i>();
		NatFloat firewoodEfficiency = NatFloat.createUniform(0.75f, 0.25f);
		BlockPos bposGround = Pos;
		float totalEfficiency = 0f;
		int firewoodQuantity = 0;
		int bposY = 0;
		if (!WalkPit(delegate(BlockPos bpos)
		{
			bposY = bpos.Y;
			bposGround = bpos.DownCopy(bposY);
			if (quantityPerColumn.TryGetValue(bposGround, out Vec3i value))
			{
				value.Y = Math.Min(value.Y, bposY);
				value.Z = Math.Max(value.Z, bposY);
			}
			else
			{
				Vec3i vec3i = (quantityPerColumn[bposGround] = new Vec3i(0, bposY, bposY));
				value = vec3i;
			}
			firewoodQuantity = (base.Block as BlockCharcoalPit)?.GetFirewoodQuantity(Api.World, bpos, ref firewoodEfficiency) ?? 0;
			totalEfficiency = firewoodEfficiency.nextFloat(PitEfficiency);
			value.X += Math.Clamp(GameMath.RoundRandom(Api.World.Rand, (float)firewoodQuantity / 4f * totalEfficiency), 0, 8);
		}, defaultCheckAction))
		{
			return;
		}
		BlockPos blockPos = new BlockPos();
		int num = 0;
		int max = charcoalPileId.Length;
		foreach (KeyValuePair<BlockPos, Vec3i> item in quantityPerColumn)
		{
			blockPos.Set(item.Key.X, item.Value.Y, item.Key.Z);
			num = item.Value.X;
			while (blockPos.Y <= item.Value.Z)
			{
				if (BlockFirepit.IsFirewoodPile(Api.World, blockPos) || blockPos == Pos)
				{
					if (num > 0)
					{
						Api.World.BlockAccessor.SetBlock(charcoalPileId[GameMath.Clamp(num, 0, max) - 1], blockPos);
						num -= 8;
					}
					else
					{
						Api.World.BlockAccessor.SetBlock(0, blockPos);
					}
				}
				blockPos.Up();
			}
		}
	}

	public void Init(IPlayer? player)
	{
		startedByPlayerUid = player?.PlayerUID ?? string.Empty;
	}

	protected virtual HashSet<BlockPos>? FindHolesInPit()
	{
		HashSet<BlockPos> holes = new HashSet<BlockPos>();
		Block nBlock = base.Block;
		bool containsNew = false;
		bool isFirewood = false;
		if (!WalkPit(null, delegate(BlockPos bpos, BlockPos npos, BlockFacing facing, IWorldChunk chunk)
		{
			isFirewood = BlockFirepit.IsFirewoodPile(Api.World, npos);
			containsNew = holes.Contains(npos);
			if (!holes.Contains(bpos) || !containsNew)
			{
				nBlock = chunk.GetLocalBlockAtBlockPos(Api.World, npos);
				if (!isFirewood && nBlock.BlockId != charcoalPitId)
				{
					if (IsCombustible(npos))
					{
						holes.Add(npos.Copy());
						holes.Add(bpos.Copy());
					}
					else if (nBlock.GetLiquidBarrierHeightOnSide(facing.Opposite, npos) != 1f)
					{
						holes.Add(bpos.Copy());
					}
				}
				else if (containsNew && nBlock.BlockId == charcoalPitId)
				{
					holes.Add(bpos);
				}
			}
			return isFirewood ? true : false;
		}))
		{
			return null;
		}
		return holes;
	}

	protected virtual void UpdateSmokeLocations()
	{
		smokeLocations.Clear();
		if (state == EnumCharcoalPitState.Sealed)
		{
			WalkPit(delegate(BlockPos bpos)
			{
				BlockPos key = bpos.DownCopy(bpos.Y);
				smokeLocations.TryGetValue(key, out var value);
				smokeLocations[key] = Math.Max(value, bpos.Y);
			}, defaultCheckAction);
		}
	}

	protected bool InCube(BlockPos npos, ref BlockPos minPos, ref BlockPos maxPos)
	{
		BlockPos blockPos = minPos.Copy();
		BlockPos blockPos2 = maxPos.Copy();
		if (npos.X < blockPos.X)
		{
			blockPos.X = npos.X;
		}
		else if (npos.X > blockPos2.X)
		{
			blockPos2.X = npos.X;
		}
		if (npos.Y < blockPos.Y)
		{
			blockPos.Y = npos.Y;
		}
		else if (npos.Y > blockPos2.Y)
		{
			blockPos2.Y = npos.Y;
		}
		if (npos.Z < blockPos.Z)
		{
			blockPos.Z = npos.Z;
		}
		else if (npos.Z > blockPos2.Z)
		{
			blockPos2.Z = npos.Z;
		}
		if (blockPos2.X - blockPos.X + 1 <= MaxSize && blockPos2.Y - blockPos.Y + 1 <= MaxSize && blockPos2.Z - blockPos.Z + 1 <= MaxSize)
		{
			minPos = blockPos.Copy();
			maxPos = blockPos2.Copy();
			return true;
		}
		return false;
	}

	protected bool IsCombustible(BlockPos pos)
	{
		Block block = Api.World.BlockAccessor.GetBlock(pos);
		CombustibleProperties combustibleProps = block.CombustibleProps;
		if (combustibleProps != null)
		{
			return combustibleProps.BurnDuration > 0f;
		}
		ICombustible combustible = block.GetInterface<ICombustible>(Api.World, pos);
		if (combustible != null)
		{
			return combustible.GetBurnDuration(Api.World, pos) > 0f;
		}
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		EnumCharcoalPitState num = state;
		bool lit = Lit;
		base.FromTreeAttributes(tree, worldForResolving);
		finishedAfterTotalHours = tree.GetDouble("finishedAfterTotalHours");
		startingAfterTotalHours = tree.GetDouble("startingAfterTotalHours");
		state = (EnumCharcoalPitState)tree.GetInt("state");
		startedByPlayerUid = tree.GetString("startedByPlayerUid");
		Lit = tree.GetBool("lit", defaultValue: true);
		if (num != state || lit != Lit)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				UpdateSmokeLocations();
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("finishedAfterTotalHours", finishedAfterTotalHours);
		tree.SetDouble("startingAfterTotalHours", startingAfterTotalHours);
		tree.SetInt("state", (int)state);
		tree.SetBool("lit", Lit);
		if (startedByPlayerUid != null)
		{
			tree.SetString("startedByPlayerUid", startedByPlayerUid);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		double num = 60.0 * (startingAfterTotalHours - Api.World.Calendar.TotalHours);
		if (Lit)
		{
			if (num <= 0.0)
			{
				dsc.AppendLine(Lang.Get("Lit."));
				return;
			}
			dsc.AppendLine(Lang.Get("lit-starting", (int)num));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Unlit."));
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!Lit)
		{
			MeshData orCreate = ObjectCacheUtil.GetOrCreate(Api, "litCharcoalMesh", delegate
			{
				((ICoreClientAPI)Api).Tesselator.TesselateShape(base.Block, Shape.TryGet(Api, "shapes/block/wood/firepit/cold-normal.json"), out var modeldata);
				return modeldata;
			});
			mesher.AddMeshData(orCreate);
			return true;
		}
		return false;
	}
}
