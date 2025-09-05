using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorWritingSurface : EntityBehavior, ITexPositionSource
{
	protected MultiTextureMeshRef meshref;

	protected ICoreClientAPI capi;

	protected LoadedTexture loadedTexture;

	protected TextAreaConfig signTextConfig;

	protected CairoFont font;

	protected EnumVerticalAlign verticalAlign;

	protected int TextWidth = 208;

	protected int TextHeight = 96;

	protected float DefaultFontSize;

	protected string SurfaceName = "leftplaque";

	private int tempColor;

	private ItemStack tempStack;

	private GuiDialogTextInput editDialog;

	private string previousText;

	private int previousColor;

	private float previousFontSize = -1f;

	private TextureAtlasPosition texPos;

	private int textureSubId;

	private Size2i dummysize = new Size2i(2048, 2048);

	private TextureAtlasPosition dummyPos = new TextureAtlasPosition
	{
		x1 = 0f,
		y1 = 0f,
		x2 = 1f,
		y2 = 1f
	};

	public float FontSize
	{
		get
		{
			return entity.WatchedAttributes.GetFloat(SurfaceName + "_fontSize", DefaultFontSize);
		}
		set
		{
			entity.WatchedAttributes.SetFloat(SurfaceName + "_fontSize", value);
		}
	}

	public string Text
	{
		get
		{
			return entity.WatchedAttributes.GetString(SurfaceName + "_writingSurfaceText");
		}
		set
		{
			entity.WatchedAttributes.SetString(SurfaceName + "_writingSurfaceText", value);
		}
	}

	public int Color
	{
		get
		{
			return entity.WatchedAttributes.GetInt(SurfaceName + "_textColor", -16777216);
		}
		set
		{
			entity.WatchedAttributes.SetInt(SurfaceName + "_textColor", value);
		}
	}

	public double RenderOrder => 0.36;

	public int RenderRange => 99;

	public Size2i AtlasSize => dummysize;

	public TextureAtlasPosition this[string textureCode] => dummyPos;

	public EntityBehaviorWritingSurface(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		capi = entity.World.Api as ICoreClientAPI;
		if (capi != null)
		{
			capi.Event.ReloadTextures += Event_ReloadTextures;
			entity.WatchedAttributes.RegisterModifiedListener(SurfaceName + "_writingSurfaceText", entity.MarkShapeModified);
			signTextConfig = attributes["fontConfig"].AsObject<TextAreaConfig>();
			font = new CairoFont(signTextConfig.FontSize, signTextConfig.FontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
			if (signTextConfig.BoldFont)
			{
				font.WithWeight((FontWeight)1);
			}
			font.LineHeightMultiplier = 0.8999999761581421;
			verticalAlign = signTextConfig.VerticalAlign;
			TextWidth = signTextConfig.MaxWidth;
			TextHeight = signTextConfig.MaxHeight;
			DefaultFontSize = signTextConfig.FontSize;
		}
	}

	private void Event_ReloadTextures()
	{
		texPos = null;
		previousText = null;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			return;
		}
		EntityBehaviorSelectionBoxes? behavior = entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior == null || !behavior.IsAPCode((byEntity as EntityPlayer).EntitySelection, "LPlaqueAP") || (editDialog != null && editDialog.IsOpened()))
		{
			return;
		}
		ICoreClientAPI capi = entity.Api as ICoreClientAPI;
		if (loadPigment(byEntity))
		{
			editDialog = new GuiDialogTextInput(Lang.Get("Edit Sign text"), Text, capi, signTextConfig.CopyWithFontSize(FontSize));
			editDialog.OnSave = delegate(string text)
			{
				FontSize = editDialog.FontSize;
				Text = text;
				capi.Network.SendEntityPacket(entity.EntityId, 12312, SerializerUtil.Serialize(new TextDataPacket
				{
					Text = text,
					FontSize = FontSize
				}));
				Color = tempColor;
			};
			editDialog.OnCloseCancel = delegate
			{
				capi.Network.SendEntityPacket(entity.EntityId, 12313);
			};
			editDialog.TryOpen();
			capi.Network.SendEntityPacket(entity.EntityId, 12311);
		}
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		base.OnReceivedClientPacket(player, packetid, data, ref handled);
		if (packetid == 12311)
		{
			loadPigment(player.Entity);
		}
		if (packetid == 12313)
		{
			player.Entity.TryGiveItemStack(tempStack);
			tempStack = null;
		}
		if (packetid == 12312)
		{
			if (entity.World.Rand.NextDouble() < 0.85)
			{
				player.Entity.TryGiveItemStack(tempStack);
			}
			tempStack = null;
			TextDataPacket textDataPacket = SerializerUtil.Deserialize<TextDataPacket>(data);
			Text = textDataPacket.Text;
			FontSize = textDataPacket.FontSize;
			entity.MarkShapeModified();
			Color = tempColor;
		}
	}

	private bool loadPigment(EntityAgent eagent)
	{
		ItemSlot rightHandItemSlot = eagent.RightHandItemSlot;
		if (rightHandItemSlot != null && rightHandItemSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true)
		{
			JsonObject jsonObject = rightHandItemSlot.Itemstack.ItemAttributes["pigment"]["color"];
			int r = jsonObject["red"].AsInt();
			int g = jsonObject["green"].AsInt();
			int b = jsonObject["blue"].AsInt();
			tempColor = ColorUtil.ToRgba(255, r, g, b);
			if (eagent.World.Side == EnumAppSide.Server)
			{
				tempStack = rightHandItemSlot.TakeOut(1);
				rightHandItemSlot.MarkDirty();
			}
			return true;
		}
		return false;
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			return;
		}
		ShapeElementFace[] facesResolved;
		if (previousText == Text && previousColor == Color && previousFontSize == FontSize)
		{
			if (Text != null && Text.Length != 0)
			{
				return;
			}
			if (!shapeIsCloned)
			{
				entityShape = entityShape.Clone();
			}
			ShapeElement elementByName = entityShape.GetElementByName("PlaqueLeftFrontText");
			if (elementByName == null)
			{
				return;
			}
			facesResolved = elementByName.FacesResolved;
			foreach (ShapeElementFace shapeElementFace in facesResolved)
			{
				if (shapeElementFace != null)
				{
					shapeElementFace.Texture = "transparent";
				}
			}
			return;
		}
		previousText = Text;
		previousColor = Color;
		previousFontSize = FontSize;
		ICoreClientAPI coreClientAPI = entity.Api as ICoreClientAPI;
		font.WithColor(ColorUtil.ToRGBADoubles(Color));
		this.loadedTexture?.Dispose();
		this.loadedTexture = null;
		font.UnscaledFontsize = FontSize / RuntimeEnv.GUIScale;
		double num = ((verticalAlign == EnumVerticalAlign.Middle) ? ((double)TextHeight - coreClientAPI.Gui.Text.GetMultilineTextHeight(font, Text, TextWidth)) : 0.0);
		TextBackground background = new TextBackground
		{
			VerPadding = (int)num / 2
		};
		this.loadedTexture = coreClientAPI.Gui.TextTexture.GenTextTexture(Text, font, TextWidth, TextHeight, background, EnumTextOrientation.Center);
		string text = "writingsurface-" + SurfaceName + "-" + entity.EntityId;
		if (texPos == null)
		{
			coreClientAPI.EntityTextureAtlas.AllocateTextureSpace(TextWidth, TextHeight, out textureSubId, out texPos, new AssetLocationAndSource(text));
		}
		CompositeTexture compositeTexture = new CompositeTexture(text);
		entity.Properties.Client.Textures[text] = compositeTexture;
		compositeTexture.Bake(entity.Api.Assets);
		compositeTexture.Baked.TextureSubId = textureSubId;
		LoadedTexture loadedTexture = coreClientAPI.EntityTextureAtlas.AtlasTextures[texPos.atlasNumber];
		coreClientAPI.Render.RenderTextureIntoTexture(this.loadedTexture, 0f, 0f, TextWidth, TextHeight, loadedTexture, texPos.x1 * (float)loadedTexture.Width, texPos.y1 * (float)loadedTexture.Height, -1f);
		coreClientAPI.EntityTextureAtlas.RegenMipMaps(texPos.atlasNumber);
		if (!shapeIsCloned)
		{
			entityShape = entityShape.Clone();
		}
		ShapeElement elementByName2 = entityShape.GetElementByName("PlaqueLeftFrontText");
		if (elementByName2 == null)
		{
			return;
		}
		facesResolved = elementByName2.FacesResolved;
		foreach (ShapeElementFace shapeElementFace2 in facesResolved)
		{
			if (shapeElementFace2 != null)
			{
				shapeElementFace2.Texture = text;
			}
		}
		shapeIsCloned = true;
	}

	public override string PropertyName()
	{
		return "writingsurface";
	}
}
