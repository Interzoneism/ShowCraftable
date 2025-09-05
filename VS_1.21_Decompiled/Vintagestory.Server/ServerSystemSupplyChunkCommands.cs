using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemSupplyChunkCommands : ServerSystem
{
	private string backupFileName;

	private ChunkServerThread chunkthread;

	public ServerSystemSupplyChunkCommands(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		server.api.ChatCommands.GetOrCreate("chunk").BeginSub("cit").WithDescription("Chunk information from the supply chunks thread")
			.WithArgs(server.api.ChatCommands.Parsers.OptionalWord("perf"))
			.HandleWith(OnChunkInfoCmd)
			.EndSub()
			.BeginSub("printmap")
			.WithDescription("Export a png file of a map of loaded chunks. Marks call location with a yellow pixel")
			.HandleWith(OnChunkMap)
			.EndSub();
	}

	private TextCommandResult OnChunkMap(TextCommandCallingArgs args)
	{
		string text = PrintServerChunkMap(new Vec2i(args.Caller.Pos.XInt / 32, args.Caller.Pos.ZInt / 32));
		return TextCommandResult.Success("map " + text + " generated");
	}

	public override void OnBeginModsAndConfigReady()
	{
		base.OnBeginModsAndConfigReady();
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		IChatCommand chatCommand = server.api.ChatCommands.GetOrCreate("db").RequiresPrivilege(Privilege.controlserver).WithDesc("Save-game related commands");
		if (!server.Config.HostedMode)
		{
			chatCommand.BeginSub("backup").WithDesc("Creates a copy of the current save game in the Backups folder").WithArgs(parsers.OptionalWord("filename"))
				.HandleWith(onCmdGenBackup)
				.WithRootAlias("genbackup")
				.EndSub()
				.BeginSub("vacuum")
				.WithDesc("Repack save game to minimize its file size")
				.HandleWith(onCmdVacuum)
				.EndSub();
		}
		else
		{
			chatCommand.WithAdditionalInformation("(/db backup and /db vacuum sub-commands are not available for hosted servers, sorry)");
		}
		chatCommand.BeginSub("prune").WithDesc("Delete all unchanged or hardly changed chunks, with changes below a specified threshold. Chunks with claims can be protected.").WithAdditionalInformation("'Changes' refers to edits by players, counted separately in each 32x32 chunk in the world. The number of edits is the count of blocks of any kind placed or broken by any player in either Survival or Creative modes. Breaking grass or leaves is counted, harvesting berries or collecting sticks is not counted. Only player actions since game version 1.18.0 (April 2023) are counted. Chunks with land claims of any size, even a single block, can be protected using the 'keep' option. The 'keep' option will preserve all trader caravans and the Resonance Archives.\n\nPruned chunks are fully deleted and destroyed and, when next visited, will be regenerated with up-to-date worldgen from the current game version, including new vegetation and ruins. Bodies of water, general terrain shape and climate conditions will be unchanged or almost unchanged. Ore presence in each chunk will be similar as before, may be in slightly different positions.\n\nWithout the 'confirm' arg, does a dry-run only! If mods or worldconfig have changed since the world was first created, or if the map was first created in game version 1.17 or earlier, results of a prune may be unpredictable or chunk borders may become visible, a backup first is advisable. This command is irreversible, use with care!")
			.WithArgs(parsers.Int("threshold"), parsers.WordRange("choice whether to protect (keep) all chunks which have land claims", "keep", "drop"), parsers.OptionalWordRange("confirm flag", "confirm"))
			.HandleWith(onCmdPrune)
			.EndSub()
			.Validate();
	}

	private TextCommandResult onCmdVacuum(TextCommandCallingArgs args)
	{
		IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
		processInBackground(chunkthread.gameDatabase.Vacuum, delegate
		{
			notifyIndirect(logToPlayer, Lang.Get("Vacuum complete!"));
		});
		return TextCommandResult.Success(Lang.Get("Vacuum started, this may take some time"));
	}

	private TextCommandResult onCmdPrune(TextCommandCallingArgs args)
	{
		IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
		int threshold = (int)args[0];
		bool keepClaims = (string)args[1] == "keep";
		bool dryRun = (string)args[2] != "confirm";
		return prune(logToPlayer, threshold, dryRun, keepClaims);
	}

	private TextCommandResult prune(IServerPlayer logToPlayer, int threshold, bool dryRun, bool keepClaims)
	{
		int qBelowThreshold = 0;
		HashSet<Vec2i> toDelete = new HashSet<Vec2i>();
		HashSet<Vec2i> toKeep = new HashSet<Vec2i>();
		List<LandClaim> claims = server.WorldMap.All;
		HorRectanglei rect = new HorRectanglei();
		int chunksize = server.api.worldapi.ChunkSize;
		processInBackground(delegate
		{
			foreach (DbChunk allChunk in chunkthread.gameDatabase.GetAllChunks())
			{
				ServerChunk serverChunk = ServerChunk.FromBytes(allChunk.Data, server.serverChunkDataPool, server);
				if (keepClaims)
				{
					bool flag = false;
					rect.X1 = allChunk.Position.X * chunksize;
					rect.Z1 = allChunk.Position.Z * chunksize;
					rect.X2 = allChunk.Position.X * chunksize + chunksize;
					rect.Z2 = allChunk.Position.Z * chunksize + chunksize;
					foreach (LandClaim item2 in claims)
					{
						if (item2 != null && item2.Intersects2d(rect))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						toKeep.Add(new Vec2i(allChunk.Position.X, allChunk.Position.Z));
						continue;
					}
				}
				int blocksRemoved = serverChunk.BlocksRemoved;
				int blocksPlaced = serverChunk.BlocksPlaced;
				if (blocksRemoved + blocksPlaced > threshold)
				{
					toKeep.Add(new Vec2i(allChunk.Position.X, allChunk.Position.Z));
				}
				else
				{
					qBelowThreshold++;
					toDelete.Add(new Vec2i(allChunk.Position.X, allChunk.Position.Z));
				}
			}
			foreach (Vec2i item3 in toKeep)
			{
				toDelete.Remove(item3);
			}
			server.EnqueueMainThreadTask(delegate
			{
				if (dryRun)
				{
					notifyIndirect(logToPlayer, Lang.Get("Dry run prune complete. With a {0} block edits threshold, {1} chunk columns can be removed, {2} chunk columns would be kept.", threshold, toDelete.Count, toKeep.Count));
				}
				else
				{
					int num = server.api.worldapi.RegionSize / chunksize;
					Cuboidi cuboidi = new Cuboidi();
					Dictionary<long, ServerMapRegion> dictionary = new Dictionary<long, ServerMapRegion>(10);
					Queue<long> queue = new Queue<long>(10);
					FastMemoryStream ms = new FastMemoryStream();
					foreach (Vec2i item4 in toDelete)
					{
						int num2 = item4.X / num;
						int num3 = item4.Y / num;
						ServerMapRegion value = server.WorldMap.GetMapRegion(num2, num3) as ServerMapRegion;
						long num4 = server.WorldMap.MapRegionIndex2D(num2, num3);
						if (value == null && !dictionary.TryGetValue(num4, out value))
						{
							byte[] mapRegion = chunkthread.gameDatabase.GetMapRegion(num2, num3);
							if (mapRegion != null)
							{
								if (queue.Count >= 9)
								{
									long num5 = queue.Dequeue();
									ServerMapRegion serverMapRegion = dictionary[num5];
									DbChunk item = new DbChunk
									{
										Position = server.WorldMap.MapRegionPosFromIndex2D(num5),
										Data = serverMapRegion.ToBytes(ms)
									};
									chunkthread.gameDatabase.SetMapRegions(new List<DbChunk> { item });
									dictionary.Remove(num5);
								}
								value = (dictionary[num4] = ServerMapRegion.FromBytes(mapRegion));
								queue.Enqueue(num4);
							}
						}
						List<GeneratedStructure> list = new List<GeneratedStructure>();
						cuboidi.X1 = item4.X * chunksize;
						cuboidi.Z1 = item4.Y * chunksize;
						cuboidi.X2 = item4.X * chunksize + chunksize;
						cuboidi.Z2 = item4.Y * chunksize + chunksize;
						if (value?.GeneratedStructures != null)
						{
							foreach (GeneratedStructure generatedStructure in value.GeneratedStructures)
							{
								if (cuboidi.Contains(generatedStructure.Location.Start.X, generatedStructure.Location.Start.Z))
								{
									list.Add(generatedStructure);
								}
							}
							foreach (GeneratedStructure item5 in list)
							{
								value.GeneratedStructures.Remove(item5);
							}
						}
						server.api.WorldManager.DeleteChunkColumn(item4.X, item4.Y);
					}
					chunkthread.gameDatabase.SetMapRegions(dictionary.Select((KeyValuePair<long, ServerMapRegion> r) => new DbChunk
					{
						Position = server.WorldMap.MapRegionPosFromIndex2D(r.Key),
						Data = r.Value.ToBytes(ms)
					}));
					notifyIndirect(logToPlayer, Lang.Get("Prune complete, {1} chunk columns were removed, {2} chunk columns were kept.", threshold, toDelete.Count, toKeep.Count));
				}
			});
		}, null);
		return TextCommandResult.Success(dryRun ? Lang.Get("Dry run prune started, this may take some time.") : Lang.Get("Prune started, this may take some time."));
	}

	private TextCommandResult onCmdGenBackup(TextCommandCallingArgs args)
	{
		if (server.Config.HostedMode)
		{
			return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		backupFileName = (args.Parsers[0].IsMissing ? null : Path.GetFileName(args[0] as string));
		GenBackup(args.Caller.Player as IServerPlayer);
		return TextCommandResult.Success(Lang.Get("Ok, generating backup, this might take a while"));
	}

	private void GenBackup(IServerPlayer logToPlayer = null)
	{
		if (chunkthread.BackupInProgress)
		{
			notifyIndirect(logToPlayer, Lang.Get("Can't run backup. A backup is already in progress"));
			return;
		}
		chunkthread.BackupInProgress = true;
		if (backupFileName == null || backupFileName.Length == 0 || backupFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			string text = Path.GetFileName(server.Config.WorldConfig.SaveFileLocation).Replace(".vcdbs", "");
			if (text.Length == 0)
			{
				text = "world";
			}
			backupFileName = text + "-" + $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" + ".vcdbs";
		}
		processInBackground(delegate
		{
			chunkthread.gameDatabase.CreateBackup(backupFileName);
		}, delegate
		{
			chunkthread.BackupInProgress = false;
			string msg = Lang.Get("Backup complete!");
			notifyIndirect(logToPlayer, msg);
		});
	}

	private void processInBackground(Action backgroundProc, Action onDoneOnMainthread)
	{
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			backgroundProc();
			server.EnqueueMainThreadTask(delegate
			{
				onDoneOnMainthread?.Invoke();
			});
		}, "supplychunkcommand");
	}

	private void notifyIndirect(IServerPlayer logToPlayer, string msg)
	{
		if (logToPlayer != null)
		{
			logToPlayer.SendMessage(server.IsDedicatedServer ? GlobalConstants.ServerInfoChatGroup : GlobalConstants.GeneralChatGroup, msg, EnumChatType.CommandSuccess, "backupdone");
		}
		else
		{
			ServerMain.Logger.Notification(msg);
		}
	}

	private TextCommandResult OnChunkInfoCmd(TextCommandCallingArgs args)
	{
		if ((string)args[0] == "perf")
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < 20; i++)
			{
				stringBuilder.AppendLine(ServerSystemSendChunks.performanceTest(server));
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
		int chunkX = asBlockPos.X / 32;
		int chunkY = asBlockPos.Y / 32;
		int chunkZ = asBlockPos.Z / 32;
		long index = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ServerChunk generatingChunk = chunkthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
		ChunkColumnLoadRequest byIndex = chunkthread.requestedChunkColumns.GetByIndex(index);
		if (byIndex != null)
		{
			return TextCommandResult.Success($"Chunk in genQ: {generatingChunk != null}, chunkReq in Q: {byIndex != null}, currentPass: {byIndex.CurrentIncompletePass}, untilPass: {byIndex.GenerateUntilPass}");
		}
		return TextCommandResult.Success($"Chunk in genQ: {generatingChunk != null}, chunkReq in Q: {byIndex != null}");
	}

	public string PrintServerChunkMap(Vec2i markChunkPos = null)
	{
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Expected O, but got Unknown
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		ChunkPos chunkPos = new ChunkPos(int.MaxValue, 0, int.MaxValue, 0);
		ChunkPos chunkPos2 = new ChunkPos(0, 0, 0, 0);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long key in server.loadedChunks.Keys)
			{
				ChunkPos chunkPos3 = server.WorldMap.ChunkPosFromChunkIndex3D(key);
				if (chunkPos3.Dimension <= 0)
				{
					chunkPos.X = Math.Min(chunkPos.X, chunkPos3.X);
					chunkPos.Z = Math.Min(chunkPos.Z, chunkPos3.Z);
					chunkPos2.X = Math.Max(chunkPos2.X, chunkPos3.X);
					chunkPos2.Z = Math.Max(chunkPos2.Z, chunkPos3.Z);
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		if (chunkPos.X == int.MaxValue)
		{
			return "";
		}
		int num = chunkPos2.X - chunkPos.X;
		int num2 = chunkPos2.Z - chunkPos.Z;
		SKBitmap val = new SKBitmap(num + 1, num2 + 1, false);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long key2 in server.loadedChunks.Keys)
			{
				ChunkPos chunkPos4 = server.WorldMap.ChunkPosFromChunkIndex3D(key2);
				if (chunkPos4.Dimension <= 0)
				{
					val.SetPixel(chunkPos4.X - chunkPos.X, chunkPos4.Z - chunkPos.Z, new SKColor((byte)0, byte.MaxValue, (byte)0, byte.MaxValue));
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		SKColor val2 = default(SKColor);
		foreach (ChunkColumnLoadRequest item in chunkthread.requestedChunkColumns.Snapshot())
		{
			if (item != null && !item.Disposed)
			{
				if (item.Chunks == null)
				{
					val.SetPixel(item.chunkX, item.chunkZ, new SKColor((byte)20, (byte)20, (byte)20, byte.MaxValue));
					continue;
				}
				int currentIncompletePass_AsInt = item.CurrentIncompletePass_AsInt;
				SKColor pixel = val.GetPixel(item.chunkX, item.chunkZ);
				((SKColor)(ref val2))._002Ector((byte)(5 + ((SKColor)(ref pixel)).Red), (byte)(currentIncompletePass_AsInt * 30), (byte)(currentIncompletePass_AsInt * 30), byte.MaxValue);
				val.SetPixel(item.chunkX - chunkPos.X, item.chunkZ - chunkPos.Z, val2);
			}
		}
		int num3 = 0;
		while (File.Exists("serverchunks" + num3 + ".png"))
		{
			num3++;
		}
		if (markChunkPos != null)
		{
			val.SetPixel(markChunkPos.X - chunkPos.X, markChunkPos.Y - chunkPos.Z, new SKColor(byte.MaxValue, (byte)20, byte.MaxValue, byte.MaxValue));
		}
		val.Save("serverchunks" + num3 + ".png");
		return "serverchunks" + num3 + ".png";
	}
}
