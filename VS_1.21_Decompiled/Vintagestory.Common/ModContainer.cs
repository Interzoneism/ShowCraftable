using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Cecil;
using Newtonsoft.Json;
using ProperVersion;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class ModContainer : Mod
{
	public List<string> MissingDependencies;

	private string selectedAssemblyFile;

	private static HashAlgorithm fileHasher = SHA1.Create();

	public bool Enabled => Status == ModStatus.Enabled;

	public ModStatus Status { get; set; } = ModStatus.Enabled;

	public ModError? Error { get; set; }

	public string FolderPath { get; private set; }

	public List<string> SourceFiles { get; } = new List<string>();

	public List<string> AssemblyFiles { get; } = new List<string>();

	public bool RequiresCompilation => SourceFiles.Count > 0;

	public Assembly Assembly { get; private set; }

	public ModContainer(FileSystemInfo fsInfo, ILogger parentLogger, bool logDebug)
	{
		base.SourceType = GetSourceType(fsInfo).Value;
		base.FileName = fsInfo.Name;
		base.SourcePath = fsInfo.FullName;
		base.Logger = new ModLogger(parentLogger, this);
		base.Logger.TraceLog = logDebug;
		switch (base.SourceType)
		{
		case EnumModSourceType.CS:
			SourceFiles.Add(base.SourcePath);
			break;
		case EnumModSourceType.DLL:
			AssemblyFiles.Add(base.SourcePath);
			break;
		case EnumModSourceType.Folder:
			FolderPath = base.SourcePath;
			break;
		case EnumModSourceType.ZIP:
			break;
		}
	}

	public static EnumModSourceType? GetSourceType(FileSystemInfo fsInfo)
	{
		if (fsInfo is DirectoryInfo)
		{
			return EnumModSourceType.Folder;
		}
		return GetSourceTypeFromExtension(fsInfo.Name);
	}

	private static EnumModSourceType? GetSourceTypeFromExtension(string fileName)
	{
		string extension = Path.GetExtension(fileName);
		if (string.IsNullOrEmpty(extension))
		{
			return null;
		}
		extension = extension.Substring(1).ToUpperInvariant();
		if (!Enum.TryParse<EnumModSourceType>(extension, out var result))
		{
			return null;
		}
		return result;
	}

	public void SetError(ModError error)
	{
		Status = ModStatus.Errored;
		Error = error;
	}

	public static IEnumerable<string> EnumerateModFiles(string path)
	{
		IEnumerable<string> enumerable = Directory.EnumerateFiles(path);
		foreach (string item in Directory.EnumerateDirectories(path))
		{
			if (!(Path.GetFileName(item) == ".git"))
			{
				enumerable = enumerable.Concat(EnumerateModFiles(item));
			}
		}
		return enumerable;
	}

	public void Unpack(string unpackPath)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		if (!Enabled || SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
		{
			return;
		}
		if (base.SourceType == EnumModSourceType.ZIP)
		{
			using (FileStream inputStream = File.OpenRead(base.SourcePath))
			{
				byte[] source = fileHasher.ComputeHash(inputStream);
				StringBuilder stringBuilder = new StringBuilder(12);
				foreach (byte item in source.Take(6))
				{
					stringBuilder.Append(item.ToString("x2"));
				}
				FolderPath = Path.Combine(unpackPath, base.FileName + "_" + stringBuilder.ToString());
			}
			if (!Directory.Exists(FolderPath))
			{
				try
				{
					Directory.CreateDirectory(FolderPath);
					ZipFile val = new ZipFile(base.SourcePath, (StringCodec)null);
					try
					{
						foreach (ZipEntry item2 in val)
						{
							ZipEntry val2 = item2;
							string path = Path.Combine(FolderPath, val2.Name);
							if (val2.IsDirectory)
							{
								Directory.CreateDirectory(path);
								continue;
							}
							string directoryName = Path.GetDirectoryName(path);
							if (!Directory.Exists(directoryName))
							{
								Directory.CreateDirectory(directoryName);
							}
							using Stream stream = val.GetInputStream(val2);
							using FileStream destination = new FileStream(path, FileMode.Create);
							stream.CopyTo(destination);
						}
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
				}
				catch (Exception e)
				{
					base.Logger.Error("An exception was thrown when trying to extract the mod archive to '{0}':", FolderPath);
					base.Logger.Error(e);
					SetError(ModError.Loading);
					try
					{
						Directory.Delete(FolderPath, recursive: true);
						return;
					}
					catch (Exception)
					{
						base.Logger.Error("Additionally, there was an exception when deleting cached mod folder path '{0}':", FolderPath);
						base.Logger.Error(e);
						return;
					}
				}
			}
		}
		string text = Path.Combine(FolderPath, ".ignore");
		IgnoreFile ignoreFile = (File.Exists(text) ? new IgnoreFile(text, FolderPath) : null);
		foreach (string item3 in EnumerateModFiles(FolderPath))
		{
			if (ignoreFile != null && !ignoreFile.Available(item3))
			{
				continue;
			}
			EnumModSourceType? sourceTypeFromExtension = GetSourceTypeFromExtension(item3);
			string text2 = item3.Substring(FolderPath.Length + 1);
			int num = text2.IndexOfAny(new char[2] { '/', '\\' });
			string text3 = ((num >= 0) ? text2.Substring(0, num) : null);
			switch (sourceTypeFromExtension)
			{
			case EnumModSourceType.CS:
				if (text3 != "src")
				{
					base.Logger.Error("File '{0}' is not in the 'src/' subfolder.", Path.GetFileName(item3));
					if (base.SourceType != EnumModSourceType.Folder)
					{
						SetError(ModError.Loading);
						return;
					}
				}
				else
				{
					SourceFiles.Add(item3);
				}
				break;
			case EnumModSourceType.DLL:
				if (text3 == "native")
				{
					break;
				}
				if (text3 != null)
				{
					base.Logger.Error("File '{0}' is not in the mod's root folder. Won't load this mod. If you need to ship unmanaged dlls, put them in the native/ folder.", Path.GetFileName(item3));
					if (base.SourceType != EnumModSourceType.Folder)
					{
						SetError(ModError.Loading);
						return;
					}
				}
				else
				{
					AssemblyFiles.Add(item3);
				}
				break;
			}
		}
	}

	public void LoadModInfo(ModCompilationContext compilationContext, ModAssemblyLoader loader)
	{
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Expected O, but got Unknown
		if (!Enabled || base.Info != null)
		{
			return;
		}
		try
		{
			if (base.SourceType == EnumModSourceType.ZIP || base.SourceType == EnumModSourceType.Folder)
			{
				if (FolderPath != null)
				{
					string path = Path.Combine(FolderPath, "modinfo.json");
					if (File.Exists(path))
					{
						string text = File.ReadAllText(path);
						base.Info = JsonConvert.DeserializeObject<ModInfo>(text);
						base.Info?.Init();
					}
					string path2 = Path.Combine(FolderPath, "worldconfig.json");
					if (File.Exists(path2))
					{
						string text2 = File.ReadAllText(path2);
						base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(text2);
					}
					string text3 = null;
					if (!string.IsNullOrWhiteSpace(base.Info?.IconPath))
					{
						try
						{
							text3 = Path.GetFullPath(Path.Combine(FolderPath, base.Info.IconPath));
						}
						catch (Exception ex)
						{
							base.Logger.Warning("Failed create path from the IconPath '{0}' specified in the ModInfo, did you use characters that are not valid in a path?: {1}.", base.Info.IconPath, ex);
							text3 = null;
						}
						if (!text3.StartsWithOrdinal(FolderPath))
						{
							base.Logger.Warning("The IconPath '{0}' specified in the ModInfo tried to escape the mod root. This is not allowed.", base.Info.IconPath);
							text3 = null;
						}
					}
					else
					{
						text3 = Path.Combine(FolderPath, "modicon.png");
						if (!File.Exists(text3))
						{
							text3 = GetFallbackIconPath();
						}
					}
					if (File.Exists(text3))
					{
						base.Icon = new BitmapExternal(text3, base.Logger);
					}
				}
				else
				{
					ZipFile val = new ZipFile(base.SourcePath, (StringCodec)null);
					try
					{
						ZipEntry entry = val.GetEntry("modinfo.json");
						if (entry != null)
						{
							using StreamReader streamReader = new StreamReader(val.GetInputStream(entry));
							string text4 = streamReader.ReadToEnd();
							base.Info = JsonConvert.DeserializeObject<ModInfo>(text4);
							base.Info?.Init();
						}
						ZipEntry entry2 = val.GetEntry("worldconfig.json");
						if (entry2 != null)
						{
							using StreamReader streamReader2 = new StreamReader(val.GetInputStream(entry2));
							string text5 = streamReader2.ReadToEnd();
							base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(text5);
						}
						if (!string.IsNullOrWhiteSpace(base.Info?.IconPath))
						{
							ZipEntry entry3 = val.GetEntry(base.Info.IconPath);
							if (entry3 != null)
							{
								using MemoryStream memoryStream = new MemoryStream(1048576);
								using Stream stream = val.GetInputStream(entry3);
								stream.CopyTo(memoryStream);
								base.Icon = new BitmapExternal(memoryStream, base.Logger);
							}
							else
							{
								base.Logger.Warning("Failed find the IconPath '{0}' specified in the ModInfo within the mod archive.", base.Info.IconPath);
							}
						}
						else
						{
							ZipEntry entry4 = val.GetEntry("modicon.png");
							if (entry4 != null)
							{
								using MemoryStream memoryStream2 = new MemoryStream(1048576);
								using Stream stream2 = val.GetInputStream(entry4);
								stream2.CopyTo(memoryStream2);
								base.Icon = new BitmapExternal(memoryStream2, base.Logger);
							}
							else
							{
								string text6 = GetFallbackIconPath();
								if (File.Exists(text6))
								{
									base.Icon = new BitmapExternal(text6, base.Logger);
								}
							}
						}
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
					if (base.WorldConfig != null)
					{
						Unpack(Path.Combine(GamePaths.Cache, "unpack"));
					}
				}
				if (base.Info == null)
				{
					base.Logger.Error("Missing modinfo.json");
					SetError(ModError.Loading);
				}
				if (SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
				{
					base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", base.SourceType);
				}
				if (base.WorldConfig != null)
				{
					Lang.PreLoadModWorldConfig(FolderPath, base.Info?.ModID, Lang.CurrentLocale);
				}
			}
			else
			{
				base.Info = LoadModInfoFromCode(compilationContext, loader, out var modWorldConfig);
				base.Info?.Init();
				base.WorldConfig = modWorldConfig;
				if (base.Info == null)
				{
					base.Logger.Error("Missing ModInfoAttribute");
					SetError(ModError.Loading);
				}
				string text7 = null;
				if (!string.IsNullOrWhiteSpace(base.Info?.IconPath))
				{
					try
					{
						text7 = Path.GetFullPath(Path.Combine(GamePaths.AssetsPath, base.Info.IconPath));
					}
					catch (Exception ex2)
					{
						base.Logger.Warning("Failed create path from the IconPath '{0}' specified in the ModInfo, did you use characters that are not valid in a path?: {0}.", base.Info.IconPath, ex2);
						text7 = null;
					}
					if (!text7.StartsWithOrdinal(GamePaths.AssetsPath))
					{
						base.Logger.Warning("The IconPath '{0}' specified in the ModInfo tried to escape the AssetPath. This is not allowed.", base.Info.IconPath);
						text7 = null;
					}
				}
				else
				{
					text7 = GetFallbackIconPath();
				}
				if (File.Exists(text7))
				{
					base.Icon = new BitmapExternal(text7, base.Logger);
				}
			}
			if (base.Info != null)
			{
				CheckProperVersions();
			}
		}
		catch (Exception e)
		{
			base.Logger.Error("An exception was thrown trying to to load the ModInfo:");
			base.Logger.Error(e);
			SetError(ModError.Loading);
		}
		static string GetFallbackIconPath()
		{
			return Path.Combine(GamePaths.AssetsPath, "game/textures/gui/3rdpartymodicon.png");
		}
	}

	private ModInfo LoadModInfoFromCode(ModCompilationContext compilationContext, ModAssemblyLoader loader, out ModWorldConfiguration modWorldConfig)
	{
		if (RequiresCompilation)
		{
			if (AssemblyFiles.Count > 0)
			{
				throw new Exception("Found both .cs and .dll files, this is not supported");
			}
			Assembly = compilationContext.CompileFromFiles(this);
			base.Logger.Notification("Successfully compiled {0} source files", SourceFiles.Count);
			base.Logger.VerboseDebug("Successfully compiled {0} source files", SourceFiles.Count);
			return LoadModInfoFromAssembly(Assembly, out modWorldConfig);
		}
		base.Logger.VerboseDebug("Check for mod systems in mod {0}", string.Join(", ", AssemblyFiles));
		List<string> list = AssemblyFiles.Where((string file) => isEligible(file)).ToList();
		if (list.Count == 0)
		{
			throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
		}
		if (list.Count >= 2)
		{
			throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
		}
		selectedAssemblyFile = list[0];
		base.Logger.VerboseDebug("Selected assembly {0}", selectedAssemblyFile);
		return LoadModInfoFromAssemblyDefinition(loader.LoadAssemblyDefinition(selectedAssemblyFile), out modWorldConfig);
		bool isEligible(string path)
		{
			AssemblyDefinition val = loader.LoadAssemblyDefinition(path);
			if (((IEnumerable<CustomAttribute>)val.CustomAttributes).Any((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModInfoAttribute"))
			{
				return ((IEnumerable<ModuleDefinition>)val.Modules).SelectMany((ModuleDefinition module) => (IEnumerable<TypeDefinition>)module.Types).Any((TypeDefinition type) => !type.IsAbstract && isModSystem(type));
			}
			return false;
		}
		static bool isModSystem(TypeDefinition typeDefinition)
		{
			TypeReference val = typeDefinition.BaseType;
			while (val != null)
			{
				if (((MemberReference)val).FullName == typeof(ModSystem).FullName)
				{
					return true;
				}
				TypeDefinition obj = val.Resolve();
				val = ((obj != null) ? obj.BaseType : null);
			}
			return false;
		}
	}

	public void LoadAssembly(ModCompilationContext compilationContext, ModAssemblyLoader loader)
	{
		EnumModType enumModType = base.Info?.Type ?? EnumModType.Code;
		if (!Enabled || Assembly != null)
		{
			return;
		}
		if (enumModType != EnumModType.Code)
		{
			if (SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
			{
				base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", enumModType);
			}
			return;
		}
		try
		{
			if (RequiresCompilation)
			{
				Assembly = compilationContext.CompileFromFiles(this);
				base.Logger.Notification("Successfully compiled {0} source files", SourceFiles.Count);
				base.Logger.VerboseDebug("Successfully compiled {0} source files", SourceFiles.Count);
				return;
			}
			if (selectedAssemblyFile != null)
			{
				Assembly = loader.LoadFrom(selectedAssemblyFile);
				return;
			}
			base.Logger.VerboseDebug("Check for mod systems in mod {0}", string.Join(", ", AssemblyFiles));
			List<Assembly> list = (from path in AssemblyFiles
				select loader.LoadFrom(path) into ass
				where ass.GetCustomAttribute<ModInfoAttribute>() != null || GetModSystems(ass).Any()
				select ass).ToList();
			if (list.Count == 0)
			{
				throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
			}
			if (list.Count >= 2)
			{
				throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
			}
			Assembly = list[0];
			base.Logger.VerboseDebug("Loaded assembly {0}", Assembly.Location);
		}
		catch (Exception ex)
		{
			if (ex.Message == "Assembly with same name is already loaded")
			{
				base.Logger.Error("The mod's .dll was already loaded and cannot be reloaded. Most likely cause is switching mod versions after already playing one world. Other rare causes include two mods with .dlls with the same name");
				SetError(ModError.ChangedVersion);
			}
			else
			{
				base.Logger.Error("An exception was thrown when trying to load assembly:");
				base.Logger.Error(ex);
				SetError(ModError.Loading);
			}
		}
	}

	public void InstantiateModSystems(EnumAppSide side)
	{
		if (!Enabled || Assembly == null || base.Systems.Count > 0)
		{
			return;
		}
		if (base.Info == null)
		{
			throw new InvalidOperationException("LoadModInfo was not called before InstantiateModSystems");
		}
		if (!base.Info.Side.Is(side))
		{
			Status = ModStatus.Unused;
			return;
		}
		List<ModSystem> list = new List<ModSystem>();
		foreach (Type modSystem2 in GetModSystems(Assembly))
		{
			try
			{
				ModSystem modSystem = (ModSystem)Activator.CreateInstance(modSystem2);
				modSystem.Mod = this;
				list.Add(modSystem);
			}
			catch (Exception e)
			{
				base.Logger.Error("Exception thrown when trying to create an instance of ModSystem {0}:", modSystem2);
				base.Logger.Error(e);
			}
		}
		base.Systems = list.AsReadOnly();
		if (base.Systems.Count == 0 && FolderPath == null)
		{
			base.Logger.Warning("Is a Code mod, but no ModSystems found");
		}
	}

	private IEnumerable<Type> GetModSystems(Assembly assembly)
	{
		try
		{
			return from type in assembly.GetTypes()
				where typeof(ModSystem).IsAssignableFrom(type) && !type.IsAbstract
				select type;
		}
		catch (Exception ex)
		{
			if (ex is ReflectionTypeLoadException)
			{
				Exception[] loaderExceptions = (ex as ReflectionTypeLoadException).LoaderExceptions;
				base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}. Will ignore asssembly. Loader exceptions:", assembly.FullName);
				base.Logger.Error(ex);
				if (ex.InnerException != null)
				{
					base.Logger.Error("InnerException:");
					base.Logger.Error(ex.InnerException);
				}
				for (int num = 0; num < loaderExceptions.Length; num++)
				{
					base.Logger.Error(loaderExceptions[num]);
				}
			}
			else
			{
				base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}: {1}, InnerException: {2}. Will ignore asssembly.", assembly.FullName, ex, ex.InnerException);
			}
			return Enumerable.Empty<Type>();
		}
	}

	private ModInfo LoadModInfoFromAssembly(Assembly assembly, out ModWorldConfiguration modWorldConfig)
	{
		ModInfoAttribute customAttribute = assembly.GetCustomAttribute<ModInfoAttribute>();
		if (customAttribute == null)
		{
			modWorldConfig = null;
			return null;
		}
		List<ModDependency> dependencies = (from attr in assembly.GetCustomAttributes<ModDependencyAttribute>()
			select new ModDependency(attr.ModID, attr.Version)).ToList();
		return LoadModInfoFromModInfoAttribute(customAttribute, dependencies, out modWorldConfig);
	}

	private ModInfo LoadModInfoFromAssemblyDefinition(AssemblyDefinition assemblyDefinition, out ModWorldConfiguration modWorldConfig)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		CustomAttribute val = ((IEnumerable<CustomAttribute>)assemblyDefinition.CustomAttributes).SingleOrDefault((System.Func<CustomAttribute, bool>)((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModInfoAttribute"));
		if (val == null)
		{
			modWorldConfig = null;
			return null;
		}
		CustomAttributeArgument val2 = val.ConstructorArguments[0];
		string name = ((CustomAttributeArgument)(ref val2)).Value as string;
		val2 = val.ConstructorArguments[1];
		string modID = ((CustomAttributeArgument)(ref val2)).Value as string;
		ModInfoAttribute modInfoAttribute = new ModInfoAttribute(name, modID);
		foreach (CustomAttributeNamedArgument item in ((IEnumerable<CustomAttributeNamedArgument>)val.Properties).Where((CustomAttributeNamedArgument p) => ((CustomAttributeNamedArgument)(ref p)).Name != "Name" && ((CustomAttributeNamedArgument)(ref p)).Name != "ModID"))
		{
			CustomAttributeNamedArgument current = item;
			PropertyInfo property = modInfoAttribute.GetType().GetProperty(((CustomAttributeNamedArgument)(ref current)).Name);
			val2 = ((CustomAttributeNamedArgument)(ref current)).Argument;
			if (((CustomAttributeArgument)(ref val2)).Value is CustomAttributeArgument[] source)
			{
				property.SetValue(modInfoAttribute, source.Select((CustomAttributeArgument item) => ((CustomAttributeArgument)(ref item)).Value as string).ToArray());
			}
			else
			{
				val2 = ((CustomAttributeNamedArgument)(ref current)).Argument;
				property.SetValue(modInfoAttribute, ((CustomAttributeArgument)(ref val2)).Value);
			}
		}
		List<ModDependency> dependencies = ((IEnumerable<CustomAttribute>)assemblyDefinition.CustomAttributes).Where((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModDependencyAttribute").Select(delegate(CustomAttribute attribute)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			CustomAttributeArgument val3 = attribute.ConstructorArguments[0];
			string modID2 = (string)((CustomAttributeArgument)(ref val3)).Value;
			val3 = attribute.ConstructorArguments[1];
			return new ModDependency(modID2, ((CustomAttributeArgument)(ref val3)).Value as string);
		}).ToList();
		return LoadModInfoFromModInfoAttribute(modInfoAttribute, dependencies, out modWorldConfig);
	}

	private ModInfo LoadModInfoFromModInfoAttribute(ModInfoAttribute modInfoAttr, List<ModDependency> dependencies, out ModWorldConfiguration modWorldConfig)
	{
		if (!Enum.TryParse<EnumAppSide>(modInfoAttr.Side, ignoreCase: true, out var result))
		{
			base.Logger.Warning("Cannot parse '{0}', must be either 'Client', 'Server' or 'Universal'. Defaulting to 'Universal'.", modInfoAttr.Side);
			result = EnumAppSide.Universal;
		}
		if (modInfoAttr.WorldConfig != null)
		{
			modWorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(modInfoAttr.WorldConfig);
		}
		else
		{
			modWorldConfig = null;
		}
		return new ModInfo(EnumModType.Code, modInfoAttr.Name, modInfoAttr.ModID, modInfoAttr.Version, modInfoAttr.Description, modInfoAttr.Authors, modInfoAttr.Contributors, modInfoAttr.Website, result, modInfoAttr.RequiredOnClient, modInfoAttr.RequiredOnServer, dependencies)
		{
			NetworkVersion = modInfoAttr.NetworkVersion,
			CoreMod = modInfoAttr.CoreMod,
			IconPath = modInfoAttr.IconPath
		};
	}

	private void CheckProperVersions()
	{
		if (!string.IsNullOrEmpty(base.Info.Version) && !SemVer.TryParse(base.Info.Version, out var result, out var error))
		{
			base.Logger.Warning("{0} (best guess: {1})", error, result);
		}
		foreach (ModDependency dependency in base.Info.Dependencies)
		{
			if (!(dependency.Version == "*") && !string.IsNullOrEmpty(dependency.Version) && !SemVer.TryParse(dependency.Version, out result, out error))
			{
				base.Logger.Warning("Dependency '{0}': {1} (best guess: {2})", dependency.ModID, error, result);
			}
		}
	}
}
