using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class GuiHandbookGroupedItemstackPage : GuiHandbookItemStackPage
{
	public List<ItemStack> Stacks = new List<ItemStack>();

	public string Name;

	public override string PageCode => Name;

	public GuiHandbookGroupedItemstackPage(ICoreClientAPI capi, ItemStack stack)
		: base(capi, null)
	{
	}

	public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
	{
		float num = (float)GuiElement.scaled(25.0);
		float num2 = (float)GuiElement.scaled(10.0);
		int index = (int)(capi.ElapsedMilliseconds / 1000 % Stacks.Count);
		dummySlot.Itemstack = Stacks[index];
		capi.Render.RenderItemstackToGui(dummySlot, x + (double)num2 + (double)(num / 2f), y + (double)(num / 2f), 100.0, num, -1, shading: true, rotate: false, showStackSize: false);
		if (Texture == null)
		{
			Texture = new TextTextureUtil(capi).GenTextTexture(Name, CairoFont.WhiteSmallText());
		}
		capi.Render.Render2DTexturePremultipliedAlpha(Texture.TextureId, x + (double)num + GuiElement.scaled(25.0), y + (double)(num / 4f) - GuiElement.scaled(3.0), Texture.Width, Texture.Height);
	}

	protected override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		dummySlot.Itemstack = Stacks[0];
		return Stacks[0].Collectible.GetBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>().GetHandbookInfo(dummySlot, capi, allStacks, openDetailPageFor);
	}
}
