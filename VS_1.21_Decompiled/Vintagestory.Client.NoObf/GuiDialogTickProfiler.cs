using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiDialogTickProfiler : GuiDialog
{
	private ProfileEntryRange root1sSum = new ProfileEntryRange();

	private int frames;

	private int maxLines = 20;

	public override string ToggleKeyCombinationCode => "tickprofiler";

	public override bool Focusable => false;

	public override EnumDialogType DialogType => EnumDialogType.HUD;

	public GuiDialogTickProfiler(ICoreClientAPI capi)
		: base(capi)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		root1sSum.Code = "root";
		capi.Event.RegisterGameTickListener(OnEverySecond, 1000);
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		FontExtents fontExtents = cairoFont.GetFontExtents();
		double num = ((FontExtents)(ref fontExtents)).Height * cairoFont.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 450.0, 30.0 + (double)maxLines * num);
		ElementBounds bounds = elementBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds bounds2 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("tickprofiler", bounds2).AddGameOverlay(bounds).AddDynamicText("", cairoFont, elementBounds, "text")
			.Compose();
	}

	private void OnEverySecond(float dt)
	{
		if (IsOpened())
		{
			StringBuilder stringBuilder = new StringBuilder();
			ticksToString(root1sSum, stringBuilder);
			string text = stringBuilder.ToString();
			string[] array = text.Split('\n');
			if (array.Length > maxLines)
			{
				text = string.Join("\n", array, 0, maxLines);
			}
			base.SingleComposer.GetDynamicText("text").SetNewText(text, autoHeight: true);
			frames = 0;
			root1sSum = new ProfileEntryRange();
			root1sSum.Code = "root";
		}
	}

	private void ticksToString(ProfileEntryRange entry, StringBuilder strib, string indent = "")
	{
		double num = (double)entry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0 / (double)frames;
		if (num < 0.2)
		{
			return;
		}
		string arg = ((entry.Code.Length > 37) ? ("..." + entry.Code?.Substring(Math.Max(0, entry.Code.Length - 40))) : entry.Code);
		strib.AppendLine(indent + $"{num:0.00}ms, {entry.CallCount:####} calls/s, {arg}");
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
		foreach (ProfileEntryRange item in list.OrderByDescending((ProfileEntryRange prof) => prof.ElapsedTicks).Take(25))
		{
			ticksToString(item, strib, indent + "  ");
		}
	}

	public override void OnFinalizeFrame(float dt)
	{
		if (IsOpened())
		{
			ProfileEntryRange prevRootEntry = ScreenManager.FrameProfiler.PrevRootEntry;
			if (prevRootEntry != null)
			{
				sumUpTickCosts(prevRootEntry, root1sSum);
			}
			frames++;
			base.OnFinalizeFrame(dt);
		}
	}

	private void sumUpTickCosts(ProfileEntryRange entry, ProfileEntryRange sumEntry)
	{
		sumEntry.ElapsedTicks += entry.ElapsedTicks;
		sumEntry.CallCount += entry.CallCount;
		if (entry.Marks != null)
		{
			if (sumEntry.Marks == null)
			{
				sumEntry.Marks = new Dictionary<string, ProfileEntry>();
			}
			foreach (KeyValuePair<string, ProfileEntry> mark in entry.Marks)
			{
				if (!sumEntry.Marks.TryGetValue(mark.Key, out var value))
				{
					ProfileEntry profileEntry = (sumEntry.Marks[mark.Key] = new ProfileEntry(mark.Value.ElapsedTicks, mark.Value.CallCount));
					value = profileEntry;
				}
				value.ElapsedTicks += mark.Value.ElapsedTicks;
				value.CallCount += mark.Value.CallCount;
			}
		}
		if (entry.ChildRanges == null)
		{
			return;
		}
		if (sumEntry.ChildRanges == null)
		{
			sumEntry.ChildRanges = new Dictionary<string, ProfileEntryRange>();
		}
		foreach (KeyValuePair<string, ProfileEntryRange> childRange in entry.ChildRanges)
		{
			if (!sumEntry.ChildRanges.TryGetValue(childRange.Key, out var value2))
			{
				ProfileEntryRange profileEntryRange = (sumEntry.ChildRanges[childRange.Key] = new ProfileEntryRange());
				value2 = profileEntryRange;
				value2.Code = childRange.Key;
			}
			sumUpTickCosts(childRange.Value, value2);
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ScreenManager.FrameProfiler.Enabled = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ScreenManager.FrameProfiler.Enabled = false;
	}
}
