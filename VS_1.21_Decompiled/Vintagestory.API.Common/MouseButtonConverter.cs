using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Vintagestory.API.Common;

public class MouseButtonConverter
{
	public static EnumMouseButton ToEnumMouseButton(MouseButton button)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected I4, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		return (int)button switch
		{
			0 => EnumMouseButton.Left, 
			1 => EnumMouseButton.Right, 
			2 => EnumMouseButton.Middle, 
			3 => EnumMouseButton.Button4, 
			4 => EnumMouseButton.Button5, 
			5 => EnumMouseButton.Button6, 
			6 => EnumMouseButton.Button7, 
			7 => EnumMouseButton.Button8, 
			_ => throw new ArgumentOutOfRangeException("button", button, null), 
		};
	}
}
