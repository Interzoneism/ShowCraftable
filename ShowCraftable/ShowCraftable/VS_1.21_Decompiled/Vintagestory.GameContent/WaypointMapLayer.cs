using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WaypointMapLayer : MarkerMapLayer
{
	public List<Waypoint> Waypoints = new List<Waypoint>();

	private ICoreServerAPI sapi;

	public List<Waypoint> ownWaypoints = new List<Waypoint>();

	private List<MapComponent> wayPointComponents = new List<MapComponent>();

	public MeshRef quadModel;

	private List<MapComponent> tmpWayPointComponents = new List<MapComponent>();

	public Dictionary<string, LoadedTexture> texturesByIcon;

	private static string[] hexcolors = new string[36]
	{
		"#F9D0DC", "#F179AF", "#F15A4A", "#ED272A", "#A30A35", "#FFDE98", "#EFFD5F", "#F6EA5E", "#FDBB3A", "#C8772E",
		"#F47832", "C3D941", "#9FAB3A", "#94C948", "#47B749", "#366E4F", "#516D66", "93D7E3", "#7698CF", "#20909E",
		"#14A4DD", "#204EA2", "#28417A", "#C395C4", "#92479B", "#8E007E", "#5E3896", "D9D4CE", "#AFAAA8", "#706D64",
		"#4F4C2B", "#BF9C86", "#9885530", "#5D3D21", "#FFFFFF", "#080504"
	};

	public override bool RequireChunkLoaded => false;

	public OrderedDictionary<string, CreateIconTextureDelegate> WaypointIcons { get; set; } = new OrderedDictionary<string, CreateIconTextureDelegate>();

	public List<int> WaypointColors { get; set; } = new List<int>();

	public override string Title => "Player Set Markers";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

	public override string LayerGroupCode => "waypoints";

	public WaypointMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
		WaypointColors = new List<int>();
		for (int i = 0; i < hexcolors.Length; i++)
		{
			WaypointColors.Add(ColorUtil.Hex2Int(hexcolors[i]));
		}
		List<IAsset> many = api.Assets.GetMany("textures/icons/worldmap/", null, loadAsset: false);
		ICoreClientAPI capi = api as ICoreClientAPI;
		foreach (IAsset icon in many)
		{
			string input = icon.Name.Substring(0, icon.Name.IndexOf('.'));
			input = Regex.Replace(input, "\\d+\\-", "");
			if (api.Side == EnumAppSide.Server)
			{
				WaypointIcons[input] = () => (LoadedTexture)null;
				continue;
			}
			WaypointIcons[input] = delegate
			{
				int num = (int)Math.Ceiling(20f * RuntimeEnv.GUIScale);
				return capi.Gui.LoadSvg(icon.Location, num, num, num, num, -1);
			};
			capi.Gui.Icons.CustomIcons["wp" + input.UcFirst()] = delegate(Context ctx, int x, int y, float w, float h, double[] rgba)
			{
				int value = ColorUtil.ColorFromRgba(rgba);
				IGuiAPI gui = capi.Gui;
				IAsset svgAsset = icon;
				Surface target = ctx.GetTarget();
				gui.DrawSvg(svgAsset, (ImageSurface)(object)((target is ImageSurface) ? target : null), ctx.Matrix, x, y, (int)w, (int)h, value);
			};
		}
		if (api.Side == EnumAppSide.Server)
		{
			ICoreServerAPI coreServerAPI = (sapi = api as ICoreServerAPI);
			coreServerAPI.Event.GameWorldSave += OnSaveGameGettingSaved;
			coreServerAPI.Event.PlayerDeath += Event_PlayerDeath;
			CommandArgumentParsers parsers = coreServerAPI.ChatCommands.Parsers;
			coreServerAPI.ChatCommands.Create("waypoint").WithDescription("Put a waypoint at this location which will be visible for you on the map").RequiresPrivilege(Privilege.chat)
				.BeginSubCommand("deathwp")
				.WithDescription("Enable/Disable automatic adding of a death waypoint")
				.WithArgs(parsers.OptionalBool("enabled"))
				.RequiresPlayer()
				.HandleWith(OnCmdWayPointDeathWp)
				.EndSubCommand()
				.BeginSubCommand("add")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAdd)
				.EndSubCommand()
				.BeginSubCommand("addp")
				.RequiresPlayer()
				.WithDescription("Add a waypoint to the map")
				.WithArgs(parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddp)
				.EndSubCommand()
				.BeginSubCommand("addat")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddat)
				.EndSubCommand()
				.BeginSubCommand("addati")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.Word("icon"), parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddati)
				.EndSubCommand()
				.BeginSubCommand("modify")
				.WithDescription("")
				.RequiresPlayer()
				.WithArgs(parsers.Int("waypoint_id"), parsers.Color("color"), parsers.Word("icon"), parsers.Bool("pinned"), parsers.All("title"))
				.HandleWith(OnCmdWayPointModify)
				.EndSubCommand()
				.BeginSubCommand("remove")
				.WithDescription("Remove a waypoint by its id. Get a lost of ids using /waypoint list")
				.RequiresPlayer()
				.WithArgs(parsers.Int("waypoint_id"))
				.HandleWith(OnCmdWayPointRemove)
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDescription("List your own waypoints")
				.RequiresPlayer()
				.WithArgs(parsers.OptionalWordRange("details", "details", "d"))
				.HandleWith(OnCmdWayPointList)
				.EndSubCommand();
			coreServerAPI.ChatCommands.Create("tpwp").WithDescription("Teleport yourself to a waypoint starting with the supplied name").RequiresPrivilege(Privilege.tp)
				.WithArgs(parsers.All("name"))
				.HandleWith(OnCmdTpTo);
		}
		else
		{
			quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
		}
	}

	private TextCommandResult OnCmdWayPointList(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		bool flag = args[0] as string == "details" || args[0] as string == "d";
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == args.Caller.Player.PlayerUID).ToArray();
		foreach (Waypoint waypoint in array)
		{
			Vec3d vec3d = waypoint.Position.Clone();
			vec3d.X -= api.World.DefaultSpawnPosition.X;
			vec3d.Z -= api.World.DefaultSpawnPosition.Z;
			if (flag)
			{
				stringBuilder.AppendLine($"{num}: {waypoint.Title} at {vec3d.AsBlockPos} {ColorUtil.Int2Hex(waypoint.Color)} {waypoint.Icon}");
			}
			else
			{
				stringBuilder.AppendLine($"{num}: {waypoint.Title} at {vec3d.AsBlockPos}");
			}
			num++;
		}
		if (stringBuilder.Length == 0)
		{
			return TextCommandResult.Success(Lang.Get("You have no waypoints"));
		}
		return TextCommandResult.Success(Lang.Get("Your waypoints:") + "\n" + stringBuilder.ToString());
	}

	private bool IsMapDisallowed(out TextCommandResult response)
	{
		if (!api.World.Config.GetBool("allowMap", defaultValue: true))
		{
			response = TextCommandResult.Success(Lang.Get("Maps are disabled on this server"));
			return true;
		}
		response = null;
		return false;
	}

	private TextCommandResult OnCmdWayPointRemove(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int num = (int)args.Parsers[0].GetValue();
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		if (array.Length == 0)
		{
			return TextCommandResult.Success(Lang.Get("You have no waypoints to delete"));
		}
		if (args.Parsers[0].IsMissing || num < 0 || num >= array.Length)
		{
			return TextCommandResult.Success(Lang.Get("Invalid waypoint number, valid ones are 0..{0}", array.Length - 1));
		}
		Waypoints.Remove(array[num]);
		RebuildMapComponents();
		ResendWaypoints(player);
		return TextCommandResult.Success(Lang.Get("Ok, deleted waypoint."));
	}

	private TextCommandResult OnCmdWayPointDeathWp(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		if (!api.World.Config.GetBool("allowDeathwaypointing", defaultValue: true))
		{
			return TextCommandResult.Success(Lang.Get("Death waypointing is disabled on this server"));
		}
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		if (args.Parsers[0].IsMissing)
		{
			bool modData = serverPlayer.GetModData("deathWaypointing", defaultValue: false);
			return TextCommandResult.Success(Lang.Get("Death waypoint is {0}", modData ? Lang.Get("on") : Lang.Get("off")));
		}
		bool flag = (bool)args.Parsers[0].GetValue();
		serverPlayer.SetModData("deathWaypointing", flag);
		return TextCommandResult.Success(Lang.Get("Death waypoint now {0}", flag ? Lang.Get("on") : Lang.Get("off")));
	}

	private void Event_PlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
	{
		if (!api.World.Config.GetBool("allowMap", defaultValue: true) || !api.World.Config.GetBool("allowDeathwaypointing", defaultValue: true) || !byPlayer.GetModData("deathWaypointing", defaultValue: true))
		{
			return;
		}
		string text = Lang.Get("You died here");
		for (int i = 0; i < Waypoints.Count; i++)
		{
			Waypoint waypoint = Waypoints[i];
			if (waypoint.OwningPlayerUid == byPlayer.PlayerUID && waypoint.Title == text)
			{
				Waypoints.RemoveAt(i);
				i--;
			}
		}
		Waypoint waypoint2 = new Waypoint
		{
			Color = ColorUtil.ColorFromRgba(200, 200, 200, 255),
			OwningPlayerUid = byPlayer.PlayerUID,
			Position = byPlayer.Entity.Pos.XYZ,
			Title = text,
			Icon = "gravestone",
			Pinned = true
		};
		AddWaypoint(waypoint2, byPlayer);
	}

	private TextCommandResult OnCmdTpTo(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		string value = (args.Parsers[0].GetValue() as string).ToLowerInvariant();
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		foreach (Waypoint waypoint in array)
		{
			if (waypoint.Title != null && waypoint.Title.StartsWith(value, StringComparison.InvariantCultureIgnoreCase))
			{
				player.Entity.TeleportTo(waypoint.Position);
				return TextCommandResult.Success(Lang.Get("Ok teleported you to waypoint {0}.", waypoint.Title));
			}
		}
		return TextCommandResult.Success(Lang.Get("No such waypoint found"));
	}

	private TextCommandResult OnCmdWayPointAdd(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		Color parsedColor = (Color)args.Parsers[0].GetValue();
		string title = args.Parsers[1].GetValue() as string;
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		return AddWp(serverPlayer, serverPlayer.Entity.Pos.XYZ, title, parsedColor, "circle", pinned: false);
	}

	private TextCommandResult OnCmdWayPointAddp(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		Color parsedColor = (Color)args.Parsers[0].GetValue();
		string title = args.Parsers[1].GetValue() as string;
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		return AddWp(serverPlayer, serverPlayer.Entity.Pos.XYZ, title, parsedColor, "circle", pinned: true);
	}

	private TextCommandResult OnCmdWayPointAddat(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		Vec3d pos = args.Parsers[0].GetValue() as Vec3d;
		bool pinned = (bool)args.Parsers[1].GetValue();
		Color parsedColor = (Color)args.Parsers[2].GetValue();
		string title = args.Parsers[3].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, pos, title, parsedColor, "circle", pinned);
	}

	private TextCommandResult OnCmdWayPointAddati(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		string icon = args.Parsers[0].GetValue() as string;
		Vec3d pos = args.Parsers[1].GetValue() as Vec3d;
		bool pinned = (bool)args.Parsers[2].GetValue();
		Color parsedColor = (Color)args.Parsers[3].GetValue();
		string title = args.Parsers[4].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, pos, title, parsedColor, icon, pinned);
	}

	private TextCommandResult OnCmdWayPointModify(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var response))
		{
			return response;
		}
		int num = (int)args.Parsers[0].GetValue();
		Color color = (Color)args.Parsers[1].GetValue();
		string text = args.Parsers[2].GetValue() as string;
		bool pinned = (bool)args.Parsers[3].GetValue();
		string text2 = args.Parsers[4].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		if (args.Parsers[0].IsMissing || num < 0 || num >= array.Length)
		{
			return TextCommandResult.Success(Lang.Get("command-modwaypoint-invalidindex", array.Length - 1));
		}
		if (string.IsNullOrEmpty(text2))
		{
			return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
		}
		array[num].Color = color.ToArgb() | -16777216;
		array[num].Title = text2;
		array[num].Pinned = pinned;
		if (text != null)
		{
			array[num].Icon = text;
		}
		ResendWaypoints(player);
		return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} modified", num));
	}

	private TextCommandResult AddWp(IServerPlayer player, Vec3d pos, string title, Color parsedColor, string icon, bool pinned)
	{
		if (string.IsNullOrEmpty(title))
		{
			return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
		}
		Waypoint waypoint = new Waypoint
		{
			Color = (parsedColor.ToArgb() | -16777216),
			OwningPlayerUid = player.PlayerUID,
			Position = pos,
			Title = title,
			Icon = icon,
			Pinned = pinned,
			Guid = Guid.NewGuid().ToString()
		};
		int num = AddWaypoint(waypoint, player);
		return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} added", num));
	}

	public int AddWaypoint(Waypoint waypoint, IServerPlayer player)
	{
		Waypoints.Add(waypoint);
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		ResendWaypoints(player);
		return array.Length - 1;
	}

	private void OnSaveGameGettingSaved()
	{
		sapi.WorldManager.SaveGame.StoreData("playerMapMarkers_v2", SerializerUtil.Serialize(Waypoints));
	}

	[Obsolete("Receiving the OnViewChangedPacket now calls: OnViewChangedServer(fromPlayer, int x1, int z1, int x2, int z2) but retained in 1.20.10 for backwards compatibility")]
	public override void OnViewChangedServer(IServerPlayer fromPlayer, List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
	{
		ResendWaypoints(fromPlayer);
	}

	public override void OnViewChangedServer(IServerPlayer fromPlayer, int x1, int z1, int x2, int z2)
	{
		OnViewChangedServer(fromPlayer, null, null);
	}

	public override void OnMapOpenedClient()
	{
		reloadIconTextures();
		ensureIconTexturesLoaded();
		RebuildMapComponents();
	}

	public void reloadIconTextures()
	{
		if (texturesByIcon != null)
		{
			foreach (KeyValuePair<string, LoadedTexture> item in texturesByIcon)
			{
				item.Value.Dispose();
			}
		}
		texturesByIcon = null;
		ensureIconTexturesLoaded();
	}

	protected void ensureIconTexturesLoaded()
	{
		if (texturesByIcon != null)
		{
			return;
		}
		texturesByIcon = new Dictionary<string, LoadedTexture>();
		foreach (KeyValuePair<string, CreateIconTextureDelegate> waypointIcon in WaypointIcons)
		{
			texturesByIcon[waypointIcon.Key] = waypointIcon.Value();
		}
	}

	public override void OnMapClosedClient()
	{
		foreach (MapComponent tmpWayPointComponent in tmpWayPointComponents)
		{
			wayPointComponents.Remove(tmpWayPointComponent);
		}
		tmpWayPointComponents.Clear();
	}

	public override void Dispose()
	{
		if (texturesByIcon != null)
		{
			foreach (KeyValuePair<string, LoadedTexture> item in texturesByIcon)
			{
				item.Value.Dispose();
			}
		}
		texturesByIcon = null;
		quadModel?.Dispose();
		base.Dispose();
	}

	public override void OnLoaded()
	{
		if (sapi == null)
		{
			return;
		}
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("playerMapMarkers_v2");
			if (data != null)
			{
				Waypoints = SerializerUtil.Deserialize<List<Waypoint>>(data);
				sapi.World.Logger.Notification("Successfully loaded " + Waypoints.Count + " waypoints");
			}
			else
			{
				data = sapi.WorldManager.SaveGame.GetData("playerMapMarkers");
				if (data != null)
				{
					Waypoints = JsonUtil.FromBytes<List<Waypoint>>(data);
				}
			}
			for (int i = 0; i < Waypoints.Count; i++)
			{
				Waypoint waypoint = Waypoints[i];
				if (waypoint == null)
				{
					sapi.World.Logger.Error("Waypoint with no information loaded, will remove");
					Waypoints.RemoveAt(i);
					i--;
				}
				if (waypoint.Title == null)
				{
					waypoint.Title = waypoint.Text;
				}
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed deserializing player map markers. Won't load them, sorry! Exception thrown:");
			sapi.World.Logger.Error(e);
		}
		foreach (Waypoint waypoint2 in Waypoints)
		{
			if (waypoint2.Guid == null)
			{
				waypoint2.Guid = Guid.NewGuid().ToString();
			}
		}
	}

	public override void OnDataFromServer(byte[] data)
	{
		ownWaypoints.Clear();
		ownWaypoints.AddRange(SerializerUtil.Deserialize<List<Waypoint>>(data));
		RebuildMapComponents();
	}

	public void AddTemporaryWaypoint(Waypoint waypoint)
	{
		WaypointMapComponent item = new WaypointMapComponent(ownWaypoints.Count, waypoint, this, api as ICoreClientAPI);
		wayPointComponents.Add(item);
		tmpWayPointComponents.Add(item);
	}

	private void RebuildMapComponents()
	{
		if (!mapSink.IsOpened)
		{
			return;
		}
		foreach (MapComponent tmpWayPointComponent in tmpWayPointComponents)
		{
			wayPointComponents.Remove(tmpWayPointComponent);
		}
		foreach (WaypointMapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.Dispose();
		}
		wayPointComponents.Clear();
		for (int i = 0; i < ownWaypoints.Count; i++)
		{
			WaypointMapComponent item = new WaypointMapComponent(i, ownWaypoints[i], this, api as ICoreClientAPI);
			wayPointComponents.Add(item);
		}
		wayPointComponents.AddRange(tmpWayPointComponents);
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.Render(mapElem, dt);
		}
	}

	public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.OnMouseMove(args, mapElem, hoverText);
		}
	}

	public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.OnMouseUpOnElement(args, mapElem);
			if (args.Handled)
			{
				break;
			}
		}
	}

	private void ResendWaypoints(IServerPlayer toPlayer)
	{
		Dictionary<int, PlayerGroupMembership> playerGroupMemberships = toPlayer.ServerData.PlayerGroupMemberships;
		List<Waypoint> list = new List<Waypoint>();
		foreach (Waypoint waypoint in Waypoints)
		{
			if (!(toPlayer.PlayerUID != waypoint.OwningPlayerUid) || playerGroupMemberships.ContainsKey(waypoint.OwningPlayerGroupId))
			{
				list.Add(waypoint);
			}
		}
		mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(list));
	}
}
