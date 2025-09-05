using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Vintagestory.API.Common;

public class FrameProfilerUtil
{
	public bool Enabled;

	public bool PrintSlowTicks;

	public int PrintSlowTicksThreshold = 40;

	public ProfileEntryRange PrevRootEntry;

	public string summary;

	public string OutputPrefix = "";

	public static ConcurrentQueue<string> offThreadProfiles = new ConcurrentQueue<string>();

	public static bool PrintSlowTicks_Offthreads;

	public static int PrintSlowTicksThreshold_Offthreads = 40;

	private Stopwatch stopwatch = new Stopwatch();

	private ProfileEntryRange rootEntry;

	private ProfileEntryRange currentEntry;

	private string beginText;

	private Action<string> onLogoutputHandler;

	public FrameProfilerUtil(Action<string> onLogoutputHandler)
	{
		this.onLogoutputHandler = onLogoutputHandler;
		stopwatch.Start();
	}

	public FrameProfilerUtil(string outputPrefix)
		: this(delegate(string text)
		{
			offThreadProfiles.Enqueue(text);
		})
	{
		OutputPrefix = outputPrefix;
	}

	public void Begin(string beginText = null, params object[] args)
	{
		if (Enabled || PrintSlowTicks)
		{
			this.beginText = ((beginText == null) ? null : string.Format(beginText, args));
			currentEntry = null;
			rootEntry = Enter("all");
		}
	}

	public ProfileEntryRange Enter(string code)
	{
		if (!Enabled && !PrintSlowTicks)
		{
			return null;
		}
		long elapsedTicks = stopwatch.ElapsedTicks;
		if (currentEntry == null)
		{
			ProfileEntryRange obj = new ProfileEntryRange
			{
				Code = code,
				Start = elapsedTicks,
				LastMark = elapsedTicks,
				CallCount = 0
			};
			ProfileEntryRange result = obj;
			currentEntry = obj;
			return result;
		}
		if (currentEntry.ChildRanges == null)
		{
			currentEntry.ChildRanges = new Dictionary<string, ProfileEntryRange>();
		}
		if (!currentEntry.ChildRanges.TryGetValue(code, out var value))
		{
			Dictionary<string, ProfileEntryRange> childRanges = currentEntry.ChildRanges;
			ProfileEntryRange obj2 = new ProfileEntryRange
			{
				Code = code,
				Start = elapsedTicks,
				LastMark = elapsedTicks,
				CallCount = 0
			};
			value = obj2;
			childRanges[code] = obj2;
			value.ParentRange = currentEntry;
		}
		else
		{
			value.Start = elapsedTicks;
			value.LastMark = elapsedTicks;
		}
		currentEntry = value;
		value.CallCount++;
		return value;
	}

	public void Leave()
	{
		if (Enabled || PrintSlowTicks)
		{
			long elapsedTicks = stopwatch.ElapsedTicks;
			currentEntry.ElapsedTicks += elapsedTicks - currentEntry.Start;
			currentEntry.LastMark = elapsedTicks;
			currentEntry = currentEntry.ParentRange;
			for (ProfileEntryRange parentRange = currentEntry; parentRange != null; parentRange = parentRange.ParentRange)
			{
				parentRange.LastMark = elapsedTicks;
			}
		}
	}

	public void Mark(string code, object param)
	{
		if (Enabled || PrintSlowTicks)
		{
			if (code == null)
			{
				throw new ArgumentNullException("marker name may not be null!");
			}
			MarkInternal(code + param);
		}
	}

	public void Mark(string code)
	{
		if (Enabled || PrintSlowTicks)
		{
			if (code == null)
			{
				throw new ArgumentNullException("marker name may not be null!");
			}
			MarkInternal(code);
		}
	}

	private void MarkInternal(string code)
	{
		try
		{
			ProfileEntryRange profileEntryRange = currentEntry;
			if (profileEntryRange != null)
			{
				Dictionary<string, ProfileEntry> dictionary = profileEntryRange.Marks;
				if (dictionary == null)
				{
					dictionary = (profileEntryRange.Marks = new Dictionary<string, ProfileEntry>());
				}
				if (!dictionary.TryGetValue(code, out var value))
				{
					value = (dictionary[code] = new ProfileEntry());
				}
				long elapsedTicks = stopwatch.ElapsedTicks;
				value.ElapsedTicks += (int)(elapsedTicks - profileEntryRange.LastMark);
				value.CallCount++;
				profileEntryRange.LastMark = elapsedTicks;
			}
		}
		catch (Exception)
		{
		}
	}

	public void End()
	{
		if (!Enabled && !PrintSlowTicks)
		{
			return;
		}
		Mark("end");
		Leave();
		PrevRootEntry = rootEntry;
		double num = (double)rootEntry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
		if (PrintSlowTicks && num > (double)PrintSlowTicksThreshold)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (beginText != null)
			{
				stringBuilder.Append(beginText).Append(' ');
			}
			stringBuilder.AppendLine($"{OutputPrefix}A tick took {num:0.##} ms");
			slowTicksToString(rootEntry, stringBuilder);
			summary = "Stopwatched total= " + num + "ms";
			onLogoutputHandler(stringBuilder.ToString());
		}
	}

	public void OffThreadEnd()
	{
		End();
		Enabled = (PrintSlowTicks = PrintSlowTicks_Offthreads);
		PrintSlowTicksThreshold = PrintSlowTicksThreshold_Offthreads;
	}

	private void slowTicksToString(ProfileEntryRange entry, StringBuilder strib, double thresholdMs = 0.35, string indent = "")
	{
		try
		{
			double num = (double)entry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
			if (num < thresholdMs)
			{
				return;
			}
			if (entry.CallCount > 1)
			{
				strib.AppendLine(indent + $"{num:0.00}ms, {entry.CallCount:####} calls, avg {num * 1000.0 / (double)Math.Max(entry.CallCount, 1):0.00} us/call: {entry.Code:0.00}");
			}
			else
			{
				strib.AppendLine(indent + $"{num:0.00}ms, {entry.CallCount:####} call : {entry.Code}");
			}
			List<ProfileEntryRange> list = new List<ProfileEntryRange>();
			if (entry.Marks != null)
			{
				list.AddRange(entry.Marks.Select((KeyValuePair<string, ProfileEntry> e) => new ProfileEntryRange
				{
					ElapsedTicks = e.Value.ElapsedTicks,
					Code = e.Key,
					CallCount = e.Value.CallCount
				}));
			}
			if (entry.ChildRanges != null)
			{
				list.AddRange(entry.ChildRanges.Values);
			}
			IOrderedEnumerable<ProfileEntryRange> orderedEnumerable = list.OrderByDescending((ProfileEntryRange prof) => prof.ElapsedTicks);
			int num2 = 0;
			foreach (ProfileEntryRange item in orderedEnumerable)
			{
				if (num2++ > 8)
				{
					break;
				}
				slowTicksToString(item, strib, thresholdMs, indent + "  ");
			}
		}
		catch (Exception)
		{
		}
	}
}
