using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogSpawner : GuiDialogGeneric
{
	public BESpawnerData spawnerData = new BESpawnerData();

	private BlockPos blockEntityPos;

	private bool updating;

	private List<string> codes = new List<string>();

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogSpawner(BlockPos blockEntityPos, ICoreClientAPI capi)
		: base("Spawner config", capi)
	{
		this.blockEntityPos = blockEntityPos;
	}

	public override void OnGuiOpened()
	{
		Compose();
	}

	private void Compose()
	{
		ClearComposers();
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, 400.0, 25.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 400.0, 28.0).FixedUnder(elementBounds);
		ElementBounds.Fixed(0.0, 30.0, 400.0, 25.0).FixedUnder(elementBounds2);
		ElementBounds elementBounds3 = ElementBounds.FixedSize(0.0, 0.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(20.0, 4.0);
		ElementBounds elementBounds4 = ElementBounds.FixedSize(0.0, 0.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(20.0, 4.0);
		ElementBounds elementBounds5 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds5.BothSizing = ElementSizing.FitToChildren;
		elementBounds5.WithChildren(elementBounds2, elementBounds, elementBounds3, elementBounds4);
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-20.0, 0.0);
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (EntityProperties item in from type in (from item in capi.World.SearchItems(new AssetLocation("*", "creature*"))
				select new AssetLocation(item.Code.Domain, item.CodeEndWithoutParts(1)) into location
				select capi.World.GetEntityType(location)).OfType<EntityProperties>()
			orderby Lang.Get("item-creature-" + type.Code.Path)
			orderby type.Code.FirstCodePart()
			select type)
		{
			if (!item.Code.Path.Contains("butterfly"))
			{
				list.Add(item.Code.ToString());
				list2.Add(Lang.Get("item-creature-" + item.Code.Path));
			}
		}
		if (list2.Count != 0)
		{
			ElementBounds elementBounds6;
			ElementBounds elementBounds7;
			GuiComposer composer = capi.Gui.CreateCompo("spawnwerconfig", bounds).AddShadedDialogBG(elementBounds5).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
				.AddStaticText("Entities to spawn", CairoFont.WhiteDetailText(), elementBounds)
				.AddMultiSelectDropDown(list.ToArray(), list2.ToArray(), 0, null, elementBounds2, "entityCode")
				.AddStaticText("Spawn area dimensions", CairoFont.WhiteDetailText(), elementBounds6 = elementBounds2.BelowCopy(0.0, 10.0))
				.AddStaticText("X1", CairoFont.WhiteDetailText(), elementBounds6 = elementBounds6.BelowCopy(0.0, 7.0).WithFixedSize(20.0, 29.0))
				.AddNumberInput(elementBounds6 = elementBounds6.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "x1")
				.AddStaticText("Y1", CairoFont.WhiteDetailText(), elementBounds6 = elementBounds6.RightCopy(10.0, 7.0).WithFixedSize(20.0, 29.0))
				.AddNumberInput(elementBounds6 = elementBounds6.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "y1")
				.AddStaticText("Z1", CairoFont.WhiteDetailText(), elementBounds6 = elementBounds6.RightCopy(10.0, 7.0).WithFixedSize(20.0, 29.0))
				.AddNumberInput(elementBounds6 = elementBounds6.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "z1")
				.AddStaticText("X2", CairoFont.WhiteDetailText(), elementBounds7 = elementBounds2.FlatCopy().WithFixedSize(20.0, 29.0).FixedUnder(elementBounds6, -40.0))
				.AddNumberInput(elementBounds7 = elementBounds7.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "x2")
				.AddStaticText("Y2", CairoFont.WhiteDetailText(), elementBounds7 = elementBounds7.RightCopy(10.0, 7.0).WithFixedSize(20.0, 29.0))
				.AddNumberInput(elementBounds7 = elementBounds7.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "y2")
				.AddStaticText("Z2", CairoFont.WhiteDetailText(), elementBounds7 = elementBounds7.RightCopy(10.0, 7.0).WithFixedSize(20.0, 29.0))
				.AddNumberInput(elementBounds7 = elementBounds7.RightCopy(5.0, -7.0).WithFixedSize(60.0, 29.0), OnDimensionsChanged, CairoFont.WhiteDetailText(), "z2");
			CairoFont font = CairoFont.WhiteDetailText();
			ElementBounds elementBounds8 = elementBounds2.FlatCopy().WithFixedSize(400.0, 30.0).FixedUnder(elementBounds7, -40.0);
			ElementBounds elementBounds9;
			base.SingleComposer = Vintagestory.API.Client.GuiComposerHelpers.AddDropDown(bounds: elementBounds9 = elementBounds8.BelowCopy(0.0, -10.0).WithFixedSize(300.0, 29.0), composer: composer.AddStaticText("Player range mode", font, elementBounds8), values: new string[4] { "0", "1", "2", "3" }, names: new string[4] { "Ignore player", "Spawn only when player is within minum range", "Spawn only when player is outside maximum range", "Spawn only when player is outside minimum range but within maximum range" }, selectedIndex: 0, onSelectionChanged: null, font: CairoFont.WhiteDetailText(), key: "playerRangeMode").AddStaticText("Minimum player range", CairoFont.WhiteDetailText(), elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0).WithFixedSize(200.0, 30.0)).AddStaticText("Maximum player range", CairoFont.WhiteDetailText(), elementBounds9.RightCopy(20.0).WithFixedSize(200.0, 30.0))
				.AddNumberInput(elementBounds9 = elementBounds9.BelowCopy(0.0, -10.0).WithFixedSize(100.0, 29.0), null, CairoFont.WhiteDetailText(), "minPlayerRange")
				.AddNumberInput(elementBounds9.RightCopy(120.0).WithFixedSize(100.0, 29.0), null, CairoFont.WhiteDetailText(), "maxPlayerRange")
				.AddStaticText("Spawn Interval (ingame hours)", CairoFont.WhiteDetailText(), elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0).WithFixedSize(400.0, 30.0))
				.AddNumberInput(elementBounds9 = elementBounds9.BelowCopy(0.0, -10.0).WithFixedSize(100.0, 29.0), null, CairoFont.WhiteDetailText(), "interval")
				.AddStaticText("Max concurrent entities to spawn", CairoFont.WhiteDetailText(), elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0).WithFixedSize(400.0, 30.0))
				.AddNumberInput(elementBounds9 = elementBounds9.BelowCopy(0.0, -10.0).WithFixedSize(100.0, 29.0), null, CairoFont.WhiteDetailText(), "maxentities")
				.AddStaticText("Spawn 'x' entities, then remove block (0 for infinite)", CairoFont.WhiteDetailText(), elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0).WithFixedSize(400.0, 30.0))
				.AddNumberInput(elementBounds9 = elementBounds9.BelowCopy(0.0, -10.0).WithFixedSize(100.0, 29.0), null, CairoFont.WhiteDetailText(), "spawncount")
				.AddSwitch(null, elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0), "primerSwitch", 20.0)
				.AddStaticText("Begin spawning only after being imported", CairoFont.WhiteDetailText(), elementBounds9.RightCopy(10.0).WithFixedSize(400.0, 30.0))
				.AddSwitch(null, elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0), "rechargeMode", 20.0)
				.AddStaticText("Slowly recharge before spawning more entities", CairoFont.WhiteDetailText(), elementBounds9.RightCopy(10.0).WithFixedSize(400.0, 30.0))
				.AddStaticText("Max charge", CairoFont.WhiteDetailText(), elementBounds9 = elementBounds9.BelowCopy(0.0, 10.0).WithFixedSize(100.0, 30.0))
				.AddStaticText("Recharge rate per hour", CairoFont.WhiteDetailText(), elementBounds9.RightCopy(75.0).WithFixedSize(200.0, 30.0))
				.AddNumberInput(elementBounds9 = elementBounds9.BelowCopy(0.0, -10.0).WithFixedSize(75.0, 29.0), null, CairoFont.WhiteDetailText(), "chargeCapacity")
				.AddNumberInput(elementBounds9.RightCopy(100.0).WithFixedSize(75.0, 29.0), null, CairoFont.WhiteDetailText(), "rechargePerHour")
				.AddSmallButton("Close", OnButtonClose, elementBounds3.FixedUnder(elementBounds9, 20.0))
				.AddSmallButton("Save", OnButtonSave, elementBounds4.FixedUnder(elementBounds9, 20.0))
				.Compose();
			UpdateFromServer(spawnerData);
		}
	}

	private void OnDimensionsChanged(string val)
	{
		if (!updating)
		{
			spawnerData.SpawnArea = ParseDimensions();
			capi.World.HighlightBlocks(capi.World.Player, 2, new List<BlockPos>
			{
				spawnerData.SpawnArea.Start.AsBlockPos.Add(blockEntityPos),
				spawnerData.SpawnArea.End.AsBlockPos.Add(blockEntityPos)
			}, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
		}
	}

	private Cuboidi ParseDimensions()
	{
		float value = base.SingleComposer.GetNumberInput("x1").GetValue();
		float value2 = base.SingleComposer.GetNumberInput("y1").GetValue();
		float value3 = base.SingleComposer.GetNumberInput("z1").GetValue();
		float value4 = base.SingleComposer.GetNumberInput("x2").GetValue();
		float value5 = base.SingleComposer.GetNumberInput("y2").GetValue();
		float value6 = base.SingleComposer.GetNumberInput("z2").GetValue();
		return new Cuboidi((int)value, (int)value2, (int)value3, (int)value4, (int)value5, (int)value6);
	}

	public void UpdateFromServer(BESpawnerData data)
	{
		updating = true;
		spawnerData = data;
		base.SingleComposer.GetNumberInput("x1").SetValue(data.SpawnArea.X1);
		base.SingleComposer.GetNumberInput("y1").SetValue(data.SpawnArea.Y1);
		base.SingleComposer.GetNumberInput("z1").SetValue(data.SpawnArea.Z1);
		base.SingleComposer.GetNumberInput("x2").SetValue(data.SpawnArea.X2);
		base.SingleComposer.GetNumberInput("y2").SetValue(data.SpawnArea.Y2);
		base.SingleComposer.GetNumberInput("z2").SetValue(data.SpawnArea.Z2);
		base.SingleComposer.GetNumberInput("maxentities").SetValue(data.MaxCount);
		base.SingleComposer.GetNumberInput("spawncount").SetValue(data.RemoveAfterSpawnCount);
		base.SingleComposer.GetNumberInput("minPlayerRange").SetValue(data.MinPlayerRange);
		base.SingleComposer.GetNumberInput("maxPlayerRange").SetValue(data.MaxPlayerRange);
		base.SingleComposer.GetSwitch("primerSwitch").SetValue(data.SpawnOnlyAfterImport);
		base.SingleComposer.GetNumberInput("interval").SetValue(data.InGameHourInterval);
		base.SingleComposer.GetDropDown("entityCode").SetSelectedValue(data.EntityCodes);
		base.SingleComposer.GetSwitch("rechargeMode").On = data.InternalCapacity > 0;
		base.SingleComposer.GetNumberInput("chargeCapacity").SetValue(data.InternalCapacity);
		base.SingleComposer.GetNumberInput("rechargePerHour").SetValue((float)data.RechargePerHour);
		base.SingleComposer.GetDropDown("playerRangeMode").SetSelectedIndex((int)data.SpawnRangeMode);
		capi.World.HighlightBlocks(capi.World.Player, 2, new List<BlockPos>
		{
			spawnerData.SpawnArea.Start.AsBlockPos.Add(blockEntityPos),
			spawnerData.SpawnArea.End.AsBlockPos.Add(blockEntityPos)
		}, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
		updating = false;
	}

	private void OnTitleBarClose()
	{
		OnButtonClose();
	}

	private bool OnButtonClose()
	{
		TryClose();
		return true;
	}

	private bool OnButtonSave()
	{
		spawnerData.SpawnArea.X1 = (int)base.SingleComposer.GetNumberInput("x1").GetValue();
		spawnerData.SpawnArea.Y1 = (int)base.SingleComposer.GetNumberInput("y1").GetValue();
		spawnerData.SpawnArea.Z1 = (int)base.SingleComposer.GetNumberInput("z1").GetValue();
		spawnerData.SpawnArea.X2 = (int)base.SingleComposer.GetNumberInput("x2").GetValue();
		spawnerData.SpawnArea.Y2 = (int)base.SingleComposer.GetNumberInput("y2").GetValue();
		spawnerData.SpawnArea.Z2 = (int)base.SingleComposer.GetNumberInput("z2").GetValue();
		spawnerData.MinPlayerRange = (int)base.SingleComposer.GetNumberInput("minPlayerRange").GetValue();
		spawnerData.MaxPlayerRange = (int)base.SingleComposer.GetNumberInput("maxPlayerRange").GetValue();
		spawnerData.MaxCount = (int)base.SingleComposer.GetNumberInput("maxentities").GetValue();
		spawnerData.RemoveAfterSpawnCount = (int)base.SingleComposer.GetNumberInput("spawncount").GetValue();
		spawnerData.SpawnOnlyAfterImport = base.SingleComposer.GetSwitch("primerSwitch").On;
		spawnerData.InGameHourInterval = base.SingleComposer.GetNumberInput("interval").GetValue();
		bool flag = base.SingleComposer.GetSwitch("rechargeMode").On;
		spawnerData.InternalCapacity = (flag ? ((int)base.SingleComposer.GetNumberInput("chargeCapacity").GetValue()) : 0);
		spawnerData.RechargePerHour = base.SingleComposer.GetNumberInput("rechargePerHour").GetValue();
		spawnerData.SpawnRangeMode = (EnumSpawnRangeMode)base.SingleComposer.GetDropDown("playerRangeMode").SelectedValue.ToInt();
		spawnerData.EntityCodes = base.SingleComposer.GetDropDown("entityCode").SelectedValues;
		byte[] data = SerializerUtil.Serialize(spawnerData);
		capi.Network.SendBlockEntityPacket(blockEntityPos, 1001, data);
		return true;
	}

	public override bool CaptureAllInputs()
	{
		return false;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		capi.World.HighlightBlocks(capi.World.Player, 2, new List<BlockPos>());
	}
}
