using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class EquipAction : EntityActionBase
{
	[JsonProperty]
	private string Target;

	[JsonProperty]
	private string Value;

	public override string Type => "equip";

	public EquipAction()
	{
	}

	public EquipAction(EntityActivitySystem vas, string target, string value)
	{
		base.vas = vas;
		Target = target;
		Value = value;
	}

	public override void Start(EntityActivity act)
	{
		string target = Target;
		if (target == "righthand" || target == "lefthand")
		{
			JsonItemStack jsonItemStack = JsonItemStack.FromString(Value);
			if (jsonItemStack.Resolve(vas.Entity.World, string.Concat(vas.Entity.Code, " entity activity system, equip action - could not resolve ", Value, ". Will ignore.")))
			{
				ItemSlot obj = ((Target == "righthand") ? vas.Entity.RightHandItemSlot : vas.Entity.LeftHandItemSlot);
				obj.Itemstack = jsonItemStack.ResolvedItemstack;
				obj.MarkDirty();
				vas.Entity.GetBehavior<EntityBehaviorContainer>().storeInv();
			}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] array = new string[2] { "lefthand", "righthand" };
		string[] array2 = new string[2] { "item", "block" };
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Target", CairoFont.WhiteDetailText(), elementBounds).AddDropDown(array, array, array.IndexOf(Target), null, elementBounds.BelowCopy(0.0, -5.0), "target").AddStaticText("Class", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 25.0))
			.AddDropDown(array2, array2, array.IndexOf(Target), null, elementBounds.BelowCopy(0.0, -5.0), "cclass")
			.AddStaticText("Block/Item code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code")
			.AddStaticText("Attributes", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "attr");
		if (Value != null && Value.Length > 0)
		{
			JsonItemStack jsonItemStack = JsonItemStack.FromString(Value);
			singleComposer.GetDropDown("cclass").SetSelectedIndex(array2.IndexOf<string>(jsonItemStack.Type.ToString().ToLowerInvariant()));
			singleComposer.GetTextInput("code").SetValue(jsonItemStack.Code.ToShortString());
			singleComposer.GetTextInput("attr").SetValue(jsonItemStack.Attributes?.ToString() ?? "");
		}
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string selectedValue = singleComposer.GetDropDown("cclass").SelectedValue;
		string text = singleComposer.GetTextInput("code").GetText();
		string text2 = singleComposer.GetTextInput("attr").GetText();
		if (text2.Length > 0)
		{
			Value = $"{{ type: \"{selectedValue}\", code: \"{text}\", attributes: {text2} }}";
		}
		else
		{
			Value = $"{{ type: \"{selectedValue}\", code: \"{text}\" }}";
		}
		Target = singleComposer.GetDropDown("target").SelectedValue;
		try
		{
			if (!JsonItemStack.FromString(Value).Resolve(capi.World, "Entity activity system, equip action - could not resolve " + Value + ". Will ignore."))
			{
				capi.TriggerIngameError(this, "cantresolve", "Can't save. Unable to resolve json stack " + Value + ".");
				return false;
			}
		}
		catch
		{
			capi.TriggerIngameError(this, "cantresolve", "Can't save. Not valid json stack " + Value + " - an exception was thrown.");
			return false;
		}
		return true;
	}

	public override IEntityAction Clone()
	{
		return new EquipAction(vas, Target, Value);
	}

	public override string ToString()
	{
		return "Grab " + Value + " in " + Target;
	}
}
