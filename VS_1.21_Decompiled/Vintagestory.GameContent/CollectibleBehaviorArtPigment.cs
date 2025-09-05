using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorArtPigment : CollectibleBehavior
{
	private EnumBlockMaterial[] paintableOnBlockMaterials;

	private MeshRef[] meshes;

	private TextureAtlasPosition texPos;

	private SkillItem[] toolModes;

	private List<Block> decorBlocks = new List<Block>();

	private string[] onmaterialsStrTmp;

	private AssetLocation[] decorCodesTmp;

	private bool requireSprintKey = true;

	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	private float consumeChance;

	public CollectibleBehaviorArtPigment(CollectibleObject collObj)
		: base(collObj)
	{
		base.collObj = collObj;
	}

	public override void Initialize(JsonObject properties)
	{
		onmaterialsStrTmp = properties["paintableOnBlockMaterials"].AsArray(Array.Empty<string>());
		decorCodesTmp = properties["decorBlockCodes"].AsObject(Array.Empty<AssetLocation>(), collObj.Code.Domain);
		consumeChance = properties["consumeChance"].AsFloat(0.15f);
		requireSprintKey = properties["requireSprintKey"]?.AsBool(defaultValue: true) ?? true;
		base.Initialize(properties);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		paintableOnBlockMaterials = new EnumBlockMaterial[onmaterialsStrTmp.Length];
		for (int i = 0; i < onmaterialsStrTmp.Length; i++)
		{
			if (onmaterialsStrTmp[i] != null)
			{
				try
				{
					paintableOnBlockMaterials[i] = (EnumBlockMaterial)Enum.Parse(typeof(EnumBlockMaterial), onmaterialsStrTmp[i]);
				}
				catch (Exception)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, paintable on material {1} is not a valid block material, will default to stone", collObj.Code, onmaterialsStrTmp[i]);
					paintableOnBlockMaterials[i] = EnumBlockMaterial.Stone;
				}
			}
		}
		onmaterialsStrTmp = null;
		ICoreClientAPI capi = api as ICoreClientAPI;
		AssetLocation[] array = decorCodesTmp;
		foreach (AssetLocation assetLocation in array)
		{
			if (assetLocation.Path.Contains('*'))
			{
				Block[] array2 = (from block2 in api.World.SearchBlocks(assetLocation)
					orderby block2.Variant["col"].ToInt() + 1000 * block2.Variant["row"].ToInt()
					select block2).ToArray();
				Block[] array3 = array2;
				foreach (Block item in array3)
				{
					decorBlocks.Add(item);
				}
				if (array2.Length == 0)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, decor {1}, no such block using this wildcard found", collObj.Code, assetLocation);
				}
			}
			else
			{
				Block block = api.World.GetBlock(assetLocation);
				if (block == null)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, decor {1} is not a loaded block", collObj.Code, assetLocation);
				}
				else
				{
					decorBlocks.Add(block);
				}
			}
		}
		if (api.Side == EnumAppSide.Client)
		{
			if (decorBlocks.Count > 0)
			{
				BakedCompositeTexture baked = decorBlocks[0].Textures["up"].Baked;
				texPos = capi.BlockTextureAtlas.Positions[baked.TextureSubId];
			}
			else
			{
				texPos = capi.BlockTextureAtlas.UnknownTexturePosition;
			}
		}
		AssetLocation code = collObj.Code;
		toolModes = new SkillItem[decorBlocks.Count];
		for (int num2 = 0; num2 < toolModes.Length; num2++)
		{
			toolModes[num2] = new SkillItem
			{
				Code = code.CopyWithPath("art" + num2),
				Linebreak = (num2 % GlobalConstants.CaveArtColsPerRow == 0),
				Name = "",
				Data = decorBlocks[num2],
				RenderHandler = delegate(AssetLocation assetLocation2, float dt, double atPosX, double atPosY)
				{
					float num4 = (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
					string s = assetLocation2.Path.Substring(3);
					capi.Render.Render2DTexture(meshes[int.Parse(s)], texPos.atlasTextureId, (float)atPosX, (float)atPosY, num4, num4);
				}
			};
		}
		if (capi != null)
		{
			meshes = new MeshRef[decorBlocks.Count];
			for (int num3 = 0; num3 < meshes.Length; num3++)
			{
				MeshData data = genMesh(num3);
				meshes[num3] = capi.Render.UploadMesh(data);
			}
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (api is ICoreClientAPI && meshes != null)
		{
			for (int i = 0; i < meshes.Length; i++)
			{
				meshes[i]?.Dispose();
			}
		}
	}

	public MeshData genMesh(int index)
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		float x = texPos.x1;
		float y = texPos.y1;
		float x2 = texPos.x2;
		float y2 = texPos.y2;
		float num = (x2 - x) / (float)GlobalConstants.CaveArtColsPerRow;
		float num2 = (y2 - y) / (float)GlobalConstants.CaveArtColsPerRow;
		x += (float)(index % GlobalConstants.CaveArtColsPerRow) * num;
		y += (float)(index / GlobalConstants.CaveArtColsPerRow) * num2;
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(quadVertices[i * 3], quadVertices[i * 3 + 1], quadVertices[i * 3 + 2], x + (float)(1 - quadTextureCoords[i * 2]) * num, y + (float)quadTextureCoords[i * 2 + 1] * num2);
		}
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		return meshData;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		if (!(blockSel?.Position == null))
		{
			IPlayer player = (byEntity as EntityPlayer)?.Player;
			if (byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) && player != null && (!requireSprintKey || player.Entity.Controls.CtrlKey) && SuitablePosition(byEntity.World.BlockAccessor, blockSel))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel?.Position == null)
		{
			return false;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		if (player == null || (requireSprintKey && !player.Entity.Controls.CtrlKey))
		{
			return false;
		}
		if (!SuitablePosition(byEntity.World.BlockAccessor, blockSel))
		{
			return false;
		}
		handling = EnumHandling.PreventSubsequent;
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel?.Position == null)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) || player == null || (requireSprintKey && !player.Entity.Controls.CtrlKey))
		{
			return;
		}
		IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
		if (SuitablePosition(blockAccessor, blockSel))
		{
			handling = EnumHandling.PreventDefault;
			DrawCaveArt(blockSel, blockAccessor, player);
			if (byEntity.World.Side == EnumAppSide.Server && byEntity.World.Rand.NextDouble() < (double)consumeChance)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw"), (double)blockSel.Position.X + blockSel.HitPosition.X, (double)blockSel.Position.InternalY + blockSel.HitPosition.Y, (double)blockSel.Position.Z + blockSel.HitPosition.Z, player, randomizePitch: true, 8f);
		}
	}

	private void DrawCaveArt(BlockSelection blockSel, IBlockAccessor blockAccessor, IPlayer byPlayer)
	{
		int toolMode = GetToolMode(null, byPlayer, blockSel);
		Block block = (Block)toolModes[toolMode].Data;
		blockAccessor.SetDecor(block, blockSel.Position, blockSel.ToDecorIndex());
	}

	public static int BlockSelectionToSubPosition(BlockFacing face, Vec3i voxelPos)
	{
		return new DecorBits(face, voxelPos.X, 15 - voxelPos.Y, voxelPos.Z);
	}

	private bool SuitablePosition(IBlockAccessor blockAccessor, BlockSelection blockSel)
	{
		Block block = blockAccessor.GetBlock(blockSel.Position);
		if (block.SideSolid[blockSel.Face.Index])
		{
			goto IL_005c;
		}
		if (block is BlockMicroBlock)
		{
			BlockEntityMicroBlock obj = blockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
			if (obj != null && obj.sideAlmostSolid[blockSel.Face.Index])
			{
				goto IL_005c;
			}
		}
		goto IL_008b;
		IL_008b:
		return false;
		IL_005c:
		EnumBlockMaterial blockMaterial = block.GetBlockMaterial(blockAccessor, blockSel.Position);
		for (int i = 0; i < paintableOnBlockMaterials.Length; i++)
		{
			if (blockMaterial == paintableOnBlockMaterials[i])
			{
				return true;
			}
		}
		goto IL_008b;
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (!requireSprintKey)
		{
			return toolModes;
		}
		if (blockSel == null)
		{
			return null;
		}
		IBlockAccessor blockAccessor = forPlayer.Entity.World.BlockAccessor;
		if (!SuitablePosition(blockAccessor, blockSel))
		{
			return null;
		}
		return toolModes;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer?.Entity == null)
		{
			return 0;
		}
		return byPlayer.Entity.WatchedAttributes.GetInt("toolModeCaveArt");
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		byPlayer?.Entity.WatchedAttributes.SetInt("toolModeCaveArt", toolMode);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "ctrl",
				ActionLangCode = "heldhelp-draw",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
	}
}
