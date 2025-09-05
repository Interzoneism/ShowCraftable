namespace Vintagestory.API.Common;

public struct SourceStringComponents
{
	private readonly string message;

	private readonly string domain;

	private readonly string path;

	private readonly int alternate;

	private readonly object[] formattedArguments;

	public SourceStringComponents(string message, string sourceDomain, string sourcePath, int sourceAlt)
	{
		formattedArguments = null;
		this.message = message;
		domain = sourceDomain;
		path = sourcePath;
		alternate = sourceAlt;
	}

	public SourceStringComponents(string message, AssetLocation source, int sourceAlt = -1)
	{
		formattedArguments = null;
		this.message = message;
		domain = source.Domain;
		path = source.Path;
		alternate = -1;
	}

	public SourceStringComponents(string formattedString, params object[] arguments)
	{
		domain = null;
		path = null;
		alternate = 0;
		message = formattedString;
		formattedArguments = arguments;
	}

	public override string ToString()
	{
		if (formattedArguments != null)
		{
			return string.Format(message, formattedArguments);
		}
		if (alternate >= 0)
		{
			return message + domain + ":" + path + " alternate:" + alternate;
		}
		return message + domain + ":" + path;
	}
}
