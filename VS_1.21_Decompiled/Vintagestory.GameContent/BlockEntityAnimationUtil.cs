using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityAnimationUtil : AnimationUtil
{
	private BlockEntity be;

	public Action<MeshData> OnAfterTesselate;

	public BlockEntityAnimationUtil(ICoreAPI api, BlockEntity be)
		: base(api, be.Pos.ToVec3d())
	{
		this.be = be;
	}

	public virtual MeshData InitializeAnimator(string cacheDictKey, Shape shape = null, ITexPositionSource texSource = null, Vec3f rotationDeg = null)
	{
		Shape resultingShape;
		MeshData meshData = CreateMesh(cacheDictKey, shape, out resultingShape, texSource);
		InitializeAnimator(cacheDictKey, meshData, resultingShape, rotationDeg);
		return meshData;
	}

	public virtual MeshData CreateMesh(string nameForLogging, Shape shape, out Shape resultingShape, ITexPositionSource texSource)
	{
		if (api.Side != EnumAppSide.Client)
		{
			throw new NotImplementedException("Server side animation system not implemented yet.");
		}
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		Block block = api.World.BlockAccessor.GetBlock(be.Pos);
		if (texSource == null)
		{
			texSource = coreClientAPI.Tesselator.GetTextureSource(block);
		}
		if (shape == null)
		{
			AssetLocation assetLocation = block.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			shape = Shape.TryGet(api, assetLocation);
			if (shape == null)
			{
				api.World.Logger.Error("Shape for block {0} not found or errored, was supposed to be at {1}. Block animations not loaded!", be.Block.Code, assetLocation);
				resultingShape = shape;
				return new MeshData();
			}
		}
		Dictionary<string, ShapeElement> elementsByName = shape.CollectAndResolveReferences(api.World.Logger, nameForLogging);
		shape.CacheInvTransforms();
		shape.ResolveAndFindJoints(api.World.Logger, nameForLogging, elementsByName);
		TesselationMetaData meta = new TesselationMetaData
		{
			QuantityElements = block.Shape.QuantityElements,
			SelectiveElements = block.Shape.SelectiveElements,
			IgnoreElements = block.Shape.IgnoreElements,
			TexSource = texSource,
			WithJointIds = true,
			WithDamageEffect = true,
			TypeForLogging = nameForLogging
		};
		coreClientAPI.Tesselator.TesselateShape(meta, shape, out var modeldata);
		OnAfterTesselate?.Invoke(modeldata);
		resultingShape = shape;
		return modeldata;
	}

	public override void InitializeAnimatorServer(string cacheDictKey, Shape blockShape)
	{
		base.InitializeAnimatorServer(cacheDictKey, blockShape);
		be.RegisterGameTickListener(base.AnimationTickServer, 20);
	}

	protected override void OnAnimationsStateChange(bool animsNowActive)
	{
		if (animsNowActive)
		{
			if (renderer != null)
			{
				api.World.BlockAccessor.MarkBlockDirty(be.Pos, delegate
				{
					renderer.ShouldRender = true;
				});
			}
		}
		else
		{
			api.World.BlockAccessor.MarkBlockDirty(be.Pos, delegate
			{
				renderer.ShouldRender = false;
			});
		}
	}
}
