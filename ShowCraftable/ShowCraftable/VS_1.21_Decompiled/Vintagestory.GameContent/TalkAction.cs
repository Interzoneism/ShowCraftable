using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TalkAction : EntityActionBase
{
	[JsonProperty]
	private string talkType;

	public override string Type => "talk";

	public TalkAction()
	{
	}

	public TalkAction(EntityActivitySystem vas, string talkType)
	{
		base.vas = vas;
		this.talkType = talkType;
	}

	public override void Start(EntityActivity act)
	{
		int data = Enum.GetNames(typeof(EnumTalkType)).IndexOf<string>(talkType);
		(vas.Entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(vas.Entity.EntityId, 203, SerializerUtil.Serialize(data));
	}

	public override string ToString()
	{
		return "Talk utterance: " + talkType;
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		string[] names = Enum.GetNames(typeof(EnumTalkType));
		singleComposer.AddStaticText("Utterance", CairoFont.WhiteDetailText(), elementBounds).AddDropDown(names, names, names.IndexOf(talkType), null, elementBounds.BelowCopy(0.0, -5.0), "talkType");
	}

	public override IEntityAction Clone()
	{
		return new TalkAction(vas, talkType);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		talkType = singleComposer.GetDropDown("talkType").SelectedValue;
		return true;
	}
}
