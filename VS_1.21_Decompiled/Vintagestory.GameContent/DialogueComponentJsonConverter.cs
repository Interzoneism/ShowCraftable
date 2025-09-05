using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class DialogueComponentJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(DialogueComponent);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		JsonObject jsonObject = new JsonObject(JToken.ReadFrom(reader));
		string code = jsonObject["code"].AsString();
		string jumpTo = jsonObject["jumpTo"].AsString();
		string owner = jsonObject["owner"].AsString();
		string sound = jsonObject["sound"].AsString();
		DialogeTextElement[] text = jsonObject["text"].AsArray<DialogeTextElement>();
		string text2 = jsonObject["type"].AsString("");
		Dictionary<string, string> setVariables = jsonObject["setVariables"].AsObject<Dictionary<string, string>>();
		string trigger = jsonObject["trigger"].AsString();
		bool flag = !jsonObject["isvalue"].Exists && jsonObject["isnotvalue"].Exists;
		switch (text2)
		{
		default:
			if (text2.Length == 0)
			{
				break;
			}
			goto case null;
		case null:
			switch (text2)
			{
			case "trigger":
			case "setvariables":
			case "jump":
				break;
			default:
				throw new JsonReaderException("Invalid dialog component type " + text2);
			}
			break;
		case "talk":
			return new DlgTalkComponent
			{
				Code = code,
				SetVariables = setVariables,
				Owner = owner,
				Text = text,
				Type = text2,
				Trigger = trigger,
				TriggerData = jsonObject["triggerdata"],
				JumpTo = jumpTo,
				Sound = sound
			};
		case "condition":
			return new DlgConditionComponent
			{
				Code = code,
				SetVariables = setVariables,
				Owner = owner,
				Type = text2,
				Variable = jsonObject["variable"].AsString(),
				IsValue = (flag ? jsonObject["isnotvalue"].AsString() : jsonObject["isvalue"].AsString()),
				InvertCondition = flag,
				ThenJumpTo = jsonObject["thenJumpTo"].AsString(),
				ElseJumpTo = jsonObject["elseJumpTo"].AsString(),
				Trigger = trigger,
				TriggerData = jsonObject["triggerdata"],
				Sound = sound
			};
		}
		return new DlgGenericComponent
		{
			Code = code,
			SetVariables = setVariables,
			Owner = owner,
			Type = text2,
			Trigger = trigger,
			TriggerData = jsonObject["triggerdata"],
			JumpTo = jumpTo,
			Sound = sound
		};
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
