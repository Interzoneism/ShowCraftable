using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vintagestory.API.Util;

public class IgnoreFile
{
	public readonly string filename;

	public readonly string fullpath;

	private List<string> ignored = new List<string>();

	private List<string> ignoredFiles = new List<string>();

	public IgnoreFile(string filename, string fullpath)
	{
		this.filename = filename;
		this.fullpath = fullpath;
		string[] array = File.ReadAllLines(filename);
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.StartsWithOrdinal("!"))
			{
				ignoredFiles.Add(WildCardToRegular(text.Substring(1)));
				continue;
			}
			bool num = text.EndsWith('/');
			string text2 = cleanUpPath(text.Replace('/', Path.DirectorySeparatorChar));
			if (num)
			{
				text2 = text2 + Path.DirectorySeparatorChar + "*";
			}
			ignored.Add(WildCardToRegular(text2));
		}
	}

	private string cleanUpPath(string path)
	{
		return Path.Combine(path.Split('/', '\\'));
	}

	private static string WildCardToRegular(string value)
	{
		return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
	}

	public bool Available(string path)
	{
		if (ignoredFiles.Count > 0 && File.Exists(path))
		{
			string fileName = Path.GetFileName(path);
			foreach (string ignoredFile in ignoredFiles)
			{
				if (Regex.IsMatch(fileName, ignoredFile))
				{
					return false;
				}
			}
		}
		path = cleanUpPath(path.Replace(fullpath, ""));
		foreach (string item in ignored)
		{
			if (Regex.IsMatch(path, item))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsPathDirectory(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		path = path.Trim();
		if (Directory.Exists(path))
		{
			return true;
		}
		if (File.Exists(path))
		{
			return false;
		}
		if (new char[2] { '\\', '/' }.Any((char x) => path.EndsWith(x)))
		{
			return true;
		}
		return string.IsNullOrWhiteSpace(Path.GetExtension(path));
	}
}
