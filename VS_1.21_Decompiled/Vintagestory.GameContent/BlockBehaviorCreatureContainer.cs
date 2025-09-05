using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorCreatureContainer : BlockBehavior
{
	public double CreatureSurvivalDays = 1.0;

	private ICoreAPI api;

	private static Dictionary<string, MultiTextureMeshRef> containedMeshrefs = new Dictionary<string, MultiTextureMeshRef>();

	public BlockBehaviorCreatureContainer(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
	}

	public bool HasAnimal(ItemStack itemStack)
	{
		return itemStack.Attributes?.HasAttribute("animalSerialized") ?? false;
	}

	public static double GetStillAliveDays(IWorldAccessor world, ItemStack itemStack)
	{
		return itemStack.Block.GetBehavior<BlockBehaviorCreatureContainer>().CreatureSurvivalDays - (world.Calendar.TotalDays - itemStack.Attributes.GetDouble("totalDaysCaught"));
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (HasAnimal(itemstack))
		{
			string text = itemstack.Attributes.GetString("type", "");
			string text2 = itemstack.Collectible.Attributes?["creatureContainedShape"][text].AsString();
			if (GetStillAliveDays(capi.World, itemstack) > 0.0)
			{
				float num = itemstack.TempAttributes.GetFloat("triesToEscape") - renderinfo.dt;
				if (api.World.Rand.NextDouble() < 0.001)
				{
					num = 1f + (float)api.World.Rand.NextDouble() * 2f;
				}
				itemstack.TempAttributes.SetFloat("triesToEscape", num);
				if (num > 0f)
				{
					if (api.World.Rand.NextDouble() < 0.05)
					{
						itemstack.TempAttributes.SetFloat("wiggle", 0.05f + (float)api.World.Rand.NextDouble() / 10f);
					}
					float num2 = itemstack.TempAttributes.GetFloat("wiggle") - renderinfo.dt;
					itemstack.TempAttributes.SetFloat("wiggle", num2);
					if (num2 > 0f)
					{
						if (text2 != null)
						{
							text2 += "-wiggle";
						}
						renderinfo.Transform = renderinfo.Transform.Clone();
						float num3 = (float)api.World.Rand.NextDouble() * 4f - 2f;
						float num4 = (float)api.World.Rand.NextDouble() * 4f - 2f;
						if (target != EnumItemRenderTarget.Gui)
						{
							num3 /= 25f;
							num4 /= 25f;
						}
						if (target == EnumItemRenderTarget.Ground)
						{
							num3 /= 4f;
							num4 /= 4f;
						}
						renderinfo.Transform.EnsureDefaultValues();
						renderinfo.Transform.Translation.X += num3;
						renderinfo.Transform.Translation.Z += num4;
					}
				}
			}
			if (text2 != null)
			{
				if (!containedMeshrefs.TryGetValue(text2 + text, out var value))
				{
					Shape shapeBase = capi.Assets.TryGet(new AssetLocation(text2).WithPathPrefix("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
					ITexPositionSource texPositionSource = capi.Tesselator.GetTextureSource(block);
					if (block is BlockGenericTypedContainer)
					{
						texPositionSource = new GenericContainerTextureSource
						{
							blockTextureSource = texPositionSource,
							curType = text
						};
					}
					capi.Tesselator.TesselateShape("creature container shape", shapeBase, out var modeldata, texPositionSource, new Vec3f(0f, 270f, 0f), 0, 0, 0);
					value = (containedMeshrefs[text2 + text] = capi.Render.UploadMultiTextureMesh(modeldata));
				}
				renderinfo.ModelRef = value;
			}
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		foreach (KeyValuePair<string, MultiTextureMeshRef> containedMeshref in containedMeshrefs)
		{
			containedMeshref.Value.Dispose();
		}
		containedMeshrefs.Clear();
	}

	public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
	{
		if (HasAnimal(itemstack))
		{
			handling = EnumHandling.PreventDefault;
			return EnumItemStorageFlags.Backpack;
		}
		return base.GetStorageFlags(itemstack, ref handling);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (HasAnimal(activeHotbarSlot.Itemstack))
		{
			handling = EnumHandling.PreventSubsequent;
			if (world.Side == EnumAppSide.Client)
			{
				handling = EnumHandling.PreventSubsequent;
				return false;
			}
			if (!ReleaseCreature(activeHotbarSlot, blockSel, byPlayer.Entity))
			{
				failureCode = "creaturenotplaceablehere";
			}
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		IServerPlayer player = (byEntity as EntityPlayer).Player as IServerPlayer;
		ICoreServerAPI coreServerAPI = api as ICoreServerAPI;
		BlockPos blockPos = blockSel?.Position ?? entitySel?.Position?.AsBlockPos;
		if (blockPos == null || !api.World.Claims.TryAccess((byEntity as EntityPlayer).Player, blockPos, EnumBlockAccessFlags.Use))
		{
			return;
		}
		if (HasAnimal(slot.Itemstack))
		{
			if (blockSel == null)
			{
				base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
				return;
			}
			if (!ReleaseCreature(slot, blockSel, byEntity))
			{
				coreServerAPI?.SendIngameError(player, "nospace", Lang.Get("Not enough space to release animal here"));
			}
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventDefault;
			slot.MarkDirty();
		}
		else
		{
			if (entitySel == null || !entitySel.Entity.Alive || entitySel.Entity is EntityBoat)
			{
				return;
			}
			if (!IsCatchableAtThisGeneration(entitySel.Entity))
			{
				(byEntity.Api as ICoreClientAPI)?.TriggerIngameError(this, "toowildtocatch", Lang.Get("animaltrap-toowildtocatch-error"));
				return;
			}
			if (!IsCatchableInThisTrap(entitySel.Entity))
			{
				(byEntity.Api as ICoreClientAPI)?.TriggerIngameError(this, "notcatchable", Lang.Get("animaltrap-notcatchable-error"));
				return;
			}
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventDefault;
			ItemSlot itemSlot = null;
			if (slot is ItemSlotBackpack)
			{
				itemSlot = slot;
			}
			else
			{
				IInventory inventory = (byEntity as EntityPlayer)?.Player?.InventoryManager.GetOwnInventory("backpack");
				if (inventory != null)
				{
					itemSlot = inventory.Where((ItemSlot itemSlot2) => itemSlot2 is ItemSlotBackpack).FirstOrDefault((ItemSlot itemSlot2) => itemSlot2.Empty);
				}
			}
			if (itemSlot == null)
			{
				coreServerAPI?.SendIngameError(player, "canthold", Lang.Get("Must have empty backpack slot to catch an animal"));
				return;
			}
			ItemStack itemstack = null;
			if (slot.StackSize > 1)
			{
				itemstack = slot.TakeOut(slot.StackSize - 1);
			}
			CatchCreature(slot, entitySel.Entity);
			slot.TryFlipWith(itemSlot);
			if (slot.Empty)
			{
				slot.Itemstack = itemstack;
			}
			else if (!byEntity.TryGiveItemStack(itemstack))
			{
				byEntity.World.SpawnItemEntity(itemstack, byEntity.ServerPos.XYZ);
			}
			slot.MarkDirty();
			itemSlot.MarkDirty();
		}
	}

	private bool IsCatchableInThisTrap(Entity entity)
	{
		Dictionary<string, TrapChances> dictionary = TrapChances.FromEntityAttr(entity);
		if (dictionary != null && dictionary.TryGetValue(block.Attributes["traptype"].AsString("small"), out var value))
		{
			return value.TrapChance > 0f;
		}
		return false;
	}

	private bool IsCatchableAtThisGeneration(Entity entity)
	{
		return entity.WatchedAttributes.GetAsInt("generation") >= (entity.Properties.Attributes?["trapPickupGeneration"].AsInt(5) ?? 5);
	}

	public static void CatchCreature(ItemSlot slot, Entity entity)
	{
		if (entity.World.Side != EnumAppSide.Client)
		{
			ItemStack itemstack = slot.Itemstack;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(memoryStream);
				entity.ToBytes(writer, forClient: false);
				itemstack.Attributes.SetString("classname", entity.Api.ClassRegistry.GetEntityClassName(entity.GetType()));
				itemstack.Attributes.SetString("creaturecode", entity.Code.ToShortString());
				itemstack.Attributes.SetBytes("animalSerialized", memoryStream.ToArray());
				double num = entity.Attributes.GetDouble("totalDaysReleased");
				double num2 = num - entity.Attributes.GetDouble("totalDaysCaught");
				double num3 = entity.World.Calendar.TotalDays - num;
				double num4 = Math.Max(0.0, num2 - num3 * 2.0);
				itemstack.Attributes.SetDouble("totalDaysCaught", entity.World.Calendar.TotalDays - num4);
			}
			entity.Die(EnumDespawnReason.PickedUp);
		}
	}

	public static bool ReleaseCreature(ItemSlot slot, BlockSelection blockSel, Entity byEntity)
	{
		IWorldAccessor world = byEntity.World;
		if (world.Side == EnumAppSide.Client)
		{
			return true;
		}
		string entityClass = slot.Itemstack.Attributes.GetString("classname");
		string creaturecode = slot.Itemstack.Attributes.GetString("creaturecode");
		Entity entity = world.Api.ClassRegistry.CreateEntity(entityClass);
		EntityProperties entityProperties = world.EntityTypes.FirstOrDefault((EntityProperties type) => type.Code.ToShortString() == creaturecode);
		if (entityProperties == null)
		{
			return false;
		}
		ItemStack itemstack = slot.Itemstack;
		using (MemoryStream input = new MemoryStream(slot.Itemstack.Attributes.GetBytes("animalSerialized")))
		{
			BinaryReader reader = new BinaryReader(input);
			entity.FromBytes(reader, isSync: false, ((IServerWorldAccessor)world).RemappedEntities);
			Vec3d fullPosition = blockSel.FullPosition;
			Cuboidf entityBoxRel = entityProperties.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
			if (world.CollisionTester.IsColliding(world.BlockAccessor, entityBoxRel, fullPosition, alsoCheckTouch: false))
			{
				return false;
			}
			entity.ServerPos.X = (float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f;
			entity.ServerPos.Y = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
			entity.ServerPos.Z = (float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f;
			entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
			entity.Pos.SetFrom(entity.ServerPos);
			entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			entity.Attributes.SetString("origin", "playerplaced");
			entity.Attributes.SetDouble("totalDaysCaught", itemstack.Attributes.GetDouble("totalDaysCaught"));
			entity.Attributes.SetDouble("totalDaysReleased", world.Calendar.TotalDays);
			world.SpawnEntity(entity);
			if (GetStillAliveDays(world, slot.Itemstack) < 0.0)
			{
				(world.Api as ICoreServerAPI).Event.EnqueueMainThreadTask(delegate
				{
					entity.Properties.ResolvedSounds = null;
					entity.Die(EnumDespawnReason.Death, new DamageSource
					{
						CauseEntity = byEntity,
						Type = EnumDamageType.Hunger
					});
				}, "die");
			}
			itemstack.Attributes.RemoveAttribute("classname");
			itemstack.Attributes.RemoveAttribute("creaturecode");
			itemstack.Attributes.RemoveAttribute("animalSerialized");
			itemstack.Attributes.RemoveAttribute("totalDaysCaught");
		}
		return true;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		AddCreatureInfo(inSlot.Itemstack, dsc, world);
	}

	public void AddCreatureInfo(ItemStack stack, StringBuilder dsc, IWorldAccessor world)
	{
		if (HasAnimal(stack))
		{
			if (GetStillAliveDays(world, stack) > 0.0)
			{
				dsc.AppendLine(Lang.Get("Contains a frightened {0}", Lang.Get("item-creature-" + stack.Attributes.GetString("creaturecode"))));
				dsc.AppendLine(Lang.Get("It remains alive for {0:0.##} more hours", GetStillAliveDays(world, stack) * (double)world.Calendar.HoursPerDay));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Contains a dead {0}", Lang.Get("item-creature-" + stack.Attributes.GetString("creaturecode"))));
			}
		}
	}
}
