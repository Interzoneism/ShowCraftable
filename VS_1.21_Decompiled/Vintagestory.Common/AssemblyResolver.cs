using System;
using System.IO;
using System.Reflection;
using Vintagestory.API.Config;

namespace Vintagestory.Common;

public static class AssemblyResolver
{
	private static readonly string[] AssemblySearchPaths = new string[4]
	{
		GamePaths.Binaries,
		Path.Combine(GamePaths.Binaries, "Lib"),
		GamePaths.BinariesMods,
		GamePaths.DataPathMods
	};

	public static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
	{
		string path = new AssemblyName(args.Name).Name + ".dll";
		string text = null;
		try
		{
			string[] assemblySearchPaths = AssemblySearchPaths;
			for (int i = 0; i < assemblySearchPaths.Length; i++)
			{
				text = Path.Combine(assemblySearchPaths[i], path);
				if (File.Exists(text))
				{
					return Assembly.LoadFrom(text);
				}
			}
			return null;
		}
		catch (Exception innerException)
		{
			throw new Exception($"Failed to load assembly '{args.Name}' from '{text}'", innerException);
		}
	}
}
