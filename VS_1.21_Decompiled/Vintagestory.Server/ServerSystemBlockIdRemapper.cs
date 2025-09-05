using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemBlockIdRemapper : ServerSystem
{
	public ServerSystemBlockIdRemapper(ServerMain server)
		: base(server)
	{
		server.ModEventManager.AssetsFirstLoaded += RemapBlocks;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("bir").RequiresPrivilege(Privilege.controlserver).WithDescription("Block id remapper info and fixing tool")
			.BeginSubCommand("list")
			.WithDescription("list")
			.HandleWith(OnCmdList)
			.EndSubCommand()
			.BeginSubCommand("getcode")
			.WithDescription("getcode")
			.WithArgs(parsers.Int("blockId"))
			.HandleWith(OnCmdGetcode)
			.EndSubCommand()
			.BeginSubCommand("getid")
			.WithDescription("getid")
			.WithArgs(parsers.Word("domainAndPath"))
			.HandleWith(OnCmdGetid)
			.EndSubCommand()
			.BeginSubCommand("map")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_block"), parsers.Word("old_block"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdMap)
			.EndSubCommand()
			.BeginSubCommand("remap")
			.WithAlias("remapq")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_block"), parsers.Word("old_block"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdReMap)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		bool quiet = args.SubCmdCode == "remapq";
		string text = args[0] as string;
		string text2 = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(text, out var result) && int.TryParse(text2, out var result2))
		{
			MapById(storedBlockCodesById, result, result2, player, args.Caller.FromChatGroupId, remap: true, force, quiet);
			return TextCommandResult.Success();
		}
		MapByCode(storedBlockCodesById, new AssetLocation(text), new AssetLocation(text2), player, args.Caller.FromChatGroupId, remap: true, force, quiet);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		string text = args[0] as string;
		string text2 = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(text, out var result) && int.TryParse(text2, out var result2))
		{
			MapById(storedBlockCodesById, result, result2, player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
			return TextCommandResult.Success();
		}
		MapByCode(storedBlockCodesById, new AssetLocation(text), new AssetLocation(text2), player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGetid(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredBlockCodesById();
		string domainAndPath = args[0] as string;
		if (!dictionary.ContainsValue(new AssetLocation(domainAndPath)))
		{
			return TextCommandResult.Success("No mapping for blockcode " + domainAndPath + " found");
		}
		return TextCommandResult.Success("Blockcode " + domainAndPath + " is currently mapped to " + dictionary.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(new AssetLocation(domainAndPath))).Key);
	}

	private TextCommandResult OnCmdGetcode(TextCommandCallingArgs args)
	{
		int key = (int)args[0];
		Dictionary<int, AssetLocation> dictionary = LoadStoredBlockCodesById();
		if (!dictionary.ContainsKey(key))
		{
			return TextCommandResult.Success("No mapping for blockid " + key + " found");
		}
		return TextCommandResult.Success("Blockid " + key + " is currently mapped to " + dictionary[key]);
	}

	private TextCommandResult OnCmdList(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredBlockCodesById();
		ServerMain.Logger.Notification("Current block id mapping (issued by /bir list command)");
		foreach (KeyValuePair<int, AssetLocation> item in dictionary)
		{
			ServerMain.Logger.Notification("  " + item.Key + ": " + item.Value);
		}
		return TextCommandResult.Success("Full mapping printed to console and main log file");
	}

	private void MapById(Dictionary<int, AssetLocation> storedBlockCodesById, int newBlockId, int oldBlockId, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!force && storedBlockCodesById.TryGetValue(oldBlockId, out var value))
		{
			player.SendMessage(groupId, string.Concat("newblockid ", oldBlockId.ToString(), " is already mapped to ", value, ", type '/bir ", remap ? "remap" : "map", " ", newBlockId.ToString(), " ", oldBlockId.ToString(), " force' to overwrite"), EnumChatType.CommandError);
			return;
		}
		AssetLocation assetLocation = (storedBlockCodesById[oldBlockId] = storedBlockCodesById[newBlockId]);
		if (remap)
		{
			storedBlockCodesById.Remove(newBlockId);
		}
		if (!quiet)
		{
			string text = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(assetLocation, " is now ", text, " to id ", oldBlockId.ToString()), EnumChatType.CommandSuccess);
		}
		StoreBlockCodesById(storedBlockCodesById);
	}

	private void MapByCode(Dictionary<int, AssetLocation> storedBlockCodesById, AssetLocation newCode, AssetLocation oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!storedBlockCodesById.ContainsValue(newCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for blockcode ", newCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!storedBlockCodesById.ContainsValue(oldCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for blockcode ", oldCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!force)
		{
			player.SendMessage(groupId, string.Concat("Both block codes found. Type '/bir ", remap ? "remap" : "map", " ", newCode, " ", oldCode, " force' to make the remap permanent."), EnumChatType.CommandError);
			return;
		}
		int key = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(newCode)).Key;
		int key2 = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(oldCode)).Key;
		storedBlockCodesById[key2] = newCode;
		if (remap)
		{
			storedBlockCodesById.Remove(key);
		}
		if (!quiet)
		{
			string text = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(newCode, " is now ", text, " to id ", key2.ToString()), EnumChatType.CommandSuccess);
		}
		StoreBlockCodesById(storedBlockCodesById);
	}

	private void RemapBlocks()
	{
		ServerMain.Logger.Event("Remapping blocks and items...");
		ServerMain.Logger.VerboseDebug("BlockID Remapper: Begin");
		Dictionary<AssetLocation, int> dictionary = new Dictionary<AssetLocation, int>();
		Dictionary<int, AssetLocation> dictionary2 = new Dictionary<int, AssetLocation>();
		Dictionary<int, AssetLocation> dictionary3 = new Dictionary<int, AssetLocation>();
		Dictionary<int, int> dictionary4 = new Dictionary<int, int>();
		for (int i = 0; i < server.Blocks.Count; i++)
		{
			Block block = server.Blocks[i];
			if (block != null && !(block.Code == null))
			{
				dictionary[block.Code] = block.BlockId;
			}
		}
		Dictionary<int, AssetLocation> dictionary5 = LoadStoredBlockCodesById();
		if (dictionary5 == null)
		{
			dictionary5 = new Dictionary<int, AssetLocation>();
		}
		if (server.Config.RepairMode)
		{
			int val = 0;
			Dictionary<string, int> dictionary6 = new Dictionary<string, int>();
			server.api.Logger.Notification("Stored blocks by mod domain:");
			foreach (KeyValuePair<int, AssetLocation> item2 in dictionary5)
			{
				AssetLocation value = item2.Value;
				dictionary6.TryGetValue(value.Domain, out var value2);
				dictionary6[value.Domain] = value2 + 1;
				val = Math.Max(val, item2.Key);
			}
			foreach (KeyValuePair<string, int> item3 in dictionary6)
			{
				ServerMain.Logger.Notification("{0}: {1}", item3.Key, item3.Value);
			}
		}
		int num = 0;
		foreach (KeyValuePair<int, AssetLocation> item4 in dictionary5)
		{
			AssetLocation value3 = item4.Value;
			int key = item4.Key;
			num = Math.Max(key, num);
			if (!dictionary.TryGetValue(value3, out var value4))
			{
				dictionary3.Add(key, value3);
			}
			else if (value4 != key)
			{
				dictionary4[value4] = key;
			}
		}
		for (int j = 0; j < server.Blocks.Count; j++)
		{
			Block block2 = server.Blocks[j];
			if (block2 != null)
			{
				num = Math.Max(block2.BlockId, num);
			}
		}
		server.nextFreeBlockId = num + 1;
		ServerMain.Logger.VerboseDebug("Max BlockID is " + num);
		bool flag = dictionary5.Count == 0;
		HashSet<AssetLocation> hashSet = new HashSet<AssetLocation>(dictionary5.Values);
		foreach (KeyValuePair<AssetLocation, int> item5 in dictionary)
		{
			AssetLocation key2 = item5.Key;
			if (!hashSet.Contains(key2))
			{
				dictionary2[item5.Value] = key2;
			}
		}
		ServerMain.Logger.VerboseDebug("Found {0} blocks requiring remapping and {1} new blocks", dictionary4.Count, dictionary2.Count);
		StringBuilder stringBuilder = new StringBuilder();
		List<Block> list = new List<Block>();
		foreach (KeyValuePair<int, AssetLocation> item6 in dictionary2)
		{
			int key3 = item6.Key;
			Block item = server.Blocks[key3];
			list.Add(item);
			if (!flag)
			{
				server.Blocks[key3] = new Block
				{
					BlockId = key3
				};
			}
		}
		List<Block> list2 = new List<Block>();
		int num2 = 0;
		foreach (KeyValuePair<int, int> item7 in dictionary4)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			int key4 = item7.Key;
			int value5 = item7.Value;
			num2 = Math.Max(num2, value5);
			Block block3 = server.Blocks[key4];
			block3.BlockId = value5;
			list2.Add(block3);
			server.Blocks[key4] = new Block
			{
				BlockId = key4,
				IsMissing = true
			};
			stringBuilder.Append(key4 + "=>" + value5);
		}
		(server.Blocks as BlockList).PreAlloc(num2);
		foreach (Block item8 in list2)
		{
			server.RemapBlock(item8);
		}
		if (!flag)
		{
			int num3 = 0;
			foreach (Block item9 in list)
			{
				if (item9.BlockId != 0)
				{
					server.BlocksByCode.Remove(item9.Code);
					server.RegisterBlock(item9);
					num3++;
				}
			}
			ServerMain.Logger.VerboseDebug("Remapped {0} new blockids", num3);
		}
		num = 0;
		for (int k = 0; k < server.Blocks.Count; k++)
		{
			if (server.Blocks[k] != null)
			{
				num = Math.Max(server.Blocks[k].BlockId, num);
			}
		}
		server.nextFreeBlockId = num + 1;
		if (stringBuilder.Length > 0)
		{
			ServerMain.Logger.VerboseDebug("Remapped {0} existing blockids", dictionary4.Count);
		}
		ServerMain.Logger.Debug("Found {0} missing blocks", dictionary3.Count);
		stringBuilder = new StringBuilder();
		FastSmallDictionary<string, CompositeTexture> textures = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
		foreach (KeyValuePair<int, AssetLocation> item10 in dictionary3)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			server.FillMissingBlock(item10.Key, new Block
			{
				Textures = textures,
				Code = item10.Value,
				DrawType = EnumDrawType.Cube,
				MatterState = EnumMatterState.Solid,
				IsMissing = true,
				Replaceable = 1
			});
			stringBuilder.Append(item10.Value.ToShortString());
		}
		if (stringBuilder.Length > 0)
		{
			ServerMain.Logger.Debug("Added unknown block for {0} blocks", dictionary3.Count);
			ServerMain.Logger.Debug(stringBuilder.ToString());
		}
		foreach (Block item11 in list)
		{
			dictionary5[item11.BlockId] = item11.Code;
		}
		if (list.Count > 0)
		{
			ServerMain.Logger.Debug("Added {0} new blocks to the mapping", list.Count);
		}
		StoreBlockCodesById(dictionary5);
	}

	public Dictionary<int, AssetLocation> LoadStoredBlockCodesById()
	{
		Dictionary<int, AssetLocation> dictionary = new Dictionary<int, AssetLocation>();
		try
		{
			byte[] data = server.api.WorldManager.SaveGame.GetData("BlockIDs");
			if (data != null)
			{
				Dictionary<int, string> dictionary2 = Serializer.Deserialize<Dictionary<int, string>>((Stream)new MemoryStream(data));
				dictionary = new Dictionary<int, AssetLocation>();
				foreach (KeyValuePair<int, string> item in dictionary2)
				{
					dictionary.Add(item.Key, new AssetLocation(item.Value));
				}
				ServerMain.Logger.VerboseDebug(dictionary.Count + " block IDs loaded from savegame.");
			}
			else
			{
				ServerMain.Logger.Debug("Block IDs not found in savegame.");
			}
		}
		catch
		{
			throw new Exception("Error at loading blocks!");
		}
		return dictionary;
	}

	public void StoreBlockCodesById(Dictionary<int, AssetLocation> storedBlockCodesById)
	{
		int val = 0;
		MemoryStream memoryStream = new MemoryStream();
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		foreach (KeyValuePair<int, AssetLocation> item in storedBlockCodesById)
		{
			val = Math.Max(val, item.Key);
			dictionary.Add(item.Key, item.Value.ToShortString());
		}
		Serializer.Serialize<Dictionary<int, string>>((Stream)memoryStream, dictionary);
		server.api.WorldManager.SaveGame.StoreData("BlockIDs", memoryStream.ToArray());
		ServerMain.Logger.Debug("Block IDs have been written to savegame. Saved max BlockID was " + val);
	}
}
