using System;

namespace Vintagestory.API;

public class DocumentAsJsonAttribute : Attribute
{
	public DocumentAsJsonAttribute()
	{
	}

	public DocumentAsJsonAttribute(string requiredStatus, string defaultValue = "", bool isAttribute = false)
	{
	}
}
