using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class DialogueConditionComponentJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(ConditionElement);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JsonObject jsonObject = new JsonObject(JToken.ReadFrom(reader));
		bool flag = !jsonObject["isvalue"].Exists && jsonObject["isnotvalue"].Exists;
		return new ConditionElement
		{
			Variable = jsonObject["variable"].AsString(),
			IsValue = (flag ? jsonObject["isnotvalue"].AsString() : jsonObject["isvalue"].AsString()),
			Invert = flag
		};
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
