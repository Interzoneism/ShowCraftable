using System;

namespace Vintagestory.API.Util;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class CustomFastSerializerAttribute : Attribute
{
}
