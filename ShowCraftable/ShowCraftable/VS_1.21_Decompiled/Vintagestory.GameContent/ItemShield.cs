using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemShield : Item, IContainedMeshSource, IAttachableToEntity
{
	private float offY;

	private float curOffY;

	private ICoreClientAPI capi;

	private Dictionary<string, Dictionary<string, int>> durabilityGains;

	private IAttachableToEntity attrAtta;

	private Dictionary<int, MultiTextureMeshRef> meshrefs => ObjectCacheUtil.GetOrCreate(api, "shieldmeshrefs", () => new Dictionary<int, MultiTextureMeshRef>());

	public string Construction => Variant["construction"];

	public int RequiresBehindSlots { get; set; }

	string IAttachableToEntity.GetCategoryCode(ItemStack stack)
	{
		return attrAtta?.GetCategoryCode(stack);
	}

	CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
	{
		return attrAtta.GetAttachedShape(stack, slotCode);
	}

	string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
	{
		return attrAtta.GetDisableElements(stack);
	}

	string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
	{
		return attrAtta.GetKeepElements(stack);
	}

	string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack)
	{
		string[] source = new string[4] { "wood", "metal", "color", "deco" };
		source = source.Select((string code) => stack.Attributes.GetString(code)).ToArray();
		return attrAtta.GetTexturePrefixCode(stack) + "-" + string.Join("-", source);
	}

	void IAttachableToEntity.CollectTextures(ItemStack itemstack, Shape intoShape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		foreach (KeyValuePair<string, AssetLocation> texture in genTextureSource(itemstack, null).Textures)
		{
			intoShape.Textures[texture.Key] = texture.Value;
		}
	}

	public bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		return true;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			curOffY = (offY = FpHandTransform.Translation.Y);
		}
		durabilityGains = Attributes["durabilityGains"].AsObject<Dictionary<string, Dictionary<string, int>>>();
		AddAllTypesToCreativeInventory();
		attrAtta = IAttachableToEntity.FromAttributes(this);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (api.ObjectCache.ContainsKey("shieldmeshrefs") && meshrefs.Count > 0)
		{
			foreach (KeyValuePair<int, MultiTextureMeshRef> meshref in meshrefs)
			{
				meshref.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "shieldmeshrefs");
		}
		base.OnUnloaded(api);
	}

	public override int GetMaxDurability(ItemStack itemstack)
	{
		int num = 0;
		foreach (KeyValuePair<string, Dictionary<string, int>> durabilityGain in durabilityGains)
		{
			string text = itemstack.Attributes.GetString(durabilityGain.Key);
			if (text != null)
			{
				durabilityGain.Value.TryGetValue(text, out var value);
				num += value;
			}
		}
		return base.GetMaxDurability(itemstack) + num;
	}

	public void AddAllTypesToCreativeInventory()
	{
		if (Construction == "crude" || Construction == "blackguard")
		{
			return;
		}
		List<JsonItemStack> list = new List<JsonItemStack>();
		Dictionary<string, string[]> dictionary = Attributes["variantGroups"].AsObject<Dictionary<string, string[]>>();
		string[] array = dictionary["metal"];
		foreach (string arg in array)
		{
			switch (Construction)
			{
			case "woodmetal":
			{
				string[] array2 = dictionary["wood"];
				foreach (string arg2 in array2)
				{
					list.Add(genJstack($"{{ wood: \"{arg2}\", metal: \"{arg}\", deco: \"none\" }}"));
				}
				break;
			}
			case "woodmetalleather":
			{
				string[] array2 = dictionary["color"];
				foreach (string text2 in array2)
				{
					list.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"none\" }}", "generic", arg, text2)));
					if (text2 != "redblack")
					{
						list.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"ornate\" }}", "generic", arg, text2)));
					}
				}
				break;
			}
			case "metal":
			{
				list.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", deco: \"none\" }}", "generic", arg)));
				string[] array2 = dictionary["color"];
				foreach (string text in array2)
				{
					if (text != "redblack")
					{
						list.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"ornate\" }}", "generic", arg, text)));
					}
				}
				break;
			}
			}
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = list.ToArray(),
				Tabs = new string[3] { "general", "items", "tools" }
			}
		};
	}

	private JsonItemStack genJstack(string json)
	{
		JsonItemStack jsonItemStack = new JsonItemStack();
		jsonItemStack.Code = Code;
		jsonItemStack.Type = EnumItemClass.Item;
		jsonItemStack.Attributes = new JsonObject(JToken.Parse(json));
		jsonItemStack.Resolve(api.World, "shield type");
		return jsonItemStack;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		int num = itemstack.TempAttributes.GetInt("meshRefId");
		if (num == 0 || !meshrefs.TryGetValue(num, out renderinfo.ModelRef))
		{
			int num2 = meshrefs.Count + 1;
			MultiTextureMeshRef multiTextureMeshRef = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas));
			ItemRenderInfo obj = renderinfo;
			MultiTextureMeshRef modelRef = (meshrefs[num2] = multiTextureMeshRef);
			obj.ModelRef = modelRef;
			itemstack.TempAttributes.SetInt("meshRefId", num2);
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		string text = ((byEntity.LeftHandItemSlot == slot) ? "left" : "right");
		string text2 = ((byEntity.LeftHandItemSlot == slot) ? "right" : "left");
		if (byEntity.Controls.Sneak && !byEntity.Controls.RightMouseDown)
		{
			if (!byEntity.AnimManager.IsAnimationActive("raiseshield-" + text))
			{
				byEntity.AnimManager.StartAnimation("raiseshield-" + text);
			}
		}
		else if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + text))
		{
			byEntity.AnimManager.StopAnimation("raiseshield-" + text);
		}
		if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + text2))
		{
			byEntity.AnimManager.StopAnimation("raiseshield-" + text2);
		}
		base.OnHeldIdle(slot, byEntity);
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
	{
		ContainedTextureSource containedTextureSource = genTextureSource(itemstack, targetAtlas);
		if (containedTextureSource == null)
		{
			return new MeshData();
		}
		capi.Tesselator.TesselateItem(this, out var modeldata, containedTextureSource);
		return modeldata;
	}

	private ContainedTextureSource genTextureSource(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
	{
		ContainedTextureSource containedTextureSource = new ContainedTextureSource(api as ICoreClientAPI, targetAtlas, new Dictionary<string, AssetLocation>(), $"For render in shield {Code}");
		containedTextureSource.Textures.Clear();
		string text = itemstack.Attributes.GetString("wood");
		string text2 = itemstack.Attributes.GetString("metal");
		string text3 = itemstack.Attributes.GetString("color");
		string text4 = itemstack.Attributes.GetString("deco");
		if (text == null && text2 == null && Construction != "crude" && Construction != "blackguard")
		{
			return null;
		}
		if (text == null || text == "")
		{
			text = "generic";
		}
		Dictionary<string, AssetLocation> textures = containedTextureSource.Textures;
		Dictionary<string, AssetLocation> textures2 = containedTextureSource.Textures;
		AssetLocation assetLocation = (containedTextureSource.Textures["handle"] = new AssetLocation("block/wood/planks/generic.png"));
		AssetLocation value = (textures2["back"] = assetLocation);
		textures["front"] = value;
		foreach (KeyValuePair<string, AssetLocation> texture in capi.TesselatorManager.GetCachedShape(Shape.Base).Textures)
		{
			containedTextureSource.Textures[texture.Key] = texture.Value;
		}
		switch (Construction)
		{
		case "woodmetal":
			if (text != "generic")
			{
				Dictionary<string, AssetLocation> textures5 = containedTextureSource.Textures;
				Dictionary<string, AssetLocation> textures6 = containedTextureSource.Textures;
				assetLocation = (containedTextureSource.Textures["front"] = new AssetLocation("block/wood/debarked/" + text + ".png"));
				value = (textures6["back"] = assetLocation);
				textures5["handle"] = value;
			}
			containedTextureSource.Textures["rim"] = new AssetLocation("block/metal/sheet/" + text2 + "1.png");
			if (text4 == "ornate")
			{
				containedTextureSource.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + text3 + ".png");
			}
			break;
		case "woodmetalleather":
			if (text != "generic")
			{
				Dictionary<string, AssetLocation> textures7 = containedTextureSource.Textures;
				Dictionary<string, AssetLocation> textures8 = containedTextureSource.Textures;
				assetLocation = (containedTextureSource.Textures["front"] = new AssetLocation("block/wood/debarked/" + text + ".png"));
				value = (textures8["back"] = assetLocation);
				textures7["handle"] = value;
			}
			containedTextureSource.Textures["front"] = new AssetLocation("item/tool/shield/leather/" + text3 + ".png");
			containedTextureSource.Textures["rim"] = new AssetLocation("block/metal/sheet/" + text2 + "1.png");
			if (text4 == "ornate")
			{
				containedTextureSource.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + text3 + ".png");
			}
			break;
		case "metal":
		{
			Dictionary<string, AssetLocation> textures3 = containedTextureSource.Textures;
			value = (containedTextureSource.Textures["handle"] = new AssetLocation("block/metal/sheet/" + text2 + "1.png"));
			textures3["rim"] = value;
			Dictionary<string, AssetLocation> textures4 = containedTextureSource.Textures;
			value = (containedTextureSource.Textures["back"] = new AssetLocation("block/metal/plate/" + text2 + ".png"));
			textures4["front"] = value;
			if (text4 == "ornate")
			{
				containedTextureSource.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + text3 + ".png");
			}
			break;
		}
		}
		return containedTextureSource;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		bool flag = itemStack.Attributes.GetString("deco") == "ornate";
		string text = itemStack.Attributes.GetString("metal");
		string text2 = itemStack.Attributes.GetString("wood");
		string text3 = itemStack.Attributes.GetString("color");
		switch (Construction)
		{
		case "crude":
			return Lang.Get("Crude shield");
		case "woodmetal":
			if (text2 == "generic")
			{
				if (!flag)
				{
					return Lang.Get("Wooden shield");
				}
				return Lang.Get("Ornate wooden shield");
			}
			if (text2 == "aged")
			{
				if (!flag)
				{
					return Lang.Get("Aged wooden shield");
				}
				return Lang.Get("Aged ornate shield");
			}
			if (!flag)
			{
				return Lang.Get("{0} shield", Lang.Get("material-" + text2));
			}
			return Lang.Get("Ornate {0} shield", Lang.Get("material-" + text2));
		case "woodmetalleather":
			if (!flag)
			{
				return Lang.Get("Leather reinforced wooden shield");
			}
			return Lang.Get("Ornate leather reinforced wooden shield");
		case "metal":
			if (!flag)
			{
				return Lang.Get("shield-withmaterial", Lang.Get("material-" + text));
			}
			return Lang.Get("shield-ornatemetal", Lang.Get("color-" + text3), Lang.Get("material-" + text));
		case "blackguard":
			return Lang.Get("Blackguard shield");
		default:
			return base.GetHeldItemName(itemStack);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		JsonObject jsonObject = inSlot.Itemstack?.ItemAttributes?["shield"];
		if (jsonObject == null || !jsonObject.Exists)
		{
			return;
		}
		if (jsonObject["protectionChance"]["active-projectile"].Exists)
		{
			float num = jsonObject["protectionChance"]["active-projectile"].AsFloat();
			float num2 = jsonObject["protectionChance"]["passive-projectile"].AsFloat();
			float num3 = jsonObject["projectileDamageAbsorption"].AsFloat();
			dsc.AppendLine("<strong>" + Lang.Get("Projectile protection") + "</strong>");
			dsc.AppendLine(Lang.Get("shield-stats", (int)(100f * num), (int)(100f * num2), num3));
			dsc.AppendLine();
		}
		float num4 = jsonObject["damageAbsorption"].AsFloat();
		float num5 = jsonObject["protectionChance"]["active"].AsFloat();
		float num6 = jsonObject["protectionChance"]["passive"].AsFloat();
		dsc.AppendLine("<strong>" + Lang.Get("Melee attack protection") + "</strong>");
		dsc.AppendLine(Lang.Get("shield-stats", (int)(100f * num5), (int)(100f * num6), num4));
		dsc.AppendLine();
		string construction = Construction;
		if (!(construction == "woodmetal"))
		{
			if (construction == "woodmetalleather")
			{
				dsc.AppendLine(Lang.Get("shield-metaltype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("metal"))));
			}
		}
		else
		{
			dsc.AppendLine(Lang.Get("shield-woodtype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("wood"))));
			dsc.AppendLine(Lang.Get("shield-metaltype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("metal"))));
		}
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
	{
		return GenMesh(itemstack, targetAtlas);
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		string text = itemstack.Attributes.GetString("wood");
		string text2 = itemstack.Attributes.GetString("metal");
		string text3 = itemstack.Attributes.GetString("color");
		string text4 = itemstack.Attributes.GetString("deco");
		return Code.ToShortString() + "-" + text + "-" + text2 + "-" + text3 + "-" + text4;
	}
}
