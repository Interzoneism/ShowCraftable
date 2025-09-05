using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class HeldCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private AssetLocation Code;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "held";

	public HeldCondition()
	{
	}

	public HeldCondition(EntityActivitySystem vas, string Code, bool invert = false)
	{
		this.vas = vas;
		this.Code = Code;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		EntityAgent entityAgent = e as EntityAgent;
		ItemStack itemstack = entityAgent.LeftHandItemSlot.Itemstack;
		if (itemstack != null && WildcardUtil.Match(Code, itemstack.Collectible.Code))
		{
			return true;
		}
		ItemStack itemstack2 = entityAgent.RightHandItemSlot.Itemstack;
		if (itemstack2 != null && WildcardUtil.Match(Code, itemstack2.Collectible.Code))
		{
			return true;
		}
		return false;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0);
		singleComposer.AddStaticText("Block or Item Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 15.0)).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code");
		singleComposer.GetTextInput("code").SetValue(Code?.ToShortString());
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Code = new AssetLocation(singleComposer.GetTextInput("code").GetText());
	}

	public IActionCondition Clone()
	{
		return new HeldCondition(vas, Code, Invert);
	}

	public override string ToString()
	{
		return string.Format(Invert ? "When not holding {0} in hands" : "When holding {0} in hands", Code);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
