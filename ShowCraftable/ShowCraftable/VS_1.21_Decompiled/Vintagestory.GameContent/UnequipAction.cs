using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class UnequipAction : EntityActionBase
{
	[JsonProperty]
	private string Target;

	public override string Type => "unequip";

	public UnequipAction()
	{
	}

	public UnequipAction(EntityActivitySystem vas, string target)
	{
		base.vas = vas;
		Target = target;
	}

	public override void Start(EntityActivity act)
	{
		string target = Target;
		if (target == "righthand" || target == "lefthand")
		{
			ItemSlot obj = ((Target == "righthand") ? vas.Entity.RightHandItemSlot : vas.Entity.LeftHandItemSlot);
			obj.Itemstack = null;
			obj.MarkDirty();
			vas.Entity.GetBehavior<EntityBehaviorContainer>().storeInv();
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] array = new string[2] { "lefthand", "righthand" };
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Target", CairoFont.WhiteDetailText(), elementBounds).AddDropDown(array, array, array.IndexOf(Target), null, elementBounds.BelowCopy(0.0, -5.0), "target");
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Target = singleComposer.GetDropDown("target").SelectedValue;
		return true;
	}

	public override IEntityAction Clone()
	{
		return new UnequipAction(vas, Target);
	}

	public override string ToString()
	{
		return "Remove item from " + Target;
	}
}
