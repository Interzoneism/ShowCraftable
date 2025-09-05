namespace Vintagestory.API.Server;

public class Privilege
{
	public static string buildblocks = "build";

	public static string useblock = "useblock";

	public static string buildblockseverywhere = "buildblockseverywhere";

	public static string useblockseverywhere = "useblockseverywhere";

	public static string attackplayers = "attackplayers";

	public static string attackcreatures = "attackcreatures";

	public static string freemove = "freemove";

	public static string gamemode = "gamemode";

	public static string pickingrange = "pickingrange";

	public static string chat = "chat";

	public static string selfkill = "selfkill";

	public static string kick = "kick";

	public static string ban = "ban";

	public static string whitelist = "whitelist";

	public static string setwelcome = "setwelcome";

	public static string announce = "announce";

	public static string readlists = "readlists";

	public static string give = "give";

	public static string claimland = "areamodify";

	public static string setspawn = "setspawn";

	public static string controlserver = "controlserver";

	public static string tp = "tp";

	public static string time = "time";

	public static string grantrevoke = "grantrevoke";

	public static string root = "root";

	public static string commandplayer = "commandplayer";

	public static string controlplayergroups = "controlplayergroups";

	public static string manageplayergroups = "manageplayergroups";

	public static string[] AllCodes()
	{
		return new string[28]
		{
			buildblocks, useblock, buildblockseverywhere, useblockseverywhere, attackplayers, attackcreatures, freemove, gamemode, pickingrange, chat,
			kick, ban, whitelist, setwelcome, announce, readlists, give, claimland, setspawn, controlserver,
			tp, time, grantrevoke, root, commandplayer, controlplayergroups, manageplayergroups, selfkill
		};
	}
}
