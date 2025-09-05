using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBoatConstruction : Entity
{
	private ConstructionStage[] stages;

	private string material = "oak";

	private Vec3f launchStartPos = new Vec3f();

	private Dictionary<string, string> storedWildCards = new Dictionary<string, string>();

	private WorldInteraction[] nextConstructWis;

	private EntityAgent launchingEntity;

	public override double FrustumSphereRadius => base.FrustumSphereRadius * 2.0;

	private int CurrentStage
	{
		get
		{
			return WatchedAttributes.GetInt("currentStage");
		}
		set
		{
			WatchedAttributes.SetInt("currentStage", value);
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		requirePosesOnServer = true;
		WatchedAttributes.RegisterModifiedListener("currentStage", stagedChanged);
		WatchedAttributes.RegisterModifiedListener("wildcards", loadWildcards);
		base.Initialize(properties, api, InChunkIndex3d);
		stages = properties.Attributes["stages"].AsArray<ConstructionStage>();
		genNextInteractionStage();
	}

	private void stagedChanged()
	{
		MarkShapeModified();
		genNextInteractionStage();
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		HashSet<string> hashSet = new HashSet<string>();
		int currentStage = CurrentStage;
		for (int i = 0; i <= currentStage; i++)
		{
			ConstructionStage constructionStage = stages[i];
			if (constructionStage.AddElements != null)
			{
				string[] addElements = constructionStage.AddElements;
				foreach (string text in addElements)
				{
					hashSet.Add(text + "/*");
				}
			}
			if (constructionStage.RemoveElements != null)
			{
				string[] addElements = constructionStage.RemoveElements;
				foreach (string text2 in addElements)
				{
					hashSet.Remove(text2 + "/*");
				}
			}
		}
		if (base.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
		{
			entityShapeRenderer.OverrideSelectiveElements = hashSet.ToArray();
		}
		if (Api is ICoreClientAPI)
		{
			setTexture("debarked", new AssetLocation($"block/wood/debarked/{material}"));
			setTexture("planks", new AssetLocation($"block/wood/planks/{material}1"));
		}
		base.OnTesselation(ref entityShape, shapePathForLogging);
	}

	private void setTexture(string code, AssetLocation assetLocation)
	{
		ICoreClientAPI obj = Api as ICoreClientAPI;
		CompositeTexture compositeTexture = (base.Properties.Client.Textures[code] = new CompositeTexture(assetLocation));
		CompositeTexture compositeTexture3 = compositeTexture;
		obj.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3, out var textureSubId, out var _);
		compositeTexture3.Baked.TextureSubId = textureSubId;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot handslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		base.OnInteract(byEntity, handslot, hitPosition, mode);
		if (Api.Side == EnumAppSide.Client || CurrentStage >= stages.Length - 1)
		{
			return;
		}
		if (CurrentStage == 0 && handslot.Empty && byEntity.Controls.ShiftKey)
		{
			byEntity.TryGiveItemStack(new ItemStack(Api.World.GetItem(new AssetLocation("roller")), 5));
			Die();
		}
		else if (tryConsumeIngredients(byEntity, handslot))
		{
			if (CurrentStage < stages.Length - 1)
			{
				CurrentStage++;
				MarkShapeModified();
			}
			if (CurrentStage >= stages.Length - 2 && !AnimManager.IsAnimationActive("launch"))
			{
				launchingEntity = byEntity;
				launchStartPos = getCenterPos();
				StartAnimation("launch");
			}
			genNextInteractionStage();
		}
	}

	private Vec3f getCenterPos()
	{
		AttachmentPointAndPose attachmentPointAndPose = AnimManager.Animator?.GetAttachmentPointPose("Center");
		if (attachmentPointAndPose != null)
		{
			Matrixf matrixf = new Matrixf();
			matrixf.RotateY(ServerPos.Yaw + (float)Math.PI / 2f);
			attachmentPointAndPose.Mul(matrixf);
			return matrixf.TransformVector(new Vec4f(0f, 0f, 0f, 1f)).XYZ;
		}
		return null;
	}

	private void genNextInteractionStage()
	{
		if (CurrentStage + 1 >= stages.Length)
		{
			nextConstructWis = null;
			return;
		}
		ConstructionStage constructionStage = stages[CurrentStage + 1];
		if (constructionStage.RequireStacks == null)
		{
			nextConstructWis = null;
			return;
		}
		List<WorldInteraction> list = new List<WorldInteraction>();
		int num = 0;
		ConstructionIgredient[] requireStacks = constructionStage.RequireStacks;
		foreach (ConstructionIgredient constructionIgredient in requireStacks)
		{
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (KeyValuePair<string, string> storedWildCard in storedWildCards)
			{
				constructionIgredient.FillPlaceHolder(storedWildCard.Key, storedWildCard.Value);
			}
			if (!constructionIgredient.Resolve(Api.World, "Require stack for construction stage " + (CurrentStage + 1) + " on entity " + Code))
			{
				return;
			}
			num++;
			foreach (CollectibleObject collectible in Api.World.Collectibles)
			{
				ItemStack itemStack = new ItemStack(collectible);
				if (constructionIgredient.SatisfiesAsIngredient(itemStack, checkStacksize: false))
				{
					itemStack.StackSize = constructionIgredient.Quantity;
					list2.Add(itemStack);
				}
			}
			ItemStack[] stacks = list2.ToArray();
			list.Add(new WorldInteraction
			{
				ActionLangCode = constructionStage.ActionLangCode,
				Itemstacks = stacks,
				GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => stacks,
				MouseButton = EnumMouseButton.Right
			});
		}
		if (constructionStage.RequireStacks.Length == 0)
		{
			list.Add(new WorldInteraction
			{
				ActionLangCode = constructionStage.ActionLangCode,
				MouseButton = EnumMouseButton.Right
			});
		}
		nextConstructWis = list.ToArray();
	}

	private bool tryConsumeIngredients(EntityAgent byEntity, ItemSlot handslot)
	{
		_ = Api;
		IServerPlayer serverPlayer = (byEntity as EntityPlayer).Player as IServerPlayer;
		ConstructionStage constructionStage = stages[CurrentStage + 1];
		IInventory hotbarInventory = serverPlayer.InventoryManager.GetHotbarInventory();
		List<KeyValuePair<ItemSlot, int>> list = new List<KeyValuePair<ItemSlot, int>>();
		List<ConstructionIgredient> list2 = new List<ConstructionIgredient>();
		if (constructionStage.RequireStacks == null)
		{
			return true;
		}
		for (int i = 0; i < constructionStage.RequireStacks.Length; i++)
		{
			list2.Add(constructionStage.RequireStacks[i].Clone());
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		bool flag = serverPlayer != null && serverPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && byEntity.Controls.CtrlKey;
		foreach (ItemSlot item in hotbarInventory)
		{
			if (item.Empty)
			{
				continue;
			}
			if (list2.Count == 0)
			{
				break;
			}
			for (int j = 0; j < list2.Count; j++)
			{
				ConstructionIgredient constructionIgredient = list2[j];
				foreach (KeyValuePair<string, string> storedWildCard in storedWildCards)
				{
					constructionIgredient.FillPlaceHolder(storedWildCard.Key, storedWildCard.Value);
				}
				constructionIgredient.Resolve(Api.World, "Require stack for construction stage " + j + " on entity " + Code);
				if (!flag && constructionIgredient.SatisfiesAsIngredient(item.Itemstack, checkStacksize: false))
				{
					int num = Math.Min(constructionIgredient.Quantity, item.Itemstack.StackSize);
					list.Add(new KeyValuePair<ItemSlot, int>(item, num));
					constructionIgredient.Quantity -= num;
					if (constructionIgredient.Quantity <= 0)
					{
						list2.RemoveAt(j);
						j--;
						if (constructionIgredient.StoreWildCard != null)
						{
							dictionary[constructionIgredient.StoreWildCard] = item.Itemstack.Collectible.Variant[constructionIgredient.StoreWildCard];
						}
					}
				}
				else if (flag && constructionIgredient.StoreWildCard != null)
				{
					dictionary[constructionIgredient.StoreWildCard] = item.Itemstack.Collectible.Variant[constructionIgredient.StoreWildCard];
				}
			}
		}
		if (!flag && list2.Count > 0)
		{
			ConstructionIgredient constructionIgredient2 = list2[0];
			string languageCode = serverPlayer.LanguageCode;
			serverPlayer.SendIngameError("missingstack", null, constructionIgredient2.Quantity, constructionIgredient2.IsWildCard ? Lang.GetL(languageCode, constructionIgredient2.Name ?? "") : constructionIgredient2.ResolvedItemstack.GetName());
			return false;
		}
		foreach (KeyValuePair<string, string> item2 in dictionary)
		{
			storedWildCards[item2.Key] = item2.Value;
		}
		if (!flag)
		{
			bool flag2 = false;
			foreach (KeyValuePair<ItemSlot, int> item3 in list)
			{
				if (!flag2)
				{
					AssetLocation assetLocation = null;
					ItemStack itemstack = item3.Key.Itemstack;
					if (itemstack.Block != null)
					{
						assetLocation = itemstack.Block.Sounds?.Place;
					}
					if (assetLocation == null)
					{
						assetLocation = itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps?.PlaceRemoveSound;
					}
					if (assetLocation != null)
					{
						flag2 = true;
						Api.World.PlaySoundAt(assetLocation, this);
					}
				}
				item3.Key.TakeOut(item3.Value);
				item3.Key.MarkDirty();
			}
		}
		storeWildcards();
		WatchedAttributes.MarkPathDirty("wildcards");
		return true;
	}

	private ItemSlot tryTakeFrom(CraftingRecipeIngredient requireStack, List<ItemSlot> skipSlots, IReadOnlyCollection<ItemSlot> fromSlots)
	{
		foreach (ItemSlot fromSlot in fromSlots)
		{
			if (!fromSlot.Empty && !skipSlots.Contains(fromSlot) && requireStack.SatisfiesAsIngredient(fromSlot.Itemstack))
			{
				return fromSlot;
			}
		}
		return null;
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if ((double)(AnimManager.Animator?.GetAnimationState("launch").AnimProgress ?? 0f) >= 0.99)
		{
			AnimManager.StopAnimation("launch");
			CurrentStage = 0;
			MarkShapeModified();
			if (World.Side == EnumAppSide.Server)
			{
				Spawn();
			}
		}
	}

	private void Spawn()
	{
		Vec3f centerPos = getCenterPos();
		Vec3f vec3f = ((centerPos == null) ? new Vec3f() : (centerPos - launchStartPos));
		EntityProperties entityType = World.GetEntityType(new AssetLocation("boat-sailed-" + material));
		Entity entity = World.ClassRegistry.CreateEntity(entityType);
		if ((int)Math.Abs(ServerPos.Yaw * (180f / (float)Math.PI)) == 90 || (int)Math.Abs(ServerPos.Yaw * (180f / (float)Math.PI)) == 270)
		{
			vec3f.X *= 1.1f;
		}
		vec3f.Y = 0.5f;
		entity.ServerPos.SetFrom(ServerPos).Add(vec3f);
		entity.ServerPos.Motion.Add((double)vec3f.X / 50.0, 0.0, (double)vec3f.Z / 50.0);
		IPlayer player = (launchingEntity as EntityPlayer)?.Player;
		if (player != null)
		{
			entity.WatchedAttributes.SetString("createdByPlayername", player.PlayerName);
			entity.WatchedAttributes.SetString("createdByPlayerUID", player.PlayerUID);
		}
		entity.Pos.SetFrom(entity.ServerPos);
		World.SpawnEntity(entity);
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		WorldInteraction[] interactionHelp = base.GetInteractionHelp(world, es, player);
		if (nextConstructWis == null)
		{
			return interactionHelp;
		}
		interactionHelp = interactionHelp.Append(nextConstructWis);
		if (CurrentStage == 0)
		{
			interactionHelp = interactionHelp.Append(new WorldInteraction
			{
				HotKeyCode = "sneak",
				RequireFreeHand = true,
				MouseButton = EnumMouseButton.Right,
				ActionLangCode = "rollers-deconstruct"
			});
		}
		return interactionHelp;
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		storeWildcards();
		base.ToBytes(writer, forClient);
	}

	private void storeWildcards()
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		foreach (KeyValuePair<string, string> storedWildCard in storedWildCards)
		{
			treeAttribute[storedWildCard.Key] = new StringAttribute(storedWildCard.Value);
		}
		WatchedAttributes["wildcards"] = treeAttribute;
	}

	public override void FromBytes(BinaryReader reader, bool isSync)
	{
		base.FromBytes(reader, isSync);
		loadWildcards();
	}

	public override string GetInfoText()
	{
		return base.GetInfoText() + "\n" + Lang.Get("Material: {0}", Lang.Get("material-" + material));
	}

	private void loadWildcards()
	{
		storedWildCards.Clear();
		if (WatchedAttributes["wildcards"] is TreeAttribute treeAttribute)
		{
			foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
			{
				storedWildCards[item.Key] = (item.Value as StringAttribute).value;
			}
		}
		if (storedWildCards.TryGetValue("wood", out var value))
		{
			material = value;
			if (material == null || material.Length == 0)
			{
				storedWildCards["wood"] = (material = "oak");
			}
		}
	}
}
