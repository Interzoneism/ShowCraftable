using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Vintagestory.Common;

public class ModAssemblyLoader : IDisposable
{
	private readonly IReadOnlyCollection<string> modSearchPaths;

	private readonly IReadOnlyCollection<ModContainer> mods;

	public ModAssemblyLoader(IReadOnlyCollection<string> modSearchPaths, IReadOnlyCollection<ModContainer> mods)
	{
		this.modSearchPaths = modSearchPaths;
		this.mods = mods;
		AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
	}

	public void Dispose()
	{
		AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHandler;
	}

	public Assembly LoadFrom(string path)
	{
		return Assembly.UnsafeLoadFrom(path);
	}

	public AssemblyDefinition LoadAssemblyDefinition(string path)
	{
		return AssemblyDefinition.ReadAssembly(path);
	}

	private IEnumerable<string> GetAssemblySearchPaths()
	{
		yield return AppDomain.CurrentDomain.BaseDirectory;
		yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
		foreach (string modSearchPath in modSearchPaths)
		{
			yield return modSearchPath;
		}
		IEnumerable<string> enumerable = from mod in mods
			select mod.FolderPath into path
			where path != null
			select path;
		foreach (string item in enumerable)
		{
			yield return item;
		}
	}

	private Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
	{
		return (from searchPath in GetAssemblySearchPaths()
			select Path.Combine(searchPath, args.Name + ".dll") into assemblyPath
			where File.Exists(assemblyPath)
			select LoadFrom(assemblyPath)).FirstOrDefault();
	}
}
