using System;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Client.NoObf;

public class HudDebugScreen : HudElement
{
	private long lastUpdateMilliseconds;

	private int frameCount;

	private float longestframedt;

	private GuiComposer debugTextComposer;

	private GuiComposer systemInfoComposer;

	private GuiElementDynamicText textElement;

	private float[] dtHistory;

	private const int QuantityRenderedFrameSlices = 300;

	private MeshData frameSlicesUpdate;

	private MeshRef frameSlicesRef;

	private bool displayFullDebugInfo;

	private bool displayOnlyFpsDebugInfo;

	private bool displayOnlyFpsDebugInfoTemporary;

	private LoadedTexture[] fpsLabels;

	private Process _process;

	private int historyheight = 80;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudDebugScreen(ICoreClientAPI capi)
		: base(capi)
	{
		displayFullDebugInfo = false;
		dtHistory = new float[300];
		for (int i = 0; i < 300; i++)
		{
			dtHistory[i] = 0f;
		}
		GenFrameSlicesMesh();
		CairoFont font = CairoFont.WhiteDetailText();
		fpsLabels = new LoadedTexture[4]
		{
			capi.Gui.TextTexture.GenUnscaledTextTexture("30", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("60", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("75", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("150", font)
		};
		capi.ChatCommands.GetOrCreate("debug").BeginSubCommand("edi").RequiresPrivilege(Privilege.chat)
			.WithRootAlias("edi")
			.WithDescription("Show/Hide Extended information on debug screen")
			.HandleWith(ToggleExtendedDebugInfo)
			.EndSubCommand();
		capi.Event.RegisterGameTickListener(EveryOtherSecond, 2000);
		capi.Event.RegisterEventBusListener(delegate
		{
			displayOnlyFpsDebugInfoTemporary = false;
			if (!displayFullDebugInfo && !displayOnlyFpsDebugInfo)
			{
				TryClose();
			}
		}, 0.5, "leftGraphicsDlg");
		capi.Event.RegisterEventBusListener(delegate
		{
			displayOnlyFpsDebugInfoTemporary = true;
			TryOpen();
		}, 0.5, "enteredGraphicsDlg");
		debugTextComposer = capi.Gui.CreateCompo("debugScreenText", ElementBounds.Percentual(EnumDialogArea.RightTop, 0.5, 0.7).WithFixedAlignmentOffset(-5.0, 5.0)).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Right), ElementBounds.Fill, "debugScreenTextElem").OnlyDynamic()
			.Compose();
		systemInfoComposer = capi.Gui.CreateCompo("sysInfoText", ElementBounds.Percentual(EnumDialogArea.LeftTop, 0.5, 0.7).WithFixedAlignmentOffset(5.0, 5.0)).AddDynamicText("Game Version: " + GameVersion.LongGameVersion + "\n" + ScreenManager.Platform.GetGraphicCardInfos() + "\n" + ScreenManager.Platform.GetFrameworkInfos(), CairoFont.WhiteSmallishText(), ElementBounds.Fill).OnlyDynamic()
			.Compose();
		textElement = debugTextComposer.GetDynamicText("debugScreenTextElem");
		ScreenManager.hotkeyManager.SetHotKeyHandler("fpsgraph", OnKeyGraph);
		ScreenManager.hotkeyManager.SetHotKeyHandler("debugscreenandgraph", OnKeyDebugScreenAndGraph);
	}

	private void EveryOtherSecond(float dt)
	{
		GenFrameSlicesMesh();
	}

	private static TextCommandResult ToggleExtendedDebugInfo(TextCommandCallingArgs textCommandCallingArgs)
	{
		ClientSettings.ExtendedDebugInfo = !ClientSettings.ExtendedDebugInfo;
		return TextCommandResult.Success("Extended debug info " + (ClientSettings.ExtendedDebugInfo ? "on" : "off"));
	}

	public override void OnFinalizeFrame(float dt)
	{
		UpdateGraph(dt);
		UpdateText(dt);
		debugTextComposer.PostRender(dt);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (displayOnlyFpsDebugInfo || displayFullDebugInfo || displayOnlyFpsDebugInfoTemporary)
		{
			DrawGraph();
			debugTextComposer.Render(deltaTime);
		}
		if (displayFullDebugInfo)
		{
			systemInfoComposer.Render(deltaTime);
		}
	}

	private void UpdateText(float dt)
	{
		frameCount++;
		longestframedt = Math.Max(longestframedt, dt);
		float num = (capi.ElapsedMilliseconds - lastUpdateMilliseconds) / 1000;
		if (!(num >= 1f) || (!displayFullDebugInfo && !displayOnlyFpsDebugInfo && !displayOnlyFpsDebugInfoTemporary))
		{
			return;
		}
		lastUpdateMilliseconds = capi.ElapsedMilliseconds;
		ClientMain clientMain = capi.World as ClientMain;
		string text = GetFpsText(num);
		RuntimeStats.drawCallsCount = 0;
		longestframedt = 0f;
		frameCount = 0;
		if (!displayFullDebugInfo)
		{
			textElement.SetNewTextAsync(text);
			return;
		}
		OrderedDictionary<string, string> debugScreenInfo = clientMain.DebugScreenInfo;
		string text2 = decimal.Round((decimal)((float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		if (_process == null)
		{
			_process = Process.GetCurrentProcess();
		}
		_process.Refresh();
		string text3 = decimal.Round((decimal)((float)_process.WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		if (ClientSettings.GlDebugMode)
		{
			text += " (gl debug mode enabled!)";
		}
		if (clientMain.extendedDebugInfo)
		{
			text += " (edi enabled!)";
		}
		clientMain.DebugScreenInfo["fps"] = text;
		clientMain.DebugScreenInfo["mem"] = "CPU Mem Managed/Total: " + text2 + " / " + text3 + " MB";
		clientMain.DebugScreenInfo["entitycount"] = "entities: " + RuntimeStats.renderedEntities + " / " + clientMain.LoadedEntities.Count;
		bool flag = capi.World.Config.GetBool("allowCoordinateHud", defaultValue: true);
		if (clientMain.EntityPlayer != null)
		{
			EntityPos pos = clientMain.EntityPlayer.Pos;
			if (!flag)
			{
				debugScreenInfo["position"] = "(disabled)";
				debugScreenInfo["chunkpos"] = "(disabled)";
				debugScreenInfo["regpos"] = "(disabled)";
			}
			else
			{
				debugScreenInfo["position"] = "Position: " + pos.OnlyPosToString() + ((pos.Dimension > 0) ? (", dim " + pos.Dimension) : "");
				debugScreenInfo["chunkpos"] = "Chunk: " + (int)(pos.X / (double)clientMain.WorldMap.ClientChunkSize) + ", " + (int)(pos.Y / (double)clientMain.WorldMap.ClientChunkSize) + ", " + (int)(pos.Z / (double)clientMain.WorldMap.ClientChunkSize);
				debugScreenInfo["regpos"] = "Region: " + (int)(pos.X / (double)clientMain.WorldMap.RegionSize) + ", " + (int)(pos.Z / (double)clientMain.WorldMap.RegionSize);
			}
			float num2 = GameMath.Mod(clientMain.EntityPlayer.Pos.Yaw, (float)Math.PI * 2f);
			debugScreenInfo["orientation"] = "Yaw: " + (180f * num2 / (float)Math.PI).ToString("#.##", GlobalConstants.DefaultCultureInfo) + " deg., Facing: " + BlockFacing.HorizontalFromYaw(num2);
		}
		if (clientMain.BlockSelection != null)
		{
			BlockPos position = clientMain.BlockSelection.Position;
			Block block = clientMain.WorldMap.RelaxedBlockAccess.GetBlock(position, 1);
			Block block2 = clientMain.WorldMap.RelaxedBlockAccess.GetBlock(position, 2);
			BlockEntity blockEntity = clientMain.WorldMap.RelaxedBlockAccess.GetBlockEntity(clientMain.BlockSelection.Position);
			string text4 = string.Concat("Selected: ", block.BlockId.ToString(), "/", block.Code, " @", flag ? position.ToString() : "(disabled)");
			if (block2.BlockId != 0)
			{
				text4 = text4 + "\nFluids layer: " + block2.BlockId + "/" + block2.Code;
			}
			debugScreenInfo["curblock"] = text4;
			debugScreenInfo["curblockentity"] = "Selected BE: " + blockEntity?.GetType();
		}
		else
		{
			debugScreenInfo["curblock"] = "";
			debugScreenInfo["curblocklight"] = "";
		}
		if (clientMain.extendedDebugInfo)
		{
			if (clientMain.BlockSelection != null)
			{
				BlockPos blockPos = clientMain.BlockSelection.Position.AddCopy(clientMain.BlockSelection.Face);
				int[] lightHSVLevels = clientMain.WorldMap.GetLightHSVLevels(blockPos.X, blockPos.InternalY, blockPos.Z);
				debugScreenInfo["curblocklight"] = "FO: Sun V: " + lightHSVLevels[0] + ", Block H: " + lightHSVLevels[2] + ", Block S: " + lightHSVLevels[3] + ", Block V: " + lightHSVLevels[1];
				blockPos = clientMain.BlockSelection.Position;
				lightHSVLevels = clientMain.WorldMap.GetLightHSVLevels(blockPos.X, blockPos.InternalY, blockPos.Z);
				debugScreenInfo["curblocklight2"] = "Sun V: " + lightHSVLevels[0] + ", Block H: " + lightHSVLevels[2] + ", Block S: " + lightHSVLevels[3] + ", Block V: " + lightHSVLevels[1];
			}
			debugScreenInfo["tickstopwatch"] = clientMain.tickSummary;
		}
		else
		{
			debugScreenInfo["curblocklight"] = "";
			debugScreenInfo["curblocklight2"] = "";
			debugScreenInfo["tickstopwatch"] = "";
		}
		string text5 = "";
		foreach (string value in clientMain.DebugScreenInfo.Values)
		{
			text5 = text5 + value + "\n";
		}
		textElement.SetNewTextAsync(text5);
	}

	private string GetFpsText(float seconds)
	{
		if (!displayFullDebugInfo)
		{
			return $"Avg FPS: {(int)(1f * (float)frameCount / seconds)}, Min FPS: {(int)(1f / longestframedt)}";
		}
		return $"Avg FPS: {(int)(1f * (float)frameCount / seconds)}, Min FPS: {(int)(1f / longestframedt)}, DCs: {(int)((float)RuntimeStats.drawCallsCount / (1f * (float)frameCount / seconds))}";
	}

	public override bool TryClose()
	{
		return false;
	}

	public void DoClose()
	{
		base.TryClose();
	}

	private bool OnKeyDebugScreenAndGraph(KeyCombination viaKeyComb)
	{
		if (displayFullDebugInfo)
		{
			displayFullDebugInfo = false;
			DoClose();
			return true;
		}
		displayFullDebugInfo = true;
		TryOpen();
		return true;
	}

	private bool OnKeyGraph(KeyCombination viaKeyComb)
	{
		if (displayFullDebugInfo)
		{
			return true;
		}
		if (displayOnlyFpsDebugInfo)
		{
			displayOnlyFpsDebugInfo = false;
			if (!displayOnlyFpsDebugInfoTemporary)
			{
				DoClose();
			}
			return true;
		}
		displayOnlyFpsDebugInfo = true;
		TryOpen();
		return true;
	}

	private void UpdateGraph(float dt)
	{
		for (int i = 0; i < 299; i++)
		{
			dtHistory[i] = dtHistory[i + 1];
		}
		dtHistory[299] = dt;
	}

	private void DrawGraph()
	{
		updateFrameSlicesMesh();
		ClientMain clientMain = capi.World as ClientMain;
		int num = clientMain.Width - 310;
		int num2 = clientMain.Height - historyheight - 40;
		clientMain.Platform.BindTexture2d(clientMain.WhiteTexture());
		clientMain.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
		clientMain.guiShaderProg.ExtraGlow = 0;
		clientMain.guiShaderProg.ApplyColor = 1;
		clientMain.guiShaderProg.AlphaTest = 0f;
		clientMain.guiShaderProg.DarkEdges = 0;
		clientMain.guiShaderProg.NoTexture = 1f;
		clientMain.guiShaderProg.Tex2d2D = clientMain.WhiteTexture();
		clientMain.guiShaderProg.ProjectionMatrix = clientMain.CurrentProjectionMatrix;
		clientMain.guiShaderProg.ModelViewMatrix = clientMain.CurrentModelViewMatrix;
		clientMain.Platform.RenderMesh(frameSlicesRef);
		clientMain.Render2DTexture(clientMain.WhiteTexture(), num, num2 - historyheight, 300f, 1f);
		clientMain.Render2DTexture(clientMain.WhiteTexture(), num, (float)num2 - (float)(historyheight * 60) * (1f / 75f), 300f, 1f);
		clientMain.Render2DTexture(clientMain.WhiteTexture(), num, (float)num2 - (float)(historyheight * 60) * (1f / 30f), 300f, 1f);
		clientMain.Render2DTexture(clientMain.WhiteTexture(), num, (float)num2 - (float)(historyheight * 60) * (1f / 150f), 300f, 1f);
		clientMain.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		clientMain.Render2DLoadedTexture(fpsLabels[0], num, (float)num2 - (float)(historyheight * 60) * (1f / 30f));
		clientMain.Render2DLoadedTexture(fpsLabels[1], num, (float)num2 - (float)(historyheight * 60) * (1f / 60f));
		clientMain.Render2DLoadedTexture(fpsLabels[2], num, (float)num2 - (float)(historyheight * 60) * (1f / 75f));
		clientMain.Render2DLoadedTexture(fpsLabels[3], num, (float)num2 - (float)(historyheight * 60) * (1f / 150f));
		clientMain.Platform.GlToggleBlend(on: true);
	}

	private void updateFrameSlicesMesh()
	{
		int num = capi.Render.FrameHeight - historyheight - 40;
		for (int i = 0; i < 300; i++)
		{
			float num2 = dtHistory[i];
			num2 = num2 * 60f * (float)historyheight;
			int num3 = i * 4 * 3;
			frameSlicesUpdate.xyz[num3 + 7] = (float)num - num2;
			frameSlicesUpdate.xyz[num3 + 10] = (float)num - num2;
		}
		capi.Render.UpdateMesh(frameSlicesRef, frameSlicesUpdate);
	}

	private void GenFrameSlicesMesh()
	{
		MeshData meshData = new MeshData(1200, 1800, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		int num = capi.Render.FrameWidth - 310;
		int num2 = capi.Render.FrameHeight - historyheight - 40;
		for (int i = 0; i < 300; i++)
		{
			byte r = (byte)(255f * (float)i / 300f);
			MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(num + i, num2, 50f, 1f, 1f, r, 0, 0, byte.MaxValue);
			meshData.AddMeshData(customQuadModelData);
		}
		if (frameSlicesRef != null)
		{
			capi.Render.DeleteMesh(frameSlicesRef);
		}
		frameSlicesRef = capi.Render.UploadMesh(meshData);
		frameSlicesUpdate = meshData;
		frameSlicesUpdate.Rgba = null;
		frameSlicesUpdate.Indices = null;
	}

	public override void Dispose()
	{
		base.Dispose();
		debugTextComposer?.Dispose();
		systemInfoComposer?.Dispose();
		frameSlicesRef?.Dispose();
		int num = 0;
		while (fpsLabels != null && num < fpsLabels.Length)
		{
			fpsLabels[num]?.Dispose();
			num++;
		}
		_process?.Dispose();
	}
}
