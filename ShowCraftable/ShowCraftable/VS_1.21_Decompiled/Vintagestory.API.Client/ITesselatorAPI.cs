using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface ITesselatorAPI
{
	void TesselateBlock(Block block, out MeshData modeldata);

	void TesselateItem(Item item, out MeshData modeldata);

	void TesselateItem(Item item, CompositeShape forShape, out MeshData modeldata);

	void TesselateItem(Item item, out MeshData modeldata, ITexPositionSource texSource);

	void TesselateShape(string type, AssetLocation name, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[] selectiveElements = null);

	void TesselateShape(CollectibleObject textureSourceCollectible, Shape shape, out MeshData modeldata, Vec3f meshRotationDeg = null, int? quantityElements = null, string[] selectiveElements = null);

	void TesselateShape(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f meshRotationDeg = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null);

	void TesselateShapeWithJointIds(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements = null, string[] selectiveElements = null);

	void TesselateShape(TesselationMetaData meta, Shape shapeBase, out MeshData modeldata);

	MeshData VoxelizeTexture(CompositeTexture texture, Size2i atlasSize, TextureAtlasPosition atlasPos);

	MeshData VoxelizeTexture(int[] texturePixels, int width, int height, Size2i atlasSize, TextureAtlasPosition pos);

	ITexPositionSource GetTextureSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false);

	[Obsolete("Use GetTextureSource instead")]
	ITexPositionSource GetTexSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false);

	ITexPositionSource GetTextureSource(Item item, bool returnNullWhenMissing = false);

	ITexPositionSource GetTextureSource(Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0, bool returnNullWhenMissing = false);
}
