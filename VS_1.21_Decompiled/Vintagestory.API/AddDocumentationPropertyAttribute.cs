using System;

namespace Vintagestory.API;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AddDocumentationPropertyAttribute : Attribute
{
	public AddDocumentationPropertyAttribute(string name, string summary, string typeWithFullNamespace, string requiredStatus, string defaultStatus, bool attribute = false)
	{
	}
}
