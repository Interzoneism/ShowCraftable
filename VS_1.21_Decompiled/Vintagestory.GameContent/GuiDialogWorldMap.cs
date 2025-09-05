using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogWorldMap : GuiDialogGeneric
{
	protected EnumDialogType dialogType = EnumDialogType.HUD;

	protected OnViewChangedDelegate viewChanged;

	protected OnViewChangedSyncDelegate viewChangedSync;

	protected long listenerId;

	protected bool requireRecompose;

	protected int mapWidth = 1200;

	protected int mapHeight = 800;

	protected GuiComposer fullDialog;

	protected GuiComposer hudDialog;

	protected List<GuiTab> tabs;

	private List<string> tabnames;

	private Vec3d hoveredWorldPos = new Vec3d();

	private GuiDialogAddWayPoint addWpDlg;

	public override bool PrefersUngrabbedMouse => true;

	public override EnumDialogType DialogType => dialogType;

	public override double DrawOrder
	{
		get
		{
			if (dialogType != EnumDialogType.HUD)
			{
				return 0.11;
			}
			return 0.07;
		}
	}

	public List<MapLayer> MapLayers => (base.SingleComposer?.GetElement("mapElem") as GuiElementMap)?.mapLayers;

	public GuiDialogWorldMap(OnViewChangedDelegate viewChanged, OnViewChangedSyncDelegate viewChangedSync, ICoreClientAPI capi, List<string> tabnames)
		: base("", capi)
	{
		this.viewChanged = viewChanged;
		this.viewChangedSync = viewChangedSync;
		this.tabnames = tabnames;
		fullDialog = ComposeDialog(EnumDialogType.Dialog);
		hudDialog = ComposeDialog(EnumDialogType.HUD);
		CommandArgumentParsers parsers = capi.ChatCommands.Parsers;
		capi.ChatCommands.GetOrCreate("map").BeginSubCommand("worldmapsize").WithDescription("Show/set worldmap size")
			.WithArgs(parsers.OptionalInt("mapWidth", 1200), parsers.OptionalInt("mapHeight", 800))
			.HandleWith(OnCmdMapSize);
	}

	private TextCommandResult OnCmdMapSize(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success($"Current map size: {mapWidth}x{mapHeight}");
		}
		mapWidth = (int)args.Parsers[0].GetValue();
		mapHeight = (int)args.Parsers[1].GetValue();
		fullDialog = ComposeDialog(EnumDialogType.Dialog);
		return TextCommandResult.Success($"Map size {mapWidth}x{mapHeight} set");
	}

	private GuiComposer ComposeDialog(EnumDialogType dlgType)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 28.0, mapWidth, mapHeight);
		ElementBounds elementBounds2 = elementBounds.RightCopy().WithFixedSize(1.0, 350.0);
		ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(3.0);
		elementBounds3.BothSizing = ElementSizing.FitToChildren;
		elementBounds3.WithChildren(elementBounds, elementBounds2);
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		GuiComposer guiComposer;
		if (dlgType == EnumDialogType.HUD)
		{
			elementBounds = ElementBounds.Fixed(0.0, 0.0, 250.0, 250.0);
			elementBounds3 = ElementBounds.Fill.WithFixedPadding(2.0);
			elementBounds3.BothSizing = ElementSizing.FitToChildren;
			elementBounds3.WithChildren(elementBounds);
			bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(GetMinimapPosition(out var offsetX, out var offsetY)).WithFixedAlignmentOffset(offsetX, offsetY);
			guiComposer = hudDialog;
		}
		else
		{
			guiComposer = fullDialog;
		}
		Cuboidd cuboidd = null;
		if (guiComposer != null)
		{
			cuboidd = (guiComposer.GetElement("mapElem") as GuiElementMap)?.CurrentBlockViewBounds;
			guiComposer.Dispose();
		}
		tabs = new List<GuiTab>();
		int i;
		for (i = 0; i < tabnames.Count; i++)
		{
			tabs.Add(new GuiTab
			{
				Name = Lang.Get("maplayer-" + tabnames[i]),
				DataInt = i,
				Active = (MapLayers?.FirstOrDefault((MapLayer layer) => layer.LayerGroupCode == tabnames[i])?.Active ?? true)
			});
		}
		ElementBounds bounds2 = ElementBounds.Fixed(-200.0, 45.0, 200.0, 545.0);
		List<MapLayer> mapLayers = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers;
		guiComposer = capi.Gui.CreateCompo("worldmap" + dlgType, bounds).AddShadedDialogBG(elementBounds3, withTitleBar: false).AddIf(dlgType == EnumDialogType.Dialog)
			.AddDialogTitleBar(Lang.Get("World Map"), OnTitleBarClose)
			.AddInset(elementBounds, 2)
			.EndIf()
			.BeginChildElements(elementBounds3)
			.AddHoverText("", CairoFont.WhiteDetailText(), 350, elementBounds.FlatCopy(), "hoverText")
			.AddIf(dlgType == EnumDialogType.Dialog)
			.AddVerticalToggleTabs(tabs.ToArray(), bounds2, OnTabClicked, "verticalTabs")
			.EndIf()
			.AddInteractiveElement(new GuiElementMap(mapLayers, capi, this, elementBounds, dlgType == EnumDialogType.HUD), "mapElem")
			.EndChildElements()
			.Compose();
		tabs[0].Active = MapLayers?.FirstOrDefault((MapLayer layer) => layer.LayerGroupCode == tabnames[0])?.Active ?? true;
		guiComposer.OnComposed += OnRecomposed;
		GuiElementMap guiElementMap = guiComposer.GetElement("mapElem") as GuiElementMap;
		if (cuboidd != null)
		{
			guiElementMap.chunkViewBoundsBefore = cuboidd.ToCuboidi().Div(32);
		}
		guiElementMap.viewChanged = viewChanged;
		guiElementMap.viewChangedSync = viewChangedSync;
		guiElementMap.ZoomAdd(1f, 0.5f, 0.5f);
		guiComposer.GetHoverText("hoverText").SetAutoWidth(on: true);
		if (listenerId == 0L)
		{
			listenerId = capi.Event.RegisterGameTickListener(delegate
			{
				if (IsOpened())
				{
					(base.SingleComposer.GetElement("mapElem") as GuiElementMap)?.EnsureMapFullyLoaded();
					if (requireRecompose)
					{
						EnumDialogType asType = dialogType;
						capi.ModLoader.GetModSystem<WorldMapManager>().ToggleMap(asType);
						capi.ModLoader.GetModSystem<WorldMapManager>().ToggleMap(asType);
						requireRecompose = false;
					}
				}
			}, 100);
		}
		if (dlgType == EnumDialogType.Dialog)
		{
			foreach (MapLayer item in mapLayers)
			{
				item.ComposeDialogExtras(this, guiComposer);
			}
		}
		capi.World.FrameProfiler.Mark("composeworldmap");
		updateMaplayerExtrasState();
		return guiComposer;
	}

	private void OnTabClicked(int arg1, GuiTab tab)
	{
		string text = tabnames[arg1];
		foreach (MapLayer mapLayer in MapLayers)
		{
			if (mapLayer.LayerGroupCode == text)
			{
				mapLayer.Active = tab.Active;
			}
		}
		updateMaplayerExtrasState();
	}

	private void updateMaplayerExtrasState()
	{
		if (tabs == null)
		{
			return;
		}
		for (int i = 0; i < tabs.Count; i++)
		{
			string text = tabnames[i];
			GuiTab guiTab = tabs[i];
			if (Composers["worldmap-layer-" + text] != null)
			{
				Composers["worldmap-layer-" + text].Enabled = guiTab.Active && dialogType == EnumDialogType.Dialog;
			}
		}
	}

	private void OnRecomposed()
	{
		requireRecompose = true;
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		updateMaplayerExtrasState();
		if (dialogType == EnumDialogType.HUD)
		{
			base.SingleComposer = hudDialog;
			base.SingleComposer.Bounds.Alignment = GetMinimapPosition(out var offsetX, out var offsetY);
			base.SingleComposer.Bounds.fixedOffsetX = offsetX;
			base.SingleComposer.Bounds.fixedOffsetY = offsetY;
			base.SingleComposer.ReCompose();
		}
		else
		{
			base.SingleComposer = ComposeDialog(EnumDialogType.Dialog);
		}
		if (base.SingleComposer.GetElement("mapElem") is GuiElementMap guiElementMap)
		{
			guiElementMap.chunkViewBoundsBefore = new Cuboidi();
		}
		OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override bool TryClose()
	{
		if (DialogType == EnumDialogType.Dialog && capi.Settings.Bool["showMinimapHud"])
		{
			Open(EnumDialogType.HUD);
			return false;
		}
		return base.TryClose();
	}

	public void Open(EnumDialogType type)
	{
		dialogType = type;
		opened = false;
		TryOpen();
	}

	public override void OnGuiClosed()
	{
		updateMaplayerExtrasState();
		base.OnGuiClosed();
	}

	public override void Dispose()
	{
		base.Dispose();
		capi.Event.UnregisterGameTickListener(listenerId);
		listenerId = 0L;
		fullDialog.Dispose();
		hudDialog.Dispose();
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		if (base.SingleComposer == null || !base.SingleComposer.Bounds.PointInside(args.X, args.Y))
		{
			return;
		}
		loadWorldPos(args.X, args.Y, ref hoveredWorldPos);
		double y = hoveredWorldPos.Y;
		hoveredWorldPos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		hoveredWorldPos.Y = y;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"{(int)hoveredWorldPos.X}, {(int)hoveredWorldPos.Y}, {(int)hoveredWorldPos.Z}");
		GuiElementMap guiElementMap = base.SingleComposer.GetElement("mapElem") as GuiElementMap;
		GuiElementHoverText hoverText = base.SingleComposer.GetHoverText("hoverText");
		foreach (MapLayer mapLayer in guiElementMap.mapLayers)
		{
			mapLayer.OnMouseMoveClient(args, guiElementMap, stringBuilder);
		}
		string newText = stringBuilder.ToString().TrimEnd();
		hoverText.SetNewText(newText);
	}

	private void loadWorldPos(double mouseX, double mouseY, ref Vec3d worldPos)
	{
		double num = mouseX - base.SingleComposer.Bounds.absX;
		double num2 = mouseY - base.SingleComposer.Bounds.absY - ((dialogType == EnumDialogType.Dialog) ? GuiElement.scaled(30.0) : 0.0);
		(base.SingleComposer.GetElement("mapElem") as GuiElementMap).TranslateViewPosToWorldPos(new Vec2f((float)num, (float)num2), ref worldPos);
		worldPos.Y += 1.0;
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
		capi.Render.CheckGlError("map-rend2d");
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		capi.Render.CheckGlError("map-fina");
		bool flag = base.SingleComposer.Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && Focused;
		GuiElementHoverText hoverText = base.SingleComposer.GetHoverText("hoverText");
		hoverText.SetVisible(flag);
		hoverText.SetAutoDisplay(flag);
	}

	public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
	{
		(base.SingleComposer.GetElement("mapElem") as GuiElementMap).TranslateWorldPosToViewPos(worldPos, ref viewPos);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (!base.SingleComposer.Bounds.PointInside(args.X, args.Y))
		{
			base.OnMouseUp(args);
			return;
		}
		GuiElementMap guiElementMap = base.SingleComposer.GetElement("mapElem") as GuiElementMap;
		foreach (MapLayer mapLayer in guiElementMap.mapLayers)
		{
			mapLayer.OnMouseUpClient(args, guiElementMap);
			if (args.Handled)
			{
				return;
			}
		}
		if (args.Button == EnumMouseButton.Right)
		{
			Vec3d worldPos = new Vec3d();
			loadWorldPos(args.X, args.Y, ref worldPos);
			if (addWpDlg != null)
			{
				addWpDlg.TryClose();
				addWpDlg.Dispose();
			}
			WaypointMapLayer wml = MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer;
			addWpDlg = new GuiDialogAddWayPoint(capi, wml);
			addWpDlg.WorldPos = worldPos;
			addWpDlg.TryOpen();
			addWpDlg.OnClosed += delegate
			{
				capi.Gui.RequestFocus(this);
			};
		}
		base.OnMouseUp(args);
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		if (base.ShouldReceiveKeyboardEvents())
		{
			return dialogType == EnumDialogType.Dialog;
		}
		return false;
	}

	private EnumDialogArea GetMinimapPosition(out double offsetX, out double offsetY)
	{
		offsetX = GuiStyle.DialogToScreenPadding;
		offsetY = GuiStyle.DialogToScreenPadding;
		EnumDialogArea result;
		switch (capi.Settings.Int["minimapHudPosition"])
		{
		case 1:
			result = EnumDialogArea.LeftTop;
			break;
		case 2:
			result = EnumDialogArea.LeftBottom;
			offsetY = 0.0 - offsetY;
			break;
		case 3:
			result = EnumDialogArea.RightBottom;
			offsetX = 0.0 - offsetX;
			offsetY = 0.0 - offsetY;
			break;
		default:
			result = EnumDialogArea.RightTop;
			offsetX = 0.0 - offsetX;
			break;
		}
		return result;
	}
}
