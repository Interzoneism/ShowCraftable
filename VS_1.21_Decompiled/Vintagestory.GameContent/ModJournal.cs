using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModJournal : ModSystem
{
	private ICoreServerAPI sapi;

	private Dictionary<string, Journal> journalsByPlayerUid = new Dictionary<string, Journal>();

	private Dictionary<string, Dictionary<string, LoreDiscovery>> loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();

	private Dictionary<string, JournalAsset> journalAssetsByCode;

	private IServerNetworkChannel serverChannel;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	private Journal ownJournal = new Journal();

	private GuiDialogJournal dialog;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Input.RegisterHotKey("journal", Lang.Get("Journal"), GlKeys.J);
		capi.Input.SetHotKeyHandler("journal", OnHotkeyJournal);
		clientChannel = api.Network.RegisterChannel("journal").RegisterMessageType(typeof(JournalEntry)).RegisterMessageType(typeof(Journal))
			.RegisterMessageType(typeof(JournalChapter))
			.SetMessageHandler<Journal>(OnJournalItemsReceived)
			.SetMessageHandler<JournalEntry>(OnJournalItemReceived)
			.SetMessageHandler<JournalChapter>(OnJournalPieceReceived);
	}

	private bool OnHotkeyJournal(KeyCombination comb)
	{
		if (dialog != null)
		{
			dialog.TryClose();
			dialog = null;
			return true;
		}
		dialog = new GuiDialogJournal(ownJournal.Entries, capi);
		dialog.TryOpen();
		dialog.OnClosed += delegate
		{
			dialog = null;
		};
		return true;
	}

	private void OnJournalPieceReceived(JournalChapter entryPiece)
	{
		ownJournal.Entries[entryPiece.EntryId].Chapters.Add(entryPiece);
	}

	private void OnJournalItemReceived(JournalEntry entry)
	{
		if (entry.EntryId >= ownJournal.Entries.Count)
		{
			ownJournal.Entries.Add(entry);
		}
		else
		{
			ownJournal.Entries[entry.EntryId] = entry;
		}
	}

	private void OnJournalItemsReceived(Journal fullJournal)
	{
		ownJournal = fullJournal;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.PlayerJoin += OnPlayerJoin;
		api.Event.SaveGameLoaded += OnSaveGameLoaded;
		api.Event.GameWorldSave += OnGameGettingSaved;
		serverChannel = api.Network.RegisterChannel("journal").RegisterMessageType(typeof(JournalEntry)).RegisterMessageType(typeof(Journal))
			.RegisterMessageType(typeof(JournalChapter));
		api.Event.RegisterEventBusListener(OnLoreDiscovery, 0.5, "loreDiscovery");
	}

	private void OnGameGettingSaved()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		sapi.WorldManager.SaveGame.StoreData("journalItemsByPlayerUid", SerializerUtil.Serialize(journalsByPlayerUid, ms));
		sapi.WorldManager.SaveGame.StoreData("loreDiscoveriesByPlayerUid", SerializerUtil.Serialize(loreDiscoveryiesByPlayerUid, ms));
	}

	private void OnSaveGameLoaded()
	{
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("journalItemsByPlayerUid");
			if (GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.LastSavedGameVersion, "1.14-pre.1"))
			{
				if (data != null)
				{
					journalsByPlayerUid = upgrade(SerializerUtil.Deserialize<Dictionary<string, JournalOld>>(data));
				}
				sapi.World.Logger.Notification("Upgraded journalItemsByPlayerUid from v1.13 format to v1.14 format");
			}
			else if (data != null)
			{
				journalsByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Journal>>(data);
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed loading journalItemsByPlayerUid. Resetting.");
			sapi.World.Logger.Error(e);
		}
		if (journalsByPlayerUid == null)
		{
			journalsByPlayerUid = new Dictionary<string, Journal>();
		}
		try
		{
			byte[] data2 = sapi.WorldManager.SaveGame.GetData("loreDiscoveriesByPlayerUid");
			if (data2 != null)
			{
				loreDiscoveryiesByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Dictionary<string, LoreDiscovery>>>(data2);
			}
		}
		catch (Exception ex)
		{
			sapi.World.Logger.Error("Failed loading loreDiscoveryiesByPlayerUid. Resetting. Exception: {0}", ex);
		}
		if (loreDiscoveryiesByPlayerUid == null)
		{
			loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();
		}
	}

	private Dictionary<string, Journal> upgrade(Dictionary<string, JournalOld> dict)
	{
		Dictionary<string, Journal> dictionary = new Dictionary<string, Journal>();
		foreach (KeyValuePair<string, JournalOld> item in dict)
		{
			List<JournalEntry> list = new List<JournalEntry>();
			foreach (JournalEntryOld entry in item.Value.Entries)
			{
				List<JournalChapter> list2 = new List<JournalChapter>();
				foreach (JournalChapterOld chapter in entry.Chapters)
				{
					list2.Add(new JournalChapter
					{
						Text = chapter.Text,
						EntryId = chapter.EntryId
					});
				}
				list.Add(new JournalEntry
				{
					Chapters = list2,
					Editable = entry.Editable,
					EntryId = entry.EntryId,
					LoreCode = entry.LoreCode,
					Title = entry.Title
				});
			}
			dictionary[item.Key] = new Journal
			{
				Entries = list
			};
		}
		return dictionary;
	}

	private void OnPlayerJoin(IServerPlayer byPlayer)
	{
		if (journalsByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var value))
		{
			serverChannel.SendPacket(value, byPlayer);
		}
	}

	public void AddOrUpdateJournalEntry(IServerPlayer forPlayer, JournalEntry entry)
	{
		if (!journalsByPlayerUid.TryGetValue(forPlayer.PlayerUID, out var value))
		{
			value = (journalsByPlayerUid[forPlayer.PlayerUID] = new Journal());
		}
		for (int i = 0; i < value.Entries.Count; i++)
		{
			if (value.Entries[i].LoreCode == entry.LoreCode)
			{
				value.Entries[i] = entry;
				serverChannel.SendPacket(entry, forPlayer);
				return;
			}
		}
		value.Entries.Add(entry);
		serverChannel.SendPacket(entry, forPlayer);
	}

	private void OnLoreDiscovery(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute obj = data as TreeAttribute;
		string playerUid = obj.GetString("playeruid");
		string category = obj.GetString("category");
		IServerPlayer serverPlayer = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
		ItemSlot activeHotbarSlot = serverPlayer.InventoryManager.ActiveHotbarSlot;
		string text = activeHotbarSlot.Itemstack.Attributes.GetString("discoveryCode");
		LoreDiscovery loreDiscovery;
		if (text != null)
		{
			int[] value = (activeHotbarSlot.Itemstack.Attributes["chapterIds"] as IntArrayAttribute).value;
			loreDiscovery = new LoreDiscovery
			{
				Code = text,
				ChapterIds = new List<int>(value)
			};
		}
		else
		{
			loreDiscovery = getRandomLoreDiscovery(sapi.World, serverPlayer, category);
		}
		if (loreDiscovery == null)
		{
			if (text == null)
			{
				serverPlayer.SendIngameError("alreadydiscovered", Lang.Get("Nothing new in these pages"));
			}
		}
		else if (TryDiscoverLore(loreDiscovery, serverPlayer, activeHotbarSlot))
		{
			activeHotbarSlot.MarkDirty();
			serverPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), serverPlayer.Entity);
			handling = EnumHandling.PreventDefault;
		}
	}

	public bool TryDiscoverLore(LoreDiscovery newdiscovery, IServerPlayer plr, ItemSlot slot = null)
	{
		string playerUID = plr.PlayerUID;
		if (!journalsByPlayerUid.TryGetValue(playerUID, out var value))
		{
			value = (journalsByPlayerUid[playerUID] = new Journal());
		}
		JournalEntry journalEntry = null;
		ensureJournalAssetsLoaded();
		JournalAsset asset = journalAssetsByCode[newdiscovery.Code];
		for (int i = 0; i < value.Entries.Count; i++)
		{
			if (value.Entries[i].LoreCode == newdiscovery.Code)
			{
				journalEntry = value.Entries[i];
				break;
			}
		}
		bool flag = false;
		if (journalEntry == null)
		{
			List<JournalEntry> entries = value.Entries;
			JournalEntry obj = new JournalEntry
			{
				Editable = false,
				Title = asset.Title,
				LoreCode = newdiscovery.Code,
				EntryId = value.Entries.Count
			};
			journalEntry = obj;
			entries.Add(obj);
			flag = true;
		}
		bool flag2 = false;
		loreDiscoveryiesByPlayerUid.TryGetValue(plr.PlayerUID, out var value2);
		if (value2 == null)
		{
			value2 = (loreDiscoveryiesByPlayerUid[plr.PlayerUID] = new Dictionary<string, LoreDiscovery>());
		}
		if (value2.TryGetValue(asset.Code, out var value3))
		{
			foreach (int chapterId in newdiscovery.ChapterIds)
			{
				if (!value3.ChapterIds.Contains(chapterId))
				{
					value3.ChapterIds.Add(chapterId);
					flag2 = true;
				}
			}
		}
		else
		{
			value2[asset.Code] = newdiscovery;
			flag2 = true;
		}
		if (!flag2)
		{
			return false;
		}
		int num = 0;
		int num2 = asset.Pieces.Length;
		for (int j = 0; j < newdiscovery.ChapterIds.Count; j++)
		{
			JournalChapter journalChapter = new JournalChapter
			{
				Text = asset.Pieces[newdiscovery.ChapterIds[j]],
				EntryId = journalEntry.EntryId,
				ChapterId = newdiscovery.ChapterIds[j]
			};
			journalEntry.Chapters.Add(journalChapter);
			if (!flag)
			{
				serverChannel.SendPacket(journalChapter, plr);
			}
			num = newdiscovery.ChapterIds[j];
		}
		if (slot != null)
		{
			slot.Itemstack.Attributes.SetString("discoveryCode", newdiscovery.Code);
			slot.Itemstack.Attributes["chapterIds"] = new IntArrayAttribute(newdiscovery.ChapterIds.ToArray());
			slot.Itemstack.Attributes["textCodes"] = new StringArrayAttribute(newdiscovery.ChapterIds.Select((int id) => asset.Pieces[id]).ToArray());
			slot.Itemstack.Attributes.SetString("titleCode", journalEntry.Title);
			slot.MarkDirty();
		}
		if (flag)
		{
			serverChannel.SendPacket(journalEntry, plr);
		}
		sapi.SendIngameDiscovery(plr, "lore-" + newdiscovery.Code, null, num + 1, num2);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/deepbell"), plr.Entity, null, randomizePitch: false, 5f, 0.5f);
		return true;
	}

	protected TextCommandResult DiscoverEverything(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		JournalAsset[] array = sapi.World.AssetManager.GetMany<JournalAsset>(sapi.World.Logger, "config/lore/").Values.ToArray();
		if (!journalsByPlayerUid.TryGetValue(serverPlayer.PlayerUID, out var value))
		{
			value = (journalsByPlayerUid[serverPlayer.PlayerUID] = new Journal());
		}
		value.Entries.Clear();
		JournalAsset[] array2 = array;
		foreach (JournalAsset journalAsset in array2)
		{
			JournalEntry journalEntry = null;
			value.Entries.Add(journalEntry = new JournalEntry
			{
				Editable = false,
				Title = journalAsset.Title,
				LoreCode = journalAsset.Code,
				EntryId = value.Entries.Count
			});
			serverChannel.SendPacket(journalEntry, serverPlayer);
			string[] pieces = journalAsset.Pieces;
			foreach (string text in pieces)
			{
				JournalChapter journalChapter = new JournalChapter
				{
					Text = text,
					EntryId = journalEntry.EntryId
				};
				journalEntry.Chapters.Add(journalChapter);
				serverChannel.SendPacket(journalChapter, serverPlayer);
			}
		}
		return TextCommandResult.Success("All lore added");
	}

	private LoreDiscovery getRandomLoreDiscovery(IWorldAccessor world, IPlayer serverplayer, string category)
	{
		ensureJournalAssetsLoaded();
		JournalAsset[] array = journalAssetsByCode.Values.ToArray();
		array.Shuffle(world.Rand);
		foreach (JournalAsset journalAsset in array)
		{
			if (category == null || !(journalAsset.Category != category))
			{
				LoreDiscovery nextUndiscoveredChapter = getNextUndiscoveredChapter(serverplayer, journalAsset);
				if (nextUndiscoveredChapter != null)
				{
					return nextUndiscoveredChapter;
				}
			}
		}
		return null;
	}

	private LoreDiscovery getNextUndiscoveredChapter(IPlayer plr, JournalAsset asset)
	{
		loreDiscoveryiesByPlayerUid.TryGetValue(plr.PlayerUID, out var value);
		if (value == null)
		{
			value = (loreDiscoveryiesByPlayerUid[plr.PlayerUID] = new Dictionary<string, LoreDiscovery>());
		}
		if (!value.ContainsKey(asset.Code))
		{
			return new LoreDiscovery
			{
				Code = asset.Code,
				ChapterIds = new List<int> { 0 }
			};
		}
		LoreDiscovery loreDiscovery = value[asset.Code];
		for (int i = 0; i < asset.Pieces.Length; i++)
		{
			if (!loreDiscovery.ChapterIds.Contains(i))
			{
				return new LoreDiscovery
				{
					ChapterIds = new List<int> { i },
					Code = loreDiscovery.Code
				};
			}
		}
		return null;
	}

	private void ensureJournalAssetsLoaded()
	{
		if (journalAssetsByCode == null)
		{
			journalAssetsByCode = new Dictionary<string, JournalAsset>();
			JournalAsset[] array = sapi.World.AssetManager.GetMany<JournalAsset>(sapi.World.Logger, "config/lore/").Values.ToArray();
			foreach (JournalAsset journalAsset in array)
			{
				journalAssetsByCode[journalAsset.Code] = journalAsset;
			}
		}
	}

	public bool DidDiscoverLore(string playerUid, string code, int chapterId)
	{
		if (!journalsByPlayerUid.TryGetValue(playerUid, out var value))
		{
			return false;
		}
		for (int i = 0; i < value.Entries.Count; i++)
		{
			if (!(value.Entries[i].LoreCode == code))
			{
				continue;
			}
			JournalEntry journalEntry = value.Entries[i];
			for (int j = 0; j < journalEntry.Chapters.Count; j++)
			{
				if (journalEntry.Chapters[j].ChapterId == chapterId)
				{
					return true;
				}
			}
			break;
		}
		return false;
	}
}
