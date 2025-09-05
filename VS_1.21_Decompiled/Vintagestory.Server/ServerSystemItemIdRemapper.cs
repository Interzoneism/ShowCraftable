using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemItemIdRemapper : ServerSystem
{
	public ServerSystemItemIdRemapper(ServerMain server)
		: base(server)
	{
		server.ModEventManager.AssetsFirstLoaded += RemapItems;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("iir").RequiresPrivilege(Privilege.controlserver).WithDescription("Item id remapper info and fixing tool")
			.BeginSubCommand("list")
			.WithDescription("list")
			.HandleWith(OnCmdList)
			.EndSubCommand()
			.BeginSubCommand("getcode")
			.WithDescription("getcode")
			.WithArgs(parsers.Int("itemId"))
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
			.WithArgs(parsers.Word("new_item"), parsers.Word("old_item"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdMap)
			.EndSubCommand()
			.BeginSubCommand("remap")
			.WithAlias("remapq")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_item"), parsers.Word("old_item"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdReMap)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedItemCodesById = LoadStoredItemCodesById();
		bool quiet = args.SubCmdCode == "remapq";
		string text = args[0] as string;
		string text2 = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(text, out var result) && int.TryParse(text2, out var result2))
		{
			MapById(storedItemCodesById, result, result2, player, args.Caller.FromChatGroupId, remap: true, force, quiet);
			return TextCommandResult.Success();
		}
		MapByCode(storedItemCodesById, new AssetLocation(text), new AssetLocation(text2), player, args.Caller.FromChatGroupId, remap: true, force, quiet);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedItemCodesById = LoadStoredItemCodesById();
		string text = args[0] as string;
		string text2 = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(text, out var result) && int.TryParse(text2, out var result2))
		{
			MapById(storedItemCodesById, result, result2, player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
			return TextCommandResult.Success();
		}
		MapByCode(storedItemCodesById, new AssetLocation(text), new AssetLocation(text2), player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGetid(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredItemCodesById();
		string domainAndPath = args[0] as string;
		if (!dictionary.ContainsValue(new AssetLocation(domainAndPath)))
		{
			return TextCommandResult.Success("No mapping for itemcode " + domainAndPath + " found");
		}
		return TextCommandResult.Success("Itemcode " + domainAndPath + " is currently mapped to " + dictionary.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(new AssetLocation(domainAndPath))).Key);
	}

	private TextCommandResult OnCmdGetcode(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredItemCodesById();
		int key = (int)args[0];
		if (!dictionary.ContainsKey(key))
		{
			return TextCommandResult.Success("No mapping for itemid " + key + " found");
		}
		return TextCommandResult.Success("itemid " + key + " is currently mapped to " + dictionary[key]);
	}

	private TextCommandResult OnCmdList(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredItemCodesById();
		ServerMain.Logger.Notification("Current item id mapping (issued by /bir list command)");
		foreach (KeyValuePair<int, AssetLocation> item in dictionary)
		{
			ServerMain.Logger.Notification("  " + item.Key + ": " + item.Value);
		}
		return TextCommandResult.Success("Full mapping printed to console and main log file");
	}

	private void MapById(Dictionary<int, AssetLocation> storedItemCodesById, int newItemId, int oldItemId, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!force && storedItemCodesById.TryGetValue(oldItemId, out var value))
		{
			player.SendMessage(groupId, string.Concat("newitemid ", oldItemId.ToString(), " is already mapped to ", value, ", type '/bir ", remap ? "remap" : "map", " ", newItemId.ToString(), " ", oldItemId.ToString(), " force' to overwrite"), EnumChatType.CommandError);
			return;
		}
		AssetLocation assetLocation = (storedItemCodesById[oldItemId] = storedItemCodesById[newItemId]);
		if (remap)
		{
			storedItemCodesById.Remove(newItemId);
		}
		if (!quiet)
		{
			string text = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(assetLocation, " is now ", text, " to id ", oldItemId.ToString()), EnumChatType.CommandSuccess);
		}
		StoreItemCodesById(storedItemCodesById);
	}

	private void MapByCode(Dictionary<int, AssetLocation> storedItemCodesById, AssetLocation newCode, AssetLocation oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!storedItemCodesById.ContainsValue(newCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for itemcode ", newCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!storedItemCodesById.ContainsValue(oldCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for itemcode ", oldCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!force)
		{
			player.SendMessage(groupId, string.Concat("Both item codes found. Type '/bir ", remap ? "remap" : "map", " ", newCode, " ", oldCode, " force' to make the remap permanent."), EnumChatType.CommandError);
			return;
		}
		int key = storedItemCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(newCode)).Key;
		int key2 = storedItemCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(oldCode)).Key;
		storedItemCodesById[key2] = newCode;
		if (remap)
		{
			storedItemCodesById.Remove(key);
		}
		if (!quiet)
		{
			string text = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(newCode, " is now ", text, " to id ", key2.ToString()), EnumChatType.CommandSuccess);
		}
		StoreItemCodesById(storedItemCodesById);
	}

	public void RemapItems()
	{
		ServerMain.Logger.Debug("ItemID Remapper: Begin");
		Dictionary<AssetLocation, int> dictionary = new Dictionary<AssetLocation, int>();
		Dictionary<int, AssetLocation> dictionary2 = new Dictionary<int, AssetLocation>();
		Dictionary<int, AssetLocation> dictionary3 = new Dictionary<int, AssetLocation>();
		Dictionary<int, AssetLocation> dictionary4 = new Dictionary<int, AssetLocation>();
		Dictionary<int, int> dictionary5 = new Dictionary<int, int>();
		for (int i = 0; i < server.Items.Count; i++)
		{
			Item item = server.Items[i];
			if (item != null && !(item.Code == null))
			{
				dictionary[item.Code] = item.ItemId;
			}
		}
		dictionary2 = LoadStoredItemCodesById();
		if (dictionary2 == null)
		{
			dictionary2 = new Dictionary<int, AssetLocation>();
		}
		int num = 0;
		foreach (KeyValuePair<int, AssetLocation> item4 in dictionary2)
		{
			AssetLocation value = item4.Value;
			int key = item4.Key;
			num = Math.Max(key, num);
			if (!dictionary.ContainsKey(value))
			{
				dictionary4.Add(key, value);
				continue;
			}
			int num2 = dictionary[value];
			if (num2 != key)
			{
				dictionary5[num2] = key;
			}
		}
		for (int j = 0; j < server.Items.Count; j++)
		{
			if (server.Items[j] != null)
			{
				num = Math.Max(server.Items[j].ItemId, num);
			}
		}
		server.nextFreeItemId = num + 1;
		bool flag = dictionary2.Count == 0;
		HashSet<AssetLocation> hashSet = new HashSet<AssetLocation>(dictionary2.Values);
		foreach (KeyValuePair<AssetLocation, int> item5 in dictionary)
		{
			AssetLocation key2 = item5.Key;
			int value2 = item5.Value;
			if (value2 != 0 && !hashSet.Contains(key2))
			{
				dictionary3[value2] = key2;
			}
		}
		ServerMain.Logger.Debug("Found {0} Item requiring remapping", dictionary5.Count);
		StringBuilder stringBuilder = new StringBuilder();
		List<Item> list = new List<Item>();
		foreach (KeyValuePair<int, AssetLocation> item6 in dictionary3)
		{
			int key3 = item6.Key;
			Item item2 = server.Items[key3];
			list.Add(item2);
			if (!flag)
			{
				server.Items[key3] = new Item();
			}
		}
		List<Item> list2 = new List<Item>();
		foreach (KeyValuePair<int, int> item7 in dictionary5)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			int key4 = item7.Key;
			int value3 = item7.Value;
			Item item3 = server.Items[key4];
			item3.ItemId = value3;
			list2.Add(item3);
			server.Items[key4] = new Item();
			stringBuilder.Append(key4 + "=>" + value3);
		}
		foreach (Item item8 in list2)
		{
			server.RemapItem(item8);
		}
		if (!flag)
		{
			int num3 = 0;
			foreach (Item item9 in list)
			{
				if (item9.ItemId != 0)
				{
					server.ItemsByCode.Remove(item9.Code);
					server.RegisterItem(item9);
					num3++;
				}
			}
			ServerMain.Logger.Debug("Remapped {0} new Itemids", num3);
		}
		num = 0;
		for (int k = 0; k < server.Items.Count; k++)
		{
			if (server.Items[k] != null)
			{
				num = Math.Max(server.Items[k].ItemId, num);
			}
		}
		server.nextFreeItemId = num + 1;
		if (stringBuilder.Length > 0)
		{
			ServerMain.Logger.VerboseDebug("Remapped existing Itemids: {0}", stringBuilder.ToString());
		}
		ServerMain.Logger.Debug("Found {0} missing Items", dictionary4.Count);
		stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, AssetLocation> item10 in dictionary4)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			server.FillMissingItem(item10.Key, new Item
			{
				Textures = new Dictionary<string, CompositeTexture> { 
				{
					"all",
					new CompositeTexture(new AssetLocation("unknown"))
				} },
				IsMissing = true,
				Code = item10.Value
			});
			stringBuilder.Append(item10.Value.ToShortString());
		}
		if (stringBuilder.Length > 0)
		{
			ServerMain.Logger.Debug("Added unknown Item for {0} Items", dictionary4.Count);
			ServerMain.Logger.Debug(stringBuilder.ToString());
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (Item item11 in list)
		{
			dictionary2[item11.ItemId] = item11.Code;
			if (stringBuilder2.Length > 0)
			{
				stringBuilder2.Append(", ");
			}
			stringBuilder2.Append(string.Concat(item11.Code, "(", item11.ItemId.ToString(), ")"));
		}
		if (list.Count > 0)
		{
			ServerMain.Logger.Debug("Added {0} new Items to the mapping", list.Count);
		}
		StoreItemCodesById(dictionary2);
	}

	public Dictionary<int, AssetLocation> LoadStoredItemCodesById()
	{
		Dictionary<int, AssetLocation> dictionary = new Dictionary<int, AssetLocation>();
		try
		{
			byte[] data = server.api.WorldManager.SaveGame.GetData("ItemIDs");
			if (data != null)
			{
				Dictionary<int, string> dictionary2 = Serializer.Deserialize<Dictionary<int, string>>((Stream)new MemoryStream(data));
				dictionary = new Dictionary<int, AssetLocation>();
				foreach (KeyValuePair<int, string> item in dictionary2)
				{
					dictionary.Add(item.Key, new AssetLocation(item.Value));
				}
				ServerMain.Logger.Debug("Item IDs loaded from savegame.");
			}
			else
			{
				ServerMain.Logger.Debug("Item IDs not found in savegame.");
			}
		}
		catch
		{
			throw new Exception("Error at loading Items!");
		}
		return dictionary;
	}

	public void StoreItemCodesById(Dictionary<int, AssetLocation> storedItemCodesById)
	{
		MemoryStream memoryStream = new MemoryStream();
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		foreach (KeyValuePair<int, AssetLocation> item in storedItemCodesById)
		{
			dictionary.Add(item.Key, item.Value.ToShortString());
		}
		Serializer.Serialize<Dictionary<int, string>>((Stream)memoryStream, dictionary);
		server.api.WorldManager.SaveGame.StoreData("ItemIDs", memoryStream.ToArray());
		ServerMain.Logger.Debug("Item IDs have been written to savegame");
	}
}
