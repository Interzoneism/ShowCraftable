using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class EntityActivityCollection
{
	[JsonProperty]
	public string Name;

	[JsonProperty]
	public List<EntityActivity> Activities = new List<EntityActivity>();

	private EntityActivitySystem vas;

	public EntityActivityCollection()
	{
	}

	public EntityActivityCollection(EntityActivitySystem vas)
	{
		this.vas = vas;
	}

	public EntityActivityCollection Clone()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		JsonSerializerSettings val = new JsonSerializerSettings
		{
			TypeNameHandling = (TypeNameHandling)3
		};
		return JsonUtil.ToObject<EntityActivityCollection>(JsonConvert.SerializeObject((object)this, (Formatting)1, val), "", val);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		foreach (EntityActivity activity in Activities)
		{
			activity.OnLoaded(vas);
		}
	}
}
