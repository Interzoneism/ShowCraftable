using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PlayerMapLayer : MarkerMapLayer
{
	private Dictionary<IPlayer, EntityMapComponent> MapComps = new Dictionary<IPlayer, EntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture ownTexture;

	private LoadedTexture otherTexture;

	public override string Title => "Players";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "terrain";

	public PlayerMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	private void Event_PlayerDespawn(IClientPlayer byPlayer)
	{
		if (MapComps.TryGetValue(byPlayer, out var value))
		{
			value.Dispose();
			MapComps.Remove(byPlayer);
		}
	}

	private void Event_PlayerSpawn(IClientPlayer byPlayer)
	{
		if ((!capi.World.Config.GetBool("mapHideOtherPlayers") || !(byPlayer.PlayerUID != capi.World.Player.PlayerUID)) && mapSink.IsOpened && !MapComps.ContainsKey(byPlayer))
		{
			EntityMapComponent value = new EntityMapComponent(capi, otherTexture, byPlayer.Entity);
			MapComps[byPlayer] = value;
		}
	}

	public override void OnLoaded()
	{
		if (capi != null)
		{
			capi.Event.PlayerEntitySpawn += Event_PlayerSpawn;
			capi.Event.PlayerEntityDespawn += Event_PlayerDespawn;
		}
	}

	public override void OnMapOpenedClient()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		int num = (int)GuiElement.scaled(32.0);
		if (ownTexture == null)
		{
			ImageSurface val = new ImageSurface((Format)0, num, num);
			Context val2 = new Context((Surface)(object)val);
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			val2.Paint();
			capi.Gui.Icons.DrawMapPlayer(val2, 0, 0, num, num, new double[4] { 0.0, 0.0, 0.0, 1.0 }, new double[4] { 1.0, 1.0, 1.0, 1.0 });
			ownTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(val, linearMag: false), num / 2, num / 2);
			val2.Dispose();
			((Surface)val).Dispose();
		}
		if (otherTexture == null)
		{
			ImageSurface val3 = new ImageSurface((Format)0, num, num);
			Context val4 = new Context((Surface)(object)val3);
			val4.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			val4.Paint();
			capi.Gui.Icons.DrawMapPlayer(val4, 0, 0, num, num, new double[4] { 0.3, 0.3, 0.3, 1.0 }, new double[4] { 0.7, 0.7, 0.7, 1.0 });
			otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(val3, linearMag: false), num / 2, num / 2);
			val4.Dispose();
			((Surface)val3).Dispose();
		}
		IPlayer[] allOnlinePlayers = capi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			if (MapComps.TryGetValue(player, out var value))
			{
				value?.Dispose();
				MapComps.Remove(player);
			}
			if (player.Entity == null)
			{
				capi.World.Logger.Warning("Can't add player {0} to world map, missing entity :<", player.PlayerUID);
			}
			else if (!capi.World.Config.GetBool("mapHideOtherPlayers") || !(player.PlayerUID != capi.World.Player.PlayerUID))
			{
				value = new EntityMapComponent(capi, (player == capi.World.Player) ? ownTexture : otherTexture, player.Entity);
				MapComps[player] = value;
			}
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void OnMapClosedClient()
	{
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		ownTexture?.Dispose();
		ownTexture = null;
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
