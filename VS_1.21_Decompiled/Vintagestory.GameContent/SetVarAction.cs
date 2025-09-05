using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class SetVarAction : EntityActionBase
{
	[JsonProperty]
	private EnumActivityVariableScope scope;

	[JsonProperty]
	private string op;

	[JsonProperty]
	private string name;

	[JsonProperty]
	private string value;

	public override string Type => "setvariable";

	public SetVarAction()
	{
	}

	public SetVarAction(EntityActivitySystem vas, EnumActivityVariableScope scope, string op, string name, string value)
	{
		base.vas = vas;
		this.op = op;
		this.scope = scope;
		this.name = name;
		this.value = value;
	}

	public override void Start(EntityActivity act)
	{
		VariablesModSystem modSystem = vas.Entity.Api.ModLoader.GetModSystem<VariablesModSystem>();
		switch (op)
		{
		case "set":
			modSystem.SetVariable(vas.Entity, scope, name, value);
			break;
		case "incrementby":
		case "decrementby":
		{
			string variable = modSystem.GetVariable(scope, name, vas.Entity);
			int num = ((!(op == "decrementby")) ? 1 : (-1));
			modSystem.SetVariable(vas.Entity, scope, name, (variable.ToDouble() + (double)num * value.ToDouble()).ToString() ?? "");
			break;
		}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] array = new string[3] { "entity", "group", "global" };
		string[] array2 = new string[3] { "set", "incrementby", "decrementby" };
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Variable Scope", CairoFont.WhiteDetailText(), elementBounds).AddDropDown(array, array, (int)scope, null, elementBounds = elementBounds.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "scope").AddStaticText("Operation", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 15.0))
			.AddDropDown(array2, array2, Math.Max(0, array2.IndexOf(op)), null, elementBounds = elementBounds.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "op")
			.AddStaticText("Name", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 15.0).WithFixedWidth(150.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "name")
			.AddStaticText("Value", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 15.0).WithFixedWidth(150.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "value");
		singleComposer.GetTextInput("name").SetValue(name);
		singleComposer.GetTextInput("value").SetValue(value);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		scope = (EnumActivityVariableScope)singleComposer.GetDropDown("scope").SelectedIndices[0];
		op = singleComposer.GetDropDown("op").SelectedValue;
		name = singleComposer.GetTextInput("name").GetText();
		value = singleComposer.GetTextInput("value").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new SetVarAction(vas, scope, op, name, value);
	}

	public override string ToString()
	{
		(vas?.Entity.Api.ModLoader.GetModSystem<VariablesModSystem>())?.GetVariable(scope, name, vas.Entity);
		if (op == "incrementby" || op == "decrementby")
		{
			return string.Format("{3} {0} variable {1} by {2}", scope, name, value, (op == "incrementby") ? "Increment" : "Decrement");
		}
		return $"Set {scope} variable {name} to {value}";
	}
}
