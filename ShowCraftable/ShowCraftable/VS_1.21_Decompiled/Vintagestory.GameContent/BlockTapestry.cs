using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTapestry : Block
{
	private ICoreClientAPI capi;

	private BlockFacing orientation;

	private bool noLoreEvent;

	public static string[][] tapestryGroups;

	public static string[][] wallcarvingGroups;

	private static Dictionary<string, TVec2i[]> neighbours2x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[1]
			{
				new TVec2i(1, 0, "2")
			}
		},
		{
			"2",
			new TVec2i[1]
			{
				new TVec2i(-1, 0, "1")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours1x2 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[1]
			{
				new TVec2i(0, -1, "2")
			}
		},
		{
			"2",
			new TVec2i[1]
			{
				new TVec2i(0, 1, "1")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours1x3 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[2]
			{
				new TVec2i(0, -1, "2"),
				new TVec2i(0, -2, "3")
			}
		},
		{
			"2",
			new TVec2i[2]
			{
				new TVec2i(0, 1, "1"),
				new TVec2i(0, -1, "3")
			}
		},
		{
			"3",
			new TVec2i[2]
			{
				new TVec2i(0, 2, "1"),
				new TVec2i(0, 1, "2")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours3x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[2]
			{
				new TVec2i(1, 0, "2"),
				new TVec2i(2, 0, "3")
			}
		},
		{
			"2",
			new TVec2i[2]
			{
				new TVec2i(-1, 0, "1"),
				new TVec2i(1, 0, "3")
			}
		},
		{
			"3",
			new TVec2i[2]
			{
				new TVec2i(-2, 0, "1"),
				new TVec2i(-1, 0, "2")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours4x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[3]
			{
				new TVec2i(1, 0, "2"),
				new TVec2i(2, 0, "3"),
				new TVec2i(3, 0, "4")
			}
		},
		{
			"2",
			new TVec2i[3]
			{
				new TVec2i(-1, 0, "1"),
				new TVec2i(1, 0, "3"),
				new TVec2i(2, 0, "4")
			}
		},
		{
			"3",
			new TVec2i[3]
			{
				new TVec2i(-2, 0, "1"),
				new TVec2i(-1, 0, "2"),
				new TVec2i(1, 0, "4")
			}
		},
		{
			"4",
			new TVec2i[3]
			{
				new TVec2i(-3, 0, "1"),
				new TVec2i(-2, 0, "2"),
				new TVec2i(-1, 0, "3")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours2x2 = new Dictionary<string, TVec2i[]>
	{
		{
			"11",
			new TVec2i[3]
			{
				new TVec2i(1, 0, "12"),
				new TVec2i(0, -1, "21"),
				new TVec2i(1, -1, "22")
			}
		},
		{
			"12",
			new TVec2i[3]
			{
				new TVec2i(-1, 0, "11"),
				new TVec2i(0, -1, "22"),
				new TVec2i(-1, -1, "21")
			}
		},
		{
			"21",
			new TVec2i[3]
			{
				new TVec2i(0, 1, "11"),
				new TVec2i(1, 0, "22"),
				new TVec2i(1, 1, "12")
			}
		},
		{
			"22",
			new TVec2i[3]
			{
				new TVec2i(0, 1, "12"),
				new TVec2i(-1, 0, "21"),
				new TVec2i(-1, 1, "11")
			}
		}
	};

	public string LoreCode => FirstCodePart();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		orientation = BlockFacing.FromCode(Variant["side"]);
		noLoreEvent = Attributes.IsTrue("noLoreEvent");
		if (tapestryGroups == null)
		{
			tapestryGroups = Attributes["tapestryGroups"].AsObject<string[][]>();
		}
		if (wallcarvingGroups == null)
		{
			wallcarvingGroups = Attributes["wallcarvingGroups"].AsObject<string[][]>();
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		tapestryGroups = null;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, FirstCodePart() + "MeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
		renderinfo.NormalShaded = false;
		string text = itemstack.Attributes.GetString("type", "");
		if (!orCreate.TryGetValue(text, out var value))
		{
			MeshData data = genMesh(rotten: false, text, 0, inventory: true);
			value = (orCreate[text] = capi.Render.UploadMultiTextureMesh(data));
		}
		renderinfo.ModelRef = value;
	}

	public static string GetBaseCode(string type)
	{
		if (type.Length == 0)
		{
			return null;
		}
		int num = 0;
		if (char.IsDigit(type[type.Length - 1]))
		{
			num++;
		}
		if (char.IsDigit(type[type.Length - 2]))
		{
			num++;
		}
		return type.Substring(0, type.Length - num);
	}

	public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
	{
		if (noLoreEvent || !firstTick || api.Side != EnumAppSide.Server)
		{
			return;
		}
		BlockEntityTapestry blockEntityTapestry = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTapestry;
		if (blockEntityTapestry.Rotten || blockEntityTapestry.Type == null)
		{
			return;
		}
		string baseCode = GetBaseCode(blockEntityTapestry.Type);
		if (baseCode == null)
		{
			return;
		}
		int loreChapterId = GetLoreChapterId(baseCode);
		if (loreChapterId >= 0)
		{
			string text = Attributes["sizes"][baseCode].AsString();
			Dictionary<string, TVec2i[]> dictionary;
			switch (text)
			{
			case "1x1":
				TryDiscoverLore(byPlayer, loreChapterId);
				return;
			case "2x1":
				dictionary = neighbours2x1;
				break;
			case "1x2":
				dictionary = neighbours1x2;
				break;
			case "1x3":
				dictionary = neighbours1x3;
				break;
			case "3x1":
				dictionary = neighbours3x1;
				break;
			case "4x1":
				dictionary = neighbours4x1;
				break;
			case "2x2":
				dictionary = neighbours2x2;
				break;
			default:
				throw new Exception("invalid " + FirstCodePart() + " json config - missing size attribute for size '" + text + "'");
			}
			string key = blockEntityTapestry.Type.Substring(baseCode.Length);
			TVec2i[] vecs = dictionary[key];
			if (isComplete(blockSel.Position, baseCode, vecs))
			{
				TryDiscoverLore(byPlayer, loreChapterId);
			}
		}
	}

	private void TryDiscoverLore(IPlayer byPlayer, int id)
	{
		ModJournal modSystem = api.ModLoader.GetModSystem<ModJournal>();
		LoreDiscovery newdiscovery = new LoreDiscovery
		{
			Code = LoreCode,
			ChapterIds = new List<int> { id }
		};
		modSystem.TryDiscoverLore(newdiscovery, byPlayer as IServerPlayer);
	}

	public int GetLoreChapterId(string baseCode)
	{
		if (!Attributes["loreChapterIds"][baseCode].Exists)
		{
			throw new Exception("incomplete " + FirstCodePart() + " json configuration - missing lore piece id");
		}
		return Attributes["loreChapterIds"][baseCode].AsInt();
	}

	private bool isComplete(BlockPos position, string baseCode, TVec2i[] vecs)
	{
		foreach (TVec2i tVec2i in vecs)
		{
			Vec3i vec3i;
			switch (orientation.Index)
			{
			case 0:
				vec3i = new Vec3i(tVec2i.X, tVec2i.Y, 0);
				break;
			case 1:
				vec3i = new Vec3i(0, tVec2i.Y, tVec2i.X);
				break;
			case 2:
				vec3i = new Vec3i(-tVec2i.X, tVec2i.Y, 0);
				break;
			case 3:
				vec3i = new Vec3i(0, tVec2i.Y, -tVec2i.X);
				break;
			default:
				return false;
			}
			if (!(api.World.BlockAccessor.GetBlockEntity(position.AddCopy(vec3i.X, vec3i.Y, vec3i.Z)) is BlockEntityTapestry blockEntityTapestry))
			{
				return false;
			}
			string baseCode2 = GetBaseCode(blockEntityTapestry.Type);
			if (baseCode2 != baseCode)
			{
				return false;
			}
			if (blockEntityTapestry.Rotten)
			{
				return false;
			}
			if (blockEntityTapestry.Type.Substring(baseCode2.Length) != tVec2i.IntComp)
			{
				return false;
			}
		}
		return true;
	}

	public MeshData genMesh(bool rotten, string type, int rotVariant, bool inventory = false)
	{
		TapestryTextureSource texSource = new TapestryTextureSource(capi, rotten, type, rotVariant);
		Shape cachedShape = capi.TesselatorManager.GetCachedShape(inventory ? ShapeInventory.Base : Shape.Base);
		capi.Tesselator.TesselateShape(FirstCodePart() + "block", cachedShape, out var modeldata, texSource, null, 0, 0, 0);
		return modeldata;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityTapestry blockEntityTapestry = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		if (blockEntityTapestry.Rotten)
		{
			return Array.Empty<ItemStack>();
		}
		drops[0].Attributes.SetString("type", blockEntityTapestry?.Type);
		return drops;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityTapestry blockEntityTapestry = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		ItemStack itemStack = new ItemStack(this);
		itemStack.Attributes.SetString("type", blockEntityTapestry?.Type);
		itemStack.Attributes.SetBool("rotten", blockEntityTapestry?.Rotten ?? false);
		return itemStack;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("type", "");
		return Lang.Get(FirstCodePart() + "-name", Lang.GetMatching(FirstCodePart() + "-" + text));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityTapestry blockEntityTapestry = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		if (blockEntityTapestry != null && blockEntityTapestry.Rotten)
		{
			return Lang.Get("Rotten Tapestry");
		}
		string text = blockEntityTapestry?.Type;
		return Lang.Get(FirstCodePart() + "-name", Lang.GetMatching(FirstCodePart() + "-" + text));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(GetWordedSection(inSlot, world));
		if (withDebugInfo)
		{
			string value = inSlot.Itemstack.Attributes.GetString("type", "");
			dsc.AppendLine(value);
		}
	}

	public string GetWordedSection(ItemSlot slot, IWorldAccessor world)
	{
		string text = slot.Itemstack.Attributes.GetString("type", "");
		string baseCode = GetBaseCode(text);
		if (baseCode == null)
		{
			return "unknown";
		}
		string text2 = Attributes["sizes"][baseCode].AsString();
		string text3 = text.Substring(baseCode.Length);
		switch (text2)
		{
		case "1x1":
			return "";
		case "2x1":
			if (!(text3 == "1"))
			{
				if (text3 == "2")
				{
					return Lang.Get("Section: Right Half");
				}
				return "unknown";
			}
			return Lang.Get("Section: Left Half");
		case "1x2":
			if (!(text3 == "1"))
			{
				if (text3 == "2")
				{
					return Lang.Get("Section: Bottom Half");
				}
				return "unknown";
			}
			return Lang.Get("Section: Top Half");
		case "3x1":
			return text3 switch
			{
				"1" => Lang.Get("Section: Left third"), 
				"2" => Lang.Get("Section: Center third"), 
				"3" => Lang.Get("Section: Right third"), 
				_ => "unknown", 
			};
		case "1x3":
			return text3 switch
			{
				"1" => Lang.Get("Section: Top third"), 
				"2" => Lang.Get("Section: Middle third"), 
				"3" => Lang.Get("Section: Bottom third"), 
				_ => "unknown", 
			};
		case "4x1":
			return text3 switch
			{
				"1" => Lang.Get("Section: Top quarter"), 
				"2" => Lang.Get("Section: Top middle quarter"), 
				"3" => Lang.Get("Section: Bottom middle quarter"), 
				"4" => Lang.Get("Section: Bottom quarter"), 
				_ => "unknown", 
			};
		case "2x2":
			return text3 switch
			{
				"11" => Lang.Get("Section: Top Left Quarter"), 
				"21" => Lang.Get("Section: Bottom Left Quarter"), 
				"12" => Lang.Get("Section: Top Right Quarter"), 
				"22" => Lang.Get("Section: Bottom Right Quarter"), 
				_ => "unknown", 
			};
		default:
			throw new Exception("invalid " + FirstCodePart() + " json config - missing size attribute for size '" + text2 + "'");
		}
	}
}
