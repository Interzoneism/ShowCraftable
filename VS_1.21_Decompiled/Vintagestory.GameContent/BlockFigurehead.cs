using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFigurehead : BlockMaterialFromAttributes, IAttachableToEntity, IWrenchOrientable
{
	public override string MeshKey => "Figurehead";

	public int RequiresBehindSlots { get; set; }

	public bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		return true;
	}

	public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string newValue = stack.Attributes.GetString("material", "oak");
		foreach (string key in shape.Textures.Keys)
		{
			CompositeTexture compositeTexture = TexturesBMFA[key].Clone();
			compositeTexture.Base.Path = compositeTexture.Base.Path.Replace("{material}", newValue);
			compositeTexture.Bake(api.Assets);
			intoDict[key] = compositeTexture;
		}
	}

	public string GetCategoryCode(ItemStack stack)
	{
		return Attributes["attachableToEntity"]["categoryCode"].AsString();
	}

	public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
	{
		return Shape;
	}

	public string[] GetDisableElements(ItemStack stack)
	{
		return Array.Empty<string>();
	}

	public string[] GetKeepElements(ItemStack stack)
	{
		return Array.Empty<string>();
	}

	public string GetTexturePrefixCode(ItemStack stack)
	{
		return string.Empty;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("material", "oak");
		dsc.AppendLine(Lang.Get("Material: {0}", Lang.Get("material-" + text)));
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("material", "oak");
		return Lang.GetMatching("block-" + Code.Path + "-" + text, Lang.Get("material-" + text));
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BEBehaviorMaterialFromAttributes bEBehavior = GetBEBehavior<BEBehaviorMaterialFromAttributes>(pos);
		if (bEBehavior != null)
		{
			float[] values = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(bEBehavior.MeshAngleY)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
			blockModelData = GetOrCreateMesh(bEBehavior.Material).Clone().MatrixTransform(values);
			decalModelData = GetOrCreateMesh(bEBehavior.Material, decalTexSource).Clone().MatrixTransform(values);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		(GetBlockEntity<BlockEntityGeneric>(blockSel.Position)?.GetBehavior<BEBehaviorMaterialFromAttributes>())?.Rotate(byEntity, blockSel, dir);
	}
}
