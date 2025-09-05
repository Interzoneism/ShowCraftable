using System;
using System.Collections.Generic;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorExtraSkinnable : EntityBehavior
{
	public Dictionary<string, SkinnablePart> AvailableSkinPartsByCode = new Dictionary<string, SkinnablePart>();

	public SkinnablePart[] AvailableSkinParts;

	public string VoiceType = "altoflute";

	public string VoicePitch = "medium";

	public string mainTextureCode;

	public List<AppliedSkinnablePartVariant> appliedTemp = new List<AppliedSkinnablePartVariant>();

	protected ITreeAttribute skintree;

	private bool didInit;

	public IReadOnlyList<AppliedSkinnablePartVariant> AppliedSkinParts
	{
		get
		{
			appliedTemp.Clear();
			ITreeAttribute treeAttribute = skintree.GetTreeAttribute("appliedParts");
			if (treeAttribute == null)
			{
				return appliedTemp;
			}
			SkinnablePart[] availableSkinParts = AvailableSkinParts;
			foreach (SkinnablePart skinnablePart in availableSkinParts)
			{
				string text = treeAttribute.GetString(skinnablePart.Code);
				if (text != null && skinnablePart.VariantsByCode.TryGetValue(text, out var value))
				{
					appliedTemp.Add(value.AppliedCopy(skinnablePart.Code));
				}
			}
			return appliedTemp;
		}
	}

	public EntityBehaviorExtraSkinnable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		base.Initialize(properties, attributes);
		skintree = entity.WatchedAttributes.GetTreeAttribute("skinConfig");
		if (skintree == null)
		{
			entity.WatchedAttributes["skinConfig"] = (skintree = new TreeAttribute());
		}
		mainTextureCode = properties.Attributes["mainTextureCode"].AsString("seraph");
		entity.WatchedAttributes.RegisterModifiedListener("skinConfig", onSkinConfigChanged);
		entity.WatchedAttributes.RegisterModifiedListener("voicetype", onVoiceConfigChanged);
		entity.WatchedAttributes.RegisterModifiedListener("voicepitch", onVoiceConfigChanged);
		AvailableSkinParts = properties.Attributes["skinnableParts"].AsObject<SkinnablePart[]>();
		AvailableSkinParts = entity.Api.ModLoader.GetModSystem<ModSystemSkinnableAdditions>().AppendAdditions(AvailableSkinParts);
		SkinnablePart[] availableSkinParts = AvailableSkinParts;
		foreach (SkinnablePart skinnablePart in availableSkinParts)
		{
			_ = skinnablePart.Code;
			skinnablePart.VariantsByCode = new Dictionary<string, SkinnablePartVariant>();
			AvailableSkinPartsByCode[skinnablePart.Code] = skinnablePart;
			if (skinnablePart.Type == EnumSkinnableType.Texture && entity.Api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI coreClientAPI = entity.Api as ICoreClientAPI;
				new LoadedTexture(coreClientAPI);
				SkinnablePartVariant[] variants = skinnablePart.Variants;
				foreach (SkinnablePartVariant skinnablePartVariant in variants)
				{
					AssetLocation assetLocation;
					if (skinnablePart.TextureTemplate != null)
					{
						assetLocation = skinnablePart.TextureTemplate.Clone();
						assetLocation.Path = assetLocation.Path.Replace("{code}", skinnablePartVariant.Code);
					}
					else
					{
						assetLocation = skinnablePartVariant.Texture;
					}
					IAsset asset = coreClientAPI.Assets.TryGet(assetLocation.Clone().WithPathAppendixOnce(".png").WithPathPrefixOnce("textures/"));
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					float num4 = 0f;
					BitmapRef bitmapRef = asset.ToBitmap(coreClientAPI);
					for (int k = 0; k < 8; k++)
					{
						Vec2d vec2d = GameMath.R2Sequence2D(k);
						SKColor pixelRel = bitmapRef.GetPixelRel((float)vec2d.X, (float)vec2d.Y);
						if ((double)(int)((SKColor)(ref pixelRel)).Alpha > 0.5)
						{
							num += ((SKColor)(ref pixelRel)).Red;
							num2 += ((SKColor)(ref pixelRel)).Green;
							num3 += ((SKColor)(ref pixelRel)).Blue;
							num4 += 1f;
						}
					}
					bitmapRef.Dispose();
					num4 = Math.Max(1f, num4);
					skinnablePartVariant.Color = ColorUtil.ColorFromRgba((int)((float)num3 / num4), (int)((float)num2 / num4), (int)((float)num / num4), 255);
					skinnablePart.VariantsByCode[skinnablePartVariant.Code] = skinnablePartVariant;
				}
			}
			else
			{
				SkinnablePartVariant[] variants = skinnablePart.Variants;
				foreach (SkinnablePartVariant skinnablePartVariant2 in variants)
				{
					skinnablePart.VariantsByCode[skinnablePartVariant2.Code] = skinnablePartVariant2;
				}
			}
		}
		if (entity.Api.Side == EnumAppSide.Server && AppliedSkinParts.Count == 0)
		{
			entity.Api.ModLoader.GetModSystem<CharacterSystem>().randomizeSkin(entity, null, playVoice: false);
		}
		onVoiceConfigChanged();
	}

	private void onSkinConfigChanged()
	{
		skintree = entity.WatchedAttributes["skinConfig"] as ITreeAttribute;
		entity.MarkShapeModified();
	}

	private void onVoiceConfigChanged()
	{
		VoiceType = entity.WatchedAttributes.GetString("voicetype");
		VoicePitch = entity.WatchedAttributes.GetString("voicepitch");
		ApplyVoice(VoiceType, VoicePitch, testTalk: false);
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		init();
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		init();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		EntityBehaviorTexturedClothing behavior = entity.GetBehavior<EntityBehaviorTexturedClothing>();
		if (behavior != null)
		{
			behavior.OnReloadSkin -= Essr_OnReloadSkin;
		}
	}

	private void init()
	{
		if (entity.World.Side == EnumAppSide.Client && !didInit)
		{
			if (!(entity.Properties.Client.Renderer is EntityShapeRenderer))
			{
				throw new InvalidOperationException("The extra skinnable entity behavior requires the entity to use the Shape renderer.");
			}
			(entity.GetBehavior<EntityBehaviorTexturedClothing>() ?? throw new InvalidOperationException("The extra skinnable entity behavior requires the entity to have the TextureClothing entitybehavior.")).OnReloadSkin += Essr_OnReloadSkin;
			didInit = true;
		}
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		if (!shapeIsCloned)
		{
			Shape shape = entityShape.Clone();
			entityShape = shape;
			shapeIsCloned = true;
		}
		foreach (AppliedSkinnablePartVariant appliedSkinPart in AppliedSkinParts)
		{
			AvailableSkinPartsByCode.TryGetValue(appliedSkinPart.PartCode, out var value);
			if (value != null && value.Type == EnumSkinnableType.Shape)
			{
				entityShape = addSkinPart(appliedSkinPart, entityShape, value.DisableElements, shapePathForLogging);
			}
		}
		foreach (AppliedSkinnablePartVariant appliedSkinPart2 in AppliedSkinParts)
		{
			AvailableSkinPartsByCode.TryGetValue(appliedSkinPart2.PartCode, out var value2);
			if (value2 != null && value2.Type == EnumSkinnableType.Texture && value2.TextureTarget != null && value2.TextureTarget != mainTextureCode)
			{
				AssetLocation assetLocation;
				if (value2.TextureTemplate != null)
				{
					assetLocation = value2.TextureTemplate.Clone();
					assetLocation.Path = assetLocation.Path.Replace("{code}", appliedSkinPart2.Code);
				}
				else
				{
					assetLocation = appliedSkinPart2.Texture;
				}
				string text = "skinpart-" + value2.TextureTarget;
				entityShape.TextureSizes.TryGetValue(text, out var value3);
				if (value3 != null)
				{
					loadTexture(entityShape, text, assetLocation, value3[0], value3[1], shapePathForLogging);
				}
				else
				{
					entity.Api.Logger.Error("Skinpart has no textureSize: " + text + " in: " + shapePathForLogging);
				}
			}
		}
		EntityBehaviorTexturedClothing behavior = entity.GetBehavior<EntityBehaviorTexturedClothing>();
		InventoryBase inventory = behavior.Inventory;
		if (inventory == null)
		{
			return;
		}
		foreach (ItemSlot item in inventory)
		{
			if (item.Empty || behavior.hideClothing)
			{
				continue;
			}
			JsonObject attributes = item.Itemstack.Collectible.Attributes;
			entityShape.RemoveElements(attributes?["disableElements"]?.AsArray<string>());
			string[] array = attributes?["keepElements"]?.AsArray<string>();
			if (array != null && willDeleteElements != null)
			{
				string[] array2 = array;
				foreach (string value4 in array2)
				{
					willDeleteElements = willDeleteElements.Remove(value4);
				}
			}
		}
	}

	private void Essr_OnReloadSkin(LoadedTexture atlas, TextureAtlasPosition skinTexPos, int textureSubId)
	{
		ICoreClientAPI coreClientAPI = entity.World.Api as ICoreClientAPI;
		foreach (AppliedSkinnablePartVariant appliedSkinPart in AppliedSkinParts)
		{
			SkinnablePart skinnablePart = AvailableSkinPartsByCode[appliedSkinPart.PartCode];
			if (skinnablePart.Type == EnumSkinnableType.Texture && (skinnablePart.TextureTarget == null || !(skinnablePart.TextureTarget != mainTextureCode)))
			{
				LoadedTexture intoTexture = new LoadedTexture(coreClientAPI);
				coreClientAPI.Render.GetOrLoadTexture(appliedSkinPart.Texture.Clone().WithPathAppendixOnce(".png"), ref intoTexture);
				int num = skinnablePart.TextureRenderTo?.X ?? 0;
				int num2 = skinnablePart.TextureRenderTo?.Y ?? 0;
				coreClientAPI.EntityTextureAtlas.RenderTextureIntoAtlas(skinTexPos.atlasTextureId, intoTexture, 0f, 0f, intoTexture.Width, intoTexture.Height, skinTexPos.x1 * (float)coreClientAPI.EntityTextureAtlas.Size.Width + (float)num, skinTexPos.y1 * (float)coreClientAPI.EntityTextureAtlas.Size.Height + (float)num2, (skinnablePart.Code == "baseskin") ? (-1f) : 0.005f);
			}
		}
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		textures[mainTextureCode].Baked.TextureSubId = textureSubId;
		textures["skinpart-" + mainTextureCode] = textures[mainTextureCode];
	}

	public void selectSkinPart(string partCode, string variantCode, bool retesselateShape = true, bool playVoice = true)
	{
		AvailableSkinPartsByCode.TryGetValue(partCode, out var value);
		ITreeAttribute treeAttribute = skintree.GetTreeAttribute("appliedParts");
		if (treeAttribute == null)
		{
			treeAttribute = (ITreeAttribute)(skintree["appliedParts"] = new TreeAttribute());
		}
		treeAttribute[partCode] = new StringAttribute(variantCode);
		if (value != null && value.Type == EnumSkinnableType.Voice)
		{
			entity.WatchedAttributes.SetString(partCode, variantCode);
			if (partCode == "voicetype")
			{
				VoiceType = variantCode;
			}
			if (partCode == "voicepitch")
			{
				VoicePitch = variantCode;
			}
			ApplyVoice(VoiceType, VoicePitch, playVoice);
		}
		else
		{
			EntityShapeRenderer entityShapeRenderer = entity.Properties.Client.Renderer as EntityShapeRenderer;
			if (retesselateShape)
			{
				entityShapeRenderer?.TesselateShape();
			}
		}
	}

	public void ApplyVoice(string voiceType, string voicePitch, bool testTalk)
	{
		if (!AvailableSkinPartsByCode.TryGetValue("voicetype", out var value) || !AvailableSkinPartsByCode.TryGetValue("voicepitch", out var _))
		{
			return;
		}
		VoiceType = voiceType;
		VoicePitch = voicePitch;
		if (entity is EntityPlayer { talkUtil: not null } entityPlayer && voiceType != null)
		{
			if (!value.VariantsByCode.ContainsKey(voiceType))
			{
				voiceType = value.Variants[0].Code;
			}
			entityPlayer.talkUtil.soundName = value.VariantsByCode[voiceType].Sound;
			float pitchModifier = 1f;
			switch (VoicePitch)
			{
			case "verylow":
				pitchModifier = 0.6f;
				break;
			case "low":
				pitchModifier = 0.8f;
				break;
			case "medium":
				pitchModifier = 1f;
				break;
			case "high":
				pitchModifier = 1.2f;
				break;
			case "veryhigh":
				pitchModifier = 1.4f;
				break;
			}
			entityPlayer.talkUtil.pitchModifier = pitchModifier;
			entityPlayer.talkUtil.chordDelayMul = 1.1f;
			if (testTalk)
			{
				entityPlayer.talkUtil.Talk(EnumTalkType.Idle);
			}
		}
	}

	protected Shape addSkinPart(AppliedSkinnablePartVariant part, Shape entityShape, string[] disableElements, string shapePathForLogging)
	{
		SkinnablePart skinpart = AvailableSkinPartsByCode[part.PartCode];
		if (skinpart.Type == EnumSkinnableType.Voice)
		{
			entity.WatchedAttributes.SetString("voicetype", part.Code);
			return entityShape;
		}
		entityShape.RemoveElements(disableElements);
		ICoreAPI api = entity.World.Api;
		ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
		CompositeShape shapeTemplate = skinpart.ShapeTemplate;
		AssetLocation assetLocation;
		if (part.Shape == null && shapeTemplate != null)
		{
			assetLocation = shapeTemplate.Base.CopyWithPath("shapes/" + shapeTemplate.Base.Path + ".json");
			assetLocation.Path = assetLocation.Path.Replace("{code}", part.Code);
		}
		else
		{
			assetLocation = part.Shape.Base.CopyWithPath("shapes/" + part.Shape.Base.Path + ".json");
		}
		Shape shape = Shape.TryGet(api, assetLocation);
		if (shape == null)
		{
			api.World.Logger.Warning("Entity skin shape {0} defined in entity config {1} not found or errored, was supposed to be at {2}. Skin part will be invisible.", assetLocation, entity.Properties.Code, assetLocation);
			return null;
		}
		string prefixcode = "skinpart";
		shape.SubclassForStepParenting(prefixcode + "-");
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		entityShape.StepParentShape(shape, assetLocation.ToShortString(), shapePathForLogging, api.Logger, delegate(string texcode, AssetLocation loc)
		{
			if (capi != null && !textures.ContainsKey("skinpart-" + texcode) && skinpart.TextureRenderTo == null)
			{
				CompositeTexture compositeTexture = (textures[prefixcode + "-" + texcode] = new CompositeTexture(loc));
				CompositeTexture compositeTexture3 = compositeTexture;
				compositeTexture3.Bake(api.Assets);
				capi.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _);
				compositeTexture3.Baked.TextureSubId = textureSubId;
			}
		});
		return entityShape;
	}

	private void loadTexture(Shape entityShape, string code, AssetLocation location, int textureWidth, int textureHeight, string shapePathForLogging)
	{
		if (entity.World.Side != EnumAppSide.Server)
		{
			IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
			ICoreClientAPI coreClientAPI = entity.World.Api as ICoreClientAPI;
			CompositeTexture compositeTexture = (textures[code] = new CompositeTexture(location));
			CompositeTexture compositeTexture3 = compositeTexture;
			compositeTexture3.Bake(coreClientAPI.Assets);
			if (!coreClientAPI.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _, null, -1f))
			{
				coreClientAPI.Logger.Warning("Skin part shape {0} defined texture {1}, no such texture found.", shapePathForLogging, location);
			}
			compositeTexture3.Baked.TextureSubId = textureSubId;
			entityShape.TextureSizes[code] = new int[2] { textureWidth, textureHeight };
			textures[code] = compositeTexture3;
		}
	}

	public override string PropertyName()
	{
		return "skinnableplayer";
	}
}
