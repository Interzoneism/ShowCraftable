using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

public class ModCompatiblityUtil : ModSystem
{
	public static AssetCategory compatibility;

	public static string[] partiallyWorkingCategories = new string[2] { "shapes", "textures" };

	public static List<string> LoadedModIds { get; private set; } = new List<string>();

	public override double ExecuteOrder()
	{
		return 0.04;
	}

	public override void StartPre(ICoreAPI api)
	{
		compatibility = new AssetCategory("compatibility", AffectsGameplay: true, EnumAppSide.Universal);
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		LoadedModIds = api.ModLoader.Mods.Select((Mod m) => m.Info.ModID).ToList();
		RemapFromCompatbilityFolder(api);
	}

	private void RemapFromCompatbilityFolder(ICoreAPI api)
	{
		int num = 0;
		int num2 = 0;
		foreach (Mod mod in api.ModLoader.Mods)
		{
			string text = "compatibility/" + mod.Info.ModID + "/";
			foreach (IAsset item in api.Assets.GetManyInCategory("compatibility", mod.Info.ModID + "/"))
			{
				AssetLocation assetLocation = new AssetLocation(mod.Info.ModID, item.Location.Path.Remove(0, text.Length));
				if (api.Assets.AllAssets.ContainsKey(assetLocation))
				{
					num2++;
				}
				else
				{
					num++;
				}
				if ((assetLocation.Category.SideType & api.Side) != 0)
				{
					api.Assets.Add(assetLocation, item);
				}
			}
		}
		api.World.Logger.Notification("Compatibility lib: {0} assets added, {1} assets replaced.", num, num2);
	}
}
