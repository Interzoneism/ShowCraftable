using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemCreatureInventory : Item, ITexPositionSource
{
	private ICoreClientAPI capi;

	private EntityProperties nowTesselatingEntityType;

	private static Dictionary<EnumItemRenderTarget, string> map = new Dictionary<EnumItemRenderTarget, string>
	{
		{
			EnumItemRenderTarget.Ground,
			"groundTransform"
		},
		{
			EnumItemRenderTarget.HandTp,
			"tpHandTransform"
		},
		{
			EnumItemRenderTarget.Gui,
			"guiTransform"
		},
		{
			EnumItemRenderTarget.HandTpOff,
			"tpOffHandTransform"
		}
	};

	public Size2i AtlasSize => capi.ItemTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			nowTesselatingEntityType.Client.Textures.TryGetValue(textureCode, out var value);
			AssetLocation value2;
			if (value == null)
			{
				nowTesselatingEntityType.Client.LoadedShape.Textures.TryGetValue(textureCode, out value2);
			}
			else
			{
				value2 = value.Base;
			}
			if (value2 != null)
			{
				capi.ItemTextureAtlas.GetOrInsertTexture(value2, out var _, out var texPos);
				return texPos;
			}
			return null;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		List<JsonItemStack> list = new List<JsonItemStack>();
		foreach (EntityProperties entityType in api.World.EntityTypes)
		{
			JsonObject attributes = entityType.Attributes;
			if (attributes == null || attributes["inCreativeInventory"].AsBool(defaultValue: true))
			{
				JsonItemStack jsonItemStack = new JsonItemStack
				{
					Code = Code,
					Type = EnumItemClass.Item,
					Attributes = new JsonObject(JToken.Parse(string.Concat("{ \"type\": \"", entityType.Code, "\" }")))
				};
				jsonItemStack.Resolve(api.World, "creatureinventory");
				list.Add(jsonItemStack);
			}
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = list.ToArray(),
				Tabs = new string[3] { "general", "items", "creatures" }
			}
		};
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "itemcreatureinventorymeshes", () => new Dictionary<string, MultiTextureMeshRef>());
		string text = itemstack.Attributes.GetString("type");
		if (!orCreate.ContainsKey(text))
		{
			AssetLocation entityCode = new AssetLocation(text);
			EntityProperties entityProperties = (nowTesselatingEntityType = api.World.GetEntityType(entityCode));
			Shape loadedShape = entityProperties.Client.LoadedShape;
			if (loadedShape != null)
			{
				capi.Tesselator.TesselateShape("itemcreatureinventory", loadedShape, out var modeldata, this, null, 0, 0, 0);
				ModelTransform modelTransform = entityProperties.Attributes?[map[target]]?.AsObject<ModelTransform>();
				if (modelTransform != null)
				{
					modeldata.ModelTransform(modelTransform);
				}
				ItemRenderInfo obj = renderinfo;
				MultiTextureMeshRef modelRef = (orCreate[text] = capi.Render.UploadMultiTextureMesh(modeldata));
				obj.ModelRef = modelRef;
			}
		}
		else
		{
			renderinfo.ModelRef = orCreate[text];
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		if (!(byEntity is EntityPlayer) || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			slot.TakeOut(1);
			slot.MarkDirty();
		}
		AssetLocation assetLocation = new AssetLocation(slot.Itemstack.Attributes.GetString("type"));
		EntityProperties entityType = byEntity.World.GetEntityType(assetLocation);
		if (entityType == null)
		{
			byEntity.World.Logger.Error("ItemCreature: No such entity - {0}", assetLocation);
			if (api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", $"No such entity loaded - '{assetLocation}'.");
			}
			return;
		}
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		if (entity == null)
		{
			return;
		}
		entity.ServerPos.X = (float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f;
		entity.ServerPos.Y = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
		entity.ServerPos.Z = (float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f;
		entity.ServerPos.Yaw = (float)byEntity.World.Rand.NextDouble() * 2f * (float)Math.PI;
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "playerplaced");
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("setGuardedEntityAttribute"))
		{
			entity.WatchedAttributes.SetLong("guardedEntityId", byEntity.EntityId);
			if (byEntity is EntityPlayer entityPlayer)
			{
				entity.WatchedAttributes.SetString("guardedPlayerUid", entityPlayer.PlayerUID);
			}
		}
		byEntity.World.SpawnEntity(entity);
		handHandling = EnumHandHandling.PreventDefaultAction;
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity byEntity, EnumHand hand)
	{
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation(Code.Domain, CodeEndWithoutParts(1)));
		if (entityType == null)
		{
			return base.GetHeldTpIdleAnimation(activeHotbarSlot, byEntity, hand);
		}
		if (Math.Max(entityType.CollisionBoxSize.X, entityType.CollisionBoxSize.Y) > 1f)
		{
			return "holdunderarm";
		}
		return "holdbothhands";
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
