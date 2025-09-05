using System;

namespace Vintagestory.Client;

public class RestartGameException : Exception
{
	public RestartGameException(string message)
		: base(message)
	{
	}
}
