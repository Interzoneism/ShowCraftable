using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class CmdDebug
{
	private class Eproprs
	{
		public EntityProperties Props;

		public SKColor Color;
	}

	private Thread MainThread;

	private ServerMain server;

	public CmdDebug(ServerMain server)
	{
		CmdDebug cmdDebug = this;
		MainThread = Thread.CurrentThread;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		_ = server.api;
		chatCommands.GetOrCreate("debug").WithDesc("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
			.BeginSub("blockcodes")
			.WithDesc("Print codes of all loaded blocks to the server log file")
			.HandleWith(printBlockCodes)
			.EndSub()
			.BeginSub("itemcodes")
			.WithDesc("Print codes of all loaded items to the server log file")
			.HandleWith(printItemCodes)
			.EndSub()
			.BeginSub("blockstats")
			.WithDesc("Generates counds amount of block ids used, grouped by first block code part, prints it to the server log file")
			.HandleWith(printBlockStats)
			.EndSub()
			.BeginSub("helddurability")
			.WithAlias("helddura")
			.WithDesc("Set held item durability")
			.WithArgs(parsers.Int("durability"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ItemSlot activeHandItemSlot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
				if (activeHandItemSlot.Itemstack == null)
				{
					return TextCommandResult.Error("Nothing in active hands");
				}
				getSetItemStackAttr(activeHandItemSlot, "durability", "int", ((int)args[0]).ToString() ?? "");
				return TextCommandResult.Success((int)args[0] + " durability set.");
			})
			.EndSub()
			.BeginSub("heldtemperature")
			.WithAlias("heldtemp")
			.WithDesc("Set held item temperature")
			.WithArgs(parsers.Int("temperature in °C"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ItemSlot activeHandItemSlot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
				ItemStack itemstack = activeHandItemSlot.Itemstack;
				if (itemstack == null)
				{
					return TextCommandResult.Error("Nothing in active hands");
				}
				int num = (int)args[0];
				itemstack.Collectible.SetTemperature(server, itemstack, num);
				activeHandItemSlot.MarkDirty();
				return TextCommandResult.Success(num + " °C set.");
			})
			.EndSub()
			.BeginSub("heldcoattr")
			.WithDesc("Get/Set collectible attributes of the currently held item")
			.WithArgs(parsers.Word("key"), parsers.OptionalAll("value"))
			.HandleWith(getSetCollectibleAttr)
			.EndSub()
			.BeginSub("heldstattr")
			.WithDesc("Get/Set itemstack attributes of the currently held item")
			.WithArgs(parsers.Word("key"), parsers.OptionalWordRange("type", "int", "bool", "string", "tree", "double", "float"), parsers.OptionalAll("value"))
			.HandleWith(getSetItemstackAttr)
			.EndSub()
			.BeginSub("netbench")
			.WithDesc("Toggle network benchmarking mode")
			.HandleWith(toggleNetworkBenchmarking)
			.EndSub()
			.BeginSub("rebuildlandclaimpartitions")
			.WithDesc("Rebuild land claim partitions")
			.HandleWith(delegate
			{
				server.WorldMap.RebuildLandClaimPartitions();
				return TextCommandResult.Success("Partitioned land claim index rebuilt");
			})
			.EndSub()
			.BeginSub("privileges")
			.WithDesc("Toggle privileges debug mode")
			.WithArgs(parsers.OptionalBool("on"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				if (args.Parsers[0].IsMissing)
				{
					return TextCommandResult.Success("Privilege debugging is currently " + (server.DebugPrivileges ? "on" : "off"));
				}
				server.DebugPrivileges = (bool)args[0];
				return TextCommandResult.Success("Privilege debugging now " + (server.DebugPrivileges ? "on" : "off"));
			})
			.EndSub()
			.BeginSub("cloh")
			.WithDesc("Compact the large object heap")
			.HandleWith(delegate
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
				return TextCommandResult.Success("Ok, compacted large object heap");
			})
			.EndSub()
			.BeginSub("logticks")
			.WithDesc("Toggle slow tick profiler")
			.WithArgs(parsers.Int("millisecond threshold"), parsers.OptionalBool("include offthreads"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ServerMain.FrameProfiler.PrintSlowTicks = !ServerMain.FrameProfiler.PrintSlowTicks;
				ServerMain.FrameProfiler.Enabled = ServerMain.FrameProfiler.PrintSlowTicks;
				ServerMain.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
				if ((!args.Parsers[1].IsMissing && (bool)args[1]) || !ServerMain.FrameProfiler.Enabled)
				{
					FrameProfilerUtil.PrintSlowTicks_Offthreads = ServerMain.FrameProfiler.PrintSlowTicks;
					FrameProfilerUtil.PrintSlowTicksThreshold_Offthreads = ServerMain.FrameProfiler.PrintSlowTicksThreshold;
					FrameProfilerUtil.offThreadProfiles = new ConcurrentQueue<string>();
				}
				ServerMain.FrameProfiler.Begin(null);
				return TextCommandResult.Success("Server Tick Profiling now " + (ServerMain.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ServerMain.FrameProfiler.PrintSlowTicksThreshold + " ms") : "off"));
			})
			.EndSub()
			.BeginSub("octagonpoints")
			.WithDesc("Exports a map of chunks that ought to be sent to the client as a png image")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				cmdDebug.PrintOctagonPoints(args.Caller.Player.WorldData.DesiredViewDistance);
				return TextCommandResult.Success("Printed octagon points");
			})
			.EndSub()
			.BeginSub("tickposition")
			.WithDesc("Print current server tick position (for debugging a frozen server)")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.TickPosition.ToString() ?? ""))
			.EndSub()
			.BeginSub("mainthreadstate")
			.WithDesc("Print current main thread state")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(cmdDebug.MainThread.ThreadState.ToString()))
			.EndSub()
			.BeginSub("threadpoolstate")
			.WithDesc("Print current thread pool state")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(TyronThreadPool.Inst.ListAllRunningTasks() + "\n" + TyronThreadPool.Inst.ListAllThreads()))
			.EndSub()
			.BeginSub("tickhandlers")
			.WithDesc("Counts amount of game tick listeners grouped by listener type")
			.HandleWith(countTickHandlers)
			.BeginSub("dump")
			.WithDesc("Export full list of all listeners to the log file")
			.WithArgs(parsers.Word("Listener type"))
			.HandleWith(exportTickHandlers)
			.EndSub()
			.EndSub()
			.BeginSub("chunk")
			.WithDesc("Chunk debug utilities")
			.BeginSub("queue")
			.WithAlias("q")
			.WithDesc("Amount of generating chunks in queue")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success($"Currently {server.chunkThread.requestedChunkColumns.Count} chunks in generation queue"))
			.EndSub()
			.BeginSub("stats")
			.WithDesc("Statics on currently loaded chunks")
			.HandleWith(getChunkStats)
			.EndSub()
			.BeginSub("printmap")
			.WithDesc("Exports a map of loaded chunk as a png image")
			.HandleWith(delegate
			{
				server.WorldMap.PrintChunkMap(new Vec2i(server.MapSize.X / 2 / 32, server.MapSize.Z / 2 / 32));
				return TextCommandResult.Success("Printed chunk map");
			})
			.EndSub()
			.BeginSub("here")
			.WithDesc("Information about the chunk at the callers position")
			.HandleWith(getHereChunkInfo)
			.EndSub()
			.BeginSub("resend")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.WithDesc("Resend a chunk to all players")
			.HandleWith(resendChunk)
			.EndSub()
			.BeginSub("relight")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.WithDesc("Relight a chunk for all players")
			.HandleWith(relightChunk)
			.EndSub()
			.EndSub()
			.BeginSub("sendchunks")
			.WithDescription("Allows toggling of the normal chunk generation/sending operations to all clients.")
			.WithAdditionalInformation("Force loaded chunks are not affected by this switch.")
			.WithArgs(parsers.Bool("state"))
			.HandleWith(toggleSendChunks)
			.EndSub()
			.BeginSub("expclang")
			.WithDescription("Export a list of missing block and item translations, with suggestions")
			.HandleWith(handleExpCLang)
			.EndSub()
			.BeginSub("blu")
			.WithDesc("Place every block type in the game")
			.HandleWith(handleBlu)
			.EndSub()
			.BeginSub("dumpanimstate")
			.WithDesc("Dump animation state into log file")
			.WithArgs(parsers.Entities("target entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdDebug.handleDumpAnimState(e, args)))
			.EndSub()
			.BeginSub("dumprecipes")
			.WithDesc("Dump grid recipes into log file")
			.HandleWith(delegate
			{
				foreach (GridRecipe gridRecipe in server.GridRecipes)
				{
					bool flag = false;
					foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in gridRecipe.Ingredients)
					{
						if (ingredient.Value.ResolvedItemstack?.TempAttributes != null && ingredient.Value.ResolvedItemstack.TempAttributes.Count > 0)
						{
							if (!flag)
							{
								ServerMain.Logger.VerboseDebug(gridRecipe.Name);
							}
							flag = true;
							ServerMain.Logger.VerboseDebug(ingredient.Key + ": " + ingredient.Value.ToString() + "/" + ingredient.Value.ResolvedItemstack.TempAttributes);
						}
					}
				}
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("testcond")
			.WithDescription("Test conditionals")
			.WithArgs(parsers.OptionalAll("cond"))
			.HandleWith(handleTestCond)
			.EndSub()
			.BeginSubCommand("spawnheatmap")
			.WithDescription("spawnheatmap")
			.HandleWith(OnCmdSpawnHeatmap)
			.WithArgs(parsers.WordRange("x-axis", "temp", "rain", "forest", "elevation"), parsers.WordRange("y-axis", "temp", "rain", "forest", "elevation"), parsers.OptionalWord("entity type"), parsers.OptionalBool("negate entity type filter"))
			.EndSubCommand()
			.Validate();
	}

	private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
	{
		ServerMain.Logger.Notification(e.AnimManager?.Animator?.DumpCurrentState());
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSpawnHeatmap(TextCommandCallingArgs args)
	{
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		byte[] data = server.AssetManager.TryGet("textures/environment/planttint.png").Data;
		BitmapExternal bitmapExternal = new BitmapExternal(data, data.Length, ServerMain.Logger);
		List<Eproprs> list = new List<Eproprs>();
		string text = args[0] as string;
		string text2 = args[1] as string;
		AssetLocation assetLocation = (args.Parsers[2].IsMissing ? null : new AssetLocation(args[2] as string));
		bool flag = (bool)args[3];
		Random random = new Random(0);
		SKColor color = default(SKColor);
		foreach (EntityProperties entityType in server.EntityTypes)
		{
			RuntimeSpawnConditions runtimeSpawnConditions = entityType.Server?.SpawnConditions?.Runtime;
			if (runtimeSpawnConditions == null || entityType.Code.Path.Contains("drifter") || entityType.Code.Path.Contains("butter"))
			{
				continue;
			}
			if (assetLocation != null)
			{
				bool flag2 = !WildcardUtil.Match(assetLocation, entityType.Code);
				if ((flag2 && !flag) || (!flag2 && flag))
				{
					continue;
				}
			}
			if (runtimeSpawnConditions.MaxQuantity > 0 || (runtimeSpawnConditions.MaxQuantityByGroup != null && runtimeSpawnConditions.MaxQuantityByGroup.MaxQuantity > 0))
			{
				if (entityType.Color == null || entityType.Color == "")
				{
					((SKColor)(ref color))._002Ector((uint)(((int)random.NextInt64() & 0xFFFFFF) | 0x60000000));
				}
				else
				{
					((SKColor)(ref color))._002Ector((uint)((ColorUtil.Hex2Int(entityType.Color) & 0xFFFFFF) | 0x60000000));
				}
				list.Add(new Eproprs
				{
					Props = entityType,
					Color = color
				});
				ServerMain.Logger.Notification(entityType.Code.ToString());
			}
		}
		SKColor val = default(SKColor);
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				for (int k = 0; k < list.Count; k++)
				{
					Eproprs eproprs = list[k];
					RuntimeSpawnConditions runtime = eproprs.Props.Server.SpawnConditions.Runtime;
					float num = GameMath.Clamp(((float)i - 0f) / 4.25f - 20f, -20f, 40f);
					float num2 = GameMath.Clamp(((float)j - 0f) / 4.25f - 20f, -20f, 40f);
					float num3 = (float)i / 255f;
					float num4 = (float)j / 255f;
					bool flag3 = false;
					bool flag4 = false;
					switch (text)
					{
					case "temp":
						flag3 = num >= runtime.MinTemp && num <= runtime.MaxTemp;
						break;
					case "rain":
						flag3 = num4 >= runtime.MinRain && num4 <= runtime.MaxRain;
						break;
					case "forest":
						flag3 = num3 >= runtime.MinForest && num3 <= runtime.MaxForest;
						break;
					case "elevation":
						flag3 = num3 >= runtime.MinY - 1f && num3 <= runtime.MaxY - 1f;
						break;
					}
					switch (text2)
					{
					case "temp":
						flag4 = num2 >= runtime.MinTemp && num2 <= runtime.MaxTemp;
						break;
					case "rain":
						flag4 = num4 >= runtime.MinRain && num4 <= runtime.MaxRain;
						break;
					case "forest":
						flag4 = num4 >= runtime.MinForest && num4 <= runtime.MaxForest;
						break;
					case "elevation":
						flag4 = num4 >= runtime.MinY - 1f && num4 <= runtime.MaxY - 1f;
						break;
					}
					if (flag3 && flag4)
					{
						int num5 = ColorUtil.ColorOverlay(bitmapExternal.bmp.GetPixel(4 + i, 4 + j).ToInt(), eproprs.Color.ToInt(), (float)(int)((SKColor)(ref eproprs.Color)).Alpha / 255f);
						((SKColor)(ref val))._002Ector((uint)num5);
						bitmapExternal.bmp.SetPixel(4 + i, 4 + j, val);
					}
				}
			}
		}
		bitmapExternal.Save("spawnheatmap.png");
		if (!(assetLocation == null))
		{
			return TextCommandResult.Success("ok, spawnheatmap.png generated for " + assetLocation.Path + ". Also printed matching entities to server-main.log");
		}
		return TextCommandResult.Success("ok, spawnheatmap.png generated. Also printed matching entities to server-main.log");
	}

	private TextCommandResult handleBlu(TextCommandCallingArgs args)
	{
		BlockLineup(args.Caller.Pos.AsBlockPos, args.RawArgs);
		return TextCommandResult.Success("Block lineup created");
	}

	private void BlockLineup(BlockPos pos, CmdArgs args)
	{
		IList<Block> blocks = server.World.Blocks;
		bool flag = args.PopWord() == "all";
		List<Block> list = new List<Block>();
		for (int i = 0; i < blocks.Count; i++)
		{
			Block block = blocks[i];
			if (block != null && !(block.Code == null))
			{
				if (flag)
				{
					list.Add(block);
				}
				else if (block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0)
				{
					list.Add(block);
				}
			}
		}
		int num = (int)Math.Sqrt(list.Count);
		for (int j = 0; j < list.Count; j++)
		{
			server.World.BlockAccessor.SetBlock(list[j].BlockId, pos.AddCopy(j / num, 0, j % num));
		}
	}

	private TextCommandResult printBlockCodes(TextCommandCallingArgs args)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Block block in server.Blocks)
		{
			if (!(block.Code == null))
			{
				string key = block.Code.ToShortString();
				dictionary.TryGetValue(key, out var value);
				value = (dictionary[key] = value + 1);
			}
		}
		List<KeyValuePair<string, int>> list = dictionary.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item in list)
		{
			stringBuilder.AppendLine(item.Key);
		}
		ServerMain.Logger.Notification(stringBuilder.ToString());
		return TextCommandResult.Success("Block codes written to log file.");
	}

	private TextCommandResult printItemCodes(TextCommandCallingArgs args)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Item item in server.Items)
		{
			if (!(item.Code == null))
			{
				string key = item.Code.ToShortString();
				dictionary.TryGetValue(key, out var value);
				value = (dictionary[key] = value + 1);
			}
		}
		List<KeyValuePair<string, int>> list = dictionary.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item2 in list)
		{
			stringBuilder.AppendLine(item2.Key);
		}
		ServerMain.Logger.Notification(stringBuilder.ToString());
		return TextCommandResult.Success("Item codes written to log file.");
	}

	private TextCommandResult printBlockStats(TextCommandCallingArgs args)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Block block in server.Blocks)
		{
			if (!(block.Code == null))
			{
				string key = block.Code.Domain + ":" + block.FirstCodePart();
				dictionary.TryGetValue(key, out var value);
				value = (dictionary[key] = value + 1);
			}
		}
		List<KeyValuePair<string, int>> list = dictionary.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item in list)
		{
			stringBuilder.AppendLine(item.Key + ": " + item.Value);
		}
		ServerMain.Logger.Notification(stringBuilder.ToString());
		return TextCommandResult.Success("Block ids summary written to log file.");
	}

	private TextCommandResult getSetCollectibleAttr(TextCommandCallingArgs args)
	{
		ItemSlot activeHandItemSlot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
		ItemStack itemstack = activeHandItemSlot.Itemstack;
		if (itemstack == null)
		{
			return TextCommandResult.Success("Nothing in right hands");
		}
		string text = (string)args[0];
		string text2 = (string)args[1];
		JToken token = itemstack.Collectible.Attributes.Token;
		if (text == null)
		{
			return TextCommandResult.Error("Syntax: /debug heldcoattr key value");
		}
		if (text2 == null)
		{
			return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} has value {1}.", text, token[(object)text]));
		}
		token[(object)text] = JToken.Parse(text2);
		activeHandItemSlot.MarkDirty();
		return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} set to {1}.", text, text2));
	}

	private TextCommandResult getSetItemstackAttr(TextCommandCallingArgs args)
	{
		ItemSlot activeHandItemSlot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
		if (activeHandItemSlot.Itemstack == null)
		{
			return TextCommandResult.Error("Nothing in active hands");
		}
		string key = (string)args[0];
		string type = (string)args[1];
		string value = (string)args[2];
		return getSetItemStackAttr(activeHandItemSlot, key, type, value);
	}

	private static TextCommandResult getSetItemStackAttr(ItemSlot slot, string key, string type, string value)
	{
		ItemStack itemstack = slot.Itemstack;
		if (type == null)
		{
			if (itemstack.Attributes.HasAttribute(key))
			{
				IAttribute attribute = itemstack.Attributes[key];
				Type type2 = TreeAttribute.AttributeIdMapping[attribute.GetAttributeId()];
				return TextCommandResult.Success(Lang.Get("Attribute {0} is of type {1} and has value {2}", type2, attribute.ToString()));
			}
			return TextCommandResult.Error(Lang.Get("Attribute {0} does not exist"));
		}
		switch (type)
		{
		case "int":
			itemstack.Attributes.SetInt(key, value.ToInt());
			break;
		case "bool":
			itemstack.Attributes.SetBool(key, value.ToBool());
			break;
		case "string":
			itemstack.Attributes.SetString(key, value);
			break;
		case "tree":
			itemstack.Attributes[key] = new JsonObject((JToken)(object)JObject.Parse(value)).ToAttribute();
			break;
		case "double":
			itemstack.Attributes.SetDouble(key, value.ToDouble());
			break;
		case "float":
			itemstack.Attributes.SetFloat(key, value.ToFloat());
			break;
		default:
			return TextCommandResult.Error("Invalid type");
		}
		slot.MarkDirty();
		return TextCommandResult.Success($"Stack Attribute {key}={itemstack.Attributes[key].ToString()} set.");
	}

	private TextCommandResult toggleNetworkBenchmarking(TextCommandCallingArgs args)
	{
		if (server.doNetBenchmark)
		{
			server.doNetBenchmark = false;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<int, int> packetBenchmarkByte in server.packetBenchmarkBytes)
			{
				SystemNetworkProcess.ServerPacketNames.TryGetValue(packetBenchmarkByte.Key, out var value);
				int num = server.packetBenchmark[packetBenchmarkByte.Key];
				stringBuilder.AppendLine(num + "x " + value + ": " + ((packetBenchmarkByte.Value > 9999) ? (((float)packetBenchmarkByte.Value / 1024f).ToString("#.#") + "kb") : (packetBenchmarkByte.Value + "b")));
			}
			stringBuilder.AppendLine("-----");
			foreach (KeyValuePair<string, int> packetBenchmarkBlockEntitiesByte in server.packetBenchmarkBlockEntitiesBytes)
			{
				string key = packetBenchmarkBlockEntitiesByte.Key;
				stringBuilder.AppendLine("BE " + key + ": " + ((packetBenchmarkBlockEntitiesByte.Value > 9999) ? (((float)packetBenchmarkBlockEntitiesByte.Value / 1024f).ToString("#.#") + "kb") : (packetBenchmarkBlockEntitiesByte.Value + "b")));
			}
			stringBuilder.AppendLine("-----");
			foreach (KeyValuePair<int, int> udpPacketBenchmarkByte in server.udpPacketBenchmarkBytes)
			{
				string text = udpPacketBenchmarkByte.Key.ToString();
				int num2 = server.udpPacketBenchmark[udpPacketBenchmarkByte.Key];
				stringBuilder.AppendLine(num2 + "x " + text + ": " + ((udpPacketBenchmarkByte.Value > 9999) ? (((float)udpPacketBenchmarkByte.Value / 1024f).ToString("#.#") + "kb") : (udpPacketBenchmarkByte.Value + "b")));
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		server.doNetBenchmark = true;
		server.packetBenchmark.Clear();
		server.packetBenchmarkBytes.Clear();
		server.packetBenchmarkBlockEntitiesBytes.Clear();
		server.udpPacketBenchmark.Clear();
		server.udpPacketBenchmarkBytes.Clear();
		return TextCommandResult.Success("Benchmarking started. Stop it after a while to get results.");
	}

	private TextCommandResult toggleSendChunks(TextCommandCallingArgs args)
	{
		server.SendChunks = (bool)args[0];
		return TextCommandResult.Success("Sending chunks is now " + (server.SendChunks ? "on" : "off"));
	}

	private TextCommandResult handleExpCLang(TextCommandCallingArgs args)
	{
		if (server.Config.HostedMode)
		{
			return TextCommandResult.Error("Can't access this feature, server is in hosted mode");
		}
		List<string> list = new List<string>();
		for (int i = 0; i < server.Blocks.Count; i++)
		{
			Block block = server.Blocks[i];
			if (block != null && !(block.Code == null) && block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0 && block.GetHeldItemName(new ItemStack(block)) == block.Code?.Domain + ":block-" + block.Code?.Path)
			{
				string text = block.Code.ShortDomain();
				if (text.Length > 0)
				{
					text += ":";
				}
				list.Add("\t\"" + text + "block-" + block.Code.Path + "\": \"" + Lang.GetNamePlaceHolder(block.Code) + "\",");
			}
		}
		for (int j = 0; j < server.Items.Count; j++)
		{
			Item item = server.Items[j];
			if (item != null && !(item.Code == null) && item.CreativeInventoryTabs != null && item.CreativeInventoryTabs.Length != 0 && item.GetHeldItemName(new ItemStack(item)) == item.Code?.Domain + ":item-" + item.Code?.Path)
			{
				string text2 = item.Code.ShortDomain();
				if (text2.Length > 0)
				{
					text2 += ":";
				}
				list.Add("\t\"" + text2 + "item-" + item.Code.Path + "\": \"" + Lang.GetNamePlaceHolder(item.Code) + "\",");
			}
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		server.api.eventapi.PushEvent("expclang", treeAttribute);
		foreach (KeyValuePair<string, IAttribute> item2 in treeAttribute)
		{
			string text3 = (item2.Value as StringAttribute)?.value;
			if (text3 != null)
			{
				list.Add(text3);
			}
		}
		list.Sort();
		string text4 = "collectiblelang.json";
		using (TextWriter textWriter = new StreamWriter(text4))
		{
			textWriter.Write(string.Join("\r\n", list));
			textWriter.Close();
		}
		return TextCommandResult.Success("Ok, Missing translations exported to " + text4);
	}

	private TextCommandResult getChunkStats(TextCommandCallingArgs args)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (KeyValuePair<long, ServerChunk> loadedChunk in server.loadedChunks)
			{
				num++;
				if (loadedChunk.Value.IsPacked())
				{
					num2++;
				}
				if (loadedChunk.Value.Empty)
				{
					num4++;
				}
				else
				{
					num3++;
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		ChunkDataPool serverChunkDataPool = server.serverChunkDataPool;
		return TextCommandResult.Success(string.Format("{0} Total chunks ({1} with data and {2} empty)\n{3} of which are packed\nFree pool objects {0}", num, num3, num4, num2, serverChunkDataPool.CountFree()));
	}

	private TextCommandResult getHereChunkInfo(TextCommandCallingArgs args)
	{
		int chunkX = (int)args.Caller.Pos.X / 32;
		int chunkY = (int)args.Caller.Pos.Y / 32;
		int chunkZ = (int)args.Caller.Pos.Z / 32;
		long num = server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		long num2 = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ConnectedClient clientByUID = server.GetClientByUID(args.Caller.Player.PlayerUID);
		bool flag = server.WorldMap.GetServerChunk(num) != null;
		bool flag2 = server.ChunkColumnRequested.ContainsKey(num2);
		bool flag3 = clientByUID.DidSendChunk(num);
		bool flag4 = server.requestedChunkColumns.Contains(num2);
		IServerChunk serverChunk = server.WorldMap.GetChunk(chunkX, chunkY, chunkZ) as IServerChunk;
		return TextCommandResult.Success(string.Format("Loaded: {0}, DidRequest: {1}, DidSend: {2}, InRequestQueue: {3}, your current chunk sent radius: {4}{5}, Player placed blocks: {6}, Player removed blocks: {7}", flag, flag2, flag3, flag4, clientByUID.CurrentChunkSentRadius, flag ? (", " + string.Format("Gameversioncreated: {0} , WorldGenVersion: {1}", serverChunk.GameVersionCreated ?? "1.10 or earlier", ((ServerMapChunk)serverChunk.MapChunk).WorldGenVersion)) : "", serverChunk.BlocksPlaced, serverChunk.BlocksRemoved));
	}

	private TextCommandResult resendChunk(TextCommandCallingArgs args)
	{
		Vec3d obj = args[0] as Vec3d;
		int chunkX = (int)obj.X / 32;
		int chunkY = (int)obj.Y / 32;
		int chunkZ = (int)obj.Z / 32;
		server.BroadcastChunk(chunkX, chunkY, chunkZ, onlyIfInRange: false);
		return TextCommandResult.Success("Ok, chunk now resent");
	}

	private TextCommandResult relightChunk(TextCommandCallingArgs args)
	{
		Vec3d obj = args[0] as Vec3d;
		int num = 32;
		int num2 = (int)obj.X / num;
		int num3 = (int)obj.Y / num;
		int num4 = (int)obj.Z / num;
		BlockPos minPos = new BlockPos(num2 * num, num3 * num, num4 * num);
		BlockPos maxPos = new BlockPos((num2 + 1) * num - 1, (num3 + 1) * num - 1, (num4 + 1) * num - 1);
		server.api.WorldManager.FullRelight(minPos, maxPos);
		return TextCommandResult.Success("Ok, chunk now relit");
	}

	private TextCommandResult exportTickHandlers(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		server.EventManager.defragLists();
		switch (text)
		{
		case "gtblock":
			dumpList(server.EventManager.GameTickListenersBlock);
			break;
		case "gtentity":
			dumpList(server.EventManager.GameTickListenersEntity);
			break;
		case "dcblock":
			dumpList(server.EventManager.DelayedCallbacksBlock);
			break;
		case "sdcblock":
			dumpList(server.EventManager.SingleDelayedCallbacksBlock.Values);
			break;
		case "dcentity":
			dumpList(server.EventManager.DelayedCallbacksEntity);
			break;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult countTickHandlers(TextCommandCallingArgs args)
	{
		server.EventManager.defragLists();
		return TextCommandResult.Success(Lang.Get("GameTickListenersBlock={0}, GameTickListenersEntity={1}, DelayedCallbacksBlock={2}, DelayedCallbacksEntity={3}, SingleDelayedCallbacksBlock={4}", server.EventManager.GameTickListenersBlock.Count, server.EventManager.GameTickListenersEntity.Count, server.EventManager.DelayedCallbacksBlock.Count, server.EventManager.DelayedCallbacksEntity.Count, server.EventManager.SingleDelayedCallbacksBlock.Count));
	}

	private void PrintOctagonPoints(int viewDistance)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		int num = (int)Math.Ceiling((float)viewDistance / (float)MagicNum.ServerChunkSize);
		for (int i = 1; i < num; i++)
		{
			Vec2i[] octagonPoints = ShapeUtil.GetOctagonPoints(num / 2 + 25, num / 2 + 25, i);
			SKBitmap val = new SKBitmap(num + 50, num + 50, false);
			Vec2i[] array = octagonPoints;
			foreach (Vec2i vec2i in array)
			{
				val.SetPixel(vec2i.X, vec2i.Y, new SKColor(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue));
			}
			val.Save("octapoints" + i + ".png");
		}
	}

	private void dumpList<T>(ICollection<T> list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (T item in list)
		{
			if (item is GameTickListener gameTickListener)
			{
				stringBuilder.AppendLine(gameTickListener.Origin().ToString() + ":" + gameTickListener.Handler.Method.ToString());
			}
			if (item is DelayedCallback delayedCallback)
			{
				stringBuilder.AppendLine(delayedCallback.Handler.Target.ToString() + ":" + delayedCallback.Handler.Method.ToString());
			}
			if (item is GameTickListenerBlock gameTickListenerBlock)
			{
				stringBuilder.AppendLine(gameTickListenerBlock.Handler.Target.ToString() + ":" + gameTickListenerBlock.Handler.Method.ToString());
			}
			if (item is DelayedCallbackBlock delayedCallbackBlock)
			{
				stringBuilder.AppendLine(delayedCallbackBlock.Handler.Target.ToString() + ":" + delayedCallbackBlock.Handler.Method.ToString());
			}
		}
		ServerMain.Logger.VerboseDebug(stringBuilder.ToString());
	}

	private TextCommandResult handleTestCond(TextCommandCallingArgs args)
	{
		if (args == null || args.ArgCount == 0)
		{
			return TextCommandResult.Error("Need to specify a condition");
		}
		string text = (string)args[0];
		string message = "";
		if (text.StartsWith("isBlock"))
		{
			message = IsBlockArgParser.Test(server.api, args.Caller, text);
		}
		return TextCommandResult.Success(message);
	}

	private StackTrace GetStackTrace(Thread targetThread)
	{
		StackTrace result = null;
		ManualResetEventSlim ready = new ManualResetEventSlim();
		new Thread((ThreadStart)delegate
		{
			ready.Set();
			Thread.Sleep(200);
			try
			{
				targetThread.Resume();
			}
			catch
			{
			}
		}).Start();
		ready.Wait();
		targetThread.Suspend();
		try
		{
		}
		finally
		{
			try
			{
				targetThread.Resume();
			}
			catch
			{
				result = null;
			}
		}
		return result;
	}
}
