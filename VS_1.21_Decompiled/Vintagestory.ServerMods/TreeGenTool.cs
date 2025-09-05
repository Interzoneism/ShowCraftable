using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

internal class TreeGenTool : ToolBase
{
	private readonly IRandom _rand;

	private TreeGeneratorsUtil _treeGenerators;

	public float MinTreeSize
	{
		get
		{
			return base.workspace.FloatValues["std.treeToolMinTreeSize"];
		}
		set
		{
			base.workspace.FloatValues["std.treeToolMinTreeSize"] = value;
		}
	}

	public float MaxTreeSize
	{
		get
		{
			return base.workspace.FloatValues["std.treeToolMaxTreeSize"];
		}
		set
		{
			base.workspace.FloatValues["std.treeToolMaxTreeSize"] = value;
		}
	}

	public string TreeVariant
	{
		get
		{
			return base.workspace.StringValues["std.treeToolTreeVariant"];
		}
		set
		{
			base.workspace.StringValues["std.treeToolTreeVariant"] = value;
		}
	}

	public int WithForestFloor
	{
		get
		{
			return base.workspace.IntValues["std.treeToolWithForestFloor"];
		}
		set
		{
			base.workspace.IntValues["std.treeToolWithForestFloor"] = value;
		}
	}

	public float VinesGrowthChance
	{
		get
		{
			return base.workspace.FloatValues["std.treeToolVinesGrowthChance"];
		}
		set
		{
			base.workspace.FloatValues["std.treeToolVinesGrowthChance"] = value;
		}
	}

	public override Vec3i Size => new Vec3i(0, 0, 0);

	public TreeGenTool()
	{
	}

	public TreeGenTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		_rand = new NormalRandom();
		if (!workspace.FloatValues.ContainsKey("std.treeToolMinTreeSize"))
		{
			MinTreeSize = 0.7f;
		}
		if (!workspace.FloatValues.ContainsKey("std.treeToolMaxTreeSize"))
		{
			MaxTreeSize = 1.3f;
		}
		if (!workspace.StringValues.ContainsKey("std.treeToolTreeVariant"))
		{
			TreeVariant = null;
		}
		if (!workspace.FloatValues.ContainsKey("std.treeToolVinesGrowthChance"))
		{
			VinesGrowthChance = 0f;
		}
		if (!workspace.IntValues.ContainsKey("std.treeToolWithForestFloor"))
		{
			WithForestFloor = 0;
		}
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer serverPlayer = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs rawArgs = callerArgs.RawArgs;
		if (_treeGenerators == null)
		{
			_treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
		}
		switch (rawArgs.PopWord())
		{
		case "tsizemin":
		{
			float result2 = 0.7f;
			if (rawArgs.Length > 0)
			{
				float.TryParse(rawArgs[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result2);
			}
			MinTreeSize = result2;
			WorldEdit.Good(serverPlayer, "Tree Min Size=" + result2 + " set.", Array.Empty<object>());
			return true;
		}
		case "tsizemax":
		{
			float result3 = 0.7f;
			if (rawArgs.Length > 0)
			{
				float.TryParse(rawArgs[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result3);
			}
			MaxTreeSize = result3;
			WorldEdit.Good(serverPlayer, "Tree Max Size=" + result3 + " set.", Array.Empty<object>());
			return true;
		}
		case "tsize":
		{
			float result4 = 0.7f;
			if (rawArgs.Length > 0)
			{
				float.TryParse(rawArgs[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result4);
			}
			MinTreeSize = result4;
			float result5 = 1.3f;
			if (rawArgs.Length > 1)
			{
				float.TryParse(rawArgs[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result5);
			}
			MaxTreeSize = result5;
			WorldEdit.Good(serverPlayer, "Tree Min Size=" + result4 + ", max size =" + MaxTreeSize + " set.", Array.Empty<object>());
			return true;
		}
		case "trnd":
			return true;
		case "tforestfloor":
		{
			bool? flag = rawArgs.PopBool(false);
			WithForestFloor = ((flag == true) ? 1 : 0);
			WorldEdit.Good(serverPlayer, "Forest floor generation now {0}.", new object[1] { (flag == true) ? "on" : "off" });
			return true;
		}
		case "tvines":
		{
			float num2 = (VinesGrowthChance = rawArgs.PopFloat(0f).Value);
			WorldEdit.Good(serverPlayer, "Vines growth chance now at {0}.", new object[1] { num2 });
			return true;
		}
		case "tv":
		{
			string text = rawArgs.PopWord();
			int result;
			bool num = int.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result);
			_treeGenerators.ReloadTreeGenerators();
			if (num)
			{
				KeyValuePair<AssetLocation, ITreeGenerator> generator = _treeGenerators.GetGenerator(result);
				if (generator.Key == null)
				{
					WorldEdit.Bad(serverPlayer, "No such tree variant found.", Array.Empty<object>());
					return true;
				}
				TreeVariant = generator.Key.ToShortString();
				WorldEdit.Good(serverPlayer, string.Concat("Tree variant ", generator.Key, " set."), Array.Empty<object>());
			}
			else if (text != null && _treeGenerators.GetGenerator(new AssetLocation(text)) != null)
			{
				TreeVariant = text;
				WorldEdit.Good(serverPlayer, "Tree variant " + text + " set.", Array.Empty<object>());
			}
			else
			{
				WorldEdit.Bad(serverPlayer, "No such tree variant found.", Array.Empty<object>());
			}
			return true;
		}
		default:
			return false;
		}
	}

	public override void OnInteractStart(WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (_treeGenerators == null)
		{
			_treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
		}
		if (TreeVariant == null)
		{
			WorldEdit.Bad((IServerPlayer)worldEdit.sapi.World.PlayerByUid(base.workspace.PlayerUID), "Please select a tree variant first.", Array.Empty<object>());
			return;
		}
		base.ba.ReadFromStagedByDefault = true;
		_treeGenerators.ReloadTreeGenerators();
		_treeGenerators.GetGenerator(new AssetLocation(TreeVariant)).GrowTree(treeGenParams: new TreeGenParams
		{
			skipForestFloor = (WithForestFloor == 0),
			size = MinTreeSize + (float)_rand.NextDouble() * (MaxTreeSize - MinTreeSize),
			vinesGrowthChance = VinesGrowthChance
		}, blockAccessor: base.ba, pos: blockSelection.Position, random: _rand);
		base.ba.Commit();
	}
}
