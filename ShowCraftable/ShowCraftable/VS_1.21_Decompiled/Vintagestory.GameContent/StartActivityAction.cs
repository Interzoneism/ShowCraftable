using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class StartActivityAction : EntityActionBase
{
	[JsonProperty]
	private string activityCode;

	[JsonProperty]
	private string target;

	[JsonProperty]
	private float priority = -1f;

	[JsonProperty]
	private int slot = -1;

	[JsonProperty]
	private string entitySelector;

	public override string Type => "startactivity";

	public StartActivityAction()
	{
	}

	public StartActivityAction(EntityActivitySystem vas, string activityCode, int slot, string target, float priority, string entitySelector)
	{
		base.vas = vas;
		this.activityCode = activityCode;
		this.slot = slot;
		this.target = target;
		this.priority = priority;
		this.entitySelector = entitySelector;
	}

	public override void Start(EntityActivity act)
	{
		string text = activityCode;
		if (activityCode.Contains(","))
		{
			string[] array = activityCode.Split(',');
			text = array[vas.Entity.World.Rand.Next(array.Length)].Trim();
		}
		if (target == "other")
		{
			EntitiesArgParser entitiesArgParser = new EntitiesArgParser("target", vas.Entity.Api, isMandatoryArg: true);
			TextCommandCallingArgs args = new TextCommandCallingArgs
			{
				Caller = new Caller
				{
					Entity = vas.Entity,
					Pos = vas.Entity.ServerPos.XYZ,
					Type = EnumCallerType.Entity
				},
				RawArgs = new CmdArgs(entitySelector)
			};
			if (entitiesArgParser.TryProcess(args) == EnumParseResult.Good)
			{
				Entity[] array2 = entitiesArgParser.GetValue() as Entity[];
				foreach (Entity entity in array2)
				{
					if (entity.EntityId != vas.Entity.EntityId)
					{
						entity.GetBehavior<EntityBehaviorActivityDriven>()?.ActivitySystem.StartActivity(text, priority, slot);
					}
				}
			}
			else
			{
				vas.Entity.World.Logger.Debug("Unable to parse entity selector '{0}' for the Entity Activity Action 'StartActivity' - {1}", entitySelector, entitiesArgParser.LastErrorMessage);
			}
		}
		else
		{
			if (vas.Debug)
			{
				vas.Entity.World.Logger.Debug("StartActivity, starting {0}", text);
			}
			vas.StartActivity(text, priority, slot);
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] array = new string[2] { "self", "other" };
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 350.0, 25.0);
		GuiComposer composer = singleComposer.AddStaticText("Activity code, or 'multiple,randomly,selected,codes'", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "codes").AddStaticText("Activity slot (-1 for default)", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedWidth(200.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0).WithFixedWidth(80.0), null, CairoFont.WhiteDetailText(), "slot")
			.AddStaticText("Priority (-1 to ignore priority)", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedWidth(200.0))
			.AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0).WithFixedWidth(80.0), null, CairoFont.WhiteDetailText(), "priority");
		CairoFont font = CairoFont.WhiteDetailText();
		ElementBounds elementBounds2 = elementBounds.BelowCopy(0.0, 5.0).WithFixedWidth(200.0);
		Vintagestory.API.Client.GuiComposerHelpers.AddDropDown(bounds: elementBounds = elementBounds2.BelowCopy(0.0, -5.0).WithFixedWidth(100.0), composer: composer.AddStaticText("Target entity", font, elementBounds2), values: array, names: new string[2] { "On self", "On another entity" }, selectedIndex: array.IndexOf(target), onSelectionChanged: delegate(string code, bool selected)
		{
			onSeleChanged(code, singleComposer);
		}, key: "target").AddStaticText("Target entity selector", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedWidth(200.0)).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0).WithFixedWidth(200.0), null, CairoFont.WhiteDetailText(), "selector");
		singleComposer.GetTextInput("codes").SetValue(activityCode);
		singleComposer.GetNumberInput("slot").SetValue(slot);
		singleComposer.GetNumberInput("priority").SetValue(priority);
		singleComposer.GetTextInput("selector").SetValue(entitySelector);
	}

	private void onSeleChanged(string code, GuiComposer singleComposer)
	{
		singleComposer.GetTextInput("selector").Enabled = code == "other";
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		activityCode = singleComposer.GetTextInput("codes").GetText();
		slot = (int)singleComposer.GetNumberInput("slot").GetValue();
		target = singleComposer.GetDropDown("target").SelectedValue;
		priority = singleComposer.GetNumberInput("priority").GetValue();
		entitySelector = singleComposer.GetTextInput("selector").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new StartActivityAction(vas, activityCode, slot, target, priority, entitySelector);
	}

	public override string ToString()
	{
		if (target == "other")
		{
			if (!activityCode.Contains(","))
			{
				return "Start activity " + activityCode + " on " + entitySelector;
			}
			return "Start random activity on " + entitySelector + " (" + activityCode + ")";
		}
		if (!activityCode.Contains(","))
		{
			return "Start activity " + activityCode;
		}
		return "Start random activity (" + activityCode + ")";
	}
}
