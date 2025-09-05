using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogSurvivalHandbook : GuiDialogHandbook
{
	public override string DialogTitle => Lang.Get("Survival Handbook");

	public GuiDialogSurvivalHandbook(ICoreClientAPI capi, OnCreatePagesDelegate createPageHandlerAsync, OnComposePageDelegate composePageHandler)
		: base(capi, createPageHandlerAsync, composePageHandler)
	{
		setupReloadCommand(capi);
		capi.Event.HotkeysChanged += loadEntries;
		currentCatgoryCode = capi.Settings.String["currentSurvivalHandbookCategoryCode"];
	}

	protected virtual void setupReloadCommand(ICoreClientAPI capi)
	{
		capi.ChatCommands.GetOrCreate("debug").BeginSub("reloadhandbook").WithDesc("Reload handbook pages")
			.HandleWith(delegate
			{
				capi.Assets.Reload(AssetCategory.config);
				Lang.Load(capi.World.Logger, capi.Assets, capi.Settings.String["language"]);
				loadEntries();
				return TextCommandResult.Success("Lang file and handbook entries now reloaded");
			})
			.EndSub();
	}

	protected override HashSet<string> initCustomPages()
	{
		List<GuiHandbookTextPage> list = (from pair in capi.Assets.GetMany<GuiHandbookTextPage>(capi.Logger, "config/handbook")
			orderby pair.Key.ToString()
			select pair.Value).ToList();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (GuiHandbookTextPage item in list)
		{
			item.Init(capi);
			allHandbookPages.Add(item);
		}
		capi.ModLoader.GetModSystem<ModSystemSurvivalHandbook>().TriggerOnInitCustomPages(allHandbookPages);
		for (int num = 0; num < allHandbookPages.Count; num++)
		{
			GuiHandbookPage guiHandbookPage = allHandbookPages[num];
			hashSet.Add(guiHandbookPage.CategoryCode);
			pageNumberByPageCode[guiHandbookPage.PageCode] = num;
		}
		return hashSet;
	}

	protected override GuiTab[] genTabs(out int curTab)
	{
		List<GuiTab> list = new List<GuiTab>();
		list.Add(new HandbookTab
		{
			DataInt = 0,
			Name = Lang.Get("handbook-category-tutorials"),
			CategoryCode = "tutorial"
		});
		list.Add(new HandbookTab
		{
			PaddingTop = 20.0,
			DataInt = 0,
			Name = Lang.Get("handbook-category-everything"),
			CategoryCode = null
		});
		curTab = ((!(currentCatgoryCode == "tutorial")) ? 1 : 0);
		categoryCodes.Remove("tutorial");
		for (int i = 0; i < categoryCodes.Count; i++)
		{
			int count = list.Count;
			list.Add(new HandbookTab
			{
				DataInt = count,
				Name = Lang.Get("handbook-category-" + categoryCodes[i]),
				CategoryCode = categoryCodes[i]
			});
			if (currentCatgoryCode == categoryCodes[i])
			{
				curTab = count;
			}
		}
		return list.ToArray();
	}
}
