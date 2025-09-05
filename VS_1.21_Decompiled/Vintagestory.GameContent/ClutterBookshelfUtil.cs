using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ClutterBookshelfUtil : ModSystem
{
	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("dev").RequiresPrivilege(Privilege.controlserver).BeginSub("bookshelfvariant")
			.WithDesc("Set book shelf variant")
			.WithArgs(parsers.WorldPosition("block position"), parsers.Word("index or 'random' or 'dec'/'inc' to dec/increment by 1"))
			.HandleWith((TextCommandCallingArgs args) => setBookshelfVariant(args, 1))
			.EndSub()
			.BeginSub("bookshelfvariant2")
			.WithDesc("Set book shelf variant, other side on double sided ones")
			.WithArgs(parsers.WorldPosition("block position"), parsers.Word("index or 'random' or 'dec'/'inc' to dec/increment by 1"))
			.HandleWith((TextCommandCallingArgs args) => setBookshelfVariant(args, 2))
			.EndSub();
	}

	private TextCommandResult setBookshelfVariant(TextCommandCallingArgs args, int type)
	{
		Vec3d vec3d = args[0] as Vec3d;
		string text = args[1] as string;
		int num = text.ToInt(-1);
		BEBehaviorClutterBookshelf bEBehaviorClutterBookshelf = api.World.BlockAccessor.GetBlockEntity(vec3d.AsBlockPos)?.GetBehavior<BEBehaviorClutterBookshelf>();
		if (bEBehaviorClutterBookshelf == null)
		{
			return TextCommandResult.Error("Not looking at a bookshelf");
		}
		BookShelfVariantGroup bookShelfVariantGroup = (bEBehaviorClutterBookshelf.Block as BlockClutterBookshelf).variantGroupsByCode[bEBehaviorClutterBookshelf.Variant];
		if (text == "inc" || text == "dec")
		{
			num = GameMath.Mod(bookShelfVariantGroup.typesByCode.IndexOfKey(bEBehaviorClutterBookshelf.Type) + ((text == "inc") ? 1 : (-1)), bookShelfVariantGroup.typesByCode.Count);
		}
		else
		{
			if (num < 0)
			{
				num = api.World.Rand.Next(bookShelfVariantGroup.typesByCode.Count);
			}
			if (bookShelfVariantGroup.typesByCode.Count <= num)
			{
				return TextCommandResult.Error("Wrong index");
			}
		}
		if (type == 1)
		{
			bEBehaviorClutterBookshelf.Type = bookShelfVariantGroup.typesByCode.GetKeyAtIndex(num);
		}
		else
		{
			bEBehaviorClutterBookshelf.Type2 = bookShelfVariantGroup.typesByCode.GetKeyAtIndex(num);
		}
		bEBehaviorClutterBookshelf.loadMesh();
		bEBehaviorClutterBookshelf.Blockentity.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("type " + ((type == 1) ? bEBehaviorClutterBookshelf.Type : bEBehaviorClutterBookshelf.Type2) + " set.");
	}
}
