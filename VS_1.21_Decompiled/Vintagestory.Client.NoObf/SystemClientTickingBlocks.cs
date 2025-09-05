using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemClientTickingBlocks : ClientSystem
{
	private Vec3i commitedPlayerPosDiv8 = new Vec3i();

	private Queue<TickingBlockData> blockChangedTickers = new Queue<TickingBlockData>();

	private Dictionary<int, TickerMetaData> committedTickers = new Dictionary<int, TickerMetaData>();

	private object committedTickersLock = new object();

	private List<TickingBlockData> currentTickers = new List<TickingBlockData>();

	private Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>> currentAmbientSoundsBySection = new Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>>();

	private Vec3i currentPlayerPosDiv8 = new Vec3i();

	private bool shouldStartScanning;

	private object shouldStartScanningLock = new object();

	private BlockScanState scanState;

	private int scanPosition;

	private int finalScanPosition;

	private static int scanRange = 37;

	private static int scanSize = 2 * scanRange;

	private static int scanSectionSize = scanSize / 8;

	private IBlockAccessor searchBlockAccessor;

	private bool freezeCtBlocks;

	private float offthreadAccum;

	private int currentLeavesCount;

	public override string Name => "ctb";

	public SystemClientTickingBlocks(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, PlayerPosDiv8Changed);
		game.eventManager.OnBlockChanged.Add(OnBlockChanged);
		game.api.eventapi.RegisterAsyncParticleSpawner(onOffThreadParticleTick);
		game.api.ChatCommands.Create("ctblocks").WithDescription("Lets to toggle on/off the updating of client ticking blocks. This can be useful when recording water falls and such").WithArgs(game.api.ChatCommands.Parsers.OptionalBool("freezeCtBlocks"))
			.HandleWith(OnCmdCtBlocks);
		searchBlockAccessor = new BlockAccessorRelaxed(game.WorldMap, game, synchronize: false, relight: false);
		finalScanPosition = scanSize * scanSize * scanSize;
	}

	private TextCommandResult OnCmdCtBlocks(TextCommandCallingArgs args)
	{
		freezeCtBlocks = (bool)args[0];
		return TextCommandResult.Success("Ct block updating now " + (freezeCtBlocks ? "frozen" : "active"));
	}

	public override void OnBlockTexturesLoaded()
	{
		for (int i = 0; i < game.Blocks.Count; i++)
		{
			if (game.Blocks[i] != null)
			{
				game.Blocks[i].DetermineTopMiddlePos();
			}
		}
		game.RegisterCallback(delegate
		{
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = true;
			}
		}, 1000);
		game.RegisterGameTickListener(onTick20Secs, 20000, 123);
	}

	private void onTick20Secs(float dt)
	{
		lock (shouldStartScanningLock)
		{
			shouldStartScanning = true;
		}
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		Block block = game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
		if (block.ShouldReceiveClientParticleTicks(game, game.player, pos, out var isWindAffected))
		{
			int num = commitedPlayerPosDiv8.X * 8 - scanRange;
			int num2 = commitedPlayerPosDiv8.Y * 8 - scanRange;
			int num3 = commitedPlayerPosDiv8.Z * 8 - scanRange;
			int num4 = pos.X - num;
			int num5 = pos.Y - num2;
			int num6 = pos.Z - num3;
			int num7 = num4 | (num5 << 10) | (num6 << 20);
			lock (committedTickersLock)
			{
				if (!committedTickers.ContainsKey(num7))
				{
					blockChangedTickers.Enqueue(new TickingBlockData
					{
						DeltaIndex3d = num7,
						IsWindAffected = isWindAffected,
						WindAffectedNess = (isWindAffected ? SearchWindAffectedNess(pos, game.BlockAccessor) : 0f)
					});
				}
			}
		}
		if (block.Sounds?.Ambient != oldBlock?.Sounds?.Ambient)
		{
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = true;
			}
		}
	}

	private bool onOffThreadParticleTick(float dt, IAsyncParticleManager manager)
	{
		bool flag = false;
		offthreadAccum += dt;
		if (offthreadAccum > 4f)
		{
			offthreadAccum = 0f;
			flag = true;
		}
		Dictionary<int, TickerMetaData> dictionary;
		lock (committedTickersLock)
		{
			dictionary = committedTickers;
			while (blockChangedTickers.Count > 0)
			{
				TickingBlockData tickingBlockData = blockChangedTickers.Dequeue();
				dictionary[tickingBlockData.DeltaIndex3d] = new TickerMetaData
				{
					TickingSinceMs = game.ElapsedMilliseconds,
					IsWindAffected = tickingBlockData.IsWindAffected,
					WindAffectedNess = tickingBlockData.WindAffectedNess
				};
			}
		}
		if (manager.BlockAccess is ICachingBlockAccessor cachingBlockAccessor)
		{
			cachingBlockAccessor.Begin();
		}
		int num = commitedPlayerPosDiv8.X * 8 - scanRange;
		int num2 = commitedPlayerPosDiv8.Y * 8 - scanRange;
		int num3 = commitedPlayerPosDiv8.Z * 8 - scanRange;
		long elapsedMilliseconds = game.ElapsedMilliseconds;
		foreach (KeyValuePair<int, TickerMetaData> item in dictionary)
		{
			BlockPos pos = new BlockPos(num + (item.Key & 0x3FF), num2 + ((item.Key >> 10) & 0x3FF), num3 + ((item.Key >> 20) & 0x3FF));
			if (flag && item.Value.IsWindAffected)
			{
				item.Value.WindAffectedNess = SearchWindAffectedNess(pos, manager.BlockAccess);
			}
			manager.BlockAccess.GetBlock(pos)?.OnAsyncClientParticleTick(manager, pos, item.Value.WindAffectedNess, (float)(elapsedMilliseconds - item.Value.TickingSinceMs) / 1000f);
		}
		return true;
	}

	private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		lock (shouldStartScanningLock)
		{
			shouldStartScanning = true;
			currentPlayerPosDiv8 = newValues.PlayerPosDiv8.ToVec3i();
		}
	}

	public void CommitScan()
	{
		if (freezeCtBlocks)
		{
			currentTickers.Clear();
			return;
		}
		List<AmbientSound> ambientSounds;
		lock (shouldStartScanningLock)
		{
			long elapsedMilliseconds = game.ElapsedMilliseconds;
			Dictionary<int, TickerMetaData> dictionary = new Dictionary<int, TickerMetaData>();
			int num = (currentPlayerPosDiv8.X - commitedPlayerPosDiv8.X) * 8;
			int num2 = (currentPlayerPosDiv8.Y - commitedPlayerPosDiv8.Y) * 8;
			int num3 = (currentPlayerPosDiv8.Z - commitedPlayerPosDiv8.Z) * 8;
			foreach (TickingBlockData currentTicker in currentTickers)
			{
				int num4 = currentTicker.DeltaIndex3d & 0x3FF;
				int num5 = (currentTicker.DeltaIndex3d >> 10) & 0x3FF;
				int num6 = (currentTicker.DeltaIndex3d >> 20) & 0x3FF;
				int key = (num4 + num) | (num5 + num2 << 10) | (num6 + num3 << 20);
				long tickingSinceMs = elapsedMilliseconds;
				if (committedTickers.TryGetValue(key, out var value))
				{
					tickingSinceMs = value.TickingSinceMs;
				}
				dictionary[currentTicker.DeltaIndex3d] = new TickerMetaData
				{
					TickingSinceMs = tickingSinceMs,
					IsWindAffected = currentTicker.IsWindAffected,
					WindAffectedNess = currentTicker.WindAffectedNess
				};
			}
			commitedPlayerPosDiv8 = currentPlayerPosDiv8;
			currentTickers.Clear();
			lock (committedTickersLock)
			{
				committedTickers = dictionary;
			}
			ambientSounds = MergeEqualAmbientSounds();
		}
		game.eventManager?.OnAmbientSoundsScanComplete(ambientSounds);
	}

	private List<AmbientSound> MergeEqualAmbientSounds()
	{
		Dictionary<AssetLocation, List<AmbientSound>> dictionary = new Dictionary<AssetLocation, List<AmbientSound>>();
		foreach (Dictionary<AssetLocation, AmbientSound> value2 in currentAmbientSoundsBySection.Values)
		{
			foreach (AssetLocation key in value2.Keys)
			{
				bool flag = false;
				if (dictionary.TryGetValue(key, out var value))
				{
					for (int i = 0; i < value.Count; i++)
					{
						AmbientSound ambientSound = value[i];
						if (ambientSound.DistanceTo(value2[key]) < ambientSound.MaxDistanceMerge)
						{
							ambientSound.BoundingBoxes.AddRange(value2[key].BoundingBoxes);
							ambientSound.QuantityNearbyBlocks += value2[key].QuantityNearbyBlocks;
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						value.Add(value2[key]);
					}
				}
				else
				{
					dictionary[key] = new List<AmbientSound> { value2[key] };
				}
			}
		}
		List<AmbientSound> list = new List<AmbientSound>();
		foreach (KeyValuePair<AssetLocation, List<AmbientSound>> item in dictionary)
		{
			list.AddRange(item.Value);
		}
		return list;
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 5;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (shouldStartScanning && scanState != BlockScanState.Done)
		{
			scanState = BlockScanState.Scanning;
			scanPosition = 0;
			currentLeavesCount = 0;
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = false;
			}
			currentTickers.Clear();
			currentAmbientSoundsBySection.Clear();
		}
		if (scanState != BlockScanState.Scanning)
		{
			return;
		}
		int num = currentPlayerPosDiv8.X * 8 - scanRange;
		int num2 = currentPlayerPosDiv8.Y * 8 - scanRange;
		int num3 = currentPlayerPosDiv8.Z * 8 - scanRange;
		IWorldChunk worldChunk = null;
		int num4 = 0;
		int num5 = -1;
		int num6 = -912312;
		BlockPos blockPos = new BlockPos();
		IList<Block> blocks = game.Blocks;
		for (int i = 0; i < 11000; i++)
		{
			int num7 = scanPosition % scanSize;
			int num8 = scanPosition / (scanSize * scanSize);
			int num9 = scanPosition / scanSize % scanSize;
			blockPos.Set(num + num7, num2 + num8, num3 + num9);
			if (!game.WorldMap.IsValidPos(blockPos))
			{
				scanPosition++;
				continue;
			}
			int num10 = blockPos.X / 32;
			int num11 = blockPos.Y / 32;
			int num12 = blockPos.Z / 32;
			if (num10 != num4 || num11 != num5 || num12 != num6)
			{
				num4 = num10;
				num5 = num11;
				num6 = num12;
				worldChunk = game.WorldMap.GetChunk(num10, num11, num12);
				worldChunk?.Unpack();
			}
			if (worldChunk == null)
			{
				scanPosition++;
				continue;
			}
			int num13 = blockPos.X % 32;
			int num14 = blockPos.Y % 32;
			int num15 = blockPos.Z % 32;
			Block block = blocks[worldChunk.Data[(num14 * 32 + num15) * 32 + num13]];
			float ambientSoundStrength;
			if (block?.Sounds?.Ambient != null && (ambientSoundStrength = block.GetAmbientSoundStrength(game, blockPos)) > 0f)
			{
				Vec3i vec3i = new Vec3i(blockPos.X / scanSectionSize, blockPos.Y / scanSectionSize, blockPos.Z / scanSectionSize);
				if (!currentAmbientSoundsBySection.TryGetValue(vec3i, out var value))
				{
					value = (currentAmbientSoundsBySection[vec3i] = new Dictionary<AssetLocation, AmbientSound>());
				}
				value.TryGetValue(block.Sounds.Ambient, out var value2);
				if (value2 == null)
				{
					value2 = new AmbientSound
					{
						AssetLoc = block.Sounds.Ambient,
						Ratio = block.Sounds.AmbientBlockCount,
						VolumeMul = ambientSoundStrength,
						SoundType = block.Sounds.AmbientSoundType,
						SectionPos = vec3i,
						MaxDistanceMerge = block.Sounds.AmbientMaxDistanceMerge
					};
					value2.BoundingBoxes.Add(new Cuboidi(blockPos.X, blockPos.Y, blockPos.Z, blockPos.X + 1, blockPos.Y + 1, blockPos.Z + 1));
					value[block.Sounds.Ambient] = value2;
				}
				else
				{
					value2.VolumeMul = ambientSoundStrength;
				}
				value2.QuantityNearbyBlocks++;
				Cuboidi cuboidi = value2.BoundingBoxes[0];
				cuboidi.GrowToInclude(blockPos);
				if (blockPos.X == cuboidi.X2)
				{
					cuboidi.X2++;
				}
				if (blockPos.Y == cuboidi.Y2)
				{
					cuboidi.Y2++;
				}
				if (blockPos.Z == cuboidi.Z2)
				{
					cuboidi.Z2++;
				}
			}
			if (block.BlockMaterial == EnumBlockMaterial.Leaves)
			{
				currentLeavesCount++;
			}
			if (block.ShouldReceiveClientParticleTicks(game, game.player, blockPos, out var isWindAffected))
			{
				currentTickers.Add(new TickingBlockData
				{
					DeltaIndex3d = (num7 | (num8 << 10) | (num9 << 20)),
					IsWindAffected = isWindAffected,
					WindAffectedNess = (isWindAffected ? SearchWindAffectedNess(blockPos, searchBlockAccessor) : 0f)
				});
			}
			scanPosition++;
			if (scanPosition < finalScanPosition)
			{
				continue;
			}
			if (scanState == BlockScanState.Scanning)
			{
				scanState = BlockScanState.Done;
				game.EnqueueMainThreadTask(delegate
				{
					CommitScan();
					GlobalConstants.CurrentNearbyRelLeavesCountClient = (float)currentLeavesCount / (float)finalScanPosition;
					scanState = BlockScanState.Idle;
				}, "commitscan");
			}
			break;
		}
	}

	private float SearchWindAffectedNess(BlockPos pos, IBlockAccessor blockAccess)
	{
		return Math.Max(0f, 1f - (float)blockAccess.GetDistanceToRainFall(pos) / 5f);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
