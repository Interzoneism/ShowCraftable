using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class CompositeShape
{
	[DocumentAsJson]
	public AssetLocation Base;

	[DocumentAsJson]
	public EnumShapeFormat Format;

	[DocumentAsJson]
	public bool InsertBakedTextures;

	[DocumentAsJson]
	public float rotateX;

	[DocumentAsJson]
	public float rotateY;

	[DocumentAsJson]
	public float rotateZ;

	[DocumentAsJson]
	public float offsetX;

	[DocumentAsJson]
	public float offsetY;

	[DocumentAsJson]
	public float offsetZ;

	[DocumentAsJson]
	public float Scale = 1f;

	[DocumentAsJson]
	public CompositeShape[] Alternates;

	public CompositeShape[] BakedAlternates;

	[DocumentAsJson]
	public CompositeShape[] Overlays;

	[DocumentAsJson]
	public bool VoxelizeTexture;

	[DocumentAsJson]
	public int? QuantityElements;

	[DocumentAsJson]
	public string[] SelectiveElements;

	public string[] IgnoreElements;

	public Vec3f RotateXYZCopy => new Vec3f(rotateX, rotateY, rotateZ);

	public Vec3f OffsetXYZCopy => new Vec3f(offsetX, offsetY, offsetZ);

	public override int GetHashCode()
	{
		int num = Base.GetHashCode() + ("@" + rotateX + "/" + rotateY + "/" + rotateZ + "o" + offsetX + "/" + offsetY + "/" + offsetZ).GetHashCode();
		if (Overlays != null)
		{
			for (int i = 0; i < Overlays.Length; i++)
			{
				num ^= Overlays[i].GetHashCode();
			}
		}
		return num;
	}

	public override string ToString()
	{
		return Base.ToString();
	}

	public CompositeShape Clone()
	{
		CompositeShape[] array = null;
		if (Alternates != null)
		{
			array = new CompositeShape[Alternates.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Alternates[i].CloneWithoutAlternatesNorOverlays();
			}
		}
		CompositeShape compositeShape = CloneWithoutAlternates();
		compositeShape.Alternates = array;
		return compositeShape;
	}

	public CompositeShape CloneWithoutAlternates()
	{
		CompositeShape[] array = null;
		if (Overlays != null)
		{
			array = new CompositeShape[Overlays.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Overlays[i].CloneWithoutAlternatesNorOverlays();
			}
		}
		CompositeShape compositeShape = CloneWithoutAlternatesNorOverlays();
		compositeShape.Overlays = array;
		return compositeShape;
	}

	internal CompositeShape CloneWithoutAlternatesNorOverlays()
	{
		return new CompositeShape
		{
			Base = Base?.Clone(),
			Format = Format,
			InsertBakedTextures = InsertBakedTextures,
			rotateX = rotateX,
			rotateY = rotateY,
			rotateZ = rotateZ,
			offsetX = offsetX,
			offsetY = offsetY,
			offsetZ = offsetZ,
			Scale = Scale,
			VoxelizeTexture = VoxelizeTexture,
			QuantityElements = QuantityElements,
			SelectiveElements = (string[])SelectiveElements?.Clone(),
			IgnoreElements = (string[])IgnoreElements?.Clone()
		};
	}

	public void Bake(IAssetManager assetManager, ILogger logger)
	{
		LoadAlternates(assetManager, logger);
	}

	public void LoadAlternates(IAssetManager assetManager, ILogger logger)
	{
		List<CompositeShape> list = new List<CompositeShape>();
		if (Base.Path.EndsWith('*'))
		{
			list.AddRange(resolveShapeWildCards(this, assetManager, logger, addCubeIfNone: true));
		}
		else
		{
			list.Add(this);
		}
		if (Alternates != null)
		{
			CompositeShape[] alternates = Alternates;
			foreach (CompositeShape compositeShape in alternates)
			{
				if (compositeShape.Base == null)
				{
					compositeShape.Base = Base.Clone();
				}
				if (compositeShape.Base.Path.EndsWith('*'))
				{
					list.AddRange(resolveShapeWildCards(compositeShape, assetManager, logger, addCubeIfNone: false));
				}
				else
				{
					list.Add(compositeShape);
				}
			}
		}
		Base = list[0].Base;
		if (list.Count == 1)
		{
			return;
		}
		Alternates = new CompositeShape[list.Count - 1];
		for (int j = 0; j < list.Count - 1; j++)
		{
			Alternates[j] = list[j + 1];
		}
		BakedAlternates = new CompositeShape[Alternates.Length + 1];
		BakedAlternates[0] = CloneWithoutAlternates();
		for (int k = 0; k < Alternates.Length; k++)
		{
			CompositeShape compositeShape2 = (BakedAlternates[k + 1] = Alternates[k]);
			if (compositeShape2.Base == null)
			{
				compositeShape2.Base = Base.Clone();
			}
			if (!compositeShape2.QuantityElements.HasValue)
			{
				compositeShape2.QuantityElements = QuantityElements;
			}
			if (compositeShape2.SelectiveElements == null)
			{
				compositeShape2.SelectiveElements = SelectiveElements;
			}
			if (compositeShape2.IgnoreElements == null)
			{
				compositeShape2.IgnoreElements = IgnoreElements;
			}
		}
	}

	private CompositeShape[] resolveShapeWildCards(CompositeShape shape, IAssetManager assetManager, ILogger logger, bool addCubeIfNone)
	{
		List<IAsset> manyInCategory = assetManager.GetManyInCategory("shapes", shape.Base.Path.Substring(0, Base.Path.Length - 1), shape.Base.Domain);
		if (manyInCategory.Count == 0)
		{
			if (addCubeIfNone)
			{
				logger.Warning("Could not find any variants for wildcard shape {0}, will use standard cube shape.", shape.Base.Path);
				return new CompositeShape[1]
				{
					new CompositeShape
					{
						Base = new AssetLocation("block/basic/cube")
					}
				};
			}
			logger.Warning("Could not find any variants for wildcard shape {0}.", shape.Base.Path);
			return Array.Empty<CompositeShape>();
		}
		CompositeShape[] array = new CompositeShape[manyInCategory.Count];
		int num = 0;
		foreach (IAsset item in manyInCategory)
		{
			AssetLocation assetLocation = item.Location.CopyWithPath(item.Location.Path.Substring("shapes/".Length));
			assetLocation.RemoveEnding();
			array[num++] = new CompositeShape
			{
				Base = assetLocation,
				rotateX = shape.rotateX,
				rotateY = shape.rotateY,
				rotateZ = shape.rotateZ,
				Scale = shape.Scale,
				QuantityElements = shape.QuantityElements,
				SelectiveElements = shape.SelectiveElements,
				IgnoreElements = shape.IgnoreElements
			};
		}
		return array;
	}
}
