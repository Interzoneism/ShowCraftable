using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CharacterSystem : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private GuiDialogCreateCharacter createCharDlg;

	private GuiDialogCharacterBase charDlg;

	private bool didSelect;

	public List<CharacterClass> characterClasses = new List<CharacterClass>();

	public List<Trait> traits = new List<Trait>();

	public Dictionary<string, CharacterClass> characterClassesByCode = new Dictionary<string, CharacterClass>();

	public Dictionary<string, Trait> TraitsByCode = new Dictionary<string, Trait>();

	private SeraphRandomizerConstraints randomizerConstraints;

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Network.RegisterChannel("charselection").RegisterMessageType<CharacterSelectionPacket>().RegisterMessageType<CharacterSelectedState>();
		api.Event.MatchesGridRecipe += Event_MatchesGridRecipe;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.BlockTexturesLoaded += onLoadedUniversal;
		api.Network.GetChannel("charselection").SetMessageHandler<CharacterSelectedState>(onSelectedState);
		api.Event.IsPlayerReady += Event_IsPlayerReady;
		api.Event.PlayerJoin += Event_PlayerJoin;
		this.api.ChatCommands.Create("charsel").WithDescription("Open the character selection menu").HandleWith(onCharSelCmd);
		api.Event.BlockTexturesLoaded += loadCharacterClasses;
		charDlg = api.Gui.LoadedGuis.Find((GuiDialog dlg) => dlg is GuiDialogCharacterBase) as GuiDialogCharacterBase;
		charDlg.Tabs.Add(new GuiTab
		{
			Name = Lang.Get("charactertab-traits"),
			DataInt = 1
		});
		charDlg.RenderTabHandlers.Add(composeTraitsTab);
	}

	private void onLoadedUniversal()
	{
		randomizerConstraints = api.Assets.Get("config/seraphrandomizer.json").ToObject<SeraphRandomizerConstraints>();
	}

	private void composeTraitsTab(GuiComposer compo)
	{
		compo.AddRichtext(getClassTraitText(), CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15), ElementBounds.Fixed(0.0, 25.0, 385.0, 200.0));
	}

	private string getClassTraitText()
	{
		string charClass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		CharacterClass characterClass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == charClass);
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (Trait item in from code in characterClass.Traits
			select TraitsByCode[code] into trait
			orderby (int)trait.Type
			select trait)
		{
			stringBuilder2.Clear();
			foreach (KeyValuePair<string, double> attribute in item.Attributes)
			{
				if (stringBuilder2.Length > 0)
				{
					stringBuilder2.Append(", ");
				}
				stringBuilder2.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", attribute.Key, attribute.Value)));
			}
			if (stringBuilder2.Length > 0)
			{
				stringBuilder.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + item.Code), stringBuilder2));
				continue;
			}
			string ifExists = Lang.GetIfExists("traitdesc-" + item.Code);
			if (ifExists != null)
			{
				stringBuilder.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + item.Code), ifExists));
			}
			else
			{
				stringBuilder.AppendLine(Lang.Get("trait-" + item.Code));
			}
		}
		if (characterClass.Traits.Length == 0)
		{
			stringBuilder.AppendLine(Lang.Get("No positive or negative traits"));
		}
		return stringBuilder.ToString();
	}

	private void loadCharacterClasses()
	{
		onLoadedUniversal();
		LoadTraits();
		LoadClasses();
		foreach (Trait trait in traits)
		{
			TraitsByCode[trait.Code] = trait;
		}
		foreach (CharacterClass characterClass in characterClasses)
		{
			characterClassesByCode[characterClass.Code] = characterClass;
			JsonItemStack[] gear = characterClass.Gear;
			foreach (JsonItemStack jsonItemStack in gear)
			{
				if (!jsonItemStack.Resolve(api.World, "character class gear", printWarningOnError: false))
				{
					api.World.Logger.Warning(string.Concat("Unable to resolve character class gear ", jsonItemStack.Type.ToString(), " with code ", jsonItemStack.Code, " item/block does not seem to exist. Will ignore."));
				}
			}
		}
	}

	private void LoadTraits()
	{
		traits = new List<Trait>();
		Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Logger, "config/traits");
		int num = 0;
		string[] array = new string[26]
		{
			"focused", "resourceful", "fleetfooted", "bowyer", "forager", "pilferer", "furtive", "precise", "technical", "soldier",
			"hardy", "clothier", "mender", "merciless", "farsighted", "claustrophobic", "frail", "nervous", "ravenous", "nearsighted",
			"heavyhanded", "kind", "weak", "civil", "improviser", "tinkerer"
		};
		HashSet<string> hashSet = new HashSet<string>();
		string[] array2 = array;
		foreach (string item in array2)
		{
			hashSet.Add(item);
		}
		HashSet<string> hashSet2 = hashSet;
		foreach (var (assetLocation2, val2) in many)
		{
			if (val2 is JObject)
			{
				Trait trait = val2.ToObject<Trait>(assetLocation2.Domain);
				if (traits.Find((Trait element) => element.Code == trait.Code) != null)
				{
					api.World.Logger.Warning($"Trying to add character trait from domain '{assetLocation2.Domain}', but character trait with code '{trait.Code}' already exists. Will add it anyway, but it can cause undefined behavior.");
				}
				traits.Add(trait);
				num++;
			}
			JArray val3 = (JArray)(object)((val2 is JArray) ? val2 : null);
			if (val3 == null)
			{
				continue;
			}
			int num2 = 0;
			foreach (JToken item2 in val3)
			{
				Trait trait2 = item2.ToObject<Trait>(assetLocation2.Domain);
				if (traits.Find((Trait element) => element.Code == trait2.Code) != null)
				{
					api.World.Logger.Warning($"Trying to add character trait from domain '{assetLocation2.Domain}', but character trait with code '{trait2.Code}' already exists. Will add it anyway, but it can cause undefined behavior.");
				}
				if (assetLocation2.Domain == "game")
				{
					hashSet2.Remove(trait2.Code);
					if (!array.Contains(trait2.Code))
					{
						api.World.Logger.Warning("Instead of json patching in new traits into vanilla asset, add 'traits.json' into 'config' folder in your mod domain with new traits.");
					}
					else if (array.IndexOf(trait2.Code) != num2)
					{
						api.World.Logger.Warning("Order of vanilla character traits has changed. Dont remove vanilla character traits or add new traits between or before vanilla traits. That will cause incompatibility with other mods that change traits, that can result in crashes.");
					}
				}
				traits.Add(trait2);
				num++;
				num2++;
			}
		}
		if (hashSet2.Count > 0)
		{
			api.World.Logger.Warning("Failed to find vanilla traits: " + hashSet2.Aggregate((string a, string b) => a + ", " + b) + ", dont remove vanilla traits, it will cause incompatibility with other mods that change traits or classes, that can result in crashes.");
		}
		api.World.Logger.Event($"{num} traits loaded from {many.Count} files");
	}

	private void LoadClasses()
	{
		characterClasses = new List<CharacterClass>();
		Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Logger, "config/characterclasses");
		int num = 0;
		string[] array = new string[6] { "commoner", "hunter", "malefactor", "clockmaker", "blackguard", "tailor" };
		HashSet<string> hashSet = new HashSet<string>();
		string[] array2 = array;
		foreach (string item in array2)
		{
			hashSet.Add(item);
		}
		HashSet<string> hashSet2 = hashSet;
		foreach (var (assetLocation2, val2) in many)
		{
			if (val2 is JObject)
			{
				CharacterClass characterClass = val2.ToObject<CharacterClass>(assetLocation2.Domain);
				if (!characterClass.Enabled)
				{
					continue;
				}
				if (characterClasses.Find((CharacterClass element) => element.Code == characterClass.Code) != null)
				{
					api.World.Logger.Warning($"Trying to add character class from domain '{assetLocation2.Domain}', but character class with code '{characterClass.Code}' already exists. Will add it anyway, but it can cause undefined behavior.");
				}
				characterClasses.Add(characterClass);
				num++;
			}
			JArray val3 = (JArray)(object)((val2 is JArray) ? val2 : null);
			if (val3 == null)
			{
				continue;
			}
			int num2 = 0;
			foreach (JToken item2 in val3)
			{
				CharacterClass characterClass2 = item2.ToObject<CharacterClass>(assetLocation2.Domain);
				if (!characterClass2.Enabled)
				{
					continue;
				}
				if (characterClasses.Find((CharacterClass element) => element.Code == characterClass2.Code) != null)
				{
					api.World.Logger.Warning($"Trying to add character class from domain '{assetLocation2.Domain}', but character class with code '{characterClass2.Code}' already exists. Will add it anyway, but it can cause undefined behavior.");
				}
				if (assetLocation2.Domain == "game")
				{
					hashSet2.Remove(characterClass2.Code);
					if (!array.Contains(characterClass2.Code))
					{
						api.World.Logger.Warning("Instead of json patching in new classes into vanilla asset, add 'characterclasses.json' into 'config' folder in your mod domain with new classes.");
					}
					else if (array.IndexOf(characterClass2.Code) != num2)
					{
						api.World.Logger.Warning("Order of vanilla character classes has changed. Dont remove vanilla character classes (set 'enabled' attribute to 'false' instead) or add new classes between or before vanilla classes. That will cause incompatibility with other mods that change classes, that can result in crashes.");
					}
				}
				characterClasses.Add(characterClass2);
				num++;
				num2++;
			}
		}
		if (hashSet2.Count > 0)
		{
			api.World.Logger.Warning("Failed to find vanilla classes: " + hashSet2.Aggregate((string a, string b) => a + ", " + b) + ", dont remove vanilla classes (set 'enabled' attribute to 'false' instead), it will cause incompatibility with other mods that change classes, that can result in crashes.");
		}
		api.World.Logger.Event($"{num} classes loaded from {many.Count} files");
	}

	public void setCharacterClass(EntityPlayer eplayer, string classCode, bool initializeGear = true)
	{
		CharacterClass characterClass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == classCode);
		if (characterClass == null)
		{
			throw new ArgumentException("Not a valid character class code!");
		}
		eplayer.WatchedAttributes.SetString("characterClass", characterClass.Code);
		if (initializeGear)
		{
			EntityBehaviorPlayerInventory behavior = eplayer.GetBehavior<EntityBehaviorPlayerInventory>();
			EntityShapeRenderer entityShapeRenderer = capi?.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
			behavior.doReloadShapeAndSkin = false;
			IInventory inventory = behavior.Inventory;
			if (inventory != null)
			{
				for (int num = 0; num < inventory.Count && num < 12; num++)
				{
					inventory[num].Itemstack = null;
				}
				JsonItemStack[] gear = characterClass.Gear;
				foreach (JsonItemStack jsonItemStack in gear)
				{
					if (!jsonItemStack.Resolve(api.World, "character class gear", printWarningOnError: false))
					{
						api.World.Logger.Warning(string.Concat("Unable to resolve character class gear ", jsonItemStack.Type.ToString(), " with code ", jsonItemStack.Code, " item/block does not seem to exist. Will ignore."));
						continue;
					}
					ItemStack itemStack = jsonItemStack.ResolvedItemstack?.Clone();
					if (itemStack != null)
					{
						if (!Enum.TryParse<EnumCharacterDressType>(itemStack.ItemAttributes["clothescategory"].AsString(), ignoreCase: true, out var result))
						{
							eplayer.TryGiveItemStack(itemStack);
							continue;
						}
						inventory[(int)result].Itemstack = itemStack;
						inventory[(int)result].MarkDirty();
					}
				}
				if (entityShapeRenderer != null)
				{
					behavior.doReloadShapeAndSkin = true;
					entityShapeRenderer.TesselateShape();
				}
			}
		}
		applyTraitAttributes(eplayer);
	}

	private void applyTraitAttributes(EntityPlayer eplr)
	{
		string classcode = eplr.WatchedAttributes.GetString("characterClass");
		CharacterClass characterClass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == classcode);
		if (characterClass == null)
		{
			throw new ArgumentException("Not a valid character class code!");
		}
		foreach (KeyValuePair<string, EntityFloatStats> stat in eplr.Stats)
		{
			foreach (KeyValuePair<string, EntityStat<float>> item in stat.Value.ValuesByKey)
			{
				if (item.Key == "trait")
				{
					stat.Value.Remove(item.Key);
					break;
				}
			}
		}
		string[] stringArray = eplr.WatchedAttributes.GetStringArray("extraTraits");
		IEnumerable<string> enumerable;
		if (stringArray != null)
		{
			enumerable = characterClass.Traits.Concat(stringArray);
		}
		else
		{
			IEnumerable<string> enumerable2 = characterClass.Traits;
			enumerable = enumerable2;
		}
		foreach (string item2 in enumerable)
		{
			if (!TraitsByCode.TryGetValue(item2, out var value))
			{
				continue;
			}
			foreach (KeyValuePair<string, double> attribute in value.Attributes)
			{
				string key = attribute.Key;
				double value2 = attribute.Value;
				eplr.Stats.Set(key, "trait", (float)value2, persistent: true);
			}
		}
		eplr.GetBehavior<EntityBehaviorHealth>()?.MarkDirty();
	}

	private TextCommandResult onCharSelCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		bool flag = capi.World.Player.Entity.WatchedAttributes.GetBool("allowcharselonce") || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		if (createCharDlg == null && flag)
		{
			createCharDlg = new GuiDialogCreateCharacter(capi, this);
			createCharDlg.PrepAndOpen();
		}
		else if (createCharDlg == null)
		{
			return TextCommandResult.Success(Lang.Get("You don't have permission to change you character and class. An admin needs to grant you allowcharselonce permission"));
		}
		if (!createCharDlg.IsOpened())
		{
			createCharDlg.TryOpen();
		}
		return TextCommandResult.Success();
	}

	private void onSelectedState(CharacterSelectedState p)
	{
		didSelect = p.DidSelect;
	}

	private void Event_PlayerJoin(IClientPlayer byPlayer)
	{
		if (!(byPlayer.PlayerUID == capi.World.Player.PlayerUID))
		{
			return;
		}
		if (!didSelect)
		{
			createCharDlg = new GuiDialogCreateCharacter(capi, this);
			createCharDlg.PrepAndOpen();
			createCharDlg.OnClosed += delegate
			{
				capi.PauseGame(paused: false);
			};
			capi.Event.EnqueueMainThreadTask(delegate
			{
				capi.PauseGame(paused: true);
			}, "pausegame");
			capi.Event.PushEvent("begincharacterselection");
		}
		else
		{
			capi.Event.PushEvent("skipcharacterselection");
		}
	}

	private bool Event_IsPlayerReady(ref EnumHandling handling)
	{
		if (didSelect)
		{
			return true;
		}
		handling = EnumHandling.PreventDefault;
		return false;
	}

	private bool Event_MatchesGridRecipe(IPlayer player, GridRecipe recipe, ItemSlot[] ingredients, int gridWidth)
	{
		if (recipe.RequiresTrait == null)
		{
			return true;
		}
		string text = player.Entity.WatchedAttributes.GetString("characterClass");
		if (text == null)
		{
			return true;
		}
		if (characterClassesByCode.TryGetValue(text, out var value))
		{
			if (value.Traits.Contains(recipe.RequiresTrait))
			{
				return true;
			}
			string[] stringArray = player.Entity.WatchedAttributes.GetStringArray("extraTraits");
			if (stringArray != null && stringArray.Contains(recipe.RequiresTrait))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTrait(IPlayer player, string trait)
	{
		string text = player.Entity.WatchedAttributes.GetString("characterClass");
		if (text == null)
		{
			return true;
		}
		if (characterClassesByCode.TryGetValue(text, out var value))
		{
			if (value.Traits.Contains(trait))
			{
				return true;
			}
			string[] stringArray = player.Entity.WatchedAttributes.GetStringArray("extraTraits");
			if (stringArray != null && stringArray.Contains(trait))
			{
				return true;
			}
		}
		return false;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Network.GetChannel("charselection").SetMessageHandler<CharacterSelectionPacket>(onCharacterSelection);
		api.Event.PlayerJoin += Event_PlayerJoinServer;
		api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, loadCharacterClasses);
	}

	private void Event_PlayerJoinServer(IServerPlayer byPlayer)
	{
		didSelect = SerializerUtil.Deserialize(byPlayer.GetModdata("createCharacter"), defaultValue: false);
		if (!didSelect)
		{
			setCharacterClass(byPlayer.Entity, characterClasses[0].Code, initializeGear: false);
		}
		double num = sapi.World.Config.GetDecimal("allowClassChangeAfterMonths", -1.0);
		if (sapi.World.Config.GetBool("allowOneFreeClassChange") && byPlayer.ServerData.LastCharacterSelectionDate == null)
		{
			byPlayer.Entity.WatchedAttributes.SetBool("allowcharselonce", value: true);
		}
		else if (num >= 0.0)
		{
			DateTime utcNow = DateTime.UtcNow;
			string input = byPlayer.ServerData.LastCharacterSelectionDate ?? byPlayer.ServerData.FirstJoinDate ?? "1/1/1970 00:00 AM";
			double num2 = utcNow.Subtract(DateTimeOffset.Parse(input).UtcDateTime).TotalDays / 30.0;
			if (num < num2)
			{
				byPlayer.Entity.WatchedAttributes.SetBool("allowcharselonce", value: true);
			}
		}
		sapi.Network.GetChannel("charselection").SendPacket(new CharacterSelectedState
		{
			DidSelect = didSelect
		}, byPlayer);
	}

	public bool randomizeSkin(Entity entity, Dictionary<string, string> preSelection, bool playVoice = true)
	{
		if (preSelection == null)
		{
			preSelection = new Dictionary<string, string>();
		}
		EntityBehaviorExtraSkinnable behavior = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		bool flag = api.World.Rand.NextDouble() < 0.3;
		Dictionary<string, RandomizerConstraint> dictionary = new Dictionary<string, RandomizerConstraint>();
		SkinnablePart[] availableSkinParts = behavior.AvailableSkinParts;
		foreach (SkinnablePart skinnablePart in availableSkinParts)
		{
			SkinnablePartVariant[] array = skinnablePart.Variants.Where((SkinnablePartVariant v) => v.Category == "standard").ToArray();
			int num = api.World.Rand.Next(array.Length);
			if (preSelection.TryGetValue(skinnablePart.Code, out var variantCode))
			{
				num = array.IndexOf((SkinnablePartVariant val) => val.Code == variantCode);
			}
			else
			{
				if (dictionary.TryGetValue(skinnablePart.Code, out var value))
				{
					variantCode = value.SelectRandom(api.World.Rand, array);
					num = array.IndexOf((SkinnablePartVariant val) => val.Code == variantCode);
				}
				if ((skinnablePart.Code == "mustache" || skinnablePart.Code == "beard") && !flag)
				{
					num = 0;
					variantCode = "none";
				}
			}
			if (variantCode == null)
			{
				variantCode = array[num].Code;
			}
			behavior.selectSkinPart(skinnablePart.Code, variantCode, retesselateShape: true, playVoice);
			if (randomizerConstraints.Constraints.TryGetValue(skinnablePart.Code, out var value2) && value2.TryGetValue(variantCode, out var value3))
			{
				foreach (KeyValuePair<string, RandomizerConstraint> item in value3)
				{
					dictionary[item.Key] = item.Value;
				}
			}
			if (skinnablePart.Code == "voicetype" && variantCode == "high")
			{
				flag = false;
			}
		}
		return true;
	}

	private void onCharacterSelection(IServerPlayer fromPlayer, CharacterSelectionPacket p)
	{
		bool modData = fromPlayer.GetModData("createCharacter", defaultValue: false);
		if (modData && !fromPlayer.Entity.WatchedAttributes.GetBool("allowcharselonce") && fromPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
			fromPlayer.BroadcastPlayerData(sendInventory: true);
			return;
		}
		if (p.DidSelect)
		{
			fromPlayer.SetModData("createCharacter", data: true);
			setCharacterClass(fromPlayer.Entity, p.CharacterClass, !modData || fromPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative);
			EntityBehaviorExtraSkinnable behavior = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
			behavior.ApplyVoice(p.VoiceType, p.VoicePitch, testTalk: false);
			foreach (KeyValuePair<string, string> skinPart in p.SkinParts)
			{
				behavior.selectSkinPart(skinPart.Key, skinPart.Value, retesselateShape: false);
			}
			DateTime utcNow = DateTime.UtcNow;
			fromPlayer.ServerData.LastCharacterSelectionDate = utcNow.ToShortDateString() + " " + utcNow.ToShortTimeString();
			bool flag = sapi.World.Config.GetBool("allowOneFreeClassChange");
			if (!modData && flag)
			{
				fromPlayer.ServerData.LastCharacterSelectionDate = null;
			}
			else
			{
				fromPlayer.Entity.WatchedAttributes.RemoveAttribute("allowcharselonce");
			}
		}
		fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
		fromPlayer.BroadcastPlayerData(sendInventory: true);
	}

	internal void ClientSelectionDone(IInventory characterInv, string characterClass, bool didSelect)
	{
		List<ClothStack> list = new List<ClothStack>();
		for (int i = 0; i < characterInv.Count; i++)
		{
			ItemSlot itemSlot = characterInv[i];
			if (itemSlot.Itemstack != null)
			{
				list.Add(new ClothStack
				{
					Code = itemSlot.Itemstack.Collectible.Code.ToShortString(),
					SlotNum = i,
					Class = itemSlot.Itemstack.Class
				});
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		EntityBehaviorExtraSkinnable behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (AppliedSkinnablePartVariant appliedSkinPart in behavior.AppliedSkinParts)
		{
			dictionary[appliedSkinPart.PartCode] = appliedSkinPart.Code;
		}
		if (didSelect)
		{
			storePreviousSelection(dictionary);
		}
		capi.Network.GetChannel("charselection").SendPacket(new CharacterSelectionPacket
		{
			Clothes = list.ToArray(),
			DidSelect = didSelect,
			SkinParts = dictionary,
			CharacterClass = characterClass,
			VoicePitch = behavior.VoicePitch,
			VoiceType = behavior.VoiceType
		});
		capi.Network.SendPlayerNowReady();
		createCharDlg = null;
		capi.Event.PushEvent("finishcharacterselection");
	}

	public Dictionary<string, string> getPreviousSelection()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (capi == null || !capi.Settings.String.Exists("lastSkinSelection"))
		{
			return dictionary;
		}
		string[] array = capi.Settings.String["lastSkinSelection"].Split(",");
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(":");
			dictionary[array2[0]] = array2[1];
		}
		return dictionary;
	}

	public void storePreviousSelection(Dictionary<string, string> selection)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, string> item in selection)
		{
			list.Add(item.Key + ":" + item.Value);
		}
		capi.Settings.String["lastSkinSelection"] = string.Join(",", list);
	}
}
