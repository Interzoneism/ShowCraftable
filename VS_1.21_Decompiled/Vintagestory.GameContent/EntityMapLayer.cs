using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityMapLayer : MarkerMapLayer
{
	private Dictionary<long, EntityMapComponent> MapComps = new Dictionary<long, EntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture otherTexture;

	public override string Title => "Creatures";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "creatures";

	public EntityMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	public override void OnLoaded()
	{
		if (capi != null)
		{
			capi.Event.OnEntitySpawn += Event_OnEntitySpawn;
			capi.Event.OnEntityLoaded += Event_OnEntitySpawn;
			capi.Event.OnEntityDespawn += Event_OnEntityDespawn;
		}
	}

	private void Event_OnEntityDespawn(Entity entity, EntityDespawnData reasonData)
	{
		if (MapComps.TryGetValue(entity.EntityId, out var value))
		{
			value.Dispose();
			MapComps.Remove(entity.EntityId);
		}
	}

	private void Event_OnEntitySpawn(Entity entity)
	{
		if (!(entity is EntityPlayer) && !entity.Code.Path.Contains("drifter") && mapSink.IsOpened && !MapComps.ContainsKey(entity.EntityId))
		{
			EntityMapComponent value = new EntityMapComponent(capi, otherTexture, entity, entity.Properties.Color);
			MapComps[entity.EntityId] = value;
		}
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
		foreach (KeyValuePair<long, Entity> loadedEntity in capi.World.LoadedEntities)
		{
			if (!(loadedEntity.Value is EntityPlayer))
			{
				if (MapComps.TryGetValue(loadedEntity.Value.EntityId, out var value))
				{
					value?.Dispose();
					MapComps.Remove(loadedEntity.Value.EntityId);
				}
				value = new EntityMapComponent(capi, otherTexture, loadedEntity.Value, loadedEntity.Value.Properties.Color);
				MapComps[loadedEntity.Value.EntityId] = value;
			}
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
