using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class SlideshowGridRecipeTextComponent : ItemstackComponentBase
{
	public GridRecipeAndUnnamedIngredients[] GridRecipesAndUnIn;

	private Action<ItemStack> onStackClicked;

	private ItemSlot dummyslot = new DummySlot();

	private double size;

	private float secondsVisible = 1f;

	private int curItemIndex;

	private Dictionary<AssetLocation, ItemStack[]> resolveCache = new Dictionary<AssetLocation, ItemStack[]>();

	private Dictionary<int, LoadedTexture> extraTexts = new Dictionary<int, LoadedTexture>();

	private LoadedTexture hoverTexture;

	public GridRecipeAndUnnamedIngredients CurrentVisibleRecipe;

	private static int[][,] variantDisplaySequence = new int[30][,];

	private int secondCounter;

	public SlideshowGridRecipeTextComponent(ICoreClientAPI capi, GridRecipe[] gridrecipes, double size, EnumFloat floatType, Action<ItemStack> onStackClicked = null, ItemStack[] allStacks = null)
		: base(capi)
	{
		SlideshowGridRecipeTextComponent slideshowGridRecipeTextComponent = this;
		size = GuiElement.scaled(size);
		this.onStackClicked = onStackClicked;
		Float = floatType;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, 3.0 * (size + 3.0), 3.0 * (size + 3.0))
		};
		this.size = size;
		Random rand = new Random(123);
		for (int i = 0; i < 30; i++)
		{
			int[,] array = (variantDisplaySequence[i] = new int[3, 3]);
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					array[j, k] = capi.World.Rand.Next(99999);
				}
			}
		}
		List<GridRecipeAndUnnamedIngredients> list = new List<GridRecipeAndUnnamedIngredients>();
		Queue<GridRecipe> queue = new Queue<GridRecipe>(gridrecipes);
		bool flag = false;
		while (!flag)
		{
			flag = true;
			int count = queue.Count;
			while (count-- > 0)
			{
				GridRecipe gridRecipe = queue.Dequeue();
				Dictionary<int, ItemStack[]> dictionary = null;
				bool flag2 = true;
				for (int l = 0; l < gridRecipe.resolvedIngredients.Length; l++)
				{
					CraftingRecipeIngredient craftingRecipeIngredient = gridRecipe.resolvedIngredients[l];
					if (craftingRecipeIngredient == null || !craftingRecipeIngredient.IsWildCard)
					{
						continue;
					}
					flag = false;
					flag2 = false;
					ItemStack[] array2 = ResolveWildCard(capi.World, craftingRecipeIngredient, allStacks);
					if (array2.Length == 0)
					{
						resolveCache.Remove(craftingRecipeIngredient.Code);
						array2 = ResolveWildCard(capi.World, craftingRecipeIngredient);
						if (array2.Length == 0)
						{
							throw new ArgumentException(string.Concat("Attempted to resolve the recipe ingredient wildcard ", craftingRecipeIngredient.Type.ToString(), " ", craftingRecipeIngredient.Code, " but there are no such items/blocks!"));
						}
					}
					if (craftingRecipeIngredient.Name == null)
					{
						if (dictionary == null)
						{
							dictionary = new Dictionary<int, ItemStack[]>();
						}
						dictionary[l] = ((ItemStack[])array2.Clone()).Shuffle(rand);
						flag2 = true;
						continue;
					}
					for (int m = 0; m < array2.Length; m++)
					{
						GridRecipe gridRecipe2 = gridRecipe.Clone();
						for (int n = 0; n < gridRecipe2.resolvedIngredients.Length; n++)
						{
							CraftingRecipeIngredient craftingRecipeIngredient2 = gridRecipe2.resolvedIngredients[n];
							if (craftingRecipeIngredient2 != null && craftingRecipeIngredient2.Code.Equals(craftingRecipeIngredient.Code))
							{
								craftingRecipeIngredient2.Code = array2[m].Collectible.Code;
								craftingRecipeIngredient2.IsWildCard = false;
								craftingRecipeIngredient2.ResolvedItemstack = array2[m];
							}
						}
						queue.Enqueue(gridRecipe2);
					}
					break;
				}
				if (flag2)
				{
					list.Add(new GridRecipeAndUnnamedIngredients
					{
						Recipe = gridRecipe,
						unnamedIngredients = dictionary
					});
				}
			}
		}
		resolveCache.Clear();
		GridRecipesAndUnIn = list.ToArray();
		GridRecipesAndUnIn.Shuffle(rand);
		bool flag3 = false;
		for (int num = 0; num < GridRecipesAndUnIn.Length; num++)
		{
			string requiresTrait = GridRecipesAndUnIn[num].Recipe.RequiresTrait;
			if (requiresTrait != null)
			{
				extraTexts[num] = capi.Gui.TextTexture.GenTextTexture(Lang.Get("gridrecipe-requirestrait", Lang.Get("traitname-" + requiresTrait)), CairoFont.WhiteDetailText());
				if (!flag3)
				{
					BoundsPerLine[0].Height += GuiElement.scaled(20.0);
				}
				flag3 = true;
			}
		}
		if (GridRecipesAndUnIn.Length == 0)
		{
			throw new ArgumentException("Could not resolve any of the supplied grid recipes?");
		}
		genHover();
		stackInfo.onRenderStack = delegate
		{
			GridRecipeAndUnnamedIngredients gridRecipeAndUnnamedIngredients = slideshowGridRecipeTextComponent.GridRecipesAndUnIn[slideshowGridRecipeTextComponent.curItemIndex];
			double num2 = (int)GuiElement.scaled(30.0 + GuiElementItemstackInfo.ItemStackSize / 2.0);
			ItemSlot curSlot = slideshowGridRecipeTextComponent.stackInfo.curSlot;
			int stackSize = curSlot.StackSize;
			curSlot.Itemstack.StackSize = 1;
			curSlot.Itemstack.Collectible.OnHandbookRecipeRender(capi, gridRecipeAndUnnamedIngredients.Recipe, curSlot, (double)(int)slideshowGridRecipeTextComponent.stackInfo.Bounds.renderX + num2, (double)(int)slideshowGridRecipeTextComponent.stackInfo.Bounds.renderY + num2 + (double)(int)GuiElement.scaled(GuiElementItemstackInfo.MarginTop), 1000.0 + GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 2.0, (float)GuiElement.scaled(GuiElementItemstackInfo.ItemStackSize) * 1f / 0.58f);
			curSlot.Itemstack.StackSize = stackSize;
		};
	}

	private void genHover()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
		val2.Paint();
		hoverTexture = new LoadedTexture(capi);
		api.Gui.LoadOrUpdateCairoTexture(val, linearMag: false, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private ItemStack[] ResolveWildCard(IWorldAccessor world, CraftingRecipeIngredient ingred, ItemStack[] allStacks = null)
	{
		if (resolveCache.ContainsKey(ingred.Code))
		{
			return resolveCache[ingred.Code];
		}
		List<ItemStack> list = new List<ItemStack>();
		if (allStacks != null)
		{
			foreach (ItemStack itemStack in allStacks)
			{
				if (itemStack.Class == ingred.Type && !(itemStack.Collectible.Code == null) && WildcardUtil.Match(ingred.Code, itemStack.Collectible.Code, ingred.AllowedVariants))
				{
					list.Add(new ItemStack(itemStack.Collectible, ingred.Quantity));
				}
			}
			resolveCache[ingred.Code] = list.ToArray();
			return list.ToArray();
		}
		foreach (CollectibleObject collectible in world.Collectibles)
		{
			if (WildcardUtil.Match(ingred.Code, collectible.Code, ingred.AllowedVariants))
			{
				list.Add(new ItemStack(collectible, ingred.Quantity));
			}
		}
		resolveCache[ingred.Code] = list.ToArray();
		return resolveCache[ingred.Code];
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath currentFlowPathSection = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool flag = offsetX + BoundsPerLine[0].Width > currentFlowPathSection.X2;
		BoundsPerLine[0].X = (flag ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (flag ? (currentLineHeight + GuiElement.scaled(UnscaledMarginTop)) : 0.0);
		BoundsPerLine[0].Width = 3.0 * (size + 3.0) + GuiElement.scaled(PaddingRight);
		nextOffsetX = (flag ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!flag)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				ctx.Rectangle(BoundsPerLine[0].X + (double)i * (size + GuiElement.scaled(3.0)), BoundsPerLine[0].Y + (double)j * (size + GuiElement.scaled(3.0)), size, size);
				ctx.Fill();
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		LineRectangled lineRectangled = BoundsPerLine[0];
		int num = (int)((double)api.Input.MouseX - renderX);
		int num2 = (int)((double)api.Input.MouseY - renderY);
		if (!lineRectangled.PointInside(num, num2) && (secondsVisible -= deltaTime) <= 0f)
		{
			secondsVisible = 1f;
			curItemIndex = (curItemIndex + 1) % GridRecipesAndUnIn.Length;
			secondCounter++;
		}
		GridRecipeAndUnnamedIngredients gridRecipeAndUnnamedIngredients = (CurrentVisibleRecipe = GridRecipesAndUnIn[curItemIndex]);
		if (extraTexts.TryGetValue(curItemIndex, out var value))
		{
			capi.Render.Render2DTexturePremultipliedAlpha(value.TextureId, (float)(renderX + lineRectangled.X), (float)(renderY + lineRectangled.Y + 3.0 * (size + 3.0)), value.Width, value.Height);
		}
		double num3 = 0.0;
		double num4 = 0.0;
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				int gridIndex = gridRecipeAndUnnamedIngredients.Recipe.GetGridIndex(j, i, gridRecipeAndUnnamedIngredients.Recipe.resolvedIngredients, gridRecipeAndUnnamedIngredients.Recipe.Width);
				num3 = renderX + lineRectangled.X + (double)i * (size + GuiElement.scaled(3.0));
				num4 = renderY + lineRectangled.Y + (double)j * (size + GuiElement.scaled(3.0));
				float gUIScale = RuntimeEnv.GUIScale;
				ElementBounds elementBounds = ElementBounds.Fixed(num3 / (double)gUIScale, num4 / (double)gUIScale, size / (double)gUIScale, size / (double)gUIScale).WithEmptyParent();
				elementBounds.CalcWorldBounds();
				CraftingRecipeIngredient elementInGrid = gridRecipeAndUnnamedIngredients.Recipe.GetElementInGrid(j, i, gridRecipeAndUnnamedIngredients.Recipe.resolvedIngredients, gridRecipeAndUnnamedIngredients.Recipe.Width);
				if (elementInGrid != null)
				{
					api.Render.PushScissor(elementBounds, stacking: true);
					Dictionary<int, ItemStack[]> unnamedIngredients = gridRecipeAndUnnamedIngredients.unnamedIngredients;
					if (unnamedIngredients != null && unnamedIngredients.TryGetValue(gridIndex, out var value2) && value2.Length != 0)
					{
						dummyslot.Itemstack = value2[variantDisplaySequence[secondCounter % 30][i, j] % value2.Length];
						dummyslot.Itemstack.StackSize = elementInGrid.Quantity;
					}
					else
					{
						dummyslot.Itemstack = elementInGrid.ResolvedItemstack.Clone();
					}
					dummyslot.BackgroundIcon = gridIndex.ToString() ?? "";
					dummyslot.Itemstack.Collectible.OnHandbookRecipeRender(capi, gridRecipeAndUnnamedIngredients.Recipe, dummyslot, num3 + size * 0.5, num4 + size * 0.5, 100.0, size);
					api.Render.PopScissor();
					double num5 = (double)mouseX - num3 + 1.0;
					double num6 = (double)mouseY - num4 + 2.0;
					if (num5 >= 0.0 && num5 < size && num6 >= 0.0 && num6 < size)
					{
						RenderItemstackTooltip(dummyslot, num3 + num5, num4 + num6, deltaTime);
					}
					dummyslot.BackgroundIcon = null;
				}
			}
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		GridRecipeAndUnnamedIngredients gridRecipeAndUnnamedIngredients = GridRecipesAndUnIn[curItemIndex];
		GridRecipe recipe = gridRecipeAndUnnamedIngredients.Recipe;
		LineRectangled[] boundsPerLine = BoundsPerLine;
		foreach (LineRectangled lineRectangled in boundsPerLine)
		{
			if (lineRectangled.PointInside(args.X, args.Y))
			{
				int num = (int)(((double)args.X - lineRectangled.X) / (size + 3.0));
				int num2 = (int)(((double)args.Y - lineRectangled.Y) / (size + 3.0));
				CraftingRecipeIngredient elementInGrid = recipe.GetElementInGrid(num2, num, recipe.resolvedIngredients, recipe.Width);
				if (elementInGrid == null)
				{
					break;
				}
				int gridIndex = recipe.GetGridIndex(num2, num, recipe.resolvedIngredients, recipe.Width);
				Dictionary<int, ItemStack[]> unnamedIngredients = gridRecipeAndUnnamedIngredients.unnamedIngredients;
				if (unnamedIngredients != null && unnamedIngredients.TryGetValue(gridIndex, out var value))
				{
					onStackClicked?.Invoke(value[variantDisplaySequence[secondCounter % 30][num, num2] % value.Length]);
				}
				else
				{
					onStackClicked?.Invoke(elementInGrid.ResolvedItemstack);
				}
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		foreach (KeyValuePair<int, LoadedTexture> extraText in extraTexts)
		{
			extraText.Value.Dispose();
		}
		hoverTexture?.Dispose();
	}
}
