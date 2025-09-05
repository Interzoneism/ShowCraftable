using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class ModCompilationContext
{
	private readonly string[] references;

	public ModCompilationContext()
	{
		references = new string[17]
		{
			"System.dll",
			"System.Core.dll",
			"System.Data.dll",
			"System.Runtime.dll",
			"System.Private.CoreLib.dll",
			"SkiaSharp.dll",
			"System.Xml.dll",
			"System.Xml.Linq.dll",
			"System.Net.Http.dll",
			"VintagestoryAPI.dll",
			"Newtonsoft.Json.dll",
			"protobuf-net.dll",
			"Tavis.JsonPatch.dll",
			"cairo-sharp.dll",
			Path.Combine("Mods", "VSCreativeMod.dll"),
			Path.Combine("Mods", "VSEssentials.dll"),
			Path.Combine("Mods", "VSSurvivalMod.dll")
		};
		string directoryName = Path.GetDirectoryName(typeof(object).Assembly.Location);
		if (directoryName == null)
		{
			throw new Exception("Could not find core/system assembly path for mod compilation.");
		}
		for (int i = 0; i < references.Length; i++)
		{
			if (File.Exists(Path.Combine(GamePaths.Binaries, references[i])))
			{
				references[i] = Path.Combine(GamePaths.Binaries, references[i]);
				continue;
			}
			if (File.Exists(Path.Combine(GamePaths.Binaries, "Lib", references[i])))
			{
				references[i] = Path.Combine(GamePaths.Binaries, "Lib", references[i]);
				continue;
			}
			if (File.Exists(Path.Combine(directoryName, references[i])))
			{
				references[i] = Path.Combine(directoryName, references[i]);
				continue;
			}
			throw new Exception("Referenced library not found: " + references[i]);
		}
	}

	public Assembly CompileFromFiles(ModContainer mod)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		List<PortableExecutableReference> list = (from sourceFile in mod.SourceFiles
			where sourceFile.EndsWithOrdinal(".dll")
			select MetadataReference.CreateFromFile(sourceFile, default(MetadataReferenceProperties), (DocumentationProvider)null)).ToList();
		list.AddRange(references.Select((string dlls) => MetadataReference.CreateFromFile(dlls, default(MetadataReferenceProperties), (DocumentationProvider)null)));
		IEnumerable<SyntaxTree> enumerable = mod.SourceFiles.Select((string file) => CSharpSyntaxTree.ParseText(File.ReadAllText(file), (CSharpParseOptions)null, "", (Encoding)null, default(CancellationToken)));
		CSharpCompilation val = CSharpCompilation.Create(mod.FileName + Guid.NewGuid(), enumerable, (IEnumerable<MetadataReference>)list, new CSharpCompilationOptions((OutputKind)2, false, (string)null, (string)null, (string)null, (IEnumerable<string>)null, (OptimizationLevel)0, false, false, (string)null, (string)null, default(ImmutableArray<byte>), (bool?)null, (Platform)0, (ReportDiagnostic)0, 4, (IEnumerable<KeyValuePair<string, ReportDiagnostic>>)null, true, false, (XmlReferenceResolver)null, (SourceReferenceResolver)null, (MetadataReferenceResolver)null, (AssemblyIdentityComparer)null, (StrongNameProvider)null, false, (MetadataImportOptions)0, (NullableContextOptions)0));
		using MemoryStream memoryStream = new MemoryStream();
		EmitResult val2 = ((Compilation)val).Emit((Stream)memoryStream, (Stream)null, (Stream)null, (Stream)null, (IEnumerable<ResourceDescription>)null, (EmitOptions)null, (IMethodSymbol)null, (Stream)null, (IEnumerable<EmbeddedText>)null, (Stream)null, default(CancellationToken));
		if (!val2.Success)
		{
			foreach (Diagnostic item in val2.Diagnostics.Where((Diagnostic d) => d.IsWarningAsError || (int)d.Severity == 3))
			{
				mod.Logger.Error("{0}: {1}", item.Id, item.GetMessage((IFormatProvider)null));
			}
			return null;
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		mod.Logger.Debug("Successfully compiled mod with Roslyn");
		return Assembly.Load(memoryStream.ToArray());
	}
}
