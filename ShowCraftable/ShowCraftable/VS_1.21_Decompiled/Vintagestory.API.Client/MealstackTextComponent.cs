using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.API.Client;

public class MealstackTextComponent : ItemstackComponentBase
{
	public bool ShowTooltip = true;

	private DummySlot dummySlot;

	protected Action<CookingRecipe>? onMealClicked;

	protected float secondsVisible = 1f;

	protected int curItemIndex;

	public Vec3f renderOffset = new Vec3f();

	public float renderSize = 0.58f;

	private double unscaledSize;

	private CookingRecipe recipe;

	private ItemStack? ingredient;

	private ItemStack[] allstacks;

	private Dictionary<CookingRecipeIngredient, HashSet<ItemStack?>>? cachedValidStacks;

	private int slots;

	private bool isPie;

	public bool ShowStackSize { get; set; }

	public bool Background { get; set; }

	public bool RandomBowlBlock { get; set; } = true;

	public MealstackTextComponent(ICoreClientAPI capi, ref Dictionary<CookingRecipeIngredient, HashSet<ItemStack?>>? cachedValidStacks, ItemStack mealBlock, CookingRecipe recipe, double unscaledSize, EnumFloat floatType, ItemStack[] allstacks, Action<CookingRecipe>? onMealClicked = null, int slots = 4, bool isPie = false, ItemStack? ingredient = null)
		: base(capi)
	{
		dummySlot = new DummySlot(mealBlock);
		this.cachedValidStacks = cachedValidStacks;
		if (dummySlot.Itemstack?.Collectible is IBlockMealContainer blockMealContainer)
		{
			if (isPie)
			{
				dummySlot.Itemstack.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
			}
			blockMealContainer.SetContents(recipe.Code, dummySlot.Itemstack, isPie ? BlockPie.GenerateRandomPie(capi, ref cachedValidStacks, recipe, ingredient) : recipe.GenerateRandomMeal(capi, ref cachedValidStacks, allstacks, slots, ingredient));
		}
		this.ingredient = ingredient;
		this.allstacks = allstacks;
		this.slots = slots;
		this.isPie = isPie;
		this.recipe = recipe;
		this.unscaledSize = unscaledSize;
		Float = floatType;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, GuiElement.scaled(unscaledSize), GuiElement.scaled(unscaledSize))
		};
		this.onMealClicked = onMealClicked;
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath currentFlowPathSection = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool flag = offsetX + BoundsPerLine[0].Width > currentFlowPathSection.X2;
		BoundsPerLine[0].X = (flag ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (flag ? currentLineHeight : 0.0);
		BoundsPerLine[0].Width = GuiElement.scaled(unscaledSize) + GuiElement.scaled(PaddingRight);
		nextOffsetX = (flag ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!flag)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		if (Background)
		{
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
			ctx.Rectangle(BoundsPerLine[0].X, BoundsPerLine[0].Y, BoundsPerLine[0].Width, BoundsPerLine[0].Height);
			ctx.Fill();
		}
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		int num = (int)((double)api.Input.MouseX - renderX + (double)renderOffset.X);
		int num2 = (int)((double)api.Input.MouseY - renderY + (double)renderOffset.Y);
		LineRectangled lineRectangled = BoundsPerLine[0];
		bool flag = lineRectangled.PointInside(num, num2);
		if (!(dummySlot.Itemstack?.Collectible is IBlockMealContainer blockMealContainer))
		{
			return;
		}
		if (!flag && (secondsVisible -= deltaTime) <= 0f)
		{
			secondsVisible = 1f;
			if (RandomBowlBlock)
			{
				if (isPie)
				{
					dummySlot.Itemstack?.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
				}
				else
				{
					dummySlot.Itemstack = new ItemStack(BlockMeal.AllMealBowls(api)[capi.World.Rand.Next(BlockMeal.AllMealBowls(api).Length)]);
				}
			}
			blockMealContainer.SetContents(recipe.Code, dummySlot.Itemstack, isPie ? BlockPie.GenerateRandomPie(capi, ref cachedValidStacks, recipe, ingredient) : recipe.GenerateRandomMeal(capi, ref cachedValidStacks, allstacks, slots, ingredient));
		}
		ElementBounds elementBounds = ElementBounds.FixedSize((int)(lineRectangled.Width / (double)RuntimeEnv.GUIScale), (int)(lineRectangled.Height / (double)RuntimeEnv.GUIScale));
		elementBounds.ParentBounds = capi.Gui.WindowBounds;
		elementBounds.CalcWorldBounds();
		elementBounds.absFixedX = renderX + lineRectangled.X + (double)renderOffset.X;
		elementBounds.absFixedY = renderY + lineRectangled.Y + (double)renderOffset.Y;
		elementBounds.absInnerWidth *= renderSize / 0.58f;
		elementBounds.absInnerHeight *= renderSize / 0.58f;
		api.Render.PushScissor(elementBounds, stacking: true);
		api.Render.RenderItemstackToGui(dummySlot, renderX + lineRectangled.X + lineRectangled.Width * 0.5 + (double)renderOffset.X + offX, renderY + lineRectangled.Y + lineRectangled.Height * 0.5 + (double)renderOffset.Y + offY, 100f + renderOffset.Z, (float)GuiElement.scaled(unscaledSize) * renderSize, -1, shading: true, rotate: false, ShowStackSize);
		api.Render.PopScissor();
		if (flag && ShowTooltip)
		{
			RenderItemstackTooltip(dummySlot, renderX + (double)num, renderY + (double)num2, deltaTime);
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside(args.X, args.Y))
			{
				onMealClicked?.Invoke(recipe);
			}
		}
	}
}
