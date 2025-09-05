using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFruiting : BlockCrop
{
	private double[] FruitPoints { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		return base.GetColor(capi, pos);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		JsonObject attributes = Attributes;
		if (attributes == null || !attributes["pickPrompt"].AsBool())
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-fruiting-harvest",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = BlockUtil.GetKnifeStacks(api)
			}
		};
	}

	public virtual double[] GetFruitingPoints()
	{
		if (FruitPoints == null)
		{
			SetUpFruitPoints();
		}
		return FruitPoints;
	}

	public virtual void SetUpFruitPoints()
	{
		ShapeElement[] array = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(Shape.Base).Elements;
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		float scale = Shape.Scale;
		if (array.Length == 1 && array[0].Children != null)
		{
			num = (array[0].From[0] + array[0].To[0]) / 32.0;
			num2 = (array[0].From[1] + array[0].To[1]) / 32.0;
			num3 = (array[0].From[2] + array[0].To[2]) / 32.0;
			array = array[0].Children;
		}
		int num4 = 0;
		ShapeElement[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Name.StartsWithOrdinal("fruit"))
			{
				num4++;
			}
		}
		FruitPoints = new double[num4 * 3];
		double[] matrix = new double[16];
		double[] triple = new double[3];
		double[] array3 = new double[4];
		num4 = 0;
		array2 = array;
		foreach (ShapeElement shapeElement in array2)
		{
			if (!shapeElement.Name.StartsWithOrdinal("fruit"))
			{
				continue;
			}
			double num5 = shapeElement.From[0] / 16.0;
			double num6 = shapeElement.From[1] / 16.0;
			double num7 = shapeElement.From[2] / 16.0;
			double num8 = (shapeElement.To[0] - shapeElement.From[0]) / 32.0;
			double num9 = (shapeElement.To[1] - shapeElement.From[1]) / 16.0;
			double num10 = (shapeElement.To[2] - shapeElement.From[2]) / 32.0;
			if (shapeElement.Children != null)
			{
				ShapeElement[] children = shapeElement.Children;
				foreach (ShapeElement shapeElement2 in children)
				{
					array3[0] = (shapeElement2.To[0] - shapeElement2.From[0]) / 32.0;
					array3[1] = (shapeElement2.To[1] - shapeElement2.From[1]) / 16.0;
					array3[2] = (shapeElement2.To[2] - shapeElement2.From[2]) / 32.0;
					array3[3] = 1.0;
					double[] array4 = Rotate(array3, shapeElement2, matrix, triple);
					if (array4[1] > num9)
					{
						num8 = array4[0];
						num9 = array4[1];
						num10 = array4[2];
					}
				}
			}
			array3[0] = num8;
			array3[1] = num9;
			array3[2] = num10;
			array3[3] = 0.0;
			double[] array5 = Rotate(array3, shapeElement, matrix, triple);
			FruitPoints[num4 * 3] = (array5[0] + num5 + num - 0.5) * (double)scale + 0.5 + (double)Shape.offsetX;
			FruitPoints[num4 * 3 + 1] = (array5[1] + num6 + num2) * (double)scale + (double)Shape.offsetY;
			FruitPoints[num4 * 3 + 2] = (array5[2] + num7 + num3 - 0.5) * (double)scale + 0.5 + (double)Shape.offsetZ;
			num4++;
		}
	}

	private double[] Rotate(double[] pos, ShapeElement element, double[] matrix, double[] triple)
	{
		Mat4d.Identity(matrix);
		Mat4d.Translate(matrix, matrix, element.RotationOrigin[0] / 16.0, element.RotationOrigin[1] / 16.0, element.RotationOrigin[2] / 16.0);
		if (element.RotationX != 0.0)
		{
			triple[0] = 1.0;
			triple[1] = 0.0;
			triple[2] = 0.0;
			Mat4d.Rotate(matrix, matrix, element.RotationX * 0.01745329238474369, triple);
		}
		if (element.RotationY != 0.0)
		{
			triple[0] = 0.0;
			triple[1] = 1.0;
			triple[2] = 0.0;
			Mat4d.Rotate(matrix, matrix, element.RotationY * 0.01745329238474369, triple);
		}
		if (element.RotationZ != 0.0)
		{
			triple[0] = 0.0;
			triple[1] = 0.0;
			triple[2] = 1.0;
			Mat4d.Rotate(matrix, matrix, element.RotationZ * 0.01745329238474369, triple);
		}
		Mat4d.Translate(matrix, matrix, (element.From[0] - element.RotationOrigin[0]) / 16.0, (element.From[1] - element.RotationOrigin[1]) / 16.0, (element.From[2] - element.RotationOrigin[2]) / 16.0);
		return Mat4d.MulWithVec4(matrix, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFarmland blockEntityFarmland && blockEntityFarmland.OnBlockInteract(byPlayer))
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>() != null)
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>())?.OnPlayerInteract(secondsUsed, byPlayer, blockSel.HitPosition) ?? base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		(world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>())?.OnPlayerInteractStop(secondsUsed, byPlayer, blockSel.HitPosition);
	}
}
