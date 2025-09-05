using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class OwnedEntityMapLayer : MarkerMapLayer
{
	private Dictionary<long, OwnedEntityMapComponent> MapComps = new Dictionary<long, OwnedEntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture otherTexture;

	public override string Title => "Owned Creatures";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "ownedcreatures";

	public OwnedEntityMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	public void Reload()
	{
		Dispose();
		OnMapOpenedClient();
	}

	public override void OnMapOpenedClient()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		int num = (int)GuiElement.scaled(32.0);
		if (otherTexture == null)
		{
			ImageSurface val = new ImageSurface((Format)0, num, num);
			Context val2 = new Context((Surface)(object)val);
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			val2.Paint();
			capi.Gui.Icons.DrawMapPlayer(val2, 0, 0, num, num, new double[4] { 0.3, 0.3, 0.3, 1.0 }, new double[4] { 0.95, 0.95, 0.95, 1.0 });
			otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(val, linearMag: false), num / 2, num / 2);
			val2.Dispose();
			((Surface)val).Dispose();
		}
		ModSystemEntityOwnership modSystem = capi.ModLoader.GetModSystem<ModSystemEntityOwnership>();
		foreach (OwnedEntityMapComponent value in MapComps.Values)
		{
			value?.Dispose();
		}
		MapComps.Clear();
		foreach (KeyValuePair<string, EntityOwnership> selfOwnerShip in modSystem.SelfOwnerShips)
		{
			MapComps[selfOwnerShip.Value.EntityId] = new OwnedEntityMapComponent(capi, otherTexture, selfOwnerShip.Value, selfOwnerShip.Value.Color);
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.Render(mapElem, dt);
		}
	}

	public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseMove(args, mapElem, hoverText);
		}
	}

	public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
