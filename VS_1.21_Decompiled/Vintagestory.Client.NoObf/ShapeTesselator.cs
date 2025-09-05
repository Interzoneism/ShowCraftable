using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class ShapeTesselator : ITesselatorAPI
{
	public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

	public OrderedDictionary<AssetLocation, IAsset> objs;

	public OrderedDictionary<AssetLocation, GltfType> gltfs;

	private ClientMain game;

	private Vec3f noRotation = new Vec3f();

	private Vec3f constantCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	private Vec3f constantCenterXZ = new Vec3f(0.5f, 0f, 0.5f);

	private Vec3f rotationVec = new Vec3f();

	private Vec3f offsetVec = new Vec3f();

	private Vec3f xyzVec = new Vec3f();

	private Vec3f centerVec = new Vec3f();

	public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private ObjTesselator objTesselator = new ObjTesselator();

	private GltfTesselator gltfTesselator = new GltfTesselator();

	private TesselationMetaData meta = new TesselationMetaData();

	private MeshData elementMeshData = new MeshData(24, 36).WithColorMaps().WithRenderpasses();

	private StackMatrix4 stackMatrix = new StackMatrix4(64);

	private int[] flags = new int[4];

	private static int[] noFlags = new int[4];

	public ShapeTesselator(ClientMain game, OrderedDictionary<AssetLocation, UnloadableShape> shapes, OrderedDictionary<AssetLocation, IAsset> objs, OrderedDictionary<AssetLocation, GltfType> gltfs)
	{
		this.shapes = shapes;
		this.objs = objs;
		this.game = game;
		this.gltfs = gltfs;
	}

	public void TesselateShape(string type, AssetLocation sourceName, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[] selectiveElements = null)
	{
		if (!quantityElements.HasValue && compositeShape.QuantityElements > 0)
		{
			quantityElements = compositeShape.QuantityElements;
		}
		if (selectiveElements == null)
		{
			selectiveElements = compositeShape.SelectiveElements;
		}
		meta.UsesColorMap = false;
		meta.TypeForLogging = type + " " + sourceName;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = generalGlowLevel;
		meta.QuantityElements = quantityElements;
		meta.WithJointIds = false;
		meta.SelectiveElements = selectiveElements;
		meta.IgnoreElements = compositeShape.IgnoreElements;
		meta.ClimateColorMapId = climateColorMapIndex;
		meta.SeasonColorMapId = seasonColorMapIndex;
		switch (compositeShape.Format)
		{
		case EnumShapeFormat.Obj:
			objTesselator.Load(objs[compositeShape.Base], out modeldata, texSource["obj"], meta, -1);
			ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
			return;
		case EnumShapeFormat.GltfEmbedded:
		{
			TextureAtlasPosition textureAtlasPosition = ((texSource["gltf"] == game.api.BlockTextureAtlas[new AssetLocation("unknown")]) ? null : texSource["gltf"]);
			gltfTesselator.Load(gltfs[compositeShape.Base], out modeldata, textureAtlasPosition, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, -1, out var bakedTextures);
			if (compositeShape.InsertBakedTextures)
			{
				gltfs[compositeShape.Base].BaseTextures = new TextureAtlasPosition[bakedTextures.Length];
				gltfs[compositeShape.Base].PBRTextures = new TextureAtlasPosition[bakedTextures.Length];
				gltfs[compositeShape.Base].NormalTextures = new TextureAtlasPosition[bakedTextures.Length];
				for (int i = 0; i < bakedTextures.Length; i++)
				{
					byte[][] array = bakedTextures[i];
					if (array[0] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(array[0], out var _, out var texPos))
						{
							game.Logger.Debug("Failed adding baked in gltf base texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
							gltfs[compositeShape.Base].BaseTextures[i] = game.api.BlockTextureAtlas[new AssetLocation("unknown")];
						}
						else
						{
							gltfs[compositeShape.Base].BaseTextures[i] = texPos;
							if (textureAtlasPosition == null)
							{
								modeldata.SetTexPos(texPos);
							}
						}
					}
					if (array[1] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(array[1], out var _, out var texPos2))
						{
							game.Logger.Debug("Failed adding baked in gltf pbr texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
						}
						else
						{
							gltfs[compositeShape.Base].PBRTextures[i] = texPos2;
						}
					}
					if (array[2] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(array[2], out var _, out var texPos3))
						{
							game.Logger.Debug("Failed adding baked in gltf normal texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
						}
						else
						{
							gltfs[compositeShape.Base].NormalTextures[i] = texPos3;
						}
					}
				}
			}
			ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
			return;
		}
		}
		if (!shapes.TryGetValue(compositeShape.Base, out var value))
		{
			if (shapes.Count < 2)
			{
				throw new Exception("Something went wrong in the startup process, no " + type + " shapes have been loaded at all. Please try disabling all mods apart from Essentials, Survival, Creative. If that solves the issue, check which mod is causing this. If that does not solve the issue, please report.");
			}
			game.Logger.Error("Could not find shape {0} for {1} {2}", compositeShape.Base, type, sourceName);
			if (compositeShape.Base is AssetLocationAndSource assetLocationAndSource)
			{
				game.Logger.Notification(assetLocationAndSource.Source.ToString());
			}
			throw new FileNotFoundException(string.Concat("Could not find shape file: ", compositeShape.Base, " in ", type, "type ", sourceName, ".  Possibly a broken mod (", sourceName.Domain, ") or different versions of that mod between server and client?"));
		}
		if (!value.Loaded)
		{
			value.Load(game, new AssetLocationAndSource(compositeShape.Base));
		}
		rotationVec.Set(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ);
		offsetVec.Set(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
		TesselateShape(value, out modeldata, rotationVec, offsetVec, compositeShape.Scale, meta);
		if (compositeShape.Overlays != null)
		{
			for (int j = 0; j < compositeShape.Overlays.Length; j++)
			{
				CompositeShape compositeShape2 = compositeShape.Overlays[j];
				meta.QuantityElements = quantityElements;
				rotationVec.Set(compositeShape2.rotateX, compositeShape2.rotateY, compositeShape2.rotateZ);
				offsetVec.Set(compositeShape2.offsetX, compositeShape2.offsetY, compositeShape2.offsetZ);
				TesselateShape(shapes[compositeShape2.Base], out var modeldata2, rotationVec, offsetVec, compositeShape.Scale, meta);
				modeldata.AddMeshData(modeldata2);
			}
		}
	}

	public void TesselateShape(CollectibleObject collObj, Shape shape, out MeshData modeldata, Vec3f rotation = null, int? quantityElements = null, string[] selectiveElements = null)
	{
		if (collObj.ItemClass == EnumItemClass.Item)
		{
			TextureSource texSource = new TextureSource(game, game.ItemAtlasManager.Size, collObj as Item);
			TesselateShape("item shape", shape, out modeldata, texSource, rotation, 0, 0, 0, quantityElements, selectiveElements);
		}
		else
		{
			TextureSource texSource2 = new TextureSource(game, game.BlockAtlasManager.Size, collObj as Block);
			TesselateShape("block shape", shape, out modeldata, texSource2, rotation, 0, 0, 0, quantityElements, selectiveElements);
		}
	}

	public void TesselateShape(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f wholeMeshRotation = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null)
	{
		meta.TypeForLogging = typeForLogging;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = generalGlowLevel;
		meta.GeneralWindMode = 0;
		meta.ClimateColorMapId = climateColorMapId;
		meta.SeasonColorMapId = seasonColorMapId;
		meta.QuantityElements = quantityElements;
		meta.SelectiveElements = selectiveElements;
		meta.IgnoreElements = null;
		meta.WithJointIds = false;
		meta.WithDamageEffect = false;
		TesselateShape(shapeBase, out modeldata, wholeMeshRotation, null, 1f, meta);
	}

	public void TesselateShapeWithJointIds(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements, string[] selectiveElements)
	{
		meta.TypeForLogging = typeForLogging;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = 0;
		meta.GeneralWindMode = 0;
		meta.ClimateColorMapId = 0;
		meta.SeasonColorMapId = 0;
		meta.QuantityElements = quantityElements;
		meta.SelectiveElements = selectiveElements;
		meta.IgnoreElements = null;
		meta.WithJointIds = true;
		meta.WithDamageEffect = false;
		TesselateShape(shapeBase, out modeldata, rotation, null, 1f, meta);
	}

	public void TesselateShape(TesselationMetaData meta, Shape shapeBase, out MeshData modeldata)
	{
		this.meta.TypeForLogging = meta.TypeForLogging;
		this.meta.TexSource = meta.TexSource;
		this.meta.GeneralGlowLevel = meta.GeneralGlowLevel;
		this.meta.GeneralWindMode = meta.GeneralWindMode;
		this.meta.ClimateColorMapId = meta.ClimateColorMapId;
		this.meta.SeasonColorMapId = meta.SeasonColorMapId;
		this.meta.QuantityElements = meta.QuantityElements;
		this.meta.SelectiveElements = meta.SelectiveElements;
		this.meta.IgnoreElements = meta.IgnoreElements;
		this.meta.WithJointIds = meta.WithJointIds;
		this.meta.WithDamageEffect = meta.WithDamageEffect;
		TesselateShape(shapeBase, out modeldata, meta.Rotation, null, 1f, meta);
	}

	public void TesselateShape(Shape shapeBase, out MeshData modeldata, Vec3f wholeMeshRotation, Vec3f wholeMeshOffset, float wholeMeshScale, TesselationMetaData meta)
	{
		if (wholeMeshRotation == null)
		{
			wholeMeshRotation = noRotation;
		}
		modeldata = new MeshData(24, 36).WithColorMaps().WithRenderpasses();
		if (meta.WithJointIds)
		{
			modeldata.CustomInts = new CustomMeshDataPartInt();
			modeldata.CustomInts.InterleaveSizes = new int[1] { 1 };
			modeldata.CustomInts.InterleaveOffsets = new int[1];
			modeldata.CustomInts.InterleaveStride = 0;
			elementMeshData.CustomInts = new CustomMeshDataPartInt();
		}
		else
		{
			elementMeshData.CustomInts = null;
		}
		if (meta.WithDamageEffect)
		{
			modeldata.CustomFloats = new CustomMeshDataPartFloat();
			modeldata.CustomFloats.InterleaveSizes = new int[1] { 1 };
			modeldata.CustomFloats.InterleaveOffsets = new int[1];
			modeldata.CustomFloats.InterleaveStride = 0;
			elementMeshData.CustomFloats = new CustomMeshDataPartFloat();
		}
		stackMatrix.Clear();
		stackMatrix.PushIdentity();
		Dictionary<string, int[]> textureSizes = shapeBase.TextureSizes;
		meta.TexturesSizes = textureSizes;
		meta.defaultTextureSize = new int[2] { shapeBase.TextureWidth, shapeBase.TextureHeight };
		TesselateShapeElements(modeldata, shapeBase.Elements, meta);
		if (wholeMeshScale != 1f)
		{
			modeldata.Scale(constantCenterXZ, wholeMeshScale, wholeMeshScale, wholeMeshScale);
		}
		if (wholeMeshRotation.X != 0f || wholeMeshRotation.Y != 0f || wholeMeshRotation.Z != 0f)
		{
			modeldata.Rotate(constantCenter, wholeMeshRotation.X * ((float)Math.PI / 180f), wholeMeshRotation.Y * ((float)Math.PI / 180f), wholeMeshRotation.Z * ((float)Math.PI / 180f));
		}
		if (wholeMeshOffset != null && !wholeMeshOffset.IsZero)
		{
			modeldata.Translate(wholeMeshOffset);
		}
	}

	private void TesselateShapeElements(MeshData meshdata, ShapeElement[] elements, TesselationMetaData meta)
	{
		int num = 0;
		string[] childHaystackElements = null;
		foreach (ShapeElement shapeElement in elements)
		{
			if (meta.QuantityElements.HasValue && meta.QuantityElements-- <= 0)
			{
				break;
			}
			if (!SelectiveMatch(shapeElement.Name, meta.SelectiveElements, out var childHaystackElements2) || (meta.IgnoreElements != null && SelectiveMatch(shapeElement.Name, meta.IgnoreElements, out childHaystackElements)))
			{
				continue;
			}
			if (shapeElement.From == null || shapeElement.From.Length != 3)
			{
				ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + num + " has illegal from coordinates (not set or not length 3). Ignoring element.");
				break;
			}
			if (shapeElement.To == null || shapeElement.To.Length != 3)
			{
				ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + num + " has illegal to coordinates (not set or not length 3). Ignoring element.");
				break;
			}
			stackMatrix.Push();
			double num2;
			double num3;
			double num4;
			if (shapeElement.RotationOrigin == null)
			{
				num2 = 0.0;
				num3 = 0.0;
				num4 = 0.0;
			}
			else
			{
				num2 = shapeElement.RotationOrigin[0];
				num3 = shapeElement.RotationOrigin[1] * (double)meta.drawnHeight;
				num4 = shapeElement.RotationOrigin[2];
				stackMatrix.Translate(num2 / 16.0, num3 / 16.0, num4 / 16.0);
			}
			if (shapeElement.RotationX != 0.0)
			{
				stackMatrix.Rotate(shapeElement.RotationX * (Math.PI / 180.0), 1.0, 0.0, 0.0);
			}
			if (shapeElement.RotationY != 0.0)
			{
				stackMatrix.Rotate(shapeElement.RotationY * (Math.PI / 180.0), 0.0, 1.0, 0.0);
			}
			if (shapeElement.RotationZ != 0.0)
			{
				stackMatrix.Rotate(shapeElement.RotationZ * (Math.PI / 180.0), 0.0, 0.0, 1.0);
			}
			if (shapeElement.ScaleX != 1.0 || shapeElement.ScaleY != 1.0 || shapeElement.ScaleZ != 1.0)
			{
				stackMatrix.Scale(shapeElement.ScaleX, shapeElement.ScaleY, shapeElement.ScaleZ);
			}
			stackMatrix.Translate((shapeElement.From[0] - num2) / 16.0, (shapeElement.From[1] - num3) / 16.0, (shapeElement.From[2] - num4) / 16.0);
			if (shapeElement.HasFaces())
			{
				elementMeshData.Clear();
				TesselateShapeElement(num, elementMeshData, shapeElement, meta);
				elementMeshData.MatrixTransform(stackMatrix.Top);
				meshdata.AddMeshData(elementMeshData);
			}
			num++;
			if (shapeElement.Children != null)
			{
				TesselationMetaData tesselationMetaData = meta;
				if (childHaystackElements2 != null || childHaystackElements != null)
				{
					tesselationMetaData = meta.Clone();
					tesselationMetaData.SelectiveElements = childHaystackElements2;
					tesselationMetaData.IgnoreElements = childHaystackElements;
				}
				TesselateShapeElements(meshdata, shapeElement.Children, tesselationMetaData);
			}
			stackMatrix.Pop();
		}
	}

	private void TesselateShapeElement(int indexForLogging, MeshData meshdata, ShapeElement element, TesselationMetaData meta)
	{
		Size2i atlasSize = meta.TexSource.AtlasSize;
		xyzVec.Set((float)(element.To[0] - element.From[0]) / 16f, (float)(element.To[1] - element.From[1]) / 16f * meta.drawnHeight, (float)(element.To[2] - element.From[2]) / 16f);
		Vec3f vec3f = xyzVec;
		if (vec3f.IsZero)
		{
			return;
		}
		centerVec.Set(vec3f.X / 2f, vec3f.Y / 2f, vec3f.Z / 2f);
		Vec3f centerXyz = centerVec;
		byte b = 0;
		byte b2 = 0;
		short num = element.RenderPass;
		if (element.DisableRandomDrawOffset)
		{
			num += 1024;
		}
		bool flag = true;
		for (int i = 0; i < 6; i++)
		{
			ShapeElementFace shapeElementFace = element.FacesResolved[i];
			if (shapeElementFace == null)
			{
				continue;
			}
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			if (flag)
			{
				flag = false;
				b = ((element.ClimateColorMap == null || element.ClimateColorMap.Length == 0) ? meta.ClimateColorMapId : ((byte)(game.ColorMaps.IndexOfKey(element.ClimateColorMap) + 1)));
				b2 = (byte)((element.SeasonColorMap == null || element.SeasonColorMap.Length == 0) ? meta.SeasonColorMapId : (game.ColorMaps.TryGetValue(element.SeasonColorMap, out var value) ? ((byte)(value.RectIndex + 1)) : 0));
				meta.UsesColorMap |= b + b2 > 0;
			}
			float num2;
			float num3;
			float num4;
			float num5;
			if (shapeElementFace.Uv == null)
			{
				num2 = 0f;
				num3 = 0f;
				num4 = 0f;
				num5 = 0f;
				if (blockFacing.Axis == EnumAxis.Y)
				{
					num4 = vec3f.X * 16f;
					num5 = vec3f.Z * 16f;
				}
				else if (blockFacing.Axis == EnumAxis.X)
				{
					num4 = vec3f.Z * 16f;
					num5 = vec3f.Y * 16f;
				}
				else if (blockFacing.Axis == EnumAxis.Z)
				{
					num4 = vec3f.X * 16f;
					num5 = vec3f.Y * 16f;
				}
			}
			else
			{
				if (shapeElementFace.Uv.Length != 4)
				{
					ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ", shape element " + indexForLogging + ": Facing '" + blockFacing.Code + "' doesn't have exactly 4 uv values. Ignoring face.");
					continue;
				}
				num2 = shapeElementFace.Uv[0];
				num3 = shapeElementFace.Uv[3] + (shapeElementFace.Uv[1] - shapeElementFace.Uv[3]) * meta.drawnHeight;
				num4 = shapeElementFace.Uv[2];
				num5 = shapeElementFace.Uv[3];
			}
			string texture = shapeElementFace.Texture;
			TextureAtlasPosition textureAtlasPosition = meta.TexSource[texture];
			if (textureAtlasPosition == null)
			{
				throw new ArgumentNullException("Unable to find a texture for texture code '" + texture + "' in " + meta.TypeForLogging + ". Giving up. Sorry.");
			}
			if (!meta.TexturesSizes.TryGetValue(texture, out var value2))
			{
				value2 = meta.defaultTextureSize;
			}
			float num6 = (textureAtlasPosition.x2 - textureAtlasPosition.x1) * (float)atlasSize.Width / (float)value2[0];
			float num7 = (textureAtlasPosition.y2 - textureAtlasPosition.y1) * (float)atlasSize.Height / (float)value2[1];
			num2 *= num6;
			num3 *= num7;
			num4 *= num6;
			num5 *= num7;
			if (num3 == num5)
			{
				num5 += 1f / 32f;
			}
			if (num2 == num4)
			{
				num4 += 1f / 32f;
			}
			int num8 = (int)(shapeElementFace.Rotation / 90f);
			Vec2f vec2f = new Vec2f(textureAtlasPosition.x1 + num2 / (float)atlasSize.Width, textureAtlasPosition.y1 + num5 / (float)atlasSize.Height);
			Vec2f vec2f2 = new Vec2f((num4 - num2) / (float)atlasSize.Width, (num3 - num5) / (float)atlasSize.Height);
			vec2f2.X -= Math.Max(0f, vec2f.X + vec2f2.X - textureAtlasPosition.x2);
			vec2f2.Y -= Math.Max(0f, vec2f.Y + vec2f2.Y - textureAtlasPosition.y2);
			ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
			int num9 = ((element.ZOffset & 7) << 8) | (meta.GeneralGlowLevel + shapeElementFace.Glow);
			if (shapeElementFace.ReflectiveMode != EnumReflectiveMode.None)
			{
				num9 |= 0x800;
				sbyte b3 = (sbyte)Math.Max(0, (int)(shapeElementFace.ReflectiveMode - 1));
				shapeElementFace.WindData = new sbyte[4] { b3, b3, b3, b3 };
			}
			if (element.Shade)
			{
				num9 |= BlockFacing.AllVertexFlagsNormals[blockFacing.Index];
			}
			else if (element.GradientShade)
			{
				shade = ModelCubeUtilExt.EnumShadeMode.Gradient;
			}
			else
			{
				shade = ModelCubeUtilExt.EnumShadeMode.Off;
				num9 |= BlockFacing.UP.NormalPackedFlags;
			}
			flags[0] = (flags[1] = (flags[2] = (flags[3] = num9)));
			if (shapeElementFace.WindMode == null)
			{
				int num10 = meta.GeneralWindMode << 25;
				if (num10 != 0)
				{
					flags[0] |= num10;
					flags[1] |= num10;
					flags[2] |= num10;
					flags[3] |= num10;
					meshdata.HasAnyWindModeSet = true;
				}
			}
			else
			{
				for (int j = 0; j < flags.Length; j++)
				{
					int num11 = ((i / 2 == 1) ? shapeElementFace.WindMode[(j + 1) % flags.Length] : shapeElementFace.WindMode[j]);
					if (num11 > 0)
					{
						VertexFlags.SetWindMode(ref flags[j], num11);
						meshdata.HasAnyWindModeSet = true;
					}
				}
			}
			if (shapeElementFace.WindData != null)
			{
				for (int k = 0; k < flags.Length; k++)
				{
					int num12 = ((i / 2 == 1) ? shapeElementFace.WindData[(k + 1) % flags.Length] : shapeElementFace.WindData[k]);
					if (num12 > 0)
					{
						VertexFlags.SetWindData(ref flags[k], num12);
					}
				}
			}
			ModelCubeUtilExt.AddFace(meshdata, blockFacing, centerXyz, vec3f, vec2f, vec2f2, textureAtlasPosition.atlasTextureId, element.Color, shade, flags, 1f, num8 % 4, b, b2, num);
			if (meta.WithJointIds)
			{
				meshdata.CustomInts.Add(element.JointId, element.JointId, element.JointId, element.JointId);
			}
			if (meta.WithDamageEffect)
			{
				meshdata.CustomFloats.Add(element.DamageEffect, element.DamageEffect, element.DamageEffect, element.DamageEffect);
			}
		}
	}

	public void TesselateBlock(Block block, out MeshData meshdata)
	{
		TextureSource textureSource = new TextureSource(game, game.BlockAtlasManager.Size, block);
		TesselateBlock(block, out meshdata, textureSource);
	}

	public void TesselateBlock(Block block, out MeshData modeldata, TextureSource textureSource, int? quantityElements = null, string[] selectiveElements = null)
	{
		TesselateBlock(block, block.Shape, out modeldata, textureSource, quantityElements, selectiveElements);
	}

	public void TesselateBlock(Block block, CompositeShape compositeShape, out MeshData modeldata, TextureSource texSource, int? quantityElements = null, string[] selectiveElements = null)
	{
		byte climateColorMapIndex = (byte)((block.ClimateColorMapResolved != null) ? ((byte)(block.ClimateColorMapResolved.RectIndex + 1)) : 0);
		byte seasonColorMapIndex = (byte)((block.SeasonColorMapResolved != null) ? ((byte)(block.SeasonColorMapResolved.RectIndex + 1)) : 0);
		meta.GeneralWindMode = (int)block.VertexFlags.WindMode;
		meta.drawnHeight = ((block is IWithDrawnHeight { drawnHeight: >0 } withDrawnHeight) ? ((float)withDrawnHeight.drawnHeight / 48f) : 1f);
		TesselateShape("block", block.Code, compositeShape, out modeldata, texSource, block.VertexFlags.GlowLevel, climateColorMapIndex, seasonColorMapIndex, quantityElements, selectiveElements);
		meta.drawnHeight = 1f;
		if (compositeShape.Format == EnumShapeFormat.VintageStory)
		{
			block.ShapeUsesColormap |= meta.UsesColorMap || block.ClimateColorMap != null || block.SeasonColorMap != null;
		}
	}

	public void TesselateItem(Item item, out MeshData modeldata, ITexPositionSource texSource)
	{
		meta.GeneralWindMode = 0;
		if (item.Shape == null || item.Shape.VoxelizeTexture)
		{
			CompositeTexture compositeTexture = item.FirstTexture;
			if (item.Shape?.Base != null)
			{
				compositeTexture = item.Textures[item.Shape.Base.Path.ToString()];
			}
			BakedBitmap bakedBitmap = TextureAtlasManager.LoadCompositeBitmap(game, compositeTexture.Baked.BakedName.ToString());
			TextureAtlasPosition pos = texSource[compositeTexture.Baked.BakedName.ToString()];
			modeldata = VoxelizeTextureStatic(bakedBitmap.TexturePixels, bakedBitmap.Width, bakedBitmap.Height, pos);
		}
		else
		{
			TesselateShape("item", item.Code, item.Shape, out modeldata, texSource, 0, 0, 0);
		}
	}

	public void TesselateItem(Item item, out MeshData modeldata)
	{
		TesselateItem(item, item.Shape, out modeldata);
	}

	public void TesselateItem(Item item, CompositeShape forShape, out MeshData modeldata)
	{
		meta.GeneralWindMode = 0;
		if (item == null || item.Code == null)
		{
			modeldata = unknownItemModelData;
		}
		else if (forShape == null || forShape.VoxelizeTexture)
		{
			CompositeTexture value = item.FirstTexture;
			if (forShape?.Base != null && !item.Textures.TryGetValue(forShape.Base.ToShortString(), out value))
			{
				ScreenManager.Platform.Logger.Warning("Item {0} has no shape defined and has no texture definition. Will use unknown texture.", item.Code);
			}
			if (value != null)
			{
				int textureSubId = value.Baked.TextureSubId;
				TextureAtlasPosition pos = game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
				BakedBitmap bakedBitmap = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(value.Baked.BakedName, "Item code ", item.Code));
				modeldata = VoxelizeTextureStatic(bakedBitmap.TexturePixels, bakedBitmap.Width, bakedBitmap.Height, pos);
			}
			else
			{
				modeldata = unknownItemModelData;
			}
		}
		else
		{
			TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, item);
			TesselateShape("item", item.Code, forShape, out modeldata, texSource, 0, 0, 0);
		}
	}

	public static MeshData VoxelizeTextureStatic(int[] texturePixels, int width, int height, TextureAtlasPosition pos, Vec3f rotation = null)
	{
		MeshData meshData = new MeshData(20, 20);
		if (rotation == null)
		{
			rotation = new Vec3f();
		}
		if (pos == null)
		{
			pos = new TextureAtlasPosition();
		}
		float num = 1.5f;
		float num2 = pos.x2 - pos.x1;
		float num3 = pos.y2 - pos.y1;
		Vec3f vec3f = new Vec3f(0f, 0f, 0.5f);
		Vec3f sizeXyz = new Vec3f(num / (float)width, num / (float)height, num / 24f);
		Vec2f vec2f = new Vec2f(0f, 0f);
		Vec2f sizeUv = new Vec2f(num2 / (float)width, num3 / (float)height);
		int[] array = new int[6];
		ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
		for (int i = 0; i < 6; i++)
		{
			float faceBrightness = BlockFacing.ALLFACES[i].GetFaceBrightness(rotation.X, rotation.Y, rotation.Z, CubeMeshUtil.DefaultBlockSideShadingsByFacing);
			array[i] = ColorUtil.ColorMultiply3(-1, faceBrightness);
		}
		int atlasTextureId = pos.atlasTextureId;
		for (int j = 0; j < width; j++)
		{
			for (int k = 0; k < height; k++)
			{
				vec3f.X = num * (float)j / (float)width - (num - 1f) / 4f;
				vec3f.Y = num * (float)k / (float)height - (num - 1f) / 4f;
				vec2f.X = pos.x1 + num2 * (float)j / (float)width;
				vec2f.Y = pos.y1 + num3 * (float)k / (float)height;
				if (((texturePixels[k * width + j] >> 24) & 0xFF) > 5)
				{
					bool num4 = j > 0 && ((texturePixels[k * width + j - 1] >> 24) & 0xFF) > 5;
					bool flag = k > 0 && ((texturePixels[(k - 1) * width + j] >> 24) & 0xFF) > 5;
					bool num5 = j < width - 1 && ((texturePixels[k * width + j + 1] >> 24) & 0xFF) > 5;
					bool flag2 = k < height - 1 && ((texturePixels[(k + 1) * width + j] >> 24) & 0xFF) > 5;
					if (!num5)
					{
						ModelCubeUtilExt.AddFace(meshData, BlockFacing.EAST, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.EAST.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!num4)
					{
						ModelCubeUtilExt.AddFace(meshData, BlockFacing.WEST, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.WEST.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!flag2)
					{
						ModelCubeUtilExt.AddFace(meshData, BlockFacing.UP, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.DOWN.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!flag)
					{
						ModelCubeUtilExt.AddFace(meshData, BlockFacing.DOWN, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.UP.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					ModelCubeUtilExt.AddFace(meshData, BlockFacing.NORTH, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.SOUTH.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					ModelCubeUtilExt.AddFace(meshData, BlockFacing.SOUTH, vec3f, sizeXyz, vec2f, sizeUv, atlasTextureId, array[BlockFacing.NORTH.Index], shade, noFlags, 1f, 0, 0, 0, -1);
				}
			}
		}
		return meshData;
	}

	public MeshData VoxelizeTexture(CompositeTexture texture, Size2i atlasSize, TextureAtlasPosition atlasPos)
	{
		BakedBitmap bakedBitmap = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(texture.Baked.BakedName));
		return VoxelizeTextureStatic(bakedBitmap.TexturePixels, bakedBitmap.Width, bakedBitmap.Height, atlasPos);
	}

	public MeshData VoxelizeTexture(int[] texturePixels, int width, int height, Size2i atlasSize, TextureAtlasPosition atlasPos)
	{
		return VoxelizeTextureStatic(texturePixels, width, height, atlasPos);
	}

	public int AltTexturesCount(Block block)
	{
		int num = 0;
		foreach (CompositeTexture value in block.Textures.Values)
		{
			BakedCompositeTexture[] array = value.Baked?.BakedVariants;
			if (array != null && array.Length > num)
			{
				num = array.Length;
			}
		}
		return num;
	}

	public int TileTexturesCount(Block block)
	{
		int num = 0;
		foreach (CompositeTexture value in block.Textures.Values)
		{
			BakedCompositeTexture[] array = value.Baked?.BakedTiles;
			if (array != null && array.Length > num)
			{
				num = array.Length;
			}
		}
		return num;
	}

	public ITexPositionSource GetTexSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return GetTextureSource(block, altTextureNumber, returnNullWhenMissing);
	}

	public ITexPositionSource GetTextureSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.BlockAtlasManager.Size, block, altTextureNumber)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public ITexPositionSource GetTextureSource(Item item, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.ItemAtlasManager.Size, item)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public ITexPositionSource GetTextureSource(Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.EntityAtlasManager.Size, entity, extraTextures, altTextureNumber)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public void ApplyCompositeShapeModifiers(ref MeshData modeldata, CompositeShape compositeShape)
	{
		if (compositeShape.Scale != 1f)
		{
			modeldata.Scale(constantCenterXZ, compositeShape.Scale, compositeShape.Scale, compositeShape.Scale);
		}
		if (compositeShape.rotateX != 0f || compositeShape.rotateY != 0f || compositeShape.rotateZ != 0f)
		{
			modeldata.Rotate(constantCenter, compositeShape.rotateX * ((float)Math.PI / 180f), compositeShape.rotateY * ((float)Math.PI / 180f), compositeShape.rotateZ * ((float)Math.PI / 180f));
		}
		if (compositeShape.offsetX != 0f || compositeShape.offsetY != 0f || compositeShape.offsetZ != 0f)
		{
			modeldata.Translate(new Vec3f(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ));
		}
	}

	private bool SelectiveMatch(string needle, string[] haystackElements, out string[] childHaystackElements)
	{
		childHaystackElements = null;
		if (haystackElements == null)
		{
			return true;
		}
		for (int i = 0; i < haystackElements.Length; i++)
		{
			string text = haystackElements[i];
			if (text.Length == 0)
			{
				continue;
			}
			if (text == needle)
			{
				childHaystackElements = Array.Empty<string>();
				return true;
			}
			if (text == "*" || text.EqualsFast(needle + "/*") || (text[text.Length - 1] == '*' && needle.StartsWithFast(text.Substring(0, text.Length - 1))))
			{
				childHaystackElements = new string[1] { "*" };
				return true;
			}
			if (text.IndexOf('/') != needle.Length || !text.StartsWithFast(needle))
			{
				continue;
			}
			int num = 0;
			for (int j = i; j < haystackElements.Length; j++)
			{
				if (haystackElements[j].IndexOf('/') == needle.Length && haystackElements[j].StartsWithFast(needle))
				{
					num++;
				}
			}
			childHaystackElements = new string[num];
			if (num > 0)
			{
				int num2 = 0;
				for (int k = i; k < haystackElements.Length; k++)
				{
					text = haystackElements[k];
					int num3 = text.IndexOf('/');
					if (num3 == needle.Length && text.StartsWithFast(needle))
					{
						childHaystackElements[num2++] = text.Substring(num3 + 1);
					}
				}
			}
			return true;
		}
		return false;
	}
}
