using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Config;

public class GlobalConstants
{
	public static CultureInfo DefaultCultureInfo = CultureInfo.InvariantCulture;

	public const string DefaultDomain = "game";

	public const int MaxWorldSizeXZ = 67108864;

	public const int MaxWorldSizeY = 16384;

	public const int ChunkSize = 32;

	public const int DimensionSizeInChunks = 1024;

	public static int MaxAnimatedElements = 230;

	public const int MaxColorMaps = 40;

	public static int CaveArtColsPerRow = 6;

	public static float PhysicsFrameTime = 1f / 30f;

	public static float MaxPhysicsIntervalInSlowTicks = 0.135f;

	public static float GravityStrengthParticle = 0.3f;

	public static float DefaultAttackRange = 1.5f;

	public static float OverallSpeedMultiplier = 1f;

	public static float BaseMoveSpeed = 1.5f;

	public static float BaseJumpForce = 8.2f;

	public static float SneakSpeedMultiplier = 0.35f;

	public static double SprintSpeedMultiplier = 2.0;

	public static float AirDragAlways = 0.983f;

	public static float AirDragFlying = 0.8f;

	public static float WaterDrag = 0.92f;

	public static float GravityPerSecond = 0.37f;

	public static int DefaultSimulationRange = 128;

	public static float DefaultPickingRange = 4.5f;

	public static int TimeToDespawnPlayerInventoryDrops = 600;

	public static Vec3f CurrentWindSpeedClient = new Vec3f();

	public static Vec3f CurrentSurfaceWindSpeedClient = new Vec3f();

	public static float CurrentDistanceToRainfallClient;

	public static float CurrentNearbyRelLeavesCountClient;

	public static bool MeltingFreezingEnabled;

	public static float GuiGearRotJitter = 0f;

	public const int MaxViewDistanceForLodBiases = 640;

	public static string[] ReservedCharacterSequences = new string[6] { "å", "~", "++", "@90", "@180", "@270" };

	public const string WorldSaveExtension = ".vcdbs";

	public const string hotBarInvClassName = "hotbar";

	public const string creativeInvClassName = "creative";

	public const string backpackInvClassName = "backpack";

	public const string groundInvClassName = "ground";

	public const string mousecursorInvClassName = "mouse";

	public const string characterInvClassName = "character";

	public const string craftingInvClassName = "craftinggrid";

	public static Dictionary<string, double[]> playerColorByEntitlement = new Dictionary<string, double[]>
	{
		{
			"vsteam",
			new double[4]
			{
				13.0 / 255.0,
				128.0 / 255.0,
				62.0 / 255.0,
				1.0
			}
		},
		{
			"vscontributor",
			new double[4]
			{
				0.5294117647058824,
				179.0 / 255.0,
				148.0 / 255.0,
				1.0
			}
		},
		{
			"vssupporter",
			new double[4]
			{
				254.0 / 255.0,
				197.0 / 255.0,
				0.0,
				1.0
			}
		},
		{
			"securityresearcher",
			new double[4]
			{
				49.0 / 255.0,
				53.0 / 85.0,
				58.0 / 85.0,
				1.0
			}
		},
		{
			"bughunter",
			new double[4]
			{
				58.0 / 85.0,
				32.0 / 85.0,
				49.0 / 255.0,
				1.0
			}
		},
		{
			"chiselmaster",
			new double[4]
			{
				242.0 / 255.0,
				244.0 / 255.0,
				11.0 / 15.0,
				1.0
			}
		}
	};

	public static Dictionary<string, TextBackground> playerTagBackgroundByEntitlement = new Dictionary<string, TextBackground>
	{
		{
			"vsteam",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		},
		{
			"vscontributor",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		},
		{
			"vssupporter",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		},
		{
			"securityresearcher",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		},
		{
			"bughunter",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		},
		{
			"chiselmaster",
			new TextBackground
			{
				FillColor = GuiStyle.DialogLightBgColor,
				Padding = 3,
				Radius = GuiStyle.ElementBGRadius,
				Shade = true,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 3.0
			}
		}
	};

	public static int[] DefaultChatGroups = new int[5] { GeneralChatGroup, ServerInfoChatGroup, DamageLogChatGroup, InfoLogChatGroup, ConsoleGroup };

	public static int GeneralChatGroup = 0;

	public static int ServerInfoChatGroup = -1;

	public static int DamageLogChatGroup = -5;

	public static int InfoLogChatGroup = -6;

	public static int CurrentChatGroup = -2;

	public static int AllChatGroups = -3;

	public static int ConsoleGroup = -4;

	public static string AllowedChatGroupChars = "a-z0-9A-Z_";

	public static string SinglePlayerEntitlements;

	public static AssetLocation EntityItemTypeCode = new AssetLocation("item");

	public static AssetLocation EntityPlayerTypeCode = new AssetLocation("player");

	public static AssetLocation EntityBlockFallingTypeCode = new AssetLocation("blockfalling");

	public static string[] IgnoredStackAttributes = new string[4] { "temperature", "toolMode", "renderVariant", "transitionstate" };

	public static float PerishSpeedModifier = 1f;

	public static float HungerSpeedModifier = 1f;

	public static float CreatureDamageModifier = 1f;

	public static float ToolMiningSpeedModifier = 1f;

	public static FoodSpoilageCalcDelegate FoodSpoilHealthLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

	public static FoodSpoilageCalcDelegate FoodSpoilSatLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

	public static bool OutsideWorld(int x, int y, int z, IBlockAccessor blockAccessor)
	{
		if (x >= -30 && z >= -30 && y >= -30 && x <= blockAccessor.MapSizeX + 30)
		{
			return z > blockAccessor.MapSizeZ + 30;
		}
		return true;
	}

	public static bool OutsideWorld(double x, double y, double z, IBlockAccessor blockAccessor)
	{
		if (!(x < -30.0) && !(z < -30.0) && !(y < -30.0) && !(x > (double)(blockAccessor.MapSizeX + 30)))
		{
			return z > (double)(blockAccessor.MapSizeZ + 30);
		}
		return true;
	}

	public static float FoodSpoilageHealthLossMul(float spoilState, ItemStack stack, EntityAgent byEntity)
	{
		return FoodSpoilHealthLossMulHandler(spoilState, stack, byEntity);
	}

	public static float FoodSpoilageSatLossMul(float spoilState, ItemStack stack, EntityAgent byEntity)
	{
		return FoodSpoilSatLossMulHandler(spoilState, stack, byEntity);
	}
}
