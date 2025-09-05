using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common.Entities;

public abstract class EntityBehavior
{
	public Entity entity;

	public string ProfilerName { get; private set; }

	public virtual bool ThreadSafe => false;

	public EntityBehavior(Entity entity)
	{
		this.entity = entity;
		ProfilerName = "done-behavior-" + PropertyName();
	}

	public virtual void Initialize(EntityProperties properties, JsonObject attributes)
	{
	}

	public virtual void AfterInitialized(bool onFirstSpawn)
	{
	}

	public virtual void OnGameTick(float deltaTime)
	{
	}

	public virtual void OnEntitySpawn()
	{
	}

	public virtual void OnEntityLoaded()
	{
	}

	public virtual void OnEntityDespawn(EntityDespawnData despawn)
	{
	}

	public abstract string PropertyName();

	public virtual void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
	}

	public virtual void OnEntityRevive()
	{
	}

	public virtual void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
	{
	}

	public virtual void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
	{
	}

	public virtual void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
	}

	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
	{
	}

	public virtual void Notify(string key, object data)
	{
	}

	public virtual void GetInfoText(StringBuilder infotext)
	{
	}

	public virtual void OnEntityDeath(DamageSource damageSourceForDeath)
	{
	}

	public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
	}

	public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
	}

	public virtual void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
	{
	}

	public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return null;
	}

	public virtual void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
	}

	public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
	}

	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
	{
	}

	public virtual void ToBytes(bool forClient)
	{
	}

	public virtual void FromBytes(bool isSync)
	{
	}

	public virtual void TestCommand(object arg)
	{
	}

	public virtual bool TryGiveItemStack(ItemStack itemstack, ref EnumHandling handling)
	{
		return false;
	}

	public virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
	}

	public virtual ITexPositionSource GetTextureSource(ref EnumHandling handling)
	{
		return null;
	}

	public virtual bool IntersectsRay(Ray ray, AABBIntersectionTest interesectionTester, out double intersectionDistance, ref int selectionBoxIndex, ref EnumHandling handled)
	{
		intersectionDistance = 0.0;
		return false;
	}

	public virtual void OnTesselated()
	{
	}

	public virtual void UpdateColSelBoxes()
	{
	}

	public virtual float GetTouchDistance(ref EnumHandling handling)
	{
		return 0f;
	}

	public virtual string GetName(ref EnumHandling handling)
	{
		return null;
	}

	public virtual bool ToleratesDamageFrom(Entity eOther, ref EnumHandling handling)
	{
		return false;
	}
}
