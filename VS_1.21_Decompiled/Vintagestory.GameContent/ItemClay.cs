using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemClay : Item
{
	private SkillItem[] toolModes;

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		return getClayFormAnim(byEntity) ?? base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		return getClayFormAnim(forEntity) ?? base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
	}

	public string getClayFormAnim(Entity byEntity)
	{
		EntityPlayer entityPlayer = byEntity as EntityPlayer;
		BlockPos blockPos = entityPlayer?.BlockSelection?.Position;
		if (blockPos != null && (entityPlayer.Controls.HandUse != EnumHandInteract.None || entityPlayer.Controls.RightMouseDown) && api.World.BlockAccessor.GetBlock(blockPos) is BlockClayForm)
		{
			return "clayform";
		}
		return null;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi != null)
		{
			toolModes = ObjectCacheUtil.GetOrCreate(api, "clayToolModes", () => new SkillItem[4]
			{
				new SkillItem
				{
					Code = new AssetLocation("1size"),
					Name = Lang.Get("1x1")
				}.WithIcon(capi, Drawcreate1_svg),
				new SkillItem
				{
					Code = new AssetLocation("2size"),
					Name = Lang.Get("2x2")
				}.WithIcon(capi, Drawcreate4_svg),
				new SkillItem
				{
					Code = new AssetLocation("3size"),
					Name = Lang.Get("3x3")
				}.WithIcon(capi, Drawcreate9_svg),
				new SkillItem
				{
					Code = new AssetLocation("duplicate"),
					Name = Lang.Get("Duplicate layer")
				}.WithIcon(capi, Drawduplicate_svg)
			});
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		int num = 0;
		while (toolModes != null && num < toolModes.Length)
		{
			toolModes[num]?.Dispose();
			num++;
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			return;
		}
		BlockEntityClayForm blockEntityClayForm = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityClayForm;
		if (blockEntityClayForm != null && blockEntityClayForm.BaseMaterial.Collectible.Variant["type"] != Variant["type"])
		{
			return;
		}
		if (byEntity.Controls.ShiftKey)
		{
			IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			if (!byEntity.World.Claims.TryAccess(player, blockPos, EnumBlockAccessFlags.BuildOrBreak))
			{
				slot.MarkDirty();
				return;
			}
			if (blockEntityClayForm != null)
			{
				OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
				return;
			}
			IWorldAccessor world = byEntity.World;
			Block block = world.GetBlock(new AssetLocation("clayform"));
			if (block == null)
			{
				return;
			}
			BlockPos pos = blockSel.Position.AddCopy(blockSel.Face).Down();
			if (world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(byEntity.World.BlockAccessor, block, pos, BlockFacing.UP) && world.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block))
			{
				world.BlockAccessor.SetBlock(block.BlockId, blockPos);
				if (block.Sounds != null)
				{
					world.PlaySoundAt(block.Sounds.Place, blockSel.Position, -0.5);
				}
				if (byEntity.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityClayForm blockEntityClayForm2)
				{
					blockEntityClayForm2.PutClay(slot);
				}
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
		else
		{
			OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		}
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockClayForm) || !(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityClayForm blockEntityClayForm))
		{
			return;
		}
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player != null && byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			if (blockEntityClayForm.AvailableVoxels <= 0)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
				blockEntityClayForm.AvailableVoxels += 25;
			}
			if (byEntity.World is IClientWorldAccessor)
			{
				blockEntityClayForm.OnUseOver(player, blockSel.SelectionBoxIndex, blockSel.Face, mouseBreakMode: false);
			}
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockClayForm && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityClayForm blockEntityClayForm)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null && byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				blockEntityClayForm.OnBeginUse(player, blockSel);
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockClayForm && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityClayForm blockEntityClayForm)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null && byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use) && byEntity.World is IClientWorldAccessor)
			{
				blockEntityClayForm.OnUseOver(player, blockSel.SelectionBoxIndex, blockSel.Face, mouseBreakMode: true);
			}
		}
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return null;
		}
		if (!(forPlayer.Entity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockClayForm))
		{
			return null;
		}
		return toolModes;
	}

	public static void Drawcreate1_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(51.828125, 51.828125);
		cr.LineTo(76.828125, 51.828125);
		cr.LineTo(76.828125, 76.828125);
		cr.LineTo(51.828125, 76.828125);
		cr.ClosePath();
		cr.MoveTo(51.828125, 51.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(51.828125, 51.828125);
		cr.LineTo(76.828125, 51.828125);
		cr.LineTo(76.828125, 76.828125);
		cr.LineTo(51.828125, 76.828125);
		cr.ClosePath();
		cr.MoveTo(51.828125, 51.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public void Drawremove1_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(51.828125, 51.828125);
		cr.LineTo(76.828125, 51.828125);
		cr.LineTo(76.828125, 76.828125);
		cr.LineTo(51.828125, 76.828125);
		cr.ClosePath();
		cr.MoveTo(51.828125, 51.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(51.828125, 51.828125);
		cr.LineTo(76.828125, 51.828125);
		cr.LineTo(76.828125, 76.828125);
		cr.LineTo(51.828125, 76.828125);
		cr.ClosePath();
		cr.MoveTo(51.828125, 51.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(91.828125, 36.828125);
		cr.LineTo(36.328125, 92.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public static void Drawcreate4_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected O, but got Unknown
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Expected O, but got Unknown
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Expected O, but got Unknown
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Expected O, but got Unknown
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Expected O, but got Unknown
		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fb: Expected O, but got Unknown
		//IL_06f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 33.828125);
		cr.LineTo(59.078125, 33.828125);
		cr.LineTo(59.078125, 58.828125);
		cr.LineTo(34.078125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 33.828125);
		cr.LineTo(59.078125, 33.828125);
		cr.LineTo(59.078125, 58.828125);
		cr.LineTo(34.078125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 33.828125);
		cr.LineTo(96.578125, 33.828125);
		cr.LineTo(96.578125, 58.828125);
		cr.LineTo(71.578125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 33.828125);
		cr.LineTo(96.578125, 33.828125);
		cr.LineTo(96.578125, 58.828125);
		cr.LineTo(71.578125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 71.828125);
		cr.LineTo(59.078125, 71.828125);
		cr.LineTo(59.078125, 96.828125);
		cr.LineTo(34.078125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 71.828125);
		cr.LineTo(59.078125, 71.828125);
		cr.LineTo(59.078125, 96.828125);
		cr.LineTo(34.078125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 71.828125);
		cr.LineTo(96.578125, 71.828125);
		cr.LineTo(96.578125, 96.828125);
		cr.LineTo(71.578125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 71.828125);
		cr.LineTo(96.578125, 71.828125);
		cr.LineTo(96.578125, 96.828125);
		cr.LineTo(71.578125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public void Drawremove4_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Expected O, but got Unknown
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Expected O, but got Unknown
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Expected O, but got Unknown
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Expected O, but got Unknown
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Expected O, but got Unknown
		//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f6: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 33.828125);
		cr.LineTo(59.078125, 33.828125);
		cr.LineTo(59.078125, 58.828125);
		cr.LineTo(34.078125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 33.828125);
		cr.LineTo(59.078125, 33.828125);
		cr.LineTo(59.078125, 58.828125);
		cr.LineTo(34.078125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 33.828125);
		cr.LineTo(96.578125, 33.828125);
		cr.LineTo(96.578125, 58.828125);
		cr.LineTo(71.578125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 33.828125);
		cr.LineTo(96.578125, 33.828125);
		cr.LineTo(96.578125, 58.828125);
		cr.LineTo(71.578125, 58.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 33.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 71.828125);
		cr.LineTo(59.078125, 71.828125);
		cr.LineTo(59.078125, 96.828125);
		cr.LineTo(34.078125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(34.078125, 71.828125);
		cr.LineTo(59.078125, 71.828125);
		cr.LineTo(59.078125, 96.828125);
		cr.LineTo(34.078125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(34.078125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 71.828125);
		cr.LineTo(96.578125, 71.828125);
		cr.LineTo(96.578125, 96.828125);
		cr.LineTo(71.578125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.578125, 71.828125);
		cr.LineTo(96.578125, 71.828125);
		cr.LineTo(96.578125, 96.828125);
		cr.LineTo(71.578125, 96.828125);
		cr.ClosePath();
		cr.MoveTo(71.578125, 71.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(108.828125, 21.828125);
		cr.LineTo(19.328125, 111.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public void Drawcreate9_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Expected O, but got Unknown
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Expected O, but got Unknown
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Expected O, but got Unknown
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Expected O, but got Unknown
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Expected O, but got Unknown
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ca: Expected O, but got Unknown
		//IL_08c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ca: Expected O, but got Unknown
		//IL_0991: Unknown result type (might be due to invalid IL or missing references)
		//IL_0997: Expected O, but got Unknown
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a97: Expected O, but got Unknown
		//IL_0b5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b64: Expected O, but got Unknown
		//IL_0c5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c64: Expected O, but got Unknown
		//IL_0d2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d31: Expected O, but got Unknown
		//IL_0e2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e31: Expected O, but got Unknown
		//IL_0ef8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efe: Expected O, but got Unknown
		//IL_0ff8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffe: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 14.828125);
		cr.LineTo(40.328125, 14.828125);
		cr.LineTo(40.328125, 39.828125);
		cr.LineTo(15.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 14.828125);
		cr.LineTo(40.328125, 14.828125);
		cr.LineTo(40.328125, 39.828125);
		cr.LineTo(15.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 14.828125);
		cr.LineTo(77.828125, 14.828125);
		cr.LineTo(77.828125, 39.828125);
		cr.LineTo(52.828125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 14.828125);
		cr.LineTo(77.828125, 14.828125);
		cr.LineTo(77.828125, 39.828125);
		cr.LineTo(52.828125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 52.828125);
		cr.LineTo(40.328125, 52.828125);
		cr.LineTo(40.328125, 77.828125);
		cr.LineTo(15.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 52.828125);
		cr.LineTo(40.328125, 52.828125);
		cr.LineTo(40.328125, 77.828125);
		cr.LineTo(15.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 52.828125);
		cr.LineTo(77.828125, 52.828125);
		cr.LineTo(77.828125, 77.828125);
		cr.LineTo(52.828125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 52.828125);
		cr.LineTo(77.828125, 52.828125);
		cr.LineTo(77.828125, 77.828125);
		cr.LineTo(52.828125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 14.828125);
		cr.LineTo(115.328125, 14.828125);
		cr.LineTo(115.328125, 39.828125);
		cr.LineTo(90.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 14.828125);
		cr.LineTo(115.328125, 14.828125);
		cr.LineTo(115.328125, 39.828125);
		cr.LineTo(90.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 52.828125);
		cr.LineTo(115.328125, 52.828125);
		cr.LineTo(115.328125, 77.828125);
		cr.LineTo(90.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 52.828125);
		cr.LineTo(115.328125, 52.828125);
		cr.LineTo(115.328125, 77.828125);
		cr.LineTo(90.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 91.328125);
		cr.LineTo(40.328125, 91.328125);
		cr.LineTo(40.328125, 116.328125);
		cr.LineTo(15.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 91.328125);
		cr.LineTo(40.328125, 91.328125);
		cr.LineTo(40.328125, 116.328125);
		cr.LineTo(15.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 91.328125);
		cr.LineTo(77.828125, 91.328125);
		cr.LineTo(77.828125, 116.328125);
		cr.LineTo(52.828125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 91.328125);
		cr.LineTo(77.828125, 91.328125);
		cr.LineTo(77.828125, 116.328125);
		cr.LineTo(52.828125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 91.328125);
		cr.LineTo(115.328125, 91.328125);
		cr.LineTo(115.328125, 116.328125);
		cr.LineTo(90.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 91.328125);
		cr.LineTo(115.328125, 91.328125);
		cr.LineTo(115.328125, 116.328125);
		cr.LineTo(90.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public void Drawremove9_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Expected O, but got Unknown
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Expected O, but got Unknown
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Expected O, but got Unknown
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Expected O, but got Unknown
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Expected O, but got Unknown
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ca: Expected O, but got Unknown
		//IL_08c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ca: Expected O, but got Unknown
		//IL_0991: Unknown result type (might be due to invalid IL or missing references)
		//IL_0997: Expected O, but got Unknown
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a97: Expected O, but got Unknown
		//IL_0b5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b64: Expected O, but got Unknown
		//IL_0c5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c64: Expected O, but got Unknown
		//IL_0d2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d31: Expected O, but got Unknown
		//IL_0e2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e31: Expected O, but got Unknown
		//IL_0ef8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efe: Expected O, but got Unknown
		//IL_0ff8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffe: Expected O, but got Unknown
		//IL_10f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f7: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 14.828125);
		cr.LineTo(40.328125, 14.828125);
		cr.LineTo(40.328125, 39.828125);
		cr.LineTo(15.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 14.828125);
		cr.LineTo(40.328125, 14.828125);
		cr.LineTo(40.328125, 39.828125);
		cr.LineTo(15.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 14.828125);
		cr.LineTo(77.828125, 14.828125);
		cr.LineTo(77.828125, 39.828125);
		cr.LineTo(52.828125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 14.828125);
		cr.LineTo(77.828125, 14.828125);
		cr.LineTo(77.828125, 39.828125);
		cr.LineTo(52.828125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 52.828125);
		cr.LineTo(40.328125, 52.828125);
		cr.LineTo(40.328125, 77.828125);
		cr.LineTo(15.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 52.828125);
		cr.LineTo(40.328125, 52.828125);
		cr.LineTo(40.328125, 77.828125);
		cr.LineTo(15.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 52.828125);
		cr.LineTo(77.828125, 52.828125);
		cr.LineTo(77.828125, 77.828125);
		cr.LineTo(52.828125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 52.828125);
		cr.LineTo(77.828125, 52.828125);
		cr.LineTo(77.828125, 77.828125);
		cr.LineTo(52.828125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 14.828125);
		cr.LineTo(115.328125, 14.828125);
		cr.LineTo(115.328125, 39.828125);
		cr.LineTo(90.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 14.828125);
		cr.LineTo(115.328125, 14.828125);
		cr.LineTo(115.328125, 39.828125);
		cr.LineTo(90.328125, 39.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 14.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 52.828125);
		cr.LineTo(115.328125, 52.828125);
		cr.LineTo(115.328125, 77.828125);
		cr.LineTo(90.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 52.828125);
		cr.LineTo(115.328125, 52.828125);
		cr.LineTo(115.328125, 77.828125);
		cr.LineTo(90.328125, 77.828125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 52.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 91.328125);
		cr.LineTo(40.328125, 91.328125);
		cr.LineTo(40.328125, 116.328125);
		cr.LineTo(15.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(15.328125, 91.328125);
		cr.LineTo(40.328125, 91.328125);
		cr.LineTo(40.328125, 116.328125);
		cr.LineTo(15.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(15.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 91.328125);
		cr.LineTo(77.828125, 91.328125);
		cr.LineTo(77.828125, 116.328125);
		cr.LineTo(52.828125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.828125, 91.328125);
		cr.LineTo(77.828125, 91.328125);
		cr.LineTo(77.828125, 116.328125);
		cr.LineTo(52.828125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(52.828125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 91.328125);
		cr.LineTo(115.328125, 91.328125);
		cr.LineTo(115.328125, 116.328125);
		cr.LineTo(90.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(90.328125, 91.328125);
		cr.LineTo(115.328125, 91.328125);
		cr.LineTo(115.328125, 116.328125);
		cr.LineTo(90.328125, 116.328125);
		cr.ClosePath();
		cr.MoveTo(90.328125, 91.328125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(125.828125, 2.828125);
		cr.LineTo(2.828125, 125.828125);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public void Drawduplicate_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Expected O, but got Unknown
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Expected O, but got Unknown
		//IL_06dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e3: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 129f;
		float num2 = 129f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		cr.LineWidth = 5.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.328125, 66.042969);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 5.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(71.328125, 46.078125);
		cr.LineTo(71.328125, 30.828125);
		cr.CurveTo(71.328125, 29.691406, 70.667969, 28.097656, 69.863281, 27.292969);
		cr.LineTo(60.363281, 17.792969);
		cr.CurveTo(59.558594, 16.988281, 57.96875, 16.328125, 56.828125, 16.328125);
		cr.LineTo(29.898438, 16.328125);
		cr.CurveTo(28.761719, 16.328125, 27.828125, 17.261719, 27.828125, 18.398438);
		cr.LineTo(27.828125, 76.398438);
		cr.CurveTo(27.828125, 77.539063, 28.761719, 78.472656, 29.898438, 78.472656);
		cr.LineTo(50.828125, 78.472656);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(58.035156, 27.6875);
		cr.CurveTo(58.035156, 28.828125, 58.96875, 29.757813, 60.109375, 29.757813);
		cr.LineTo(68.394531, 29.757813);
		cr.CurveTo(69.535156, 29.757813, 69.804688, 29.097656, 69.0, 28.292969);
		cr.LineTo(59.5, 18.796875);
		cr.CurveTo(58.695313, 17.988281, 58.035156, 18.261719, 58.035156, 19.402344);
		cr.ClosePath();
		cr.MoveTo(58.035156, 27.6875);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 5.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(94.328125, 95.792969);
		cr.LineTo(94.328125, 60.578125);
		cr.CurveTo(94.328125, 59.441406, 93.667969, 57.847656, 92.863281, 57.042969);
		cr.LineTo(83.363281, 47.542969);
		cr.CurveTo(82.558594, 46.738281, 80.96875, 46.078125, 79.828125, 46.078125);
		cr.LineTo(52.898438, 46.078125);
		cr.CurveTo(51.761719, 46.078125, 50.828125, 47.011719, 50.828125, 48.148438);
		cr.LineTo(50.828125, 106.148438);
		cr.CurveTo(50.828125, 107.289063, 51.761719, 108.222656, 52.898438, 108.222656);
		cr.LineTo(92.257813, 108.222656);
		cr.CurveTo(93.398438, 108.222656, 94.328125, 107.289063, 94.328125, 106.148438);
		cr.LineTo(94.328125, 95.792969);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(81.035156, 57.4375);
		cr.CurveTo(81.035156, 58.578125, 81.96875, 59.507813, 83.109375, 59.507813);
		cr.LineTo(91.394531, 59.507813);
		cr.CurveTo(92.535156, 59.507813, 92.804688, 58.847656, 92.0, 58.042969);
		cr.LineTo(82.5, 48.546875);
		cr.CurveTo(81.695313, 47.738281, 81.035156, 48.011719, 81.035156, 49.152344);
		cr.ClosePath();
		cr.MoveTo(81.035156, 57.4375);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return slot.Itemstack.Attributes.GetInt("toolMode");
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-placetoclayform",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
