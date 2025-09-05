using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemSurvivalHandbook : ModSystem
{
	private ICoreClientAPI capi;

	private GuiDialogHandbook dialog;

	protected ItemStack[] allstacks;

	public event InitCustomPagesDelegate OnInitCustomPages;

	internal void TriggerOnInitCustomPages(List<GuiHandbookPage> pages)
	{
		this.OnInitCustomPages?.Invoke(pages);
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Input.RegisterHotKeyFirst("selectionhandbook", Lang.Get("Show Handbook for current selection"), GlKeys.H, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: false, shiftPressed: true);
		api.Input.SetHotKeyHandler("selectionhandbook", OnSurvivalHandbookHotkey);
		api.Input.RegisterHotKeyFirst("handbook", Lang.Get("Show Survival handbook"), GlKeys.H, HotkeyType.HelpAndOverlays);
		api.Input.SetHotKeyHandler("handbook", OnSurvivalHandbookHotkey);
		api.Event.LevelFinalize += Event_LevelFinalize;
		api.RegisterLinkProtocol("handbook", onHandBookLinkClicked);
		api.RegisterLinkProtocol("handbooksearch", onHandBookSearchLinkClicked);
	}

	private void onHandBookSearchLinkClicked(LinkTextComponent comp)
	{
		string text = comp.Href.Substring("handbooksearch://".Length);
		if (!dialog.IsOpened())
		{
			dialog.TryOpen();
		}
		dialog.Search(text);
	}

	private void onHandBookLinkClicked(LinkTextComponent comp)
	{
		string text = comp.Href.Substring("handbook://".Length);
		text = text.Replace("\\", "");
		if (text.StartsWithOrdinal("tab-"))
		{
			if (!dialog.IsOpened())
			{
				dialog.TryOpen();
			}
			dialog.selectTab(text.Substring(4));
		}
		else
		{
			if (!dialog.IsOpened())
			{
				dialog.TryOpen();
			}
			dialog.OpenDetailPageFor(text);
		}
	}

	private void Event_LevelFinalize()
	{
		List<ItemStack> list = SetupBehaviorAndGetItemStacks();
		allstacks = list.ToArray();
		dialog = new GuiDialogSurvivalHandbook(capi, onCreatePagesAsync, onComposePage);
		capi.Logger.VerboseDebug("Done initialising handbook");
	}

	private List<GuiHandbookPage> onCreatePagesAsync()
	{
		List<GuiHandbookPage> list = new List<GuiHandbookPage>();
		foreach (CookingRecipe cookingRecipe in capi.GetCookingRecipes())
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			if (cookingRecipe.CooksInto == null)
			{
				GuiHandbookMealRecipePage item = new GuiHandbookMealRecipePage(capi, cookingRecipe, allstacks)
				{
					Visible = true
				};
				list.Add(item);
			}
		}
		foreach (CookingRecipe handbookRecipe in BlockPie.GetHandbookRecipes(capi, allstacks))
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			GuiHandbookMealRecipePage item2 = new GuiHandbookMealRecipePage(capi, handbookRecipe, allstacks, 6, isPie: true)
			{
				Visible = true
			};
			list.Add(item2);
		}
		ItemStack[] array = allstacks;
		foreach (ItemStack stack in array)
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			GuiHandbookItemStackPage item3 = new GuiHandbookItemStackPage(capi, stack)
			{
				Visible = true
			};
			list.Add(item3);
		}
		return list;
	}

	private void onComposePage(GuiHandbookPage page, GuiComposer detailViewGui, ElementBounds textBounds, ActionConsumable<string> openDetailPageFor)
	{
		page.ComposePage(detailViewGui, textBounds, allstacks, openDetailPageFor);
	}

	protected List<ItemStack> SetupBehaviorAndGetItemStacks()
	{
		List<ItemStack> list = new List<ItemStack>();
		foreach (CollectibleObject collectible in capi.World.Collectibles)
		{
			if (!collectible.HasBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>())
			{
				CollectibleBehaviorHandbookTextAndExtraInfo collectibleBehaviorHandbookTextAndExtraInfo = new CollectibleBehaviorHandbookTextAndExtraInfo(collectible);
				collectibleBehaviorHandbookTextAndExtraInfo.OnLoaded(capi);
				collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(collectibleBehaviorHandbookTextAndExtraInfo);
			}
			List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
			if (handBookStacks == null)
			{
				continue;
			}
			foreach (ItemStack item in handBookStacks)
			{
				list.Add(item);
			}
		}
		return list;
	}

	private bool OnSurvivalHandbookHotkey(KeyCombination key)
	{
		if (dialog.IsOpened())
		{
			dialog.TryClose();
		}
		else
		{
			dialog.TryOpen();
			dialog.ignoreNextKeyPress = true;
			ItemStack itemStack = capi.World.Player.InventoryManager.CurrentHoveredSlot?.Itemstack;
			if (key != null && key.Shift)
			{
				BlockPos blockPos = capi.World.Player.CurrentBlockSelection?.Position;
				if (blockPos != null)
				{
					itemStack = capi.World.BlockAccessor.GetBlock(blockPos).OnPickBlock(capi.World, blockPos);
				}
			}
			if (itemStack != null)
			{
				string pageCode = itemStack.Collectible.GetCollectibleInterface<IHandBookPageCodeProvider>()?.HandbookPageCodeForStack(capi.World, itemStack) ?? GuiHandbookItemStackPage.PageCodeForStack(itemStack);
				if (!dialog.OpenDetailPageFor(pageCode))
				{
					dialog.OpenDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(new ItemStack(itemStack.Collectible)));
				}
			}
		}
		return true;
	}

	public override void Dispose()
	{
		base.Dispose();
		dialog?.Dispose();
		capi?.Input.HotKeys.Remove("handbook");
		capi?.Input.HotKeys.Remove("selectionhandbook");
	}
}
